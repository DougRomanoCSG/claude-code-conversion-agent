# SpecKit Integration Proposal

## Problem Statement

Templates generated in `ClaudeOnshoreConversionAgent/output/{Entity}/templates/` become stale and hard to maintain. Copying them to the main repo creates synchronization issues.

## Solution

Use SpecKit to create specifications in the main repo (`BargeOps.Admin.Mono`) that drive implementation directly. Agent analysis informs the spec, but implementation happens against the spec, not templates.

## Workflow

### 1. Agent Analysis Phase (ClaudeOnshoreConversionAgent)

```bash
cd C:\source\agents\ClaudeOnshoreConversionAgent
bun run agents/orchestrator.ts --entity Vendor --create-spec
```

**Outputs:**
- Analysis JSON files in `output/Vendor/` (for reference)
- SpecKit spec in `C:\Dev\BargeOps.Admin.Mono\.speckit/entities/Vendor/spec.md`

### 2. Implementation Phase (BargeOps.Admin.Mono)

```bash
cd C:\Dev\BargeOps.Admin.Mono
spec-kit plan --entity Vendor    # Creates plan.md and tasks/
claude code                       # Implement against tasks
```

**Implements:**
- Shared DTOs in `src/BargeOps.Shared/`
- API components in `src/BargeOps.API/`
- UI components in `src/BargeOps.UI/`

### 3. Quality Review Phase (BargeOps.Admin.Mono)

```bash
spec-kit review --entity Vendor   # Compare spec vs implementation
# Creates quality-review.md with gaps
```

**Outputs:**
- Gap analysis (what's missing vs spec)
- Additional tasks for gaps
- Quality checklist results

### 4. Gap Closure Phase (BargeOps.Admin.Mono)

```bash
# Add gap tasks to .speckit/entities/Vendor/tasks/
# Implement gaps
# Mark spec as complete
spec-kit complete --entity Vendor
```

## Spec Structure

### `.speckit/entities/{Entity}/spec.md`

```markdown
# Specification: {Entity} Conversion

## Analysis Source
Generated from: ClaudeOnshoreConversionAgent analysis on {date}
Forms analyzed:
- frm{Entity}Search.vb
- frm{Entity}Detail.vb
Business object: {Entity}.vb

## Scope

### In Scope
- [ ] Search functionality (grid with filters)
- [ ] Detail form with {N} tabs
- [ ] CRUD operations (Create, Read, Update, Delete)
- [ ] Related entities: {list}
- [ ] Security: {subsystem} permissions
- [ ] Validation: {count} business rules

### Out of Scope
- Legacy VB.NET modifications
- Database schema changes
- Stored procedure rewrites

## Architecture

### Shared Layer (Create First!)
**Location:** `src/BargeOps.Shared/BargeOps.Shared/Dto/`

- [ ] `{Entity}Dto.cs` - Main DTO with {N} properties
- [ ] `{Entity}SearchRequest.cs` - Search criteria
- [ ] `{Child}Dto.cs` - Related entity DTOs (if applicable)

**Key Properties from Analysis:**
{list key properties from business-logic.json}

### API Layer
**Location:** `src/BargeOps.API/`

- [ ] `Controllers/{Entity}Controller.cs` - REST API endpoints
- [ ] `Services/{Entity}Service.cs` - Business logic
- [ ] `Repositories/I{Entity}Repository.cs` - Interface
- [ ] `Repositories/{Entity}Repository.cs` - Dapper implementation

**Key Endpoints:**
- GET /api/{entity} - Search/list
- GET /api/{entity}/{id} - Get by ID
- POST /api/{entity} - Create
- PUT /api/{entity}/{id} - Update
- DELETE /api/{entity}/{id} - Delete

**Data Access Patterns from Analysis:**
{list SPs and queries from data-access.json}

### UI Layer
**Location:** `src/BargeOps.UI/`

- [ ] `Controllers/{Entity}SearchController.cs` - MVC controller
- [ ] `Models/{Entity}SearchViewModel.cs` - Search ViewModel
- [ ] `Models/{Entity}EditViewModel.cs` - Edit ViewModel
- [ ] `Models/{Entity}DetailsViewModel.cs` - Details ViewModel
- [ ] `Views/{Entity}Search/Index.cshtml` - Search page
- [ ] `Views/{Entity}Search/Edit.cshtml` - Edit page
- [ ] `Views/{Entity}Search/Details.cshtml` - Details page
- [ ] `wwwroot/js/{entity}Search.js` - DataTables config

**UI Components from Analysis:**
{list controls from form-structure.json}

## Success Criteria

### Functional Requirements
- [ ] Search form matches legacy behavior
  - Grid columns: {list from FormatGridColumns}
  - Search filters: {list from AddFetchParameters}
  - Sorting and pagination work
- [ ] Detail form includes all fields
  - Tabs: {list from tabs.json}
  - Related entities editable
  - Validation rules enforced
- [ ] CRUD operations work correctly
- [ ] Security/authorization preserved
  - Subsystem: {from security.json}
  - Button permissions: {list}

### Technical Requirements
- [ ] All DTOs in BargeOps.Shared namespace
- [ ] Repository uses Dapper with .sql files
- [ ] Service layer has business logic
- [ ] ViewModels use MVVM (no ViewBag/ViewData)
- [ ] Bootstrap 5 styling
- [ ] DataTables server-side processing
- [ ] Select2 for dropdowns
- [ ] Proper async/await patterns

### Quality Gates
- [ ] No compiler errors or warnings
- [ ] Business rules from CheckBusinessRules implemented
- [ ] Validation from AreFieldsValid implemented
- [ ] Security from SetButtonTypes implemented
- [ ] Related entities from child grids implemented
- [ ] Code follows .cursorrules conventions
- [ ] ID fields uppercase (not Id)
- [ ] IdentityConstants.ApplicationScheme used

## Validation Rules (from analysis)

{extract from validation.json}

## Business Rules (from analysis)

{extract from business-logic.json}

## Security Requirements (from analysis)

{extract from security.json}

## Dependencies

### Prerequisites
- [ ] BargeOps.Shared.Dto namespace exists
- [ ] Database tables/SPs exist
- [ ] Bootstrap 5 and DataTables configured
- [ ] Authentication/authorization setup

### Related Entities
{from related-entities.json}

## Reference Implementations
- Similar entity: {suggest similar completed entity}
- Crewing example: CrewingController.cs, CrewingSearchController.cs
- Location: `C:\source\BargeOps.Crewing.API` and `.UI`

## Implementation Notes

### Known Gaps (to be filled during implementation)
- TBD during development

### Decisions Made
- TBD during development

### Risks
- TBD during development
```

## Benefits

1. **Single Source of Truth**: Spec lives in main repo where implementation happens
2. **No Template Sync**: Implementation references spec directly
3. **Quality Built-In**: Spec defines "done" with checkboxes
4. **Gap Tracking**: Compare implementation vs spec, not template vs implementation
5. **Scalable**: 20+ entities all follow same spec structure
6. **Onboarding**: New devs read spec to understand what to build

## Agent Changes Required

### New orchestrator flag: `--create-spec`

```typescript
// In agents/orchestrator.ts
if (parsedArgs['create-spec']) {
  const specPath = path.join(
    config.targetProjects.monorepo,
    '.speckit/entities',
    entity,
    'spec.md'
  );

  await generateSpecFromAnalysis({
    entity,
    analysisData: {
      formStructure: formStructureData,
      businessLogic: businessLogicData,
      dataAccess: dataAccessData,
      security: securityData,
      validation: validationData,
      relatedEntities: relatedEntitiesData,
      tabs: tabsData,
    },
    outputPath: specPath,
  });

  console.log(`✓ Spec created: ${specPath}`);
  console.log(`Next: cd to BargeOps.Admin.Mono and run: spec-kit plan --entity ${entity}`);
}
```

### New utility: `lib/spec-generator.ts`

Transforms analysis JSON into SpecKit spec markdown.

## Implementation Plan

### Phase 1: Pilot with One Entity
1. Install SpecKit in main repo
2. Create spec template
3. Modify orchestrator to generate spec
4. Test with Vendor entity
5. Validate workflow

### Phase 2: Standardize
1. Refine spec template based on pilot
2. Document process
3. Create quality-review checklist
4. Update agent README

### Phase 3: Scale
1. Generate specs for all remaining entities
2. Track completion in .speckit/status.md
3. Deprecate template generation

## Questions to Resolve

1. Should specs be in `.speckit/` or `docs/conversions/`?
2. Who runs quality reviews - automated or manual?
3. How do we handle spec updates when requirements change?
4. Should we create specs for already-converted entities retroactively?
