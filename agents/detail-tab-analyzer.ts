#!/usr/bin/env -S bun run
/**
 * DETAIL FORM TAB ANALYZER: Extract tab structure and related entities
 */

import { spawn } from "bun";
import { buildClaudeFlags, parsedArgs } from "../lib/flags";
import type { ClaudeFlags } from "../lib/claude-flags.types";
import tabSettings from "../settings/tab-analyzer.settings.json" with { type: "json" };
import tabMcp from "../settings/tab-analyzer.mcp.json" with { type: "json" };

function resolvePath(relativeFromThisFile: string): string {
	const url = new URL(relativeFromThisFile, import.meta.url);
	return url.pathname;
}

const projectRoot = resolvePath("../");
const settingsJson = JSON.stringify(tabSettings);
const mcpJson = JSON.stringify(tabMcp);

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
You are a Detail Form Tab Analyzer agent.

TASK: Extract tab structure for ${entity}Detail form.

GOALS:
1. Parse tab definitions from Designer file
2. Extract controls per tab
3. Identify related entity grids
4. Extract toolbar button configurations
5. Identify shared controls (submit/cancel)

OUTPUT: ${outputPath}/tabs.json

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
