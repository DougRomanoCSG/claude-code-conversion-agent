#!/usr/bin/env -S bun run
/**
 * Generates a top-down master conversion plan for Barge screens.
 *
 * Outputs:
 * - output/Barge/MASTER_BARGE_SCREENS.md
 * - output/Barge/master-barge-index.json
 */
import { promises as fs } from "node:fs";
import path from "node:path";

type EntityStatus = {
	entity: string;
	overallStatus?: string;
	totalSteps?: number;
	completedSteps?: number;
	skippedSteps?: number;
	failedSteps?: number;
	startTime?: string;
	endTime?: string;
	steps?: Array<{ stepNumber: number; name: string; status: string; outputFile?: string }>;
};

type Screen = {
	screenId: string;
	legacyForm: string;
	label: string;
	entityFolder?: string; // folder name under output/, e.g. "Barge"
	notes?: string[];
};

type ChildFormsIndex = {
	mainForm: string;
	entity: string;
	childForms: string[];
	detectedAt?: string;
};

type Edge = {
	from: string;
	to: string;
	label: string;
};

type ScreenIndex = {
	screenId: string;
	legacyForm: string;
	label: string;
	entityFolder?: string;
	entityOutputPath?: string;
	entityStatus?: EntityStatus;
	requiredIncoming: Array<{ from: string; label: string }>;
	requiredOutgoing: Array<{ to: string; label: string }>;
	missingOutputFolder: boolean;
	recommendedCommands: {
		analyze: string[];
		generateTemplates: string[];
	};
};

async function fileExists(filePath: string): Promise<boolean> {
	try {
		await fs.access(filePath);
		return true;
	} catch {
		return false;
	}
}

async function readJsonIfExists<T>(filePath: string): Promise<T | undefined> {
	if (!(await fileExists(filePath))) return undefined;
	const raw = await fs.readFile(filePath, "utf8");
	return JSON.parse(raw) as T;
}

async function buildChildFormToEntityFolderMap(outputRoot: string): Promise<Map<string, string>> {
	const map = new Map<string, string>();
	const entries = await fs.readdir(outputRoot, { withFileTypes: true });

	for (const entry of entries) {
		if (!entry.isDirectory()) continue;

		const entityFolder = entry.name;
		const childFormsPath = path.join(outputRoot, entityFolder, "child-forms.json");
		const childIndex = await readJsonIfExists<ChildFormsIndex>(childFormsPath);
		if (!childIndex?.childForms?.length) continue;

		for (const childForm of childIndex.childForms) {
			// First win to avoid flapping if multiple parents mention same child.
			if (!map.has(childForm)) map.set(childForm, entityFolder);
		}
	}

	return map;
}

function inferEntityFolderFromLegacyForm(outputRoot: string, legacyForm: string): string | undefined {
	// Heuristic:
	// - `frmBargeDraft` -> `BargeDraft`
	// - `frmBargeDailyExpenseSearch` -> `BargeDailyExpense`
	if (!legacyForm.startsWith("frm")) return undefined;
	const searchDetailMatch = legacyForm.match(/^frm(.+?)(Search|Detail)$/);
	const candidate = searchDetailMatch ? searchDetailMatch[1] : legacyForm.slice(3);
	if (!candidate) return undefined;
	const candidatePath = path.join(outputRoot, candidate);
	// NOTE: We can't await here; caller should check existence.
	return candidate;
}

function mermaidGraph(screens: Screen[], edges: Edge[]): string {
	const lines: string[] = [];
	lines.push("```mermaid");
	lines.push("flowchart TB");

	for (const s of screens) {
		// Mermaid node IDs can't contain spaces; use screenId for ID and label for display
		lines.push(`  ${s.screenId}["${s.label}"]`);
	}

	lines.push("");
	for (const e of edges) {
		lines.push(`  ${e.from} -->|"${e.label}"| ${e.to}`);
	}
	lines.push("```");
	return lines.join("\n");
}

function formatCode(cmd: string): string {
	return "`" + cmd.replaceAll("`", "\\`") + "`";
}

function analyzeCommands(entity: string, formNames: string[]): string[] {
	return formNames.map((formName) => `bun run agents/orchestrator.ts --entity "${entity}" --form-name "${formName}"`);
}

function templateCommands(entity: string): string[] {
	return [`bun run generate-template --entity "${entity}"`];
}

async function main() {
	const projectRoot = path.resolve(import.meta.dir, "..");
	const outputRoot = path.join(projectRoot, "output");
	const outBargeDir = path.join(outputRoot, "Barge");
	const childFormToEntityFolder = await buildChildFormToEntityFolderMap(outputRoot);

	const screens: Screen[] = [
		{
			screenId: "frmBargeSearch",
			legacyForm: "frmBargeSearch",
			label: "frmBargeSearch (Barge Search)",
			entityFolder: "Barge",
		},
		{
			screenId: "frmBargeDetail",
			legacyForm: "frmBargeDetail",
			label: "frmBargeDetail (Barge Detail)",
			entityFolder: "Barge",
		},
		{
			screenId: "frmBargeDraft",
			legacyForm: "frmBargeDraft",
			label: "frmBargeDraft (Draft Detail)",
		},
		{
			screenId: "frmBargeCharters",
			legacyForm: "frmBargeCharters",
			label: "frmBargeCharters (Charters)",
			entityFolder: "Barge",
			notes: ["Often implemented as a Barge Detail tab/child grid (see Barge templates)."],
		},
		{
			screenId: "frmBargeTokenMap",
			legacyForm: "frmBargeTokenMap",
			label: "frmBargeTokenMap (Token Map)",
		},
		{
			screenId: "frmBargeTokenColors",
			legacyForm: "frmBargeTokenColors",
			label: "frmBargeTokenColors (Token Color Definitions)",
		},
		{
			screenId: "frmBargeConditions",
			legacyForm: "frmBargeConditions",
			label: "frmBargeConditions (Barge Conditions / Photos)",
		},
		// Related entry points
		{
			screenId: "frmBargeEventSearch",
			legacyForm: "frmBargeEventSearch",
			label: "frmBargeEventSearch",
			entityFolder: "BargeEvent",
		},
		{
			screenId: "frmBargeEvent",
			legacyForm: "frmBargeEvent",
			label: "frmBargeEvent",
			entityFolder: "BargeEvent",
		},
		{
			screenId: "frmBargePositions",
			legacyForm: "frmBargePositions",
			label: "frmBargePositions",
		},
		{
			screenId: "frmBargePositionHistory",
			legacyForm: "frmBargePositionHistory",
			label: "frmBargePositionHistory",
			entityFolder: "BargePositionHistory",
		},
		{
			screenId: "frmBargeViewConflicts",
			legacyForm: "frmBargeViewConflicts",
			label: "frmBargeViewConflicts",
		},
		{
			screenId: "frmSendTowDiagram",
			legacyForm: "frmSendTowDiagram",
			label: "frmSendTowDiagram",
		},
		{
			screenId: "frmBargeSeriesSearch",
			legacyForm: "frmBargeSeriesSearch",
			label: "frmBargeSeriesSearch",
			entityFolder: "BargeSeries",
		},
		{
			screenId: "frmBargeSeriesDetail",
			legacyForm: "frmBargeSeriesDetail",
			label: "frmBargeSeriesDetail",
			entityFolder: "BargeSeries",
		},
		{
			screenId: "frmBargeDailyExpenseSearch",
			legacyForm: "frmBargeDailyExpenseSearch",
			label: "frmBargeDailyExpenseSearch",
		},
		{
			screenId: "frmBargeDailyExpenseDetail",
			legacyForm: "frmBargeDailyExpenseDetail",
			label: "frmBargeDailyExpenseDetail",
		},
		{
			screenId: "frmBargeExVendorDocInboxSearch",
			legacyForm: "frmBargeExVendorDocInboxSearch",
			label: "frmBargeExVendorDocInboxSearch",
		},
		{
			screenId: "frmBargeExVendorDocInboxImport",
			legacyForm: "frmBargeExVendorDocInboxImport",
			label: "frmBargeExVendorDocInboxImport",
		},
	];

	const edges: Edge[] = [
		{ from: "frmBargeSearch", to: "frmBargeDetail", label: "Add" },
		{ from: "frmBargeSearch", to: "frmBargeDetail", label: "Modify" },

		{ from: "frmBargeDetail", to: "frmBargeDraft", label: "DraftDetail" },
		{ from: "frmBargeDetail", to: "frmBargeCharters", label: "BargeCharters" },
		{ from: "frmBargeDetail", to: "frmBargeTokenMap", label: "MapColors" },
		{ from: "frmBargeDetail", to: "frmBargeTokenColors", label: "DefineColors" },
		{ from: "frmBargeDetail", to: "frmBargeConditions", label: "ViewPhotos" },

		{ from: "frmBargeEventSearch", to: "frmBargeEvent", label: "Modify" },
		{ from: "frmBargeEvent", to: "frmBargeDetail", label: "NewOrModifyBarge" },
		{ from: "frmBargeEvent", to: "frmBargePositions", label: "BargePositions" },
		{ from: "frmBargePositions", to: "frmBargePositionHistory", label: "History" },
		{ from: "frmBargePositions", to: "frmBargeViewConflicts", label: "ViewConflicts" },
		{ from: "frmBargePositions", to: "frmSendTowDiagram", label: "SendTowDiagram" },

		{ from: "frmBargeSeriesSearch", to: "frmBargeSeriesDetail", label: "Modify" },
		{ from: "frmBargeDailyExpenseSearch", to: "frmBargeDailyExpenseDetail", label: "AddOrModify" },
		{ from: "frmBargeExVendorDocInboxSearch", to: "frmBargeDetail", label: "OpenBarge" },
		{ from: "frmBargeExVendorDocInboxSearch", to: "frmBargeExVendorDocInboxImport", label: "Import" },
	];

	// Precompute incoming/outgoing.
	const incoming = new Map<string, Array<{ from: string; label: string }>>();
	const outgoing = new Map<string, Array<{ to: string; label: string }>>();
	for (const e of edges) {
		incoming.set(e.to, [...(incoming.get(e.to) ?? []), { from: e.from, label: e.label }]);
		outgoing.set(e.from, [...(outgoing.get(e.from) ?? []), { to: e.to, label: e.label }]);
	}

	const screenIndexes: ScreenIndex[] = [];
	for (const s of screens) {
		const childMappedEntityFolder = childFormToEntityFolder.get(s.legacyForm);
		const inferredEntityFolder =
			s.entityFolder ?? childMappedEntityFolder ?? inferEntityFolderFromLegacyForm(outputRoot, s.legacyForm);
		const entityFolder =
			inferredEntityFolder && (await fileExists(path.join(outputRoot, inferredEntityFolder)))
				? inferredEntityFolder
				: s.entityFolder;

		const entityOutputPath = entityFolder ? path.join(outputRoot, entityFolder) : undefined;
		const statusPath = entityOutputPath ? path.join(entityOutputPath, "conversion-status.json") : undefined;
		const entityStatus = statusPath ? await readJsonIfExists<EntityStatus>(statusPath) : undefined;
		const missingOutputFolder = entityFolder ? !(await fileExists(entityOutputPath!)) : true;

		const recommended = {
			analyze: [] as string[],
			generateTemplates: [] as string[],
		};

		if (!missingOutputFolder && entityFolder) {
			recommended.analyze = analyzeCommands(entityFolder, [s.legacyForm]);
			recommended.generateTemplates = templateCommands(entityFolder);
		} else {
			recommended.analyze = [
				`bun run agents/orchestrator.ts --entity "<ENTITY>" --form-name "${s.legacyForm}"`,
			];
			recommended.generateTemplates = [`bun run generate-template --entity "<ENTITY>"`];
		}

		screenIndexes.push({
			screenId: s.screenId,
			legacyForm: s.legacyForm,
			label: s.label,
			entityFolder,
			entityOutputPath: entityOutputPath ? path.relative(projectRoot, entityOutputPath) : undefined,
			entityStatus,
			requiredIncoming: incoming.get(s.screenId) ?? [],
			requiredOutgoing: outgoing.get(s.screenId) ?? [],
			missingOutputFolder,
			recommendedCommands: recommended,
		});
	}

	const mdLines: string[] = [];
	mdLines.push("# Barge Screens - Master Conversion Plan (Top-Down)");
	mdLines.push("");
	mdLines.push("This file is generated by `bun run barge-master-plan`.");
	mdLines.push("");
	mdLines.push("## Flow map (legacy)");
	mdLines.push("");
	mdLines.push(mermaidGraph(screens, edges));
	mdLines.push("");
	mdLines.push("## Screen inventory (top-down)");
	mdLines.push("");

	for (const s of screens) {
		const idx = screenIndexes.find((x) => x.screenId === s.screenId)!;
		mdLines.push(`### ${s.label}`);
		mdLines.push("");
		mdLines.push(`- **legacy form**: \`${s.legacyForm}\``);
		mdLines.push(`- **output folder**: ${idx.entityOutputPath ? `\`${idx.entityOutputPath}\`` : "_(unknown)_"}${idx.missingOutputFolder ? " (missing)" : ""}`);

		if (idx.entityStatus) {
			const st = idx.entityStatus;
			mdLines.push(
				`- **status**: \`${st.overallStatus ?? "unknown"}\` (steps: ${st.completedSteps ?? "?"}/${st.totalSteps ?? "?"}, skipped: ${st.skippedSteps ?? "?"}, failed: ${st.failedSteps ?? "?"})`,
			);
		} else {
			mdLines.push("- **status**: _(unknown - no conversion-status.json found)_");
		}

		if (s.notes?.length) {
			for (const n of s.notes) mdLines.push(`- **notes**: ${n}`);
		}
		if (idx.missingOutputFolder) {
			mdLines.push("- **notes**: No matching output/<Entity>/ folder found yet; will be flagged until generated.");
		}

		if (idx.requiredIncoming.length) {
			mdLines.push("- **required incoming links**:");
			for (const inc of idx.requiredIncoming) mdLines.push(`  - \`${inc.from}\` → (${inc.label})`);
		}

		if (idx.requiredOutgoing.length) {
			mdLines.push("- **required outgoing links**:");
			for (const out of idx.requiredOutgoing) mdLines.push(`  - (${out.label}) → \`${out.to}\``);
		}

		mdLines.push("- **commands**:");
		mdLines.push(`  - analyze: ${idx.recommendedCommands.analyze.map(formatCode).join(" ")}`);
		mdLines.push(`  - templates: ${idx.recommendedCommands.generateTemplates.map(formatCode).join(" ")}`);
		mdLines.push("");
	}

	mdLines.push("## Missing screens / next folders to generate");
	mdLines.push("");
	const missing = screenIndexes.filter((x) => x.missingOutputFolder);
	if (!missing.length) {
		mdLines.push("- None");
	} else {
		for (const m of missing) {
			mdLines.push(`- \`${m.legacyForm}\` (screenId: \`${m.screenId}\`) — no ` + "`output/<Entity>/`" + " folder found yet");
		}
	}
	mdLines.push("");

	await fs.mkdir(outBargeDir, { recursive: true });
	await fs.writeFile(path.join(outBargeDir, "MASTER_BARGE_SCREENS.md"), mdLines.join("\n"), "utf8");
	await fs.writeFile(
		path.join(outBargeDir, "master-barge-index.json"),
		JSON.stringify(
			{
				generatedAt: new Date().toISOString(),
				outputRoot: path.relative(projectRoot, outputRoot),
				screens: screenIndexes,
				edges,
			},
			null,
			2,
		),
		"utf8",
	);
}

await main();


