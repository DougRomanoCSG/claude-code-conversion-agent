#!/usr/bin/env -S bun run
/**
 * CONVERSION TEMPLATE GENERATOR (API/SHARED): Generate backend templates (ALWAYS INTERACTIVE)
 *
 * Usage:
 *   bun run agents/conversion-template-generator-api.ts --entity "Facility"
 */

import { spawn } from "bun";
import { existsSync } from "fs";
import { dirname, join } from "path";
import { fileURLToPath } from "url";
import { buildClaudeFlags, parsedArgs } from "../lib/flags";
import type { ClaudeFlags } from "../lib/claude-flags.types";
import {
	getAdminApiPath,
	getCrewingApiPath,
	getDetailedReferenceExamples,
	getProjectRoot,
	getSharedProjectPath,
} from "../lib/paths";
import templateGenSettings from "../settings/template-generator.settings.json" with { type: "json" };
import apiSystemPromptBase from "../system-prompts/conversion-template-generator-api-prompt.md" with { type: "text" };

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const projectRoot = getProjectRoot(import.meta.url);
const settingsJson = JSON.stringify(templateGenSettings);
const mcpConfigPath = join(__dirname, "..", "settings", "template-generator.mcp.json");

interface GeneratorOptions {
	entity: string;
	outputDir?: string;
	formName?: string;
}

function parseOptions(): GeneratorOptions {
	const entity = parsedArgs.values.entity as string;
	const outputDir = parsedArgs.values.output as string;
	const formName = parsedArgs.values["form-name"] as string;

	if (!entity) {
		console.error("Error: --entity parameter is required");
		process.exit(1);
	}

	return { entity, outputDir, formName };
}

function getMissingFiles(outputPath: string, files: string[]): string[] {
	return files.filter((f) => !existsSync(join(outputPath, f)));
}

function getSkipStepsFromExistingFiles(outputPath: string, filesInStepOrder: string[]): number[] {
	const skipSteps: number[] = [];
	for (let i = 0; i < filesInStepOrder.length; i++) {
		const file = filesInStepOrder[i];
		if (existsSync(join(outputPath, file))) {
			// Step numbers are 1-based; file order matches orchestrator steps 1-10
			skipSteps.push(i + 1);
		}
	}
	return skipSteps;
}

function getAnalysisFilesForMode(mode: "search-detail" | "single-form"): string[] {
	if (mode === "single-form") {
		return [
			"form-structure.json",
			"business-logic.json",
			"data-access.json",
			"security.json",
			"ui-mapping.json",
			"workflow.json",
			"tabs.json",
			"validation.json",
			"related-entities.json",
		];
	}

	return [
		"form-structure-search.json",
		"form-structure-detail.json",
		"business-logic.json",
		"data-access.json",
		"security.json",
		"ui-mapping.json",
		"workflow.json",
		"tabs.json",
		"validation.json",
		"related-entities.json",
	];
}

function detectAnalysisMode(outputPath: string, options: GeneratorOptions): "search-detail" | "single-form" {
	// If user explicitly provides a form name, treat non-Search/Detail as a single-form workflow.
	if (options.formName) {
		const isSearchOrDetail = /^(frm)?\w+(Search|Detail)$/i.test(options.formName);
		if (!isSearchOrDetail) return "single-form";
	}

	// Infer from output files already present.
	if (existsSync(join(outputPath, "form-structure.json"))) return "single-form";
	if (
		existsSync(join(outputPath, "form-structure-search.json")) ||
		existsSync(join(outputPath, "form-structure-detail.json"))
	) {
		return "search-detail";
	}

	// Default to search/detail pairs.
	return "search-detail";
}

async function ensureAnalysisOutputsExist(
	options: GeneratorOptions,
	outputPath: string,
	analysisFiles: string[],
	mode: "search-detail" | "single-form",
): Promise<void> {
	const missingBefore = getMissingFiles(outputPath, analysisFiles);
	if (missingBefore.length === 0) {
		return;
	}

	console.log(`
╔════════════════════════════════════════════════════════════════════════════╗
║                    TEMPLATE GENERATION PRE-FLIGHT CHECK                    ║
║                                                                            ║
║  Entity: ${options.entity.padEnd(68, " ")}║
║  Output: ${outputPath.padEnd(67, " ")}║
║                                                                            ║
║  Missing analysis files detected. Running orchestrator to (re)generate     ║
║  missing analysis outputs before starting interactive template generation. ║
╚════════════════════════════════════════════════════════════════════════════╝
`);

	console.log("Missing analysis files:");
	missingBefore.forEach((f) => console.log(`  - ${f}`));
	console.log();

	const existingSkipSteps = getSkipStepsFromExistingFiles(outputPath, analysisFiles);
	const orchestratorPath = `${projectRoot}agents/orchestrator.ts`;

	const args = ["run", orchestratorPath, "--entity", options.entity, "--output", outputPath];

	// For single-form entities, the orchestrator needs a form name to avoid assuming frm{Entity}Search/Detail.
	// Prefer user-provided form name, otherwise fall back to frm{Entity}.
	if (mode === "single-form") {
		const formName = options.formName || `frm${options.entity}`;
		args.push("--form-name", formName);
	}

	if (existingSkipSteps.length > 0) {
		args.push("--skip-steps", existingSkipSteps.sort((a, b) => a - b).join(","));
	}

	console.log(`Running: bun ${args.join(" ")}`);
	console.log();

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
	const exitCode = child.exitCode ?? 0;
	if (exitCode !== 0) {
		console.error(`\n❌ Orchestrator failed (exit ${exitCode}). Aborting template generation.\n`);
		process.exit(exitCode);
	}

	const missingAfter = getMissingFiles(outputPath, analysisFiles);
	if (missingAfter.length > 0) {
		console.error("\n❌ Analysis outputs are still missing after orchestrator run. Aborting template generation.");
		missingAfter.forEach((f) => console.error(`  - ${f}`));
		console.error(`\nTry running a full analysis:\n  bun run agents/orchestrator.ts --entity "${options.entity}"\n`);
		process.exit(1);
	}
}

async function runTemplateGenerator(options: GeneratorOptions): Promise<number> {
	const outputPath = options.outputDir || `${projectRoot}output/${options.entity}`;

	const mode = detectAnalysisMode(outputPath, options);
	const analysisFiles = getAnalysisFilesForMode(mode);

	// Ensure analysis outputs (steps 1-10) exist before starting template generation.
	await ensureAnalysisOutputsExist(options, outputPath, analysisFiles, mode);

	const systemPrompt = `
${apiSystemPromptBase}

TASK: Generate API + Shared conversion templates for ${options.entity}.

INPUT DATA:
You have access to analysis files in: ${outputPath}/
${analysisFiles.map((f) => `- ${f}`).join("\n")}

CREWING API REFERENCE:
- Crewing API project: ${getCrewingApiPath()}
- Review CREWING_API_PATTERN_ANALYSIS.md for the correct service + repository patterns.

GENERATION GOALS:
1. Generate Shared DTOs FIRST (BargeOps.Shared)
2. Generate API repositories, services, and controllers using Crewing patterns
3. Use DbHelper (concrete) and SqlText patterns for repositories
4. Use ApiControllerBase + IUnitOfWork + API Services (hybrid pattern)
5. Output plan in conversion-plan-api.md

OUTPUT STRUCTURE:
Primary file: ${outputPath}/conversion-plan-api.md

Templates:
${outputPath}/templates/
├── shared/
│   └── Dto/
│       ├── {Entity}Dto.cs
│       ├── {Entity}SearchRequest.cs
│       └── {Child}Dto.cs
└── api/
    ├── Controllers/
    │   └── {Entity}Controller.cs
    ├── Repositories/
    │   ├── I{Entity}Repository.cs
    │   └── {Entity}Repository.cs
    ├── Services/
    │   ├── I{Entity}Service.cs
    │   └── {Entity}Service.cs
    └── Sql/
        └── {Entity}/
            ├── Create{Entity}.sql
            ├── Update{Entity}.sql
            ├── Get{Entity}ById.sql
            └── Get{Entity}List.sql

${getDetailedReferenceExamples()}

TARGET PROJECTS:
⭐ Shared: ${getSharedProjectPath()}
⭐ API: ${getAdminApiPath()}

IMPORTANT:
- Do NOT generate UI templates in this run.
- Follow Crewing.API patterns for controller/service/repository structure.
- Place repository interfaces under templates/api/Repositories (deploy script routes to Abstractions).

Begin template generation now.
`;

	const baseFlags = {
		settings: settingsJson,
		"mcp-config": mcpConfigPath,
		"append-system-prompt": systemPrompt,
	} as const;

	const flags = buildClaudeFlags({ ...baseFlags }, parsedArgs.values as ClaudeFlags);

	const initialPrompt = `Generate API + Shared conversion templates for ${options.entity} using ${outputPath}. Output conversion-plan-api.md and templates/shared + templates/api.`;
	const args = [...flags, initialPrompt];

	console.log(`
╔════════════════════════════════════════════════════════════════════════════╗
║            CONVERSION TEMPLATE GENERATOR (API/Shared - Interactive)         ║
║                                                                            ║
║  Entity: ${options.entity.padEnd(68, " ")}║
║  Output: ${outputPath.padEnd(67, " ")}║
╚════════════════════════════════════════════════════════════════════════════╝
	`);

	const child = spawn(["claude", ...args], {
		stdin: "inherit",
		stdout: "inherit",
		stderr: "inherit",
		env: {
			...process.env,
			CLAUDE_PROJECT_DIR: projectRoot,
			ENTITY_NAME: options.entity,
			OUTPUT_PATH: outputPath,
		},
	});

	const onExit = () => {
		try {
			child.kill("SIGTERM");
		} catch {}
	};

	process.on("SIGINT", onExit);
	process.on("SIGTERM", onExit);

	await child.exited;
	return child.exitCode ?? 0;
}

async function main() {
	const options = parseOptions();
	console.log(`[conversion-template-generator-api] Launching interactive template generator for ${options.entity}...`);

	const code = await runTemplateGenerator(options);
	process.exit(code);
}

await main();
