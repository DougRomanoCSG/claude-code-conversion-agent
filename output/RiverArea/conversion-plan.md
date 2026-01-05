# RiverArea Conversion Plan

**Entity**: RiverArea
**Generated**: 2025-12-11
**Architecture**: ASP.NET Core 8 MVC with Mono Shared Structure

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Architecture Summary](#architecture-summary)
3. [Implementation Order](#implementation-order)
4. [Generated Templates](#generated-templates)
5. [Entity Analysis](#entity-analysis)
6. [Step-by-Step Implementation Guide](#step-by-step-implementation-guide)
7. [Validation Rules](#validation-rules)
8. [Special Considerations](#special-considerations)

---

## Overview

This conversion plan transforms the legacy VB.NET WinForms RiverArea entity into a modern ASP.NET Core 8 MVC application using the **Mono Shared** architecture pattern.

### Key Characteristics

- **Primary Entity**: RiverArea (river area management with geographic segments)
- **Child Entity**: RiverAreaSegment (mile ranges on specific rivers)
- **Form Pattern**: Search + Master/Detail Edit with Child Collection
- **Delete Type**: **Hard Delete** (legacy pattern, despite IsActive property)
- **Complex Validation**: Mutually exclusive flags, conditional requirements, overlap detection for pricing zones

### Business Purpose

RiverArea manages geographic areas on rivers used for:
- **Pricing Zones**: Areas with specific barge event rates
- **Portal Areas**: Areas visible in customer portals
- **High Water Areas**: Areas with high water customer assignments
- **Fuel Tax Areas**: Areas for fuel tax calculations
- **Liquid Rate Areas**: Areas for liquid commodity rates

---

## Architecture Summary

### 🏗️ Mono Shared Structure

```
BargeOps.Admin.Mono/
├── src/
│   ├── BargeOps.Shared/           ⭐ SINGLE SOURCE OF TRUTH
│   │   └── Dto/
│   │       ├── RiverAreaDto.cs              # Complete entity DTO
│   │       ├── RiverAreaListDto.cs          # Search results DTO
│   │       ├── RiverAreaSearchRequest.cs    # Search criteria
│   │       └── RiverAreaSegmentDto.cs       # Child segment DTO
│   │
│   ├── BargeOps.API/              # API Project
│   │   ├── src/Admin.Api/Controllers/
│   │   │   └── RiverAreaController.cs
│   │   ├── src/Admin.Infrastructure/Repositories/
│   │   │   ├── IRiverAreaRepository.cs
│   │   │   └── RiverAreaRepository.cs
│   │   └── src/Admin.Infrastructure/Services/
│   │       ├── IRiverAreaService.cs
│   │       └── RiverAreaService.cs
│   │
│   └── BargeOps.UI/               # UI Project
│       ├── Controllers/
│       │   └── RiverAreaController.cs
│       ├── Services/
│       │   ├── IRiverAreaService.cs
│       │   └── RiverAreaService.cs
│       ├── ViewModels/
│       │   ├── RiverAreaSearchViewModel.cs
│       │   └── RiverAreaEditViewModel.cs
│       ├── Views/RiverArea/
│       │   ├── Index.cshtml
│       │   └── Edit.cshtml
│       └── wwwroot/js/
│           ├── riverAreaSearch.js
│           └── riverAreaEdit.js
```

### 🔑 Critical Architecture Notes

1. **DTOs are in BargeOps.Shared** - NO duplication in API or UI!
2. **DTOs are the ONLY data models** - no separate Models/ folder
3. **Repositories return DTOs directly** - no mapping layer needed
4. **Both API and UI use the same DTOs** - single source of truth

---

## Implementation Order

### Phase 1: Shared DTOs (Create FIRST!)
1. ✅ `RiverAreaDto.cs` - Complete entity with all fields
2. ✅ `RiverAreaListDto.cs` - Search grid results
3. ✅ `RiverAreaSearchRequest.cs` - Search criteria
4. ✅ `RiverAreaSegmentDto.cs` - Child segment entity

### Phase 2: API Infrastructure
5. ✅ `IRiverAreaRepository.cs` - Repository interface
6. ✅ `RiverAreaRepository.cs` - Dapper implementation (returns DTOs directly!)
7. ✅ `IRiverAreaService.cs` - Service interface
8. ✅ `RiverAreaService.cs` - Business logic layer
9. ✅ `RiverAreaController.cs` - RESTful API endpoints

### Phase 3: UI Layer
10. ✅ `RiverAreaSearchViewModel.cs` - Search screen ViewModel
11. ✅ `RiverAreaEditViewModel.cs` - Edit screen ViewModel
12. ✅ `IRiverAreaService.cs` (UI) - API client interface
13. ✅ `RiverAreaService.cs` (UI) - HTTP client implementation
14. ✅ `RiverAreaController.cs` (UI) - MVC controller
15. ✅ `Index.cshtml` - Search view
16. ✅ `Edit.cshtml` - Edit view
17. ✅ `riverAreaSearch.js` - DataTables initialization
18. ✅ `riverAreaEdit.js` - Form validation and child grid management

---

## Generated Templates

All templates are located in: `C:/source/agents/ClaudeOnshoreConversionAgent/output/RiverArea/templates/`

### Shared Project Templates
```
templates/shared/Dto/
├── RiverAreaDto.cs                 # Main entity DTO
├── RiverAreaListDto.cs            # Search results DTO
├── RiverAreaSearchRequest.cs      # Search criteria DTO
└── RiverAreaSegmentDto.cs         # Child segment DTO
```

### API Project Templates
```
templates/api/
├── Controllers/
│   └── RiverAreaController.cs     # REST API endpoints
├── Repositories/
│   ├── IRiverAreaRepository.cs    # Repository interface
│   └── RiverAreaRepository.cs     # Dapper implementation
└── Services/
    ├── IRiverAreaService.cs       # Service interface
    └── RiverAreaService.cs        # Business logic
```

### UI Project Templates
```
templates/ui/
├── Controllers/
│   └── RiverAreaController.cs     # MVC controller
├── Services/
│   ├── IRiverAreaService.cs       # API client interface
│   └── RiverAreaService.cs        # HTTP client
├── ViewModels/
│   ├── RiverAreaSearchViewModel.cs  # Search screen
│   └── RiverAreaEditViewModel.cs    # Edit screen
├── Views/RiverArea/
│   ├── Index.cshtml               # Search view
│   └── Edit.cshtml                # Edit view
└── wwwroot/js/
    ├── riverAreaSearch.js         # Search page JS
    └── riverAreaEdit.js           # Edit page JS
```

---

## Entity Analysis

### RiverArea Properties

| Property | Type | Required | Max Length | Description |
|----------|------|----------|------------|-------------|
| RiverAreaID | int | Auto | - | Primary key |
| Name | string | Yes | 50 | River area name |
| IsActive | bool | Yes | - | Active status (default: true) |
| IsPriceZone | bool | Yes | - | Pricing zone flag |
| IsPortalArea | bool | Yes | - | Portal area flag |
| IsHighWaterArea | bool | Yes | - | High water area flag |
| CustomerID | int? | Conditional | - | Required if IsHighWaterArea = true |
| IsFuelTaxArea | bool | Yes | - | Fuel tax area flag |
| IsLiquidRateArea | bool | Yes | - | Liquid rate area flag |
| Segments | List&lt;RiverAreaSegmentDto&gt; | No | - | Child collection |

### RiverAreaSegment Properties

| Property | Type | Required | Max Length | Description |
|----------|------|----------|------------|-------------|
| RiverAreaSegmentID | int | Auto | - | Primary key |
| RiverAreaID | int | Yes | - | Foreign key to RiverArea |
| River | string | Yes | 3 | 3-character river code |
| StartMile | decimal? | Yes | - | Start mile marker |
| EndMile | decimal? | Yes | - | End mile marker |

### Search Criteria

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| Name | string | - | Partial name match |
| ActiveOnly | bool | true | Show active only |
| PricingZonesOnly | bool | false | Show pricing zones only |
| PortalAreasOnly | bool | false | Show portal areas only |
| CustomerID | int? | - | Filter by high water customer |
| HighWaterAreasOnly | bool | false | Show high water areas only |

---

## Step-by-Step Implementation Guide

### Step 1: Create Shared DTOs

**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\Dto\`

1. Copy templates from `templates/shared/Dto/` to Shared project
2. Verify namespace: `BargeOps.Shared.Dto`
3. Add `[Sortable]` and `[Filterable]` attributes to RiverAreaDto properties as needed
4. Build Shared project to verify compilation

**Reference**: `FacilityDto.cs` in BargeOps.Shared

### Step 2: Create API Repository

**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Repositories\`

1. Copy `IRiverAreaRepository.cs` and `RiverAreaRepository.cs` from templates
2. **IMPORTANT**: Repository methods return DTOs directly (no separate domain models!)
3. Uses **Dapper** for SQL execution (NOT Entity Framework)
4. Uses **DIRECT SQL queries** embedded as resources (NOT stored procedures)
5. Register in DI container

**Key Pattern**:
```csharp
public async Task<DataTableResponse<RiverAreaListDto>> SearchAsync(RiverAreaSearchRequest request)
{
    // Returns DTOs directly from Dapper query
}
```

**Reference**: `FacilityRepository.cs` in Admin.Infrastructure

### Step 3: Create API Service

**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Services\`

1. Copy `IRiverAreaService.cs` and `RiverAreaService.cs` from templates
2. Service layer uses DTOs from repository (no mapping!)
3. Implements business logic validation
4. Register in DI container

### Step 4: Create API Controller

**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Api\Controllers\`

1. Copy `RiverAreaController.cs` from templates
2. Inherits from `ControllerBase`
3. Uses `[Authorize]` attributes
4. RESTful endpoints return DTOs

**Endpoints**:
- `GET /api/riverarea/{id}` - Get by ID
- `POST /api/riverarea/search` - Search (DataTables)
- `POST /api/riverarea` - Create
- `PUT /api/riverarea/{id}` - Update
- `DELETE /api/riverarea/{id}` - Delete (hard delete!)
- `GET /api/riverarea/{id}/segments` - Get segments
- `POST /api/riverarea/{id}/segments` - Create segment
- `PUT /api/riverarea/segments/{id}` - Update segment
- `DELETE /api/riverarea/segments/{id}` - Delete segment

**Reference**: `FacilityController.cs`, `BoatLocationController.cs` in Admin.Api

### Step 5: Create UI ViewModels

**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\ViewModels\`

1. Copy ViewModels from templates
2. ViewModels **contain** DTOs from BargeOps.Shared
3. Add `SelectList` properties for dropdowns
4. Add `[Display]` attributes for labels
5. Follow **MVVM pattern** - NO ViewBag/ViewData!

**Pattern**:
```csharp
public class RiverAreaEditViewModel
{
    public RiverAreaDto RiverArea { get; set; }  // DTO from Shared!
    public IEnumerable<SelectListItem> Customers { get; set; }  // Dropdown data
    public IEnumerable<SelectListItem> Rivers { get; set; }
}
```

**Reference**: `BoatLocationSearchViewModel.cs`, `BoatLocationEditViewModel.cs`

### Step 6: Create UI API Client Service

**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Services\`

1. Copy UI service templates
2. HTTP client to call API endpoints
3. Returns DTOs from API
4. Register in DI container

### Step 7: Create UI Controller

**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Controllers\`

1. Copy `RiverAreaController.cs` from templates
2. Inherits from `AppController`
3. Uses `[Authorize]` attributes
4. Populates ViewModels with DTOs and lookup data

**Actions**:
- `Index()` - Search page
- `RiverAreaTable(request)` - DataTables AJAX endpoint
- `Create()` - New river area form
- `Edit(id)` - Edit river area form
- `Save(viewModel)` - Save river area
- `Delete(id)` - Delete river area

**Reference**: `BoatLocationSearchController.cs`

### Step 8: Create Razor Views

**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Views\RiverArea\`

1. Copy `Index.cshtml` and `Edit.cshtml` from templates
2. Use `asp-for` tag helpers for all form fields
3. Use Bootstrap 5 classes (NO inline styles!)
4. Use DataTables for grids
5. Use Select2 for dropdowns

**Key Patterns**:
- Search form in Bootstrap card
- DataTables with server-side processing
- Inline editing for child segments
- Mutually exclusive checkboxes with JavaScript
- Conditional field visibility (CustomerId based on IsHighWaterArea)

**Reference**: `BoatLocationSearch/Index.cshtml`, `BoatLocationSearch/Edit.cshtml`

### Step 9: Create JavaScript Files

**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\wwwroot\js\`

1. Copy JavaScript templates
2. Initialize DataTables with server-side processing
3. Initialize Select2 dropdowns
4. Implement validation logic
5. Handle child segment grid

**riverAreaSearch.js**:
- DataTables initialization
- Search button handler
- Reset button handler
- Select2 for Customer dropdown

**riverAreaEdit.js**:
- Mutually exclusive checkbox logic
- Conditional CustomerId visibility/validation
- Segments grid management (add/delete rows)
- Form submission validation
- Overlap detection for pricing zones

**Reference**: `boatLocationSearch.js`

---

## Validation Rules

### Client-Side Validation (JavaScript)

1. **Name**: Required, max length 50
2. **Mutually Exclusive Flags**: Only one of IsPriceZone/IsPortalArea/IsHighWaterArea can be checked
3. **Conditional Required**: CustomerID required when IsHighWaterArea = true
4. **Segment Fields**: River (required, 3 chars), StartMile (required, decimal), EndMile (required, decimal)
5. **Mile Range**: StartMile < EndMile
6. **Overlap Detection**: Pricing zones cannot have overlapping mile ranges on same river

### Server-Side Validation (C#)

1. **Data Annotations** on DTOs:
   - `[Required]` for Name, boolean flags
   - `[StringLength(50)]` for Name
   - `[StringLength(3)]` for River

2. **Business Rules** in Service:
   - Mutually exclusive area types
   - CustomerID validation with IsHighWaterArea
   - River segment overlap detection (calls business logic)
   - Mile range validation against river min/max

### Validation Error Display

- **Field Errors**: `<span asp-validation-for="Name" class="text-danger"></span>`
- **Summary**: `<div asp-validation-summary="ModelOnly" class="alert alert-danger"></div>`
- **Business Rule Errors**: Alert banners with specific messages

---

## Special Considerations

### 1. **Mono Shared Architecture**

⚠️ **CRITICAL**: DTOs must be in BargeOps.Shared ONLY!
- DO NOT create duplicate DTOs in API or UI projects
- Both API and UI reference BargeOps.Shared
- DTOs are the single source of truth

### 2. **Direct SQL Queries (NOT Stored Procedures)**

The repository uses **inline SQL queries** embedded as resources:
- `RiverArea_Search.sql`
- `RiverArea_GetById.sql`
- `RiverArea_Insert.sql`
- `RiverArea_Update.sql`
- `RiverArea_Delete.sql`
- Plus RiverAreaSegment CRUD queries

### 3. **Hard Delete Pattern**

Despite having an `IsActive` property, RiverArea uses **hard delete** (legacy pattern):
```csharp
public async Task DeleteAsync(int riverAreaId)
{
    // Actual DELETE from database, not soft delete
}
```

### 4. **Mutually Exclusive Flags**

Only ONE of these can be true:
- IsPriceZone
- IsPortalArea
- IsHighWaterArea

**JavaScript Implementation**:
```javascript
$('#chkIsPriceZone, #chkIsPortalArea, #chkIsHighWaterArea').on('change', function() {
    if (this.checked) {
        $('#chkIsPriceZone, #chkIsPortalArea, #chkIsHighWaterArea').not(this).prop('checked', false);
    }
});
```

### 5. **Conditional Field: CustomerID**

CustomerID field visibility and requirement depends on IsHighWaterArea:

**JavaScript**:
```javascript
$('#chkIsHighWaterArea').on('change', function() {
    $('#CustomerID').closest('.form-group').toggle(this.checked);
    if (!this.checked) {
        $('#CustomerID').val('');
    }
});
```

### 6. **Pricing Zone Overlap Validation**

When `IsPriceZone = true`, validate that river segments don't overlap with other pricing zones on the same river:

**Logic**: Check if `(myEndMile <= otherStartMile) OR (myStartMile >= otherEndMile)` - if false, ranges overlap.

**Server-Side**: Call business logic to query other pricing zones and check for overlaps.

### 7. **DateTime Handling**

If future enhancements add DateTime fields:
- ViewModel: Single `DateTime?` property
- View: Split into date + time inputs
- JavaScript: Combine on submit
- Pattern: See BoatLocationEditViewModel

### 8. **ID Field Naming**

⚠️ Use **uppercase ID** suffix (not Id):
- RiverAreaID ✅
- RiverAreaId ❌
- CustomerID ✅
- CustomerId ❌

### 9. **DataTables Server-Side Processing**

Search grid uses server-side DataTables:
```javascript
$('#riverAreaTable').DataTable({
    serverSide: true,
    processing: true,
    ajax: {
        url: '/RiverArea/RiverAreaTable',
        type: 'POST',
        data: function(d) {
            d.name = $('#Name').val();
            d.activeOnly = $('#ActiveOnly').is(':checked');
            // ... other search criteria
        }
    }
});
```

### 10. **Child Collection Management**

Segments grid (child collection):
- Use client-side DataTables (no server processing for child grid)
- Add/Delete rows via JavaScript
- Serialize to array on form submission
- Validate each segment before adding
- Server-side: cascade delete segments when parent deleted

---

## Testing Checklist

### Functionality Testing

- [ ] Create new river area with segments
- [ ] Edit existing river area
- [ ] Delete river area (verify hard delete)
- [ ] Search with various criteria combinations
- [ ] Add/edit/delete segments
- [ ] Toggle between area types (Price Zone, Portal, High Water)
- [ ] Customer dropdown shows/hides based on High Water Area checkbox

### Validation Testing

- [ ] Name required validation
- [ ] Name max length (50 chars)
- [ ] Mutually exclusive flags (can't check multiple)
- [ ] CustomerID required when IsHighWaterArea checked
- [ ] CustomerID cleared when IsHighWaterArea unchecked
- [ ] River segment fields required
- [ ] StartMile < EndMile validation
- [ ] Pricing zone overlap detection

### UI/UX Testing

- [ ] DataTables pagination works
- [ ] DataTables sorting works
- [ ] DataTables search works
- [ ] Select2 dropdowns work (Customer, River)
- [ ] Reset button clears form
- [ ] Success/error messages display
- [ ] Responsive layout on mobile
- [ ] Accessibility (aria-labels, keyboard navigation)

### Integration Testing

- [ ] API endpoints return correct DTOs
- [ ] UI service calls API correctly
- [ ] DTOs serialize/deserialize properly
- [ ] Database queries execute correctly
- [ ] Authorization works
- [ ] Concurrent user testing

---

## Reference Files

### Patterns to Follow

**Shared DTOs**:
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\Dto\FacilityDto.cs`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\Dto\BoatLocationDto.cs`

**API Patterns**:
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Api\Controllers\FacilityController.cs`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Api\Controllers\BoatLocationController.cs`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Repositories\FacilityRepository.cs`

**UI Patterns**:
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Controllers\BoatLocationSearchController.cs`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\ViewModels\BoatLocationSearchViewModel.cs`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Views\BoatLocationSearch\Index.cshtml`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\wwwroot\js\boatLocationSearch.js`

---

## Common Mistakes to Avoid

1. ❌ Creating separate Models/ folder in Shared - DTOs ARE the models!
2. ❌ Duplicating DTOs in API or UI projects
3. ❌ Using AutoMapper when repositories return DTOs directly
4. ❌ Using stored procedures instead of direct SQL queries
5. ❌ Using ViewBag/ViewData instead of ViewModels
6. ❌ Using inline styles instead of Bootstrap classes
7. ❌ Not using asp-for tag helpers
8. ❌ Forgetting to implement mutually exclusive checkbox logic
9. ❌ Not validating pricing zone overlaps
10. ❌ Using soft delete when entity requires hard delete

---

## Summary

This conversion transforms the RiverArea WinForms application into a modern ASP.NET Core 8 MVC application following the **Mono Shared** architecture pattern. Key highlights:

✅ **Single Source of Truth**: DTOs in BargeOps.Shared used by both API and UI
✅ **No Duplication**: Repositories return DTOs directly, no mapping layer
✅ **Direct SQL**: Embedded SQL queries, not stored procedures
✅ **MVVM Pattern**: ViewModels contain DTOs and dropdown data
✅ **Modern UI**: Bootstrap 5, DataTables, Select2, jQuery Validate
✅ **Complex Validation**: Mutually exclusive flags, conditional requirements, overlap detection
✅ **Hard Delete**: Legacy pattern maintained for compatibility

All templates are ready to use - copy to target locations and customize as needed!

---

**Generated by**: ClaudeOnshoreConversionAgent
**Date**: 2025-12-11
**Version**: 1.0
