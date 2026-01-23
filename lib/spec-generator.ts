import * as fs from "node:fs";
import * as path from "node:path";

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

interface SpecOptions {
	outputPath: string;
	monorepoPath: string;
	additionalMarkdownFiles?: Array<{ fileName: string; content: string }>;
}

interface MasterPlanData {
	forms?: string[];
	childForms?: string[];
	businessLogicClasses?: string[];
	storedProcedures?: string[];
	apiEndpoints?: string[];
	features?: string[];
	complexity?: string;
	estimatedEffort?: string;
}

interface Gap {
	type: "form" | "class" | "procedure" | "endpoint" | "feature" | "inconsistency";
	item: string;
	description: string;
	severity: "high" | "medium" | "low";
	recommendation?: string;
}

export async function generateSpec(
	analysisData: AnalysisData,
	options: SpecOptions,
): Promise<void> {
	const { entity } = analysisData;
	const specDir = path.join(
		options.monorepoPath,
		".speckit/entities",
		entity,
	);
	const tasksDir = path.join(specDir, "tasks");

	// Create directories
	fs.mkdirSync(specDir, { recursive: true });
	fs.mkdirSync(tasksDir, { recursive: true });

	// Try to load master plan from output directory
	// The output path should be something like: output/{entity}/
	// Note: outputPath in options is the spec output path, not the analysis output path
	const analysisOutputPath = path.join(process.cwd(), "output", entity);
	const masterPlan = await loadMasterPlan(entity, analysisOutputPath);
	const gaps = masterPlan ? compareMasterPlanWithAnalysis(masterPlan, analysisData) : [];

	// Load additional markdown files if not provided
	const additionalMarkdownFiles = options.additionalMarkdownFiles || await loadAllMarkdownFiles(entity, analysisOutputPath);

	// Generate spec.md
	const specContent = generateSpecContent(analysisData, masterPlan, gaps, additionalMarkdownFiles);
	fs.writeFileSync(path.join(specDir, "spec.md"), specContent);

	// Generate quality-checklist.md
	const checklistContent = generateQualityChecklist(analysisData);
	fs.writeFileSync(path.join(specDir, "quality-checklist.md"), checklistContent);

	// Generate task files
	generateTaskFiles(analysisData, tasksDir);

	console.log(`✓ Spec created: ${path.join(specDir, "spec.md")}`);
	if (masterPlan) {
		console.log(`✓ Master plan reviewed: ${gaps.length} gap(s) identified`);
	}
	console.log(`✓ Quality checklist: ${path.join(specDir, "quality-checklist.md")}`);
	console.log(`✓ Tasks created in: ${tasksDir}`);
}

/**
 * Load master plan document if it exists
 */
async function loadMasterPlan(entity: string, outputPath: string): Promise<MasterPlanData | null> {
	// Try different possible file names (case variations)
	const possibleNames = [
		`${entity}_CONVERSION_MASTER_PLAN.md`,
		`${entity.toUpperCase()}_CONVERSION_MASTER_PLAN.md`,
		`${entity}_Conversion_Master_Plan.md`,
		`${entity}ConversionMasterPlan.md`,
	];

	for (const fileName of possibleNames) {
		const filePath = path.join(outputPath, fileName);
		if (fs.existsSync(filePath)) {
			try {
				const content = fs.readFileSync(filePath, "utf-8");
				return parseMasterPlan(content, entity);
			} catch (error) {
				console.warn(`Warning: Could not read master plan ${fileName}: ${error}`);
				return null;
			}
		}
	}

	return null;
}

/**
 * Load all markdown files from the output directory (excluding master plan)
 */
async function loadAllMarkdownFiles(entity: string, outputPath: string): Promise<Array<{ fileName: string; content: string }>> {
	const markdownFiles: Array<{ fileName: string; content: string }> = [];
	
	if (!fs.existsSync(outputPath)) {
		return markdownFiles;
	}

	// Get all .md files in the output directory
	const files = fs.readdirSync(outputPath);
	const masterPlanPatterns = [
		new RegExp(`${entity}_CONVERSION_MASTER_PLAN\\.md$`, "i"),
		new RegExp(`${entity.toUpperCase()}_CONVERSION_MASTER_PLAN\\.md$`, "i"),
		new RegExp(`${entity}_Conversion_Master_Plan\\.md$`, "i"),
		new RegExp(`${entity}ConversionMasterPlan\\.md$`, "i"),
	];

	for (const file of files) {
		if (file.endsWith(".md")) {
			// Skip master plan files (they're handled separately)
			const isMasterPlan = masterPlanPatterns.some(pattern => pattern.test(file));
			if (isMasterPlan) {
				continue;
			}

			const filePath = path.join(outputPath, file);
			try {
				const content = fs.readFileSync(filePath, "utf-8");
				markdownFiles.push({ fileName: file, content });
			} catch (error) {
				console.warn(`Warning: Could not read markdown file ${file}: ${error}`);
			}
		}
	}

	return markdownFiles;
}

/**
 * Parse master plan markdown to extract structured information
 */
function parseMasterPlan(content: string, entity: string): MasterPlanData {
	const data: MasterPlanData = {};

	// Extract forms from "FORMS TO CONVERT" section
	const formsMatch = content.match(/## FORMS TO CONVERT[\s\S]*?(?=## |$)/i);
	if (formsMatch) {
		const formsSection = formsMatch[0];
		const formMatches = formsSection.match(/### \*\*(\d+)\.\s*(frm\w+\.vb)\*\*/gi);
		if (formMatches) {
			data.forms = formMatches.map(m => {
				const match = m.match(/frm\w+\.vb/i);
				return match ? match[0] : "";
			}).filter(f => f);
		}
	}

	// Extract child forms from screen hierarchy or forms section
	const childFormsMatch = content.match(/frm\w+(Status|PositionHistory|FuelDetail|Delays|TextEditor|PortalGroup)/gi);
	if (childFormsMatch) {
		data.childForms = [...new Set(childFormsMatch.map(f => f + ".vb"))];
	}

	// Extract business logic classes from "BUSINESS LOGIC CLASSES TO CONVERT" section
	const businessLogicMatch = content.match(/## BUSINESS LOGIC CLASSES TO CONVERT[\s\S]*?(?=## |$)/i);
	if (businessLogicMatch) {
		const blSection = businessLogicMatch[0];
		const classMatches = blSection.match(/- `(\w+\.vb)`/gi);
		if (classMatches) {
			data.businessLogicClasses = classMatches.map(m => {
				const match = m.match(/`(\w+\.vb)`/i);
				return match ? match[1] : "";
			}).filter(c => c);
		}
	}

	// Extract stored procedures from "DATABASE STORED PROCEDURES" section
	const spMatch = content.match(/## DATABASE STORED PROCEDURES[\s\S]*?(?=## |$)/i);
	if (spMatch) {
		const spSection = spMatch[0];
		const spMatches = spSection.match(/dbo\.(\w+)/gi);
		if (spMatches) {
			data.storedProcedures = [...new Set(spMatches.map(sp => sp.replace(/dbo\./i, "")))];
		}
	}

	// Extract API endpoints from "API ENDPOINTS TO CREATE" section
	const endpointsMatch = content.match(/## API ENDPOINTS TO CREATE[\s\S]*?(?=## |$)/i);
	if (endpointsMatch) {
		const endpointsSection = endpointsMatch[0];
		const endpointMatches = endpointsSection.match(/(GET|POST|PUT|DELETE)\s+\/api\/[\w\/\{\}]+/gi);
		if (endpointMatches) {
			data.apiEndpoints = [...new Set(endpointMatches)];
		}
	}

	// Extract complexity and effort
	const complexityMatch = content.match(/### \*\*Complexity:\s*([^\*]+)\*\*/i);
	if (complexityMatch) {
		data.complexity = complexityMatch[1].trim();
	}

	const effortMatch = content.match(/\*\*TOTAL\*\*\s*\|\s*\*\*([^\*]+)\*\*/i);
	if (effortMatch) {
		data.estimatedEffort = effortMatch[1].trim();
	}

	return data;
}

/**
 * Compare master plan against analysis data to identify gaps
 */
function compareMasterPlanWithAnalysis(masterPlan: MasterPlanData, analysisData: AnalysisData): Gap[] {
	const gaps: Gap[] = [];
	const { entity } = analysisData;

	// Check for forms mentioned in master plan but not in analysis
	if (masterPlan.forms) {
		const analysisForms = new Set<string>();
		if (analysisData.formStructureSearch) {
			analysisForms.add(`frm${entity}Search.vb`);
		}
		if (analysisData.formStructureDetail) {
			analysisForms.add(`frm${entity}Detail.vb`);
		}

		for (const form of masterPlan.forms) {
			if (!analysisForms.has(form)) {
				gaps.push({
					type: "form",
					item: form,
					description: `Master plan mentions ${form} but it was not analyzed`,
					severity: "high",
					recommendation: `Run orchestrator analysis for ${form} or verify form name is correct`,
				});
			}
		}
	}

	// Check for child forms mentioned in master plan
	// Note: Child forms are tracked separately in child-forms.json by the orchestrator
	// This is informational only - we don't flag them as gaps since they're handled differently

	// Check for business logic classes
	// Note: Business logic classes are extracted from the VB files during analysis
	// We can't easily verify all classes are covered without parsing the actual VB files
	// This is informational - the master plan documents what should be converted
	if (masterPlan.businessLogicClasses && masterPlan.businessLogicClasses.length > 0) {
		gaps.push({
			type: "class",
			item: `${masterPlan.businessLogicClasses.length} business logic class(es)`,
			description: `Master plan documents ${masterPlan.businessLogicClasses.length} business logic class(es) to convert: ${masterPlan.businessLogicClasses.slice(0, 5).join(", ")}${masterPlan.businessLogicClasses.length > 5 ? "..." : ""}`,
			severity: "low",
			recommendation: `Verify all classes are analyzed in business-logic.json. Classes: ${masterPlan.businessLogicClasses.join(", ")}`,
		});
	}

	// Check for stored procedures
	if (masterPlan.storedProcedures) {
		const analysisProcs = new Set<string>();
		if (analysisData.dataAccess?.storedProcedures) {
			Object.keys(analysisData.dataAccess.storedProcedures).forEach(key => {
				analysisProcs.add(key);
			});
		}

		for (const sp of masterPlan.storedProcedures) {
			if (!analysisProcs.has(sp)) {
				gaps.push({
					type: "procedure",
					item: sp,
					description: `Master plan mentions stored procedure ${sp} but it's not in analysis data`,
					severity: "medium",
					recommendation: `Review data-access.json to ensure ${sp} is documented`,
				});
			}
		}
	}

	// Check for API endpoints
	// Note: API endpoints are typically generated from analysis, so this is informational
	if (masterPlan.apiEndpoints && masterPlan.apiEndpoints.length > 0) {
		gaps.push({
			type: "endpoint",
			item: `${masterPlan.apiEndpoints.length} API endpoint(s)`,
			description: `Master plan documents ${masterPlan.apiEndpoints.length} API endpoint(s) to create`,
			severity: "low",
			recommendation: `Verify generated API templates include all documented endpoints. Review API endpoints section in master plan.`,
		});
	}

	// Check if analysis has data not mentioned in master plan
	if (analysisData.businessLogic?.childEntities) {
		const childEntityNames = Object.keys(analysisData.businessLogic.childEntities);
		if (childEntityNames.length > 0 && (!masterPlan.businessLogicClasses || masterPlan.businessLogicClasses.length === 0)) {
			gaps.push({
				type: "inconsistency",
				item: "Child Entities",
				description: `Analysis found ${childEntityNames.length} child entity(ies): ${childEntityNames.join(", ")} but master plan may not document them`,
				severity: "medium",
				recommendation: `Update master plan to document child entities: ${childEntityNames.join(", ")}`,
			});
		}
	}

	return gaps;
}

function generateSpecContent(
	data: AnalysisData,
	masterPlan: MasterPlanData | null = null,
	gaps: Gap[] = [],
	additionalMarkdownFiles: Array<{ fileName: string; content: string }> = []
): string {
	const { entity, businessLogic, tabs, relatedEntities, validation, security, dataAccess, formStructureSearch } =
		data;

	const properties = businessLogic?.properties || [];
	const businessRules = businessLogic?.businessRules || [];
	const childEntities = businessLogic?.childEntities || {};
	const tabList = tabs?.tabs || [];
	const validationRules = validation?.formValidation || [];
	const securityInfo = security || {};
	const dataAccessInfo = dataAccess || {};
	const gridColumns = formStructureSearch?.grid?.columns || [];

	const date = new Date().toISOString().split("T")[0];

	return `# Specification: ${entity} Conversion

**Generated:** ${date}
**Analysis Source:** ClaudeOnshoreConversionAgent
**Forms Analyzed:**
${tabList.length > 0 ? `- frm${entity}Search.vb\n- frm${entity}Detail.vb` : `- frm${entity}.vb`}

**Business Object:** ${entity}.vb

---

## Overview

Convert legacy VB.NET Windows Forms for ${entity} to modern ASP.NET Core MVC architecture following BargeOps patterns.

## Scope

### In Scope
- [x] Search functionality (grid with filters)
${tabList.length > 0 ? `- [x] Detail form with ${tabList.length} tabs` : "- [x] Detail form"}
- [x] CRUD operations (Create, Read, Update, Delete)
- [x] Related entities: ${Object.keys(childEntities).join(", ") || "None"}
- [x] Security: ${securityInfo.subSystem || "TBD"} permissions
- [x] Validation: ${businessRules.length} business rules

### Out of Scope
- Legacy VB.NET code modifications
- Database schema changes
- Stored procedure rewrites (reference only)

---

## Architecture Layers

### 1. Shared Layer (CREATE FIRST!)

**Location:** \`src/BargeOps.Shared/BargeOps.Shared/Dto/\`
**Namespace:** \`BargeOps.Shared.Dto\`

#### DTOs to Create

- [ ] **${entity}Dto.cs**
  - Main DTO with ${properties.length} properties
  - Key properties: ${properties.slice(0, 5).map((p: any) => p.name).join(", ")}${properties.length > 5 ? "..." : ""}

- [ ] **${entity}SearchRequest.cs**
  - Search criteria DTO
  - Properties from search form filters

${Object.keys(childEntities).length > 0 ? `
**Child Entity DTOs:**
${Object.keys(childEntities).map((key) => `- [ ] **${key}Dto.cs** - ${childEntities[key].description || key}`).join("\n")}
` : ""}

**Key Properties from Analysis:**

| Property | Type | Required | Max Length | Description |
|----------|------|----------|------------|-------------|
${properties.slice(0, 10).map((p: any) =>
	`| ${p.name} | ${p.type} | ${p.isRequired ? "Yes" : "No"} | ${p.maxLength || "N/A"} | ${p.description || ""} |`
).join("\n")}
${properties.length > 10 ? `\n*(${properties.length - 10} more properties...)*` : ""}

---

### 2. API Layer

**Location:** \`src/BargeOps.API/\`

#### 2.1 Repository Layer

**Interface:** \`src/Admin.Infrastructure/Abstractions/I${entity}Repository.cs\`

\`\`\`csharp
public interface I${entity}Repository
{
    Task<IEnumerable<${entity}Dto>> SearchAsync(${entity}SearchRequest request);
    Task<${entity}Dto> GetByIdAsync(int id);
    Task<int> InsertAsync(${entity}Dto dto);
    Task UpdateAsync(${entity}Dto dto);
    Task DeleteAsync(int id);
${Object.keys(childEntities).map((key) => `    Task<IEnumerable<${key}Dto>> Get${key}sByParentIdAsync(int parentId);`).join("\n")}
}
\`\`\`

**Implementation:** \`src/Admin.Infrastructure/Repositories/${entity}Repository.cs\`

- [ ] Implement using Dapper
- [ ] SQL queries in embedded \`.sql\` files
- [ ] Async/await patterns
- [ ] Proper connection management

**SQL Files:** \`src/Admin.Infrastructure/DataAccess/Sql/${entity}/\`

- [ ] \`${entity}_Search.sql\` - ${dataAccessInfo.storedProcedures?.fetchAll || "Search query"}
- [ ] \`${entity}_GetById.sql\` - ${dataAccessInfo.storedProcedures?.fetch || "Get by ID query"}
- [ ] \`${entity}_Insert.sql\` - ${dataAccessInfo.storedProcedures?.insert || "Insert query"}
- [ ] \`${entity}_Update.sql\` - ${dataAccessInfo.storedProcedures?.update || "Update query"}
- [ ] \`${entity}_Delete.sql\` - ${dataAccessInfo.storedProcedures?.delete || "Delete query"}
${Object.keys(childEntities).map((key) => `- [ ] \`${key}_GetBy${entity}Id.sql\`
- [ ] \`${key}_Insert.sql\`
- [ ] \`${key}_Update.sql\`
- [ ] \`${key}_Delete.sql\``).join("\n")}

#### 2.2 Service Layer

**Interface:** \`src/Admin.Api/Interfaces/I${entity}Service.cs\`

\`\`\`csharp
public interface I${entity}Service
{
    Task<IEnumerable<${entity}Dto>> SearchAsync(${entity}SearchRequest request);
    Task<${entity}Dto> GetByIdAsync(int id);
    Task<int> CreateAsync(${entity}Dto dto);
    Task UpdateAsync(int id, ${entity}Dto dto);
    Task DeleteAsync(int id);
}
\`\`\`

**Implementation:** \`src/Admin.Api/Services/${entity}Service.cs\`

- [ ] Business logic implementation
- [ ] Validation rules (FluentValidation)
- [ ] Business rule enforcement
- [ ] Transaction management for child entities
- [ ] Error handling

#### 2.3 Controller

**File:** \`src/Admin.Api/Controllers/${entity}Controller.cs\`

- [ ] Inherits from \`ApiControllerBase\`
- [ ] RESTful endpoints
- [ ] Proper HTTP status codes
- [ ] Authorization attributes

**Endpoints:**

- [ ] \`GET /api/${entity.toLowerCase()}\` - Search/list
- [ ] \`GET /api/${entity.toLowerCase()}/{id}\` - Get by ID
- [ ] \`POST /api/${entity.toLowerCase()}\` - Create
- [ ] \`PUT /api/${entity.toLowerCase()}/{id}\` - Update
- [ ] \`DELETE /api/${entity.toLowerCase()}/{id}\` - Delete

---

### 3. UI Layer

**Location:** \`src/BargeOps.UI/\`

#### 3.1 Controllers

**Search Controller:** \`Controllers/${entity}SearchController.cs\`

- [ ] Inherits from \`AppController\`
- [ ] Index action (search view)
- [ ] Search action (AJAX DataTables)
- [ ] Create action (GET/POST)
- [ ] Edit action (GET/POST)
- [ ] Details action (GET)
- [ ] Delete action (POST)

#### 3.2 ViewModels

**Location:** \`Models/\`

- [ ] **${entity}SearchViewModel.cs**
  - Search filters
  - SelectListItems for dropdowns
  - Validation attributes

- [ ] **${entity}EditViewModel.cs**
  - All editable properties
  - Child collections
  - Display/Validation attributes
  - SelectListItems

- [ ] **${entity}DetailsViewModel.cs**
  - Read-only display
  - Child collections

- [ ] **${entity}ListItemModel.cs** (DataTables row)
  - Grid columns only
  - Formatted display values

${Object.keys(childEntities).length > 0 ? `
**Child Entity ViewModels:**
${Object.keys(childEntities).map((key) => `- [ ] **${key}ViewModel.cs**`).join("\n")}
` : ""}

#### 3.3 Razor Views

**Location:** \`Views/${entity}Search/\`

- [ ] **Index.cshtml** - Search page with filters and DataTables grid
  - Include DataTables Buttons extension CSS/JS (CDN or local)
  - Grid table with Actions column as first column header
- [ ] **Edit.cshtml** - Create/Edit form${tabList.length > 0 ? ` with ${tabList.length} tabs` : ""}
- [ ] **Details.cshtml** - Read-only details view
- [ ] **_SearchFilters.cshtml** - Partial for search filters
- [ ] **_SearchResults.cshtml** - Partial for DataTables grid
  - Table header includes "Actions" as first column
  - Grid body is empty (populated by DataTables)
  - No action buttons above the grid (buttons are in rows)

${tabList.length > 0 ? `
**Tab Partials:**
${tabList.map((tab: any) => `- [ ] **_${tab.tabName}.cshtml** - ${tab.purpose || tab.tabText}`).join("\n")}
` : ""}

#### 3.4 JavaScript

**Location:** \`wwwroot/js/\`

- [ ] **${entity.toLowerCase()}Search.js**
  - DataTables initialization with server-side processing
  - **Action buttons column** (FIRST column) with Edit and Delete buttons in each row
    - Edit button: \`btn btn-sm btn-outline-primary\` with \`fas fa-pencil-alt\` icon
    - Delete button: \`btn btn-sm btn-outline-secondary action-btn\` with \`fas fa-trash\` icon
    - Use \`render\` function to generate buttons with \`data-id\` and \`data-name\` attributes
  - Column definitions: Actions (first), ${gridColumns.map((c: any) => c.key).join(", ")}
  - Search filter handlers
  - **DataTables Buttons extension** for export functionality
    - Include DataTables Buttons library (CDN or local)
    - Configure \`dom: 'Bfrtip'\` to show buttons toolbar
    - Add export buttons: Excel (\`extend: 'excelHtml5'\`) and CSV (\`extend: 'csvHtml5'\`)
    - Export options should exclude action column: \`columns: ':visible:not(:first-child)'\`

${Object.keys(childEntities).length > 0 ? `- [ ] **${entity.toLowerCase()}Detail.js**
  - Tab management
  - **Child entity DataTables** with action buttons in rows (if applicable)
  - **DataTables Buttons extension** for export on child entity grids
  - Inline editing for child entities (if using inline editing instead of action buttons)
  - Form validation
  - AJAX submit handlers
  - **Note:** For child entity grids (e.g., Status, Berths tabs), convert above-grid buttons (Add, Modify, Remove) to:
    - **Add button** remains above grid (creates new row)
    - **Edit/Delete buttons** move into grid rows as action buttons
    - **Export button** uses DataTables Buttons extension
` : ""}

---

## Success Criteria

### Functional Requirements

- [ ] **Search Form**
  - Grid columns: Actions (first), ${gridColumns.map((c: any) => c.header || c.key).join(", ")}
  - **Action buttons in grid rows** (Edit and Delete per row, not above grid)
  - Sorting and pagination work
  - **DataTables Buttons export** (Excel, CSV) using Buttons extension

- [ ] **Detail Form**
${tabList.length > 0 ? `  - All ${tabList.length} tabs render correctly\n  - Tab navigation works` : "  - Form renders correctly"}
  - Related entities editable${Object.keys(childEntities).length > 0 ? " with action buttons in grid rows or inline editing" : ""}
  - **Child entity grids** (if applicable) have action buttons in rows, not above grid
  - **Child entity export** uses DataTables Buttons extension
  - Form submission saves all data

- [ ] **Validation**
  - All ${businessRules.length} business rules enforced
  - Client-side validation works
  - Server-side validation returns proper errors

- [ ] **Security**
  - Subsystem: ${securityInfo.subSystem || "TBD"}
  - Authorization attributes on controllers
  - Button permissions respected

### Technical Requirements

- [ ] All DTOs in \`BargeOps.Shared.Dto\` namespace
- [ ] Repository uses Dapper with \`.sql\` embedded resources
- [ ] Service layer has business logic
- [ ] ViewModels use MVVM (no ViewBag/ViewData)
- [ ] Bootstrap 5 styling
- [ ] DataTables server-side processing
- [ ] **Action buttons in grid rows** (not above grid)
- [ ] **DataTables Buttons extension** for export functionality
- [ ] Select2 for dropdowns
- [ ] Proper async/await patterns
- [ ] ID fields uppercase (not Id)
- [ ] \`IdentityConstants.ApplicationScheme\` used for auth

### Quality Gates

- [ ] No compiler errors or warnings
- [ ] Business rules from \`CheckBusinessRules\` implemented
- [ ] Validation from \`AreFieldsValid\` implemented
- [ ] Security from \`SetButtonTypes\` implemented
- [ ] Related entities from child grids implemented
- [ ] Code follows \`.cursorrules\` conventions
- [ ] Comments added sparingly, only for complex logic

---

## Validation Rules (from analysis)

${businessRules.slice(0, 10).map((rule: any) =>
	`### ${rule.ruleName}\n- **Property:** ${rule.property}\n- **Message:** "${rule.message}"\n- **Modern:** \`${rule.modernEquivalent || "TBD"}\``
).join("\n\n")}

${businessRules.length > 10 ? `\n*(${businessRules.length - 10} more rules in analysis data)*` : ""}

---

## Business Rules (from analysis)

${businessRules.map((rule: any) => `- **${rule.ruleName}**: ${rule.message}`).join("\n")}

---

## Security Requirements (from analysis)

- **SubSystem:** ${securityInfo.subSystem || "TBD"}
- **Permissions:** ${securityInfo.permissions?.join(", ") || "TBD"}
${securityInfo.buttonSecurity ? `- **Button Security:** ${Object.keys(securityInfo.buttonSecurity).join(", ")}` : ""}

---

## Dependencies

### Prerequisites
- [ ] BargeOps.Shared.Dto namespace exists
- [ ] Database tables/SPs exist
- [ ] Bootstrap 5 and DataTables configured
- [ ] Authentication/authorization setup

### Related Entities

${Object.keys(childEntities).map((key) =>
	`- **${key}** - ${childEntities[key].description || "Child entity"}`
).join("\n") || "None"}

---

## Reference Implementations

- **Similar entity:** Look for similar patterns in existing conversions
- **Facility example** (canonical pattern):
  - Search grid: \`C:\\Dev\\BargeOps.Admin.Mono\\src\\BargeOps.UI\\wwwroot\\js\\facility-search.js\`
    - Action buttons in grid rows (Edit/Delete)
    - DataTables server-side processing
  - Edit form: \`C:\\Dev\\BargeOps.Admin.Mono\\src\\BargeOps.UI\\Views\\Facility\\Edit.cshtml\`
    - Child entity grids with action buttons
- **Crewing examples:**
  - API: \`C:\\source\\BargeOps.Crewing.API\\src\\Crewing.Api\\Controllers\\\`
  - UI: \`C:\\source\\BargeOps.Crewing.UI\\Controllers\\\`
- **Monorepo examples:**
  - API: \`C:\\Dev\\BargeOps.Admin.Mono\\src\\BargeOps.API\\\`
  - UI: \`C:\\Dev\\BargeOps.Admin.Mono\\src\\BargeOps.UI\\\`

---

## Implementation Notes

### Data Access Patterns from Analysis

**Stored Procedures:**
${dataAccessInfo.storedProcedures ? Object.entries(dataAccessInfo.storedProcedures)
	.map(([key, value]) => `- **${key}:** ${value}`)
	.join("\n") : "TBD"}

### UI Components from Analysis

${gridColumns.length > 0 ? `**Grid Columns:**
- **Actions** (first column) - Edit and Delete buttons per row
${gridColumns.map((c: any) => `- ${c.header || c.key} (${c.type || "String"}, width: ${c.width || "auto"})`).join("\n")}

**Button Conversion Pattern:**
- Legacy buttons above grid (Modify, Remove) → Convert to action buttons in grid rows
- Each row has Edit button (navigates to Edit page) and Delete button (confirms and deletes)
- Action buttons use Bootstrap classes: \`btn btn-sm btn-outline-primary\` (Edit) and \`btn btn-sm btn-outline-secondary action-btn\` (Delete)
- Delete buttons use \`data-id\` and \`data-name\` attributes for confirmation dialogs` : ""}

${tabList.length > 0 ? `\n**Tabs:**
${tabList.map((tab: any) => `- **${tab.tabText || tab.tabName}** - ${tab.purpose || "TBD"}`).join("\n")}` : ""}

### Known Gaps (to be filled during implementation)

- TBD during development

${masterPlan ? `
---

## Master Plan Review

**Master Plan Status:** ✅ Found and reviewed

${masterPlan.complexity ? `**Complexity:** ${masterPlan.complexity}` : ""}
${masterPlan.estimatedEffort ? `**Estimated Effort:** ${masterPlan.estimatedEffort}` : ""}

### Master Plan Summary

${masterPlan.forms ? `- **Forms Documented:** ${masterPlan.forms.length} form(s)` : ""}
${masterPlan.childForms ? `- **Child Forms Documented:** ${masterPlan.childForms.length} child form(s)` : ""}
${masterPlan.businessLogicClasses ? `- **Business Logic Classes:** ${masterPlan.businessLogicClasses.length} class(es)` : ""}
${masterPlan.storedProcedures ? `- **Stored Procedures:** ${masterPlan.storedProcedures.length} procedure(s)` : ""}
${masterPlan.apiEndpoints ? `- **API Endpoints:** ${masterPlan.apiEndpoints.length} endpoint(s)` : ""}

### Gaps Analysis

${gaps.length === 0 ? `
✅ **No gaps identified** - Master plan and analysis data are consistent.
` : `
⚠️ **${gaps.length} gap(s) identified** between master plan and analysis data:

${gaps.map((gap, index) => {
	const severityIcon = gap.severity === "high" ? "🔴" : gap.severity === "medium" ? "🟡" : "🟢";
	return `
#### Gap ${index + 1}: ${gap.type.toUpperCase()} - ${gap.item}
${severityIcon} **Severity:** ${gap.severity.toUpperCase()}
**Description:** ${gap.description}
${gap.recommendation ? `**Recommendation:** ${gap.recommendation}` : ""}
`;
}).join("\n")}

**Action Items:**
${gaps.filter(g => g.severity === "high").map((gap, idx) => `${idx + 1}. ${gap.description} - ${gap.recommendation || "Review and resolve"}`).join("\n") || "- No high-priority gaps"}
`}

**Note:** This gaps analysis compares the master plan document against the analysis data generated by the orchestrator. 
Any discrepancies should be reviewed and resolved before implementation begins.
` : `
---

## Master Plan Review

**Master Plan Status:** ⚠️ Not found

No master plan document was found in the output directory. Consider creating a \`${entity}_CONVERSION_MASTER_PLAN.md\` file 
to document the conversion strategy, screen hierarchy, and implementation plan.

**Recommended:** Review existing master plans (e.g., \`FACILITIES_CONVERSION_MASTER_PLAN.md\`, \`VENDOR_CONVERSION_MASTER_PLAN.md\`) 
as templates for creating a comprehensive conversion plan.
`}

${additionalMarkdownFiles.length > 0 ? `
---

## Additional Analysis Documents

The following markdown documents were found in the output directory and contain additional analysis details:

${additionalMarkdownFiles.map((file) => {
	// Extract a title from the first heading or use filename
	const titleMatch = file.content.match(/^#\s+(.+)$/m);
	const title = titleMatch ? titleMatch[1] : file.fileName.replace(/\.md$/, "");
	
	return `### ${title}

**Source:** \`${file.fileName}\`

${file.content}

---`;
}).join("\n\n")}
` : ""}

### Decisions Made

- TBD during development

### Risks

- TBD during development

---

## Completion Checklist

Use this checklist to track implementation progress:

### Phase 1: Shared DTOs ✅
- [ ] All DTOs created in BargeOps.Shared
- [ ] Properties match analysis
- [ ] Child entity DTOs created

### Phase 2: API Layer 🔄
- [ ] Repository interface defined
- [ ] Repository implementation complete
- [ ] All SQL queries created
- [ ] Service interface defined
- [ ] Service implementation complete
- [ ] FluentValidation rules added
- [ ] Controller created
- [ ] All endpoints working

### Phase 3: UI Layer ⏳
- [ ] All ViewModels created
- [ ] Search controller complete
- [ ] All views created
- [ ] JavaScript for DataTables
- [ ] JavaScript for inline editing (if applicable)

### Phase 4: Testing & Quality ⏳
- [ ] Manual testing complete
- [ ] All business rules verified
- [ ] All validation rules verified
- [ ] Security/authorization verified
- [ ] Code review complete
- [ ] Documentation updated

---

**Status:** Generated
**Last Updated:** ${date}
`;
}

function generateQualityChecklist(data: AnalysisData): string {
	const { entity, businessLogic } = data;
	const properties = businessLogic?.properties || [];
	const businessRules = businessLogic?.businessRules || [];
	const childEntities = businessLogic?.childEntities || {};

	return `# ${entity} Quality Checklist

## Shared DTOs

- [ ] ${entity}Dto.cs has all ${properties.length} properties
- [ ] ${entity}SearchRequest.cs created
${Object.keys(childEntities).map((key) => `- [ ] ${key}Dto.cs created`).join("\n")}
- [ ] All properties have correct types
- [ ] All properties have correct nullability
- [ ] Required properties marked with [Required]
- [ ] MaxLength attributes where applicable

## API Layer

### Repository
- [ ] I${entity}Repository interface defined
- [ ] ${entity}Repository implements interface
- [ ] All SQL files created and embedded
- [ ] Connection management correct
- [ ] Async patterns used throughout

### Service
- [ ] I${entity}Service interface defined
- [ ] ${entity}Service implements interface
- [ ] All ${businessRules.length} business rules implemented
- [ ] FluentValidation configured
- [ ] Transaction support for child entities
- [ ] Error handling in place

### Controller
- [ ] ${entity}Controller inherits ApiControllerBase
- [ ] All CRUD endpoints present
- [ ] Authorization attributes applied
- [ ] HTTP status codes correct
- [ ] DTOs used (not domain models)

## UI Layer

### ViewModels
- [ ] ${entity}SearchViewModel created
- [ ] ${entity}EditViewModel created
- [ ] ${entity}DetailsViewModel created
- [ ] ${entity}ListItemModel created
${Object.keys(childEntities).map((key) => `- [ ] ${key}ViewModel created`).join("\n")}
- [ ] All SelectListItems populated
- [ ] Validation attributes applied
- [ ] Display attributes for formatting

### Controllers
- [ ] ${entity}SearchController inherits AppController
- [ ] Index action returns view
- [ ] Search action returns JSON for DataTables
- [ ] Create/Edit actions handle GET/POST
- [ ] Delete action confirms and deletes
- [ ] No ViewBag/ViewData used (MVVM only)

### Views
- [ ] Index.cshtml renders search page
- [ ] Edit.cshtml renders form with tabs
- [ ] Details.cshtml renders read-only view
- [ ] All partials created
- [ ] Bootstrap 5 classes used
- [ ] Proper model binding

### JavaScript
- [ ] DataTables initialized correctly
- [ ] Server-side processing configured
- [ ] **Action buttons column** (first column) with Edit/Delete buttons in rows
- [ ] Column definitions match grid (Actions first, then data columns)
- [ ] Search filters wired up
- [ ] **DataTables Buttons extension** configured for export
- [ ] Export buttons (Excel, CSV) work correctly
- [ ] Form validation works
- [ ] AJAX submit handlers work
- [ ] Delete button handlers use \`$(document).on('click', '.action-btn', ...)\` pattern

## Business Rules

${businessRules.map((rule: any, index: number) =>
	`- [ ] **${rule.ruleName}** - ${rule.message}`
).join("\n")}

## Security

- [ ] Authorization checked on all controller actions
- [ ] IdentityConstants.ApplicationScheme used
- [ ] Button permissions enforced
- [ ] No unauthorized access possible

## Code Quality

- [ ] No compiler warnings
- [ ] ID fields uppercase (not Id)
- [ ] Comments only where logic is complex
- [ ] Follows .cursorrules conventions
- [ ] Async/await used correctly
- [ ] No magic strings

## Testing

- [ ] Search functionality tested
- [ ] Create/Edit tested
- [ ] Delete tested
- [ ] Validation tested (client & server)
- [ ] Business rules verified
- [ ] Child entity CRUD tested
- [ ] Tab navigation tested

## Documentation

- [ ] README updated (if needed)
- [ ] Implementation notes documented
- [ ] Any deviations from spec noted
`;
}

function generateTaskFiles(data: AnalysisData, tasksDir: string): void {
	const { entity } = data;

	const tasks = [
		{
			file: "01-shared-dtos.md",
			title: "Create Shared DTOs",
			content: `# Task 1: Create Shared DTOs

**Location:** \`src/BargeOps.Shared/BargeOps.Shared/Dto/\`

## Files to Create

1. **${entity}Dto.cs** - Main DTO
2. **${entity}SearchRequest.cs** - Search criteria
3. Child entity DTOs (if applicable)

## Steps

1. Create ${entity}Dto.cs with all properties from analysis
2. Add data annotations ([Required], [MaxLength], etc.)
3. Create ${entity}SearchRequest.cs with search filters
4. Create child entity DTOs
5. Build and verify no errors

## Success Criteria

- [ ] All DTOs compile without errors
- [ ] Properties match business-logic.json
- [ ] Namespace is BargeOps.Shared.Dto
- [ ] ID fields are uppercase

## Next Task

Task 2: API Repository
`,
		},
		{
			file: "02-api-repository.md",
			title: "Create API Repository",
			content: `# Task 2: Create API Repository

**Location:** \`src/BargeOps.API/\`

## Files to Create

1. **I${entity}Repository.cs** - Interface in Admin.Infrastructure/Abstractions
2. **${entity}Repository.cs** - Implementation in Admin.Infrastructure/Repositories
3. SQL files in Admin.Infrastructure/DataAccess/Sql/${entity}/

## Steps

1. Define repository interface
2. Implement repository using Dapper
3. Create all SQL query files
4. Configure SQL files as embedded resources
5. Test repository methods

## Success Criteria

- [ ] Interface defined with all CRUD methods
- [ ] Implementation uses Dapper
- [ ] All SQL files created
- [ ] Async/await patterns used
- [ ] No errors

## Next Task

Task 3: API Service
`,
		},
		{
			file: "03-api-service.md",
			title: "Create API Service",
			content: `# Task 3: Create API Service

**Location:** \`src/BargeOps.API/\`

## Files to Create

1. **I${entity}Service.cs** - Interface in Admin.Api/Interfaces
2. **${entity}Service.cs** - Implementation in Admin.Api/Services
3. **${entity}Validator.cs** - FluentValidation in Admin.Api/Validators

## Steps

1. Define service interface
2. Implement service with business logic
3. Create FluentValidation validator
4. Implement all business rules from analysis
5. Add transaction support for child entities

## Success Criteria

- [ ] Service interface defined
- [ ] Business logic implemented
- [ ] All validation rules in validator
- [ ] Transaction support works
- [ ] No errors

## Next Task

Task 4: API Controller
`,
		},
		{
			file: "04-api-controller.md",
			title: "Create API Controller",
			content: `# Task 4: Create API Controller

**Location:** \`src/BargeOps.API/src/Admin.Api/Controllers/\`

## Files to Create

1. **${entity}Controller.cs** - REST API controller

## Steps

1. Create controller inheriting ApiControllerBase
2. Inject I${entity}Service
3. Implement all CRUD endpoints
4. Add authorization attributes
5. Test all endpoints

## Success Criteria

- [ ] Controller inherits ApiControllerBase
- [ ] All endpoints return proper status codes
- [ ] Authorization attributes applied
- [ ] Swagger documentation works
- [ ] Endpoints tested

## Next Task

Task 5: UI ViewModels
`,
		},
		{
			file: "05-ui-viewmodels.md",
			title: "Create UI ViewModels",
			content: `# Task 5: Create UI ViewModels

**Location:** \`src/BargeOps.UI/Models/\`

## Files to Create

1. **${entity}SearchViewModel.cs**
2. **${entity}EditViewModel.cs**
3. **${entity}DetailsViewModel.cs**
4. **${entity}ListItemModel.cs**
5. Child entity ViewModels (if applicable)

## Steps

1. Create all ViewModel classes
2. Add display and validation attributes
3. Add SelectListItem properties for dropdowns
4. Map from DTOs to ViewModels
5. No ViewBag/ViewData usage

## Success Criteria

- [ ] All ViewModels created
- [ ] MVVM pattern followed
- [ ] Validation attributes match business rules
- [ ] Display attributes for formatting
- [ ] No errors

## Next Task

Task 6: UI Views
`,
		},
		{
			file: "06-ui-views.md",
			title: "Create UI Views",
			content: `# Task 6: Create UI Views

**Location:** \`src/BargeOps.UI/Views/${entity}Search/\`

## Files to Create

1. **Index.cshtml** - Search page
2. **Edit.cshtml** - Create/Edit form
3. **Details.cshtml** - Details view
4. **_SearchFilters.cshtml** - Search filters partial
5. **_SearchResults.cshtml** - DataTables grid partial
6. Tab partials (if applicable)

## Steps

1. Create all Razor views
2. Use Bootstrap 5 components
3. Implement tabs (if applicable)
4. Add form validation markup
5. Wire up ViewModels

## Success Criteria

- [ ] All views render correctly
- [ ] Bootstrap 5 styling applied
- [ ] Form validation visible
- [ ] Tabs work (if applicable)
- [ ] No errors

## Next Task

Task 7: JavaScript and DataTables
`,
		},
		{
			file: "07-javascript-datatables.md",
			title: "Create JavaScript and DataTables",
			content: `# Task 7: Create JavaScript and DataTables

**Location:** \`src/BargeOps.UI/wwwroot/js/\`

## Files to Create

1. **${entity.toLowerCase()}Search.js** - DataTables configuration
2. **${entity.toLowerCase()}Detail.js** - Form and tab management (if needed)

## Steps

1. **Include DataTables Buttons extension** in view:
   \`\`\`html
   <link rel="stylesheet" href="https://cdn.datatables.net/buttons/3.2.6/css/buttons.bootstrap5.min.css">
   <script src="https://cdn.datatables.net/buttons/3.2.6/js/dataTables.buttons.min.js"></script>
   <script src="https://cdn.datatables.net/buttons/3.2.6/js/buttons.bootstrap5.min.js"></script>
   <script src="https://cdn.datatables.net/buttons/3.2.6/js/buttons.html5.min.js"></script>
   \`\`\`

2. **Initialize DataTables** with server-side processing and action buttons column:
   \`\`\`javascript
   var columns = [
       {
           data: null,
           sortable: false,
           className: "text-center",
           width: "110px",
           render: function (data, type, full) {
               var id = full?.${entity.toLowerCase()}ID || 0;
               var name = escapeHtml(full?.name || "");
               var returnUrl = encodeURIComponent(window.location.href);
               var editUrl = "/${entity}Search/Edit/" + id + "?returnUrl=" + returnUrl;
               return '<a class="btn btn-sm btn-outline-primary me-1" href="' + editUrl + '" title="Edit">' +
                   '<i class="fas fa-pencil-alt"></i></a>' +
                   '<button class="btn btn-sm btn-outline-secondary action-btn" data-id="' + id + '" data-name="' + name + '" title="Delete">' +
                   '<i class="fas fa-trash"></i></button>';
           }
       },
       // ... other columns
   ];
   \`\`\`

3. **Configure DataTables** with Buttons extension:
   \`\`\`javascript
   var dataTable = $("#${entity.toLowerCase()}Table").DataTable({
       processing: true,
       serverSide: true,
       dom: 'Bfrtip',  // B = Buttons toolbar
       buttons: [
           {
               extend: 'excelHtml5',
               text: '<i class="fas fa-file-excel"></i> Export',
               className: 'btn btn-sm btn-success',
               exportOptions: {
                   columns: ':visible:not(:first-child)'  // Exclude action column
               }
           },
           {
               extend: 'csvHtml5',
               text: '<i class="fas fa-file-csv"></i> CSV',
               className: 'btn btn-sm btn-info',
               exportOptions: {
                   columns: ':visible:not(:first-child)'  // Exclude action column
               }
           }
       ],
       // ... other options
   });
   \`\`\`

4. **Wire up search filters** to trigger DataTables reload

5. **Handle Delete button clicks**:
   \`\`\`javascript
   $(document).on("click", ".action-btn", function () {
       var id = $(this).data("id");
       var name = $(this).data("name");
       // Show confirmation and delete
   });
   \`\`\`

6. Implement inline editing for child entities (if applicable)

7. Test all functionality

## Important Notes

- **Action buttons MUST be in grid rows**, not above the grid
- Legacy "Modify" and "Remove" buttons above the grid should be converted to Edit/Delete buttons in each row
- Use DataTables Buttons extension for export, not custom export buttons
- Action column should be first column and excluded from exports

## Success Criteria

- [ ] DataTables loads data correctly
- [ ] Server-side processing works
- [ ] **Action buttons column** appears as first column with Edit/Delete in each row
- [ ] Search filters functional
- [ ] **DataTables Buttons export** works (Excel and CSV)
- [ ] Export excludes action column
- [ ] Delete button handlers work correctly
- [ ] Inline editing works (if applicable)
- [ ] No JavaScript errors

## Next Task

Testing and Quality Review
`,
		},
	];

	for (const task of tasks) {
		fs.writeFileSync(path.join(tasksDir, task.file), task.content);
	}
}
