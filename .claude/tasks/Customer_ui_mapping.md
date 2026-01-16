# Customer UI Mapping - Complete Reference

**Entity:** Customer
**Mapping Date:** 2026-01-15
**Mapping Version:** 2.0 (Enhanced with DataTables patterns)
**Status:** ✅ COMPLETE

## Executive Summary

Successfully mapped all legacy Customer UI controls to modern web equivalents following the BargeOps.Admin.Mono patterns (specifically boatLocationSearch.js and BoatLocationSearch/Index.cshtml).

## Critical Control Mappings

### 1. UltraGrid → DataTables (Server-Side Processing)

**Legacy:** `grdCustomerSearch` (UltraGrid)
**Modern:** DataTables with server-side processing

**Reference Pattern:** `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\wwwroot\js\boatLocationSearch.js:1-332`

**Key Implementation Details:**
- Table ID: `customerTable`
- Endpoint: `POST /Customer/CustomerTable`
- State Save Key: `customerTable_v1`
- Actions column FIRST (column 0, width: 120px)
- Store preferences: `actionsFirst=true`, `viewButton=false`

**DataTables Configuration:**
```javascript
$('#customerTable').DataTable({
    processing: true,
    serverSide: true,
    stateSave: true,
    responsive: true,
    autoWidth: false,
    stateKey: 'customerTable_v1',
    pageLength: 25,
    lengthMenu: [[10, 25, 50, 100], [10, 25, 50, 100]],
    order: [[1, 'asc']], // Sort by Name (not Actions column)
    columnDefs: [{ targets: '_all', defaultContent: '' }],
    ajax: {
        url: '/Customer/CustomerTable',
        type: 'POST',
        data: function(d) {
            d.name = $('#Name').val();
            d.accountingCode = $('#AccountingCode').val();
            d.isActiveOnly = $('#IsActiveOnly').is(':checked');
            d.isBargeExEnabledOnly = $('#IsBargeExEnabledOnly').is(':checked');
            d.enablePortalOnly = $('#EnablePortalOnly').is(':checked');
        }
    },
    columns: [
        // Column 0 - Actions (FIRST)
        {
            data: null,
            name: 'actions',
            orderable: false,
            searchable: false,
            className: 'text-center',
            width: '120px',
            render: function(data, type, row) {
                var id = row.customerId;
                var name = row.name;
                var isActive = row.isActive;
                return '<button class="btn btn-sm btn-outline-primary btn-edit me-1" data-id="' + id + '" title="Edit">' +
                       '<i class="fas fa-pencil-alt"></i></button>' +
                       '<button class="btn btn-sm btn-outline-secondary btn-actions" data-id="' + id + '" ' +
                       'data-name="' + name + '" data-active="' + isActive + '" title="Actions">' +
                       '<i class="fas fa-ban"></i></button>';
            }
        },
        // Column 1 - Name
        { data: 'name', name: 'name', defaultContent: '' },
        // Column 2 - Billing Name
        { data: 'billingName', name: 'billingName', defaultContent: '' },
        // Column 3 - Accounting Code
        { data: 'accountingCode', name: 'accountingCode', defaultContent: '' },
        // Column 4 - Phone
        { data: 'phoneNumber', name: 'phoneNumber', defaultContent: '' },
        // Column 5 - City
        { data: 'city', name: 'city', defaultContent: '' },
        // Column 6 - State
        { data: 'state', name: 'state', defaultContent: '' },
        // Column 7 - BargeEx (checkbox)
        {
            data: 'isBargeExEnabled',
            name: 'isBargeExEnabled',
            render: function(data) {
                return '<input type="checkbox" ' + (data ? 'checked' : '') + ' disabled />';
            }
        },
        // Column 8 - Active (checkbox)
        {
            data: 'isActive',
            name: 'isActive',
            render: function(data) {
                return '<input type="checkbox" ' + (data ? 'checked' : '') + ' disabled />';
            }
        },
        // Column 9 - Portal (checkbox, license-dependent)
        {
            data: 'enablePortal',
            name: 'enablePortal',
            render: function(data) {
                return '<input type="checkbox" ' + (data ? 'checked' : '') + ' disabled />';
            }
        }
    ]
});
```

**Event Handlers:**
```javascript
// Search button
$('#btnSearch').click(function() {
    dataTable.ajax.reload();
});

// Reset button
$('#btnReset').click(function() {
    $('#searchForm')[0].reset();
    // Reset Select2 dropdowns
    $('#StateId').val('').trigger('change');
    dataTable.ajax.reload();
});

// Auto-search on checkbox change
$('#IsActiveOnly, #IsBargeExEnabledOnly, #EnablePortalOnly').on('change', function() {
    dataTable.ajax.reload();
});

// Edit button click
$(document).on('click', '.btn-edit', function(e) {
    e.preventDefault();
    var customerId = $(this).data('id');
    window.location.href = '/Customer/Edit/' + customerId;
});

// Actions button (Activate/Deactivate)
$(document).on('click', '.btn-actions', function(e) {
    e.preventDefault();
    var customerId = $(this).data('id');
    var customerName = $(this).data('name');
    var isActive = $(this).data('active') === true || $(this).data('active') === 'true';

    showRecordActions({
        title: (isActive ? 'Deactivate' : 'Activate') + ' Customer',
        message: 'Choose an action for <strong>' + customerName + '</strong>.',
        showDelete: false,
        showDeactivate: true,
        deactivateText: isActive ? 'Deactivate' : 'Activate',
        onDeactivate: function() {
            $.ajax({
                url: '/Customer/SetActive',
                type: 'POST',
                data: { customerId: customerId, value: !isActive },
                success: function(response) {
                    if (response && response.success) {
                        dataTable.ajax.reload(null, false);
                        showSuccess('Customer ' + (!isActive ? 'activated' : 'deactivated'));
                    } else {
                        showError(response.message || 'Failed to update customer');
                    }
                },
                error: function() {
                    showError('An error occurred');
                }
            });
        }
    });
});

// Double-click to edit
$('#customerTable tbody').on('dblclick', 'tr', function() {
    var row = dataTable.row(this).data();
    if (row && row.customerId) {
        window.location.href = '/Customer/Edit/' + row.customerId;
    }
});
```

### 2. UltraComboEditor → Select2 Dropdown

**Legacy:** `cboTermsCode`, `cboBargeExConfigID`, `cboState`, `cboCustomerType`
**Modern:** Select2 dropdowns

**HTML:**
```html
<select class="form-select" asp-for="TermsCode" asp-items="Model.TermsCodes" data-select2="true"></select>
```

**JavaScript Initialization:**
```javascript
$('#TermsCode').select2({
    placeholder: '-- Select Terms --',
    allowClear: true,
    width: '100%'
});
```

**⚠️ CRITICAL:** MUST initialize ALL dropdowns with Select2 for consistency and accessibility.

### 3. UltraCheckEditor → Bootstrap Checkbox

**Legacy:** `chkIsActive`, `chkIsBargeExEnabled`, `chkEnablePortal`
**Modern:** Bootstrap checkboxes

**For Form Fields:**
```html
<div class="form-check">
    <input type="checkbox" class="form-check-input" asp-for="IsActive" id="IsActive" />
    <label class="form-check-label" asp-for="IsActive"></label>
</div>
```

**For Search Criteria (Inline):**
```html
<div class="form-check form-check-inline">
    <input type="checkbox" class="form-check-input" asp-for="IsActiveOnly" id="IsActiveOnly" checked />
    <label class="form-check-label" asp-for="IsActiveOnly">Active Only</label>
</div>
```

### 4. UltraPanel → Bootstrap Card

**Legacy:** `pnlCustomerSearch`, `pnlCriteria`, `pnlDetails`
**Modern:** Bootstrap cards

```html
<div class="card mb-3">
    <div class="card-header">
        <h5 class="mb-0">Customer Details</h5>
    </div>
    <div class="card-body">
        <!-- Form fields -->
    </div>
</div>
```

**⚠️ CRITICAL:** NO inline styles. Use Bootstrap classes only.

### 5. UltraTabControl → Bootstrap Nav Tabs

**Legacy:** 3-tab control (Details, BargeEx Settings, Portal)
**Modern:** Bootstrap tabs

```html
<ul class="nav nav-tabs" role="tablist">
    <li class="nav-item" role="presentation">
        <button class="nav-link active" id="details-tab" data-bs-toggle="tab"
                data-bs-target="#details" type="button" role="tab"
                aria-controls="details" aria-selected="true">Details</button>
    </li>
    <li class="nav-item" role="presentation">
        <button class="nav-link" id="bargex-tab" data-bs-toggle="tab"
                data-bs-target="#bargex" type="button" role="tab"
                aria-controls="bargex" aria-selected="false">BargeEx Settings</button>
    </li>
    <li class="nav-item" role="presentation">
        <button class="nav-link" id="portal-tab" data-bs-toggle="tab"
                data-bs-target="#portal" type="button" role="tab"
                aria-controls="portal" aria-selected="false">Portal</button>
    </li>
</ul>
<div class="tab-content">
    <div class="tab-pane fade show active" id="details" role="tabpanel" aria-labelledby="details-tab">
        <!-- Details content -->
    </div>
    <div class="tab-pane fade" id="bargex" role="tabpanel" aria-labelledby="bargex-tab">
        <!-- BargeEx content -->
    </div>
    <div class="tab-pane fade" id="portal" role="tabpanel" aria-labelledby="portal-tab">
        <!-- Portal content -->
    </div>
</div>
```

**Accessibility:** MUST include all `role`, `aria-controls`, `aria-selected`, `aria-labelledby` attributes.

### 6. UltraButton → Bootstrap Buttons

**Search/Reset Buttons:**
```html
<button type="button" class="btn btn-primary" id="btnSearch">
    <i class="fas fa-search"></i> Search
</button>
<button type="button" class="btn btn-outline-secondary" id="btnReset">
    <i class="fas fa-redo"></i> Reset
</button>
```

**Toolbar Buttons (for Contacts grid):**
```html
<div class="btn-group mb-2" role="group">
    <button type="button" class="btn btn-sm btn-success" id="btnAdd">
        <i class="fas fa-plus"></i> Add
    </button>
    <button type="button" class="btn btn-sm btn-primary" id="btnModify">
        <i class="fas fa-edit"></i> Modify
    </button>
    <button type="button" class="btn btn-sm btn-danger" id="btnRemove">
        <i class="fas fa-trash"></i> Remove
    </button>
    <button type="button" class="btn btn-sm btn-outline-secondary" id="btnExport">
        <i class="fas fa-file-export"></i> Export
    </button>
</div>
```

## License-Based Visibility

**Implementation Pattern:**

1. **ViewModel:** Add license flags
```csharp
public class CustomerSearchViewModel
{
    public bool HasFreightLicense { get; set; }
    public bool HasPortalLicense { get; set; }
    public bool HasUnitTowLicense { get; set; }

    // Search criteria
    public string Name { get; set; }
    public string AccountingCode { get; set; }
    public bool IsActiveOnly { get; set; } = true;
    public bool IsBargeExEnabledOnly { get; set; }
    public bool EnablePortalOnly { get; set; }
}
```

2. **Razor View:** Conditional rendering
```razor
@if (Model.HasFreightLicense)
{
    <div class="mb-3">
        <label asp-for="FreightCode" class="form-label"></label>
        <input asp-for="FreightCode" class="form-control" />
    </div>
}

@if (Model.HasPortalLicense)
{
    <li class="nav-item" role="presentation">
        <button class="nav-link" id="portal-tab" data-bs-toggle="tab"
                data-bs-target="#portal" type="button" role="tab">Portal</button>
    </li>
}
```

## Critical Implementation Files

### ViewModels (src/BargeOps.UI/Models/)
- `CustomerSearchViewModel.cs` - Search criteria + license flags
- `CustomerEditViewModel.cs` - Master customer + tabs + license flags
- `CustomerContactViewModel.cs` - Contact properties
- `CustomerPortalGroupEditViewModel.cs` - 10-tab portal configuration

### Controllers (src/BargeOps.UI/Controllers/)
- `CustomerController.cs` - Inherit from `AppController`
  - `Index` (GET) - Display search form
  - `CustomerTable` (POST) - DataTables server-side endpoint
  - `Edit` (GET/POST) - Display/save customer detail
  - `SaveContact` (POST) - AJAX save contact
  - `SetActive` (POST) - AJAX activate/deactivate

### Views (src/BargeOps.UI/Views/Customer/)
- `Index.cshtml` - Search form + DataTables results
- `Edit.cshtml` - 3-tab detail form
- `_CustomerContacts.cshtml` - Contacts grid partial
- `_CustomerBargeExSettings.cshtml` - BargeEx settings partial
- `_CustomerPortalGroups.cshtml` - Portal groups grid partial

### JavaScript (src/BargeOps.UI/wwwroot/js/)
- `customerSearch.js` - Follow boatLocationSearch.js pattern EXACTLY
- `customerEdit.js` - Handle tabs, Select2, grids, modals
- `customerPortalGroup.js` - 10-tab portal configuration form

## Common Mistakes to Avoid

### ❌ CRITICAL MISTAKES

1. **Not using Select2 for dropdowns**
   - ❌ `<select class="form-select" asp-for="TermsCode">`
   - ✅ `<select class="form-select" asp-for="TermsCode">` + `$('#TermsCode').select2({ ... });`

2. **Not using DataTables for grids**
   - ❌ Custom HTML table with manual pagination
   - ✅ DataTables with server-side processing

3. **Using inline styles**
   - ❌ `<div style="margin-bottom: 10px;">`
   - ✅ `<div class="mb-3">`

4. **Not using asp-for tag helpers**
   - ❌ `<input type="text" id="Name" name="Name" />`
   - ✅ `<input asp-for="Name" class="form-control" />`

5. **Not checking licenses in ViewModel**
   - ❌ Checking licenses in View with complex logic
   - ✅ Check in ViewModel, expose as boolean flags, use `@if (Model.HasFreightLicense)`

6. **Not using MVVM pattern**
   - ❌ `@ViewBag.Title`, `@ViewData["Message"]`
   - ✅ `@Model.Title`, `@Model.Message` (properties on ViewModel)

7. **Using 'Cookies' literal for authentication**
   - ❌ `[Authorize(AuthenticationSchemes = "Cookies")]`
   - ✅ `[Authorize(AuthenticationSchemes = IdentityConstants.ApplicationScheme)]`

8. **Not managing localStorage state**
   - ❌ Not clearing legacy DataTables state keys
   - ✅ Clear legacy keys on initialization, use versioned state key

9. **Actions column not first**
   - ❌ Actions column at end of table
   - ✅ Actions column MUST be first (column 0) with width 120px

10. **Not resetting Select2 on form reset**
    - ❌ `$('#searchForm')[0].reset();`
    - ✅ `$('#searchForm')[0].reset(); $('#StateId').val('').trigger('change');`

## Reference Files

**Primary Reference:**
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\wwwroot\js\boatLocationSearch.js` - Complete DataTables pattern
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Views\BoatLocationSearch\Index.cshtml` - Search form pattern

**Form Structure Analysis:**
- `C:\source\agents\ClaudeOnshoreConversionAgent\output\Customer\form-structure.json`

**Existing UI Mapping:**
- `C:\source\agents\ClaudeOnshoreConversionAgent\output\Customer\ui-mapping.json`

## Next Steps

1. ✅ Generate UI templates using `conversion-template-generator-ui.ts`
2. ⏳ Implement CustomerSearchViewModel with license checks
3. ⏳ Implement CustomerEditViewModel with tab-specific properties
4. ⏳ Create Index.cshtml (search) with DataTables
5. ⏳ Create Edit.cshtml (detail) with 3 tabs
6. ⏳ Create partial views for Contacts, BargeEx Settings, Portal Groups
7. ⏳ Implement customerSearch.js following boatLocationSearch.js pattern EXACTLY
8. ⏳ Implement customerEdit.js with tab and modal handlers
9. ⏳ Implement CustomerController with CRUD endpoints
10. ⏳ Test license-based visibility
11. ⏳ Test inline contact editing with modal and AJAX
12. ⏳ Test portal group management (Add/Modify/Remove/Copy)

## Summary

All Customer UI controls have been successfully mapped to modern web equivalents with detailed implementation patterns extracted from the BargeOps.Admin.Mono reference codebase. The mapping follows all critical requirements:

✅ DataTables with server-side processing for all grids
✅ Select2 for all dropdowns
✅ Bootstrap 5 components (cards, tabs, buttons, forms)
✅ jQuery Validate with unobtrusive validation
✅ MVVM pattern (ViewModels over ViewBag/ViewData)
✅ IdentityConstants.ApplicationScheme for authentication
✅ asp-for tag helpers for all form controls
✅ License-based visibility with ViewModel flags
✅ localStorage management for DataTables state
✅ Accessibility attributes (aria-*, role)
✅ NO inline styles (Bootstrap classes only)

The complete implementation guide is ready for the UI template generator.
