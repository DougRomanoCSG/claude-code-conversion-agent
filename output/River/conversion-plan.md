# River Entity - Conversion Plan

## Executive Summary

This document outlines the comprehensive plan for converting the River (Rivers/Waterways) entity from the legacy VB.NET/WinForms application to the modern ASP.NET Core architecture using the **MONO SHARED** structure.

**Entity:** River/Waterway
**Legacy Forms:** frmRiverSearch, frmRiverDetail
**Complexity:** Low - Simple master data entity with no child tabs
**Target Architecture:** ASP.NET Core MVC + Web API with Shared DTOs

---

## Critical Architecture Notes

### MONO SHARED Structure

⭐ **This project uses a MONO SHARED structure where DTOs and Models are in a SHARED project!**

- **DO NOT duplicate DTOs/Models** in API or UI projects
- **DTOs are the ONLY data models** - no separate Models folder
- DTOs in `BargeOps.Shared/Dto/` are used directly by **BOTH** API and UI
- See `.claude/tasks/MONO_SHARED_STRUCTURE.md` for detailed architecture

### Implementation Order

1. **SHARED** - Create DTOs first (single source of truth)
2. **API** - Repository → Service → Controller (uses shared DTOs)
3. **UI** - ViewModels (contain shared DTOs) → Services → Controllers → Views

---

## Entity Overview

### Business Purpose
Manages master data for rivers and waterways in the barge operations system, including:
- River identification (Code, Name)
- Mile marker ranges (StartMile, EndMile)
- Directional labeling (Upstream/Downstream labels)
- Integration codes (BargeEx system)

### Key Properties
- **RiverID** (int) - Primary key
- **Name** (string, 40) - Waterway name (required)
- **Code** (string, 3) - Three-character code (required, exact length, uppercase)
- **StartMile** (decimal?) - Starting mile marker
- **EndMile** (decimal?) - Ending mile marker
- **IsLowToHighDirection** (bool) - Direction flag (default: true)
- **BargeExCode** (string, 10) - Integration code
- **UpLabel** (string, 20) - Upstream label (required)
- **DownLabel** (string, 20) - Downstream label (required)
- **IsActive** (bool) - Active status (default: true)

### Business Rules
1. Name and Code are required
2. Code must be exactly 3 characters and uppercase
3. UpLabel and DownLabel are required
4. StartMile must be <= EndMile (when both have values)
5. Mile values must be >= 0 and <= 5000
6. Soft delete via IsActive flag

---

## Data Access Patterns

### Legacy Stored Procedures
- **RiverSelect** → GetById
- **RiversSelect** → GetAll
- **RiverSearch** → Search with filters
- **RiverList** → Active list for dropdowns (cached)
- **RiverInsert** → Create
- **RiverUpdate** → Update
- **RiverDelete** → SetActive (soft delete)

### Modern SQL Queries
Convert to **embedded SQL queries** (NOT stored procedures):
- `River_GetById.sql` - Single river by ID
- `River_GetAll.sql` - All rivers
- `River_Search.sql` - Search with DataTables filters
- `River_List.sql` - Active rivers for dropdowns
- `River_Insert.sql` - Create with OUTPUT
- `River_Update.sql` - Update existing
- `River_SetActive.sql` - Soft delete/activate

**Location:** `Admin.Infrastructure/DataAccess/Sql/River_*.sql`

---

## Project Structure

### 1. BargeOps.Shared (CREATE FIRST)

**Location:** `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\BargeOps.Shared\Dto\`

**Files to Create:**
```
Dto/
├── RiverDto.cs                  ⭐ Complete entity DTO (used by API and UI)
├── RiverSearchRequest.cs        ⭐ Search criteria DTO
└── RiverListItemDto.cs          ⭐ Dropdown list DTO
```

**NO Models/ folder** - DTOs are the data models!

### 2. BargeOps.Admin.API

**Location:** `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\`

**Files to Create:**
```
src/Admin.Infrastructure/
├── Repositories/
│   ├── IRiverRepository.cs      - Interface returning DTOs
│   └── RiverRepository.cs       - Dapper implementation with SQL queries
├── Services/
│   ├── IRiverService.cs         - Service interface
│   └── RiverService.cs          - Service implementation
└── DataAccess/Sql/
    ├── River_GetById.sql
    ├── River_GetAll.sql
    ├── River_Search.sql
    ├── River_List.sql
    ├── River_Insert.sql
    ├── River_Update.sql
    └── River_SetActive.sql

src/Admin.Api/Controllers/
└── RiverController.cs           - RESTful API endpoints
```

### 3. BargeOps.Admin.UI

**Location:** `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\`

**Files to Create:**
```
Controllers/
└── RiverController.cs           - MVC controller

Services/
├── IRiverService.cs             - API client interface
└── RiverService.cs              - HTTP client to API

ViewModels/
├── RiverSearchViewModel.cs      - Search/list screen
└── RiverEditViewModel.cs        - Edit/create form

Views/River/
├── Index.cshtml                 - Search view
└── Edit.cshtml                  - Edit form

wwwroot/js/
├── river-search.js              - DataTables initialization
└── river-detail.js              - Form validation
```

---

## Reference Examples

### Shared DTOs
- `FacilityDto.cs` - Complete entity DTO pattern
- `FacilitySearchRequest.cs` - Search criteria pattern
- `PagedResult.cs`, `DataTableResponse.cs` - Generic wrappers

### API Layer
- `FacilityController.cs` - Controller pattern
- `FacilityRepository.cs` - Dapper repository pattern
- `FacilityService.cs` - Service layer pattern

### UI Layer
- `BoatLocationSearchController.cs` - MVC controller pattern
- `BoatLocationSearchViewModel.cs` - ViewModel pattern
- `BoatLocationSearch/Index.cshtml` - Search view pattern
- `boatLocationSearch.js` - DataTables pattern

---

## Implementation Steps

### Phase 1: Shared DTOs (Create First!)

#### 1.1 RiverDto.cs
```csharp
namespace BargeOps.Shared.Dto;

public class RiverDto
{
    [Sortable, Filterable]
    public int RiverID { get; set; }

    [Sortable, Filterable]
    [Required, MaxLength(40)]
    [Display(Name = "Waterway name")]
    public string Name { get; set; } = string.Empty;

    [Sortable, Filterable]
    [Required, StringLength(3, MinimumLength = 3)]
    [Display(Name = "Code")]
    public string Code { get; set; } = string.Empty;

    [MaxLength(10)]
    [Display(Name = "BargeEx Code")]
    public string? BargeExCode { get; set; }

    [Sortable]
    [Display(Name = "Start Mile")]
    public decimal? StartMile { get; set; }

    [Sortable]
    [Display(Name = "End Mile")]
    public decimal? EndMile { get; set; }

    [Display(Name = "Direction is low to high")]
    public bool IsLowToHighDirection { get; set; } = true;

    [Sortable, Filterable]
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Required, MaxLength(20)]
    [Display(Name = "Upstream")]
    public string UpLabel { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    [Display(Name = "Downstream")]
    public string DownLabel { get; set; } = string.Empty;
}
```

#### 1.2 RiverSearchRequest.cs
```csharp
namespace BargeOps.Shared.Dto;

public class RiverSearchRequest : DataTableRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public bool ActiveOnly { get; set; } = true;
}
```

#### 1.3 RiverListItemDto.cs
```csharp
namespace BargeOps.Shared.Dto;

public class RiverListItemDto
{
    public int RiverID { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
```

### Phase 2: API Infrastructure

#### 2.1 IRiverRepository.cs
```csharp
namespace Admin.Infrastructure.Abstractions;

public interface IRiverRepository
{
    Task<RiverDto?> GetByIdAsync(int riverID);
    Task<IEnumerable<RiverDto>> GetAllAsync();
    Task<DataTableResponse<RiverDto>> SearchAsync(RiverSearchRequest request);
    Task<IEnumerable<RiverListItemDto>> GetListAsync();
    Task<int> CreateAsync(RiverDto river);
    Task UpdateAsync(RiverDto river);
    Task SetActiveAsync(int riverID, bool isActive);
}
```

#### 2.2 RiverRepository.cs
Pattern:
- Use `SqlText.GetSqlText("River_GetById")` to load embedded SQL
- Use Dapper for data access
- Return DTOs directly (no mapping needed!)
- Use `IDbConnectionFactory` for connections

Reference: `FacilityRepository.cs`

#### 2.3 IRiverService.cs
```csharp
namespace Admin.Domain.Services;

public interface IRiverService
{
    Task<RiverDto?> GetByIdAsync(int riverID);
    Task<DataTableResponse<RiverDto>> SearchAsync(RiverSearchRequest request);
    Task<IEnumerable<RiverListItemDto>> GetListAsync();
    Task<int> CreateAsync(RiverDto river);
    Task UpdateAsync(RiverDto river);
    Task DeleteAsync(int riverID);
}
```

#### 2.4 RiverService.cs
Pattern:
- Inject `IRiverRepository`
- Use DTOs directly from repository (no mapping!)
- Implement business logic validation
- Use SetActiveAsync for delete operations

Reference: `FacilityService.cs`

#### 2.5 RiverController.cs
```csharp
[ApiController]
[Route("api/[controller]")]
[ApiKey]
public class RiverController : ApiControllerBase
{
    private readonly IRiverService _riverService;

    [HttpGet("{id}")]
    public async Task<ActionResult<RiverDto>> GetRiver(int id);

    [HttpPost("search")]
    public async Task<ActionResult<DataTableResponse<RiverDto>>> Search([FromBody] RiverSearchRequest request);

    [HttpGet("list")]
    public async Task<ActionResult<IEnumerable<RiverListItemDto>>> GetList();

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] RiverDto river);

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] RiverDto river);

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id);
}
```

Reference: `FacilityController.cs`

### Phase 3: SQL Query Files

Create embedded SQL files in `Admin.Infrastructure/DataAccess/Sql/`:

#### 3.1 River_GetById.sql
```sql
SELECT
    RiverID, Name, Code, BargeExCode,
    StartMile, EndMile, IsLowToHighDirection,
    IsActive, UpLabel, DownLabel
FROM River
WHERE RiverID = @RiverID
```

#### 3.2 River_Search.sql
```sql
-- DataTables server-side processing query
-- Uses @Code, @Name, @ActiveOnly, @Start, @Length, @OrderColumn, @OrderDirection
-- Returns paginated results with total count
```

#### 3.3 River_Insert.sql
```sql
INSERT INTO River (Name, Code, BargeExCode, StartMile, EndMile,
                   IsLowToHighDirection, IsActive, UpLabel, DownLabel)
OUTPUT INSERTED.RiverID
VALUES (@Name, @Code, @BargeExCode, @StartMile, @EndMile,
        @IsLowToHighDirection, @IsActive, @UpLabel, @DownLabel)
```

#### 3.4 River_Update.sql
```sql
UPDATE River
SET Name = @Name, Code = @Code, BargeExCode = @BargeExCode,
    StartMile = @StartMile, EndMile = @EndMile,
    IsLowToHighDirection = @IsLowToHighDirection, IsActive = @IsActive,
    UpLabel = @UpLabel, DownLabel = @DownLabel
WHERE RiverID = @RiverID
```

#### 3.5 River_SetActive.sql
```sql
UPDATE River
SET IsActive = @IsActive
WHERE RiverID = @RiverID
```

### Phase 4: UI Layer

#### 4.1 IRiverService.cs (UI)
```csharp
namespace BargeOpsAdmin.Services;

public interface IRiverService
{
    Task<RiverDto?> GetByIdAsync(int id);
    Task<DataTableResponse<RiverDto>> SearchAsync(RiverSearchRequest request);
    Task<IEnumerable<RiverListItemDto>> GetListAsync();
    Task<int> CreateAsync(RiverDto river);
    Task UpdateAsync(int id, RiverDto river);
    Task DeleteAsync(int id);
}
```

#### 4.2 RiverService.cs (UI)
Pattern:
- Inject `HttpClient`
- Make HTTP calls to API endpoints
- Return DTOs from API (already shared!)

Reference: `BoatLocationService.cs`

#### 4.3 ViewModels

**RiverSearchViewModel.cs**
```csharp
namespace BargeOpsAdmin.ViewModels;

public class RiverSearchViewModel
{
    [Display(Name = "Code")]
    public string? Code { get; set; }

    [Display(Name = "River/Waterway name")]
    public string? Name { get; set; }

    [Display(Name = "Active only")]
    public bool ActiveOnly { get; set; } = true;
}
```

**RiverEditViewModel.cs**
```csharp
namespace BargeOpsAdmin.ViewModels;

public class RiverEditViewModel
{
    public RiverDto River { get; set; } = new();
}
```

#### 4.4 RiverController.cs (UI)
```csharp
[Authorize]
public class RiverController : Controller
{
    private readonly IRiverService _riverService;

    [HttpGet]
    public IActionResult Index() => View(new RiverSearchViewModel());

    [HttpPost]
    public async Task<IActionResult> RiverTable([FromBody] RiverSearchRequest request);

    [HttpGet]
    public async Task<IActionResult> Edit(int? id);

    [HttpPost]
    public async Task<IActionResult> Edit(RiverEditViewModel model);

    [HttpPost]
    public async Task<IActionResult> Delete(int id);
}
```

Reference: `BoatLocationSearchController.cs`

#### 4.5 Views

**Index.cshtml** - Search view with DataTables grid

**Edit.cshtml** - Edit/create form with validation

Reference: `BoatLocationSearch/Index.cshtml`, `BoatLocationSearch/Edit.cshtml`

#### 4.6 JavaScript

**river-search.js**
- Initialize DataTables with server-side processing
- Handle search button click
- Pass search criteria to AJAX request

Reference: `boatLocationSearch.js`

**river-detail.js**
- Client-side validation
- Code field uppercase transformation
- Decimal validation for mile fields

---

## Validation

### Data Annotations (DTO/ViewModel)
```csharp
[Required(ErrorMessage = "Waterway name is required")]
[MaxLength(40, ErrorMessage = "Name cannot exceed 40 characters")]
public string Name { get; set; }

[Required, StringLength(3, MinimumLength = 3, ErrorMessage = "Code must be exactly 3 characters")]
public string Code { get; set; }

[Range(0, 5000, ErrorMessage = "Mile must be between 0 and 5000")]
public decimal? StartMile { get; set; }
```

### FluentValidation (Optional)
```csharp
public class RiverDtoValidator : AbstractValidator<RiverDto>
{
    public RiverDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Code).NotEmpty().Length(3);
        RuleFor(x => x.StartMile)
            .LessThanOrEqualTo(x => x.EndMile)
            .When(x => x.StartMile.HasValue && x.EndMile.HasValue);
    }
}
```

### Client-Side Validation
- jQuery Validation Unobtrusive
- Custom validation for exact length Code field
- Uppercase transformation on blur

---

## Security

### API
- **[ApiKey]** attribute on controller
- API key configured in appsettings.json

### UI
- **[Authorize]** on controller
- **IdentityConstants.ApplicationScheme**
- Policy-based authorization (future)

### Permissions (Future)
- **RiverView** - View rivers
- **RiverModify** - Create/Edit/Delete rivers

---

## DataTables Configuration

### Columns
1. **Code** - Text, sortable, filterable
2. **Name** - Text, sortable, filterable
3. **StartMile** - Decimal, right-aligned, numeric sort
4. **EndMile** - Decimal, right-aligned, numeric sort
5. **IsLowToHighDirection** - Checkbox, center-aligned
6. **Active** - Checkbox, center-aligned
7. **UpLabel** - Text
8. **DownLabel** - Text
9. **Actions** - Edit button (not sortable)

### Features
- Server-side processing
- State persistence (localStorage)
- Default sort: Code ASC
- Page size: 25 (10, 25, 50, 100)
- Responsive design

---

## Testing Checklist

### API Tests
- [ ] GET /api/river/{id} - Retrieve by ID
- [ ] POST /api/river/search - Search with filters
- [ ] GET /api/river/list - Active list
- [ ] POST /api/river - Create new river
- [ ] PUT /api/river/{id} - Update river
- [ ] DELETE /api/river/{id} - Soft delete

### UI Tests
- [ ] Search page loads with default filters
- [ ] DataTables displays results
- [ ] Search filters work (Code, Name, Active)
- [ ] Create new river form
- [ ] Edit existing river form
- [ ] Validation works (required fields, exact length Code)
- [ ] Code field uppercase transformation
- [ ] Mile field decimal validation
- [ ] Soft delete functionality

### Business Rules
- [ ] Code must be exactly 3 characters
- [ ] StartMile <= EndMile validation
- [ ] Mile range validation (0-5000)
- [ ] Required fields validation

---

## Migration Notes

### Database Changes
- No schema changes needed
- Soft delete via IsActive flag (already exists)

### Data Migration
- No data migration required
- Existing data compatible

### Breaking Changes
- None - backward compatible

---

## Deployment Checklist

### Shared Project
- [ ] DTOs compiled and referenced by API and UI

### API Deployment
- [ ] SQL query files embedded as resources
- [ ] Repository registered in DI
- [ ] Service registered in DI
- [ ] Controller routes tested

### UI Deployment
- [ ] ViewModels in correct namespace
- [ ] API service client configured
- [ ] Views published
- [ ] JavaScript files bundled
- [ ] CSS included

---

## Success Criteria

1. ✅ All River CRUD operations work in new UI
2. ✅ Search functionality matches legacy behavior
3. ✅ DataTables grid displays correctly
4. ✅ Validation rules enforced
5. ✅ API endpoints functional
6. ✅ No duplicate DTOs (shared between API and UI)
7. ✅ Code follows MONO SHARED architecture
8. ✅ Performance acceptable (< 2s for search)

---

## Support & References

### Documentation
- `.claude/tasks/MONO_SHARED_STRUCTURE.md` - Architecture details
- Facility implementation - Complete reference
- BoatLocation implementation - UI reference

### Key Files to Reference
- `FacilityDto.cs` - DTO pattern
- `FacilityRepository.cs` - Repository pattern
- `FacilityController.cs` (API) - API controller pattern
- `BoatLocationSearchController.cs` - UI controller pattern
- `boatLocationSearch.js` - DataTables pattern

---

## Notes

- River is a simple master data entity with no child tables or tabs
- Uses soft delete pattern (IsActive flag)
- Code field requires exact 3-character length and uppercase
- Mile markers use decimal with 2 decimal places
- No AutoMapper needed - repositories return DTOs directly
- Grid state persistence recommended for user experience

---

**Generated:** 2025-12-11
**Agent:** Conversion Template Generator
**Status:** Ready for Implementation
