# Complete Conversion Workflow

This document outlines the complete workflow from initial analysis to deployed code in the MonoRepo.

## Prerequisites

1. **Install dependencies:**
   ```bash
   cd C:\source\agents\ClaudeOnshoreConversionAgent
   bun install
   ```

2. **Configure paths in `config.json`:**
   - `inputDirectory`: Path to OnShore legacy VB.NET codebase
   - `targetProjects`: Paths to MonoRepo projects (Shared, API, UI)
   - `referenceProjects`: Paths to reference examples (Crewing API/UI)

## Complete Workflow Steps

### Phase 1: Analysis (Steps 1-10) - Automated

**Run the orchestrator to extract all analysis data:**

```bash
# Option 1: With entity and form name
bun run agents/orchestrator.ts --entity "Vendor" --form-name "frmVendorSearch"

# Option 2: With form name only (entity extracted automatically)
bun run agents/orchestrator.ts --form-name "frmVendorSearch"

# Option 3: With entity only (form names constructed automatically)
bun run agents/orchestrator.ts --entity "Vendor"

# Option 4: Interactive - select from available forms
bun run agents/orchestrator.ts
```

**What happens:**
- Steps 1-10 run automatically in sequence
- Each step extracts specific information from legacy code
- All analysis files saved to `output/{Entity}/`
- Status tracked in `output/{Entity}/conversion-status.json`

**Analysis Steps:**

| Step | Agent | Purpose | Output File |
|------|-------|---------|-------------|
| 1 | Form Structure Analyzer (Search) | Extract search form UI components | `form-structure-search.json` |
| 2 | Form Structure Analyzer (Detail) | Extract detail form UI components | `form-structure-detail.json` |
| 3 | Business Logic Extractor | Extract business rules and validation | `business-logic.json` |
| 4 | Data Access Pattern Analyzer | Extract stored procedures and queries | `data-access.json` |
| 5 | Security & Authorization Extractor | Extract permissions and authorization | `security.json` |
| 6 | UI Component Mapper | Map legacy controls to modern equivalents | `ui-mapping.json` |
| 7 | Form Workflow Analyzer | Extract user flows and state management | `workflow.json` |
| 8 | Detail Form Tab Analyzer | Extract tab structure and related entities | `tabs.json` |
| 9 | Validation Rule Extractor | Extract all validation logic | `validation.json` |
| 10 | Related Entity Analyzer | Extract entity relationships | `related-entities.json` |

**Output Location:**
```
output/{Entity}/
├── form-structure-search.json
├── form-structure-detail.json
├── business-logic.json
├── data-access.json
├── security.json
├── ui-mapping.json
├── workflow.json
├── tabs.json
├── validation.json
├── related-entities.json
├── conversion-status.json    ← Tracks progress, timestamps, failures
└── child-forms.json          ← If child forms detected
```

**Resume/Rerun Options:**
```bash
# Resume from where you left off (runs pending + failed steps)
bun run agents/orchestrator.ts --entity "Vendor" --resume

# Rerun only failed steps
bun run agents/orchestrator.ts --entity "Vendor" --rerun-failed

# Skip specific steps (if already done)
bun run agents/orchestrator.ts --entity "Vendor" --skip-steps "1,2,3"
```

---

### Phase 2: Template Generation (Step 11) - Interactive

**Generate conversion plan and code templates:**

```bash
bun run generate-template --entity "Vendor"
# or
bun run agents/conversion-template-generator.ts --entity "Vendor"
```

**What happens:**
- Launches Claude Code in interactive mode
- Reviews all analysis files from Phase 1
- Generates comprehensive conversion plan
- Creates code templates organized by project
- Offers to generate ViewModels interactively

**Generated Output:**
```
output/{Entity}/
├── conversion-plan.md         ← Master conversion plan document
└── templates/
    ├── shared/                ← BargeOps.Shared DTOs (create FIRST!)
    │   └── Dto/
    │       ├── {Entity}Dto.cs
    │       ├── {Entity}SearchRequest.cs
    │       └── {Child}Dto.cs
    ├── api/                   ← BargeOps.Admin.API templates
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
    └── ui/                    ← BargeOps.Admin.UI templates
        ├── Controllers/
        │   └── {Entity}Controller.cs
        ├── Services/
        │   ├── I{Entity}Service.cs
        │   └── {Entity}Service.cs
        ├── ViewModels/
        │   ├── {Entity}SearchViewModel.cs
        │   ├── {Entity}EditViewModel.cs
        │   ├── {Entity}DetailsViewModel.cs
        │   └── {Entity}ListItemViewModel.cs
        ├── Views/
        │   └── {Entity}/
        │       ├── Index.cshtml
        │       └── Edit.cshtml
        └── wwwroot/
            └── js/
                ├── {entity}-search.js
                └── {entity}-detail.js
```

**Note:** You can rerun template generation multiple times without re-running analysis.

---

### Phase 3: Review Templates

**Review the generated conversion plan:**
```bash
# Open the conversion plan
code output/{Entity}/conversion-plan.md
```

**Check:**
- ✅ All phases are included (Foundation, Service, Presentation, Template Deployment, Testing)
- ✅ Task sequencing is logical with proper dependencies
- ✅ Code templates use correct patterns
- ✅ References point to existing examples
- ✅ Testing requirements are comprehensive

**Review template files:**
- Check namespaces match target project structure
- Verify file paths and directory structure
- Ensure patterns match reference examples

---

### Phase 4: Deploy Templates to MonoRepo

**Preview deployment (dry run):**
```bash
bun run deploy-templates --entity "Vendor" --dry-run
```

**Deploy templates:**
```bash
bun run deploy-templates --entity "Vendor"
```

**What happens:**
- Copies Shared DTOs → `BargeOps.Shared/Dto/`
- Copies API templates → `BargeOps.Admin.API/` (Controllers, Repositories, Services, Mapping)
- Copies UI templates → `BargeOps.Admin.UI/` (Controllers, Services, ViewModels, Views, JavaScript)
- Preserves directory structure
- Creates missing directories automatically

**Manual Alternative:**
If you prefer manual copy, follow Phase 6 steps in the conversion plan:
- Copy `templates/shared/Dto/*.cs` → `BargeOps.Shared/Dto/`
- Copy `templates/api/*` → `BargeOps.Admin.API/` (matching subdirectories)
- Copy `templates/ui/*` → `BargeOps.Admin.UI/` (matching subdirectories)

---

### Phase 5: Post-Deployment Verification

**After deployment, verify and adjust:**

1. **Verify namespaces:**
   - Shared DTOs: `BargeOps.Shared.Dto`
   - API: `BargeOps.Admin.API.*` (matching project structure)
   - UI: `BargeOps.Admin.UI.*` (matching project structure)

2. **Check project references:**
   - API project references Shared project
   - UI project references Shared and API projects
   - All using statements are correct

3. **Verify file paths:**
   - Embedded resources (SQL files) have correct paths
   - View paths match controller actions
   - JavaScript file references are correct

4. **Add DI registration:**
   - Register repositories in `Startup.cs` or `Program.cs`
   - Register services in `Startup.cs` or `Program.cs`
   - Register API clients in UI `Startup.cs` or `Program.cs`

---

### Phase 6: Implementation (Follow Conversion Plan)

**Follow the conversion plan phases in order:**

#### Phase 1: Foundation (Days 1-2)
- [ ] Create Domain Model (if needed)
- [ ] Create DTOs in BargeOps.Shared
- [ ] Create Repository Interface
- [ ] Implement Dapper Repository with SQL embedded resources
- [ ] Create SQL files (marked as embedded resources)

#### Phase 2: Service Layer (Day 3)
- [ ] Create Service Interface
- [ ] Implement Service with business logic
- [ ] Create FluentValidation Validator
- [ ] Register in DI container

#### Phase 3: ViewModels (Day 4)
- [ ] Create Search ViewModel (with SelectListItem for dropdowns)
- [ ] Create Edit ViewModel (with validation attributes, single DateTime property)
- [ ] Create Details ViewModel (if needed)
- [ ] Create ListItem ViewModel (if needed)

#### Phase 4: Controller (Day 5)
- [ ] Create MVC Controller (inherit from AppController, use [Authorize])
- [ ] Implement actions (Index, Create, Edit, Delete/SetActive)
- [ ] Add authorization policies

#### Phase 5: Views (Days 6-7)
- [ ] Create Search View (Index.cshtml with DataTables)
- [ ] Create Edit View (with DateTime split inputs, Bootstrap tabs if needed)
- [ ] Create JavaScript (DataTables initialization, Select2, DateTime split/combine)
- [ ] Add CSS styling if needed

#### Phase 6: Testing (Day 8)
- [ ] Unit Tests (service layer, validation)
- [ ] Integration Tests (API endpoints, database)
- [ ] Manual Testing Checklist

---

### Phase 7: Optional - Interactive Implementation Help

**Use interactive agents for targeted help during implementation:**

```bash
# Plan the conversion with dependency management
bun run plan-conversion --entity "Vendor"
# Creates: .claude/tasks/Vendor_IMPLEMENTATION_STATUS.md

# Get help implementing entities with Dapper
bun run entity-convert --entity "Vendor"
bun run entity-convert --entity "Vendor" "Add auditing fields"

# Get help creating ViewModels
bun run viewmodel-create --entity "Vendor" --form-type Search
bun run viewmodel-create --entity "Vendor" --form-type Detail
```

---

## Quick Reference Commands

### Analysis
```bash
# Full analysis (all 10 steps)
bun run agents/orchestrator.ts --entity "Vendor"

# Resume from failure
bun run agents/orchestrator.ts --entity "Vendor" --resume

# Rerun failed steps only
bun run agents/orchestrator.ts --entity "Vendor" --rerun-failed

# Skip specific steps
bun run agents/orchestrator.ts --entity "Vendor" --skip-steps "1,2,3"
```

### Template Generation
```bash
# Generate templates (interactive)
bun run generate-template --entity "Vendor"
```

### Deployment
```bash
# Preview deployment
bun run deploy-templates --entity "Vendor" --dry-run

# Deploy templates
bun run deploy-templates --entity "Vendor"
```

### Implementation Help
```bash
# Plan conversion
bun run plan-conversion --entity "Vendor"

# Entity implementation help
bun run entity-convert --entity "Vendor"

# ViewModel creation help
bun run viewmodel-create --entity "Vendor" --form-type Search
```

### Individual Agents (for debugging)
```bash
# Run specific agent interactively
bun run agents/form-structure-analyzer.ts --entity "Vendor" --form-type "Search" --interactive
bun run agents/business-logic-extractor.ts --entity "Vendor" --interactive
```

---

## Workflow Summary

```
1. Analysis (Automated)
   └─> bun run agents/orchestrator.ts --entity "Vendor"
       └─> Generates: output/Vendor/*.json

2. Template Generation (Interactive)
   └─> bun run generate-template --entity "Vendor"
       └─> Generates: output/Vendor/conversion-plan.md
       └─> Generates: output/Vendor/templates/

3. Review Templates
   └─> Review conversion-plan.md
   └─> Check template files

4. Deploy Templates (Automated)
   └─> bun run deploy-templates --entity "Vendor" --dry-run  (preview)
   └─> bun run deploy-templates --entity "Vendor"          (deploy)
       └─> Copies to MonoRepo projects

5. Post-Deployment Verification
   └─> Verify namespaces
   └─> Check project references
   └─> Add DI registration

6. Implementation (Manual)
   └─> Follow conversion plan phases
   └─> Use interactive agents for help

7. Testing
   └─> Unit tests
   └─> Integration tests
   └─> Manual testing
```

---

## Troubleshooting

### Analysis Fails
```bash
# Check status
cat output/{Entity}/conversion-status.json

# Rerun failed steps
bun run agents/orchestrator.ts --entity "Vendor" --rerun-failed

# Run specific agent interactively to debug
bun run agents/{agent-name}.ts --entity "Vendor" --interactive
```

### Templates Not Generated
- Ensure analysis completed successfully
- Check `output/{Entity}/` has all JSON files
- Rerun template generator: `bun run generate-template --entity "Vendor"`

### Deployment Fails
- Verify `config.json` paths are correct
- Check target directories exist in MonoRepo
- Use `--dry-run` to preview first

### Implementation Issues
- Use interactive agents for targeted help
- Review reference examples in Crewing projects
- Check conversion plan for guidance

---

## Best Practices

1. **Always start with orchestrator** - Runs all agents in correct order
2. **Review conversion plan** - Understand the full scope before implementing
3. **Use dry-run first** - Preview deployment before copying files
4. **Follow implementation order** - Shared DTOs → API → UI
5. **Use interactive agents** - Get targeted help during implementation
6. **Test incrementally** - Test each phase before moving to next
7. **Reference examples** - Use Crewing and Facility examples as guides

---

## File Locations

### Agent Project
- Analysis output: `output/{Entity}/`
- Templates: `output/{Entity}/templates/`
- Status: `output/{Entity}/conversion-status.json`

### MonoRepo (Target)
- Shared DTOs: `src/BargeOps.Shared/BargeOps.Shared/Dto/`
- API: `src/BargeOps.API/`
- UI: `src/BargeOps.UI/`

### Reference Examples
- Crewing API: `C:\source\BargeOps.Crewing.API`
- Crewing UI: `C:\source\BargeOps.Crewing.UI`
- Facility/BoatLocation: In MonoRepo









