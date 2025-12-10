# BargeOps.Admin.Mono Shared Project Structure

## Overview

The BargeOps.Admin.Mono project uses a **shared library architecture** where DTOs and Models are centralized in a common `BargeOps.Shared` project, rather than duplicating them across API and UI projects.

## Project Structure

```
C:\Dev\BargeOps.Admin.Mono\
├── src/
│   ├── BargeOps.Shared/              ⭐ NEW - Shared DTOs (used by both API and UI)
│   │   └── BargeOps.Shared/
│   │       ├── Dto/                   ⭐ DTOs are the ONLY data models
│   │       │   ├── FacilityDto.cs
│   │       │   ├── FacilityBerthDto.cs
│   │       │   ├── FacilityStatusDto.cs
│   │       │   ├── FacilitySearchRequest.cs
│   │       │   ├── BoatLocationDto.cs
│   │       │   ├── LookupItemDto.cs
│   │       │   ├── PagedResult.cs
│   │       │   └── DataTableResponse.cs
│   │       ├── Constants/
│   │       ├── Interfaces/
│   │       └── Services/
│   │
│   ├── BargeOps.API/
│   │   └── src/
│   │       ├── Admin.Api/              (ASP.NET Core Web API)
│   │       │   └── Controllers/
│   │       │       ├── FacilityController.cs
│   │       │       └── BoatLocationController.cs
│   │       ├── Admin.Domain/          (Service Interfaces)
│   │       │   ├── Interfaces/
│   │       │   │   ├── IFacilityService.cs
│   │       │   │   └── IFacilityRepository.cs
│   │       │   └── Services/
│   │       │       └── (Service interface definitions)
│   │       └── Admin.Infrastructure/   (Data Access & Service Implementation)
│   │           ├── Repositories/
│   │           │   ├── IFacilityRepository.cs
│   │           │   ├── FacilityRepository.cs
│   │           │   └── UnitTowConnectionFactory.cs
│   │           ├── Services/
│   │           │   ├── FacilityService.cs
│   │           │   └── BoatLocationService.cs
│   │           ├── DataAccess/
│   │           │   └── Sql/
│   │           │       └── *.sql
│   │           └── Mapping/
│   │               └── FacilityMappingProfile.cs
│   │
│   └── BargeOps.UI/                    (ASP.NET Core MVC)
│       ├── Controllers/
│       │   ├── BoatLocationSearchController.cs
│       │   └── (future) FacilityController.cs
│       ├── Services/                   (API Client Services)
│       │   ├── AdminBaseService.cs
│       │   ├── BoatLocationService.cs
│       │   └── IBoatLocationService.cs
│       ├── ViewModels/
│       │   ├── AdminBaseModel.cs
│       │   ├── BoatLocationEditViewModel.cs
│       │   └── BoatLocationSearchViewModel.cs
│       └── Views/
│           └── BoatLocationSearch/
│               ├── Index.cshtml
│               └── Edit.cshtml
```

## Key Changes from Old Structure

### OLD Structure (Pre-Mono)
```
API Project:
├── Domain/
│   ├── Models/          ❌ Duplicated
│   │   └── Facility.cs
│   └── Dto/             ❌ Duplicated
│       └── FacilityDto.cs

UI Project:
├── Models/              ❌ Duplicated
│   └── FacilityDto.cs   (copy from API)
```

### NEW Structure (Mono Shared)
```
Shared Project:          ⭐ Single source of truth
└── Dto/                 ⭐ DTOs are the ONLY data models
    ├── FacilityDto.cs   (used by BOTH API and UI)
    ├── FacilityBerthDto.cs
    └── FacilitySearchRequest.cs

API Project:
└── References: BargeOps.Shared ✅
    Uses FacilityDto directly

UI Project:
└── References: BargeOps.Shared ✅
    Uses FacilityDto directly (no separate Models)
```

## Shared Library Details

### Location
- **Path**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\BargeOps.Shared\`
- **Project File**: `BargeOps.Shared.csproj`

### Dependencies
```xml
<ItemGroup>
  <PackageReference Include="Csg.ListQuery.AspNetCore" Version="2.0.10-preview" />
  <PackageReference Include="Csg.ListQuery.Sql" Version="2.0.10-preview" />
  <PackageReference Include="System.ComponentModel.Annotations" />
</ItemGroup>
```

### DTO Pattern Example

⭐ **IMPORTANT:** DTOs are the ONLY data models - used by both API and UI!

```csharp
// File: BargeOps.Shared/Dto/FacilityDto.cs
// This DTO is used by BOTH API and UI - no separate domain models!
namespace BargeOps.Shared.Dto
{
    [Sortable]
    [Filterable]
    public class FacilityDto
    {
        public int LocationId { get; set; }

        [Filterable]
        [Sortable]
        public string Name { get; set; }

        [Filterable]
        [Sortable]
        public string ShortName { get; set; }

        // Child collections
        public List<FacilityBerthDto> Berths { get; set; }
        public List<FacilityStatusDto> Statuses { get; set; }

        // Audit fields
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
```

**Note:** No separate domain models - DTOs serve as the data model for both API and UI!

## API Layer Architecture

### API Controller Pattern
```csharp
// File: Admin.Api/Controllers/FacilityController.cs
using BargeOps.Shared.Dto;  // ⭐ Uses shared DTOs
using Admin.Domain.Services;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FacilityController : ControllerBase
{
    private readonly IFacilityService _service;

    [HttpPost("search")]
    [ProducesResponseType(typeof(DataTableResponse<FacilityDto>), 200)]
    public async Task<IActionResult> Search([FromBody] FacilitySearchRequest request)
    {
        var result = await _service.SearchAsync(request);
        return Ok(result);
    }
}
```

### Service Layer Pattern
```csharp
// File: Admin.Infrastructure/Services/FacilityService.cs
using BargeOps.Shared.Dto;  // ⭐ Uses shared DTOs (no separate models!)

public class FacilityService : IFacilityService
{
    private readonly IFacilityRepository _repository;

    public async Task<FacilityDto> GetByIdAsync(int locationId)
    {
        // Repository returns DTO directly
        var facility = await _repository.GetByIdAsync(locationId);

        // Load child collections (also DTOs)
        facility.Berths = (await _repository.GetBerthsByFacilityIdAsync(locationId))
            .ToList();

        return facility;
    }
}
```

**Note:** No AutoMapper needed when repositories return DTOs directly!

### Repository Layer Pattern (Dapper)
```csharp
// File: Admin.Infrastructure/Repositories/FacilityRepository.cs
using BargeOps.Shared.Dto;  // ⭐ Uses shared DTOs for queries
using Dapper;

public class FacilityRepository : IFacilityRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public async Task<DataTableResponse<FacilityDto>> SearchAsync(FacilitySearchRequest request)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();

        // Add search parameters
        parameters.Add("@Name", request.Name);
        parameters.Add("@IsActive", request.IsActive);

        using var multi = await connection.QueryMultipleAsync(
            "sp_FacilityLocationSearch",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        var results = (await multi.ReadAsync<FacilityDto>()).ToList();
        var totalRecords = await multi.ReadSingleAsync<int>();

        return new DataTableResponse<FacilityDto>
        {
            Data = results,
            RecordsTotal = totalRecords,
            RecordsFiltered = totalRecords
        };
    }
}
```

## UI Layer Architecture

### UI Service Pattern (API Client)
```csharp
// File: BargeOps.UI/Services/FacilityService.cs
using BargeOps.Shared.Dto;  // ⭐ Uses shared DTOs

public class FacilityService : AdminBaseService, IFacilityService
{
    public async Task<DataTableResponse<FacilityDto>> SearchAsync(FacilitySearchRequest request)
    {
        using var client = GetClient();
        var response = await client.PostAsJsonAsync("api/facility/search", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DataTableResponse<FacilityDto>>();
    }
}
```

### UI ViewModel Pattern
```csharp
// File: BargeOps.UI/ViewModels/FacilitySearchViewModel.cs
using BargeOps.Shared.Dto;  // ⭐ Uses shared DTOs directly

public class FacilitySearchViewModel
{
    // The actual data (from API) - using DTO directly!
    public FacilityDto Facility { get; set; }

    // Search criteria properties
    public string Name { get; set; }
    public string ShortName { get; set; }
    public int? RiverId { get; set; }
    public bool? IsActive { get; set; }

    // Lookup lists (also DTOs!)
    public List<LookupItemDto> Rivers { get; set; }
    public List<LookupItemDto> FacilityTypes { get; set; }
}
```

**Note:** ViewModels use DTOs directly - no separate UI models!

### UI Controller Pattern
```csharp
// File: BargeOps.UI/Controllers/FacilityController.cs
using BargeOps.Shared.Dto;  // ⭐ Uses shared DTOs

public class FacilityController : AppController
{
    private readonly IFacilityService _service;

    public async Task<IActionResult> Index()
    {
        var model = new FacilitySearchViewModel
        {
            Rivers = await _service.GetRiversAsync(),
            FacilityTypes = await _service.GetFacilityTypesAsync()
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Search([FromBody] FacilitySearchRequest request)
    {
        var result = await _service.SearchAsync(request);
        return Json(result);
    }
}
```

## Reference Examples

### Existing Examples in Codebase

#### Shared DTOs
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\BargeOps.Shared\Dto\FacilityDto.cs`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\BargeOps.Shared\Dto\BoatLocationDto.cs`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\BargeOps.Shared\Dto\LookupItemDto.cs`

#### Shared Models
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\BargeOps.Shared\Models\Facility.cs`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\BargeOps.Shared\Models\BoatLocation.cs`

#### API Examples
- Controller: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Api\Controllers\FacilityController.cs`
- Repository: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Repositories\FacilityRepository.cs`
- Service: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Services\FacilityService.cs`

#### UI Examples
- Controller: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Controllers\BoatLocationSearchController.cs`
- Service: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Services\BoatLocationService.cs`
- ViewModel: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\ViewModels\BoatLocationSearchViewModel.cs`

## Code Generation Structure

When generating conversion templates, use this structure:

```
output/{Entity}/templates/
├── shared/                    ⭐ NEW - Shared project files
│   └── Dto/                   ⭐ DTOs are the ONLY data models
│       ├── {Entity}Dto.cs
│       ├── {Entity}CreateDto.cs (optional - if different from main DTO)
│       ├── {Entity}UpdateDto.cs (optional - if different from main DTO)
│       ├── {Entity}SearchRequest.cs
│       └── {Child}Dto.cs (for child entities)
├── api/                       (API-specific files)
│   ├── Controllers/
│   │   └── {Entity}Controller.cs
│   ├── Repositories/
│   │   ├── I{Entity}Repository.cs
│   │   └── {Entity}Repository.cs
│   ├── Services/
│   │   ├── I{Entity}Service.cs
│   │   └── {Entity}Service.cs
│   ├── Mapping/
│   │   └── {Entity}MappingProfile.cs
│   └── DataAccess/
│       └── Sql/
│           └── *.sql
└── ui/                        (UI-specific files)
    ├── Controllers/
    │   └── {Entity}Controller.cs
    ├── Services/
    │   ├── I{Entity}Service.cs
    │   └── {Entity}Service.cs
    ├── ViewModels/
    │   ├── {Entity}SearchViewModel.cs
    │   ├── {Entity}EditViewModel.cs
    │   └── {Entity}DetailViewModel.cs
    ├── Views/
    │   └── {Entity}/
    │       ├── Index.cshtml
    │       ├── Edit.cshtml
    │       └── _Partials/
    └── wwwroot/
        └── js/
            ├── {entity}-search.js
            └── {entity}-detail.js
```

## Implementation Checklist

When converting an entity, follow this order:

### 1. Shared Project (FIRST)
- [ ] Create DTOs in `BargeOps.Shared/Dto/`
  - {Entity}Dto.cs - Main DTO with all fields
  - {Entity}SearchRequest.cs - Search criteria
  - {Child}Dto.cs - Child entity DTOs
- [ ] Add `[Sortable]` and `[Filterable]` attributes where needed
- [ ] **NO separate Models folder** - DTOs are the data models!

### 2. API Infrastructure
- [ ] Create repository interface in `Admin.Infrastructure/Repositories/`
- [ ] Implement repository with Dapper (returns DTOs directly)
- [ ] Create service interface in `Admin.Domain/Services/`
- [ ] Implement service in `Admin.Infrastructure/Services/`
- [ ] **NO AutoMapper needed** - repositories return DTOs directly!
- [ ] Create stored procedure wrappers

### 3. API Controller
- [ ] Create controller in `Admin.Api/Controllers/`
- [ ] Implement all CRUD endpoints
- [ ] Add authorization attributes
- [ ] Configure dependency injection

### 4. UI Services
- [ ] Create service interface in `BargeOps.UI/Services/`
- [ ] Implement API client service
- [ ] Configure HttpClient

### 5. UI Controllers & Views
- [ ] Create ViewModels in `BargeOps.UI/ViewModels/`
- [ ] Create controller in `BargeOps.UI/Controllers/`
- [ ] Create Razor views in `BargeOps.UI/Views/`
- [ ] Create JavaScript files in `BargeOps.UI/wwwroot/js/`

## Benefits of Shared Structure

1. **Single Source of Truth**: DTOs are the ONLY data models - no duplication anywhere!
2. **Type Safety**: Changes to DTOs update both API and UI at compile time
3. **Zero Duplication**: No separate Models, domain models, or UI models
4. **Simpler Architecture**: No AutoMapper, no mapping layers, just DTOs
5. **Easier Refactoring**: Update in one place, affects all consumers instantly
6. **Consistent Data Contracts**: API and UI always use identical structures
7. **Better IntelliSense**: IDEs provide better code completion
8. **Simplified Testing**: Mock DTOs work across both projects
9. **Less Code**: Fewer files, fewer abstractions, easier maintenance

## Migration Notes

For existing entities being converted:
1. Create DTOs in `BargeOps.Shared/Dto/` first
2. Update API to reference `BargeOps.Shared` and use DTOs directly
3. Update UI to reference `BargeOps.Shared` and use DTOs directly
4. Remove all duplicate model definitions (API models, UI models, domain models)
5. Remove AutoMapper profiles (not needed when using DTOs everywhere)
6. Update namespaces in all consuming code to use `BargeOps.Shared.Dto`
