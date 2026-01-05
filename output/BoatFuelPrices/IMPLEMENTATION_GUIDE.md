# BoatFuelPrices Implementation Guide

## Overview

This guide provides step-by-step instructions for implementing the BoatFuelPrices conversion from legacy VB.NET to modern ASP.NET Core.

## Prerequisites

- .NET 6.0 SDK installed
- Access to BargeOps.Admin.Mono repository
- SQL Server access for database operations
- Understanding of Dapper, ASP.NET Core MVC, and Bootstrap 5

## Implementation Steps

### Phase 1: Shared Project - DTOs (CRITICAL - DO THIS FIRST!)

#### Step 1.1: Create BoatFuelPriceDto

**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\BargeOps.Shared\Dto\BoatFuelPriceDto.cs`

1. Copy the template from `output/BoatFuelPrices/templates/shared/Dto/BoatFuelPriceDto.cs`
2. Add to the `BargeOps.Shared` project
3. Build the project to verify no errors
4. Note: This DTO is used by BOTH API and UI - no separate models needed!

**Key Points**:
- DTOs are the ONLY data models (no Models/ folder!)
- Includes `[Sortable]` and `[Filterable]` attributes for ListQuery
- Includes validation attributes (`[Required]`, `[MaxLength]`, etc.)
- Price is `decimal` with 4 decimal place precision

### Phase 2: API Layer

#### Step 2.1: Create Repository Interface

**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Repositories\IBoatFuelPriceRepository.cs`

1. Copy template from `output/BoatFuelPrices/templates/api/Repositories/IBoatFuelPriceRepository.cs`
2. Add to Admin.Infrastructure project

#### Step 2.2: Create Repository Implementation

**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Repositories\BoatFuelPriceRepository.cs`

1. Copy template from `output/BoatFuelPrices/templates/api/Repositories/BoatFuelPriceRepository.cs`
2. Review SQL queries:
   - Search query with LEFT JOIN to BusinessUnit
   - Insert/Update with OUTPUT clause for ID
   - Delete query
   - Unique constraint check
3. Test SQL queries in SQL Server Management Studio first
4. **IMPORTANT**: Uses parameterized SQL queries, NOT stored procedures!

**Key Points**:
- Returns DTOs directly (no mapping layer!)
- Uses Dapper for data access
- Implements server-side pagination
- Handles null FuelVendorBusinessUnitID correctly

#### Step 2.3: Create Service Interface and Implementation

**Location**:
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Domain\Services\IBoatFuelPriceService.cs`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Services\BoatFuelPriceService.cs`

1. Copy templates from `output/BoatFuelPrices/templates/api/Services/`
2. Review business rules in service implementation:
   - InvoiceNumber cleared when FuelVendorBusinessUnitID is null
   - Unique constraint validation
3. Add to respective projects

#### Step 2.4: Create API Controller

**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Api\Controllers\BoatFuelPriceController.cs`

1. Copy template from `output/BoatFuelPrices/templates/api/Controllers/BoatFuelPriceController.cs`
2. Review authorization policies:
   - `BoatFuelPrice.View`
   - `BoatFuelPrice.Create`
   - `BoatFuelPrice.Edit`
   - `BoatFuelPrice.Delete`
3. Add to Admin.Api project

#### Step 2.5: Register Services in Dependency Injection

**Location**: `Startup.cs` or `Program.cs` in Admin.Api

```csharp
// Add to ConfigureServices or builder.Services
services.AddScoped<IBoatFuelPriceRepository, BoatFuelPriceRepository>();
services.AddScoped<IBoatFuelPriceService, BoatFuelPriceService>();
```

#### Step 2.6: Configure Authorization Policies

**Location**: `Startup.cs` or `Program.cs` in Admin.Api

```csharp
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

### Phase 3: UI Layer

#### Step 3.1: Create ViewModels

**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\ViewModels\`

1. Copy templates from `output/BoatFuelPrices/templates/ui/ViewModels/`:
   - `BoatFuelPriceSearchViewModel.cs`
   - `BoatFuelPriceEditViewModel.cs`
2. Add to BargeOps.UI project

**Key Points**:
- ViewModels follow MVVM pattern (no ViewBag/ViewData!)
- Include `IEnumerable<SelectListItem>` for dropdowns
- Validation attributes for client-side validation

#### Step 3.2: Create UI Service (API Client)

**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Services\`

1. Create `IBoatFuelPriceService.cs` and `BoatFuelPriceService.cs`
2. Implement HTTP client calls to API endpoints
3. Use `HttpClient` with proper error handling

**Example Implementation**:
```csharp
public class BoatFuelPriceService : IBoatFuelPriceService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BoatFuelPriceService> _logger;

    public BoatFuelPriceService(HttpClient httpClient, ILogger<BoatFuelPriceService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<DataTableResponse<BoatFuelPriceDto>> SearchAsync(BoatFuelPriceSearchRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/boatfuelprice/search", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DataTableResponse<BoatFuelPriceDto>>();
    }

    // ... other methods
}
```

#### Step 3.3: Register UI Services

**Location**: `Startup.cs` or `Program.cs` in BargeOps.UI

```csharp
services.AddHttpClient<IBoatFuelPriceService, BoatFuelPriceService>(client =>
{
    client.BaseAddress = new Uri(Configuration["ApiBaseUrl"]);
});
```

#### Step 3.4: Create MVC Controller

**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Controllers\BoatFuelPriceSearchController.cs`

**Key Methods**:
- `Index()` - Returns search view
- `GetSearchResults()` - AJAX endpoint for DataTables
- `GetById(int id)` - Returns single record for edit modal
- `Create(BoatFuelPriceEditViewModel model)` - Creates new record
- `Edit(int id, BoatFuelPriceEditViewModel model)` - Updates record
- `Delete(int id)` - Deletes record

**Authorization**:
```csharp
[Authorize(Policy = "BoatFuelPrice.View")]
public class BoatFuelPriceSearchController : Controller
{
    // ... controller methods
}
```

#### Step 3.5: Create Views

**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Views\BoatFuelPriceSearch\`

**Files to Create**:
1. `Index.cshtml` - Main search screen
2. `_Search.cshtml` - Search criteria partial (optional)
3. `_SearchResults.cshtml` - DataTables grid partial (optional)
4. `Edit.cshtml` - Edit/Create modal partial

**Index.cshtml Structure**:
```cshtml
@model BoatFuelPriceSearchViewModel

<div class="container-fluid">
    <h2>Boat Fuel Prices</h2>

    <!-- Search Criteria Card -->
    <div class="card mb-3">
        <div class="card-header">
            <h5 class="card-title mb-0"><i class="fas fa-search"></i> Search Criteria</h5>
        </div>
        <div class="card-body">
            <div class="row">
                <div class="col-md-3">
                    <label asp-for="EffectiveDateSearch" class="form-label"></label>
                    <input asp-for="EffectiveDateSearch" type="date" class="form-control" id="effectiveDateSearch" />
                </div>
                <div class="col-md-3">
                    <label asp-for="FuelVendorIDSearch" class="form-label"></label>
                    <select asp-for="FuelVendorIDSearch" asp-items="Model.FuelVendors" class="form-select select2" id="fuelVendorSearch">
                        <option value="">-- All Vendors --</option>
                    </select>
                </div>
                <div class="col-md-3 d-flex align-items-end">
                    <button type="button" id="btnSearch" class="btn btn-primary me-2">
                        <i class="fas fa-search"></i> Find
                    </button>
                    <button type="button" id="btnReset" class="btn btn-secondary">
                        <i class="fas fa-undo"></i> Reset
                    </button>
                </div>
            </div>
        </div>
    </div>

    <!-- Results Grid Card -->
    <div class="card">
        <div class="card-header">
            <h5 class="card-title mb-0"><i class="fas fa-list"></i> Boat Fuel Prices</h5>
        </div>
        <div class="card-body">
            <button type="button" id="btnAdd" class="btn btn-success mb-3">
                <i class="fas fa-plus"></i> Add New
            </button>
            <div class="table-responsive">
                <table id="boatFuelPriceTable" class="table table-striped table-bordered dt-responsive nowrap" style="width:100%">
                    <thead>
                        <tr>
                            <th>Actions</th>
                            <th>Effective Date</th>
                            <th>Fuel Price</th>
                            <th>Fuel Vendor</th>
                            <th>Invoice</th>
                        </tr>
                    </thead>
                    <tbody></tbody>
                </table>
            </div>
        </div>
    </div>
</div>

<!-- Edit Modal (include Edit.cshtml partial) -->
<div id="editModalContainer"></div>

@section Scripts {
    <script src="~/js/boatFuelPriceSearch.js"></script>
}
```

**Edit.cshtml (Modal)**:
```cshtml
@model BoatFuelPriceEditViewModel

<div class="modal fade" id="editModal" tabindex="-1" aria-labelledby="editModalLabel" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content">
            <form id="editForm" asp-action="@(Model.IsNew ? "Create" : "Edit")" method="post">
                <div class="modal-header">
                    <h5 class="modal-title" id="editModalLabel">
                        @(Model.IsNew ? "Add" : "Edit") Boat Fuel Price
                    </h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <input type="hidden" asp-for="BoatFuelPriceID" />

                    <div class="mb-3">
                        <label asp-for="EffectiveDate" class="form-label fw-bold"></label>
                        <span class="text-danger">*</span>
                        <input asp-for="EffectiveDate" type="date" class="form-control" required />
                        <span asp-validation-for="EffectiveDate" class="text-danger"></span>
                    </div>

                    <div class="mb-3">
                        <label asp-for="Price" class="form-label fw-bold"></label>
                        <span class="text-danger">*</span>
                        <div class="input-group">
                            <span class="input-group-text">$</span>
                            <input asp-for="Price" type="text" class="form-control currency-input" required />
                        </div>
                        <span asp-validation-for="Price" class="text-danger"></span>
                    </div>

                    <div class="mb-3">
                        <label asp-for="FuelVendorBusinessUnitID" class="form-label"></label>
                        <select asp-for="FuelVendorBusinessUnitID" asp-items="Model.FuelVendors" class="form-select select2" id="fuelVendorEdit">
                            <option value="">-- Select Vendor --</option>
                        </select>
                        <span asp-validation-for="FuelVendorBusinessUnitID" class="text-danger"></span>
                    </div>

                    <div class="mb-3">
                        <label asp-for="InvoiceNumber" class="form-label"></label>
                        <input asp-for="InvoiceNumber" type="text" class="form-control" id="invoiceNumber" disabled />
                        <span asp-validation-for="InvoiceNumber" class="text-danger"></span>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
                        <i class="fas fa-times"></i> Cancel
                    </button>
                    <button type="submit" class="btn btn-primary">
                        <i class="fas fa-save"></i> Submit
                    </button>
                </div>
            </form>
        </div>
    </div>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
    <script src="~/js/boatFuelPriceEdit.js"></script>
}
```

#### Step 3.6: Create JavaScript Files

**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\wwwroot\js\`

**boatFuelPriceSearch.js**:
```javascript
$(document).ready(function() {
    // Initialize Select2 for fuel vendor dropdown
    $('#fuelVendorSearch').select2({
        placeholder: '-- All Vendors --',
        allowClear: true,
        width: '100%'
    });

    // Initialize DataTables
    var table = $('#boatFuelPriceTable').DataTable({
        processing: true,
        serverSide: true,
        stateSave: true,
        stateKey: 'boatFuelPriceTable_v1',
        responsive: true,
        autoWidth: false,
        order: [[1, 'desc'], [3, 'asc']], // EffectiveDate DESC, FuelVendor ASC
        pageLength: 25,
        lengthMenu: [[10, 25, 50, 100], [10, 25, 50, 100]],
        ajax: {
            url: '/BoatFuelPriceSearch/GetSearchResults',
            type: 'POST',
            data: function(d) {
                d.effectiveDate = $('#effectiveDateSearch').val();
                d.fuelVendorID = $('#fuelVendorSearch').val();
                return d;
            }
        },
        columns: [
            {
                data: null,
                orderable: false,
                searchable: false,
                className: 'text-center',
                width: '80px',
                render: function(data, type, row) {
                    return '<div class="btn-group btn-group-sm">' +
                           '<a href="#" class="btn btn-sm btn-primary btn-edit" data-id="' + row.boatFuelPriceID + '" title="Edit">' +
                           '<i class="fas fa-edit"></i></a>' +
                           '<a href="#" class="btn btn-sm btn-danger btn-delete" data-id="' + row.boatFuelPriceID + '" title="Delete">' +
                           '<i class="fas fa-trash"></i></a>' +
                           '</div>';
                }
            },
            {
                data: 'effectiveDate',
                name: 'effectiveDate',
                render: function(data) {
                    return data ? moment(data).format('MM/DD/YYYY') : '';
                }
            },
            {
                data: 'price',
                name: 'price',
                className: 'text-end',
                render: function(data) {
                    return data != null ? '$' + parseFloat(data).toFixed(4) : '';
                }
            },
            {
                data: 'fuelVendor',
                name: 'fuelVendor',
                defaultContent: ''
            },
            {
                data: 'invoiceNumber',
                name: 'invoiceNumber',
                defaultContent: ''
            }
        ]
    });

    // Search button click
    $('#btnSearch').on('click', function() {
        table.ajax.reload();
    });

    // Reset button click
    $('#btnReset').on('click', function() {
        $('#effectiveDateSearch').val(new Date().toISOString().split('T')[0]);
        $('#fuelVendorSearch').val('').trigger('change');
        table.ajax.reload();
    });

    // Add button click
    $('#btnAdd').on('click', function() {
        // Load edit modal via AJAX
        $.get('/BoatFuelPriceSearch/Create', function(html) {
            $('#editModalContainer').html(html);
            $('#editModal').modal('show');
        });
    });

    // Edit button click (delegated event)
    $('#boatFuelPriceTable').on('click', '.btn-edit', function(e) {
        e.preventDefault();
        var id = $(this).data('id');

        $.get('/BoatFuelPriceSearch/Edit/' + id, function(html) {
            $('#editModalContainer').html(html);
            $('#editModal').modal('show');
        });
    });

    // Delete button click (delegated event)
    $('#boatFuelPriceTable').on('click', '.btn-delete', function(e) {
        e.preventDefault();
        var id = $(this).data('id');

        if (confirm('Are you sure you want to delete this boat fuel price?')) {
            $.ajax({
                url: '/BoatFuelPriceSearch/Delete/' + id,
                type: 'DELETE',
                success: function(result) {
                    table.ajax.reload();
                    alert('Boat fuel price deleted successfully.');
                },
                error: function(xhr) {
                    alert('Error deleting boat fuel price: ' + xhr.responseText);
                }
            });
        }
    });
});
```

**boatFuelPriceEdit.js**:
```javascript
$(document).ready(function() {
    // Initialize Select2 for fuel vendor dropdown in modal
    $('#fuelVendorEdit').select2({
        placeholder: '-- Select Vendor --',
        allowClear: true,
        width: '100%',
        dropdownParent: $('#editModal')
    });

    // Initialize currency input masking
    $('.currency-input').maskMoney({
        prefix: '',
        allowNegative: false,
        thousands: ',',
        decimal: '.',
        affixesStay: false,
        precision: 4
    });

    // Conditional logic: Enable/disable Invoice Number based on Fuel Vendor
    $('#fuelVendorEdit').on('change', function() {
        var hasVendor = $(this).val() !== '';
        $('#invoiceNumber').prop('disabled', !hasVendor);

        if (!hasVendor) {
            $('#invoiceNumber').val('');
        }
    });

    // Trigger on initial load
    $('#fuelVendorEdit').trigger('change');

    // Form submit handler
    $('#editForm').on('submit', function(e) {
        e.preventDefault();

        var form = $(this);
        var url = form.attr('action');

        $.ajax({
            url: url,
            type: 'POST',
            data: form.serialize(),
            success: function(result) {
                $('#editModal').modal('hide');
                $('#boatFuelPriceTable').DataTable().ajax.reload();
                alert('Boat fuel price saved successfully.');
            },
            error: function(xhr) {
                alert('Error saving boat fuel price: ' + xhr.responseText);
            }
        });
    });
});
```

### Phase 4: Testing

#### Step 4.1: Unit Tests
- Test repository CRUD operations
- Test service business logic
- Test DTO validation

#### Step 4.2: Integration Tests
- Test API endpoints
- Test authorization policies
- Test database operations

#### Step 4.3: UI Tests
- Test search functionality
- Test add/edit/delete operations
- Test conditional validation (InvoiceNumber)
- Test DataTables sorting and pagination

### Phase 5: Deployment

#### Step 5.1: Database Migration
- Ensure BoatFuelPrice table exists
- Verify foreign key constraints
- Test GenerateBoatFuelPrices stored procedure

#### Step 5.2: Deploy to Staging
- Deploy API changes
- Deploy UI changes
- Verify configuration

#### Step 5.3: User Acceptance Testing
- Test all CRUD operations
- Verify security/authorization
- Test grid state persistence

#### Step 5.4: Deploy to Production
- Deploy during maintenance window
- Monitor for errors
- Gather user feedback

## Common Issues and Solutions

### Issue 1: DTOs Not Found
**Solution**: Ensure BargeOps.Shared project is referenced by both API and UI projects.

### Issue 2: Authorization Failing
**Solution**: Verify user has correct claims in `FunctionalArea` and `AccessLevel`.

### Issue 3: InvoiceNumber Not Clearing
**Solution**: Check JavaScript conditional logic in `boatFuelPriceEdit.js`.

### Issue 4: Decimal Precision Issues
**Solution**: Ensure SQL column is `money` type and DTO property is `decimal`.

### Issue 5: DataTables Not Loading
**Solution**: Check browser console for JavaScript errors, verify API endpoint is accessible.

## Maintenance Notes

- Price field supports 4 decimal places for precision
- InvoiceNumber has conditional logic based on FuelVendorBusinessUnitID
- Grid state persistence uses DataTables `stateSave` with localStorage
- Authentication scheme is `IdentityConstants.ApplicationScheme`
- No ViewBag/ViewData - all data on ViewModels (MVVM pattern)

## References

See `conversion-plan.md` for detailed architecture and data flow information.
