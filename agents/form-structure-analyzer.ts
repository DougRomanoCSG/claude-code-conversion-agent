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
import { getProjectRoot, getFormPathForPrompt, getFormDesignerPathForPrompt, getCrewingUiPath, getAdminUiPath, getCrewingUiExamples, getAdminUiExamples } from "../lib/paths";
import formAnalyzerSettings from "../settings/form-analyzer.settings.json" with { type: "json" };
import formAnalyzerMcp from "../settings/form-analyzer.mcp.json" with { type: "json" };

const projectRoot = getProjectRoot(import.meta.url);
const settingsJson = JSON.stringify(formAnalyzerSettings);
const mcpJson = JSON.stringify(formAnalyzerMcp);

interface AnalyzerOptions {
	entity: string;
	formType: "Search" | "Detail";
	interactive: boolean;
	outputDir?: string;
}

function parseOptions(): AnalyzerOptions {
	const entity = parsedArgs.values.entity as string;
	const formType = (parsedArgs.values["form-type"] || "Search") as "Search" | "Detail";
	const interactive = parsedArgs.values.interactive === true;
	const outputDir = parsedArgs.values.output as string;

	if (!entity) {
		console.error("Error: --entity parameter is required");
		process.exit(1);
	}

	return { entity, formType, interactive, outputDir };
}

async function runFormAnalyzer(options: AnalyzerOptions): Promise<number> {
	const outputPath = options.outputDir || `${projectRoot}output/${options.entity}`;
	const outputFileName = `form-structure-${options.formType.toLowerCase()}.json`;

	const systemPrompt = `
You are a specialized Form Structure Analyzer agent.

TASK: Extract complete form structure from legacy VB.NET Windows Forms for ${options.entity}${options.formType}.

TARGET FILES:
- Form: ${getFormPathForPrompt(options.entity, options.formType)}
- Designer: ${getFormDesignerPathForPrompt(options.entity, options.formType)}

EXTRACTION GOALS:
1. Extract ALL controls (textboxes, dropdowns, buttons, grids, checkboxes)
2. Extract grid column definitions from FormatGridColumns method
3. Extract event handler mappings
4. Extract validation patterns from AreFieldsValid
5. Extract dropdown population methods
6. Extract layout structure (panels, tabs)

OUTPUT FORMAT:
Generate a JSON file at: ${outputPath}/${outputFileName}

JSON STRUCTURE:
{
  "formName": "frm${options.entity}${options.formType}",
  "implements": ["IBaseSearch" or "IBaseDetailEdit"],
  "controls": [
    {
      "name": "txtName",
      "type": "UltraTextEditor",
      "category": "SearchCriteria",
      "label": "Field Name",
      "dataBinding": null
    }
  ],
  "grids": [
    {
      "name": "grdResults",
      "type": "UltraGrid",
      "uniqueIdentifier": "ID",
      "columns": [...]
    }
  ],
  "dropdowns": [...],
  "buttons": [...],
  "panels": [...]
}

REFERENCE EXAMPLES:
For UI patterns, reference BargeOps.Crewing.UI Admin screens:
- MVC Controllers: ${getCrewingUiExamples().controllers}
  Look for: CrewingSearchController.cs, BoatSearchController.cs - Admin search screen patterns
- Razor Views: ${getCrewingUiExamples().views}
  Look for: CrewingSearch/Index.cshtml, CrewingSearch/Edit.cshtml - Search and edit form patterns
- View Models: ${getCrewingUiExamples().viewModels}
  Look for: CrewingSearchViewModel.cs, CrewingEditViewModel.cs - View model structure
- JavaScript: ${getCrewingUiExamples().javascript}
  Look for: crewingSearch.js - DataTables initialization patterns

Target patterns in BargeOps.Admin.UI:
- Primary reference: ${getAdminUiExamples().controllers}/BoatLocationSearchController.cs
- Views: ${getAdminUiExamples().views}/BoatLocationSearch/ - Index.cshtml, Edit.cshtml, Details.cshtml
- View Models: ${getAdminUiExamples().viewModels}/BoatLocationSearchViewModel.cs

Begin analysis now.
`;

	const baseFlags = {
		settings: settingsJson,
		"mcp-config": mcpJson,
		...(options.interactive ? {} : { print: true, "output-format": "json" }),
	} as const;

	const flags = buildClaudeFlags({ ...baseFlags }, parsedArgs.values as ClaudeFlags);
	const args = [...flags, systemPrompt];

	const child = spawn(["claude", ...args], {
		stdin: "inherit",
		stdout: options.interactive ? "inherit" : "pipe",
		stderr: "inherit",
		env: {
			...process.env,
			CLAUDE_PROJECT_DIR: projectRoot,
			ENTITY_NAME: options.entity,
			FORM_TYPE: options.formType,
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
	console.log(`[form-structure-analyzer] Analyzing ${options.entity} ${options.formType} form...`);

	const code = await runFormAnalyzer(options);
	process.exit(code);
}

await main();
