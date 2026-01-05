# BoatStatus Conversion Plan

## Overview

This document provides a comprehensive plan for converting the legacy **frmBoatStatus** form to the modern ASP.NET Core MVC architecture with MONO SHARED structure.

**Entity**: BoatMaintenanceLog (child of BoatLocation)
**Legacy Form**: frmBoatStatus (OnShore/apps/Onshore/Forms/frmBoatStatus.vb)
**Form Type**: Detail-Edit-List (Master-Detail with inline editing)
**Parent Entity**: BoatLocation

## Key Characteristics

### Unique Complexity
1. **Conditional Field Display**: Form shows different fields based on MaintenanceType selection:
   - **Boat Status**: Shows Status field
   - **Change Division/Facility**: Shows Division and Port Facility fields
   - **Change Boat Role**: Shows Boat Role field

2. **Split DateTime Pattern**: StartDateTime uses separate date and time inputs (24-hour format)

3. **Cascading Dropdowns**: Port Facility depends on Division AND whether boat is a fleet boat

4. **Readonly MaintenanceType**: Cannot change maintenance type when editing existing records

5. **Field Clearing Logic**: Before save, clears fields not applicable to selected MaintenanceType

## Architecture Overview

### MONO SHARED Structure

⭐ **CRITICAL**: This project uses MONO SHARED architecture where DTOs are the SINGLE SOURCE OF TRUTH!

```
BargeOps.Shared (C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\BargeOps.Shared)
├── Dto/
│   ├── Boat MaintenanceLogDto.cs           ⭐ Complete entity DTO with [Sortable]/[Filterable]
│   ├── BoatMaintenanceLogSearchRequest.cs   ⭐ Search criteria DTO
│   └── (NO Models folder - DTOs ARE the models!)

BargeOps.API (C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API)
├── Admin.Infrastructure/Repositories/
│   ├── IBoatMaintenanceLogRepository.cs     Returns DTOs directly
│   └── BoatMaintenanceLogRepository.cs      Dapper with DIRECT SQL (NOT SPs)
├── Admin.Infrastructure/Services/
│   ├── IBoatMaintenanceLogService.cs
│   └── BoatMaintenanceLogService.cs         Uses DTOs (no mapping!)
└── Admin.Api/Controllers/
    └── BoatMaintenanceLogController.cs      RESTful with [ApiKey]

BargeOps.UI (C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI)
├── ViewModels/
│   └── BoatStatusEditViewModel.cs           Contains DTOs from Shared
├── Services/
│   ├── IBoatMaintenanceLogService.cs
│   └── BoatMaintenanceLogService.cs         HTTP client returns DTOs
├── Controllers/
│   └── BoatStatusController.cs              MVC controller
├── Views/BoatStatus/
│   └── Edit.cshtml                          Detail-Edit view
└── wwwroot/js/
    └── boatStatus.js                        DataTables + conditional logic
```

## Implementation Steps

### Phase 1: Shared Project (Create FIRST!)

#### Step 1.1: Create BoatMaintenanceLogDto.cs
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\BargeOps.Shared\Dto\BoatMaintenanceLogDto.cs`

**Key Points**:
- Complete entity DTO with ALL fields
- Used by BOTH API and UI (no separate domain models)
- Add `[Sortable]` and `[Filterable]` attributes for DataTables
- Include display properties (PortFacility, BoatRole)

```csharp
namespace BargeOps.Shared.Dto;

public class BoatMaintenanceLogDto
{
    [Sortable]
    public int BoatMaintenanceLogId { get; set; }

    public int LocationId { get; set; }

    [Filterable]
    [Sortable]
    public string? Division { get; set; }

    public int? PortFacilityId { get; set; }

    [Sortable]
    public string? PortFacility { get; set; }  // Display name from join

    [Required]
    [Filterable]
    [Sortable]
    public string MaintenanceType { get; set; } = string.Empty;

    [Required]
    [Sortable]
    public DateTime StartDateTime { get; set; }

    [Filterable]
    [Sortable]
    public string? Status { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public int? BoatRoleId { get; set; }

    [Sortable]
    public string? BoatRole { get; set; }  // Display name from join

    public int? DeckLogActivityId { get; set; }

    public DateTime? ModifyDateTime { get; set; }

    public string? ModifyUser { get; set; }
}
```

#### Step 1.2: Create BoatMaintenanceLogSearchRequest.cs
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\BargeOps.Shared\Dto\BoatMaintenanceLogSearchRequest.cs`

```csharp
namespace BargeOps.Shared.Dto;

public class BoatMaintenanceLogSearchRequest
{
    public int? LocationId { get; set; }
    public string? MaintenanceType { get; set; }
    public string? Status { get; set; }
    public string? Division { get; set; }
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
}
```

### Phase 2: API Infrastructure

#### Step 2.1: Create Repository Interface
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Repositories\IBoatMaintenanceLogRepository.cs`

**Key Methods**:
```csharp
Task<BoatMaintenanceLogDto?> GetByIdAsync(int boatMaintenanceLogId);
Task<IEnumerable<BoatMaintenanceLogDto>> GetByLocationIdAsync(int locationId);
Task<int> CreateAsync(BoatMaintenanceLogDto log);
Task UpdateAsync(BoatMaintenanceLogDto log);
Task DeleteAsync(int boatMaintenanceLogId);
```

#### Step 2.2: Create Repository Implementation
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Repositories\BoatMaintenanceLogRepository.cs`

**CRITICAL**: Use **DIRECT SQL QUERIES**, NOT stored procedures!

```sql
-- GetByIdAsync SQL
SELECT
    bml.BoatMaintenanceLogID,
    bml.LocationID,
    bml.Division,
    bml.PortFacilityID,
    pf.LocationName AS PortFacility,
    bml.MaintenanceType,
    bml.StartDateTime,
    bml.Status,
    bml.Note,
    bml.BoatRoleID,
    br.BoatRole,
    bml.DeckLogActivityID,
    bml.ModifyDateTime,
    bml.ModifyUser
FROM BoatMaintenanceLog bml
LEFT JOIN Location pf ON pf.LocationID = bml.PortFacilityID
LEFT JOIN BoatRole br ON br.BoatRoleID = bml.BoatRoleID
WHERE bml.BoatMaintenanceLogID = @BoatMaintenanceLogId
```

#### Step 2.3: Create Service Interface and Implementation
**Location**:
- Interface: `Admin.Domain/Services/IBoatMaintenanceLogService.cs`
- Implementation: `Admin.Infrastructure/Services/BoatMaintenanceLogService.cs`

**Services use DTOs directly** - no mapping needed!

#### Step 2.4: Create API Controller
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Api\Controllers\BoatMaintenanceLogController.cs`

**Authentication**: `[ApiKey]` attribute

**Endpoints**:
- `GET /api/boat/{boatId}/maintenance-logs` - Get all for boat
- `GET /api/boat-maintenance-log/{id}` - Get by ID
- `POST /api/boat-maintenance-log` - Create
- `PUT /api/boat-maintenance-log/{id}` - Update
- `DELETE /api/boat-maintenance-log/{id}` - Delete

### Phase 3: UI Implementation

#### Step 3.1: Create ViewModel
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\ViewModels\BoatStatusEditViewModel.cs`

**MVVM Pattern**: NO ViewBag/ViewData - all data on ViewModel

```csharp
namespace BargeOpsAdmin.ViewModels;

public class BoatStatusEditViewModel
{
    // Main DTO from Shared
    public BoatMaintenanceLogDto MaintenanceLog { get; set; } = new();

    // Parent info
    public int LocationID { get; set; }  // Uppercase ID!
    public string BoatName { get; set; } = string.Empty;

    // Dropdowns (IEnumerable<SelectListItem>)
    public IEnumerable<SelectListItem> StatusList { get; set; } = [];
    public IEnumerable<SelectListItem> Divisions { get; set; } = [];
    public IEnumerable<SelectListItem> PortFacilities { get; set; } = [];
    public IEnumerable<SelectListItem> BoatRoles { get; set; } = [];

    // State flags
    public bool IsNew { get; set; }
    public bool IsFleetBoat { get; set; }

    // All maintenance logs for grid
    public List<BoatMaintenanceLogDto> MaintenanceLogs { get; set; } = [];
}
```

**DateTime Handling**:
- ViewModel has single `DateTime` property
- View splits into date + time inputs
- JavaScript combines on submit

#### Step 3.2: Create UI API Client Service
**Location**:
- Interface: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Services\IBoatMaintenanceLogService.cs`
- Implementation: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Services\BoatMaintenanceLogService.cs`

**Returns DTOs** from BargeOps.Shared directly!

#### Step 3.3: Create UI Controller
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Controllers\BoatStatusController.cs`

**Authentication**: `[Authorize]` with IdentityConstants.ApplicationScheme

**Actions**:
- `GET Edit/{locationId}` - Open boat status form
- `POST SaveMaintenanceLog` - Save (create/update)
- `POST Delete/{id}` - Delete
- `POST GetMaintenanceLog` - DataTables server-side
- `GET GetPortFacilitiesByDivision` - Cascading dropdown

#### Step 3.4: Create Razor View
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Views\BoatStatus\Edit.cshtml`

**Pattern**: Master-detail layout
- Top: DataTables grid with maintenance log history
- Bottom: Detail edit form (initially hidden)

**CRITICAL DateTime Pattern**:
```html
<div class="row mb-3">
    <div class="col-md-6">
        <label asp-for="MaintenanceLog.StartDateTime" class="form-label required">Start Date</label>
        <input type="date" class="form-control" id="dtStartDate" />
    </div>
    <div class="col-md-6">
        <label class="form-label required">Start Time (24-hour)</label>
        <input type="time" class="form-control" id="dtStartTime" />
    </div>
</div>
```

**Conditional Fields**:
```html
<!-- Maintenance Type Radio Buttons -->
<div class="form-check">
    <input class="form-check-input" type="radio" name="MaintenanceType"
           id="optBoatStatus" value="Boat Status" />
    <label class="form-check-label" for="optBoatStatus">Boat Status</label>
</div>

<!-- Status Section (visible when Boat Status selected) -->
<div class="mb-3" id="statusSection" style="display:none;">
    <label asp-for="MaintenanceLog.Status" class="form-label required">Status</label>
    <select asp-for="MaintenanceLog.Status" class="form-select"
            data-select2="true" id="cboStatus">
        <option value="">-- Select Status --</option>
    </select>
</div>
```

#### Step 3.5: Create JavaScript
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\wwwroot\js\boatStatus.js`

**Key Functions**:
1. **DataTables Initialization** (server-side processing)
2. **Split/Combine DateTime**
3. **Conditional Field Enabling** based on MaintenanceType
4. **Cascading Dropdown** (Division → PortFacility)
5. **Clear Unused Fields** before save
6. **Readonly MaintenanceType** when editing

```javascript
// Conditional field enabling
function enableDisableFields() {
    var selectedType = $('input[name="MaintenanceType"]:checked').val();

    $('#statusSection, #divisionSection, #facilitySection, #boatRoleSection').hide();
    $('#cboStatus, #cboDivision, #cboPortFacility, #cboBoatRole').prop('disabled', true);

    if (selectedType === 'Boat Status') {
        $('#statusSection').show();
        $('#cboStatus').prop('disabled', false);
    } else if (selectedType === 'Change Division/Facility') {
        $('#divisionSection, #facilitySection').show();
        $('#cboDivision, #cboPortFacility').prop('disabled', false);
    } else if (selectedType === 'Change Boat Role') {
        $('#boatRoleSection').show();
        $('#cboBoatRole').prop('disabled', false);
    }
}
```

## Business Logic Migration

### Conditional Validation

**Legacy**: CheckBusinessRules in BoatMaintenanceLog.vb

**Modern**: FluentValidation with conditional rules

```csharp
// Status required when MaintenanceType = 'Boat Status'
RuleFor(x => x.Status)
    .NotEmpty()
    .When(x => x.MaintenanceType == "Boat Status")
    .WithMessage("If Type is 'Boat Status', then Status is required.");

// Status must be blank when NOT 'Boat Status'
RuleFor(x => x.Status)
    .Empty()
    .When(x => x.MaintenanceType != "Boat Status")
    .WithMessage("Status must be blank unless Type is 'Boat Status'.");
```

### Field Clearing Logic

**Before Save**: Clear unused fields based on MaintenanceType

```javascript
function clearUnusedFields() {
    var selectedType = $('input[name="MaintenanceType"]:checked').val();

    if (selectedType !== 'Boat Status') {
        $('#cboStatus').val('').trigger('change.select2');
    }
    if (selectedType !== 'Change Division/Facility') {
        $('#cboDivision, #cboPortFacility').val('').trigger('change.select2');
    }
    if (selectedType !== 'Change Boat Role') {
        $('#cboBoatRole').val('').trigger('change.select2');
    }
}
```

### Cascading Dropdown Logic

**Division → PortFacility**:

```javascript
$('#cboDivision').on('change', function() {
    var divisionId = $(this).val();
    var $portFacility = $('#cboPortFacility');

    if (divisionId) {
        $.get('/BoatStatus/GetPortFacilitiesByDivision',
            { divisionId: divisionId },
            function(data) {
                $portFacility.empty().append('<option value="">-- Select Port Facility --</option>');
                $.each(data, function(i, item) {
                    $portFacility.append($('<option>', {
                        value: item.value,
                        text: item.text
                    }));
                });
                $portFacility.trigger('change.select2');
            });
    } else {
        $portFacility.empty().append('<option value="">-- Select Port Facility --</option>')
                     .trigger('change.select2');
    }
});
```

## Data Access Migration

### Legacy Pattern
- Stored procedures: `BoatMaintenanceLogSelect`, `BoatMaintenanceLogInsert`, `BoatMaintenanceLogUpdate`, `BoatMaintenanceLogDelete`
- Uses `SafeDataReader` and `SqlHelper`

### Modern Pattern
**Direct SQL queries** with Dapper (NOT stored procedures!)

```csharp
public async Task<BoatMaintenanceLogDto?> GetByIdAsync(int boatMaintenanceLogId)
{
    const string sql = @"
        SELECT
            bml.BoatMaintenanceLogID,
            bml.LocationID,
            bml.Division,
            bml.PortFacilityID,
            pf.LocationName AS PortFacility,
            bml.MaintenanceType,
            bml.StartDateTime,
            bml.Status,
            bml.Note,
            bml.BoatRoleID,
            br.BoatRole,
            bml.DeckLogActivityID,
            bml.ModifyDateTime,
            bml.ModifyUser
        FROM BoatMaintenanceLog bml
        LEFT JOIN Location pf ON pf.LocationID = bml.PortFacilityID
        LEFT JOIN BoatRole br ON br.BoatRoleID = bml.BoatRoleID
        WHERE bml.BoatMaintenanceLogID = @BoatMaintenanceLogId";

    using var connection = await _connectionFactory.CreateConnectionAsync();
    return await connection.QuerySingleOrDefaultAsync<BoatMaintenanceLogDto>(
        sql,
        new { BoatMaintenanceLogId = boatMaintenanceLogId }
    );
}
```

## Security Migration

### Legacy Security
- Uses CSG Windows Forms Security framework
- `ControlAuthorization.ToolbarButtonCanBeEnabled`
- ButtonTypes: Add, Open (Modify/Export), Remove, Submit, Cancel, CancelEdit

### Modern Security

**API**: `[ApiKey]` attribute
```csharp
[ApiController]
[Route("api/boat-maintenance-log")]
[ApiKey]
public class BoatMaintenanceLogController : ControllerBase
{
    // All endpoints require API key
}
```

**UI**: `[Authorize]` with policy-based authorization
```csharp
[Authorize(Policy = "BoatStatusModify")]
public async Task<IActionResult> SaveMaintenanceLog(BoatStatusEditViewModel model)
{
    // Save logic
}
```

**Permission Enum** (add to `AuthPermissions.cs`):
```csharp
public enum AuthPermissions
{
    BoatStatusView,      // View logs
    BoatStatusModify     // Create/Edit/Delete
}
```

## Testing Checklist

### Unit Tests
- [ ] Repository CRUD operations
- [ ] Service business logic
- [ ] Controller endpoints
- [ ] Validation rules (all conditional scenarios)

### Integration Tests
- [ ] API endpoints with authentication
- [ ] Database operations (create, update, delete)
- [ ] Cascading dropdown logic

### UI Tests
- [ ] DateTime split/combine logic
- [ ] Conditional field display based on MaintenanceType
- [ ] MaintenanceType readonly when editing
- [ ] Field clearing before save
- [ ] DataTables server-side processing
- [ ] Cascading Division → PortFacility

### Business Logic Tests
- [ ] Status required when MaintenanceType = 'Boat Status'
- [ ] Division required when MaintenanceType = 'Change Division/Facility'
- [ ] BoatRole required when MaintenanceType = 'Change Boat Role'
- [ ] Unused fields cleared before save
- [ ] MaxLength validation for all fields

## Reference Examples

### Shared DTOs
- **FacilityDto.cs**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\BargeOps.Shared\Dto\FacilityDto.cs`
- **BoatLocationDto.cs**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\BargeOps.Shared\Dto\BoatLocationDto.cs`

### API Layer
- **FacilityController.cs**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Api\Controllers\FacilityController.cs`
- **FacilityRepository.cs**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Repositories\FacilityRepository.cs`

### UI Layer
- **BoatLocationSearchController.cs**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Controllers\BoatLocationSearchController.cs`
- **boatLocationSearch.js**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\wwwroot\js\boatLocationSearch.js`

## Critical Implementation Notes

1. **⭐ DTOs are the ONLY data models** - no separate domain models in API
2. **⭐ Use DIRECT SQL queries** - not stored procedures
3. **⭐ DateTime MUST be split** into date + time (24-hour format)
4. **⭐ MaintenanceType is readonly** when editing existing records
5. **⭐ Clear unused fields** before save based on MaintenanceType
6. **⭐ Conditional validation** - fields required based on MaintenanceType
7. **⭐ Cascading dropdowns** - PortFacility depends on Division AND IsFleetBoat
8. **⭐ DataTables server-side** processing for grid
9. **⭐ Select2 for all dropdowns**
10. **⭐ Bootstrap 5** for all styling

## Maintenance Type Behavior Matrix

| Maintenance Type | Enabled Fields | Required Fields | Cleared Fields |
|---|---|---|---|
| Boat Status | Status, StartDateTime, Note | Status, StartDateTime | Division, PortFacilityID, BoatRoleID |
| Change Division/Facility | Division, PortFacilityID, StartDateTime, Note | Division, StartDateTime | Status, BoatRoleID |
| Change Boat Role | BoatRoleID, StartDateTime, Note | BoatRoleID, StartDateTime | Status, Division, PortFacilityID |

## Data Flow

### Add New Record
1. User clicks Add button
2. Detail section shown, MaintenanceType enabled
3. User selects MaintenanceType → conditional fields enabled
4. User enters data
5. Before save: Clear unused fields based on MaintenanceType
6. Validate (conditional rules)
7. POST to API
8. Refresh DataTables grid

### Edit Existing Record
1. User clicks Edit on grid row
2. Detail section shown, MaintenanceType **disabled** (readonly)
3. Load data, enable fields based on MaintenanceType
4. User edits data
5. Before save: Clear unused fields
6. Validate
7. PUT to API
8. Refresh grid

## Next Steps

1. ✅ Review this conversion plan
2. ⏭️ Generate Shared DTO templates
3. ⏭️ Generate API layer templates
4. ⏭️ Generate UI layer templates
5. ⏭️ Generate JavaScript templates
6. ⏭️ Review and refine templates

---

**Generated**: 2025-12-11
**Agent**: Conversion Template Generator (Interactive Mode)
**Entity**: BoatStatus (BoatMaintenanceLog)
