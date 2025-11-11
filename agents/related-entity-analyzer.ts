#!/usr/bin/env -S bun run
/**
 * RELATED ENTITY ANALYZER: Extract entity relationships
 */

import { spawn } from "bun";
import { buildClaudeFlags, parsedArgs } from "../lib/flags";
import type { ClaudeFlags } from "../lib/claude-flags.types";
import relatedSettings from "../settings/related-entity.settings.json" with { type: "json" };
import relatedMcp from "../settings/related-entity.mcp.json" with { type: "json" };

function resolvePath(relativeFromThisFile: string): string {
	const url = new URL(relativeFromThisFile, import.meta.url);
	return url.pathname;
}

const projectRoot = resolvePath("../");
const settingsJson = JSON.stringify(relatedSettings);
const mcpJson = JSON.stringify(relatedMcp);

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
You are a Related Entity Analyzer agent.

TASK: Extract entity relationships for ${entity}.

GOALS:
1. Identify child collection properties
2. Extract CRUD methods for related entities
3. Identify grid structures for related entities
4. Extract parent-child key relationships

OUTPUT: ${outputPath}/related-entities.json

Begin analysis.
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
