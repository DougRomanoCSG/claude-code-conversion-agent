# River Entity - Conversion Templates

## Overview

This directory contains the complete conversion templates for the **River (Rivers/Waterways)** entity from the legacy VB.NET/WinForms application to the modern ASP.NET Core architecture using the **MONO SHARED** structure.

## Generated Files

### Documentation
- **conversion-plan.md** - Comprehensive conversion plan with step-by-step implementation guide
- **README.md** - This file

### Templates

#### 1. Shared Project (BargeOps.Shared)
**Location:** `templates/shared/Dto/`

⭐ **CREATE THESE FIRST** - DTOs are the single source of truth used by both API and UI!

- **RiverDto.cs** - Complete River entity DTO with validation attributes
- **RiverSearchRequest.cs** - Search criteria DTO extending DataTableRequest
- **RiverListItemDto.cs** - Lightweight DTO for dropdown lists

**Target:** `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\BargeOps.Shared\Dto\`

#### 2. API Project (BargeOps.Admin.API)
**Location:** `templates/api/`

##### Repositories
**Target:** `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Repositories\`

- **IRiverRepository.cs** - Repository interface returning DTOs
- **RiverRepository.cs** - Dapper implementation using embedded SQL queries

##### Services
**Interfaces:** `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Domain\Services\`
**Implementations:** `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Services\`

- **IRiverService.cs** - Service interface
- **RiverService.cs** - Service implementation with business logic validation

##### Controllers
**Target:** `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Api\Controllers\`

- **RiverController.cs** - RESTful API endpoints with [ApiKey] authorization

##### SQL Queries
**Target:** `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\DataAccess\Sql\`

- **River_GetById.sql** - Get single river by ID
- **River_GetAll.sql** - Get all rivers
- **River_Search.sql** - DataTables server-side search with pagination
- **River_List.sql** - Active rivers for dropdowns
- **River_Insert.sql** - Create new river with OUTPUT
- **River_Update.sql** - Update existing river
- **River_SetActive.sql** - Soft delete/activate (set IsActive flag)

##### SQL Text Registry
**Target:** Add to `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\DataAccess\SqlText.cs`

- **SqlText_River.cs** - Properties to load embedded SQL queries

#### 3. UI Project (BargeOps.Admin.UI)
**Location:** `templates/ui/`

##### Services
**Target:** `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Services\`

- **IRiverService.cs** - UI service interface
- **RiverService.cs** - HTTP client to call API endpoints

##### ViewModels
**Target:** `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\ViewModels\`

- **RiverSearchViewModel.cs** - Search/list screen ViewModel
- **RiverEditViewModel.cs** - Edit/create form ViewModel (contains RiverDto from Shared)

##### Controllers
**Target:** `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Controllers\`

- **RiverController.cs** - MVC controller with search and CRUD actions

##### Views
**Target:** `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Views\River\`

- **Index.cshtml** - Search/list view with DataTables grid
- **Edit.cshtml** - Edit/create form with validation

##### JavaScript
**Target:** `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\wwwroot\js\`

- **river-search.js** - DataTables initialization with server-side processing
- **river-detail.js** - Form validation and client-side logic

## Implementation Order

### Phase 1: Shared DTOs (CRITICAL - Do First!)
1. Copy DTOs from `templates/shared/Dto/` to `BargeOps.Shared\Dto\`
2. Build Shared project
3. Verify DTOs are accessible from API and UI projects

### Phase 2: API Infrastructure
1. Copy SQL files to `Admin.Infrastructure\DataAccess\Sql\`
2. Add SqlText entries to `SqlText.cs`
3. Copy repository interface and implementation
4. Copy service interface and implementation
5. Copy API controller
6. Register services in DI container (`Program.cs` or `Startup.cs`)

### Phase 3: UI Layer
1. Copy UI service interface and implementation
2. Register UI service in DI container
3. Copy ViewModels
4. Copy MVC controller
5. Copy Razor views
6. Copy JavaScript files

### Phase 4: Testing
1. Test API endpoints (use Swagger/Postman)
2. Test UI search page
3. Test create/edit functionality
4. Test validation rules
5. Test soft delete

## Key Features

### DTOs (Shared)
- Complete River entity with all fields
- Data validation attributes ([Required], [MaxLength], [Range])
- Display attributes for UI rendering
- [Sortable] and [Filterable] attributes for DataTables

### API Layer
- **No stored procedures** - uses embedded SQL queries
- **No AutoMapper** - repositories return DTOs directly
- Dapper for data access
- Business logic validation in service layer
- RESTful API with proper HTTP verbs
- Soft delete via SetActive (IsActive flag)

### UI Layer
- **No ViewBag/ViewData** - all data on ViewModel (MVVM pattern)
- Server-side DataTables processing
- Client-side validation with jQuery Validate
- Bootstrap 5 styling
- Responsive design
- State persistence (localStorage)

## Business Rules

1. **Name** - Required, max 40 characters
2. **Code** - Required, exactly 3 characters, uppercase
3. **UpLabel** - Required, max 20 characters
4. **DownLabel** - Required, max 20 characters
5. **StartMile** - Optional, 0-5000, must be <= EndMile
6. **EndMile** - Optional, 0-5000
7. **IsActive** - Default true (soft delete pattern)
8. **IsLowToHighDirection** - Default true

## DataTables Configuration

### Columns
1. Code (sortable, filterable)
2. Name (sortable, filterable)
3. Start Mile (sortable, right-aligned, numeric)
4. End Mile (sortable, right-aligned, numeric)
5. Low to High (checkbox, center-aligned)
6. Active (checkbox, center-aligned)
7. Upstream Label
8. Downstream Label
9. Actions (Edit button)

### Features
- Server-side processing
- State persistence
- Default sort: Code ASC
- Page sizes: 10, 25, 50, 100
- Responsive design

## Reference Files

### Shared DTO Patterns
- `FacilityDto.cs` - Entity DTO with attributes
- `FacilitySearchRequest.cs` - Search request pattern
- `PagedResult.cs` - Pagination wrapper

### API Patterns
- `FacilityRepository.cs` - Repository pattern (uses SPs, but structure is same)
- `BoatLocationRepository.cs` - Repository with SQL text
- `FacilityController.cs` - API controller pattern

### UI Patterns
- `BoatLocationSearchController.cs` - MVC controller pattern
- `BoatLocationSearchViewModel.cs` - ViewModel pattern
- `boatLocationSearch.js` - DataTables initialization

## Security

### API
- **[ApiKey]** attribute on controller
- API key configured in appsettings.json

### UI
- **[Authorize]** on controller
- Uses **IdentityConstants.ApplicationScheme** (NOT Cookies)
- Policy-based authorization (future enhancement)

## Validation

### Server-Side
- Data annotations on DTOs
- Business logic validation in service layer
- Model state validation in controllers

### Client-Side
- jQuery Validation Unobtrusive
- Custom validation for exact length Code
- Custom validation for StartMile <= EndMile
- Uppercase transformation for Code field

## Notes

- River is a simple master data entity with no child tables or tabs
- Uses soft delete pattern (IsActive flag)
- Code field requires exact 3-character length and uppercase
- Mile markers use decimal with 2 decimal places (0.00 format)
- No AutoMapper needed - repositories return DTOs directly from Shared project
- Grid state persistence for better user experience
- All communication uses DTOs from BargeOps.Shared - single source of truth!

## Support

For questions or issues with these templates, refer to:
- **conversion-plan.md** - Detailed implementation guide
- **MONO_SHARED_STRUCTURE.md** - Architecture documentation
- Reference implementations: Facility, BoatLocation

---

**Generated:** 2025-12-11
**Agent:** Conversion Template Generator
**Status:** Ready for Implementation
