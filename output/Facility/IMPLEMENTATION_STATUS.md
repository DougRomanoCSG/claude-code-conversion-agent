# Facility Conversion - Implementation Status

## Date: 2025-11-11

## Completed Items

### ✅ Phase 1: API Development - Domain Models
**Location**: `C:\source\BargeOps\BargeOps.Admin.API\src\Admin.Domain\Models\`

1. **Facility.cs** - Created
   - Main Facility model with 40+ properties
   - Includes Location properties, FacilityLocation properties
   - Lock/Gauge conditional fields
   - NDC reference data fields (read-only)
   - Child collections (Berths, Statuses)

2. **FacilityBerth.cs** - Created (within Facility.cs)
   - FacilityBerthId, FacilityLocationId, Name, ShipName

3. **FacilityStatus.cs** - Created (within Facility.cs)
   - FacilityStatusId, LocationId, StatusId, StartDateTime, EndDateTime, Note
   - Audit fields

### ⚠️ Phase 1: API Development - DTOs
**Location**: `C:\source\BargeOps\BargeOps.Admin.API\src\Admin.Domain\Dto\`

**Status**: File exists (`FacilityDto.cs`) but needs expansion

**Existing File**: Uses Csg.ListQuery annotations and has basic fields
**Action Required**: Expand to include all required DTOs:
- `FacilityListDto` (for search results)
- `FacilityDto` (complete details) - exists but needs expansion
- `FacilitySearchRequest` (extends DataTableRequest)
- `FacilityBerthDto`
- `FacilityStatusDto`
- `DataTableRequest` base class (may already exist)
- `DataTableResponse<T>` generic class

## Pending Items - API (BargeOps.Admin.API)

### Repository Layer
**Location**: `C:\source\BargeOps\BargeOps.Admin.API\src\Admin.Infrastructure\Repositories\`
**Reference**: `BoatLocationRepository.cs`

#### Files to Create:

1. **IFacilityRepository.cs** (Interface)
```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using Admin.Domain.Dto;
using Admin.Domain.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Admin.Domain.Interfaces
{
    public interface IFacilityRepository
    {
        // Main CRUD
        Task<DataTableResponse<FacilityListDto>> SearchAsync(FacilitySearchRequest request);
        Task<Facility> GetByIdAsync(int locationId);
        Task<int> CreateAsync(Facility facility, string userId);
        Task UpdateAsync(Facility facility, string userId);
        Task DeleteAsync(int locationId, string userId);

        // Lookups
        Task<IEnumerable<SelectListItem>> GetRiversAsync();
        Task<IEnumerable<SelectListItem>> GetFacilityTypesAsync();

        // Berths
        Task<IEnumerable<FacilityBerthDto>> GetBerthsAsync(int facilityLocationId);
        Task<int> AddBerthAsync(FacilityBerthDto berth);
        Task UpdateBerthAsync(FacilityBerthDto berth);
        Task DeleteBerthAsync(int berthId);

        // Statuses
        Task<IEnumerable<FacilityStatusDto>> GetStatusesAsync(int locationId);
        Task<int> AddStatusAsync(FacilityStatusDto status, string userId);
        Task UpdateStatusAsync(FacilityStatusDto status, string userId);
        Task DeleteStatusAsync(int statusId);
    }
}
```

2. **FacilityRepository.cs** (Implementation)
**Pattern**: Follow `BoatLocationRepository.cs` structure
**Key Points**:
- Constructor: `IDbConnectionFactory` injection
- Use Dapper for all data access
- Call stored procedures: `sp_FacilityLocationSearch`, `sp_FacilityLocation_GetByID`, etc.
- `SearchAsync`: Handle DataTables pagination, sorting, filtering
- `GetByIdAsync`: Return complete Facility with all properties
- `CreateAsync`: Call insert SP, return new LocationId
- `UpdateAsync`: Call update SP
- `DeleteAsync`: Soft delete (set IsActive = 0)
- Child methods: Separate methods for Berths and Statuses CRUD

**Stored Procedures to Call**:
- `sp_FacilityLocationSearch` (search with filtering)
- `sp_FacilityLocation_GetByID` (get by ID)
- `sp_FacilityLocation_Insert` (insert new)
- `sp_FacilityLocation_Update` (update existing)
- `sp_FacilityLocation_Delete` (soft delete)
- `sp_FacilityBerth_GetByFacilityID`
- `sp_FacilityBerth_Insert`
- `sp_FacilityBerth_Update`
- `sp_FacilityBerth_Delete`
- `sp_FacilityStatus_GetByFacilityID`
- `sp_FacilityStatus_Insert`
- `sp_FacilityStatus_Update`
- `sp_FacilityStatus_Delete`
- `sp_River_GetAll` (for dropdown)
- `sp_BargeExLocationType_GetAll` (for dropdown)

### Service Layer
**Location**: `C:\source\BargeOps\BargeOps.Admin.API\src\Admin.Domain\Services\`
**Reference**: Look for similar service patterns

#### Files to Create:

1. **IFacilityService.cs** (Interface)
```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using Admin.Domain.Dto;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Admin.Domain.Interfaces
{
    public interface IFacilityService
    {
        Task<DataTableResponse<FacilityListDto>> SearchAsync(FacilitySearchRequest request);
        Task<FacilityDto> GetByIdAsync(int locationId);
        Task<FacilityDto> CreateAsync(FacilityDto dto, string userId);
        Task<FacilityDto> UpdateAsync(FacilityDto dto, string userId);
        Task DeleteAsync(int locationId, string userId);

        Task<IEnumerable<SelectListItem>> GetRiversAsync();
        Task<IEnumerable<SelectListItem>> GetFacilityTypesAsync();

        Task<IEnumerable<FacilityBerthDto>> GetBerthsAsync(int facilityLocationId);
        Task<FacilityBerthDto> AddBerthAsync(FacilityBerthDto dto);
        Task<FacilityBerthDto> UpdateBerthAsync(FacilityBerthDto dto);
        Task DeleteBerthAsync(int berthId);

        Task<IEnumerable<FacilityStatusDto>> GetStatusesAsync(int locationId);
        Task<FacilityStatusDto> AddStatusAsync(FacilityStatusDto dto, string userId);
        Task<FacilityStatusDto> UpdateStatusAsync(FacilityStatusDto dto, string userId);
        Task DeleteStatusAsync(int statusId);
    }
}
```

2. **FacilityService.cs** (Implementation)
**Key Business Logic**:
```csharp
public async Task<FacilityDto> UpdateAsync(FacilityDto dto, string userId)
{
    // Business Rule: Clear Lock/Gauge fields if facility type is not Lock or Gauge Location
    if (dto.FacilityTypeId != LockTypeId && dto.FacilityTypeId != GaugeTypeId)
    {
        dto.LockUsaceName = null;
        dto.LockFloodStage = null;
        dto.LockPoolStage = null;
        dto.LockLowWater = null;
        dto.LockNormalCurrent = null;
        dto.LockHighFlow = null;
        dto.LockHighWater = null;
        dto.LockCatastrophicLevel = null;
    }

    // Validation: River required when Mile specified
    if (dto.Mile.HasValue && !dto.RiverId.HasValue)
    {
        throw new ValidationException("River is required when Mile is specified");
    }

    // Map and update
    var facility = _mapper.Map<Facility>(dto);
    await _repository.UpdateAsync(facility, userId);

    return await GetByIdAsync(dto.LocationId);
}
```

### AutoMapper Profile
**Location**: `C:\source\BargeOps\BargeOps.Admin.API\src\Admin.Infrastructure\Mappings\`

#### File to Create:

**FacilityMappingProfile.cs**
```csharp
using Admin.Domain.Dto;
using Admin.Domain.Models;
using AutoMapper;

namespace Admin.Infrastructure.Mappings
{
    public class FacilityMappingProfile : Profile
    {
        public FacilityMappingProfile()
        {
            CreateMap<Facility, FacilityDto>().ReverseMap();
            CreateMap<Facility, FacilityListDto>();
            CreateMap<FacilityBerth, FacilityBerthDto>().ReverseMap();
            CreateMap<FacilityStatus, FacilityStatusDto>().ReverseMap();
        }
    }
}
```

### API Controller
**Location**: `C:\source\BargeOps\BargeOps.Admin.API\src\Admin.Api\Controllers\`
**Reference**: `BoatLocationController.cs`

#### File to Create:

**FacilityController.cs**
```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using Admin.Domain.Dto;
using Admin.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Admin.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "FacilityReadOnly")]
    public class FacilityController : ControllerBase
    {
        private readonly IFacilityService _facilityService;

        public FacilityController(IFacilityService facilityService)
        {
            _facilityService = facilityService;
        }

        [HttpPost("search")]
        public async Task<ActionResult<DataTableResponse<FacilityListDto>>> Search([FromBody] FacilitySearchRequest request)
        {
            var result = await _facilityService.SearchAsync(request);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<FacilityDto>> GetById(int id)
        {
            var result = await _facilityService.GetByIdAsync(id);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "FacilityModify")]
        public async Task<ActionResult<FacilityDto>> Create([FromBody] FacilityDto dto)
        {
            var userId = User.Identity.Name;
            var result = await _facilityService.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.LocationId }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "FacilityModify")]
        public async Task<ActionResult<FacilityDto>> Update(int id, [FromBody] FacilityDto dto)
        {
            if (id != dto.LocationId)
                return BadRequest();

            var userId = User.Identity.Name;
            var result = await _facilityService.UpdateAsync(dto, userId);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "FacilityModify")]
        public async Task<ActionResult> Delete(int id)
        {
            var userId = User.Identity.Name;
            await _facilityService.DeleteAsync(id, userId);
            return NoContent();
        }

        // Lookup endpoints
        [HttpGet("rivers")]
        public async Task<ActionResult<IEnumerable<SelectListItem>>> GetRivers()
        {
            var result = await _facilityService.GetRiversAsync();
            return Ok(result);
        }

        [HttpGet("facility-types")]
        public async Task<ActionResult<IEnumerable<SelectListItem>>> GetFacilityTypes()
        {
            var result = await _facilityService.GetFacilityTypesAsync();
            return Ok(result);
        }

        // Berth endpoints
        [HttpGet("{id}/berths")]
        public async Task<ActionResult<IEnumerable<FacilityBerthDto>>> GetBerths(int id)
        {
            var result = await _facilityService.GetBerthsAsync(id);
            return Ok(result);
        }

        [HttpPost("{id}/berths")]
        [Authorize(Policy = "FacilityModify")]
        public async Task<ActionResult<FacilityBerthDto>> AddBerth(int id, [FromBody] FacilityBerthDto dto)
        {
            dto.FacilityLocationId = id;
            var result = await _facilityService.AddBerthAsync(dto);
            return Ok(result);
        }

        [HttpPut("{id}/berths/{berthId}")]
        [Authorize(Policy = "FacilityModify")]
        public async Task<ActionResult<FacilityBerthDto>> UpdateBerth(int id, int berthId, [FromBody] FacilityBerthDto dto)
        {
            if (berthId != dto.FacilityBerthId)
                return BadRequest();

            var result = await _facilityService.UpdateBerthAsync(dto);
            return Ok(result);
        }

        [HttpDelete("{id}/berths/{berthId}")]
        [Authorize(Policy = "FacilityModify")]
        public async Task<ActionResult> DeleteBerth(int id, int berthId)
        {
            await _facilityService.DeleteBerthAsync(berthId);
            return NoContent();
        }

        // Status endpoints
        [HttpGet("{id}/statuses")]
        public async Task<ActionResult<IEnumerable<FacilityStatusDto>>> GetStatuses(int id)
        {
            var result = await _facilityService.GetStatusesAsync(id);
            return Ok(result);
        }

        [HttpPost("{id}/statuses")]
        [Authorize(Policy = "FacilityModify")]
        public async Task<ActionResult<FacilityStatusDto>> AddStatus(int id, [FromBody] FacilityStatusDto dto)
        {
            var userId = User.Identity.Name;
            dto.LocationId = id;
            var result = await _facilityService.AddStatusAsync(dto, userId);
            return Ok(result);
        }

        [HttpPut("{id}/statuses/{statusId}")]
        [Authorize(Policy = "FacilityModify")]
        public async Task<ActionResult<FacilityStatusDto>> UpdateStatus(int id, int statusId, [FromBody] FacilityStatusDto dto)
        {
            if (statusId != dto.FacilityStatusId)
                return BadRequest();

            var userId = User.Identity.Name;
            var result = await _facilityService.UpdateStatusAsync(dto, userId);
            return Ok(result);
        }

        [HttpDelete("{id}/statuses/{statusId}")]
        [Authorize(Policy = "FacilityModify")]
        public async Task<ActionResult> DeleteStatus(int id, int statusId)
        {
            await _facilityService.DeleteStatusAsync(statusId);
            return NoContent();
        }
    }
}
```

### Dependency Injection Registration
**Location**: `Startup.cs` or `Program.cs`

Add to services configuration:
```csharp
services.AddScoped<IFacilityRepository, FacilityRepository>();
services.AddScoped<IFacilityService, FacilityService>();
```

### Authorization Policies
**Location**: `Startup.cs` or `Program.cs`

Add to authorization configuration:
```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("FacilityReadOnly",
        policy => policy.RequireClaim("Permission", "FacilityReadOnly"));
    options.AddPolicy("FacilityModify",
        policy => policy.RequireClaim("Permission", "FacilityModify"));
});
```

## Pending Items - UI (BargeOps.Admin.UI)

### ViewModels
**Location**: `C:\source\BargeOps\BargeOps.Admin.UI\Models\`
**Reference**: Look for existing ViewModel patterns

#### Files to Create:

1. **FacilitySearchViewModel.cs**
2. **FacilityEditViewModel.cs**
3. **FacilityBerthViewModel.cs**
4. **FacilityStatusViewModel.cs**

### UI Service
**Location**: `C:\source\BargeOps\BargeOps.Admin.UI\Services\`

#### Files to Create:

1. **IFacilityService.cs** (Interface)
2. **FacilityService.cs** (Calls Admin.API via HttpClient)

### MVC Controller
**Location**: `C:\source\BargeOps\BargeOps.Admin.UI\Controllers\`
**Reference**: `BoatLocationSearchController.cs`

#### File to Create:

**FacilitySearchController.cs**

### Razor Views
**Location**: `C:\source\BargeOps\BargeOps.Admin.UI\Views\FacilitySearch\`

#### Files to Create:

1. **Index.cshtml** - Search page
2. **Edit.cshtml** - Detail form with tabs
3. **_StatusModal.cshtml** - Status add/edit modal
4. **_BerthModal.cshtml** - Berth add/edit modal

### JavaScript
**Location**: `C:\source\BargeOps\BargeOps.Admin.UI\wwwroot\js\`

#### Files to Create:

1. **facilitySearch.js** - DataTables, Select2, search logic
2. **facilityDetail.js** - Tab management, modals, validation

## Implementation Priority

### Immediate Next Steps:
1. **Expand FacilityDto.cs** to include all required DTOs
2. **Create IFacilityRepository.cs** interface
3. **Create FacilityRepository.cs** implementation
4. **Create IFacilityService.cs** interface
5. **Create FacilityService.cs** implementation

### Then:
6. Create FacilityMappingProfile.cs
7. Create FacilityController.cs
8. Register dependencies in Startup/Program.cs

### Finally (UI):
9. Create ViewModels
10. Create UI Service
11. Create MVC Controller
12. Create Razor Views
13. Create JavaScript files

## Testing Checklist

### API Testing:
- [ ] Test Search endpoint with various criteria
- [ ] Test GetById endpoint
- [ ] Test Create endpoint
- [ ] Test Update endpoint (including Lock/Gauge field clearing)
- [ ] Test Delete endpoint
- [ ] Test Lookup endpoints (Rivers, FacilityTypes)
- [ ] Test Berth CRUD endpoints
- [ ] Test Status CRUD endpoints
- [ ] Test authorization (FacilityReadOnly, FacilityModify)

### UI Testing:
- [ ] Test search functionality
- [ ] Test create new facility
- [ ] Test edit existing facility
- [ ] Test delete facility
- [ ] Test tab navigation
- [ ] Test conditional Lock/Gauge panel visibility
- [ ] Test berth add/edit/delete
- [ ] Test status add/edit/delete
- [ ] Test validation rules
- [ ] Test permission-based button visibility

## Notes

- Reference `BoatLocationController.cs` in BargeOps.Admin.API for API controller patterns
- Reference `BoatLocationSearchController.cs` in BargeOps.Admin.UI for MVC controller patterns
- Use Crewing examples for DataTables and Select2 implementations
- Follow existing code style and conventions
- All stored procedures should already exist in the database
- Ensure proper error handling and logging throughout

## Contact & Support

For questions during implementation:
- Review conversion-plan.md for detailed specifications
- Review TEMPLATE_GENERATION_SUMMARY.md for code examples
- Check analysis JSON files in output/Facility/ for business rules
- Reference BoatLocation implementations as primary pattern

---

**Last Updated**: 2025-11-11
**Status**: API Domain Models Complete, DTOs Need Expansion, Repository Layer Pending
