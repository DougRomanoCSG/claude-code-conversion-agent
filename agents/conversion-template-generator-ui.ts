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

function getAnalysisFilesForMode(
	mode: "search-detail" | "single-form",
	options: GeneratorOptions,
): string[] {
	const searchFormName = `frm${options.entity}Search`;
	const detailFormName = `frm${options.entity}Detail`;
	const primaryFormName = options.formName || searchFormName;
	const singleFormName = options.formName || `frm${options.entity}`;

	if (mode === "single-form") {
		return [
			`${singleFormName}/${appendFormToFileName("form-structure.json", singleFormName)}`,
			`${singleFormName}/${appendFormToFileName("business-logic.json", singleFormName)}`,
			`${singleFormName}/${appendFormToFileName("data-access.json", singleFormName)}`,
			`${singleFormName}/${appendFormToFileName("security.json", singleFormName)}`,
			`${singleFormName}/${appendFormToFileName("ui-mapping.json", singleFormName)}`,
			`${singleFormName}/${appendFormToFileName("workflow.json", singleFormName)}`,
			`${singleFormName}/${appendFormToFileName("tabs.json", singleFormName)}`,
			`${singleFormName}/${appendFormToFileName("validation.json", singleFormName)}`,
			`${singleFormName}/${appendFormToFileName("related-entities.json", singleFormName)}`,
		];
	}

	return [
		`${searchFormName}/${appendFormToFileName("form-structure-search.json", searchFormName)}`,
		`${detailFormName}/${appendFormToFileName("form-structure-detail.json", detailFormName)}`,
		`${primaryFormName}/${appendFormToFileName("business-logic.json", primaryFormName)}`,
		`${primaryFormName}/${appendFormToFileName("data-access.json", primaryFormName)}`,
		`${primaryFormName}/${appendFormToFileName("security.json", primaryFormName)}`,
		`${primaryFormName}/${appendFormToFileName("ui-mapping.json", primaryFormName)}`,
		`${primaryFormName}/${appendFormToFileName("workflow.json", primaryFormName)}`,
		`${detailFormName}/${appendFormToFileName("tabs.json", detailFormName)}`,
		`${primaryFormName}/${appendFormToFileName("validation.json", primaryFormName)}`,
		`${primaryFormName}/${appendFormToFileName("related-entities.json", primaryFormName)}`,
	];
}

function detectAnalysisMode(outputPath: string, options: GeneratorOptions): "search-detail" | "single-form" {
	// If user explicitly provides a form name, treat non-Search/Detail as a single-form workflow.
	if (options.formName) {
		const isSearchOrDetail = /^(frm)?\w+(Search|Detail)$/i.test(options.formName);
		if (!isSearchOrDetail) return "single-form";
	}

	// Infer from output files already present.
	const searchFormName = `frm${options.entity}Search`;
	const detailFormName = `frm${options.entity}Detail`;
	const singleFormName = options.formName || `frm${options.entity}`;

	if (existsSync(join(outputPath, singleFormName, appendFormToFileName("form-structure.json", singleFormName)))) {
		return "single-form";
	}
	if (
		existsSync(join(outputPath, searchFormName, appendFormToFileName("form-structure-search.json", searchFormName))) ||
		existsSync(join(outputPath, detailFormName, appendFormToFileName("form-structure-detail.json", detailFormName)))
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
async function findImageFiles(
	outputPath: string,
	options: GeneratorOptions,
	mode: "search-detail" | "single-form",
): Promise<{
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
	const searchFormName = `frm${options.entity}Search`;
	const detailFormName = `frm${options.entity}Detail`;
	const primaryFormName = options.formName || searchFormName;
	const singleFormName = options.formName || `frm${options.entity}`;
	const formOutputDirs = mode === "single-form"
		? [join(outputPath, singleFormName)]
		: [join(outputPath, searchFormName), join(outputPath, detailFormName), join(outputPath, primaryFormName)];
	
	// Read tabs.json to get tab names for matching
	const tabsPath = mode === "single-form"
		? join(outputPath, singleFormName, appendFormToFileName("tabs.json", singleFormName))
		: join(outputPath, detailFormName, appendFormToFileName("tabs.json", detailFormName));
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
	
	const imagePathSet = new Set<string>();
	const searchDirs = [outputPath, ...formOutputDirs].filter(dir => existsSync(dir));

	try {
		for (const dir of searchDirs) {
			const files = readdirSync(dir);

			for (const file of files) {
				const ext = extname(file).toLowerCase();
				if (!imageExtensions.includes(ext)) {
					continue;
				}

				const imagePath = join(dir, file);
				if (imagePathSet.has(imagePath)) {
					continue;
				}

				imagePathSet.add(imagePath);
				images.push(imagePath);

				const baseName = basename(file, ext).toLowerCase();
				const baseNameClean = baseName.replace(/^frm/, "").replace(/^tab/, "");

				if (baseName.includes("search") || baseName.includes("list") || baseName.includes("index")) {
					if (!imageMapping["search"]) imageMapping["search"] = [];
					imageMapping["search"].push(imagePath);
				} else if (baseName.includes("detail") || baseName.includes("edit") || baseName.includes("view")) {
					if (!imageMapping["detail"]) imageMapping["detail"] = [];
					imageMapping["detail"].push(imagePath);
				} else {
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
	const analysisFiles = getAnalysisFilesForMode(mode, options);
	const optionalFiles = ["child-forms.json"];

	// Ensure analysis outputs (steps 1-10) exist before starting template generation.
	await ensureAnalysisOutputsExist(options, outputPath, analysisFiles, mode);

	// Find image files for look-and-feel reference
	const { images, imageMapping, tabImageMapping } = await findImageFiles(outputPath, options, mode);

	const imageInfo = images.length > 0 ? `\n\nIMAGES: ${images.length} WinForms screenshots in ${outputPath}/ - Review all images for complete field coverage.` : "";

	const systemPrompt = `You are a WinForms to ASP.NET Core MVC conversion specialist.

TASK: Generate UI conversion templates for ${options.entity}.
Input: Analysis JSON files in ${outputPath}/
Output: ${outputPath}/conversion-plan-ui.md and Templates/ui/ (Controllers, Services, ViewModels, Views, JS)${imageInfo}

CRITICAL REQUIREMENTS:
1. Include ALL detail screen buttons, dialogs, and child forms
2. Follow BargeOps Crewing UI patterns from reference docs (UIStandards.md, DataTables.md, BargeOpsCrewUI.md)
3. Use AppController base class, CrewingBaseService pattern, DataTables for grids
4. Document look-and-feel: layout, grouping, spacing, visual hierarchy
5. Reference: ${getCrewingUiPath()}
6. Target: ${getAdminUiPath()}

Generate conversion plan and templates now.`;

	const baseFlags = {
		settings: settingsJson,
		"mcp-config": mcpConfigPath,
		"system-prompt": systemPrompt,
	} as const;

	const flags = buildClaudeFlags({ ...baseFlags }, parsedArgs.values as ClaudeFlags);

	const initialPrompt = `Generate UI conversion templates for ${options.entity} using ${outputPath}. Output conversion-plan-ui.md and Templates/ui only.`;
	const args = [...flags, initialPrompt];

	const imageCount = images.length;
	const imageDisplayInfo = imageCount > 0
		? `║  Images: ${imageCount} WinForms screen image(s) found for look-and-feel${" ".padEnd(25, " ")}║\n`
		: "";

	console.log(`
╔════════════════════════════════════════════════════════════════════════════╗
║               CONVERSION TEMPLATE GENERATOR (UI - Interactive)              ║
║                                                                            ║
║  Entity: ${options.entity.padEnd(68, " ")}║
║  Output: ${outputPath.padEnd(67, " ")}║
${imageDisplayInfo}╚════════════════════════════════════════════════════════════════════════════╝
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
