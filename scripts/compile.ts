#!/usr/bin/env bun
import { $ } from "bun";
import { basename } from "path";

const input = process.argv[2];
if (!input) {
  console.error("Usage: bun compile <typescript-file>");
  console.error("Example: bun compile agents/ui-component-mapper.ts");
  process.exit(1);
}

const outputName = basename(input, ".ts");
const outputPath = `./bin/${outputName}`;

// Generate static asset maps so Bun can inline all assets
console.log("Generating asset maps...");
await $`bun scripts/gen-assets.ts`;

// Build a single-file binary
console.log(`Compiling ${outputName}...`);
await $`bun build --compile ${input} --outfile ${outputPath}`;

console.log(`✓ Compiled ${outputName} to ${outputPath}`);
