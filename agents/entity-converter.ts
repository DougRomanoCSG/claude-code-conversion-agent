#!/usr/bin/env -S bun run
/**
 * ENTITY CONVERTER: Convert and migrate entities using entity-conversion-prompt
 *
 * This agent specializes in converting legacy entities to ASP.NET Core entities
 * with proper Dapper data access, ViewModels, and documentation.
 *
 * Usage:
 *   bun run agents/entity-converter.ts "Convert the Facility entity"
 *   bun run agents/entity-converter.ts --entity Facility
 *   bun run agents/entity-converter.ts --entity Facility "Add navigation properties"
 */

import { spawn } from "bun";
import type { ClaudeFlags } from "../lib/claude-flags.types";
import { buildClaudeFlags, getPositionals, parsedArgs, resolvePath } from "../lib/flags";
import entityConversionSettings from "../settings/entity-converter.settings.json" with { type: "json" };
import entityConversionPrompt from "../system-prompts/entity-conversion-prompt.md" with { type: "text" };

const projectRoot = resolvePath("../", import.meta.url);

async function main() {
	const positionals = getPositionals();
	const entity = parsedArgs.values.entity as string | undefined;

	// Build the prompt
	let prompt = positionals.join(" ").trim();

	// If entity flag is provided, prepend it to the prompt
	if (entity && !prompt.includes(entity)) {
		prompt = prompt ? `Convert the ${entity} entity: ${prompt}` : `Convert the ${entity} entity`;
	}

	if (!prompt) {
		console.error("Error: Please provide a task description or use --entity flag");
		console.error("\nUsage examples:");
		console.error("  bun run agents/entity-converter.ts \"Convert the Facility entity\"");
		console.error("  bun run agents/entity-converter.ts --entity Facility");
		console.error("  bun run agents/entity-converter.ts --entity Facility \"Add navigation properties\"");
		process.exit(1);
	}

	console.log(`Starting entity conversion with prompt: ${prompt}\n`);

	const flags = buildClaudeFlags(
		{
			"append-system-prompt": entityConversionPrompt,
			settings: JSON.stringify(entityConversionSettings),
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
