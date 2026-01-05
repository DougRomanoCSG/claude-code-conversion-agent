# Vendor Entity - Conversion Plan

## Overview

This document provides a comprehensive conversion plan for migrating the Vendor entity from the legacy VB.NET/WinForms application to the modern ASP.NET Core MVC architecture with a **MONO SHARED structure**.

### Entity Summary
- **Legacy Forms**: `frmVendorSearch`, `frmVendorDetail`
- **Primary Key**: `VendorID` (Int32)
- **Child Entities**: VendorContact, VendorBusinessUnit, VendorPortalGroup
- **Special Features**:
  - License-based features (Portal, UnitTow, BargeEx)
  - Conditional validation (BargeEx fields)
  - Soft delete (IsActive flag)
  - Complex tab structure with nested entities

---

## Critical Architecture Notes

### ⭐ MONO SHARED STRUCTURE
**DTOs are the ONLY data models - NO separate domain models!**

```
BargeOps.Shared/
└── Dto/
    ├── VendorDto.cs              ← Complete entity DTO (used by BOTH API and UI)
    ├── VendorSearchRequest.cs     ← Search criteria
    ├── VendorContactDto.cs        ← Child entity DTO
    ├── VendorBusinessUnitDto.cs   ← Child entity DTO
    └── PagedResult.cs, DataTableResponse.cs ← Generic wrappers
```

**NO Models/ folder - DTOs are used directly by:**
- API Repositories (return DTOs)
- API Services (accept/return DTOs)
- API Controllers (accept/return DTOs)
- UI Services (return DTOs from API)
- UI ViewModels (contain DTOs)

---

## Implementation Order

### 1. ⭐ SHARED PROJECT FIRST (HIGHEST PRIORITY)
Create DTOs in `BargeOps.Shared` before any other code!

### 2. API Infrastructure
- Repositories (return DTOs directly)
- Services (use DTOs)
- Controllers (accept/return DTOs)

### 3. UI Components
- Services (API clients returning DTOs)
- ViewModels (contain DTOs)
- Controllers and Views

---

## Phase 1: Shared DTOs (C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared)

### VendorDto.cs
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\Dto\VendorDto.cs`

**Purpose**: Complete entity DTO with [Sortable] and [Filterable] attributes for DataTables

**Key Properties**:
- VendorID (primary key)
- Name, LongName (required, max 20/50)
- Contact info (Address, City, State, Zip, Phone, Fax, Email)
- AccountingCode, TermsCode
- Feature flags (IsActive, EnablePortal, IsBargeExEnabled, IsLiquidBroker, IsInternalVendor, IsTankerman)
- BargeEx fields (BargeExTradingPartnerNum, BargeExConfigID)
- Child collections (VendorContacts, VendorBusinessUnits)

**Validation Attributes**:
```csharp
[Required(ErrorMessage = "Name is required")]
[StringLength(20)]
[Sortable]
[Filterable]
public string Name { get; set; }

[Required(ErrorMessage = "Long name is required")]
[StringLength(50)]
[Sortable]
[Filterable]
public string LongName { get; set; }
```

### VendorSearchRequest.cs
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\Dto\VendorSearchRequest.cs`

**Purpose**: Search criteria for filtering vendors

**Properties**:
- Name (string)
- AccountingCode (string)
- IsActiveOnly (bool, default: true)
- FuelSuppliersOnly (bool)
- InternalVendorOnly (bool)
- IsBargeExEnabledOnly (bool)
- EnablePortalOnly (bool)
- LiquidBrokerOnly (bool)
- TankermanOnly (bool)

### VendorContactDto.cs
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\Dto\VendorContactDto.cs`

**Properties**:
- VendorContactID (primary key)
- VendorID (foreign key)
- Name (required, max 80)
- PhoneNumber, PhoneExt, FaxNumber (Phone10 format)
- EmailAddress (Email format, max 100)
- IsPrimary, IsDispatcher, IsLiquidBroker (bool)
- PortalUserID (string, for portal integration)

### VendorBusinessUnitDto.cs
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\Dto\VendorBusinessUnitDto.cs`

**Properties**:
- VendorBusinessUnitID (primary key)
- VendorID (foreign key)
- Name (required, max 50)
- AccountingCode (max 50)
- IsActive (bool)
- River (string, max 3), Mile (decimal), Bank (string, max 50)
- IsFuelSupplier, IsDefaultFuelSupplier, IsBoatAssistSupplier (bool)
- MinDiscountQty (decimal), MinDiscountFrequency (string, max 20)

---

## Phase 2: API Project (C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API)

### 2.1 Repository Layer

#### IVendorRepository.cs
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Domain\Repositories\IVendorRepository.cs`

```csharp
public interface IVendorRepository
{
    Task<PagedResult<VendorDto>> SearchAsync(VendorSearchRequest request, int page, int pageSize);
    Task<DataTableResponse<VendorDto>> GetDataTableAsync(DataTableRequest request, VendorSearchRequest searchCriteria);
    Task<VendorDto?> GetByIdAsync(int vendorId);
    Task<int> CreateAsync(VendorDto vendor);
    Task UpdateAsync(VendorDto vendor);
    Task SetActiveAsync(int vendorId, bool isActive);

    // Child entity methods
    Task<IEnumerable<VendorContactDto>> GetContactsAsync(int vendorId);
    Task<VendorContactDto?> GetContactByIdAsync(int vendorContactId);
    Task<int> CreateContactAsync(VendorContactDto contact);
    Task UpdateContactAsync(VendorContactDto contact);
    Task DeleteContactAsync(int vendorContactId);

    Task<IEnumerable<VendorBusinessUnitDto>> GetBusinessUnitsAsync(int vendorId);
    Task<VendorBusinessUnitDto?> GetBusinessUnitByIdAsync(int vendorBusinessUnitId);
    Task<int> CreateBusinessUnitAsync(VendorBusinessUnitDto businessUnit);
    Task UpdateBusinessUnitAsync(VendorBusinessUnitDto businessUnit);
    Task DeleteBusinessUnitAsync(int vendorBusinessUnitId);
}
```

#### VendorRepository.cs
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Repositories\VendorRepository.cs`

**Pattern**: Dapper with DIRECT SQL QUERIES (NOT stored procedures)

**Key Methods**:
- `SearchAsync`: Execute parameterized SQL with search criteria
- `GetDataTableAsync`: Server-side DataTables with dynamic sorting/filtering
- `GetByIdAsync`: Return complete DTO with child collections
- `CreateAsync`: INSERT with parameterized SQL, return new ID
- `UpdateAsync`: UPDATE with parameterized SQL
- `SetActiveAsync`: Update IsActive flag only

**References**:
- `FacilityRepository.cs`: Dapper patterns, DTO mapping
- `BoatLocationRepository.cs`: Complete CRUD examples

### 2.2 Service Layer

#### IVendorService.cs
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Domain\Services\IVendorService.cs`

```csharp
public interface IVendorService
{
    Task<PagedResult<VendorDto>> SearchVendorsAsync(VendorSearchRequest request, int page, int pageSize);
    Task<DataTableResponse<VendorDto>> GetVendorDataTableAsync(DataTableRequest request, VendorSearchRequest searchCriteria);
    Task<VendorDto?> GetVendorByIdAsync(int vendorId);
    Task<int> CreateVendorAsync(VendorDto vendor);
    Task UpdateVendorAsync(VendorDto vendor);
    Task SetVendorActiveAsync(int vendorId, bool isActive);

    // Child entity methods follow same pattern
}
```

#### VendorService.cs
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Services\VendorService.cs`

**Pattern**: Thin service layer delegating to repository

**Key Responsibilities**:
- Business logic (e.g., clearing BargeEx fields when disabled)
- Validation (e.g., BargeEx conditional validation)
- Transaction management for complex operations
- Cache invalidation (if needed)

**References**: `FacilityService.cs`, `BoatLocationService.cs`

### 2.3 API Controller

#### VendorController.cs
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Api\Controllers\VendorController.cs`

**Authentication**: `[ApiKey]` attribute (NOT Windows Auth)

**Endpoints**:
```csharp
[ApiKey]
[Route("api/[controller]")]
public class VendorController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<VendorDto>>> GetVendors([FromQuery] VendorSearchRequest request, int page = 1, int pageSize = 25);

    [HttpPost("datatable")]
    public async Task<ActionResult<DataTableResponse<VendorDto>>> GetVendorDataTable([FromBody] DataTableRequest request, [FromQuery] VendorSearchRequest searchCriteria);

    [HttpGet("{id}")]
    public async Task<ActionResult<VendorDto>> GetVendor(int id);

    [HttpPost]
    public async Task<ActionResult<VendorDto>> CreateVendor([FromBody] VendorDto vendor);

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateVendor(int id, [FromBody] VendorDto vendor);

    [HttpPatch("{id}/active")]
    public async Task<IActionResult> SetActive(int id, [FromBody] SetActiveRequest request);

    // Child entity endpoints
    [HttpGet("{vendorId}/contacts")]
    public async Task<ActionResult<IEnumerable<VendorContactDto>>> GetContacts(int vendorId);

    [HttpPost("{vendorId}/contacts")]
    public async Task<ActionResult<VendorContactDto>> CreateContact(int vendorId, [FromBody] VendorContactDto contact);

    // ... additional endpoints for contacts and business units
}
```

**References**: `FacilityController.cs`, `BoatLocationController.cs`

---

## Phase 3: UI Project (C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI)

### 3.1 ViewModels

#### VendorSearchViewModel.cs
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\ViewModels\VendorSearchViewModel.cs`

**Purpose**: Search/list screen data

```csharp
namespace BargeOpsAdmin.ViewModels;

public class VendorSearchViewModel
{
    [Display(Name = "Name")]
    public string? Name { get; set; }

    [Display(Name = "Accounting #")]
    public string? AccountingCode { get; set; }

    [Display(Name = "Active Only")]
    public bool IsActiveOnly { get; set; } = true;

    [Display(Name = "Fuel Suppliers Only")]
    public bool FuelSuppliersOnly { get; set; }

    [Display(Name = "Internal Vendor Only")]
    public bool InternalVendorOnly { get; set; }

    [Display(Name = "BargeEx Enabled Only")]
    public bool IsBargeExEnabledOnly { get; set; }

    [Display(Name = "Portal Enabled Only")]
    public bool EnablePortalOnly { get; set; }

    [Display(Name = "Liquid Broker Only")]
    public bool LiquidBrokerOnly { get; set; }

    [Display(Name = "Tankerman Only")]
    public bool TankermanOnly { get; set; }
}
```

#### VendorEditViewModel.cs
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\ViewModels\VendorEditViewModel.cs`

**Purpose**: Edit/create form data

```csharp
namespace BargeOpsAdmin.ViewModels;

public class VendorEditViewModel
{
    // ⭐ Contains DTOs from Shared project
    public VendorDto Vendor { get; set; } = new();

    // Lookup lists
    public IEnumerable<SelectListItem> States { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> TermsCodes { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> BargeExConfigs { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Rivers { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Banks { get; set; } = new List<SelectListItem>();

    // License flags for conditional UI
    public bool PortalLicenseActive { get; set; }
    public bool UnitTowLicenseActive { get; set; }
    public bool BargeExEnabled { get; set; }
}
```

### 3.2 API Client Services

#### IVendorService.cs (UI)
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Services\IVendorService.cs`

```csharp
public interface IVendorService
{
    Task<PagedResult<VendorDto>> SearchVendorsAsync(VendorSearchRequest request, int page, int pageSize);
    Task<DataTableResponse<VendorDto>> GetVendorDataTableAsync(DataTableRequest request, VendorSearchRequest searchCriteria);
    Task<VendorDto?> GetVendorByIdAsync(int vendorId);
    Task<VendorDto?> CreateVendorAsync(VendorDto vendor);
    Task<bool> UpdateVendorAsync(VendorDto vendor);
    Task<bool> SetVendorActiveAsync(int vendorId, bool isActive);
}
```

#### VendorService.cs (UI)
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Services\VendorService.cs`

**Pattern**: HTTP client calling API endpoints

```csharp
public class VendorService : IVendorService
{
    private readonly HttpClient _httpClient;

    public VendorService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("BargeOpsApi");
    }

    public async Task<DataTableResponse<VendorDto>> GetVendorDataTableAsync(
        DataTableRequest request,
        VendorSearchRequest searchCriteria)
    {
        var queryString = BuildQueryString(searchCriteria);
        var response = await _httpClient.PostAsJsonAsync(
            $"api/vendor/datatable{queryString}",
            request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DataTableResponse<VendorDto>>();
    }

    // ... other methods
}
```

### 3.3 UI Controllers

#### VendorSearchController.cs
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Controllers\VendorSearchController.cs`

**Pattern**: Follow `BoatLocationSearchController.cs`

```csharp
[Authorize]
public class VendorSearchController : AppController
{
    private readonly IVendorService _vendorService;

    public VendorSearchController(IVendorService vendorService)
    {
        _vendorService = vendorService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var model = new VendorSearchViewModel
        {
            IsActiveOnly = true
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> VendorTable(
        [FromBody] DataTableRequest request,
        [FromQuery] VendorSearchViewModel searchModel)
    {
        var searchRequest = new VendorSearchRequest
        {
            Name = searchModel.Name,
            AccountingCode = searchModel.AccountingCode,
            ActiveOnly = searchModel.IsActiveOnly,
            FuelSuppliersOnly = searchModel.FuelSuppliersOnly,
            InternalVendorOnly = searchModel.InternalVendorOnly,
            IsBargeExEnabledOnly = searchModel.IsBargeExEnabledOnly,
            EnablePortalOnly = searchModel.EnablePortalOnly,
            LiquidBrokerOnly = searchModel.LiquidBrokerOnly,
            TankermanOnly = searchModel.TankermanOnly
        };

        var result = await _vendorService.GetVendorDataTableAsync(request, searchRequest);
        return Json(result);
    }

    [HttpGet]
    [Authorize(Policy = "VendorModify")]
    public IActionResult Create()
    {
        var model = new VendorEditViewModel();
        // Load lookup lists
        return View("Edit", model);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "VendorModify")]
    public async Task<IActionResult> Edit(int id)
    {
        var vendor = await _vendorService.GetVendorByIdAsync(id);
        if (vendor == null)
            return NotFound();

        var model = new VendorEditViewModel
        {
            Vendor = vendor
        };
        // Load lookup lists
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = "VendorModify")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(VendorEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // Reload lookup lists
            return View(model);
        }

        if (model.Vendor.VendorID == 0)
        {
            var created = await _vendorService.CreateVendorAsync(model.Vendor);
            return RedirectToAction(nameof(Index));
        }
        else
        {
            await _vendorService.UpdateVendorAsync(model.Vendor);
            return RedirectToAction(nameof(Index));
        }
    }
}
```

### 3.4 Views

#### Index.cshtml
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Views\VendorSearch\Index.cshtml`

**Layout**: Search form + DataTables grid

**Key Sections**:
1. Search criteria form (Name, AccountingCode, checkboxes)
2. Search/Reset buttons
3. Create New button (if authorized)
4. DataTables grid initialization

#### Edit.cshtml
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Views\VendorSearch\Edit.cshtml`

**Layout**: Tabbed form with:
1. **Details Tab**: Vendor information + Contacts grid
2. **Portal Tab**: Portal settings (license-dependent)
3. **Vendor Business Units Tab**: Business units grid
4. **BargeEx Settings Tab**: BargeEx config (license-dependent)

**Key Features**:
- Bootstrap tabs
- Inline child entity grids (DataTables client-side)
- Conditional validation for BargeEx fields
- Phone number masking
- State/BargeExConfig dropdowns with Select2

### 3.5 JavaScript

#### vendor-search.js
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\wwwroot\js\vendor-search.js`

**Pattern**: Follow `boatLocationSearch.js:32-214`

**Key Functions**:
```javascript
$(document).ready(function() {
    // Initialize DataTables
    var dataTable = $('#vendorTable').DataTable({
        processing: true,
        serverSide: true,
        stateSave: true,
        ajax: {
            url: '/VendorSearch/VendorTable',
            type: 'POST',
            data: function(d) {
                d.name = $('#Name').val();
                d.accountingCode = $('#AccountingCode').val();
                d.isActiveOnly = $('#IsActiveOnly').is(':checked');
                d.fuelSuppliersOnly = $('#FuelSuppliersOnly').is(':checked');
                d.internalVendorOnly = $('#InternalVendorOnly').is(':checked');
                d.isBargeExEnabledOnly = $('#IsBargeExEnabledOnly').is(':checked');
                d.enablePortalOnly = $('#EnablePortalOnly').is(':checked');
                d.liquidBrokerOnly = $('#LiquidBrokerOnly').is(':checked');
                d.tankermanOnly = $('#TankermanOnly').is(':checked');
            }
        },
        columns: [
            {
                data: null,
                orderable: false,
                searchable: false,
                width: '80px',
                render: function(data, type, row) {
                    return '<a href="/VendorSearch/Edit/' + row.vendorID + '" class="btn btn-sm btn-primary"><i class="fas fa-edit"></i></a>';
                }
            },
            { data: 'name', name: 'name' },
            { data: 'longName', name: 'longName' },
            { data: 'accountingCode', name: 'accountingCode' },
            {
                data: 'isActive',
                render: function(data) {
                    return '<input type="checkbox" ' + (data ? 'checked' : '') + ' disabled />';
                }
            }
            // ... additional columns
        ],
        order: [[1, 'asc']],
        pageLength: 25
    });

    // Search button
    $('#btnSearch').on('click', function() {
        dataTable.ajax.reload();
    });

    // Reset button
    $('#btnReset').on('click', function() {
        $('#searchForm')[0].reset();
        $('#IsActiveOnly').prop('checked', true);
        dataTable.ajax.reload();
    });

    // Auto-search on checkbox change
    $('input[type="checkbox"]').on('change', function() {
        dataTable.ajax.reload();
    });
});
```

#### vendor-edit.js
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\wwwroot\js\vendor-edit.js`

**Key Functions**:
```javascript
$(document).ready(function() {
    // Initialize Select2
    $('.select2').select2({
        placeholder: 'Select...',
        allowClear: true
    });

    // Phone number masking
    $('[data-mask]').each(function() {
        $(this).mask($(this).data('mask'));
    });

    // Conditional validation for BargeEx
    $('#IsBargeExEnabled').on('change', function() {
        var isEnabled = $(this).is(':checked');
        $('#BargeExTradingPartnerNum, #BargeExConfigID')
            .prop('disabled', !isEnabled)
            .prop('required', isEnabled);
    }).trigger('change');

    // Initialize child entity grids (client-side DataTables)
    $('#vendorContactsTable').DataTable({
        paging: false,
        searching: false,
        ordering: true,
        info: false
    });

    $('#vendorBusinessUnitsTable').DataTable({
        paging: false,
        searching: false,
        ordering: true,
        info: false
    });

    // Form validation
    $('#vendorForm').validate({
        rules: {
            'Vendor.Name': {
                required: true,
                maxlength: 20
            },
            'Vendor.LongName': {
                required: true,
                maxlength: 50
            },
            'Vendor.BargeExTradingPartnerNum': {
                required: function() {
                    return $('#IsBargeExEnabled').is(':checked');
                },
                maxlength: 8
            },
            'Vendor.BargeExConfigID': {
                required: function() {
                    return $('#IsBargeExEnabled').is(':checked');
                }
            }
        },
        messages: {
            'Vendor.Name': {
                required: 'Name is required',
                maxlength: 'Name cannot exceed 20 characters'
            },
            'Vendor.LongName': {
                required: 'Long name is required',
                maxlength: 'Long name cannot exceed 50 characters'
            },
            'Vendor.BargeExTradingPartnerNum': {
                required: 'Both Trading partner number & Configuration required for BargeEx enabled.'
            },
            'Vendor.BargeExConfigID': {
                required: 'Both Trading partner number & Configuration required for BargeEx enabled.'
            }
        }
    });
});
```

---

## Validation Strategy

### Server-Side Validation

#### FluentValidation (API)
```csharp
public class VendorDtoValidator : AbstractValidator<VendorDto>
{
    public VendorDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(20).WithMessage("Name cannot exceed 20 characters");

        RuleFor(x => x.LongName)
            .NotEmpty().WithMessage("Long name is required")
            .MaximumLength(50).WithMessage("Long name cannot exceed 50 characters");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\d{10}$").When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("Phone number must be 10 digits");

        RuleFor(x => x.EmailAddress)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.EmailAddress))
            .WithMessage("Invalid email address");

        RuleFor(x => x.BargeExTradingPartnerNum)
            .NotEmpty().When(x => x.IsBargeExEnabled)
            .WithMessage("Both Trading partner number & Configuration required for BargeEx enabled.");

        RuleFor(x => x.BargeExConfigID)
            .NotEmpty().When(x => x.IsBargeExEnabled)
            .WithMessage("Both Trading partner number & Configuration required for BargeEx enabled.");
    }
}
```

### Client-Side Validation

#### Data Annotations (UI ViewModels)
Use Data Annotations on ViewModels for client-side unobtrusive validation

#### jQuery Validate
Custom conditional validation for BargeEx fields

---

## Security Implementation

### API Authentication
- **Pattern**: `[ApiKey]` attribute on all API controllers
- **NOT Windows Auth** - use API Key authentication

### UI Authentication
- **Production**: OIDC (Azure AD) with `[Authorize]` attribute
- **Development**: DevelopmentAutoSignInMiddleware

### Permissions
```csharp
public enum AuthPermissions
{
    VendorView,      // Search, list, view details
    VendorModify     // Create, update, activate/deactivate
}
```

### Authorization Patterns
```csharp
// API Controller
[ApiKey]
[Route("api/[controller]")]
public class VendorController : ControllerBase { }

// UI Controller
[Authorize]
public class VendorSearchController : AppController
{
    [HttpGet]
    [Authorize(Policy = "VendorModify")]
    public IActionResult Create() { }
}
```

### UI Element Security
```razor
@if (User.HasPermission(AuthPermissions.VendorModify))
{
    <a href="@Url.Action("Create", "VendorSearch")" class="btn btn-primary">
        <i class="fas fa-plus"></i> Create New Vendor
    </a>
}
```

---

## License-Based Features

### Portal Tab
- **License**: Portal
- **UI Elements**: Portal tab, EnablePortal checkbox, Portal Groups grid
- **Check**: `LicenseList(Licenses.LicenseComponent.Portal).Active`

### UnitTow Features
- **License**: UnitTow
- **UI Elements**: IsLiquidBroker, IsTankerman checkboxes
- **Search Filters**: LiquidBrokerOnly, TankermanOnly
- **Check**: `LicenseList(Licenses.LicenseComponent.UnitTow).Active`

### BargeEx Settings
- **License**: EnableBargeExBargeLineSupport (global setting)
- **UI Elements**: BargeEx Settings tab, IsBargeExEnabled checkbox, BargeExTradingPartnerNum, BargeExConfigID
- **Check**: `Lists.GlobalSettingList.EnableBargeExBargeLineSupport`

---

## Testing Strategy

### Unit Tests
- Repository methods (Dapper SQL execution, DTO mapping)
- Service methods (business logic, validation)
- Validators (FluentValidation rules)

### Integration Tests
- API endpoints (request/response)
- UI controllers (view rendering, model binding)
- End-to-end workflows (create, update, search)

### UI Tests
- DataTables initialization and server-side processing
- Conditional validation (BargeEx fields)
- Tab navigation
- Child entity grids (contacts, business units)

---

## Migration Checklist

### Pre-Migration
- [ ] Review all analysis files
- [ ] Identify any missing business logic or special cases
- [ ] Set up development environment
- [ ] Create feature branch

### Phase 1: Shared DTOs
- [ ] Create VendorDto.cs with all properties and attributes
- [ ] Create VendorSearchRequest.cs
- [ ] Create VendorContactDto.cs
- [ ] Create VendorBusinessUnitDto.cs
- [ ] Verify DTOs compile and reference correctly

### Phase 2: API
- [ ] Create IVendorRepository interface
- [ ] Implement VendorRepository with Dapper
- [ ] Create unit tests for repository
- [ ] Create IVendorService interface
- [ ] Implement VendorService
- [ ] Create unit tests for service
- [ ] Create VendorController
- [ ] Create integration tests for API
- [ ] Test all endpoints with Postman/Swagger

### Phase 3: UI
- [ ] Create VendorSearchViewModel
- [ ] Create VendorEditViewModel
- [ ] Create IVendorService (UI)
- [ ] Implement VendorService (UI HTTP client)
- [ ] Create VendorSearchController
- [ ] Create Index.cshtml (search view)
- [ ] Create Edit.cshtml (edit view)
- [ ] Create vendor-search.js
- [ ] Create vendor-edit.js
- [ ] Test UI functionality manually

### Phase 4: Testing
- [ ] Run all unit tests
- [ ] Run all integration tests
- [ ] Manual UI testing (search, create, edit, delete)
- [ ] Test license-based features
- [ ] Test conditional validation (BargeEx)
- [ ] Test child entity operations (contacts, business units)
- [ ] Performance testing (DataTables with large datasets)

### Phase 5: Deployment
- [ ] Code review
- [ ] Update documentation
- [ ] Create deployment scripts
- [ ] Deploy to staging environment
- [ ] User acceptance testing
- [ ] Deploy to production
- [ ] Monitor for issues

---

## Reference Implementations

### Primary References (MONO SHARED Structure)
- **BargeOps.Shared/Dto/FacilityDto.cs**: Complete DTO example with attributes
- **BargeOps.Shared/Dto/BoatLocationDto.cs**: Full DTO used by both API and UI
- **BargeOps.Shared/Dto/FacilitySearchRequest.cs**: Search request pattern
- **Admin.Infrastructure/Repositories/FacilityRepository.cs**: Dapper patterns
- **Admin.Infrastructure/Services/FacilityService.cs**: Service patterns
- **Admin.Api/Controllers/FacilityController.cs**: API controller patterns
- **BargeOps.UI/Controllers/BoatLocationSearchController.cs**: UI controller patterns
- **BargeOps.UI/ViewModels/BoatLocationSearchViewModel.cs**: ViewModel patterns
- **BargeOps.UI/wwwroot/js/boatLocationSearch.js**: DataTables patterns

### Secondary References (Crewing - Separate API/UI)
- **Crewing.Domain/Dto/CrewingDto.cs**: DTO examples
- **Crewing.Infrastructure/Repositories/CrewingRepository.cs**: Repository patterns
- **Crewing.UI/Controllers/CrewingSearchController.cs**: Search patterns
- **Crewing.UI/wwwroot/js/crewingSearch.js**: JavaScript patterns

---

## Common Pitfalls to Avoid

1. **DON'T create separate Models folder** - DTOs ARE the models!
2. **DON'T use AutoMapper** - Repositories return DTOs directly
3. **DON'T use stored procedures** - Use parameterized SQL queries
4. **DON'T forget [Sortable]/[Filterable] attributes** on DTO properties
5. **DON'T use Windows Auth for API** - Use ApiKey attribute
6. **DON'T forget conditional validation** for BargeEx fields
7. **DON'T forget license checks** for Portal/UnitTow features
8. **DON'T use ViewBag/ViewData** - Use ViewModels with DTOs
9. **DON'T forget DataTables server-side** processing for search
10. **DON'T forget soft delete pattern** (IsActive flag, not hard delete)

---

## Next Steps

1. Generate all template files in `templates/` folder
2. Review templates with team
3. Begin implementation starting with Shared DTOs
4. Follow implementation order strictly (Shared → API → UI)
5. Test each phase before proceeding to next

---

**Generated**: 2025-12-15
**Entity**: Vendor
**Status**: Ready for implementation
