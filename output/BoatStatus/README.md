# BoatStatus Conversion Templates - Complete Package

**Generated**: 2025-12-11
**Entity**: BoatMaintenanceLog (Child of BoatLocation)
**Legacy Form**: frmBoatStatus
**Form Type**: Detail-Edit-List (Master-Detail)

## 📋 Overview

This package contains complete conversion templates for migrating the legacy **frmBoatStatus** Windows Forms application to a modern ASP.NET Core MVC architecture using the **MONO SHARED** pattern.

### Key Complexity Points

1. **Conditional Field Display** - Different fields shown based on MaintenanceType selection
2. **Split DateTime Pattern** - Separate date + time (24-hour format) inputs
3. **Cascading Dropdowns** - PortFacility depends on Division AND IsFleetBoat
4. **Readonly MaintenanceType** - Cannot change type when editing existing records
5. **Field Clearing Logic** - Clears unused fields before save based on MaintenanceType

## 📁 Generated Files

### Documentation
- ✅ **conversion-plan.md** - Comprehensive conversion guide with architecture, implementation steps, and testing checklist

### Shared Project (⭐ CREATE FIRST!)
Location: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\BargeOps.Shared\Dto\`

- ✅ **BoatMaintenanceLogDto.cs** - Complete entity DTO with [Sortable]/[Filterable] attributes
- ✅ **BoatMaintenanceLogSearchRequest.cs** - Search criteria DTO

### API Layer
Location: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\`

#### Repositories (`src\Admin.Infrastructure\Repositories\`)
- ✅ **IBoatMaintenanceLogRepository.cs** - Repository interface
- ✅ **BoatMaintenanceLogRepository.cs** - Dapper implementation with DIRECT SQL (NOT SPs)

#### Services (`src\Admin.Infrastructure\Services\`)
- ✅ **IBoatMaintenanceLogService.cs** - Service interface
- ✅ **BoatMaintenanceLogService.cs** - Business logic with conditional validation

#### Controllers (`src\Admin.Api\Controllers\`)
- ✅ **BoatMaintenanceLogController.cs** - RESTful API with [ApiKey] authentication

### UI Layer
Location: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\`

#### ViewModels (`ViewModels\`)
- ✅ **BoatStatusEditViewModel.cs** - MVVM pattern (NO ViewBag/ViewData)

#### Services (`Services\`)
- ✅ **IBoatMaintenanceLogService.cs** - UI service interface
- ✅ **BoatMaintenanceLogService.cs** - HTTP client service

#### Controllers (`Controllers\`)
- ✅ **BoatStatusController.cs** - MVC controller with [Authorize]

#### Views (`Views\BoatStatus\`)
- ✅ **Edit.cshtml** - Master-detail layout with conditional field display

#### JavaScript (`wwwroot\js\`)
- ✅ **boatStatus.js** - DataTables + conditional logic + split/combine DateTime

## 🎯 Implementation Order

**CRITICAL**: Follow this exact order to ensure proper dependencies!

### 1. Shared DTOs (Create FIRST!)
```
BargeOps.Shared/Dto/
├── BoatMaintenanceLogDto.cs
└── BoatMaintenanceLogSearchRequest.cs
```

### 2. API Infrastructure
```
Admin.Infrastructure/Repositories/
├── IBoatMaintenanceLogRepository.cs
└── BoatMaintenanceLogRepository.cs

Admin.Infrastructure/Services/
├── IBoatMaintenanceLogService.cs
└── BoatMaintenanceLogService.cs

Admin.Api/Controllers/
└── BoatMaintenanceLogController.cs
```

### 3. UI Layer
```
BargeOps.UI/ViewModels/
└── BoatStatusEditViewModel.cs

BargeOps.UI/Services/
├── IBoatMaintenanceLogService.cs
└── BoatMaintenanceLogService.cs

BargeOps.UI/Controllers/
└── BoatStatusController.cs

BargeOps.UI/Views/BoatStatus/
└── Edit.cshtml

BargeOps.UI/wwwroot/js/
└── boatStatus.js
```

## ⭐ MONO SHARED Architecture

### Critical Notes

1. **DTOs are the SINGLE SOURCE OF TRUTH**
   - Located in `BargeOps.Shared\Dto\`
   - Used by BOTH API and UI
   - NO separate domain models in API!

2. **Repositories Return DTOs Directly**
   - No AutoMapper needed!
   - No mapping layer!

3. **Use DIRECT SQL Queries**
   - NOT stored procedures
   - Embedded SQL in repository methods

4. **ViewModels Contain DTOs**
   - ViewModels contain DTOs from Shared
   - No mapping needed in UI!

## 🔧 Key Technical Patterns

### Split DateTime Pattern
```csharp
// ViewModel has single DateTime property
public DateTime StartDateTime { get; set; }

// View splits into date + time inputs
<input type="date" id="dtStartDate" />
<input type="time" id="dtStartTime" />

// JavaScript combines on submit
var combined = combineDateTime('dtStartDate', 'dtStartTime');
```

### Conditional Field Display
```javascript
// Enable/disable fields based on MaintenanceType
if (selectedType === 'Boat Status') {
    $('#statusSection').show();
    $('#cboStatus').prop('disabled', false);
} else if (selectedType === 'Change Division/Facility') {
    $('#divisionSection, #facilitySection').show();
    $('#cboDivision, #cboPortFacility').prop('disabled', false);
}
```

### Cascading Dropdown
```javascript
// Division change triggers PortFacility reload
$('#cboDivision').on('change', function() {
    $.get('/BoatStatus/GetPortFacilitiesByDivision',
        { division: division, locationId: locationId },
        function(data) { /* populate dropdown */ }
    );
});
```

### Field Clearing Logic
```javascript
// Clear unused fields before save
function clearUnusedFields() {
    var selectedType = $('input[name="MaintenanceType"]:checked').val();
    if (selectedType !== 'Boat Status') {
        $('#cboStatus').val('').trigger('change.select2');
    }
    // ... clear other fields
}
```

## 🧪 Testing Checklist

### Unit Tests
- [ ] Repository CRUD operations
- [ ] Service business logic
- [ ] Controller endpoints
- [ ] All conditional validation scenarios

### Integration Tests
- [ ] API endpoints with authentication
- [ ] Database operations
- [ ] Cascading dropdown logic

### UI Tests
- [ ] DateTime split/combine logic
- [ ] Conditional field display
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
- [ ] MaintenanceType cannot change on edit

## 📚 Reference Examples

### Shared DTOs
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\BargeOps.Shared\Dto\FacilityDto.cs`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\BargeOps.Shared\Dto\BoatLocationDto.cs`

### API Layer
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Api\Controllers\FacilityController.cs`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Repositories\FacilityRepository.cs`

### UI Layer
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Controllers\BoatLocationSearchController.cs`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\wwwroot\js\boatLocationSearch.js`

## 🚨 Critical Implementation Notes

1. ⭐ **DTOs are the ONLY data models** - no separate domain models
2. ⭐ **Use DIRECT SQL queries** - not stored procedures
3. ⭐ **DateTime MUST be split** into date + time (24-hour format)
4. ⭐ **MaintenanceType is readonly** when editing existing records
5. ⭐ **Clear unused fields** before save based on MaintenanceType
6. ⭐ **Conditional validation** - fields required based on MaintenanceType
7. ⭐ **Cascading dropdowns** - PortFacility depends on Division AND IsFleetBoat
8. ⭐ **DataTables server-side** processing for grid
9. ⭐ **Select2 for all dropdowns**
10. ⭐ **Bootstrap 5** for all styling
11. ⭐ **NO ViewBag/ViewData** - use ViewModels (MVVM pattern)
12. ⭐ **Use IdentityConstants.ApplicationScheme** for authentication

## 📊 Maintenance Type Behavior Matrix

| Maintenance Type | Enabled Fields | Required Fields | Cleared Fields |
|---|---|---|---|
| Boat Status | Status, StartDateTime, Note | Status, StartDateTime | Division, PortFacilityID, BoatRoleID |
| Change Division/Facility | Division, PortFacilityID, StartDateTime, Note | Division, StartDateTime | Status, BoatRoleID |
| Change Boat Role | BoatRoleID, StartDateTime, Note | BoatRoleID, StartDateTime | Status, Division, PortFacilityID |

## 🔐 Security

### API Layer
```csharp
[ApiKey]
public class BoatMaintenanceLogController : ControllerBase
```

### UI Layer
```csharp
[Authorize(AuthenticationSchemes = IdentityConstants.ApplicationScheme)]
public class BoatStatusController : Controller
```

## 📦 Dependencies

### API Layer
- Dapper
- Microsoft.AspNetCore.Mvc
- IDbConnectionFactory (custom)

### UI Layer
- Bootstrap 5
- jQuery
- DataTables (server-side)
- Select2
- Font Awesome

## 📞 Support

For questions or issues with these templates:
1. Review the **conversion-plan.md** for detailed guidance
2. Check reference examples in the mono repo
3. Consult the legacy source code analysis files

## 📜 License

These templates are generated for internal use in the BargeOps modernization project.

---

**Template Generator**: Claude Code (Conversion Template Generator Agent)
**Version**: 1.0
**Date**: 2025-12-11
