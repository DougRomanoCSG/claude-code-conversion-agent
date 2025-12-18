#!/usr/bin/env -S bun run
/**
 * SECURITY & AUTHORIZATION EXTRACTOR: Extract permissions and authorization
 */

import { spawn } from "bun";
import { buildClaudeFlags, parsedArgs } from "../lib/flags";
import type { ClaudeFlags } from "../lib/claude-flags.types";
import securitySettings from "../settings/security.settings.json" with { type: "json" };
import securityMcp from "../settings/security.mcp.json" with { type: "json" };
import securityExtractorPrompt from "../system-prompts/security-extractor-prompt.md" with { type: "text" };

function resolvePath(relativeFromThisFile: string): string {
	const url = new URL(relativeFromThisFile, import.meta.url);
	return url.pathname;
}

const projectRoot = resolvePath("../");
const settingsJson = JSON.stringify(securitySettings);
const mcpJson = JSON.stringify(securityMcp);

async function main() {
	const entity = parsedArgs.values.entity as string;
	const interactive = parsedArgs.values.interactive === true;
	const outputDir = parsedArgs.values.output as string;

	if (!entity) {
		console.error("Error: --entity parameter is required");
		process.exit(1);
	}

	const outputPath = outputDir || `${projectRoot}output/${entity}`;

	// Build context-specific prompt with entity details and paths
	const contextPrompt = `
TASK: Extract security patterns for ${entity}.

EXTRACTION GOALS:
1. Extract SubSystem identifier from InitializeBase
2. Parse SetButtonTypes for button security
3. Extract ControlAuthorization.SetButtonType calls
4. Identify permission requirements
5. Map button types to modern permission attributes
6. Document API authentication (ApiKey) and UI authentication (OIDC)

OUTPUT:
Generate a JSON file at: ${outputPath}/security.json

MODERN AUTHENTICATION PATTERNS:
- API: Use [ApiKey] attribute (NOT Windows Auth)
- UI: Use [Authorize] attribute with OIDC (prod) or DevelopmentAutoSignInMiddleware (dev)
- Permissions: Define in Enums/AuthPermissions.cs

Begin extraction.
`;

	const baseFlags = {
		"append-system-prompt": securityExtractorPrompt,
		settings: settingsJson,
		"mcp-config": mcpJson,
		...(interactive ? {} : { print: true, "output-format": "json" }),
	} as const;

	const flags = buildClaudeFlags({ ...baseFlags }, parsedArgs.values as ClaudeFlags);
	const child = spawn(["claude", ...flags, contextPrompt], {
		stdin: "inherit",
		stdout: interactive ? "inherit" : "pipe",
		stderr: "inherit",
		env: { ...process.env, CLAUDE_PROJECT_DIR: projectRoot },
	});

	const onExit = () => { try { child.kill("SIGTERM"); } catch {} };
	process.on("SIGINT", onExit);
	process.on("SIGTERM", onExit);

	await child.exited;
	process.exit(child.exitCode ?? 0);
}

await main();
