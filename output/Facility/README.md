# Facility Conversion Templates

## Overview

This directory contains comprehensive code templates for converting the Facility entity from legacy VB.NET to modern ASP.NET Core MVC using the BargeOps.Admin.Mono repository structure.

## Generated Files

### Conversion Plan
- **conversion-plan.md**: Complete implementation plan with architecture, phases, and success criteria

### Shared DTOs (⭐ CREATE THESE FIRST!)
Located in `templates/shared/Dto/`:
- **FacilityDto.cs**: Complete entity DTO with all Location and FacilityLocation properties
- **FacilitySearchRequest.cs**: Search criteria DTO for DataTables
- **FacilityBerthDto.cs**: Child entity DTO for berths
- **FacilityStatusDto.cs**: Child entity DTO for statuses

### API Templates
Located in `templates/api/`:

**Repositories/**
- **IFacilityRepository.cs**: Repository interface returning DTOs
- **FacilityRepository.cs**: Dapper implementation with direct SQL queries (NOT stored procedures)

**Services/**
- **IFacilityService.cs**: Service interface
- **FacilityService.cs**: Business logic implementation with FluentValidation

**Controllers/**
- **FacilityController.cs**: RESTful API controller with authorization

**Validators/**
- **FacilityDtoValidator.cs**: FluentValidation validator for Facility
- **FacilityBerthDtoValidator.cs**: FluentValidation validator for Berth
- **FacilityStatusDtoValidator.cs**: FluentValidation validator for Status

### UI Templates
Located in `templates/ui/`:

**ViewModels/**
- **FacilitySearchViewModel.cs**: Search/list screen ViewModel
- **FacilityEditViewModel.cs**: Edit/create form ViewModel

**Services/**
- **IFacilityService.cs**: API client interface
- **FacilityService.cs**: HTTP client implementation returning DTOs

**Controllers/**
- **FacilityController.cs**: MVC controller with CRUD actions and child collection endpoints

**Views/Facility/**
- **Index.cshtml**: Search/list view with DataTables
- **Edit.cshtml**: Edit/create view with Bootstrap tabs

**wwwroot/js/**
- **facility-search.js**: DataTables initialization and search logic
- **facility-detail.js**: Edit form logic with tab management, child collections, conditional fields

## Key Architecture Patterns

### ⭐ Mono Shared Structure

This implementation follows the MONO SHARED pattern:

1. **BargeOps.Shared Project** - DTOs are the ONLY data models
   - DTOs in `Dto/` folder are used by BOTH API and UI
   - No separate domain models or ViewModels with duplicate properties
   - NO AutoMapper needed!

2. **API Project** - Returns DTOs directly
   - Repositories return DTOs from SQL queries
   - Services work with DTOs (no mapping!)
   - Controllers accept and return DTOs

3. **UI Project** - Uses DTOs from Shared
   - ViewModels CONTAIN DTOs (not duplicate properties)
   - Services (API clients) return DTOs
   - Views bind to ViewModel which contains DTO

### MVVM Pattern (UI)

ViewModels follow MVVM principles:
- NO ViewBag or ViewData
- All data properties on ViewModel
- Dropdown lists as `IEnumerable<SelectListItem>`
- Data annotations for validation
- Display attributes for labels

### DateTime Handling

- Single DateTime property in DTO
- View splits into separate date and time inputs
- JavaScript combines on submit
- Example: `StartDateTime` property → separate `dtStartDate` and `cboStartTime` inputs

### ID Field Naming

- Use uppercase ID: `LocationID`, `FacilityBerthID` (NOT LocationId or FacilityBerthId)

## Implementation Sequence

### Phase 1: Shared DTOs (Day 1) ⭐ START HERE
```
1. Copy DTOs from templates/shared/Dto/ to:
   C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\Dto\

2. Verify attributes:
   - [Sortable] and [Filterable] for DataTables
   - [Display(Name = "...")] for UI labels
   - [Required], [StringLength], etc. for validation
```

### Phase 2: API Infrastructure (Days 1-2)
```
1. Copy repository files to:
   C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Repositories\

2. Copy service files to:
   - Interface: C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Domain\Services\
   - Implementation: C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Services\

3. Register in DI container (Startup.cs or Program.cs):
   services.AddScoped<IFacilityRepository, FacilityRepository>();
   services.AddScoped<IFacilityService, FacilityService>();
```

### Phase 3: API Controller (Day 2)
```
1. Copy controller to:
   C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Api\Controllers\

2. Verify authentication/authorization:
   - Uses IdentityConstants.ApplicationScheme
   - Policy-based authorization (FacilityRead, FacilityModify)

3. Test API endpoints with Swagger/Postman
```

### Phase 4-9: UI Components
Continue following conversion-plan.md phases 4-9 for UI implementation.

## Reference Implementations

### Primary References (BargeOps.Admin.Mono)
These are the canonical patterns to follow:

**Shared DTOs:**
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\Dto\FacilityDto.cs` (if exists)
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\Dto\BoatLocationDto.cs`

**API:**
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Api\Controllers\FacilityController.cs`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Repositories\FacilityRepository.cs`

**UI:**
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Controllers\BoatLocationSearchController.cs`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\ViewModels\BoatLocationEditViewModel.cs`

## Business Rules Implemented

### Conditional Field Validation
Lock/Gauge fields must be blank unless BargeExLocationType is 'Lock' or 'Gauge Location':
- LockUsaceName
- LockFloodStage
- LockPoolStage
- LockLowWater
- LockNormalCurrent
- LockHighFlow
- LockHighWater
- LockCatastrophicLevel

Implementation:
- **Backend**: FacilityService.ApplyBusinessRules() clears fields automatically
- **Frontend**: JavaScript enables/disables fields based on dropdown selection

### Data Validation
- Name: Required, max 100 characters
- Mile: Must be <= 2000.0
- River/Mile: Cannot have one without the other
- Status EndDateTime: Must be >= StartDateTime

### Child Collections
- Berths: Grid with Add/Modify/Remove operations
- Statuses: Grid with Add/Modify/Remove/Export operations

## Security Considerations

### Authorization Policies
- **FacilityRead**: View facilities and data
- **FacilityModify**: Create, update, delete facilities

### Input Validation
- All DTOs have data annotations
- FluentValidation in service layer
- XSS protection via Razor encoding
- SQL injection protection via parameterized queries (Dapper)

### Authentication
- Uses IdentityConstants.ApplicationScheme (NOT "Cookies")
- Policy-based authorization throughout

## Testing Checklist

- [ ] DTOs compile without errors
- [ ] Repository CRUD operations work
- [ ] Service validation works correctly
- [ ] API endpoints return correct status codes
- [ ] Authorization policies enforce correctly
- [ ] Lock/Gauge fields enable/disable correctly
- [ ] Child collection operations work
- [ ] DataTables search and paging work
- [ ] No XSS vulnerabilities
- [ ] No SQL injection vulnerabilities

## ✅ Template Generation Complete!

All core templates have been generated and are ready for use:

✅ **Conversion Plan** (comprehensive implementation guide)
✅ **Shared DTOs** (foundation of mono shared architecture)
✅ **API Layer** (Repositories, Services, Controllers, Validators)
✅ **UI Layer** (Services, Controllers, ViewModels, Views)
✅ **JavaScript** (DataTables, form logic, tab management)
✅ **FluentValidation** (Business rule validators)

### Additional Templates Available

If you need additional templates, I can also generate:

1. **Unit Test Templates** (xUnit tests for repositories and services)
2. **Integration Test Templates** (API endpoint tests)
3. **Migration Scripts** (SQL migration from legacy to new structure)
4. **Deployment Documentation** (Step-by-step deployment guide)

Just let me know if you need any of these!

## Support Files

All analysis data is available in this directory:
- `form-structure-search.json`: Search form analysis
- `form-structure-detail.json`: Detail form structure
- `business-logic.json`: Business rules
- `data-access.json`: Database access patterns
- `security.json`: Security requirements
- `ui-mapping.json`: UI component mapping
- `workflow.json`: User workflows
- `tabs.json`: Tab structure
- `validation.json`: Validation rules
- `related-entities.json`: Entity relationships

## Questions?

Refer to:
- **conversion-plan.md** for detailed implementation steps
- **MONO_SHARED_STRUCTURE.md** (in .claude/tasks/) for architecture details
- Existing implementations in BargeOps.Admin.Mono for patterns

---

**Generated**: 2025-12-15
**Version**: 1.0
**Generator**: Claude Code Template Generator (Interactive Mode)
