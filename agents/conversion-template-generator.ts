#!/usr/bin/env -S bun run
/**
 * CONVERSION TEMPLATE GENERATOR: Generate complete conversion plan (ALWAYS INTERACTIVE)
 *
 * This agent ALWAYS runs interactively in Claude Code.
 * It takes all extracted data from steps 1-10 and generates:
 * - Complete conversion plan document
 * - Code templates for domain models, DTOs, repositories, services, controllers
 * - View models and Razor views
 * - JavaScript files
 * - Step-by-step implementation guide
 *
 * Usage:
 *   bun run agents/conversion-template-generator.ts --entity "Facility"
 *
 * NOTE: This agent always runs interactively, --interactive flag is implicit
 */

import { spawn } from "bun";
import { buildClaudeFlags, parsedArgs } from "../lib/flags";
import type { ClaudeFlags } from "../lib/claude-flags.types";
import { getProjectRoot, getCrewingApiPath, getCrewingUiPath, getAdminApiPath, getAdminUiPath } from "../lib/paths";
import templateGenSettings from "../settings/template-generator.settings.json" with { type: "json" };
import templateGenMcp from "../settings/template-generator.mcp.json" with { type: "json" };

const projectRoot = getProjectRoot(import.meta.url);
const settingsJson = JSON.stringify(templateGenSettings);
const mcpJson = JSON.stringify(templateGenMcp);

interface GeneratorOptions {
	entity: string;
	outputDir?: string;
}

function parseOptions(): GeneratorOptions {
	const entity = parsedArgs.values.entity as string;
	const outputDir = parsedArgs.values.output as string;

	if (!entity) {
		console.error("Error: --entity parameter is required");
		process.exit(1);
	}

	return { entity, outputDir };
}

async function runTemplateGenerator(options: GeneratorOptions): Promise<number> {
	const outputPath = options.outputDir || `${projectRoot}output/${options.entity}`;

	const analysisFiles = [
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

	const systemPrompt = `
You are a specialized Conversion Template Generator agent running in INTERACTIVE MODE.

TASK: Generate complete conversion plan and code templates for ${options.entity}.

INPUT DATA:
You have access to analysis files in: ${outputPath}/
${analysisFiles.map((f) => `- ${f}`).join("\n")}

GENERATION GOALS:
1. Review all extracted data from previous agents
2. Create comprehensive conversion plan document
3. Generate code templates organized by project:
   
   FOR BargeOps.Admin.API (${getAdminApiPath()}):
   - Domain models (C# classes)
   - DTOs (Request/Response objects)
   - Repository interface and implementation
   - Service interface and implementation
   - API Controller with all endpoints
   - AutoMapper profiles
   
   FOR BargeOps.Admin.UI (${getAdminUiPath()}):
   - View models (MVC view models)
   - Razor views (Index, Edit, Details)
   - JavaScript files (DataTables initialization)
   - CSS files (if needed)
4. Include step-by-step implementation guide
5. Reference BoatLocation conversion patterns
6. Use Crewing examples from BargeOps.Crewing.API and BargeOps.Crewing.UI for clarity

OUTPUT:
Primary file: ${outputPath}/conversion-plan.md
API templates: ${outputPath}/templates/api/ (for BargeOps.Admin.API)
UI templates: ${outputPath}/templates/ui/ (for BargeOps.Admin.UI)

REFERENCE PATTERNS:
- BargeOps.Admin.API: BoatLocation controller, services, repositories (located at: ${getAdminApiPath()})
- BargeOps.Crewing.API: ${getCrewingApiPath()} (Example patterns for clarity)
- BargeOps.Admin.UI: BoatLocationSearch controller, views, services (located at: ${getAdminUiPath()})
- BargeOps.Crewing.UI: ${getCrewingUiPath()} (Example UI patterns)

TARGET PROJECTS (where generated code will be placed):
- API: ${getAdminApiPath()}
- UI: ${getAdminUiPath()}

This is an INTERACTIVE session. You can ask questions, clarify requirements, and iterate on the templates.

Begin template generation now.
`;

	const baseFlags = {
		settings: settingsJson,
		"mcp-config": mcpJson,
	} as const;

	const flags = buildClaudeFlags({ ...baseFlags }, parsedArgs.values as ClaudeFlags);
	const args = [...flags, systemPrompt];

	console.log(`
╔════════════════════════════════════════════════════════════════════════════╗
║              CONVERSION TEMPLATE GENERATOR (Interactive Mode)               ║
║                                                                            ║
║  Entity: ${options.entity.padEnd(68, " ")}║
║  Output: ${outputPath.padEnd(67, " ")}║
║                                                                            ║
║  This agent will generate conversion templates based on the analysis       ║
║  from steps 1-10. You can interact with Claude to refine the templates.   ║
╚════════════════════════════════════════════════════════════════════════════╝
	`);

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
	console.log(`[conversion-template-generator] Launching interactive template generator for ${options.entity}...`);

	const code = await runTemplateGenerator(options);
	process.exit(code);
}

await main();
