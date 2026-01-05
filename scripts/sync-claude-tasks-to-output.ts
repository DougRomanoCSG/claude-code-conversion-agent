#!/usr/bin/env -S bun run
/**
 * Sync analysis JSON files that Claude sometimes writes to `.claude/tasks/`
 * into the canonical `output/<Entity>/` folder with the filenames expected by
 * the orchestrator + template generator.
 *
 * Usage:
 *   bun run scripts/sync-claude-tasks-to-output.ts
 */

import { promises as fs } from "node:fs";
import path from "node:path";

async function exists(p: string): Promise<boolean> {
	try {
		await fs.access(p);
		return true;
	} catch {
		return false;
	}
}

async function copyIfMissing(src: string, dest: string): Promise<boolean> {
	if (!(await exists(src))) return false;
	if (await exists(dest)) return false;
	await fs.mkdir(path.dirname(dest), { recursive: true });
	await fs.copyFile(src, dest);
	return true;
}

function taskFileCandidates(entity: string, desiredOutputFile: string): string[] {
	// Map expected output filename -> potential `.claude/tasks` filenames.
	// These names reflect what we see Claude producing in practice.
	const base = entity;

	switch (desiredOutputFile) {
		case "business-logic.json":
			return [`${base}_business_logic.json`];
		case "data-access.json":
			return [`${base}_data_access.json`];
		case "security.json":
			return [`${base}_security.json`];
		case "ui-mapping.json":
			return [`${base}_ui_mapping.json`];
		case "tabs.json":
			return [`${base}_tabs.json`];
		case "related-entities.json":
			return [`${base}_relationships.json`, `${base}_related_entities.json`];
		default:
			return [];
	}
}

async function main() {
	const projectRoot = path.resolve(import.meta.dir, "..");
	const outputRoot = path.join(projectRoot, "output");
	const tasksRoot = path.join(projectRoot, ".claude", "tasks");

	const auditPath = path.join(outputRoot, "_audit-output.json");
	if (!(await exists(auditPath))) {
		console.error(`Missing audit file: ${path.relative(projectRoot, auditPath)}`);
		console.error(`Run: bun run scripts/audit-output.ts`);
		process.exit(1);
	}

	const auditRaw = await fs.readFile(auditPath, "utf8");
	const auditJson = JSON.parse(auditRaw) as {
		audits: Array<{ folder: string; entity: string; missingForTemplateGen: string[] }>;
	};

	let copied = 0;
	let attempted = 0;

	for (const a of auditJson.audits) {
		if (!a.missingForTemplateGen?.length) continue;

		for (const missingFile of a.missingForTemplateGen) {
			const candidates = taskFileCandidates(a.entity, missingFile);
			if (!candidates.length) continue;

			for (const candidate of candidates) {
				attempted++;
				const src = path.join(tasksRoot, candidate);
				const dest = path.join(outputRoot, a.folder, missingFile);
				if (await copyIfMissing(src, dest)) {
					copied++;
					console.log(`✓ Copied ${path.relative(projectRoot, src)} → ${path.relative(projectRoot, dest)}`);
					break;
				}
			}
		}
	}

	console.log(`\nSync complete. Files copied: ${copied}. Candidates checked: ${attempted}.\n`);
}

await main();





