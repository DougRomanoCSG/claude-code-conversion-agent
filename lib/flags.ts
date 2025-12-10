/**
 * Command-line argument parsing utilities
 */

import { parseArgs } from "node:util";
import type { ClaudeFlags } from "./claude-flags.types";

export const parsedArgs = parseArgs({
	args: process.argv.slice(2),
	options: {
		print: { type: "boolean", short: "p" },
		interactive: { type: "boolean", short: "i" },
		settings: { type: "string" },
		"mcp-config": { type: "string" },
		"output-format": { type: "string" },
		entity: { type: "string", short: "e" },
		"form-name": { type: "string" },
		"form-type": { type: "string" },
		output: { type: "string", short: "o" },
		"skip-steps": { type: "string" },
	},
	strict: false,
	allowPositionals: true,
});

export function getPositionals(): string[] {
	return parsedArgs.positionals;
}

export function buildClaudeFlags(
	baseFlags: Partial<ClaudeFlags>,
	userFlags: ClaudeFlags,
): string[] {
	const merged = { ...baseFlags, ...userFlags };
	const flags: string[] = [];

	for (const [key, value] of Object.entries(merged)) {
		if (value === undefined || value === null) continue;
		if (key === "entity" || key === "form-name" || key === "form-type" || key === "output" || key === "skip-steps") {
			continue;
		}

		if (typeof value === "boolean") {
			if (value) {
				flags.push(`--${key}`);
			}
		} else {
			flags.push(`--${key}`, String(value));
		}
	}

	return flags;
}
