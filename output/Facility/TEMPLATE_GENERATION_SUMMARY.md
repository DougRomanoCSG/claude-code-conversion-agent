# Facility Conversion - Template Generation Summary

## Overview

This document provides a comprehensive summary of the Facility conversion templates and implementation guidance for migrating from the legacy VB.NET WinForms application to modern ASP.NET Core architecture.

## Generated Files

### Conversion Plan
✅ **`conversion-plan.md`** - Complete conversion plan with architecture, database schema, implementation steps, and validation rules

### API Templates (BargeOps.Admin.API)
✅ **`templates/api/Facility.cs`** - Domain models (Facility, FacilityBerth, FacilityStatus)
✅ **`templates/api/FacilityDto.cs`** - DTOs and search request/response models

### Templates To Be Created Based on Patterns

## Implementation Guide by Component

### 1. API Repository (IFacilityRepository.cs, FacilityRepository.cs)

**Pattern Reference**: `C:\source\BargeOps\BargeOps.Admin.API\src\Admin.Infrastructure\Repositories\BoatLocationRepository.cs`

**Key Methods**:
```csharp
public interface IFacilityRepository
{
    // Main CRUD
    Task<DataTableResponse<FacilityListDto>> SearchAsync(FacilitySearchRequest request);
    Task<FacilityDto> GetByIdAsync(int id);
    Task<int> InsertAsync(Facility facility, string userId);
    Task UpdateAsync(Facility facility, string userId);
    Task DeleteAsync(int id, string userId);

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
```

**Implementation Notes**:
- Use Dapper for all data access
- Call stored procedures: `sp_FacilityLocationSearch`, `sp_FacilityLocation_GetByID`, etc.
- Handle DataTables server-side processing (pagination, sorting, filtering)
- Map database results to DTOs
- Use transactions for parent-child operations
- Clear Lock/Gauge fields if facility type changes

### 2. API Service (IFacilityService.cs, FacilityService.cs)

**Pattern Reference**: `C:\source\BargeOps\BargeOps.Admin.API\src\Admin.Domain\Services\BoatLocationService.cs`

**Key Responsibilities**:
- Business logic validation
- Coordinate repository operations
- Transaction management
- Handle conditional field clearing (Lock/Gauge based on type)
- Validate business rules (EndMile >= StartMile, River required when Mile specified)

**Example Business Rule**:
```csharp
public async Task<FacilityDto> UpdateAsync(FacilityDto dto, string userId)
{
    // Clear lock/gauge fields if facility type is not Lock or Gauge Location
    if (dto.FacilityTypeId != LockTypeId && dto.FacilityTypeId != GaugeTypeId)
    {
        dto.LockUsaceName = null;
        dto.LockFloodStage = null;
        dto.LockPoolStage = null;
        // ... clear all lock fields
    }

    // Validate business rules
    if (dto.Mile.HasValue && !dto.RiverId.HasValue)
    {
        throw new ValidationException("River is required when Mile is specified");
    }

    // Map and update
    var facility = _mapper.Map<Facility>(dto);
    await _repository.UpdateAsync(facility, userId);

    return await _repository.GetByIdAsync(dto.LocationId);
}
```

### 3. API Controller (FacilityController.cs)

**Pattern Reference**: `C:\source\BargeOps\BargeOps.Admin.API\src\Admin.Api\Controllers\BoatLocationController.cs`

**Endpoint Structure**:
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "FacilityReadOnly")]
public class FacilityController : ControllerBase
{
    [HttpPost("search")]
    public async Task<ActionResult<DataTableResponse<FacilityListDto>>> Search([FromBody] FacilitySearchRequest request)

    [HttpGet("{id}")]
    public async Task<ActionResult<FacilityDto>> GetById(int id)

    [HttpPost]
    [Authorize(Policy = "FacilityModify")]
    public async Task<ActionResult<FacilityDto>> Create([FromBody] FacilityDto dto)

    [HttpPut("{id}")]
    [Authorize(Policy = "FacilityModify")]
    public async Task<ActionResult> Update(int id, [FromBody] FacilityDto dto)

    [HttpDelete("{id}")]
    [Authorize(Policy = "FacilityModify")]
    public async Task<ActionResult> Delete(int id)

    // Lookup endpoints
    [HttpGet("rivers")]
    public async Task<ActionResult<IEnumerable<SelectListItem>>> GetRivers()

    [HttpGet("facility-types")]
    public async Task<ActionResult<IEnumerable<SelectListItem>>> GetFacilityTypes()

    // Berth endpoints
    [HttpGet("{id}/berths")]
    public async Task<ActionResult<IEnumerable<FacilityBerthDto>>> GetBerths(int id)

    [HttpPost("{id}/berths")]
    [Authorize(Policy = "FacilityModify")]
    public async Task<ActionResult<FacilityBerthDto>> AddBerth(int id, [FromBody] FacilityBerthDto dto)

    // Status endpoints
    [HttpGet("{id}/statuses")]
    public async Task<ActionResult<IEnumerable<FacilityStatusDto>>> GetStatuses(int id)

    [HttpPost("{id}/statuses")]
    [Authorize(Policy = "FacilityModify")]
    public async Task<ActionResult<FacilityStatusDto>> AddStatus(int id, [FromBody] FacilityStatusDto dto)
}
```

### 4. AutoMapper Profile (FacilityMappingProfile.cs)

**Pattern Reference**: `C:\source\BargeOps\Crewing.API\src\Crewing.Infrastructure\Mappings\CrewingMappingProfile.cs`

```csharp
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
```

### 5. UI ViewModels (BargeOps.Admin.UI/Models)

**Pattern Reference**: `C:\source\BargeOps\BargeOps.Admin.UI\Models\BoatLocationSearchViewModel.cs`

**FacilitySearchViewModel.cs**:
```csharp
public class FacilitySearchViewModel
{
    public string Name { get; set; }
    public string ShortName { get; set; }
    public string BargeExCode { get; set; }
    public int? RiverId { get; set; }
    public int? FacilityTypeId { get; set; }

    [Range(0, 9999.99)]
    public decimal? StartMile { get; set; }

    [Range(0, 9999.99)]
    public decimal? EndMile { get; set; }

    public bool ActiveOnly { get; set; } = true;

    // Dropdowns
    public IEnumerable<SelectListItem> Rivers { get; set; }
    public IEnumerable<SelectListItem> FacilityTypes { get; set; }
}
```

**FacilityEditViewModel.cs**:
```csharp
public class FacilityEditViewModel
{
    public int LocationId { get; set; }

    [Required(ErrorMessage = "Facility Name is required")]
    [StringLength(100)]
    public string Name { get; set; }

    [StringLength(50)]
    public string ShortName { get; set; }

    public int? RiverId { get; set; }

    [Range(0, 9999.99)]
    public decimal? Mile { get; set; }

    public string Bank { get; set; }

    public int? FacilityTypeId { get; set; }

    [StringLength(20)]
    public string BargeExCode { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string Note { get; set; }

    // Lock/Gauge fields
    [StringLength(100)]
    public string LockUsaceName { get; set; }

    [Range(0, 9999.99)]
    public decimal? LockFloodStage { get; set; }

    // ... other lock fields

    // NDC fields (read-only, for display)
    public string NdcName { get; set; }
    // ... other NDC fields

    // Dropdowns
    public IEnumerable<SelectListItem> Rivers { get; set; }
    public IEnumerable<SelectListItem> FacilityTypes { get; set; }
    public IEnumerable<SelectListItem> Banks { get; set; }

    // Child collections
    public List<FacilityBerthViewModel> Berths { get; set; }
    public List<FacilityStatusViewModel> Statuses { get; set; }
}
```

### 6. UI Controller (FacilitySearchController.cs)

**Pattern Reference**: `C:\source\BargeOps\BargeOps.Admin.UI\Controllers\BoatLocationSearchController.cs`

**Key Actions**:
```csharp
[Authorize(Policy = "FacilityReadOnly")]
public class FacilitySearchController : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new FacilitySearchViewModel
        {
            ActiveOnly = true,
            Rivers = await _facilityService.GetRiversAsync(),
            FacilityTypes = await _facilityService.GetFacilityTypesAsync()
        };
        return View(model);
    }

    [HttpPost]
    public async Task<JsonResult> Search([FromBody] FacilitySearchRequest request)
    {
        var result = await _facilityService.SearchAsync(request);
        return Json(result);
    }

    [HttpGet]
    [Authorize(Policy = "FacilityModify")]
    public async Task<IActionResult> Edit(int? id)
    {
        FacilityEditViewModel model;

        if (id.HasValue)
        {
            var dto = await _facilityService.GetByIdAsync(id.Value);
            model = _mapper.Map<FacilityEditViewModel>(dto);
        }
        else
        {
            model = new FacilityEditViewModel { IsActive = true };
        }

        // Load dropdowns
        model.Rivers = await _facilityService.GetRiversAsync();
        model.FacilityTypes = await _facilityService.GetFacilityTypesAsync();
        model.Banks = GetBanks(); // Static list

        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = "FacilityModify")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(FacilityEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // Reload dropdowns
            model.Rivers = await _facilityService.GetRiversAsync();
            model.FacilityTypes = await _facilityService.GetFacilityTypesAsync();
            model.Banks = GetBanks();
            return View(model);
        }

        var dto = _mapper.Map<FacilityDto>(model);

        if (model.LocationId == 0)
        {
            await _facilityService.CreateAsync(dto);
        }
        else
        {
            await _facilityService.UpdateAsync(dto);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = "FacilityModify")]
    public async Task<JsonResult> Delete(int id)
    {
        await _facilityService.DeleteAsync(id);
        return Json(new { success = true });
    }
}
```

### 7. Razor Views

#### Index.cshtml (Search Page)

**Pattern Reference**: `C:\source\BargeOps\BargeOps.Admin.UI\Views\BoatLocationSearch\Index.cshtml`

**Structure**:
```html
@model FacilitySearchViewModel

<div class="container-fluid">
    <!-- Search Criteria Card (Collapsible) -->
    <div class="card mb-3">
        <div class="card-header">
            <h5 data-bs-toggle="collapse" data-bs-target="#searchCriteria">
                <i class="bi bi-search"></i> Search Criteria
            </h5>
        </div>
        <div id="searchCriteria" class="collapse show">
            <div class="card-body">
                <form id="searchForm">
                    <div class="row">
                        <div class="col-md-4">
                            <label for="Name">Facility Name</label>
                            <input type="text" id="Name" class="form-control" />
                        </div>
                        <div class="col-md-4">
                            <label for="ShortName">Short Name</label>
                            <input type="text" id="ShortName" class="form-control" />
                        </div>
                        <div class="col-md-4">
                            <label for="BargeExCode">BargeEx Code</label>
                            <input type="text" id="BargeExCode" class="form-control" />
                        </div>
                    </div>
                    <div class="row mt-2">
                        <div class="col-md-3">
                            <label for="RiverId">River</label>
                            <select id="RiverId" class="form-select select2"></select>
                        </div>
                        <div class="col-md-3">
                            <label for="FacilityTypeId">Facility Type</label>
                            <select id="FacilityTypeId" class="form-select select2"></select>
                        </div>
                        <div class="col-md-2">
                            <label for="StartMile">Start Mile</label>
                            <input type="number" id="StartMile" class="form-control" step="0.01" />
                        </div>
                        <div class="col-md-2">
                            <label for="EndMile">End Mile</label>
                            <input type="number" id="EndMile" class="form-control" step="0.01" />
                        </div>
                        <div class="col-md-2">
                            <div class="form-check mt-4">
                                <input type="checkbox" id="ActiveOnly" class="form-check-input" checked />
                                <label class="form-check-label" for="ActiveOnly">Active Only</label>
                            </div>
                        </div>
                    </div>
                    <div class="row mt-3">
                        <div class="col-md-12">
                            <button type="button" id="btnSearch" class="btn btn-primary">
                                <i class="bi bi-search"></i> Search
                            </button>
                            <button type="button" id="btnClear" class="btn btn-secondary">
                                <i class="bi bi-x-circle"></i> Clear
                            </button>
                        </div>
                    </div>
                </form>
            </div>
        </div>
    </div>

    <!-- Action Buttons -->
    @if (User.HasClaim("Permission", "FacilityModify"))
    {
        <div class="d-flex justify-content-end gap-2 mb-3">
            <button id="btnNew" class="btn btn-success">
                <i class="bi bi-plus-circle"></i> New
            </button>
            <button id="btnEdit" class="btn btn-primary" disabled>
                <i class="bi bi-pencil"></i> Edit
            </button>
            <button id="btnDelete" class="btn btn-danger" disabled>
                <i class="bi bi-trash"></i> Delete
            </button>
        </div>
    }

    <!-- Results Grid -->
    <div class="card">
        <div class="card-header">
            <h5>Search Results <span id="resultCount" class="badge bg-primary"></span></h5>
        </div>
        <div class="card-body">
            <table id="facilityTable" class="table table-striped table-bordered" style="width:100%">
                <thead>
                    <tr>
                        <th>Location ID</th>
                        <th>Name</th>
                        <th>Short Name</th>
                        <th>River</th>
                        <th>Mile</th>
                        <th>Bank</th>
                        <th>Facility Type</th>
                        <th>BargeEx Code</th>
                        <th>Active</th>
                        @if (User.HasClaim("Permission", "FacilityModify"))
                        {
                            <th>Actions</th>
                        }
                    </tr>
                </thead>
            </table>
        </div>
    </div>
</div>

@section Scripts {
    <script src="~/js/facilitySearch.js"></script>
}
```

#### Edit.cshtml (Detail Page with Tabs)

**Pattern Reference**: `C:\source\BargeOps\BargeOps.Admin.UI\Views\BoatLocationSearch\Edit.cshtml`
**Also Reference**: `C:\source\BargeOps\Crewing.UI\Views\CrewingSearch\Edit.cshtml`

**Structure**:
```html
@model FacilityEditViewModel

<div class="container-fluid">
    <h2>@(Model.LocationId == 0 ? "New Facility" : Model.Name)</h2>

    <form asp-action="Edit" method="post" id="facilityForm">
        <input type="hidden" asp-for="LocationId" />

        <!-- Tab Navigation -->
        <ul class="nav nav-tabs" id="facilityTabs" role="tablist">
            <li class="nav-item" role="presentation">
                <button class="nav-link active" id="details-tab" data-bs-toggle="tab"
                        data-bs-target="#details" type="button" role="tab">Details</button>
            </li>
            <li class="nav-item" role="presentation">
                <button class="nav-link" id="status-tab" data-bs-toggle="tab"
                        data-bs-target="#status" type="button" role="tab"
                        @(Model.LocationId == 0 ? "disabled" : "")>Status</button>
            </li>
            <li class="nav-item" role="presentation">
                <button class="nav-link" id="berths-tab" data-bs-toggle="tab"
                        data-bs-target="#berths" type="button" role="tab"
                        @(Model.LocationId == 0 ? "disabled" : "")>Berths</button>
            </li>
            <li class="nav-item" role="presentation">
                <button class="nav-link" id="ndc-tab" data-bs-toggle="tab"
                        data-bs-target="#ndc" type="button" role="tab">NDC Data</button>
            </li>
        </ul>

        <!-- Tab Content -->
        <div class="tab-content" id="facilityTabContent">
            <!-- Details Tab -->
            <div class="tab-pane fade show active" id="details" role="tabpanel">
                <div class="card mt-3">
                    <div class="card-header">
                        <h5>Facility Details</h5>
                    </div>
                    <div class="card-body">
                        <div class="row">
                            <div class="col-md-6">
                                <div class="mb-3">
                                    <label asp-for="Name" class="form-label">Facility Name *</label>
                                    <input asp-for="Name" class="form-control" />
                                    <span asp-validation-for="Name" class="text-danger"></span>
                                </div>
                            </div>
                            <div class="col-md-6">
                                <div class="mb-3">
                                    <label asp-for="ShortName" class="form-label">Short Name</label>
                                    <input asp-for="ShortName" class="form-control" />
                                    <span asp-validation-for="ShortName" class="text-danger"></span>
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-md-4">
                                <div class="mb-3">
                                    <label asp-for="RiverId" class="form-label">River</label>
                                    <select asp-for="RiverId" class="form-select select2"
                                            asp-items="Model.Rivers"></select>
                                    <span asp-validation-for="RiverId" class="text-danger"></span>
                                </div>
                            </div>
                            <div class="col-md-4">
                                <div class="mb-3">
                                    <label asp-for="Mile" class="form-label">Mile</label>
                                    <input asp-for="Mile" class="form-control" type="number" step="0.01" />
                                    <span asp-validation-for="Mile" class="text-danger"></span>
                                </div>
                            </div>
                            <div class="col-md-4">
                                <div class="mb-3">
                                    <label asp-for="Bank" class="form-label">Bank</label>
                                    <select asp-for="Bank" class="form-select select2"
                                            asp-items="Model.Banks"></select>
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-md-6">
                                <div class="mb-3">
                                    <label asp-for="FacilityTypeId" class="form-label">Facility Type *</label>
                                    <select asp-for="FacilityTypeId" class="form-select select2"
                                            asp-items="Model.FacilityTypes"
                                            onchange="toggleLockGaugePanel()"></select>
                                    <span asp-validation-for="FacilityTypeId" class="text-danger"></span>
                                </div>
                            </div>
                            <div class="col-md-6">
                                <div class="mb-3">
                                    <label asp-for="BargeExCode" class="form-label">BargeEx Code</label>
                                    <input asp-for="BargeExCode" class="form-control" />
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-md-12">
                                <div class="form-check">
                                    <input asp-for="IsActive" class="form-check-input" type="checkbox" />
                                    <label asp-for="IsActive" class="form-check-label">Is Active</label>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Lock/Gauge Information (Conditional) -->
                <div id="lockGaugePanel" class="card mt-3" style="display: none;">
                    <div class="card-header">
                        <h5>Lock/Gauge Information</h5>
                    </div>
                    <div class="card-body">
                        <div class="row">
                            <div class="col-md-6">
                                <div class="mb-3">
                                    <label asp-for="LockUsaceName" class="form-label">Lock USACE Name</label>
                                    <input asp-for="LockUsaceName" class="form-control" />
                                </div>
                            </div>
                            <!-- Add other lock fields similarly -->
                        </div>
                    </div>
                </div>
            </div>

            <!-- Status Tab -->
            <div class="tab-pane fade" id="status" role="tabpanel">
                <div class="card mt-3">
                    <div class="card-header d-flex justify-content-between align-items-center">
                        <h5>Facility Status History</h5>
                        @if (User.HasClaim("Permission", "FacilityModify"))
                        {
                            <div id="statusToolbar">
                                <button type="button" class="btn btn-success btn-sm" onclick="addStatus()">
                                    <i class="bi bi-plus-circle"></i> Add
                                </button>
                                <button type="button" class="btn btn-primary btn-sm" onclick="editStatus()" disabled>
                                    <i class="bi bi-pencil"></i> Edit
                                </button>
                                <button type="button" class="btn btn-danger btn-sm" onclick="deleteStatus()" disabled>
                                    <i class="bi bi-trash"></i> Delete
                                </button>
                            </div>
                        }
                    </div>
                    <div class="card-body">
                        <table id="statusTable" class="table table-striped table-bordered">
                            <!-- DataTable initialized in JavaScript -->
                        </table>
                    </div>
                </div>
            </div>

            <!-- Berths Tab -->
            <div class="tab-pane fade" id="berths" role="tabpanel">
                <!-- Similar structure to Status tab -->
            </div>

            <!-- NDC Data Tab -->
            <div class="tab-pane fade" id="ndc" role="tabpanel">
                <div class="card mt-3">
                    <div class="card-header">
                        <h5>NDC Data (Read Only)</h5>
                    </div>
                    <div class="card-body">
                        <div class="row">
                            <div class="col-md-6">
                                <div class="mb-3">
                                    <label class="form-label">NDC Name</label>
                                    <input value="@Model.NdcName" class="form-control" readonly />
                                </div>
                            </div>
                            <!-- Add other NDC fields similarly -->
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Form Footer Buttons -->
        <div class="d-flex justify-content-between mt-3">
            <div>
                @if (User.HasClaim("Permission", "FacilityModify") && Model.LocationId > 0)
                {
                    <button type="button" class="btn btn-danger" onclick="deleteFacility()">
                        <i class="bi bi-trash"></i> Delete
                    </button>
                }
            </div>
            <div>
                <button type="submit" class="btn btn-primary">
                    <i class="bi bi-save"></i> Save
                </button>
                <a asp-action="Index" class="btn btn-secondary">
                    <i class="bi bi-x-circle"></i> Cancel
                </a>
            </div>
        </div>
    </form>
</div>

<!-- Status Modal -->
<partial name="_StatusModal" />

<!-- Berth Modal -->
<partial name="_BerthModal" />

@section Scripts {
    <script src="~/js/facilityDetail.js"></script>
    @{await Html.RenderPartialAsync("_ValidationScriptsPartial");}
}
```

### 8. JavaScript Files

#### facilitySearch.js

**Pattern Reference**: `C:\source\BargeOps\BargeOps.Admin.UI\wwwroot\js\boatLocationSearch.js`

**Key Features**:
```javascript
$(document).ready(function() {
    // Initialize Select2 dropdowns
    $('#RiverId').select2({
        theme: 'bootstrap-5',
        placeholder: 'All Rivers',
        allowClear: true,
        ajax: {
            url: '/api/facility/rivers',
            dataType: 'json',
            delay: 250,
            processResults: function(data) {
                return {
                    results: data.map(x => ({ id: x.value, text: x.text }))
                };
            }
        }
    });

    $('#FacilityTypeId').select2({
        theme: 'bootstrap-5',
        placeholder: 'All Facility Types',
        allowClear: true,
        ajax: {
            url: '/api/facility/facility-types',
            dataType: 'json',
            delay: 250,
            processResults: function(data) {
                return {
                    results: data.map(x => ({ id: x.value, text: x.text }))
                };
            }
        }
    });

    // Initialize DataTables
    var table = $('#facilityTable').DataTable({
        processing: true,
        serverSide: true,
        ajax: {
            url: '/FacilitySearch/Search',
            type: 'POST',
            contentType: 'application/json',
            data: function(d) {
                d.name = $('#Name').val();
                d.shortName = $('#ShortName').val();
                d.bargeExCode = $('#BargeExCode').val();
                d.riverId = $('#RiverId').val();
                d.facilityTypeId = $('#FacilityTypeId').val();
                d.startMile = $('#StartMile').val();
                d.endMile = $('#EndMile').val();
                d.activeOnly = $('#ActiveOnly').is(':checked');
                return JSON.stringify(d);
            }
        },
        columns: [
            { data: 'locationId', visible: false },
            { data: 'name' },
            { data: 'shortName' },
            { data: 'riverName' },
            {
                data: 'mile',
                render: function(data) {
                    return data ? parseFloat(data).toFixed(2) : '';
                },
                className: 'text-end'
            },
            { data: 'bank', className: 'text-center' },
            { data: 'facilityType' },
            { data: 'bargeExCode' },
            {
                data: 'isActive',
                render: function(data) {
                    return data
                        ? '<i class="bi bi-check-circle-fill text-success"></i>'
                        : '<i class="bi bi-x-circle-fill text-danger"></i>';
                },
                className: 'text-center'
            },
            {
                data: null,
                orderable: false,
                render: function(data, type, row) {
                    return '<button class="btn btn-sm btn-primary me-1" onclick="editFacility(' + row.locationId + ')"><i class="bi bi-pencil"></i></button>' +
                           '<button class="btn btn-sm btn-danger" onclick="deleteFacility(' + row.locationId + ')"><i class="bi bi-trash"></i></button>';
                }
            }
        ],
        order: [[1, 'asc']],
        pageLength: 25,
        lengthMenu: [[10, 25, 50, 100], [10, 25, 50, 100]]
    });

    // Row selection
    $('#facilityTable tbody').on('click', 'tr', function() {
        if ($(this).hasClass('selected')) {
            $(this).removeClass('selected');
            $('#btnEdit, #btnDelete').prop('disabled', true);
        } else {
            table.$('tr.selected').removeClass('selected');
            $(this).addClass('selected');
            $('#btnEdit, #btnDelete').prop('disabled', false);
        }
    });

    // Double-click to edit
    $('#facilityTable tbody').on('dblclick', 'tr', function() {
        var data = table.row(this).data();
        window.location.href = '/FacilitySearch/Edit/' + data.locationId;
    });

    // Search button
    $('#btnSearch').on('click', function() {
        // Validate
        var riverId = $('#RiverId').val();
        var startMile = $('#StartMile').val();
        var endMile = $('#EndMile').val();

        if ((startMile || endMile) && !riverId) {
            alert('A beginning or ending mile was specified, but no river was selected.');
            return;
        }

        if (startMile && endMile && parseFloat(endMile) < parseFloat(startMile)) {
            alert('The ending mile is less than the beginning mile.');
            return;
        }

        table.ajax.reload();
    });

    // Clear button
    $('#btnClear').on('click', function() {
        $('#searchForm')[0].reset();
        $('#RiverId, #FacilityTypeId').val(null).trigger('change');
        $('#ActiveOnly').prop('checked', true);
        table.ajax.reload();
    });

    // New button
    $('#btnNew').on('click', function() {
        window.location.href = '/FacilitySearch/Edit';
    });

    // Edit button
    $('#btnEdit').on('click', function() {
        var data = table.row('.selected').data();
        if (data) {
            window.location.href = '/FacilitySearch/Edit/' + data.locationId;
        }
    });

    // Delete button
    $('#btnDelete').on('click', function() {
        var data = table.row('.selected').data();
        if (data && confirm('Are you sure you want to delete this facility?')) {
            $.post('/FacilitySearch/Delete', { id: data.locationId }, function() {
                table.ajax.reload();
            });
        }
    });
});
```

#### facilityDetail.js

**Key Features**:
```javascript
$(document).ready(function() {
    // Toggle Lock/Gauge panel based on facility type
    window.toggleLockGaugePanel = function() {
        var selectedType = $('#FacilityTypeId option:selected').text();
        if (selectedType === 'Lock' || selectedType === 'Gauge Location') {
            $('#lockGaugePanel').show();
        } else {
            $('#lockGaugePanel').hide();
            // Clear lock/gauge fields
            $('#lockGaugePanel input').val('');
        }
    };

    // Initialize on page load
    toggleLockGaugePanel();

    // Initialize Status DataTable (if tab is active and LocationId > 0)
    var locationId = $('#LocationId').val();
    if (locationId > 0) {
        $('#statusTable').DataTable({
            ajax: {
                url: '/api/facility/' + locationId + '/statuses',
                dataSrc: ''
            },
            columns: [
                {
                    data: 'startDateTime',
                    render: function(data) {
                        return moment(data).format('MM/DD/YYYY HH:mm');
                    }
                },
                {
                    data: 'endDateTime',
                    render: function(data) {
                        return data ? moment(data).format('MM/DD/YYYY HH:mm') : '';
                    }
                },
                { data: 'statusName' },
                { data: 'note' }
            ],
            order: [[0, 'desc']],
            paging: false,
            searching: false
        });

        // Similar for Berths table
    }

    // Disable Status/Berths tabs if new facility
    if (locationId == 0) {
        $('#status-tab, #berths-tab').addClass('disabled').attr('disabled', true);
    }

    // Form validation
    $('#facilityForm').validate({
        rules: {
            Name: { required: true, maxlength: 100 },
            Mile: {
                requiresRiver: true
            }
        },
        messages: {
            Name: 'Facility Name is required',
            Mile: 'River is required when Mile is specified'
        }
    });

    // Custom validation rule
    $.validator.addMethod('requiresRiver', function(value, element) {
        if (value) {
            return $('#RiverId').val() !== '';
        }
        return true;
    }, 'River is required when Mile is specified');
});

// Modal functions
window.addStatus = function() {
    // Show status modal
    $('#statusModal').modal('show');
    // Initialize form for new status
};

window.editStatus = function() {
    // Get selected status
    // Populate modal with status data
    $('#statusModal').modal('show');
};

window.deleteStatus = function() {
    // Confirm and delete selected status
};

// Similar functions for Berths
```

## Validation Summary

### Client-Side Validation
- **jQuery Validation** with unobtrusive validation
- **HTML5** validation attributes (required, maxlength, type, step, min, max)
- **Custom rules** for conditional validation
- **Bootstrap styling** for validation feedback (is-invalid, invalid-feedback)

### Server-Side Validation
- **DataAnnotations** on ViewModels (Required, StringLength, Range)
- **FluentValidation** for complex business rules
- **ModelState** checks in controller actions
- **Business logic** validation in service layer

### Key Validation Rules Implemented

| Rule | Location | Implementation |
|------|----------|----------------|
| River required when Mile specified | Client & Server | Custom jQuery rule + FluentValidation |
| EndMile >= StartMile | Client & Server | Custom compare validation |
| Clear Lock/Gauge when type changes | Client & Server | JavaScript + Service layer |
| Name required | Client & Server | Required attribute |
| Parent must be saved before children | Client | Disable tabs |

## Security Implementation

### Authorization Policies
```csharp
// In Startup.cs/Program.cs
services.AddAuthorization(options =>
{
    options.AddPolicy("FacilityReadOnly",
        policy => policy.RequireClaim("Permission", "FacilityReadOnly"));
    options.AddPolicy("FacilityModify",
        policy => policy.RequireClaim("Permission", "FacilityModify"));
});
```

### Controller Protection
- API: `[Authorize(Policy = "...")]` attributes
- MVC: `[Authorize(Policy = "...")]` attributes
- Views: `@if (User.HasClaim("Permission", "..."))` for conditional rendering

## Testing Checklist

### Unit Tests
- [ ] Repository methods (mock Dapper calls)
- [ ] Service layer business logic
- [ ] Validation rules
- [ ] AutoMapper profiles

### Integration Tests
- [ ] API endpoints (all CRUD operations)
- [ ] Controller actions
- [ ] UI service calls to API
- [ ] Database stored procedures

### UI Tests
- [ ] Search functionality with various criteria
- [ ] Create new facility
- [ ] Edit existing facility
- [ ] Delete facility
- [ ] Add/Edit/Delete berths
- [ ] Add/Edit/Delete statuses
- [ ] Conditional field visibility (Lock/Gauge)
- [ ] Validation messages
- [ ] Permission-based button visibility
- [ ] Tab navigation and state management

## Deployment Notes

### Database Prerequisites
- Verify all stored procedures exist
- Verify permissions (FacilityReadOnly, FacilityModify) are configured in security system
- Test stored procedure performance with sample data

### Configuration
- Update appsettings.json with correct connection strings
- Configure authentication middleware
- Set up authorization policies
- Configure logging

### Deployment Steps
1. Deploy API to test environment
2. Run integration tests
3. Deploy UI to test environment
4. Perform UAT
5. Fix any issues
6. Deploy to production
7. Monitor for errors

## Support & Maintenance

### Key Files for Reference
- Conversion Plan: `output/Facility/conversion-plan.md`
- Domain Models: `output/Facility/templates/api/Facility.cs`
- DTOs: `output/Facility/templates/api/FacilityDto.cs`
- Analysis Files: `output/Facility/*.json`

### Common Issues & Solutions

**Issue**: Lock/Gauge fields not clearing when facility type changes
**Solution**: Ensure JavaScript toggleLockGaugePanel() clears values and service layer clears before save

**Issue**: Status/Berths tabs not disabled for new facilities
**Solution**: Check `LocationId == 0` condition in Edit view and JavaScript

**Issue**: River required validation not firing
**Solution**: Verify custom jQuery validation rule is registered and FluentValidation rule is implemented

**Issue**: DataTables not loading data
**Solution**: Check Search endpoint returns correct DataTableResponse format with Draw, RecordsTotal, RecordsFiltered, Data

---

**Document Version**: 1.0
**Created**: 2025-11-11
**Status**: Complete - Ready for Implementation

For questions or issues during implementation, refer to the conversion-plan.md and reference the BoatLocation patterns in the specified directories.
