#!/usr/bin/env bun
/**
 * Compile all TypeScript agents to JavaScript binaries
 * 
 * This script compiles all agents from agents/ to bin/ using Bun's build system.
 */

import { build } from "bun";
import { mkdir } from "fs/promises";
import { existsSync } from "fs";
import { join, dirname } from "path";
import { fileURLToPath } from "url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const projectRoot = join(__dirname, "..");
const agentsDir = join(projectRoot, "agents");
const binDir = join(projectRoot, "bin");

// List of all agent files to compile
const agents = [
	"orchestrator.ts",
	"form-structure-analyzer.ts",
	"business-logic-extractor.ts",
	"data-access-analyzer.ts",
	"security-extractor.ts",
	"ui-component-mapper.ts",
	"form-workflow-analyzer.ts",
	"detail-tab-analyzer.ts",
	"validation-extractor.ts",
	"related-entity-analyzer.ts",
	"conversion-template-generator.ts",
	"entity-converter.ts",
	"viewmodel-creator.ts",
	"conversion-planner.ts",
];

async function compileAgent(agentFile: string): Promise<boolean> {
	const inputPath = join(agentsDir, agentFile);
	const outputName = agentFile.replace(/\.ts$/, ".js");
	const outputPath = join(binDir, outputName);

	console.log(`Compiling ${agentFile}...`);

	try {
		const result = await build({
			entrypoints: [inputPath],
			outdir: binDir,
			target: "bun",
			format: "esm",
			minify: false,
			sourcemap: "external",
		});

		if (result.success) {
			console.log(`✅ ${outputName}`);
			return true;
		} else {
			console.error(`❌ Failed to compile ${agentFile}`);
			if (result.logs) {
				for (const log of result.logs) {
					console.error(log);
				}
			}
			return false;
		}
	} catch (error) {
		console.error(`❌ Error compiling ${agentFile}:`, error);
		return false;
	}
}

async function main() {
	console.log("🔨 Compiling agents...\n");

	// Ensure bin directory exists
	if (!existsSync(binDir)) {
		await mkdir(binDir, { recursive: true });
		console.log(`Created ${binDir}\n`);
	}

	let successCount = 0;
	let failCount = 0;

	for (const agent of agents) {
		const success = await compileAgent(agent);
		if (success) {
			successCount++;
		} else {
			failCount++;
		}
	}

	console.log(`\n📊 Compilation Summary:`);
	console.log(`   ✅ Success: ${successCount}`);
	console.log(`   ❌ Failed: ${failCount}`);
	console.log(`   📁 Output: ${binDir}\n`);

	if (failCount > 0) {
		console.error("⚠️  Some agents failed to compile!");
		process.exit(1);
	} else {
		console.log("✨ All agents compiled successfully!");
		process.exit(0);
	}
}

await main();

