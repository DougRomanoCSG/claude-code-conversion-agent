#!/usr/bin/env -S bun run
/**
 * FORM STRUCTURE ANALYZER: Extract UI components, controls, and layouts
 *
 * Analyzes legacy Windows Forms to extract complete form structure including:
 * - All controls (textboxes, dropdowns, grids, buttons)
 * - Grid column definitions
 * - Event handlers
 * - Validation patterns
 * - Layout structure
 *
 * Usage:
 *   bun run agents/form-structure-analyzer.ts --entity "Facility" --form-type "Search"
 *   bun run agents/form-structure-analyzer.ts --entity "Facility" --form-type "Detail" --interactive
 */

import { spawn } from "bun";
import { buildClaudeFlags, parsedArgs } from "../lib/flags";
import type { ClaudeFlags } from "../lib/claude-flags.types";
import {
	getProjectRoot,
	getFormDesignerPathByNameForPrompt,
	getFormDesignerPathForPrompt,
	getFormPathByNameForPrompt,
	getFormPathForPrompt,
	getCrewingUiPath,
	getAdminUiPath,
	getCrewingUiExamples,
	getAdminUiExamples,
	getSharedExamples,
} from "../lib/paths";
import formAnalyzerSettings from "../settings/form-analyzer.settings.json" with { type: "json" };
import formAnalyzerMcp from "../settings/form-analyzer.mcp.json" with { type: "json" };
import formStructureAnalyzerPrompt from "../system-prompts/form-structure-analyzer-prompt.md" with { type: "text" };

const projectRoot = getProjectRoot(import.meta.url);
const settingsJson = JSON.stringify(formAnalyzerSettings);
const mcpJson = JSON.stringify(formAnalyzerMcp);

interface AnalyzerOptions {
	entity: string;
	formType: "Search" | "Detail";
	formName?: string;
	interactive: boolean;
	outputDir?: string;
	outputFile?: string;
}

function parseOptions(): AnalyzerOptions {
	const entity = parsedArgs.values.entity as string;
	const formType = (parsedArgs.values["form-type"] || "Search") as "Search" | "Detail";
	const formName = parsedArgs.values["form-name"] as string;
	const interactive = parsedArgs.values.interactive === true;
	const outputDir = parsedArgs.values.output as string;
	const outputFile = parsedArgs.values["output-file"] as string;

	if (!entity) {
		console.error("Error: --entity parameter is required");
		process.exit(1);
	}

	return { entity, formType, formName, interactive, outputDir, outputFile };
}

async function runFormAnalyzer(options: AnalyzerOptions): Promise<number> {
	const outputPath = options.outputDir || `${projectRoot}output/${options.entity}`;
	const isSingleForm = !!options.formName;
	const defaultOutputFileName = isSingleForm ? "form-structure.json" : `form-structure-${options.formType.toLowerCase()}.json`;
	const outputFileName = options.outputFile || defaultOutputFileName;

	// Build context-specific prompt with entity details and paths
	const formLabel = isSingleForm ? options.formName! : `frm${options.entity}${options.formType}`;
	const formPath = isSingleForm
		? getFormPathByNameForPrompt(options.formName!)
		: getFormPathForPrompt(options.entity, options.formType);
	const designerPath = isSingleForm
		? getFormDesignerPathByNameForPrompt(options.formName!)
		: getFormDesignerPathForPrompt(options.entity, options.formType);

	const contextPrompt = `
TASK: Extract complete form structure from legacy VB.NET Windows Forms for ${formLabel}.

TARGET FILES:
- Form: ${formPath}
- Designer: ${designerPath}

OUTPUT:
Generate a JSON file at: ${outputPath}/${outputFileName}

ARCHITECTURE REFERENCES:
- Shared DTOs: ${getSharedExamples().dtos}
- Crewing UI Controllers: ${getCrewingUiExamples().controllers}
- Crewing UI Views: ${getCrewingUiExamples().views}
- Crewing UI ViewModels: ${getCrewingUiExamples().viewModels}
- Crewing UI JavaScript: ${getCrewingUiExamples().javascript}
- Admin UI Controllers: ${getAdminUiExamples().controllers}
- Admin UI Views: ${getAdminUiExamples().views}
- Admin UI ViewModels: ${getAdminUiExamples().viewModels}

REFERENCE EXAMPLES:
For UI patterns, reference BargeOps.Crewing.UI Admin screens:
- Look for: CrewingSearchController.cs, BoatSearchController.cs - Admin search screen patterns
- Look for: CrewingSearch/Index.cshtml, CrewingSearch/Edit.cshtml - Search and edit form patterns
- Look for: CrewingSearchViewModel.cs, CrewingEditViewModel.cs - View model structure
- Look for: crewingSearch.js - DataTables initialization patterns

Target patterns in BargeOps.Admin.UI:
- Primary reference: BoatLocationSearchController.cs
- Views: BoatLocationSearch/ - Index.cshtml, Edit.cshtml, Details.cshtml
- View Models: BoatLocationSearchViewModel.cs, BoatLocationEditViewModel.cs
- DTOs: BoatLocationDto.cs, FacilityDto.cs (used in ViewModels and API responses)

Begin analysis now.
`;

	const baseFlags = {
		"append-system-prompt": formStructureAnalyzerPrompt,
		settings: settingsJson,
		"mcp-config": mcpJson,
		...(options.interactive ? {} : { print: true, "output-format": "json" }),
	} as const;

	const flags = buildClaudeFlags({ ...baseFlags }, parsedArgs.values as ClaudeFlags);
	const args = [...flags, contextPrompt];

	const child = spawn(["claude", ...args], {
		stdin: "inherit",
		stdout: options.interactive ? "inherit" : "pipe",
		stderr: "inherit",
		env: {
			...process.env,
			CLAUDE_PROJECT_DIR: projectRoot,
			ENTITY_NAME: options.entity,
			FORM_TYPE: isSingleForm ? "Single" : options.formType,
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

	if (!options.interactive) {
		const stdoutText = await new Response(child.stdout).text();
		const exitCode = await child.exited;

		if (exitCode === 0) {
			console.log(`Form structure analysis complete. Output: ${outputPath}/${outputFileName}`);
		}

		return exitCode;
	}

	await child.exited;
	return child.exitCode ?? 0;
}

async function main() {
	const options = parseOptions();
	if (options.formName) {
		console.log(`[form-structure-analyzer] Analyzing ${options.formName} (single form) ...`);
	} else {
		console.log(`[form-structure-analyzer] Analyzing ${options.entity} ${options.formType} form...`);
	}

	const code = await runFormAnalyzer(options);
	process.exit(code);
}

await main();
