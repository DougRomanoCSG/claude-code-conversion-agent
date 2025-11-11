#!/usr/bin/env -S bun run
/**
 * DATA ACCESS PATTERN ANALYZER: Extract stored procedures and queries
 */

import { spawn } from "bun";
import { buildClaudeFlags, parsedArgs } from "../lib/flags";
import type { ClaudeFlags } from "../lib/claude-flags.types";
import { getProjectRoot, getListPathForPrompt } from "../lib/paths";
import dataAccessSettings from "../settings/data-access.settings.json" with { type: "json" };
import dataAccessMcp from "../settings/data-access.mcp.json" with { type: "json" };

const projectRoot = getProjectRoot(import.meta.url);
const settingsJson = JSON.stringify(dataAccessSettings);
const mcpJson = JSON.stringify(dataAccessMcp);

async function main() {
	const entity = parsedArgs.values.entity as string;
	const interactive = parsedArgs.values.interactive === true;
	const outputDir = parsedArgs.values.output as string;

	if (!entity) {
		console.error("Error: --entity parameter is required");
		process.exit(1);
	}

	const outputPath = outputDir || `${projectRoot}output/${entity}`;

	const systemPrompt = `
You are a Data Access Pattern Analyzer agent.

TASK: Extract data access patterns for ${entity}.

TARGET FILES:
- List class: ${getListPathForPrompt(entity)}
- Business object CRUD methods

GOALS:
1. Extract stored procedure name and parameters
2. Parse AddFetchParameters for search criteria
3. Extract result column mapping from ReadRow
4. Identify CRUD operations
5. Extract data formatting logic

OUTPUT: ${outputPath}/data-access.json

Begin extraction.
`;

	const baseFlags = {
		settings: settingsJson,
		"mcp-config": mcpJson,
		...(interactive ? {} : { print: true, "output-format": "json" }),
	} as const;

	const flags = buildClaudeFlags({ ...baseFlags }, parsedArgs.values as ClaudeFlags);
	const child = spawn(["claude", ...flags, systemPrompt], {
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
