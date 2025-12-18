#!/usr/bin/env -S bun run
/**
 * VALIDATION RULE EXTRACTOR: Extract all validation logic
 */

import { spawn } from "bun";
import { buildClaudeFlags, parsedArgs } from "../lib/flags";
import type { ClaudeFlags } from "../lib/claude-flags.types";
import validationSettings from "../settings/validation.settings.json" with { type: "json" };
import validationMcp from "../settings/validation.mcp.json" with { type: "json" };
import validationExtractorPrompt from "../system-prompts/validation-extractor-prompt.md" with { type: "text" };

function resolvePath(relativeFromThisFile: string): string {
	const url = new URL(relativeFromThisFile, import.meta.url);
	return url.pathname;
}

const projectRoot = resolvePath("../");
const settingsJson = JSON.stringify(validationSettings);
const mcpJson = JSON.stringify(validationMcp);

async function main() {
	const entity = parsedArgs.values.entity as string;
	const interactive = parsedArgs.values.interactive === true;
	const outputDir = parsedArgs.values.output as string;

	if (!entity) {
		console.error("Error: --entity parameter is required");
		process.exit(1);
	}

	const outputPath = outputDir || `${projectRoot}output/${entity}`;

	// Build context-specific prompt with entity details and paths
	const contextPrompt = `
TASK: Extract all validation rules for ${entity}.

EXTRACTION GOALS:
1. Parse validation methods in forms (AreFieldsValid)
2. Extract business rules from CheckBusinessRules
3. Identify field-level constraints
4. Extract error messages
5. Identify validation triggers

OUTPUT:
Generate a JSON file at: ${outputPath}/validation.json

Begin extraction.
`;

	const baseFlags = {
		"append-system-prompt": validationExtractorPrompt,
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
