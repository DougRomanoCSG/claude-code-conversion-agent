#!/usr/bin/env -S bun run
/**
 * CONVERSION TEMPLATE GENERATOR: Generate complete conversion plan (ALWAYS INTERACTIVE)
 *
 * This agent ALWAYS runs interactively in Claude Code.
 * It takes all extracted data from steps 1-10 and generates:
 * - Complete conversion plan document
 * - Code templates for domain models, DTOs, repositories, services, controllers
 * - View models and Razor views
 * - JavaScript files
 * - Step-by-step implementation guide
 *
 * Usage:
 *   bun run agents/conversion-template-generator.ts --entity "Facility"
 *
 * NOTE: This agent always runs interactively, --interactive flag is implicit
 */

import { spawn } from "bun";
import { buildClaudeFlags, parsedArgs } from "../lib/flags";
import type { ClaudeFlags } from "../lib/claude-flags.types";
import { getProjectRoot, getCrewingApiPath, getCrewingUiPath, getAdminApiPath, getAdminUiPath, getSharedProjectPath, getDetailedReferenceExamples } from "../lib/paths";
import { join, dirname } from "path";
import { fileURLToPath } from "url";
import templateGenSettings from "../settings/template-generator.settings.json" with { type: "json" };

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const projectRoot = getProjectRoot(import.meta.url);
const settingsJson = JSON.stringify(templateGenSettings);
const mcpConfigPath = join(__dirname, "..", "settings", "template-generator.mcp.json");

interface GeneratorOptions {
	entity: string;
	outputDir?: string;
}

function parseOptions(): GeneratorOptions {
	const entity = parsedArgs.values.entity as string;
	const outputDir = parsedArgs.values.output as string;

	if (!entity) {
		console.error("Error: --entity parameter is required");
		process.exit(1);
	}

	return { entity, outputDir };
}

async function runTemplateGenerator(options: GeneratorOptions): Promise<number> {
	const outputPath = options.outputDir || `${projectRoot}output/${options.entity}`;

	const analysisFiles = [
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

	const systemPrompt = `
You are a specialized Conversion Template Generator agent running in INTERACTIVE MODE.

TASK: Generate complete conversion plan and code templates for ${options.entity}.

INPUT DATA:
You have access to analysis files in: ${outputPath}/
${analysisFiles.map((f) => `- ${f}`).join("\n")}

CRITICAL ARCHITECTURE NOTE:
⭐ This project uses a MONO SHARED structure where DTOs and Models are in a SHARED project!
⭐ DO NOT duplicate DTOs/Models in API or UI projects!
⭐ See .claude/tasks/MONO_SHARED_STRUCTURE.md for detailed architecture documentation

GENERATION GOALS:
1. Review all extracted data from previous agents
2. Create comprehensive conversion plan document
3. Generate code templates organized by project structure:

   ⭐ FOR BargeOps.Shared (${getSharedProjectPath()}):
   GENERATE FIRST - DTOs are the ONLY data models (no separate Models folder!)
   - DTOs in Dto/ folder (used by BOTH API and UI):
     * {Entity}Dto.cs - Complete entity DTO with [Sortable]/[Filterable] attributes
       - Contains ALL fields from the entity
       - Used directly by API and UI (no separate domain models)
     * {Entity}SearchRequest.cs - Search criteria DTO
     * {Child}Dto.cs - Child entity DTOs (e.g., FacilityBerthDto)
   - NO Models/ folder - DTOs are the data models!

   FOR BargeOps.Admin.API (${getAdminApiPath()}):
   - Repository interface and implementation (in Admin.Infrastructure/Repositories/)
     * I{Entity}Repository.cs - Interface returning DTOs
     * {Entity}Repository.cs - Dapper implementation with DIRECT SQL QUERIES (NOT stored procedures)
       - Returns DTOs directly (no mapping needed!)
       - Uses async/await patterns
       - Uses parameterized SQL queries (not SPs)
   - Service interface and implementation
     * I{Entity}Service.cs - Interface (in Admin.Domain/Services/)
     * {Entity}Service.cs - Implementation (in Admin.Infrastructure/Services/)
       - Uses DTOs directly from repository
       - No AutoMapper needed!
   - API Controller (in Admin.Api/Controllers/)
     * {Entity}Controller.cs - RESTful endpoints with authorization
       - Accepts and returns DTOs
   - NO AutoMapper profiles needed (repositories return DTOs directly)

   FOR BargeOps.Admin.UI (${getAdminUiPath()}):
   - ViewModels (in ViewModels/ folder)
     * {Entity}SearchViewModel.cs - Search screen
       - Contains search criteria properties
       - Uses DTOs from BargeOps.Shared directly
     * {Entity}EditViewModel.cs - Edit screen
       - Contains the entity DTO (from BargeOps.Shared)
       - Contains lookup lists (also DTOs)
   - API Client Services (in Services/ folder)
     * I{Entity}Service.cs - Interface
     * {Entity}Service.cs - HTTP client to call API
       - Returns DTOs from BargeOps.Shared
   - Controllers (in Controllers/ folder)
     * {Entity}Controller.cs - MVC controller
       - Uses ViewModels which contain DTOs
   - Razor views (in Views/{Entity}/ folder)
     * Index.cshtml - Search/list view
     * Edit.cshtml - Edit form
     * _Partials/ - Modal dialogs for child entities
   - JavaScript files (in wwwroot/js/ folder)
     * {entity}-search.js - DataTables initialization
     * {entity}-detail.js - Detail form logic

4. Include step-by-step implementation guide
5. Reference existing Facility and BoatLocation patterns in the mono repo
6. Use Crewing examples for additional clarity

OUTPUT STRUCTURE:
Primary file: ${outputPath}/conversion-plan.md

Templates organized by project:
${outputPath}/templates/
├── shared/          ⭐ SHARED PROJECT FILES (create these FIRST!)
│   └── Dto/         ⭐ DTOs are the ONLY data models (no Models/ folder!)
│       ├── {Entity}Dto.cs - Complete entity DTO
│       ├── {Entity}SearchRequest.cs - Search criteria
│       └── {Child}Dto.cs - Child entity DTOs
├── api/             (API-specific files)
│   ├── Controllers/
│   │   └── {Entity}Controller.cs
│   ├── Repositories/
│   │   ├── I{Entity}Repository.cs
│   │   └── {Entity}Repository.cs
│   ├── Services/
│   │   ├── I{Entity}Service.cs
│   │   └── {Entity}Service.cs
│   └── Mapping/
│       └── {Entity}MappingProfile.cs
└── ui/              (UI-specific files)
    ├── Controllers/
    │   └── {Entity}Controller.cs
    ├── Services/
    │   ├── I{Entity}Service.cs
    │   └── {Entity}Service.cs
    ├── ViewModels/      ⭐ OFFER TO GENERATE THESE DURING INTERACTIVE SESSION
    │   ├── {Entity}SearchViewModel.cs   - Search/list screen
    │   ├── {Entity}EditViewModel.cs     - Edit/create form
    │   ├── {Entity}DetailsViewModel.cs  - Read-only details
    │   └── {Entity}ListItemViewModel.cs - Grid row data (optional)
    ├── Views/
    │   └── {Entity}/
    │       ├── Index.cshtml
    │       └── Edit.cshtml
    └── wwwroot/
        └── js/
            ├── {entity}-search.js
            └── {entity}-detail.js

${getDetailedReferenceExamples()}

TARGET PROJECTS (where generated code will be placed):
⭐ Shared: ${getSharedProjectPath()} (DTOs and Models - SINGLE SOURCE OF TRUTH)
- API: ${getAdminApiPath()}
- UI: ${getAdminUiPath()}

IMPORTANT IMPLEMENTATION ORDER:
1. Create SHARED DTOs first (DTOs are the ONLY data models - no separate Models!)
   - {Entity}Dto.cs - Complete entity with all fields
   - {Entity}SearchRequest.cs - Search criteria
   - {Child}Dto.cs - Child entities
2. Create API Infrastructure (Repositories, Services)
   - Repositories return DTOs directly (no mapping!)
   - Services use DTOs (no AutoMapper needed!)
3. Create API Controller (accepts/returns DTOs)
4. Create UI Services (API clients that return DTOs)
5. Create UI ViewModels (contain DTOs from Shared)
6. Create UI Controllers and Views

VIEWMODEL GENERATION:
As part of template generation, you should offer to create ViewModels for common scenarios:
- SearchViewModel - For search/list screens
- EditViewModel - For edit/create forms
- DetailsViewModel - For read-only detail views
- ListItemViewModel - For grid row data (if different from DTO)

For each ViewModel, follow these patterns:
1. **Namespace**: BargeOpsAdmin.ViewModels (file-scoped)
2. **Location**: src/BargeOps.UI/Models/
3. **MVVM Pattern**: NO ViewBag/ViewData - all data on ViewModel
4. **DateTime Properties**: Single property (e.g., PositionUpdatedDateTime)
   - View splits into date + time inputs
   - JavaScript combines on submit
5. **Dropdowns**: IEnumerable<SelectListItem> properties on ViewModel
6. **Validation**: Data annotations on ViewModel properties
7. **Display Attributes**: [Display(Name = "...")] for all user-facing properties
8. **ID Fields**: Uppercase ID (LocationID, BargeID, NOT LocationId)

INTERACTIVE WORKFLOW:
1. Generate initial templates based on analysis
2. Ask: "Would you like me to generate ViewModels for this entity?"
3. If yes, ask which types: Search, Edit, Details, ListItem
4. Generate requested ViewModels following patterns above
5. Iterate and refine based on user feedback

This is an INTERACTIVE session. You can ask questions, clarify requirements, and iterate on the templates.

Begin template generation now.
`;

	const baseFlags = {
		settings: settingsJson,
		"mcp-config": mcpConfigPath,
		"append-system-prompt": systemPrompt,
	} as const;

	const flags = buildClaudeFlags({ ...baseFlags }, parsedArgs.values as ClaudeFlags);

	// Initial prompt to start the interactive session
	const initialPrompt = `Generate conversion templates for the ${options.entity} entity based on the analysis files in ${outputPath}. Include ViewModels for the UI layer.`;
	const args = [...flags, initialPrompt];

	console.log(`
╔════════════════════════════════════════════════════════════════════════════╗
║              CONVERSION TEMPLATE GENERATOR (Interactive Mode)               ║
║                                                                            ║
║  Entity: ${options.entity.padEnd(68, " ")}║
║  Output: ${outputPath.padEnd(67, " ")}║
║                                                                            ║
║  This agent will generate conversion templates based on the analysis       ║
║  from steps 1-10. You can interact with Claude to refine the templates.   ║
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
	console.log(`[conversion-template-generator] Launching interactive template generator for ${options.entity}...`);

	const code = await runTemplateGenerator(options);
	process.exit(code);
}

await main();
