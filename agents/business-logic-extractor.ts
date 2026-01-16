#!/usr/bin/env -S bun run
/**
 * BUSINESS LOGIC EXTRACTOR: Extract business rules and validation
 *
 * Analyzes business objects to extract:
 * - All properties with types and access modifiers
 * - Business rules from CheckBusinessRules method
 * - Validation logic and error messages
 * - Initialization patterns
 * - Factory methods
 * - CRUD operations
 *
 * Usage:
 *   bun run agents/business-logic-extractor.ts --entity "Facility"
 *   bun run agents/business-logic-extractor.ts --entity "Facility" --interactive
 */

import { spawn } from "bun";
import { buildClaudeFlags, parsedArgs } from "../lib/flags";
import type { ClaudeFlags } from "../lib/claude-flags.types";
import { getProjectRoot, getBusinessObjectPathForPrompt, getBusinessObjectBasePathForPrompt, getLocationBasePathForPrompt, getAdminApiPath, getAdminApiExamples, getCrewingApiExamples } from "../lib/paths";
import businessLogicSettings from "../settings/business-logic.settings.json" with { type: "json" };
import businessLogicMcp from "../settings/business-logic.mcp.json" with { type: "json" };
import businessLogicExtractorPrompt from "../system-prompts/business-logic-extractor-prompt.md" with { type: "text" };

const projectRoot = getProjectRoot(import.meta.url);
const settingsJson = JSON.stringify(businessLogicSettings);
const mcpJson = JSON.stringify(businessLogicMcp);

interface ExtractorOptions {
	entity: string;
	interactive: boolean;
	outputDir?: string;
	outputFile?: string;
}

function parseOptions(): ExtractorOptions {
	const entity = parsedArgs.values.entity as string;
	const interactive = parsedArgs.values.interactive === true;
	const outputDir = parsedArgs.values.output as string;
	const outputFile = parsedArgs.values["output-file"] as string;

	if (!entity) {
		console.error("Error: --entity parameter is required");
		process.exit(1);
	}

	return { entity, interactive, outputDir, outputFile };
}

async function runBusinessLogicExtractor(options: ExtractorOptions): Promise<number> {
	const outputPath = options.outputDir || `${projectRoot}output/${options.entity}`;
	const outputFileName = options.outputFile || "business-logic.json";

	// Build context-specific prompt with entity details and paths
	const contextPrompt = `
TASK: Extract complete business logic from legacy VB.NET business objects for ${options.entity}.

TARGET FILES:
- Business Object: ${getBusinessObjectPathForPrompt(options.entity)}
- Base Class: ${getBusinessObjectBasePathForPrompt(options.entity)}
- Location Base: ${getLocationBasePathForPrompt()}

OUTPUT:
Generate a JSON file at: ${outputPath}/${outputFileName}

Expected business object structure:
- Business Object: "${options.entity}Location"
- Base Class: "${options.entity}LocationBase"

ARCHITECTURE REFERENCES:
For business logic patterns, reference:
- BargeOps.Admin.API Domain Models: ${getAdminApiExamples().domainModels}
  Primary reference: BoatLocation.cs - Canonical Admin domain model pattern
- BargeOps.Admin.API Services: ${getAdminApiExamples().services}
  Reference: IBoatLocationService.cs, BoatLocationService.cs - Business logic implementation
- BargeOps.Crewing.API Domain Models: ${getCrewingApiExamples().domainModels}
  Examples: Crewing.cs, Boat.cs - See how business rules are structured
- BargeOps.Crewing.API Services: ${getCrewingApiExamples().services}
  Examples: ICrewingService.cs, CrewingService.cs - Service layer patterns

Begin extraction now.
`;

	const baseFlags = {
		"append-system-prompt": businessLogicExtractorPrompt,
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
			console.log(`Business logic extraction complete. Output: ${outputPath}/${outputFileName}`);
		}

		return exitCode;
	}

	await child.exited;
	return child.exitCode ?? 0;
}

async function main() {
	const options = parseOptions();
	console.log(`[business-logic-extractor] Extracting business logic for ${options.entity}...`);

	const code = await runBusinessLogicExtractor(options);
	process.exit(code);
}

await main();
