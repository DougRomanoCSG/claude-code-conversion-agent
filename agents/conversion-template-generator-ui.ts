#!/usr/bin/env -S bun run
/**
 * CONVERSION TEMPLATE GENERATOR (UI): Generate UI templates (ALWAYS INTERACTIVE)
 *
 * Usage:
 *   bun run agents/conversion-template-generator-ui.ts --entity "Facility"
 */

import { spawn } from "bun";
import { existsSync, readdirSync } from "fs";
import { dirname, join, extname, basename } from "path";
import { fileURLToPath } from "url";
import { buildClaudeFlags, parsedArgs } from "../lib/flags";
import type { ClaudeFlags } from "../lib/claude-flags.types";
import {
	getAdminUiPath,
	getCrewingUiPath,
	getDetailedReferenceExamples,
	getProjectRoot,
} from "../lib/paths";
import templateGenSettings from "../settings/template-generator.settings.json" with { type: "json" };
import uiSystemPromptBase from "../system-prompts/conversion-template-generator-ui-prompt.md" with { type: "text" };

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const projectRoot = getProjectRoot(import.meta.url);
const settingsJson = JSON.stringify(templateGenSettings);
const mcpConfigPath = join(__dirname, "..", "settings", "template-generator.mcp.json");

interface GeneratorOptions {
	entity: string;
	outputDir?: string;
	formName?: string;
}

function parseOptions(): GeneratorOptions {
	const entity = parsedArgs.values.entity as string;
	const outputDir = parsedArgs.values.output as string;
	const formName = parsedArgs.values["form-name"] as string;

	if (!entity) {
		console.error("Error: --entity parameter is required");
		process.exit(1);
	}

	return { entity, outputDir, formName };
}

function getMissingFiles(outputPath: string, files: string[]): string[] {
	return files.filter((f) => !existsSync(join(outputPath, f)));
}

function getSkipStepsFromExistingFiles(outputPath: string, filesInStepOrder: string[]): number[] {
	const skipSteps: number[] = [];
	for (let i = 0; i < filesInStepOrder.length; i++) {
		const file = filesInStepOrder[i];
		if (existsSync(join(outputPath, file))) {
			// Step numbers are 1-based; file order matches orchestrator steps 1-10
			skipSteps.push(i + 1);
		}
	}
	return skipSteps;
}

function getAnalysisFilesForMode(mode: "search-detail" | "single-form"): string[] {
	if (mode === "single-form") {
		return [
			"form-structure.json",
			"business-logic.json",
			"data-access.json",
			"security.json",
			"ui-mapping.json",
			"workflow.json",
			"tabs.json",
			"validation.json",
			"related-entities.json",
		];
	}

	return [
		"form-structure-search.json",
		"form-structure-detail.json",
		"business-logic.json",
		"data-access.json",
		"security.json",
		"ui-mapping.json",
		"workflow.json",
		"tabs.json",
		"validation.json",
		"related-entities.json",
	];
}

function detectAnalysisMode(outputPath: string, options: GeneratorOptions): "search-detail" | "single-form" {
	// If user explicitly provides a form name, treat non-Search/Detail as a single-form workflow.
	if (options.formName) {
		const isSearchOrDetail = /^(frm)?\w+(Search|Detail)$/i.test(options.formName);
		if (!isSearchOrDetail) return "single-form";
	}

	// Infer from output files already present.
	if (existsSync(join(outputPath, "form-structure.json"))) return "single-form";
	if (
		existsSync(join(outputPath, "form-structure-search.json")) ||
		existsSync(join(outputPath, "form-structure-detail.json"))
	) {
		return "search-detail";
	}

	// Default to search/detail pairs.
	return "search-detail";
}

async function ensureAnalysisOutputsExist(
	options: GeneratorOptions,
	outputPath: string,
	analysisFiles: string[],
	mode: "search-detail" | "single-form",
): Promise<void> {
	const missingBefore = getMissingFiles(outputPath, analysisFiles);
	if (missingBefore.length === 0) {
		return;
	}

	console.log(`
╔════════════════════════════════════════════════════════════════════════════╗
║                    TEMPLATE GENERATION PRE-FLIGHT CHECK                    ║
║                                                                            ║
║  Entity: ${options.entity.padEnd(68, " ")}║
║  Output: ${outputPath.padEnd(67, " ")}║
║                                                                            ║
║  Missing analysis files detected. Running orchestrator to (re)generate     ║
║  missing analysis outputs before starting interactive template generation. ║
╚════════════════════════════════════════════════════════════════════════════╝
`);

	console.log("Missing analysis files:");
	missingBefore.forEach((f) => console.log(`  - ${f}`));
	console.log();

	const existingSkipSteps = getSkipStepsFromExistingFiles(outputPath, analysisFiles);
	const orchestratorPath = `${projectRoot}agents/orchestrator.ts`;

	const args = ["run", orchestratorPath, "--entity", options.entity, "--output", outputPath];

	// For single-form entities, the orchestrator needs a form name to avoid assuming frm{Entity}Search/Detail.
	// Prefer user-provided form name, otherwise fall back to frm{Entity}.
	if (mode === "single-form") {
		const formName = options.formName || `frm${options.entity}`;
		args.push("--form-name", formName);
	}

	if (existingSkipSteps.length > 0) {
		args.push("--skip-steps", existingSkipSteps.sort((a, b) => a - b).join(","));
	}

	console.log(`Running: bun ${args.join(" ")}`);
	console.log();

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
		console.error(`\n❌ Orchestrator failed (exit ${exitCode}). Aborting template generation.\n`);
		process.exit(exitCode);
	}

	const missingAfter = getMissingFiles(outputPath, analysisFiles);
	if (missingAfter.length > 0) {
		console.error("\n❌ Analysis outputs are still missing after orchestrator run. Aborting template generation.");
		missingAfter.forEach((f) => console.error(`  - ${f}`));
		console.error(`\nTry running a full analysis:\n  bun run agents/orchestrator.ts --entity "${options.entity}"\n`);
		process.exit(1);
	}
}

/**
 * Find image files in the output directory and match them to forms/tabs
 */
async function findImageFiles(outputPath: string, entity: string, mode: "search-detail" | "single-form"): Promise<{
	images: string[];
	imageMapping: Record<string, string[]>;
	tabImageMapping: Record<string, string[]>;
}> {
	const images: string[] = [];
	const imageMapping: Record<string, string[]> = {};
	const tabImageMapping: Record<string, string[]> = {};
	
	if (!existsSync(outputPath)) {
		return { images, imageMapping, tabImageMapping };
	}

	const imageExtensions = [".png", ".jpg", ".jpeg", ".gif", ".bmp"];
	
	// Read tabs.json to get tab names for matching
	const tabsPath = join(outputPath, "tabs.json");
	let tabNames: string[] = [];
	if (existsSync(tabsPath)) {
		try {
			const tabsContent = await Bun.file(tabsPath).json();
			if (tabsContent.tabs && Array.isArray(tabsContent.tabs)) {
				tabNames = tabsContent.tabs.map((tab: any) => {
					// Get tab name from tabName or tabText
					const name = (tab.tabName || tab.tabText || "").toLowerCase();
					// Remove common prefixes/suffixes
					return name.replace(/^tab/, "").replace(/tab$/, "");
				}).filter((name: string) => name.length > 0);
			}
		} catch (error) {
			// Ignore errors reading tabs.json
		}
	}
	
	try {
		const files = readdirSync(outputPath);
		
		// Find all image files
		for (const file of files) {
			const ext = extname(file).toLowerCase();
			if (imageExtensions.includes(ext)) {
				const imagePath = join(outputPath, file);
				images.push(imagePath);
				
				// Try to match image name to form/tab names
				const baseName = basename(file, ext).toLowerCase();
				const baseNameClean = baseName.replace(/^frm/, "").replace(/^tab/, "");
				
				// Match to search form
				if (baseName.includes("search") || baseName.includes("list") || baseName.includes("index")) {
					if (!imageMapping["search"]) imageMapping["search"] = [];
					imageMapping["search"].push(imagePath);
				}
				// Match to detail form
				else if (baseName.includes("detail") || baseName.includes("edit") || baseName.includes("view")) {
					if (!imageMapping["detail"]) imageMapping["detail"] = [];
					imageMapping["detail"].push(imagePath);
				}
				// Try to match to tab names
				else {
					let matched = false;
					for (const tabName of tabNames) {
						if (baseNameClean.includes(tabName) || tabName.includes(baseNameClean)) {
							if (!tabImageMapping[tabName]) tabImageMapping[tabName] = [];
							tabImageMapping[tabName].push(imagePath);
							matched = true;
							break;
						}
					}
					
					if (!matched) {
						// If no specific match, add to general images
						if (!imageMapping["general"]) imageMapping["general"] = [];
						imageMapping["general"].push(imagePath);
					}
				}
			}
		}
	} catch (error) {
		console.warn(`Warning: Could not read image files from ${outputPath}: ${error}`);
	}
	
	return { images, imageMapping, tabImageMapping };
}

async function runTemplateGenerator(options: GeneratorOptions): Promise<number> {
	const outputPath = options.outputDir || `${projectRoot}output/${options.entity}`;

	const mode = detectAnalysisMode(outputPath, options);
	const analysisFiles = getAnalysisFilesForMode(mode);
	const optionalFiles = ["child-forms.json"];

	// Ensure analysis outputs (steps 1-10) exist before starting template generation.
	await ensureAnalysisOutputsExist(options, outputPath, analysisFiles, mode);

	// Find image files for look-and-feel reference
	const { images, imageMapping, tabImageMapping } = await findImageFiles(outputPath, options.entity, mode);

	// Build image references for the prompt
	let imageReferences = "";
	if (images.length > 0) {
		imageReferences = `\n\n🚨 CRITICAL: WINFORMS SCREEN IMAGES AVAILABLE\n`;
		imageReferences += `The following images show the original WinForms screens you are converting from.\n`;
		imageReferences += `You MUST examine these images to ensure complete field coverage and accurate look-and-feel.\n\n`;
		
		if (imageMapping["search"] && imageMapping["search"].length > 0) {
			imageReferences += `SEARCH SCREEN IMAGES:\n`;
			imageMapping["search"].forEach(img => {
				imageReferences += `- ${img}\n`;
			});
			imageReferences += `\n`;
		}
		
		if (imageMapping["detail"] && imageMapping["detail"].length > 0) {
			imageReferences += `DETAIL SCREEN IMAGES:\n`;
			imageMapping["detail"].forEach(img => {
				imageReferences += `- ${img}\n`;
			});
			imageReferences += `\n`;
		}
		
		if (Object.keys(tabImageMapping).length > 0) {
			imageReferences += `TAB-SPECIFIC IMAGES (matched to tab names):\n`;
			for (const [tabName, tabImages] of Object.entries(tabImageMapping)) {
				imageReferences += `  Tab: ${tabName}\n`;
				tabImages.forEach(img => {
					imageReferences += `    - ${img}\n`;
				});
			}
			imageReferences += `\n`;
		}
		
		if (imageMapping["general"] && imageMapping["general"].length > 0) {
			imageReferences += `ADDITIONAL SCREEN IMAGES:\n`;
			imageMapping["general"].forEach(img => {
				imageReferences += `- ${img}\n`;
			});
			imageReferences += `\n`;
		}
		
		imageReferences += `\nINSTRUCTIONS FOR USING IMAGES:\n`;
		imageReferences += `1. Read and examine ALL images to understand the complete UI structure\n`;
		imageReferences += `2. Verify that ALL fields visible in images are captured in your templates\n`;
		imageReferences += `3. Match image file names to tab names/form names for child screens\n`;
		imageReferences += `4. Document look-and-feel details: field grouping, label placement, spacing, visual hierarchy\n`;
		imageReferences += `5. Ensure button placement, icons, and actions match what's shown in images\n`;
		imageReferences += `6. Capture any child dialogs or popups visible in the images\n`;
		imageReferences += `7. Note any visual patterns, colors, or styling shown in the images\n\n`;
	}

	const systemPrompt = `
${uiSystemPromptBase}

TASK: Generate UI conversion templates for ${options.entity}.

INPUT DATA:
You have access to analysis files in: ${outputPath}/
${analysisFiles.map((f) => `- ${f}`).join("\n")}
${optionalFiles.map((f) => `- ${f} (optional)`).join("\n")}
${imageReferences}

CREWING UI REFERENCE:
- Crewing UI project: ${getCrewingUiPath()}

GENERATION GOALS:
1. Capture ALL detail-screen actions, buttons, dialogs, and child screens (CRITICAL).
2. Follow BargeOps Crewing UI patterns EXACTLY (AppController, CrewingBaseService, DataTables).
3. Document look-and-feel: layout groups, label placement, spacing, visual hierarchy.
4. Generate UI templates only (Controllers, ViewModels, Views, JS).
5. Include DataTables configuration following Crewing UI standards.
6. Output comprehensive plan in conversion-plan-ui.md with all detail screen documentation.

OUTPUT STRUCTURE:
Primary file: ${outputPath}/conversion-plan-ui.md

Templates:
${outputPath}/templates/
└── ui/
    ├── Controllers/
    │   └── {Entity}Controller.cs
    ├── Services/
    │   ├── I{Entity}Service.cs
    │   └── {Entity}Service.cs
    ├── ViewModels/
    │   ├── {Entity}SearchViewModel.cs
    │   ├── {Entity}EditViewModel.cs
    │   ├── {Entity}DetailsViewModel.cs
    │   └── {Entity}ListItemViewModel.cs
    ├── Views/
    │   └── {Entity}/
    │       ├── Index.cshtml
    │       ├── Edit.cshtml
    │       └── Details.cshtml
    └── wwwroot/
        └── js/
            ├── {entity}-search.js
            └── {entity}-detail.js

${getDetailedReferenceExamples()}

TARGET PROJECTS:
⭐ UI: ${getAdminUiPath()}

IMPORTANT:
- Do NOT generate Shared or API templates in this run.
- Include ALL detail screen buttons, dialogs, and child forms (CRITICAL).
- Follow BargeOps Crewing UI architecture patterns from UIStandards.md.
- Use DataTables pattern from DataTables.md.
- Follow button and form patterns from BargeOpsCrewUI.md.
- Document look-and-feel for all UI elements.

Begin template generation now.
`;

	const baseFlags = {
		settings: settingsJson,
		"mcp-config": mcpConfigPath,
		"append-system-prompt": systemPrompt,
	} as const;

	const flags = buildClaudeFlags({ ...baseFlags }, parsedArgs.values as ClaudeFlags);

	const initialPrompt = `Generate UI conversion templates for ${options.entity} using ${outputPath}. Output conversion-plan-ui.md and templates/ui only.`;
	const args = [...flags, initialPrompt];

	const imageCount = images.length;
	const imageInfo = imageCount > 0 
		? `║  Images: ${imageCount} WinForms screen image(s) found for look-and-feel${" ".padEnd(25, " ")}║\n`
		: "";

	console.log(`
╔════════════════════════════════════════════════════════════════════════════╗
║               CONVERSION TEMPLATE GENERATOR (UI - Interactive)              ║
║                                                                            ║
║  Entity: ${options.entity.padEnd(68, " ")}║
║  Output: ${outputPath.padEnd(67, " ")}║
${imageInfo}╚════════════════════════════════════════════════════════════════════════════╝
	`);
	
	if (imageCount > 0) {
		console.log(`\n📸 Found ${imageCount} WinForms screen image(s) - these will be used for look-and-feel reference`);
		if (imageMapping["search"]) console.log(`   - ${imageMapping["search"].length} search screen image(s)`);
		if (imageMapping["detail"]) console.log(`   - ${imageMapping["detail"].length} detail screen image(s)`);
		if (Object.keys(tabImageMapping).length > 0) {
			const tabImageCount = Object.values(tabImageMapping).reduce((sum, imgs) => sum + imgs.length, 0);
			console.log(`   - ${tabImageCount} tab-specific image(s) matched to ${Object.keys(tabImageMapping).length} tab(s)`);
		}
		if (imageMapping["general"]) console.log(`   - ${imageMapping["general"].length} additional image(s)`);
		console.log();
	}

	const child = spawn(["claude", ...args], {
		stdin: "inherit",
		stdout: "inherit",
		stderr: "inherit",
		env: {
			...process.env,
			CLAUDE_PROJECT_DIR: projectRoot,
			ENTITY_NAME: options.entity,
			OUTPUT_PATH: outputPath,
		},
	});

	const onExit = () => {
		try {
			child.kill("SIGTERM");
		} catch {}
	};

	process.on("SIGINT", onExit);
	process.on("SIGTERM", onExit);

	await child.exited;
	return child.exitCode ?? 0;
}

async function main() {
	const options = parseOptions();
	console.log(`[conversion-template-generator-ui] Launching interactive template generator for ${options.entity}...`);

	const code = await runTemplateGenerator(options);
	process.exit(code);
}

await main();
