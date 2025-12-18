#!/usr/bin/env -S bun run
/**
 * DATA ACCESS PATTERN ANALYZER: Extract stored procedures and queries
 */

import { spawn } from "bun";
import { buildClaudeFlags, parsedArgs } from "../lib/flags";
import type { ClaudeFlags } from "../lib/claude-flags.types";
import { getProjectRoot, getListPathForPrompt, getAdminApiExamples, getCrewingApiExamples, getSharedExamples } from "../lib/paths";
import dataAccessSettings from "../settings/data-access.settings.json" with { type: "json" };
import dataAccessMcp from "../settings/data-access.mcp.json" with { type: "json" };
import dataAccessAnalyzerPrompt from "../system-prompts/data-access-analyzer-prompt.md" with { type: "text" };

const projectRoot = getProjectRoot(import.meta.url);
const settingsJson = JSON.stringify(dataAccessSettings);
const mcpJson = JSON.stringify(dataAccessMcp);

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
TASK: Extract data access patterns for ${entity}.

TARGET FILES:
- List class: ${getListPathForPrompt(entity)}
- Business object CRUD methods

OUTPUT:
Generate a JSON file at: ${outputPath}/data-access.json

ARCHITECTURE REFERENCES:
- Shared DTOs: ${getSharedExamples().dtos}
  Example: FacilityDto.cs, BoatLocationDto.cs - Target DTOs for query results
- Admin API Repositories: ${getAdminApiExamples().repositories}
  Primary reference: IFacilityRepository.cs, FacilityRepository.cs - Dapper with SQL files
  Primary reference: IBoatLocationRepository.cs, BoatLocationRepository.cs - Complete CRUD patterns
- Crewing API Repositories: ${getCrewingApiExamples().repositories}
  Examples: ICrewingRepository.cs, CrewingRepository.cs - Additional Dapper patterns

KEY PATTERNS TO IDENTIFY:
Extract stored procedure patterns from legacy VB.NET code, documenting that they will be converted to embedded SQL files:
- Search operations: sp_{Entity}Search → {Entity}_Search.sql
- GetByID: sp_{Entity}_GetByID → {Entity}_GetById.sql
- Insert: sp_{Entity}_Insert → {Entity}_Insert.sql
- Update: sp_{Entity}_Update → {Entity}_Update.sql
- Delete: sp_{Entity}_Delete → {Entity}_SetActive.sql (soft delete with IsActive)
- Child entities: sp_{Entity}{Child}_GetByParentID → {Entity}_GetRelated.sql

Begin extraction.
`;

	const baseFlags = {
		"append-system-prompt": dataAccessAnalyzerPrompt,
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
