# BoatFuelPrices Conversion Plan

## Overview

This document provides a comprehensive plan for converting the legacy BoatFuelPrices form to the modern ASP.NET Core architecture with a MONO SHARED structure.

**Entity**: BoatFuelPrice
**Form Type**: Search and Edit (Combined)
**Complexity**: Low to Medium
**Estimated Effort**: 2-3 days

## Critical Architecture

### MONO SHARED Structure
⭐ **DTOs are the ONLY data models - no separate Models folder!**
- DTOs are defined in `BargeOps.Shared` project
- Used DIRECTLY by both API and UI projects
- NO AutoMapper needed (repositories return DTOs directly)
- NO separate domain models

### Key Technologies
- **API**: ASP.NET Core 6.0, Dapper, SQL Server
- **UI**: ASP.NET Core MVC, Bootstrap 5, DataTables, Select2, jQuery
- **Shared**: DTOs with attributes for filtering/sorting

## Entity Analysis

### Database Schema
```sql
-- Table: BoatFuelPrice
PRIMARY KEY: BoatFuelPriceID (int, identity)
UNIQUE INDEX: IX_BoatFuelPrice_Unique ON (EffectiveDate, FuelVendor)

Columns:
- BoatFuelPriceID (int, PK)
- EffectiveDate (datetime, required)
- Price (money, required) -- Formatted with 4 decimal places
- FuelVendorBusinessUnitID (int, nullable) -- FK to BusinessUnit
- InvoiceNumber (varchar(50), nullable)
- CreateDateTime (datetime)
- CreateUser (varchar(50))
- ModifyDateTime (datetime)
- ModifyUser (varchar(50))
```

### Business Rules
1. **EffectiveDate** is required
2. **Price** is required and formatted as currency with 4 decimal places
3. **InvoiceNumber** must be blank when FuelVendorBusinessUnitID is blank
4. **InvoiceNumber** field is only enabled when a vendor is selected
5. Unique constraint on (EffectiveDate, FuelVendor) combination
6. Search defaults to today's date for EffectiveDate

### Related Entities
- **VendorBusinessUnit** (Many-to-One via FuelVendorBusinessUnitID)

## Implementation Order

### Phase 1: Shared Project - DTOs (CREATE FIRST!)
1. BoatFuelPriceDto.cs
2. BoatFuelPriceSearchRequest.cs

### Phase 2: API Layer
3. IBoatFuelPriceRepository.cs
4. BoatFuelPriceRepository.cs (Dapper with direct SQL queries)
5. IBoatFuelPriceService.cs
6. BoatFuelPriceService.cs
7. BoatFuelPriceController.cs

### Phase 3: UI Layer
8. IBoatFuelPriceService.cs (API client)
9. BoatFuelPriceService.cs (API client implementation)
10. BoatFuelPriceSearchViewModel.cs
11. BoatFuelPriceEditViewModel.cs
12. BoatFuelPriceSearchController.cs
13. Index.cshtml (search view)
14. Edit.cshtml (modal edit partial)
15. boatFuelPriceSearch.js (DataTables)
16. boatFuelPriceEdit.js (form logic)

## File Locations

### Shared Project
```
C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\BargeOps.Shared\
├── Dto/
│   ├── BoatFuelPriceDto.cs
│   └── BoatFuelPriceSearchRequest.cs
```

### API Project
```
C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\
├── src/Admin.Api/Controllers/
│   └── BoatFuelPriceController.cs
├── src/Admin.Domain/Services/
│   └── IBoatFuelPriceService.cs
├── src/Admin.Infrastructure/Repositories/
│   ├── IBoatFuelPriceRepository.cs
│   └── BoatFuelPriceRepository.cs
└── src/Admin.Infrastructure/Services/
    └── BoatFuelPriceService.cs
```

### UI Project
```
C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\
├── Controllers/
│   └── BoatFuelPriceSearchController.cs
├── Services/
│   ├── IBoatFuelPriceService.cs
│   └── BoatFuelPriceService.cs
├── ViewModels/
│   ├── BoatFuelPriceSearchViewModel.cs
│   └── BoatFuelPriceEditViewModel.cs
├── Views/BoatFuelPriceSearch/
│   ├── Index.cshtml
│   └── Edit.cshtml
└── wwwroot/js/
    ├── boatFuelPriceSearch.js
    └── boatFuelPriceEdit.js
```

## Data Flow

### Search Flow
1. User enters search criteria (EffectiveDate, FuelVendor)
2. User clicks "Find" button
3. JavaScript triggers DataTables AJAX reload
4. API `GET /api/boatfuelprice/search` called
5. Repository executes parameterized SQL query
6. Returns `PagedResult<BoatFuelPriceDto>`
7. DataTables renders results

### Add Flow
1. User clicks "Add" button in grid
2. Modal opens with empty form
3. FuelVendor dropdown populated
4. User enters data
5. JavaScript validates and posts to `POST /api/boatfuelprice`
6. Service validates business rules
7. Repository executes INSERT
8. Returns created DTO
9. Modal closes, grid refreshes

### Edit Flow
1. User clicks "Edit" button on grid row
2. API `GET /api/boatfuelprice/{id}` called
3. Repository returns BoatFuelPriceDto
4. Modal populated with data
5. User modifies data
6. JavaScript posts to `PUT /api/boatfuelprice/{id}`
7. Repository executes UPDATE
8. Modal closes, grid refreshes

### Delete Flow
1. User clicks "Delete" button on grid row
2. Confirmation dialog shown
3. If confirmed, API `DELETE /api/boatfuelprice/{id}` called
4. Repository executes DELETE
5. Grid refreshes

## Security Implementation

### Authorization Policies
```csharp
// Startup.cs / Program.cs
services.AddAuthorization(options =>
{
    options.AddPolicy("BoatFuelPrice.View", policy =>
        policy.RequireClaim("FunctionalArea", "frmBoatFuelPrices")
              .RequireMinimumAccessLevel(AccessLevel.Readonly));

    options.AddPolicy("BoatFuelPrice.Create", policy =>
        policy.RequireClaim("FunctionalArea", "frmBoatFuelPrices")
              .RequireMinimumAccessLevel(AccessLevel.CreateAndUpdate));

    options.AddPolicy("BoatFuelPrice.Edit", policy =>
        policy.RequireClaim("FunctionalArea", "frmBoatFuelPrices")
              .RequireMinimumAccessLevel(AccessLevel.CreateAndUpdate));

    options.AddPolicy("BoatFuelPrice.Delete", policy =>
        policy.RequireClaim("FunctionalArea", "frmBoatFuelPrices")
              .RequireMinimumAccessLevel(AccessLevel.Full));
});
```

### Controller Attributes
```csharp
[Authorize(Policy = "BoatFuelPrice.View")]
public class BoatFuelPriceController : ControllerBase
{
    [HttpGet("{id}")]
    [Authorize(Policy = "BoatFuelPrice.View")]
    public async Task<ActionResult<BoatFuelPriceDto>> GetById(int id) { }

    [HttpPost]
    [Authorize(Policy = "BoatFuelPrice.Create")]
    public async Task<ActionResult<BoatFuelPriceDto>> Create([FromBody] BoatFuelPriceDto dto) { }

    [HttpPut("{id}")]
    [Authorize(Policy = "BoatFuelPrice.Edit")]
    public async Task<ActionResult<BoatFuelPriceDto>> Update(int id, [FromBody] BoatFuelPriceDto dto) { }

    [HttpDelete("{id}")]
    [Authorize(Policy = "BoatFuelPrice.Delete")]
    public async Task<IActionResult> Delete(int id) { }
}
```

## Validation Strategy

### Client-Side Validation
- jQuery Validation (Unobtrusive)
- Data annotations from ViewModels
- Currency formatting for Price field
- Date validation for EffectiveDate
- Conditional validation for InvoiceNumber

### Server-Side Validation
- Data Annotations on DTOs
- FluentValidation for complex rules
- Business rule: InvoiceNumber cleared if FuelVendorBusinessUnitID is empty
- Unique constraint validation (EffectiveDate + FuelVendor)

### Validation Rules
```csharp
// BoatFuelPriceDto validations
[Required(ErrorMessage = "Effective date is required.")]
public DateTime EffectiveDate { get; set; }

[Required(ErrorMessage = "Fuel price is required.")]
[Range(0.01, 999999.9999, ErrorMessage = "Fuel price must be greater than 0.")]
public decimal Price { get; set; }

[MaxLength(50, ErrorMessage = "Vendor inv# must be less than or equal to 50 characters.")]
public string InvoiceNumber { get; set; }
```

## UI Control Mapping

| Legacy Control | Modern Control | Implementation |
|---------------|----------------|----------------|
| pnlCriteria | Bootstrap Card | `<div class="card">` |
| dtEffectiveDateSearch | HTML5 date input | `<input type="date">` |
| cboFuelVendorSearch | Select2 dropdown | `<select class="select2">` |
| grdSearch | DataTables | `<table id="boatFuelPriceTable">` |
| grpDetail | Bootstrap Modal | `<div class="modal">` |
| dtEffectiveDate | HTML5 date input (required) | `<input type="date" required>` |
| txtPrice | Currency input | `<input type="text" class="currency-input">` |
| cboFuelVendorBusinessUnitID | Select2 dropdown | `<select class="select2">` |
| txtInvoiceNumber | Text input (conditionally enabled) | `<input type="text">` |
| btnFind | Bootstrap button | `<button class="btn btn-primary">` |
| btnSubmit | Bootstrap button | `<button type="submit" class="btn btn-primary">` |
| btnCancel | Bootstrap button | `<button class="btn btn-secondary">` |

## DataTables Configuration

### Column Definitions
```javascript
columns: [
    {
        data: null,
        orderable: false,
        searchable: false,
        render: function(data, type, row) {
            return '<div class="btn-group btn-group-sm">' +
                   '<a href="#" class="btn btn-sm btn-primary btn-edit" data-id="' + row.boatFuelPriceID + '"><i class="fas fa-edit"></i></a>' +
                   '<a href="#" class="btn btn-sm btn-danger btn-delete" data-id="' + row.boatFuelPriceID + '"><i class="fas fa-trash"></i></a>' +
                   '</div>';
        }
    },
    { data: 'effectiveDate', name: 'effectiveDate', render: function(data) { return moment(data).format('MM/DD/YYYY'); } },
    { data: 'price', name: 'price', className: 'text-end', render: function(data) { return '$' + parseFloat(data).toFixed(4); } },
    { data: 'fuelVendor', name: 'fuelVendor' },
    { data: 'invoiceNumber', name: 'invoiceNumber' }
],
order: [[1, 'desc'], [3, 'asc']], // EffectiveDate DESC, FuelVendor ASC
```

## Conditional Logic

### Invoice Number Field
```javascript
// When FuelVendor changes
$('#FuelVendorBusinessUnitID').on('change', function() {
    var hasVendor = $(this).val() !== '';
    $('#InvoiceNumber').prop('disabled', !hasVendor);
    if (!hasVendor) {
        $('#InvoiceNumber').val('');
    }
});
```

## Testing Strategy

### Unit Tests
- Repository CRUD operations
- Service business logic validation
- DTO validation rules

### Integration Tests
- API endpoint testing
- Database operations
- Authorization policies

### UI Tests
- Search functionality
- Add/Edit/Delete operations
- Conditional field validation
- DataTables sorting and filtering

## Migration Checklist

- [ ] Create BoatFuelPriceDto in BargeOps.Shared
- [ ] Create BoatFuelPriceSearchRequest in BargeOps.Shared
- [ ] Create IBoatFuelPriceRepository
- [ ] Create BoatFuelPriceRepository with Dapper
- [ ] Create IBoatFuelPriceService
- [ ] Create BoatFuelPriceService
- [ ] Create BoatFuelPriceController (API)
- [ ] Create IBoatFuelPriceService (UI API client)
- [ ] Create BoatFuelPriceService (UI API client)
- [ ] Create BoatFuelPriceSearchViewModel
- [ ] Create BoatFuelPriceEditViewModel
- [ ] Create BoatFuelPriceSearchController (UI)
- [ ] Create Index.cshtml
- [ ] Create Edit.cshtml (modal)
- [ ] Create boatFuelPriceSearch.js
- [ ] Create boatFuelPriceEdit.js
- [ ] Configure authorization policies
- [ ] Test search functionality
- [ ] Test CRUD operations
- [ ] Test conditional validation (InvoiceNumber)
- [ ] Test security/authorization
- [ ] Test grid state persistence
- [ ] Deploy to staging
- [ ] User acceptance testing
- [ ] Deploy to production

## References

### Primary Examples
- **Shared DTOs**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\BargeOps.Shared\Dto\FacilityDto.cs`
- **API Repository**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Repositories\FacilityRepository.cs`
- **API Controller**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Api\Controllers\FacilityController.cs`
- **UI Controller**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Controllers\BoatLocationSearchController.cs`
- **UI JavaScript**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\wwwroot\js\boatLocationSearch.js`

### Crewing Examples (for additional patterns)
- `C:\source\BargeOps.Crewing.API\src\Crewing.Domain\Dto\CrewingDto.cs`
- `C:\source\BargeOps.Crewing.UI\Models\CrewingSearchViewModel.cs`

## Notes

- Price field uses 4 decimal places for precision (stored as money in SQL)
- InvoiceNumber has conditional logic based on FuelVendorBusinessUnitID
- Grid state persistence uses DataTables `stateSave` with localStorage
- Modal-based edit provides better UX than inline editing
- Authentication scheme is `IdentityConstants.ApplicationScheme`
- No ViewBag/ViewData - all data on ViewModels (MVVM pattern)
