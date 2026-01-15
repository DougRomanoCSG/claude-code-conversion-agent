#!/usr/bin/env -S bun run
/**
 * CONVERSION TEMPLATE GENERATOR (WRAPPER): Run API + UI template generation sequentially
 *
 * Usage:
 *   bun run agents/conversion-template-generator.ts --entity "Facility"
 *   bun run agents/conversion-template-generator.ts --entity "Facility" --api-only
 *   bun run agents/conversion-template-generator.ts --entity "Facility" --ui-only
 */

import { spawn } from "bun";
import { parsedArgs } from "../lib/flags";
import { getProjectRoot } from "../lib/paths";

const projectRoot = getProjectRoot(import.meta.url);

interface GeneratorOptions {
	entity: string;
	outputDir?: string;
	formName?: string;
	apiOnly?: boolean;
	uiOnly?: boolean;
}

function parseOptions(): GeneratorOptions {
	const entity = parsedArgs.values.entity as string;
	const outputDir = parsedArgs.values.output as string;
	const formName = parsedArgs.values["form-name"] as string;
	const apiOnly = parsedArgs.values["api-only"] as boolean;
	const uiOnly = parsedArgs.values["ui-only"] as boolean;

	if (!entity) {
		console.error("Error: --entity parameter is required");
		process.exit(1);
	}

	if (apiOnly && uiOnly) {
		console.error("Error: --api-only and --ui-only cannot be used together");
		process.exit(1);
	}

	return { entity, outputDir, formName, apiOnly, uiOnly };
}

async function runAgent(script: string, options: GeneratorOptions): Promise<number> {
	const args = ["run", `${projectRoot}agents/${script}`, "--entity", options.entity];
	if (options.outputDir) args.push("--output", options.outputDir);
	if (options.formName) args.push("--form-name", options.formName);

	console.log(`\n➡️  Running: bun ${args.join(" ")}`);
	const child = spawn(["bun", ...args], {
		stdin: "inherit",
		stdout: "inherit",
		stderr: "inherit",
		env: {
			...process.env,
			CLAUDE_PROJECT_DIR: projectRoot,
		},
	});

	await child.exited;
	return child.exitCode ?? 0;
}

async function main() {
	const options = parseOptions();
	const runApi = !options.uiOnly;
	const runUi = !options.apiOnly;

	console.log(`[conversion-template-generator] Running template generation for ${options.entity}...`);

	if (runApi) {
		const apiExit = await runAgent("conversion-template-generator-api.ts", options);
		if (apiExit !== 0) process.exit(apiExit);
	}

	if (runUi) {
		const uiExit = await runAgent("conversion-template-generator-ui.ts", options);
		if (uiExit !== 0) process.exit(uiExit);
	}

	console.log("\n✅ Template generation complete.");
	console.log(`- API plan: output/${options.entity}/conversion-plan-api.md`);
	console.log(`- UI plan: output/${options.entity}/conversion-plan-ui.md`);
}

await main();
