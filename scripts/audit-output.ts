#!/usr/bin/env -S bun run
/**
 * Audit conversion analysis outputs in /output to find:
 * - Failed / pending steps
 * - Step output files referenced in conversion-status.json that are missing on disk
 * - Missing "required" analysis files for template generation modes
 *
 * Usage:
 *   bun run scripts/audit-output.ts
 */

import { promises as fs } from "node:fs";
import path from "node:path";

type StepStatus = {
	stepNumber: number;
	name: string;
	status: "pending" | "running" | "completed" | "failed" | "skipped";
	outputFile?: string;
};

type ConversionStatus = {
	entity: string;
	formName?: string;
	overallStatus?: "running" | "completed" | "failed";
	totalSteps?: number;
	completedSteps?: number;
	skippedSteps?: number;
	failedSteps?: number;
	steps?: StepStatus[];
};

type EntityAudit = {
	folder: string;
	entity: string;
	formName?: string;
	overallStatus?: string;
	hasTemplatesFolder: boolean;
	problemSteps: StepStatus[];
	missingStepOutputs: Array<{ stepNumber: number; outputFile: string; stepName: string; status: string }>;
	missingForTemplateGen: string[];
};

async function exists(p: string): Promise<boolean> {
	try {
		await fs.access(p);
		return true;
	} catch {
		return false;
	}
}

async function readJson<T>(p: string): Promise<T> {
	const raw = await fs.readFile(p, "utf8");
	return JSON.parse(raw) as T;
}

async function findConversionStatuses(outputRoot: string): Promise<Array<{ folder: string; statusPath: string }>> {
	const entries = await fs.readdir(outputRoot, { withFileTypes: true });
	const results: Array<{ folder: string; statusPath: string }> = [];

	for (const entry of entries) {
		if (!entry.isDirectory()) continue;
		const folder = entry.name;
		const statusPath = path.join(outputRoot, folder, "conversion-status.json");
		if (await exists(statusPath)) results.push({ folder, statusPath });
	}

	return results.sort((a, b) => a.folder.localeCompare(b.folder));
}

async function inferTemplateMode(outputFolderPath: string, status: ConversionStatus): Promise<"single-form" | "search-detail"> {
	// Match conversion-template-generator.ts behavior:
	// - If form-structure.json exists => single-form
	// - Else if search/detail exists => search-detail
	// - Else default => search-detail
	if (await exists(path.join(outputFolderPath, "form-structure.json"))) return "single-form";
	if (
		(await exists(path.join(outputFolderPath, "form-structure-search.json"))) ||
		(await exists(path.join(outputFolderPath, "form-structure-detail.json")))
	) {
		return "search-detail";
	}

	// Default to search/detail pairs.
	return "search-detail";
}

function requiredFilesForMode(mode: "single-form" | "search-detail"): string[] {
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

async function auditEntity(outputRoot: string, folder: string, statusPath: string): Promise<EntityAudit> {
	const outputFolderPath = path.join(outputRoot, folder);
	const status = await readJson<ConversionStatus>(statusPath);

	const hasTemplatesFolder = await exists(path.join(outputFolderPath, "templates"));

	const steps = status.steps ?? [];
	const problemSteps = steps.filter((s) => s.status === "failed" || s.status === "pending");

	const missingStepOutputs: EntityAudit["missingStepOutputs"] = [];
	for (const s of steps) {
		if (!s.outputFile) continue;
		// Skipped steps are allowed to have no output file on disk.
		if (s.status === "skipped") continue;
		const p = path.join(outputFolderPath, s.outputFile);
		if (!(await exists(p))) {
			missingStepOutputs.push({
				stepNumber: s.stepNumber,
				outputFile: s.outputFile,
				stepName: s.name,
				status: s.status,
			});
		}
	}

	const mode = await inferTemplateMode(outputFolderPath, status);
	const required = requiredFilesForMode(mode);
	const missingForTemplateGen: string[] = [];
	for (const f of required) {
		if (!(await exists(path.join(outputFolderPath, f)))) missingForTemplateGen.push(f);
	}

	return {
		folder,
		entity: status.entity,
		formName: status.formName,
		overallStatus: status.overallStatus,
		hasTemplatesFolder,
		problemSteps,
		missingStepOutputs,
		missingForTemplateGen,
	};
}

function printReport(audits: EntityAudit[]): void {
	const needsAttention = audits.filter(
		(a) => a.problemSteps.length > 0 || a.missingStepOutputs.length > 0 || a.missingForTemplateGen.length > 0,
	);

	console.log("\n=== Output Audit Summary ===\n");
	console.log(`Entities scanned: ${audits.length}`);
	console.log(`Entities needing attention: ${needsAttention.length}\n`);

	if (needsAttention.length === 0) {
		console.log("All entities have complete analysis outputs for their inferred template-generation mode.");
		return;
	}

	for (const a of needsAttention) {
		console.log(`- ${a.folder} (entity: ${a.entity}${a.formName ? `, form: ${a.formName}` : ""})`);
		if (a.problemSteps.length) {
			console.log("  - problem steps:");
			for (const s of a.problemSteps) {
				console.log(`    - step ${s.stepNumber}: ${s.status} (${s.name})${s.outputFile ? ` → ${s.outputFile}` : ""}`);
			}
		}
		if (a.missingStepOutputs.length) {
			console.log("  - missing step output files referenced by conversion-status.json:");
			for (const m of a.missingStepOutputs) {
				console.log(`    - step ${m.stepNumber}: ${m.stepName} (${m.status}) → missing ${m.outputFile}`);
			}
		}
		if (a.missingForTemplateGen.length) {
			console.log("  - missing files required for template generation:");
			for (const f of a.missingForTemplateGen) console.log(`    - ${f}`);
		}
		console.log("");
	}
}

async function main() {
	const projectRoot = path.resolve(import.meta.dir, "..");
	const outputRoot = path.join(projectRoot, "output");

	const statuses = await findConversionStatuses(outputRoot);
	const audits: EntityAudit[] = [];
	for (const s of statuses) {
		audits.push(await auditEntity(outputRoot, s.folder, s.statusPath));
	}

	printReport(audits);

	// Also write machine-readable output for follow-up tooling.
	const outPath = path.join(outputRoot, "_audit-output.json");
	await fs.writeFile(outPath, JSON.stringify({ generatedAt: new Date().toISOString(), audits }, null, 2), "utf8");
	console.log(`Wrote: ${path.relative(projectRoot, outPath)}\n`);
}

await main();


