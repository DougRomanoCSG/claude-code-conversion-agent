# BargeEvent Views and JavaScript Implementation Guide

## Overview

This guide provides complete patterns and examples for implementing the Razor Views and JavaScript files for BargeEvent. Due to the complexity and size of these files (1000+ lines each), this document provides the structure, patterns, and critical code snippets needed to build them.

---

## View Files Structure

```
Views/
├── BargeEventSearch/
│   ├── Index.cshtml          (Search/List page)
│   └── _Partials/
│       └── _SearchCriteria.cshtml
└── BargeEventDetail/
    ├── Edit.cshtml            (Create/Edit page)
    └── _Partials/
        ├── _EventDetailsTab.cshtml
        ├── _BillingTab.cshtml
        ├── _FreightBillingTab.cshtml
        └── _BillingAuditTab.cshtml
```

---

## Index.cshtml (Search Page)

### File Location
`Views/BargeEventSearch/Index.cshtml`

### Complete Structure

```cshtml
@model BargeOpsAdmin.ViewModels.BargeEventSearchViewModel
@{
    ViewData["Title"] = "Barge Event Search";
}

<h2><i class="fas fa-ship"></i> Barge Event Search</h2>

<!-- Search Criteria Card -->
<div class="card mb-3">
    <div class="card-header">
        <h5 class="card-title mb-0">
            <i class="fas fa-search"></i> Search Criteria
        </h5>
    </div>
    <div class="card-body">
        <form id="searchForm">
            <div class="row mb-3">
                <!-- Event Type -->
                <div class="col-md-3">
                    <label asp-for="EventTypeId" class="form-label"></label>
                    <select asp-for="EventTypeId" asp-items="Model.EventTypeList" class="form-select select2">
                        <option value="">-- All Event Types --</option>
                    </select>
                </div>

                <!-- Billing Customer -->
                <div class="col-md-3">
                    <label asp-for="BillingCustomerId" class="form-label"></label>
                    <select asp-for="BillingCustomerId" asp-items="Model.CustomerList" class="form-select select2">
                        <option value="">-- All Customers --</option>
                    </select>
                </div>

                <!-- From Location -->
                <div class="col-md-3">
                    <label asp-for="FromLocationId" class="form-label"></label>
                    <select asp-for="FromLocationId" asp-items="Model.FromLocationList" class="form-select select2">
                        <option value="">-- All Locations --</option>
                    </select>
                </div>

                <!-- To Location -->
                <div class="col-md-3">
                    <label asp-for="ToLocationId" class="form-label"></label>
                    <select asp-for="ToLocationId" asp-items="Model.ToLocationList" class="form-select select2">
                        <option value="">-- All Locations --</option>
                    </select>
                </div>
            </div>

            <div class="row mb-3">
                <!-- Servicing Boat -->
                <div class="col-md-3">
                    <label asp-for="FleetBoatId" class="form-label"></label>
                    <select asp-for="FleetBoatId" asp-items="Model.FleetBoatList" class="form-select select2">
                        <option value="">-- All Boats --</option>
                    </select>
                </div>

                <!-- Start Date -->
                <div class="col-md-3">
                    <label asp-for="StartDate" class="form-label"></label>
                    <input asp-for="StartDate" type="date" class="form-control" />
                </div>

                <!-- End Date -->
                <div class="col-md-3">
                    <label asp-for="EndDate" class="form-label"></label>
                    <input asp-for="EndDate" type="date" class="form-control" />
                </div>

                <!-- Barge Numbers -->
                <div class="col-md-3">
                    <label asp-for="BargeNumberList" class="form-label"></label>
                    <input asp-for="BargeNumberList" class="form-control" placeholder="1234,5678,9012" />
                    <small class="form-text text-muted">Comma-separated list</small>
                </div>
            </div>

            <!-- Freight-specific fields (conditional) -->
            @if (Model.IsFreightActive)
            {
                <div class="row mb-3">
                    <!-- Freight Customer -->
                    <div class="col-md-3">
                        <label asp-for="FreightCustomerId" class="form-label"></label>
                        <select asp-for="FreightCustomerId" asp-items="Model.FreightCustomerList" class="form-select select2">
                            <option value="">-- All Freight Customers --</option>
                        </select>
                    </div>

                    <!-- Contract Number -->
                    <div class="col-md-3">
                        <label asp-for="ContractNumber" class="form-label"></label>
                        <input asp-for="ContractNumber" class="form-control" />
                    </div>
                </div>
            }

            <div class="row mb-3">
                <!-- Include Voided -->
                <div class="col-md-3">
                    <div class="form-check">
                        <input asp-for="IncludeVoided" class="form-check-input" type="checkbox" />
                        <label asp-for="IncludeVoided" class="form-check-label"></label>
                    </div>
                </div>
            </div>

            <!-- Buttons -->
            <div class="row">
                <div class="col-12">
                    <button type="button" id="btnSearch" class="btn btn-primary">
                        <i class="fas fa-search"></i> Search
                    </button>
                    <button type="reset" class="btn btn-secondary">
                        <i class="fas fa-undo"></i> Reset
                    </button>
                    @if (Model.CanModify)
                    {
                        <a asp-controller="BargeEventDetail" asp-action="Create" class="btn btn-success float-end">
                            <i class="fas fa-plus"></i> New Event
                        </a>
                    }
                </div>
            </div>
        </form>
    </div>
</div>

<!-- Results Grid Card -->
<div class="card">
    <div class="card-header">
        <h5 class="card-title mb-0">
            <i class="fas fa-list"></i> Search Results
        </h5>
    </div>
    <div class="card-body">
        <div class="table-responsive">
            <table id="bargeEventTable" class="table table-striped table-bordered dt-responsive nowrap" style="width:100%">
                <thead>
                    <tr>
                        <th>Actions</th>
                        <th>Event</th>
                        <th>Barge</th>
                        <th>Ticket</th>
                        <th>Start Time</th>
                        <th>Complete Time</th>
                        @if (Model.IsFreightActive)
                        {
                            <th>C/P Time</th>
                            <th>Release Time</th>
                        }
                        <th>From</th>
                        <th>To</th>
                        <th>Load Status</th>
                        <th>Commodity</th>
                        @if (Model.IsFreightActive)
                        {
                            <th>Qty</th>
                            <th>DQ</th>
                        }
                        <th>Customer</th>
                        <th>Boat</th>
                        <th>Division</th>
                        <th>Inv</th>
                        <th>Rebill</th>
                        <th>Void</th>
                    </tr>
                </thead>
                <tbody></tbody>
            </table>
        </div>
    </div>
</div>

@section Scripts {
    <script src="~/js/barge-event-search.js"></script>
}
```

---

## Edit.cshtml (Create/Edit Page)

### File Location
`Views/BargeEventDetail/Edit.cshtml`

### Complete Structure with Tabs

```cshtml
@model BargeOpsAdmin.ViewModels.BargeEventEditViewModel
@{
    ViewData["Title"] = Model.PageTitle;
}

<h2><i class="fas fa-edit"></i> @Model.PageTitle</h2>

<form asp-action="Save" method="post" id="bargeEventForm">
    @Html.AntiForgeryToken()
    <input type="hidden" asp-for="BargeEvent.TicketEventID" />
    <input type="hidden" asp-for="BargeEvent.TicketID" />

    <div class="card">
        <div class="card-header">
            <!-- Tab Navigation -->
            <ul class="nav nav-tabs card-header-tabs" id="eventTabs" role="tablist">
                <li class="nav-item" role="presentation">
                    <button class="nav-link active" id="details-tab" data-bs-toggle="tab" data-bs-target="#details" type="button" role="tab">
                        <i class="fas fa-info-circle"></i> Event Details
                    </button>
                </li>
                <li class="nav-item" role="presentation">
                    <button class="nav-link" id="barges-tab" data-bs-toggle="tab" data-bs-target="#barges" type="button" role="tab">
                        <i class="fas fa-ship"></i> Barges (@Model.BargeCount)
                    </button>
                </li>
                @if (Model.CanViewBilling)
                {
                    <li class="nav-item" role="presentation">
                        <button class="nav-link" id="billing-tab" data-bs-toggle="tab" data-bs-target="#billing" type="button" role="tab">
                            <i class="fas fa-dollar-sign"></i> Billing
                        </button>
                    </li>
                }
                @if (Model.IsFreightActive && Model.CanViewBilling)
                {
                    <li class="nav-item" role="presentation">
                        <button class="nav-link" id="freight-tab" data-bs-toggle="tab" data-bs-target="#freight" type="button" role="tab">
                            <i class="fas fa-truck"></i> Freight Billing
                        </button>
                    </li>
                }
                @if (Model.CanViewBilling)
                {
                    <li class="nav-item" role="presentation">
                        <button class="nav-link" id="audit-tab" data-bs-toggle="tab" data-bs-target="#audit" type="button" role="tab">
                            <i class="fas fa-history"></i> Billing Audit
                        </button>
                    </li>
                }
            </ul>
        </div>

        <div class="card-body">
            <!-- Validation Summary -->
            <div asp-validation-summary="ModelOnly" class="alert alert-danger"></div>

            <!-- Tab Content -->
            <div class="tab-content" id="eventTabsContent">
                <!-- EVENT DETAILS TAB -->
                <div class="tab-pane fade show active" id="details" role="tabpanel">
                    @await Html.PartialAsync("_Partials/_EventDetailsTab", Model)
                </div>

                <!-- BARGES TAB -->
                <div class="tab-pane fade" id="barges" role="tabpanel">
                    <div id="bargeListContainer">
                        <!-- Barge list loaded via JavaScript/AJAX -->
                    </div>
                </div>

                <!-- BILLING TAB -->
                @if (Model.CanViewBilling)
                {
                    <div class="tab-pane fade" id="billing" role="tabpanel">
                        @await Html.PartialAsync("_Partials/_BillingTab", Model)
                    </div>
                }

                <!-- FREIGHT BILLING TAB -->
                @if (Model.IsFreightActive && Model.CanViewBilling)
                {
                    <div class="tab-pane fade" id="freight" role="tabpanel">
                        @await Html.PartialAsync("_Partials/_FreightBillingTab", Model)
                    </div>
                }

                <!-- BILLING AUDIT TAB -->
                @if (Model.CanViewBilling)
                {
                    <div class="tab-pane fade" id="audit" role="tabpanel">
                        <div id="billingAuditContainer">
                            <!-- Audit trail loaded via JavaScript/AJAX -->
                        </div>
                    </div>
                }
            </div>
        </div>

        <div class="card-footer">
            @if (Model.CanModify || Model.IsEditingReadonlyOperationsWithWritableBilling)
            {
                <button type="submit" class="btn @Model.SubmitButtonClass">
                    <i class="fas fa-save"></i> @Model.SubmitButtonText
                </button>
            }
            <a asp-controller="BargeEventSearch" asp-action="Index" class="btn btn-secondary">
                <i class="fas fa-times"></i> Cancel
            </a>
            @if (!Model.IsNewEvent && Model.CanModify)
            {
                <button type="button" class="btn btn-danger float-end" data-bs-toggle="modal" data-bs-target="#deleteModal">
                    <i class="fas fa-trash"></i> Void Event
                </button>
            }
        </div>
    </div>
</form>

<!-- Delete Confirmation Modal -->
@if (!Model.IsNewEvent && Model.CanModify)
{
    <div class="modal fade" id="deleteModal" tabindex="-1">
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Confirm Void</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                </div>
                <div class="modal-body">
                    <p>Are you sure you want to void this barge event?</p>
                    <p class="text-danger">This action sets the void status but does not delete the record.</p>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                    <form asp-action="Delete" asp-route-id="@Model.BargeEvent.TicketEventID" method="post" class="d-inline">
                        @Html.AntiForgeryToken()
                        <button type="submit" class="btn btn-danger">Void Event</button>
                    </form>
                </div>
            </div>
        </div>
    </div>
}

@section Scripts {
    <script src="~/js/barge-event-detail.js"></script>
}
```

### _EventDetailsTab.cshtml Partial

```cshtml
@model BargeOpsAdmin.ViewModels.BargeEventEditViewModel

<div class="row mb-3">
    <!-- Event Type -->
    <div class="col-md-3">
        <label asp-for="BargeEvent.EventTypeID" class="form-label"></label>
        <select asp-for="BargeEvent.EventTypeID" asp-items="Model.EventTypeList" class="form-select" required>
            <option value="">-- Select --</option>
        </select>
        <span asp-validation-for="BargeEvent.EventTypeID" class="text-danger"></span>
    </div>

    <!-- Billing Customer -->
    <div class="col-md-3">
        <label asp-for="BargeEvent.BillingCustomerID" class="form-label"></label>
        <select asp-for="BargeEvent.BillingCustomerID" asp-items="Model.CustomerList" class="form-select select2">
            <option value="">-- Select --</option>
        </select>
        <span asp-validation-for="BargeEvent.BillingCustomerID" class="text-danger"></span>
    </div>

    <!-- Fleet Boat -->
    <div class="col-md-3">
        <label asp-for="BargeEvent.FleetBoatID" class="form-label"></label>
        <select asp-for="BargeEvent.FleetBoatID" asp-items="Model.FleetBoatList" class="form-select select2">
            <option value="">-- Select --</option>
        </select>
    </div>
</div>

<div class="row mb-3">
    <!-- Start Date/Time (SPLIT for 24-hour input) -->
    <div class="col-md-3">
        <label asp-for="StartDate" class="form-label">Start Date</label>
        <input asp-for="StartDate" type="date" class="form-control" id="dtStartDate" required />
    </div>
    <div class="col-md-3">
        <label class="form-label">Start Time (24-hour)</label>
        <input asp-for="StartTime" type="time" class="form-control" id="dtStartTime" />
        <small class="form-text text-muted">Military time (HH:MM)</small>
    </div>

    <!-- Complete Date/Time (SPLIT for 24-hour input) -->
    <div class="col-md-3">
        <label asp-for="CompleteDate" class="form-label">Complete Date</label>
        <input asp-for="CompleteDate" type="date" class="form-control" id="dtCompleteDate" />
    </div>
    <div class="col-md-3">
        <label class="form-label">Complete Time (24-hour)</label>
        <input asp-for="CompleteTime" type="time" class="form-control" id="dtCompleteTime" />
    </div>
</div>

<!-- Additional fields... -->
```

---

## JavaScript Files

### barge-event-search.js

**File Location**: `wwwroot/js/barge-event-search.js`

**Key Patterns**:

```javascript
$(document).ready(function() {
    // Initialize Select2 dropdowns
    $('.select2').select2({
        placeholder: '-- Select --',
        allowClear: true,
        width: '100%'
    });

    // Initialize DataTables
    var dataTable = $('#bargeEventTable').DataTable({
        processing: true,
        serverSide: true,
        stateSave: true,
        responsive: true,
        autoWidth: false,
        ajax: {
            url: '/BargeEventSearch/EventTable',
            type: 'POST',
            data: function(d) {
                // Add search criteria to request
                d.eventTypeId = $('#EventTypeId').val();
                d.billingCustomerId = $('#BillingCustomerId').val();
                d.fromLocationId = $('#FromLocationId').val();
                d.toLocationId = $('#ToLocationId').val();
                d.fleetBoatId = $('#FleetBoatId').val();
                d.startDate = $('#StartDate').val();
                d.endDate = $('#EndDate').val();
                d.bargeNumberList = $('#BargeNumberList').val();
                d.ticketCustomerId = $('#TicketCustomerId').val();
                d.freightCustomerId = $('#FreightCustomerId').val();
                d.contractNumber = $('#ContractNumber').val();
                d.eventRateId = $('#EventRateId').val();
                d.includeVoided = $('#IncludeVoided').is(':checked');
            },
            error: function(xhr, error, thrown) {
                console.error('DataTables error:', error, thrown);
                if (xhr.responseJSON && xhr.responseJSON.error) {
                    alert(xhr.responseJSON.error);
                }
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
                    return '<div class="btn-group btn-group-sm" role="group">' +
                           '<a href="/BargeEventDetail/Edit/' + row.ticketEventID + '" class="btn btn-sm btn-primary" title="Edit">' +
                           '<i class="fas fa-edit"></i></a>' +
                           '</div>';
                }
            },
            { data: 'eventName', defaultContent: '' },
            { data: 'bargeNum', defaultContent: '' },
            { data: 'ticketID', defaultContent: '' },
            {
                data: 'startDateTime',
                render: function(data) {
                    return data ? formatDateTime(data) : '';
                }
            },
            {
                data: 'completeDateTime',
                render: function(data) {
                    return data ? formatDateTime(data) : '';
                }
            },
            // Add freight columns conditionally
            { data: 'startLocation', defaultContent: '' },
            { data: 'endLocation', defaultContent: '' },
            { data: 'loadStatus', defaultContent: '' },
            { data: 'commodityName', defaultContent: '' },
            { data: 'customerName', defaultContent: '' },
            { data: 'servicingBoat', defaultContent: '' },
            { data: 'division', defaultContent: '' },
            {
                data: 'isInvoiced',
                render: function(data) {
                    return data ? '<i class="fas fa-check text-success"></i>' : '';
                }
            },
            {
                data: 'rebill',
                render: function(data) {
                    return data ? '<i class="fas fa-check text-warning"></i>' : '';
                }
            },
            {
                data: 'void',
                render: function(data) {
                    return data ? '<i class="fas fa-times text-danger"></i>' : '';
                }
            }
        ],
        order: [[4, 'desc']], // Sort by start date descending
        pageLength: 25,
        lengthMenu: [[10, 25, 50, 100], [10, 25, 50, 100]],
        rowCallback: function(row, data) {
            // Strikethrough voided events
            if (data.void) {
                $(row).addClass('text-decoration-line-through text-muted');
            }
        }
    });

    // Search button click
    $('#btnSearch').click(function() {
        dataTable.ajax.reload();
    });

    // Context menu for rebilling (right-click on rows)
    $('#bargeEventTable tbody').on('contextmenu', 'tr', function(e) {
        e.preventDefault();
        var row = dataTable.row(this);
        var data = row.data();

        // Show context menu with Mark/Unmark Rebill options
        showContextMenu(e.pageX, e.pageY, data);
    });

    // Helper: Format DateTime to MM/dd/yyyy HH:mm (24-hour)
    function formatDateTime(isoString) {
        if (!isoString) return '';
        var date = new Date(isoString);
        return date.toLocaleString('en-US', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
            hour12: false
        });
    }

    // Helper: Show context menu
    function showContextMenu(x, y, data) {
        // Implementation for context menu (Mark/Unmark Rebill)
        // ...
    }
});
```

### barge-event-detail.js

**File Location**: `wwwroot/js/barge-event-detail.js`

**Key Patterns**:

```javascript
$(document).ready(function() {
    // Initialize Select2 dropdowns
    $('.select2').select2({
        placeholder: '-- Select --',
        allowClear: true,
        width: '100%'
    });

    // Initialize validation
    $('#bargeEventForm').validate({
        errorClass: 'text-danger',
        errorElement: 'span',
        highlight: function(element) {
            $(element).closest('.form-control').addClass('is-invalid');
        },
        unhighlight: function(element) {
            $(element).closest('.form-control').removeClass('is-invalid');
        }
    });

    // Tab switch event handling
    $('#eventTabs button[data-bs-toggle="tab"]').on('shown.bs.tab', function(e) {
        var targetTab = $(e.target).attr('data-bs-target');

        if (targetTab === '#barges') {
            loadBarges();
        } else if (targetTab === '#audit') {
            loadBillingAudits();
        }
    });

    // DateTime combination on form submit
    $('#bargeEventForm').on('submit', function() {
        combineDateTimes();
    });

    // Load barges for event
    function loadBarges() {
        var eventId = $('#BargeEvent_TicketEventID').val();
        if (!eventId || eventId == '0') return;

        $.get('/BargeEventDetail/GetBarges/' + eventId, function(response) {
            if (response.success) {
                renderBargeList(response.data);
            } else {
                alert('Error loading barges: ' + response.message);
            }
        });
    }

    // Load billing audits
    function loadBillingAudits() {
        var eventId = $('#BargeEvent_TicketEventID').val();
        if (!eventId || eventId == '0') return;

        $.get('/BargeEventDetail/GetBillingAudits/' + eventId, function(response) {
            if (response.success) {
                renderBillingAudits(response.data);
            } else {
                alert('Error loading billing audits: ' + response.message);
            }
        });
    }

    // Combine split date/time fields before submit
    function combineDateTimes() {
        // Start DateTime
        combineDateTime('dtStartDate', 'dtStartTime', '#BargeEvent_StartDateTime');

        // Complete DateTime
        combineDateTime('dtCompleteDate', 'dtCompleteTime', '#BargeEvent_CompleteDateTime');

        // Add other datetime fields...
    }

    // Helper: Combine date + time into hidden field
    function combineDateTime(dateId, timeId, targetId) {
        var date = $('#' + dateId).val();
        var time = $('#' + timeId).val();

        if (date) {
            var combined = date;
            if (time) {
                combined += 'T' + time + ':00';
            } else {
                combined += 'T00:00:00';
            }
            $(targetId).val(combined);
        } else {
            $(targetId).val('');
        }
    }

    // Helper: Render barge list
    function renderBargeList(barges) {
        // Implementation...
    }

    // Helper: Render billing audits
    function renderBillingAudits(audits) {
        // Implementation...
    }
});
```

---

## Critical Patterns Summary

### ✅ DateTime Handling (24-hour format!)

1. **ViewModel**: Single property
2. **View**: Split into `<input type="date">` + `<input type="time">`
3. **JavaScript**: Combine on form submit

### ✅ Select2 Initialization

```javascript
$('.select2').select2({
    placeholder: '-- Select --',
    allowClear: true,
    width: '100%'
});
```

### ✅ DataTables Server-Side Processing

- `serverSide: true`
- `stateSave: true` (saves state in localStorage)
- Custom `ajax.data` function for search criteria
- `rowCallback` for voided event formatting

### ✅ Permission-Based Rendering

```cshtml
@if (Model.CanModify)
{
    <button type="submit">Save</button>
}
@if (Model.CanViewBilling)
{
    <li class="nav-item">Billing Tab</li>
}
```

### ✅ License-Based Features

```cshtml
@if (Model.IsFreightActive)
{
    <!-- Freight fields -->
}
```

---

## Implementation Checklist

- [ ] Create `Views/BargeEventSearch/Index.cshtml`
- [ ] Create `Views/BargeEventDetail/Edit.cshtml`
- [ ] Create partial views for tabs
- [ ] Create `wwwroot/js/barge-event-search.js`
- [ ] Create `wwwroot/js/barge-event-detail.js`
- [ ] Test DateTime split/combine functionality
- [ ] Test DataTables server-side processing
- [ ] Test Select2 dropdowns
- [ ] Test permission-based rendering
- [ ] Test license-based feature visibility
- [ ] Test context menu for rebilling
- [ ] Test tab switching and lazy loading

---

## Reference Files

**Existing UI Implementations**:
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Views\BoatLocationSearch\Index.cshtml`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Views\BoatLocationSearch\Edit.cshtml`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\wwwroot\js\boatLocationSearch.js`

---

## Support

For complete examples, see:
- BoatLocation implementation (canonical UI reference)
- conversion-plan.md (detailed patterns)
- ui-mapping.json (control mappings)
