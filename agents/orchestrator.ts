#!/usr/bin/env -S bun run
/**
 * ORCHESTRATOR: Master agent that runs analysis steps 1-10 (or 1-9 for single forms)
 *
 * This agent coordinates the execution of analysis agents in sequence,
 * passing data between them and orchestrating the complete analysis workflow.
 *
 * Supports both Search/Detail form pairs and single standalone forms.
 * All agents run automatically to gather data.
 * Template generation should be run separately.
 *
 * Usage:
 *   bun run agents/orchestrator.ts --entity "Facility" --form-name "frmFacilitySearch"
 *   bun run agents/orchestrator.ts --form-name "frmFacilitySearch"  (entity extracted from form name)
 *   bun run agents/orchestrator.ts --form-name "frmFuelPrices"  (single form, entity extracted)
 *   bun run agents/orchestrator.ts --entity "Facility"  (form names will be constructed)
 *   bun run agents/orchestrator.ts  (will prompt for form selection)
 *
 * Note: This runs analysis steps only. Template generation should be run separately:
 *       bun run generate-template --entity "Facility"
 */

import { spawn } from "bun";
import { parsedArgs } from "../lib/flags";
import { getProjectRoot, parseEntityFromFormName, getAvailableForms, getFormsDirectory, detectChildForms } from "../lib/paths";
import { mkdir } from "fs/promises";
import { existsSync } from "fs";
import { createInterface } from "readline";

function normalizePath(path: string): string {
	// Normalize Windows paths for file system operations
	if (process.platform === "win32") {
		// Convert /C:/ to C:/ and forward slashes to backslashes for Windows
		if (path.match(/^\/[A-Z]:/)) {
			path = path.substring(1);
		}
		// Keep forward slashes for Bun operations, but normalize for fs operations
		return path;
	}
	return path;
}

const projectRoot = getProjectRoot(import.meta.url);

interface OrchestratorOptions {
	entity: string;
	formName?: string;
	outputDir?: string;
	skipSteps?: number[];
	isSingleForm?: boolean;
	childForms?: string[];
}

interface AgentStep {
	name: string;
	script: string;
	description: string;
	outputFile: string;
	interactive: boolean;
	extraArgs?: string[];
}

/**
 * Prompt user to select a form from available forms
 */
async function promptForForm(): Promise<string | null> {
	const forms = await getAvailableForms();
	
	if (forms.length === 0) {
		console.error("No forms found in the Forms directory.");
		console.error(`Forms directory: ${getFormsDirectory()}`);
		return null;
	}
	
	const rl = createInterface({
		input: process.stdin,
		output: process.stdout,
	});
	
	return new Promise((resolve) => {
		console.log("\nAvailable forms:");
		forms.forEach((form, index) => {
			console.log(`  ${index + 1}. ${form}`);
		});
		
		rl.question("\nEnter the number or name of the form to convert: ", (answer) => {
			rl.close();
			
			// Check if it's a number
			const num = parseInt(answer.trim(), 10);
			if (!isNaN(num) && num >= 1 && num <= forms.length) {
				resolve(forms[num - 1]);
			} else {
				// Check if it's a form name
				const formName = answer.trim();
				if (forms.includes(formName)) {
					resolve(formName);
				} else {
					console.error(`Invalid selection: ${formName}`);
					resolve(null);
				}
			}
		});
	});
}

async function parseOptions(): Promise<OrchestratorOptions> {
	const entity = parsedArgs.values.entity as string;
	const formName = parsedArgs.values["form-name"] as string;
	const outputDir = parsedArgs.values.output as string;
	const skipStepsStr = parsedArgs.values["skip-steps"] as string;
	const skipSteps = skipStepsStr ? skipStepsStr.split(",").map(Number) : [];

	let finalEntity = entity;
	let finalFormName = formName;
	let isSingleForm = false;

	// If form name is provided, try to extract entity from it
	if (formName && !entity) {
		const extractedEntity = parseEntityFromFormName(formName);
		if (extractedEntity) {
			finalEntity = extractedEntity;
			finalFormName = formName;
		} else {
			// Try to extract entity by removing "frm" prefix for non-standard forms
			if (formName.toLowerCase().startsWith("frm")) {
				finalEntity = formName.substring(3);
				finalFormName = formName;
				isSingleForm = true;
				console.log(`Detected single form (non-Search/Detail): ${formName}`);
				console.log(`Extracted entity: ${finalEntity}`);
			} else {
				console.error(`Error: Could not parse entity name from form name "${formName}"`);
				console.error("Expected format: frm{Entity}Search, frm{Entity}Detail, or frm{Entity}");
				process.exit(1);
			}
		}
	}

	// If neither entity nor form-name is provided, prompt for form
	if (!finalEntity && !finalFormName) {
		console.log("No entity or form name provided. Searching for available forms...");
		const selectedForm = await promptForForm();

		if (!selectedForm) {
			console.error("Error: No form selected");
			process.exit(1);
		}

		finalFormName = selectedForm;
		const extractedEntity = parseEntityFromFormName(selectedForm);

		if (extractedEntity) {
			finalEntity = extractedEntity;
		} else {
			// Try to extract entity by removing "frm" prefix for non-standard forms
			if (selectedForm.toLowerCase().startsWith("frm")) {
				finalEntity = selectedForm.substring(3);
				isSingleForm = true;
				console.log(`Detected single form (non-Search/Detail): ${selectedForm}`);
				console.log(`Extracted entity: ${finalEntity}`);
			} else {
				console.error(`Error: Could not parse entity name from form name "${selectedForm}"`);
				process.exit(1);
			}
		}
	}

	// If entity is provided but form-name is not, validate entity exists
	if (finalEntity && !finalFormName) {
		// Entity is provided, form name will be constructed in agents
		// This is fine, continue
	}

	if (!finalEntity) {
		console.error("Error: Entity name is required");
		console.error("Usage: bun run agents/orchestrator.ts --entity \"Facility\" --form-name \"frmFacilitySearch\"");
		console.error("   or: bun run agents/orchestrator.ts --form-name \"frmFacilitySearch\"");
		console.error("   or: bun run agents/orchestrator.ts --form-name \"frmFuelPrices\" (single form)");
		console.error("   or: bun run agents/orchestrator.ts (will prompt for form selection)");
		process.exit(1);
	}

	// Detect child forms if we have a form name
	let childForms: string[] = [];
	if (finalFormName) {
		console.log(`\nScanning for child forms opened by ${finalFormName}...`);
		childForms = await detectChildForms(finalFormName);
		if (childForms.length > 0) {
			console.log(`Found ${childForms.length} child form(s):`);
			childForms.forEach(child => console.log(`  - ${child}`));
		} else {
			console.log("No child forms detected.");
		}
	}

	// Also detect child forms from Search/Detail forms if entity is provided
	if (finalEntity && !isSingleForm) {
		const searchForm = `frm${finalEntity}Search`;
		const detailForm = `frm${finalEntity}Detail`;

		console.log(`\nScanning for child forms in ${finalEntity} Search/Detail forms...`);
		const searchChildren = await detectChildForms(searchForm);
		const detailChildren = await detectChildForms(detailForm);

		const allChildren = new Set([...childForms, ...searchChildren, ...detailChildren]);
		childForms = Array.from(allChildren);

		if (childForms.length > 0) {
			console.log(`Found ${childForms.length} child form(s) total:`);
			childForms.forEach(child => console.log(`  - ${child}`));
		} else {
			console.log("No child forms detected.");
		}
	}

	return { entity: finalEntity, formName: finalFormName, outputDir, skipSteps, isSingleForm, childForms };
}

function getAgentSteps(isSingleForm: boolean, formName?: string): AgentStep[] {
	const steps: AgentStep[] = [];

	if (isSingleForm && formName) {
		// For single forms, run the analyzer once with the specific form name
		steps.push({
			name: `Form Structure Analyzer (${formName})`,
			script: "form-structure-analyzer.ts",
			description: `Extract ${formName} UI components`,
			outputFile: "form-structure.json",
			interactive: false,
			extraArgs: ["--form-name", formName],
		});
	} else {
		// For standard Search/Detail pairs
		steps.push(
			{
				name: "Form Structure Analyzer (Search)",
				script: "form-structure-analyzer.ts",
				description: "Extract search form UI components",
				outputFile: "form-structure-search.json",
				interactive: false,
				extraArgs: ["--form-type", "Search"],
			},
			{
				name: "Form Structure Analyzer (Detail)",
				script: "form-structure-analyzer.ts",
				description: "Extract detail form UI components",
				outputFile: "form-structure-detail.json",
				interactive: false,
				extraArgs: ["--form-type", "Detail"],
			}
		);
	}

	// Common steps for all forms
	steps.push(
		{
			name: "Business Logic Extractor",
			script: "business-logic-extractor.ts",
			description: "Extract business rules and validation",
			outputFile: "business-logic.json",
			interactive: false,
		},
		{
			name: "Data Access Pattern Analyzer",
			script: "data-access-analyzer.ts",
			description: "Extract stored procedures and queries",
			outputFile: "data-access.json",
			interactive: false,
		},
		{
			name: "Security & Authorization Extractor",
			script: "security-extractor.ts",
			description: "Extract permissions and authorization",
			outputFile: "security.json",
			interactive: false,
		},
		{
			name: "UI Component Mapper",
			script: "ui-component-mapper.ts",
			description: "Map legacy controls to modern equivalents",
			outputFile: "ui-mapping.json",
			interactive: false,
		},
		{
			name: "Form Workflow Analyzer",
			script: "form-workflow-analyzer.ts",
			description: "Extract user flows and state management",
			outputFile: "workflow.json",
			interactive: false,
		},
		{
			name: "Detail Form Tab Analyzer",
			script: "detail-tab-analyzer.ts",
			description: "Extract tab structure and related entities",
			outputFile: "tabs.json",
			interactive: false,
		},
		{
			name: "Validation Rule Extractor",
			script: "validation-extractor.ts",
			description: "Extract all validation logic",
			outputFile: "validation.json",
			interactive: false,
		},
		{
			name: "Related Entity Analyzer",
			script: "related-entity-analyzer.ts",
			description: "Extract entity relationships",
			outputFile: "related-entities.json",
			interactive: false,
		}
	);

	return steps;
}

async function runAgentStep(
	step: AgentStep,
	stepNumber: number,
	options: OrchestratorOptions,
	totalSteps: number,
): Promise<number> {
	console.log(`\n${"=".repeat(80)}`);
	console.log(`STEP ${stepNumber}/${totalSteps}: ${step.name}`);
	console.log(`Description: ${step.description}`);
	console.log(`${"=".repeat(80)}\n`);

	const outputPath = options.outputDir || `${projectRoot}output/${options.entity}`;
	const scriptPath = `${projectRoot}agents/${step.script}`;

	const args = [
		"run",
		scriptPath,
		"--entity",
		options.entity,
		"--output",
		outputPath,
	];

	if (options.formName) {
		args.push("--form-name", options.formName);
	}

	if (step.extraArgs) {
		args.push(...step.extraArgs);
	}

	if (step.interactive) {
		args.push("--interactive");
	}

	const child = spawn(["bun", ...args], {
		stdin: "inherit",
		stdout: "inherit",
		stderr: "inherit",
		env: {
			...process.env,
			CLAUDE_PROJECT_DIR: projectRoot,
		},
	});

	await child.exited;
	const exitCode = child.exitCode ?? 0;

	if (exitCode !== 0) {
		console.error(`\n❌ Step ${stepNumber} failed with exit code ${exitCode}`);
		return exitCode;
	}

	console.log(`\n✅ Step ${stepNumber} completed successfully`);
	console.log(`   Output: ${outputPath}/${step.outputFile}\n`);

	return 0;
}

async function main() {
	const options = await parseOptions();
	const outputPath = options.outputDir || `${projectRoot}output/${options.entity}`;
	const agentSteps = getAgentSteps(options.isSingleForm || false, options.formName);
	const totalSteps = agentSteps.length;

	const childFormsCount = options.childForms?.length || 0;
	let childFormsList = "";
	if (childFormsCount > 0) {
		const childFormsStr = options.childForms!.join(", ");
		if (childFormsStr.length <= 65) {
			childFormsList = `║  Child Forms: ${childFormsStr.padEnd(62, " ")}║\n`;
		} else {
			childFormsList = `║  Child Forms: ${childFormsCount} form(s) detected${" ".padEnd(42, " ")}║\n`;
		}
	}

	console.log(`
╔════════════════════════════════════════════════════════════════════════════╗
║                  CLAUDE ONSHORE CONVERSION ORCHESTRATOR                    ║
║                                                                            ║
║  Entity: ${options.entity.padEnd(68, " ")}║
${options.formName ? `║  Form Name: ${options.formName.padEnd(65, " ")}║\n` : ""}${options.isSingleForm ? `║  Form Type: Single Form (non-Search/Detail)${" ".padEnd(30, " ")}║\n` : ""}${childFormsList}║  Output: ${outputPath.padEnd(67, " ")}║
╚════════════════════════════════════════════════════════════════════════════╝
	`);

	if (childFormsCount > 0) {
		console.log("Child forms detected:");
		options.childForms!.forEach(child => console.log(`  • ${child}`));
		console.log();
	}

	console.log(`This orchestrator will run ${totalSteps} analysis agents in sequence:`);
	console.log(`\nSteps 1-${totalSteps}: Automatic analysis and data extraction`);
	console.log(`\nNote: Step ${totalSteps + 1} (Template Generation) should be run separately:`);
	console.log(`   bun run generate-template --entity "${options.entity}"`);
	console.log("   or: bun run agents/conversion-template-generator.ts --entity \"" + options.entity + "\"\n");

	// Ensure output directory exists
	try {
		const normalizedOutputPath = normalizePath(outputPath);
		if (!existsSync(normalizedOutputPath)) {
			await mkdir(normalizedOutputPath, { recursive: true });
		}
		await Bun.write(`${outputPath}/.gitkeep`, "");

		// Write child forms list to output directory
		if (options.childForms && options.childForms.length > 0) {
			const childFormsData = {
				mainForm: options.formName || `frm${options.entity}Search`,
				entity: options.entity,
				childForms: options.childForms,
				detectedAt: new Date().toISOString(),
			};
			await Bun.write(
				`${outputPath}/child-forms.json`,
				JSON.stringify(childFormsData, null, 2)
			);
			console.log(`✓ Child forms list written to: ${outputPath}/child-forms.json\n`);
		}
	} catch (error: any) {
		console.error(`Error creating output directory: ${error.message}`);
		console.error(`Path: ${outputPath}`);
		console.error(`Normalized: ${normalizePath(outputPath)}`);
		throw error;
	}

	let currentStep = 1;
	for (const step of agentSteps) {
		if (options.skipSteps?.includes(currentStep)) {
			console.log(`\n⏭️  Skipping Step ${currentStep}: ${step.name}`);
			currentStep++;
			continue;
		}

		const exitCode = await runAgentStep(step, currentStep, options, totalSteps);
		if (exitCode !== 0) {
			console.error(`\n💥 Orchestrator stopped at step ${currentStep} due to error`);
			process.exit(exitCode);
		}

		currentStep++;
	}

	let childFormsMessage = "";
	if (childFormsCount > 0) {
		childFormsMessage = `║                                                                            ║
║  ${childFormsCount} child form(s) detected - see child-forms.json for details${" ".padEnd(21, " ")}║
`;
	}

	console.log(`
╔════════════════════════════════════════════════════════════════════════════╗
║                    ANALYSIS COMPLETE ✅                                     ║
║                                                                            ║
║  All ${totalSteps} analysis steps completed successfully for ${options.entity.padEnd(28 - totalSteps.toString().length, " ")}║
║                                                                            ║
║  Output directory: ${outputPath.padEnd(55, " ")}║
${childFormsMessage}║                                                                            ║
║  NEXT STEPS:                                                               ║
║                                                                            ║
║  1. Generate conversion templates (interactive):                           ║
║     bun run generate-template --entity "${options.entity}"${" ".padEnd(Math.max(0, 33 - options.entity.length), " ")}║
║                                                                            ║
║  2. Use interactive agents for implementation help:                        ║
║     bun run plan-conversion --entity "${options.entity}"${" ".padEnd(Math.max(0, 32 - options.entity.length), " ")}║
║     bun run entity-convert --entity "${options.entity}"${" ".padEnd(Math.max(0, 33 - options.entity.length), " ")}║
║     bun run viewmodel-create --entity "${options.entity}"${" ".padEnd(Math.max(0, 31 - options.entity.length), " ")}║
║                                                                            ║
║  See QUICK_START.md for detailed next steps and examples                  ║
╚════════════════════════════════════════════════════════════════════════════╝
	`);

	process.exit(0);
}

await main();
