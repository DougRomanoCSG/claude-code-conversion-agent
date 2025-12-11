#!/usr/bin/env -S bun run
/**
 * VIEWMODEL CREATOR: Create ViewModels using viewmodel-generator-prompt
 *
 * This agent specializes in creating ViewModels following MVVM patterns,
 * with proper validation, display attributes, and UI-specific properties.
 *
 * Usage:
 *   bun run agents/viewmodel-creator.ts "Create a ViewModel for Facility list view"
 *   bun run agents/viewmodel-creator.ts --entity Facility "Create list and edit ViewModels"
 *   bun run agents/viewmodel-creator.ts --entity Facility --form-type Detail
 */

import { spawn } from "bun";
import type { ClaudeFlags } from "../lib/claude-flags.types";
import { buildClaudeFlags, getPositionals, parsedArgs, resolvePath } from "../lib/flags";
import viewmodelGeneratorSettings from "../settings/viewmodel-creator.settings.json" with { type: "json" };
import viewmodelGeneratorPrompt from "../system-prompts/viewmodel-generator-prompt.md" with { type: "text" };

const projectRoot = resolvePath("../", import.meta.url);

async function main() {
	const positionals = getPositionals();
	const entity = parsedArgs.values.entity as string | undefined;
	const formType = parsedArgs.values["form-type"] as string | undefined;

	// Build the prompt
	let prompt = positionals.join(" ").trim();

	// Enhance prompt with entity and form type info
	if (entity && !prompt.toLowerCase().includes(entity.toLowerCase())) {
		if (formType) {
			prompt = prompt
				? `Create ViewModels for ${entity} ${formType} form: ${prompt}`
				: `Create ViewModels for ${entity} ${formType} form`;
		} else {
			prompt = prompt
				? `Create ViewModels for ${entity}: ${prompt}`
				: `Create ViewModels for ${entity}`;
		}
	}

	if (!prompt) {
		console.error("Error: Please provide a task description or use --entity flag");
		console.error("\nUsage examples:");
		console.error("  bun run agents/viewmodel-creator.ts \"Create a ViewModel for Facility list view\"");
		console.error("  bun run agents/viewmodel-creator.ts --entity Facility");
		console.error("  bun run agents/viewmodel-creator.ts --entity Facility --form-type Detail");
		process.exit(1);
	}

	console.log(`Starting ViewModel creation with prompt: ${prompt}\n`);

	const flags = buildClaudeFlags(
		{
			"append-system-prompt": viewmodelGeneratorPrompt,
			settings: JSON.stringify(viewmodelGeneratorSettings),
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
