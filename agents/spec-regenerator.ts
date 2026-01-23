#!/usr/bin/env bun

/**
 * Spec Regenerator Agent
 * 
 * Regenerates SpecKit specs from existing analysis data.
 * Use this when you've updated spec-generator.ts and want to regenerate
 * specs without re-running all the analysis steps.
 * 
 * Usage:
 *   bun run agents/spec-regenerator.ts --entity "Facility"
 *   bun run agents/spec-regenerator.ts --entity "Vendor"
 */

import { existsSync, readFileSync, readdirSync } from "node:fs";
import { join } from "node:path";
import { generateSpec } from "../lib/spec-generator";

interface AnalysisData {
	entity: string;
	formStructureSearch?: any;
	formStructureDetail?: any;
	businessLogic?: any;
	dataAccess?: any;
	security?: any;
	validation?: any;
	relatedEntities?: any;
	tabs?: any;
	uiMapping?: any;
	workflow?: any;
}

async function main() {
	const args = process.argv.slice(2);
	const entityArg = args.find((arg) => arg.startsWith("--entity="));
	const entity = entityArg ? entityArg.split("=")[1] : args.find((arg) => !arg.startsWith("--"));

	if (!entity) {
		console.error("❌ Error: Entity name is required");
		console.error("\nUsage:");
		console.error('  bun run agents/spec-regenerator.ts --entity "Facility"');
		console.error('  bun run agents/spec-regenerator.ts --entity "Vendor"');
		process.exit(1);
	}

	console.log(`\n🔄 Regenerating SpecKit spec for: ${entity}\n`);

	// Load config
	const configPath = join(process.cwd(), "config.json");
	if (!existsSync(configPath)) {
		console.error("❌ Error: config.json not found");
		process.exit(1);
	}
	const config = JSON.parse(readFileSync(configPath, "utf-8"));
	const monorepoPath = config.targetProjects?.monorepo || config.targetProjects?.adminApi?.replace("/src/BargeOps.API", "");

	if (!monorepoPath) {
		console.error("❌ Error: monorepo path not found in config.json");
		process.exit(1);
	}

	// Determine output directory
	const outputRoot = join(process.cwd(), "output", entity);

	if (!existsSync(outputRoot)) {
		console.error(`❌ Error: Output directory not found: ${outputRoot}`);
		console.error(`   Run the orchestrator first to generate analysis data.`);
		process.exit(1);
	}

	// Build analysis data from existing JSON files
	const analysisData: AnalysisData = {
		entity,
	};

	// Determine form names
	const searchFormName = `frm${entity}Search`;
	const detailFormName = `frm${entity}Detail`;

	// Try to find analysis files (support multiple naming patterns)
	const possibleFiles = [
		// Pattern 1: entity-specific files in root
		{ key: "businessLogic", path: join(outputRoot, "business-logic.json") },
		{ key: "tabs", path: join(outputRoot, "tabs.json") },
		{ key: "relatedEntities", path: join(outputRoot, "related-entities.json") },
		{ key: "validation", path: join(outputRoot, "validation.json") },
		{ key: "security", path: join(outputRoot, "security.json") },
		{ key: "dataAccess", path: join(outputRoot, "data-access.json") },
		{ key: "formStructureSearch", path: join(outputRoot, "form-structure-search.json") },
		{ key: "formStructureDetail", path: join(outputRoot, "form-structure-detail.json") },
		{ key: "uiMapping", path: join(outputRoot, "ui-mapping.json") },
		{ key: "workflow", path: join(outputRoot, "workflow.json") },
		// Pattern 2: form-specific files in root
		{ key: "businessLogic", path: join(outputRoot, `business-logic.${searchFormName}.json`) },
		{ key: "tabs", path: join(outputRoot, `tabs.${detailFormName}.json`) },
		{ key: "validation", path: join(outputRoot, `validation.${searchFormName}.json`) },
		{ key: "security", path: join(outputRoot, `security.${searchFormName}.json`) },
		{ key: "dataAccess", path: join(outputRoot, `data-access.${searchFormName}.json`) },
		{ key: "formStructureSearch", path: join(outputRoot, `form-structure-search.${searchFormName}.json`) },
		{ key: "formStructureDetail", path: join(outputRoot, `form-structure-detail.${detailFormName}.json`) },
		{ key: "uiMapping", path: join(outputRoot, `ui-mapping.${searchFormName}.json`) },
		{ key: "workflow", path: join(outputRoot, `workflow.${searchFormName}.json`) },
		{ key: "relatedEntities", path: join(outputRoot, `related-entities.${searchFormName}.json`) },
		// Pattern 3: files in subdirectories (Facility pattern)
		{ key: "businessLogic", path: join(outputRoot, searchFormName, `business-logic.${searchFormName}.json`) },
		{ key: "tabs", path: join(outputRoot, detailFormName, `tabs.${detailFormName}.json`) },
		{ key: "validation", path: join(outputRoot, searchFormName, `validation.${searchFormName}.json`) },
		{ key: "security", path: join(outputRoot, searchFormName, `security.${searchFormName}.json`) },
		{ key: "dataAccess", path: join(outputRoot, searchFormName, `data-access.${searchFormName}.json`) },
		{ key: "formStructureSearch", path: join(outputRoot, searchFormName, `form-structure-search.${searchFormName}.json`) },
		{ key: "formStructureDetail", path: join(outputRoot, detailFormName, `form-structure-detail.${detailFormName}.json`) },
		{ key: "uiMapping", path: join(outputRoot, searchFormName, `ui-mapping.${searchFormName}.json`) },
		{ key: "workflow", path: join(outputRoot, searchFormName, `workflow.${searchFormName}.json`) },
		{ key: "relatedEntities", path: join(outputRoot, searchFormName, `related-entities.${searchFormName}.json`) },
	];

	for (const file of possibleFiles) {
		if (existsSync(file.path)) {
			try {
				const content = JSON.parse(readFileSync(file.path, "utf-8"));
				// Only load if we don't already have this data (first match wins)
				if (!(analysisData as any)[file.key]) {
					(analysisData as any)[file.key] = content;
					console.log(`✓ Loaded: ${file.path}`);
				}
			} catch (error: any) {
				console.warn(`⚠ Warning: Could not parse ${file.path}: ${error.message}`);
			}
		}
	}

	// Check if we have minimum required data
	if (!analysisData.businessLogic && !analysisData.formStructureSearch) {
		console.error(`\n❌ Error: No analysis data found in ${outputRoot}`);
		console.error(`   Expected files like: business-logic.json, form-structure-search.json`);
		console.error(`   Run the orchestrator first to generate analysis data.`);
		process.exit(1);
	}

	// Load all markdown files from output directory
	const additionalMarkdownFiles: Array<{ fileName: string; content: string }> = [];
	const masterPlanPatterns = [
		new RegExp(`${entity}_CONVERSION_MASTER_PLAN\\.md$`, "i"),
		new RegExp(`${entity.toUpperCase()}_CONVERSION_MASTER_PLAN\\.md$`, "i"),
		new RegExp(`${entity}_Conversion_Master_Plan\\.md$`, "i"),
		new RegExp(`${entity}ConversionMasterPlan\\.md$`, "i"),
	];

	if (existsSync(outputRoot)) {
		const files = readdirSync(outputRoot);
		for (const file of files) {
			if (file.endsWith(".md")) {
				// Skip master plan files (they're handled separately by spec-generator)
				const isMasterPlan = masterPlanPatterns.some(pattern => pattern.test(file));
				if (isMasterPlan) {
					continue;
				}

				const filePath = join(outputRoot, file);
				try {
					const content = readFileSync(filePath, "utf-8");
					additionalMarkdownFiles.push({ fileName: file, content });
					console.log(`✓ Loaded markdown: ${file}`);
				} catch (error: any) {
					console.warn(`⚠ Warning: Could not read markdown file ${file}: ${error.message}`);
				}
			}
		}
	}

	// Generate spec
	try {
		await generateSpec(analysisData, {
			outputPath: `${monorepoPath}/.speckit/entities/${entity}`,
			monorepoPath,
			additionalMarkdownFiles,
		});

		console.log(`\n✅ SpecKit spec regenerated successfully!`);
		console.log(`   Location: ${monorepoPath}/.speckit/entities/${entity}/`);
		console.log(`\nNext steps:`);
		console.log(`   1. Review spec: ${monorepoPath}/.speckit/entities/${entity}/spec.md`);
		console.log(`   2. Review tasks: ${monorepoPath}/.speckit/entities/${entity}/tasks/`);
		console.log(`   3. Review checklist: ${monorepoPath}/.speckit/entities/${entity}/quality-checklist.md\n`);
	} catch (error: any) {
		console.error(`\n❌ SpecKit spec generation failed: ${error.message}`);
		console.error(error.stack);
		process.exit(1);
	}
}

main().catch((error) => {
	console.error("❌ Fatal error:", error);
	process.exit(1);
});
