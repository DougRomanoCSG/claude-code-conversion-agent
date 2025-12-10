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

	const systemPrompt = `
You are a Data Access Pattern Analyzer agent.

TASK: Extract data access patterns for ${entity}.

TARGET FILES:
- List class: ${getListPathForPrompt(entity)}
- Business object CRUD methods

GOALS:
1. Extract stored procedure name and parameters
2. Parse AddFetchParameters for search criteria
3. Extract result column mapping from ReadRow
4. Identify CRUD operations (Insert, Update, Delete, GetByID)
5. Extract data formatting logic
6. Identify child entity operations (if any)

ARCHITECTURE NOTE:
⭐ The target project uses a MONO SHARED structure:
- DTOs and Models are in BargeOps.Shared (${getSharedExamples().dtos})
- Repositories use Dapper with stored procedures
- Results are mapped to DTOs from the shared project

REFERENCE EXAMPLES:
For data access patterns, reference:
- BargeOps.Shared DTOs: ${getSharedExamples().dtos}
  Example: FacilityDto.cs, BoatLocationDto.cs - Target DTOs for query results
- BargeOps.Admin.API Repositories: ${getAdminApiExamples().repositories}
  Primary reference: IFacilityRepository.cs, FacilityRepository.cs - Dapper with stored procedures
  Primary reference: IBoatLocationRepository.cs, BoatLocationRepository.cs - Complete CRUD patterns
- BargeOps.Crewing.API Repositories: ${getCrewingApiExamples().repositories}
  Examples: ICrewingRepository.cs, CrewingRepository.cs - Additional patterns

KEY PATTERNS TO IDENTIFY:
- Search operations: sp_{Entity}Search stored procedure with paging
- GetByID: sp_{Entity}_GetByID
- Insert: sp_{Entity}_Insert
- Update: sp_{Entity}_Update
- Delete: sp_{Entity}_Delete (usually soft delete)
- Child entities: sp_{Entity}{Child}_GetByParentID, _Insert, _Update, _Delete

OUTPUT: ${outputPath}/data-access.json

Begin extraction.
`;

	const baseFlags = {
		settings: settingsJson,
		"mcp-config": mcpJson,
		...(interactive ? {} : { print: true, "output-format": "json" }),
	} as const;

	const flags = buildClaudeFlags({ ...baseFlags }, parsedArgs.values as ClaudeFlags);
	const child = spawn(["claude", ...flags, systemPrompt], {
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
