#!/usr/bin/env -S bun run
/**
 * UI COMPONENT MAPPER: Map legacy controls to modern equivalents
 */

import { spawn } from "bun";
import { buildClaudeFlags, parsedArgs } from "../lib/flags";
import type { ClaudeFlags } from "../lib/claude-flags.types";
import { getProjectRoot, getCrewingUiPath, getCrewingUiExamples, getAdminUiExamples } from "../lib/paths";
import uiMapperSettings from "../settings/ui-mapper.settings.json" with { type: "json" };
import uiMapperMcp from "../settings/ui-mapper.mcp.json" with { type: "json" };
import uiComponentMapperPrompt from "../system-prompts/ui-component-mapper-prompt.md" with { type: "text" };

const projectRoot = getProjectRoot(import.meta.url);
const settingsJson = JSON.stringify(uiMapperSettings);
const mcpJson = JSON.stringify(uiMapperMcp);

async function main() {
	const entity = parsedArgs.values.entity as string;
	const interactive = parsedArgs.values.interactive === true;
	const outputDir = parsedArgs.values.output as string;
	const outputFile = parsedArgs.values["output-file"] as string;

	if (!entity) {
		console.error("Error: --entity parameter is required");
		process.exit(1);
	}

	const outputPath = outputDir || `${projectRoot}output/${entity}`;
	const outputFileName = outputFile || "ui-mapping.json";

	// Build context-specific prompt with entity details and paths
	const contextPrompt = `
TASK: Map legacy controls to modern equivalents for ${entity}.

MAPPING GOALS:
1. Map UltraGrid to DataTables
2. Map UltraCombo to Select2
3. Map UltraPanel to Bootstrap Card
4. Map UltraTabControl to Bootstrap Nav Tabs
5. Map TextBox to Bootstrap Form Input
6. Map CheckBox to Bootstrap Checkbox

OUTPUT:
Generate a JSON file at: ${outputPath}/${outputFileName}

ARCHITECTURE REFERENCES:
For UI component mapping patterns, reference BargeOps.Crewing.UI Admin screens:
- JavaScript/DataTables: ${getCrewingUiExamples().javascript}
  Look for: crewingSearch.js - DataTables initialization and server-side processing
- Razor Views: ${getCrewingUiExamples().views}
  Look for: CrewingSearch/Index.cshtml - Bootstrap forms, Select2, DataTables HTML
- CSS: ${getCrewingUiExamples().css}
  Look for: crewingSearch.css - Custom styling patterns

Target patterns in BargeOps.Admin.UI:
- JavaScript: ${getAdminUiExamples().javascript}/boatLocationSearch.js
- Views: ${getAdminUiExamples().views}/BoatLocationSearch/Index.cshtml

Begin mapping.
`;

	const baseFlags = {
		"append-system-prompt": uiComponentMapperPrompt,
		settings: settingsJson,
		"mcp-config": mcpJson,
		...(interactive ? {} : { print: true, "output-format": "json" }),
	} as const;

	const flags = buildClaudeFlags({ ...baseFlags }, parsedArgs.values as ClaudeFlags);
	const child = spawn(["claude", ...flags, contextPrompt], {
		stdin: "inherit",
		stdout: interactive ? "inherit" : "pipe",
		stderr: "inherit",
		env: { ...process.env, CLAUDE_PROJECT_DIR: projectRoot },
	});

	const onExit = () => { try { child.kill("SIGTERM"); } catch {} };
	process.on("SIGINT", onExit);
	process.on("SIGTERM", onExit);

	await child.exited;
	process.exit(child.exitCode ?? 0);
}

await main();
