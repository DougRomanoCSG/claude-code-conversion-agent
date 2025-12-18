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
		resume: { type: "boolean" },
		"rerun-failed": { type: "boolean" },
		"dry-run": { type: "boolean" },
	},
	strict: false,
	allowPositionals: true,
});

/**
 * Convert flags object to command line arguments
 */
export function toFlags(flagsObj: ClaudeFlags): string[] {
	return Object.entries(flagsObj).flatMap(([key, value]) => {
		if (value === true) return [`--${key}`];
		if (value === false) return [`--no-${key}`];
		if (value !== undefined) return [`--${key}`, String(value)];
		return [];
	});
}

export function getPositionals(): string[] {
	return parsedArgs.positionals;
}

/**
 * Merge default flags with user flags and convert to CLI format
 * User flags override defaults.
 */
export function buildClaudeFlags(
	baseFlags: Partial<ClaudeFlags>,
	userFlags: ClaudeFlags = parsedArgs.values as ClaudeFlags,
): string[] {
	const merged = { ...baseFlags, ...userFlags };

	// Filter out custom agent-specific flags that shouldn't be passed to Claude
	const customFlags = ["entity", "form-name", "form-type", "output", "skip-steps", "resume", "rerun-failed"];
	const claudeFlags: ClaudeFlags = {};

	for (const [key, value] of Object.entries(merged)) {
		if (!customFlags.includes(key)) {
			claudeFlags[key] = value;
		}
	}

	return toFlags(claudeFlags);
}

/**
 * Resolve path relative to a file
 */
export function resolvePath(relativeFromFile: string, importMetaUrl: string): string {
	const url = new URL(relativeFromFile, importMetaUrl);
	return url.pathname;
}
