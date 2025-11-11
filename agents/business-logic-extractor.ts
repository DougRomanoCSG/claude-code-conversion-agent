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
import { getProjectRoot, getBusinessObjectPathForPrompt, getBusinessObjectBasePathForPrompt, getLocationBasePathForPrompt, getAdminApiPath } from "../lib/paths";
import businessLogicSettings from "../settings/business-logic.settings.json" with { type: "json" };
import businessLogicMcp from "../settings/business-logic.mcp.json" with { type: "json" };

const projectRoot = getProjectRoot(import.meta.url);
const settingsJson = JSON.stringify(businessLogicSettings);
const mcpJson = JSON.stringify(businessLogicMcp);

interface ExtractorOptions {
	entity: string;
	interactive: boolean;
	outputDir?: string;
}

function parseOptions(): ExtractorOptions {
	const entity = parsedArgs.values.entity as string;
	const interactive = parsedArgs.values.interactive === true;
	const outputDir = parsedArgs.values.output as string;

	if (!entity) {
		console.error("Error: --entity parameter is required");
		process.exit(1);
	}

	return { entity, interactive, outputDir };
}

async function runBusinessLogicExtractor(options: ExtractorOptions): Promise<number> {
	const outputPath = options.outputDir || `${projectRoot}output/${options.entity}`;

	const systemPrompt = `
You are a specialized Business Logic Extractor agent.

TASK: Extract complete business logic from legacy VB.NET business objects for ${options.entity}.

TARGET FILES:
- Business Object: ${getBusinessObjectPathForPrompt(options.entity)}
- Base Class: ${getBusinessObjectBasePathForPrompt(options.entity)}
- Location Base: ${getLocationBasePathForPrompt()}

EXTRACTION GOALS:
1. Extract ALL properties from base and derived classes
2. Parse CheckBusinessRules method for validation logic
3. Extract BrokenRules.Assert calls to identify business rules
4. Extract initialization logic from Initialize method
5. Identify factory methods (New*, Get*)
6. Extract CRUD operation patterns
7. Document conditional validation (e.g., Lock/Gauge fields)

OUTPUT FORMAT:
Generate a JSON file at: ${outputPath}/business-logic.json

JSON STRUCTURE:
{
  "businessObject": "${options.entity}Location",
  "baseClass": "${options.entity}LocationBase",
  "properties": [
    {
      "name": "LocationID",
      "type": "Int32",
      "access": "ReadOnly",
      "isPrimaryKey": true
    }
  ],
  "businessRules": [
    {
      "ruleName": "MustBeBlank",
      "property": "LockUsaceName",
      "condition": "BargeExLocationType is not 'Lock' or 'Gauge Location'",
      "message": "USACE name must be blank..."
    }
  ],
  "methods": [...],
  "initialization": {...}
}

REFERENCE:
Compare with BoatLocation patterns in BargeOps.Admin.API (located at: ${getAdminApiPath()})

Begin extraction now.
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
			console.log(`Business logic extraction complete. Output: ${outputPath}/business-logic.json`);
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
