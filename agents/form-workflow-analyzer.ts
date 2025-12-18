#!/usr/bin/env -S bun run
/**
 * FORM WORKFLOW ANALYZER: Extract user flows and state management
 */

import { spawn } from "bun";
import { buildClaudeFlags, parsedArgs } from "../lib/flags";
import type { ClaudeFlags } from "../lib/claude-flags.types";
import workflowSettings from "../settings/workflow.settings.json" with { type: "json" };
import workflowMcp from "../settings/workflow.mcp.json" with { type: "json" };
import formWorkflowAnalyzerPrompt from "../system-prompts/form-workflow-analyzer-prompt.md" with { type: "text" };

function resolvePath(relativeFromThisFile: string): string {
	const url = new URL(relativeFromThisFile, import.meta.url);
	return url.pathname;
}

const projectRoot = resolvePath("../");
const settingsJson = JSON.stringify(workflowSettings);
const mcpJson = JSON.stringify(workflowMcp);

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
TASK: Extract user flows and state management for ${entity}.

ANALYSIS GOALS:
1. Trace event handler chains
2. Identify form lifecycle methods
3. Extract state persistence patterns
4. Identify modal dialog patterns
5. Extract refresh/update triggers

OUTPUT:
Generate a JSON file at: ${outputPath}/workflow.json

Begin analysis.
`;

	const baseFlags = {
		"append-system-prompt": formWorkflowAnalyzerPrompt,
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
