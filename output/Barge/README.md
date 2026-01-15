# Barge Entity - Conversion Templates

## Overview

This directory contains complete conversion templates for the **Barge** entity from the legacy OnShore VB.NET application to the modern BargeOps.Admin.Mono stack.

**Generated:** 2025-12-17
**Complexity:** Very High
**Estimated Effort:** 10-15 days
**Priority:** High

## Directory Structure

```
Barge/
├── conversion-plan.md              # Comprehensive conversion plan (START HERE!)
├── README.md                       # This file
├── GAP_ANALYSIS.md                 # Optional: gaps vs templates
├── MASTER_BARGE_SCREENS.md         # Barge screen inventory
├── TEMPLATES_GENERATED.md          # Template generation notes
│
├── templates/
│   ├── shared/                      # ⭐ BargeOps.Shared project (CREATE FIRST!)
│   │   └── Dto/
│   │       ├── BargeDto.cs          # Complete entity DTO (80+ properties)
│   │       ├── BargeSearchRequest.cs
│   │       ├── BargeSearchResultDto.cs (58 columns)
│   │       └── BargeCharterDto.cs
│   │
│   ├── api/                         # BargeOps.API project
│   │   ├── Repositories/
│   │   │   ├── IBargeRepository.cs
│   │   │   └── BargeRepository.cs   # Dapper with direct SQL
│   │   ├── Services/
│   │   │   ├── IBargeService.cs
│   │   │   └── BargeService.cs      # Business logic + validation
│   │   └── Controllers/
│   │       └── BargeController.cs   # RESTful API endpoints
│   │
│   └── ui/                          # BargeOps.UI project
│       ├── ViewModels/
│       │   ├── BargeSearchViewModel.cs
│       │   └── BargeEditViewModel.cs
│       ├── Services/
│       │   ├── IBargeService.cs
│       │   └── BargeService.cs      # HTTP client to API
│       ├── Controllers/
│       │   └── BargeSearchController.cs
│       ├── Views/
│       │   └── Index.cshtml         # Search + DataTables grid
│       └── wwwroot/
│           └── js/
│               ├── barge-search.js  # DataTables + search logic
│               └── barge-edit.js    # Form logic + validations
│
└── *.json                           # Analysis outputs (in this folder)
    ├── business-logic.json
    ├── data-access.json
    ├── form-structure-search.json
    ├── form-structure-detail.json
    ├── security.json
    ├── ui-mapping.json
    ├── workflow.json
    ├── tabs.json
    ├── validation.json
    └── related-entities.json
```

**Note:** You may also see `Barge_*.json` files. Those are task-sync copies; prefer the non-prefixed `*.json` files listed above.

## Implementation Order

### Phase 1: SHARED Project (⭐ START HERE!)

**Target:** `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared`

1. Copy DTOs to `Dto/` folder:
   - BargeDto.cs
   - BargeSearchRequest.cs
   - BargeSearchResultDto.cs
   - BargeCharterDto.cs

2. **CRITICAL:** These DTOs are the ONLY data models
   - NO separate Models/ folder!
   - Used directly by BOTH API and UI
   - No AutoMapper needed!

### Phase 2: API Infrastructure

**Target:** `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API`

1. **Create SQL Files** (`src/Admin.Infrastructure/DataAccess/Sql/`):
   - Barge_GetById.sql
   - Barge_Search.sql (complex with boat/facility/ship filters)
   - Barge_Insert.sql
   - Barge_Update.sql
   - Barge_SetActive.sql (soft delete)
   - Barge_GetLocationList.sql
   - Mark as embedded resources in .csproj

2. **Create Repository** (`src/Admin.Infrastructure/Repositories/`):
   - IBargeRepository.cs (in Abstractions/)
   - BargeRepository.cs
   - Uses Dapper with SqlText.GetSqlText()
   - Returns DTOs directly

3. **Create Service** (`src/Admin.Domain/Services/` and `src/Admin.Infrastructure/Services/`):
   - IBargeService.cs
   - BargeService.cs
   - Handles business logic and validation

4. **Create Controller** (`src/Admin.Api/Controllers/`):
   - BargeController.cs
   - RESTful endpoints with authorization

5. **Register in DI** (Startup.cs or Program.cs):
   ```csharp
   services.AddScoped<IBargeRepository, BargeRepository>();
   services.AddScoped<IBargeService, BargeService>();
   ```

### Phase 3: UI Layer

**Target:** `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI`

1. **Create ViewModels** (`ViewModels/`):
   - BargeSearchViewModel.cs
   - BargeEditViewModel.cs

2. **Create API Client Service** (`Services/`):
   - IBargeService.cs
   - BargeService.cs (HTTP client)

3. **Create MVC Controller** (`Controllers/`):
   - BargeSearchController.cs

4. **Create Razor Views** (`Views/BargeSearch/`):
   - Index.cshtml (search + grid)
   - Edit.cshtml (detail form with tabs)

5. **Create JavaScript** (`wwwroot/js/`):
   - barge-search.js
   - barge-edit.js

6. **Register in DI**:
   ```csharp
   services.AddHttpClient<IBargeService, BargeService>();
   ```

## Key Features

### Complex Validations
- Equipment Type conditional logic
- Cover Type special logic (commodity/operator-based)
- Draft validation (overall >= corner drafts)
- Charter date range overlap prevention
- Berth location matching

### Auto-Calculations
- SizeCategory from ExternalLength + ExternalWidth
- Status from LocationID changes
- Draft conversions (feet/inches ↔ decimal)

### Auto-Population
- BargeSeries selection populates multiple fields
- Equipment Type affects field requirements

### DataTables Grid
- 58 columns with conditional visibility
- Row formatting (cross-charter colors)
- Cell formatting (damage level colors)
- Server-side processing
- Multi-row selection

### DateTime Handling
- **CRITICAL:** All DateTime fields use 24-hour format
- Split into separate date + time inputs
- JavaScript combines on form submit

## Reference Examples

### MONO SHARED Structure
- **Primary:** `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\Dto\FacilityDto.cs`
- **Pattern:** DTOs with [Sortable]/[Filterable] attributes
- **Usage:** Same DTO used by API and UI (no mapping!)

### API Layer
- **Primary:** `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Repositories\FacilityRepository.cs`
- **Pattern:** Dapper with direct SQL queries
- **Returns:** DTOs directly from repository

### UI Layer
- **Primary:** `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Controllers\BoatLocationSearchController.cs`
- **Pattern:** ViewModels contain DTOs
- **Views:** Bootstrap 5 with DataTables

### Crewing Reference
- **API:** `C:\source\BargeOps.Crewing.API`
- **UI:** `C:\source\BargeOps.Crewing.UI`
- **Use for:** Additional patterns and examples

## Important Notes

### MONO SHARED Architecture
⭐ **CRITICAL**: DTOs in BargeOps.Shared are the ONLY data models!
- NO separate Models folder in API or UI
- NO AutoMapper needed (repositories return DTOs directly)
- ViewModels CONTAIN DTOs (not map to them)

### SQL Queries
- Use DIRECT SQL QUERIES (NOT stored procedures)
- Embed SQL files as resources
- Load with SqlText.GetSqlText("FileName")

### DateTime Fields
- **ALWAYS** use 24-hour format (HH:mm)
- Split into date + time inputs in UI
- Combine via JavaScript on form submit

### Soft Delete
- Use Barge_SetActive.sql (NOT Barge_Delete.sql)
- Set IsActive = false (don't hard delete)

### Validation
- FluentValidation for server-side
- jQuery Validate for client-side
- Complex business rules in BargeService

## Testing Checklist

- [ ] DTOs compile without errors
- [ ] SQL queries return expected results
- [ ] Repository methods work with Dapper
- [ ] Service validations catch errors
- [ ] API endpoints return correct responses
- [ ] ViewModels populate correctly
- [ ] Razor views render without errors
- [ ] JavaScript initializes DataTables
- [ ] Search filters work correctly
- [ ] Form submit combines DateTime fields
- [ ] Draft conversions work (feet/inches ↔ decimal)
- [ ] Auto-population works (BargeSeries)
- [ ] Auto-calculation works (SizeCategory)
- [ ] Conditional field logic works
- [ ] Validation errors display correctly

## Next Steps

1. Review the conversion-plan.md for detailed specifications
2. Copy templates to target projects in order (Shared → API → UI)
3. Create SQL query files based on legacy stored procedures
4. Adjust field names/types as needed for target database schema
5. Test each layer independently before integration
6. Add unit tests for validation logic
7. Add integration tests for repository methods

## Support

For questions about these templates:
- Review the analysis JSON files in this folder (`*.json`)
- Check conversion-plan.md for detailed specifications
- Reference Facility and BoatLocation implementations in mono repo
- Review Crewing examples for additional patterns

---

**Generated by:** Claude Code Conversion Template Generator
**Date:** 2025-12-17
**Entity:** Barge
