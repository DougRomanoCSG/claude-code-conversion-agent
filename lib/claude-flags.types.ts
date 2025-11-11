/**
 * TypeScript type definitions for Claude CLI flags
 */

export interface ClaudeFlags {
	// Mode flags
	print?: boolean;
	interactive?: boolean;

	// Configuration flags
	settings?: string;
	"mcp-config"?: string;
	"output-format"?: "json" | "text";

	// Custom flags for our agents
	entity?: string;
	"form-type"?: "Search" | "Detail";
	output?: string;
	"skip-steps"?: string;

	// Any other flags
	[key: string]: unknown;
}
