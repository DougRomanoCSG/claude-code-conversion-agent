#!/usr/bin/env -S bun run
/**
 * CONVERSION PLANNER: Plan conversions using conversion-orchestrator-prompt
 *
 * This agent specializes in creating comprehensive conversion plans,
 * managing dependencies, and coordinating multi-component conversions.
 *
 * Usage:
 *   bun run agents/conversion-planner.ts "Plan the Facility conversion"
 *   bun run agents/conversion-planner.ts --entity Facility
 *   bun run agents/conversion-planner.ts "Create a conversion plan for frmFacilitySearch and frmFacilityDetail"
 */

import { spawn } from "bun";
import type { ClaudeFlags } from "../lib/claude-flags.types";
import { buildClaudeFlags, getPositionals, parsedArgs, resolvePath } from "../lib/flags";
import conversionOrchestratorSettings from "../settings/conversion-planner.settings.json" with { type: "json" };
import conversionOrchestratorPrompt from "../system-prompts/conversion-orchestrator-prompt.md" with { type: "text" };

const projectRoot = resolvePath("../", import.meta.url);

async function main() {
	const positionals = getPositionals();
	const entity = parsedArgs.values.entity as string | undefined;

	// Build the prompt
	let prompt = positionals.join(" ").trim();

	// If entity flag is provided, enhance the prompt
	if (entity && !prompt.toLowerCase().includes(entity.toLowerCase())) {
		prompt = prompt
			? `Plan the ${entity} conversion: ${prompt}`
			: `Create a comprehensive conversion plan for the ${entity} entity and its related forms`;
	}

	if (!prompt) {
		console.error("Error: Please provide a task description or use --entity flag");
		console.error("\nUsage examples:");
		console.error("  bun run agents/conversion-planner.ts \"Plan the Facility conversion\"");
		console.error("  bun run agents/conversion-planner.ts --entity Facility");
		console.error("  bun run agents/conversion-planner.ts \"Create a conversion plan for frmFacilitySearch\"");
		process.exit(1);
	}

	console.log(`Starting conversion planning with prompt: ${prompt}\n`);

	const flags = buildClaudeFlags(
		{
			"append-system-prompt": conversionOrchestratorPrompt,
			settings: JSON.stringify(conversionOrchestratorSettings),
		},
		parsedArgs.values as ClaudeFlags
	);

	const args = [...flags, prompt];

	const child = spawn(["claude", ...args], {
		stdin: "inherit",
		stdout: "inherit",
		stderr: "inherit",
		env: {
			...process.env,
			CLAUDE_PROJECT_DIR: projectRoot,
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
	process.exit(child.exitCode ?? 0);
}

await main();
