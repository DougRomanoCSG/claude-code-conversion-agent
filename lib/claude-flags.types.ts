/**
 * Type definitions for Claude CLI flags
 * Based on claude --help output
 */

/**
 * Available output formats for --print mode
 */
export type OutputFormat = "text" | "json" | "stream-json"

/**
 * Available input formats for --print mode
 */
export type InputFormat = "text" | "stream-json"

/**
 * Permission modes for the session
 */
export type PermissionMode = "acceptEdits" | "bypassPermissions" | "default" | "plan"

/**
 * Claude CLI flags configuration
 */
export interface ClaudeFlags {
	/**
	 * Enable debug mode
	 */
	debug?: boolean

	/**
	 * Override verbose mode setting from config
	 */
	verbose?: boolean

	/**
	 * Print response and exit (useful for pipes)
	 */
	print?: boolean

	/**
	 * Output format (only works with --print)
	 * @default "text"
	 */
	'output-format'?: OutputFormat

	/**
	 * Input format (only works with --print)
	 * @default "text"
	 */
	'input-format'?: InputFormat

	/**
	 * Bypass all permission checks
	 */
	'dangerously-skip-permissions'?: boolean

	/**
	 * Comma or space-separated list of tool names to allow
	 * @example "Bash(git:*) Edit" or "Task,Bash,Glob,Grep"
	 */
	allowedTools?: string

	/**
	 * Comma or space-separated list of tool names to deny
	 */
	disallowedTools?: string

	/**
	 * Load MCP servers from a JSON file or string
	 */
	'mcp-config'?: string

	/**
	 * Append a system prompt to the default system prompt
	 */
	'append-system-prompt'?: string

	/**
	 * Permission mode to use for the session
	 */
	'permission-mode'?: PermissionMode

	/**
	 * Continue the most recent conversation
	 */
	continue?: boolean

	/**
	 * Resume a conversation - provide a session ID or interactively select
	 */
	resume?: string | boolean

	/**
	 * Model for the current session
	 */
	model?: string

	/**
	 * Enable automatic fallback to specified model
	 */
	'fallback-model'?: string

	/**
	 * Path to a settings JSON file
	 */
	settings?: string

	/**
	 * Additional directories to allow tool access to
	 */
	'add-dir'?: string | string[]

	/**
	 * Automatically connect to IDE on startup
	 */
	ide?: boolean

	/**
	 * Only use MCP servers from --mcp-config
	 */
	'strict-mcp-config'?: boolean

	/**
	 * Use a specific session ID for the conversation
	 */
	'session-id'?: string

	/**
	 * Prompt to use for the conversation
	 */
	prompt?: string

	// Custom flags for our conversion agents
	entity?: string
	'form-name'?: string
	'form-type'?: "Search" | "Detail"
	output?: string
	'skip-steps'?: string

	// Any other flags
	[key: string]: unknown
}
