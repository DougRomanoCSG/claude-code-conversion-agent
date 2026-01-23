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
 *   bun run agents/orchestrator.ts --entity "Facility" --generate-templates
 *   bun run agents/orchestrator.ts --entity "Facility" --generate-templates-api
 *   bun run agents/orchestrator.ts --entity "Facility" --generate-templates-ui
 *
 * Resume/Rerun Options:
 *   --resume              Resume from where conversion left off (runs pending and failed steps)
 *   --rerun-failed        Rerun only the steps that previously failed
 *
 * Note: This runs analysis steps only. Template generation can be run separately:
 *       bun run generate-template-api --entity "Facility"
 *       bun run generate-template-ui --entity "Facility"
 *       bun run generate-templates --entity "Facility"
 */

import { spawn } from "bun";
import { parsedArgs } from "../lib/flags";
import { getProjectRoot, parseEntityFromFormName, getAvailableForms, getFormsDirectory, detectChildForms } from "../lib/paths";
import { mkdir } from "fs/promises";
import { existsSync, readFileSync } from "fs";
import { createInterface } from "readline";
import { generateSpec } from "../lib/spec-generator";

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
	resume?: boolean;
	rerunFailed?: boolean;
	generateTemplates?: boolean;
	generateTemplatesApi?: boolean;
	generateTemplatesUi?: boolean;
	createSpec?: boolean;
}

interface AgentStep {
	name: string;
	script: string;
	description: string;
	outputFile: string;
	interactive: boolean;
	extraArgs?: string[];
	outputDir?: string;
}

interface StepStatus {
	stepNumber: number;
	name: string;
	script: string;
	description: string;
	status: "pending" | "running" | "completed" | "failed" | "skipped";
	startTime?: string;
	endTime?: string;
	durationMs?: number;
	exitCode?: number;
	error?: string;
	outputFile?: string;
	outputDir?: string;
}

interface ConversionStatus {
	entity: string;
	formName?: string;
	overallStatus: "running" | "completed" | "failed";
	startTime: string;
	endTime?: string;
	durationMs?: number;
	totalSteps: number;
	completedSteps: number;
	failedSteps: number;
	skippedSteps: number;
	steps: StepStatus[];
	childForms?: string[];
	failedStepNumbers?: number[]; // Summary of failed step numbers for easy reference
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
	const rawEntity = parsedArgs.values.entity;
	const rawFormName = parsedArgs.values["form-name"];
	const outputDir = parsedArgs.values.output as string;
	const skipStepsStr = parsedArgs.values["skip-steps"] as string;
	const skipSteps = skipStepsStr ? skipStepsStr.split(",").map(Number) : [];
	const resume = parsedArgs.values.resume as boolean;
	const rerunFailed = parsedArgs.values["rerun-failed"] as boolean;
	const generateTemplates = parsedArgs.values["generate-templates"] as boolean;
	const generateTemplatesApi = parsedArgs.values["generate-templates-api"] as boolean;
	const generateTemplatesUi = parsedArgs.values["generate-templates-ui"] as boolean;
	const createSpec = parsedArgs.values["create-spec"] as boolean;

	// Node's parseArgs can return string[] if an option is repeated; normalize to a single string.
	const entity = Array.isArray(rawEntity) ? rawEntity[0] : rawEntity;
	const formName = Array.isArray(rawFormName) ? rawFormName[0] : rawFormName;

	let finalEntity = typeof entity === "string" ? entity : undefined;
	let finalFormName = typeof formName === "string" ? formName : undefined;
	let isSingleForm = false;

	// If form name is provided, try to extract entity from it
	if (finalFormName && !finalEntity) {
		const extractedEntity = parseEntityFromFormName(finalFormName);
		if (extractedEntity) {
			finalEntity = extractedEntity;
		} else {
			// Try to extract entity by removing "frm" prefix for non-standard forms
			if (finalFormName.toLowerCase().startsWith("frm")) {
				finalEntity = finalFormName.substring(3);
				isSingleForm = true;
				console.log(`Detected single form (non-Search/Detail): ${finalFormName}`);
				console.log(`Extracted entity: ${finalEntity}`);
			} else {
				console.error(`Error: Could not parse entity name from form name "${finalFormName}"`);
				console.error("Expected format: frm{Entity}Search, frm{Entity}Detail, or frm{Entity}");
				process.exit(1);
			}
		}
	}

	// If form name is provided and is not a Search/Detail pattern, treat as single form
	if (finalFormName) {
		const isSearchOrDetail = /^(frm)?\w+(Search|Detail)$/i.test(finalFormName);
		if (!isSearchOrDetail) {
			isSingleForm = true;
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

	return {
		entity: finalEntity,
		formName: finalFormName,
		outputDir,
		skipSteps,
		isSingleForm,
		childForms,
		resume,
		rerunFailed,
		generateTemplates,
		generateTemplatesApi,
		generateTemplatesUi,
		createSpec,
	};
}

function getFailedSteps(status: ConversionStatus): number[] {
	return status.steps
		.filter(step => step.status === "failed")
		.map(step => step.stepNumber)
		.sort((a, b) => a - b);
}

function getPendingSteps(status: ConversionStatus, totalSteps: number): number[] {
	const completedSteps = new Set(status.steps
		.filter(step => step.status === "completed" || step.status === "skipped")
		.map(step => step.stepNumber));
	
	const pending: number[] = [];
	for (let i = 1; i <= totalSteps; i++) {
		if (!completedSteps.has(i)) {
			pending.push(i);
		}
	}
	return pending;
}

async function readConversionStatus(outputPath: string): Promise<ConversionStatus | null> {
	const statusPath = `${outputPath}/conversion-status.json`;
	try {
		if (existsSync(statusPath)) {
			const content = await Bun.file(statusPath).text();
			return JSON.parse(content) as ConversionStatus;
		}
	} catch (error) {
		console.warn(`Warning: Could not read conversion-status.json: ${error}`);
	}
	return null;
}

async function writeConversionStatus(outputPath: string, status: ConversionStatus): Promise<void> {
	const statusPath = `${outputPath}/conversion-status.json`;
	try {
		await Bun.write(statusPath, JSON.stringify(status, null, 2));
	} catch (error) {
		console.error(`Error writing conversion-status.json: ${error}`);
	}
}

async function initializeConversionStatus(
	outputPath: string,
	entity: string,
	formName: string | undefined,
	totalSteps: number,
	childForms: string[] | undefined,
): Promise<ConversionStatus> {
	const startTime = new Date().toISOString();
	const status: ConversionStatus = {
		entity,
		formName,
		overallStatus: "running",
		startTime,
		totalSteps,
		completedSteps: 0,
		failedSteps: 0,
		skippedSteps: 0,
		steps: [],
		childForms,
	};

	await writeConversionStatus(outputPath, status);
	return status;
}

function appendFormToFileName(fileName: string, formLabel?: string): string {
	if (!formLabel) {
		return fileName;
	}
	if (!fileName.endsWith(".json")) {
		return `${fileName}.${formLabel}`;
	}
	const baseName = fileName.slice(0, -".json".length);
	return `${baseName}.${formLabel}.json`;
}

function getAgentSteps(options: OrchestratorOptions): AgentStep[] {
	const steps: AgentStep[] = [];
	const searchFormName = `frm${options.entity}Search`;
	const detailFormName = `frm${options.entity}Detail`;
	const primaryFormName = options.formName || searchFormName;

	if (options.isSingleForm && options.formName) {
		// For single forms, run the analyzer once with the specific form name
		steps.push({
			name: `Form Structure Analyzer (${options.formName})`,
			script: "form-structure-analyzer.ts",
			description: `Extract ${options.formName} UI components`,
			outputFile: appendFormToFileName("form-structure.json", options.formName),
			interactive: false,
			extraArgs: ["--form-name", options.formName],
			outputDir: options.formName,
		});
	} else {
		// For standard Search/Detail pairs
		steps.push(
			{
				name: "Form Structure Analyzer (Search)",
				script: "form-structure-analyzer.ts",
				description: "Extract search form UI components",
				outputFile: appendFormToFileName("form-structure-search.json", searchFormName),
				interactive: false,
				extraArgs: ["--form-type", "Search"],
				outputDir: searchFormName,
			},
			{
				name: "Form Structure Analyzer (Detail)",
				script: "form-structure-analyzer.ts",
				description: "Extract detail form UI components",
				outputFile: appendFormToFileName("form-structure-detail.json", detailFormName),
				interactive: false,
				extraArgs: ["--form-type", "Detail"],
				outputDir: detailFormName,
			}
		);
	}

	// Common steps for all forms
	steps.push(
		{
			name: "Business Logic Extractor",
			script: "business-logic-extractor.ts",
			description: "Extract business rules and validation",
			outputFile: appendFormToFileName("business-logic.json", primaryFormName),
			interactive: false,
			outputDir: primaryFormName,
		},
		{
			name: "Data Access Pattern Analyzer",
			script: "data-access-analyzer.ts",
			description: "Extract stored procedures and queries",
			outputFile: appendFormToFileName("data-access.json", primaryFormName),
			interactive: false,
			outputDir: primaryFormName,
		},
		{
			name: "Security & Authorization Extractor",
			script: "security-extractor.ts",
			description: "Extract permissions and authorization",
			outputFile: appendFormToFileName("security.json", primaryFormName),
			interactive: false,
			outputDir: primaryFormName,
		},
		{
			name: "UI Component Mapper",
			script: "ui-component-mapper.ts",
			description: "Map legacy controls to modern equivalents",
			outputFile: appendFormToFileName("ui-mapping.json", primaryFormName),
			interactive: false,
			outputDir: primaryFormName,
		},
		{
			name: "Form Workflow Analyzer",
			script: "form-workflow-analyzer.ts",
			description: "Extract user flows and state management",
			outputFile: appendFormToFileName("workflow.json", primaryFormName),
			interactive: false,
			outputDir: primaryFormName,
		},
		{
			name: "Detail Form Tab Analyzer",
			script: "detail-tab-analyzer.ts",
			description: "Extract tab structure and related entities",
			outputFile: appendFormToFileName("tabs.json", options.isSingleForm ? primaryFormName : detailFormName),
			interactive: false,
			outputDir: options.isSingleForm ? primaryFormName : detailFormName,
		},
		{
			name: "Validation Rule Extractor",
			script: "validation-extractor.ts",
			description: "Extract all validation logic",
			outputFile: appendFormToFileName("validation.json", primaryFormName),
			interactive: false,
			outputDir: primaryFormName,
		},
		{
			name: "Related Entity Analyzer",
			script: "related-entity-analyzer.ts",
			description: "Extract entity relationships",
			outputFile: appendFormToFileName("related-entities.json", primaryFormName),
			interactive: false,
			outputDir: primaryFormName,
		}
	);

	return steps;
}

async function runAgentStep(
	step: AgentStep,
	stepNumber: number,
	options: OrchestratorOptions,
	totalSteps: number,
	status: ConversionStatus,
	outputRoot: string,
): Promise<number> {
	const startTime = new Date().toISOString();
	const stepStartTime = Date.now();

	console.log(`\n${"=".repeat(80)}`);
	console.log(`STEP ${stepNumber}/${totalSteps}: ${step.name}`);
	console.log(`Description: ${step.description}`);
	console.log(`Started at: ${startTime}`);
	console.log(`${"=".repeat(80)}\n`);

	const outputPath = step.outputDir ? `${outputRoot}/${step.outputDir}` : outputRoot;
	const scriptPath = `${projectRoot}agents/${step.script}`;
	const normalizedOutputPath = normalizePath(outputPath);

	if (!existsSync(normalizedOutputPath)) {
		await mkdir(normalizedOutputPath, { recursive: true });
	}
	await Bun.write(`${outputPath}/.gitkeep`, "");

	// Update status to running
	const stepStatus: StepStatus = {
		stepNumber,
		name: step.name,
		script: step.script,
		description: step.description,
		status: "running",
		startTime,
		outputFile: step.outputFile,
		outputDir: step.outputDir,
	};

	// Find existing step or add new one
	const existingStepIndex = status.steps.findIndex(s => s.stepNumber === stepNumber);
	if (existingStepIndex >= 0) {
		status.steps[existingStepIndex] = stepStatus;
	} else {
		status.steps.push(stepStatus);
	}
	await writeConversionStatus(outputRoot, status);

	const args = [
		"run",
		scriptPath,
		"--entity",
		options.entity,
		"--output",
		outputPath,
	];

	if (options.formName && options.isSingleForm) {
		args.push("--form-name", options.formName);
	}

	if (step.extraArgs) {
		args.push(...step.extraArgs);
	}

	if (step.outputFile) {
		args.push("--output-file", step.outputFile);
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
	const endTime = new Date().toISOString();
	const durationMs = Date.now() - stepStartTime;

	// Update status with result
	stepStatus.status = exitCode === 0 ? "completed" : "failed";
	stepStatus.endTime = endTime;
	stepStatus.durationMs = durationMs;
	stepStatus.exitCode = exitCode;

	if (exitCode !== 0) {
		stepStatus.error = `Step failed with exit code ${exitCode}`;
		status.failedSteps++;
		console.error(`\n❌ Step ${stepNumber} failed with exit code ${exitCode}`);
		console.error(`   Duration: ${(durationMs / 1000).toFixed(2)}s`);
	} else {
		status.completedSteps++;
		console.log(`\n✅ Step ${stepNumber} completed successfully`);
		console.log(`   Duration: ${(durationMs / 1000).toFixed(2)}s`);
		console.log(`   Output: ${outputPath}/${step.outputFile}\n`);
	}

	// Update the step in status array (recalculate index to ensure we update the correct entry)
	const finalStepIndex = status.steps.findIndex(s => s.stepNumber === stepNumber);
	if (finalStepIndex >= 0) {
		status.steps[finalStepIndex] = stepStatus;
	} else {
		status.steps.push(stepStatus);
	}
	await writeConversionStatus(outputRoot, status);

	return exitCode;
}

async function runTemplateGenerator(script: string, options: OrchestratorOptions, outputPath: string): Promise<number> {
	const args = [
		"run",
		`${projectRoot}agents/${script}`,
		"--entity",
		options.entity,
		"--output",
		outputPath,
	];

	if (options.formName) {
		args.push("--form-name", options.formName);
	}

	console.log(`\n▶️  Running template generator: bun ${args.join(" ")}`);
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
	return child.exitCode ?? 0;
}

async function main() {
	const options = await parseOptions();
	const outputRoot = options.outputDir || `${projectRoot}output/${options.entity}`;

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
${options.formName ? `║  Form Name: ${options.formName.padEnd(65, " ")}║\n` : ""}${options.isSingleForm ? `║  Form Type: Single Form (non-Search/Detail)${" ".padEnd(30, " ")}║\n` : ""}${childFormsList}║  Output: ${outputRoot.padEnd(67, " ")}║
╚════════════════════════════════════════════════════════════════════════════╝
	`);

	if (childFormsCount > 0) {
		console.log("Child forms detected:");
		options.childForms!.forEach(child => console.log(`  • ${child}`));
		console.log();
	}

	// Ensure output directory exists
	try {
		const normalizedOutputPath = normalizePath(outputRoot);
		if (!existsSync(normalizedOutputPath)) {
			await mkdir(normalizedOutputPath, { recursive: true });
		}
		await Bun.write(`${outputRoot}/.gitkeep`, "");

		// Write child forms list to output directory
		if (options.childForms && options.childForms.length > 0) {
			const childFormsData = {
				mainForm: options.formName || `frm${options.entity}Search`,
				entity: options.entity,
				childForms: options.childForms,
				detectedAt: new Date().toISOString(),
			};
			await Bun.write(
				`${outputRoot}/child-forms.json`,
				JSON.stringify(childFormsData, null, 2)
			);
			console.log(`✓ Child forms list written to: ${outputRoot}/child-forms.json\n`);
		}
	} catch (error: any) {
		console.error(`Error creating output directory: ${error.message}`);
		console.error(`Path: ${outputRoot}`);
		console.error(`Normalized: ${normalizePath(outputRoot)}`);
		throw error;
	}

	// Handle resume/rerun-failed scenarios
	let conversionStatus: ConversionStatus;
	let stepsToRun: Set<number> | null = null;
	let mainStartTime: number;
	let agentSteps = getAgentSteps(options);
	let totalSteps = agentSteps.length;

	if (options.resume || options.rerunFailed) {
		// Try to load existing status
		const existingStatus = await readConversionStatus(outputRoot);
		
		if (!existingStatus) {
			console.error(`\n❌ Error: No existing conversion-status.json found at ${outputRoot}/conversion-status.json`);
			console.error("Cannot resume or rerun failed steps without existing status.");
			console.error("Run the orchestrator normally first to create the status file.\n");
			process.exit(1);
		}

		// Validate entity matches
		if (existingStatus.entity !== options.entity) {
			console.error(`\n❌ Error: Entity mismatch. Status file is for "${existingStatus.entity}", but you specified "${options.entity}"`);
			process.exit(1);
		}

		if (!options.formName && existingStatus.formName) {
			options.formName = existingStatus.formName;
			const isSearchOrDetail = /^(frm)?\w+(Search|Detail)$/i.test(options.formName);
			options.isSingleForm = !isSearchOrDetail;
			agentSteps = getAgentSteps(options);
			totalSteps = agentSteps.length;
		}

		conversionStatus = existingStatus;
		conversionStatus.overallStatus = "running";
		conversionStatus.endTime = undefined;
		conversionStatus.durationMs = undefined;

		if (options.rerunFailed) {
			// Rerun only failed steps
			const failedSteps = getFailedSteps(conversionStatus);
			if (failedSteps.length === 0) {
				console.log("\n✅ No failed steps found. All steps completed successfully.");
				process.exit(0);
			}
			stepsToRun = new Set(failedSteps);
			console.log(`\n🔄 Rerunning ${failedSteps.length} failed step(s): ${failedSteps.join(", ")}`);
			console.log(`   Failed steps: ${failedSteps.map(s => {
				const step = conversionStatus.steps.find(st => st.stepNumber === s);
				return `${s} (${step?.name || "unknown"})`;
			}).join(", ")}\n`);
		} else {
			// Resume from where we left off (run pending and failed steps)
			const pendingSteps = getPendingSteps(conversionStatus, totalSteps);
			const failedSteps = getFailedSteps(conversionStatus);
			stepsToRun = new Set([...pendingSteps, ...failedSteps]);
			
			if (stepsToRun.size === 0) {
				console.log("\n✅ All steps already completed. Nothing to resume.");
				process.exit(0);
			}
			
			console.log(`\n🔄 Resuming conversion. Will run ${stepsToRun.size} step(s): ${Array.from(stepsToRun).sort((a, b) => a - b).join(", ")}`);
			if (failedSteps.length > 0) {
				console.log(`   Previously failed: ${failedSteps.map(s => {
					const step = conversionStatus.steps.find(st => st.stepNumber === s);
					return `${s} (${step?.name || "unknown"})`;
				}).join(", ")}`);
			}
			console.log();
		}

		// Reset failed step counts for rerun
		if (options.rerunFailed) {
			conversionStatus.failedSteps = 0;
			// Remove failed steps from status so they can be rerun
			conversionStatus.steps = conversionStatus.steps.filter(s => s.status !== "failed");
		}

		mainStartTime = Date.now();
		await writeConversionStatus(outputRoot, conversionStatus);
	} else {
		// Initialize new conversion status tracking
		conversionStatus = await initializeConversionStatus(
			outputRoot,
			options.entity,
			options.formName,
			totalSteps,
			options.childForms,
		);
		console.log(`✓ Conversion status tracking initialized: ${outputRoot}/conversion-status.json\n`);
		mainStartTime = Date.now();
	}

	// Calculate which steps will actually run
	const stepsToExecute = totalSteps - (options.skipSteps?.length || 0);
	const skipStepsList = options.skipSteps && options.skipSteps.length > 0 
		? options.skipSteps.sort((a, b) => a - b).join(", ")
		: "none";
	
	console.log(`This orchestrator will run ${stepsToExecute} of ${totalSteps} analysis agents:`);
	if (options.skipSteps && options.skipSteps.length > 0) {
		console.log(`   Skipping steps: ${skipStepsList}`);
	}
	console.log(`\nSteps 1-${totalSteps}: Automatic analysis and data extraction`);
	console.log(`\nNote: Template generation can be run separately:`);
	console.log(`   bun run generate-template-api --entity "${options.entity}"`);
	console.log(`   bun run generate-template-ui --entity "${options.entity}"`);
	console.log(`   bun run generate-templates --entity "${options.entity}"`);
	console.log("   or: bun run agents/conversion-template-generator.ts --entity \"" + options.entity + "\"\n");

	let currentStep = 1;
	for (const step of agentSteps) {
		// Skip if not in stepsToRun (for resume/rerun scenarios)
		if (stepsToRun && !stepsToRun.has(currentStep)) {
			currentStep++;
			continue;
		}

		if (options.skipSteps?.includes(currentStep)) {
			const skipTime = new Date().toISOString();
			console.log(`\n⏭️  Skipping Step ${currentStep}: ${step.name}`);
			
			// Record skipped step in status (check if it already exists)
			const existingSkipIndex = conversionStatus.steps.findIndex(s => s.stepNumber === currentStep);
			const skippedStepStatus: StepStatus = {
				stepNumber: currentStep,
				name: step.name,
				script: step.script,
				description: step.description,
				status: "skipped",
				startTime: skipTime,
				endTime: skipTime,
				durationMs: 0,
				outputFile: step.outputFile,
				outputDir: step.outputDir,
			};
			
			if (existingSkipIndex >= 0) {
				conversionStatus.steps[existingSkipIndex] = skippedStepStatus;
			} else {
				conversionStatus.steps.push(skippedStepStatus);
				conversionStatus.skippedSteps++;
			}
			await writeConversionStatus(outputRoot, conversionStatus);
			
			currentStep++;
			continue;
		}

		const exitCode = await runAgentStep(step, currentStep, options, totalSteps, conversionStatus, outputRoot);
		if (exitCode !== 0) {
			const endTime = new Date().toISOString();
			const durationMs = Date.now() - mainStartTime;
			const failedSteps = getFailedSteps(conversionStatus);
			conversionStatus.overallStatus = "failed";
			conversionStatus.endTime = endTime;
			conversionStatus.durationMs = durationMs;
			conversionStatus.failedStepNumbers = failedSteps;
			await writeConversionStatus(outputRoot, conversionStatus);
			
			console.error(`\n💥 Orchestrator stopped at step ${currentStep} due to error`);
			console.error(`Total duration: ${(durationMs / 1000).toFixed(2)}s`);
			console.error(`\nFailed steps: ${failedSteps.join(", ")}`);
			console.error(`\nTo rerun failed steps:`);
			console.error(`   bun run agents/orchestrator.ts --entity "${options.entity}" --rerun-failed`);
			console.error(`\nStatus saved to: ${outputRoot}/conversion-status.json\n`);
			process.exit(exitCode);
		}

		currentStep++;
	}

	// Optional template generation (API/UI split)
	const runApiTemplates = options.generateTemplates || options.generateTemplatesApi;
	const runUiTemplates = options.generateTemplates || options.generateTemplatesUi;
	if (runApiTemplates || runUiTemplates) {
		console.log("\nTemplate generation requested. Launching interactive template generators...");

		if (runApiTemplates) {
			const apiExit = await runTemplateGenerator("conversion-template-generator-api.ts", options, outputRoot);
			if (apiExit !== 0) {
				console.error("\n❌ API template generation failed. Aborting.");
				process.exit(apiExit);
			}
		}

		if (runUiTemplates) {
			const uiExit = await runTemplateGenerator("conversion-template-generator-ui.ts", options, outputRoot);
			if (uiExit !== 0) {
				console.error("\n❌ UI template generation failed. Aborting.");
				process.exit(uiExit);
			}
		}
	}

	// Optional spec generation
	if (options.createSpec) {
		console.log("\n📝 SpecKit spec generation requested. Generating specification...");

		try {
			// Read config to get monorepo path
			const configPath = `${projectRoot}config.json`;
			const config = JSON.parse(readFileSync(configPath, "utf-8"));
			const monorepoPath = config.targetProjects?.monorepo || "C:\\Dev\\BargeOps.Admin.Mono";

			// Load analysis data
			const analysisData: any = {
				entity: options.entity,
			};

			// Construct form names dynamically
			const searchFormName = `frm${options.entity}Search`;
			const detailFormName = `frm${options.entity}Detail`;
			const primaryFormName = options.formName || searchFormName;

			// Load business logic
			const businessLogicPath = options.isSingleForm
				? `${outputRoot}/${options.formName}/business-logic.${options.formName}.json`
				: `${outputRoot}/${searchFormName}/business-logic.${searchFormName}.json`;
			if (existsSync(businessLogicPath)) {
				analysisData.businessLogic = JSON.parse(readFileSync(businessLogicPath, "utf-8"));
			}

			// Load tabs (detail form)
			const tabsPath = options.isSingleForm
				? `${outputRoot}/${options.formName}/tabs.${options.formName}.json`
				: `${outputRoot}/${detailFormName}/tabs.${detailFormName}.json`;
			if (existsSync(tabsPath)) {
				analysisData.tabs = JSON.parse(readFileSync(tabsPath, "utf-8"));
			}

			// Load validation
			const validationPath = options.isSingleForm
				? `${outputRoot}/${options.formName}/validation.${options.formName}.json`
				: `${outputRoot}/${searchFormName}/validation.${searchFormName}.json`;
			if (existsSync(validationPath)) {
				analysisData.validation = JSON.parse(readFileSync(validationPath, "utf-8"));
			}

			// Load security
			const securityPath = options.isSingleForm
				? `${outputRoot}/${options.formName}/security.${options.formName}.json`
				: `${outputRoot}/${searchFormName}/security.${searchFormName}.json`;
			if (existsSync(securityPath)) {
				analysisData.security = JSON.parse(readFileSync(securityPath, "utf-8"));
			}

			// Load data access
			const dataAccessPath = options.isSingleForm
				? `${outputRoot}/${options.formName}/data-access.${options.formName}.json`
				: `${outputRoot}/${searchFormName}/data-access.${searchFormName}.json`;
			if (existsSync(dataAccessPath)) {
				analysisData.dataAccess = JSON.parse(readFileSync(dataAccessPath, "utf-8"));
			}

			// Load form structure (search)
			const formStructureSearchPath = options.isSingleForm
				? `${outputRoot}/${options.formName}/form-structure.${options.formName}.json`
				: `${outputRoot}/${searchFormName}/form-structure-search.${searchFormName}.json`;
			if (existsSync(formStructureSearchPath)) {
				analysisData.formStructureSearch = JSON.parse(readFileSync(formStructureSearchPath, "utf-8"));
			}

			await generateSpec(analysisData, {
				outputPath: outputRoot, // Pass the analysis output directory so master plan can be found
				monorepoPath,
			});

			console.log(`\n✅ SpecKit spec generated successfully!`);
			console.log(`   Location: ${monorepoPath}/.speckit/entities/${options.entity}/`);
			console.log(`\nNext steps:`);
			console.log(`   1. Review spec: ${monorepoPath}/.speckit/entities/${options.entity}/spec.md`);
			console.log(`   2. Implement against tasks in: ${monorepoPath}/.speckit/entities/${options.entity}/tasks/`);
			console.log(`   3. Track quality with: ${monorepoPath}/.speckit/entities/${options.entity}/quality-checklist.md\n`);
		} catch (error: any) {
			console.error(`\n❌ SpecKit spec generation failed: ${error.message}`);
			console.error(`   Make sure analysis data exists in ${outputRoot}`);
		}
	}

	// Check for failed steps before finalizing
	const failedSteps = getFailedSteps(conversionStatus);
	const endTime = new Date().toISOString();
	const durationMs = Date.now() - mainStartTime;
	
	// Only mark as completed if there are no failed steps
	if (failedSteps.length === 0) {
		conversionStatus.overallStatus = "completed";
	} else {
		conversionStatus.overallStatus = "failed";
		conversionStatus.failedStepNumbers = failedSteps;
	}
	
	conversionStatus.endTime = endTime;
	conversionStatus.durationMs = durationMs;
	await writeConversionStatus(outputRoot, conversionStatus);

	let childFormsMessage = "";
	if (childFormsCount > 0) {
		childFormsMessage = `║                                                                            ║
║  ${childFormsCount} child form(s) detected - see child-forms.json for details${" ".padEnd(21, " ")}║
`;
	}

	const totalDuration = conversionStatus.durationMs ? (conversionStatus.durationMs / 1000).toFixed(2) : "0.00";
	const durationDisplay = `Total duration: ${totalDuration}s`;
	
	let failedStepsMessage = "";
	if (failedSteps.length > 0) {
		const failedStepsList = failedSteps.map(s => {
			const step = conversionStatus.steps.find(st => st.stepNumber === s);
			return `Step ${s}: ${step?.name || "unknown"}`;
		}).join("\n     ");
		
		failedStepsMessage = `║                                                                            ║
║  ⚠️  ${failedSteps.length} step(s) failed:${" ".padEnd(60, " ")}║
║     ${failedStepsList.padEnd(75, " ")}║
║                                                                            ║
║  To rerun failed steps:                                                    ║
║     bun run agents/orchestrator.ts --entity "${options.entity}" --rerun-failed${" ".padEnd(Math.max(0, 25 - options.entity.length), " ")}║
║                                                                            ║
`;
	}
	
	const statusIcon = failedSteps.length === 0 ? "✅" : "⚠️";
	const statusText = failedSteps.length === 0 ? "ANALYSIS COMPLETE" : "ANALYSIS COMPLETE (WITH FAILURES)";
	const completionText = failedSteps.length === 0 
		? `All ${totalSteps} analysis steps completed successfully for ${options.entity}`
		: `${conversionStatus.completedSteps} of ${totalSteps} steps completed for ${options.entity}`;
	
	console.log(`
╔════════════════════════════════════════════════════════════════════════════╗
║                    ${statusText} ${statusIcon}${" ".padEnd(78 - statusText.length - 2, " ")}║
║                                                                            ║
║  ${completionText.padEnd(78, " ")}║
║  ${durationDisplay.padEnd(78, " ")}║
║                                                                            ║
║  Output directory: ${outputRoot.padEnd(55, " ")}║
║  Status file: ${`${outputRoot}/conversion-status.json`.padEnd(56, " ")}║
${childFormsMessage}${failedStepsMessage}║  NEXT STEPS:                                                               ║
║                                                                            ║
║  1. Generate conversion templates (interactive):                           ║
║     bun run generate-template-api --entity "${options.entity}"${" ".padEnd(Math.max(0, 27 - options.entity.length), " ")}║
║     bun run generate-template-ui --entity "${options.entity}"${" ".padEnd(Math.max(0, 28 - options.entity.length), " ")}║
║     bun run generate-templates --entity "${options.entity}"${" ".padEnd(Math.max(0, 30 - options.entity.length), " ")}║
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
