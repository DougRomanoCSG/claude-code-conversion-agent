# BargeEvent Conversion Plan

## Executive Summary

**Entity**: BargeEvent (wrapper around TicketEvent table)
**Complexity**: ⭐⭐⭐⭐⭐ Very High
**Priority**: High
**Estimated Effort**: 2-3 weeks
**Dependencies**: Barge, Ticket, Customer, Boat, Location, EventType, Commodity

BargeEvent is a **complex composite entity** that manages multiple barges, tickets, and ticket events with extensive billing operations. It's one of the most complex forms in the Onshore system with:
- Multi-tab interface (5 tabs: Barge Details, Billing, Freight Billing, Demurrage Billing, Billing Audit)
- Multiple child grids (barge list, billing audit)
- Complex business logic for billing calculations
- License-based feature visibility (Freight, Onboard)
- Multiple event types with different validation rules
- Cross-entity integration (Barge, Boat, Ticket, OnboardOrder)

---

## Architecture Overview

### MONO SHARED STRUCTURE ⭐

**CRITICAL**: This project uses a **MONO SHARED** architecture where DTOs and Models reside in a SHARED project:

```
C:\Dev\BargeOps.Admin.Mono\
├── src\
│   ├── BargeOps.Shared\        ⭐ SHARED PROJECT (create DTOs HERE FIRST!)
│   │   └── Dto\                ⭐ DTOs are the ONLY data models
│   │       ├── BargeEventDto.cs
│   │       ├── BargeEventSearchRequest.cs
│   │       ├── BargeEventSearchDto.cs
│   │       └── BargeEventBillingDto.cs
│   ├── BargeOps.API\           (API project - uses Shared DTOs)
│   │   ├── src\Admin.Api\Controllers\
│   │   ├── src\Admin.Infrastructure\Repositories\
│   │   └── src\Admin.Infrastructure\Services\
│   └── BargeOps.UI\            (UI project - uses Shared DTOs)
│       ├── Controllers\
│       ├── ViewModels\         (contain DTOs from Shared)
│       ├── Services\           (API clients returning Shared DTOs)
│       └── Views\
```

**Key Principles**:
1. DTOs in `BargeOps.Shared/Dto` are the SINGLE SOURCE OF TRUTH
2. NO separate domain models - repositories return DTOs directly
3. NO AutoMapper needed - DTOs used by both API and UI
4. ViewModels in UI contain DTOs (not duplicate them)

---

## Database Structure

### Primary Table: TicketEvent

**Note**: BargeEvent is a VB.NET business object wrapper. The actual database entity is **TicketEvent**.

```sql
-- Primary entity table
TicketEvent (
    TicketEventID int PK,
    TicketID int FK,
    EventTypeID int FK,
    Status smallint,
    BillableStatus tinyint,
    VoidStatus tinyint,           -- Soft delete flag
    StartDateTime datetime,
    CompleteDateTime datetime,
    FromLocationID int FK,
    ToLocationID int FK,
    BillingCustomerID int FK,
    ... (100+ columns for billing, freight, etc.)
)
```

### Search Stored Procedure

```sql
BargeEventSearch (
    @FleetID int,
    @EventRateID int NULL,
    @EventTypeID int NULL,
    @BillingCustomerID int NULL,
    @FromLocationID int NULL,
    @ToLocationID int NULL,
    @FleetBoatID int NULL,
    @StartDate datetime NULL,
    @EndDate datetime NULL,
    @IncludeVoided bit,
    @ContractNumber varchar(50) NULL,
    @BargeList varchar(MAX) NULL,
    @BargeLineCustomerID int NULL,
    @FreightCustomerID int NULL
)
```

---

## Implementation Phases

### Phase 1: Create SHARED DTOs (FIRST!) ⭐

**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\Dto\`

These DTOs are used by **BOTH** API and UI - no duplication!

#### 1.1 BargeEventDto.cs
Complete entity DTO with all fields from TicketEvent table.

**Features**:
- `[Sortable]` and `[Filterable]` attributes for ListQuery
- All ~100+ fields from TicketEvent
- Nullable types for optional fields
- DateTime fields (not split - UI handles splitting)

#### 1.2 BargeEventSearchRequest.cs
Search criteria DTO with ListQuery support.

**Fields**:
- FleetID (required)
- EventTypeId (nullable)
- BillingCustomerId (nullable)
- FromLocationId (nullable)
- ToLocationId (nullable)
- FleetBoatId (nullable)
- StartDate / EndDate (nullable)
- IncludeVoided (bool)
- ContractNumber (string)
- BargeNumberList (string - comma-separated)
- TicketCustomerId (nullable)
- FreightCustomerId (nullable)
- Sorting/Paging from ListQuery base

#### 1.3 BargeEventSearchDto.cs
Flattened DTO for search grid results.

**Fields**:
- TicketEventID
- EventName (from EventType lookup)
- BargeNum
- TicketID
- StartDateTime
- CompleteDateTime
- CpDateTime (freight license only)
- ReleaseDateTime (freight license only)
- StartLocation / EndLocation (resolved names)
- LoadStatus
- CommodityName
- LoadUnloadTons (freight license only)
- IsDefaultTons (freight license only)
- CustomerName (billing customer)
- TicketCustomerName
- FreightCustomerName (freight license only)
- ContractNumber (freight license only)
- FreightOrigin / FreightDestination (freight license only)
- ServicingBoat (boat name)
- Division
- Vendor / VendorBusinessUnit
- SchedTime / EventTime
- EventRateID
- IsInvoiced (bool)
- Rebill (bool)
- Void (bool)
- BargeID (for navigation)

#### 1.4 BargeEventBillingDto.cs
DTO for billing search results.

**Fields**:
- TicketEventID
- TicketEventAdjustmentID
- IsReadyToInvoice
- CustomerName
- BargeNum
- SizeCategory
- TicketID
- EventTypeName
- StartDateTime / CompleteDateTime
- FromLocationName / ToLocationName
- LoadStatus
- TicketStatus
- IsAdjustment
- CommodityName
- NotDefault (rate not using defaults)
- RateMissing (no rate found)
- TotalAmount (currency)
- Division
- BaseRate / RateType
- Minimum / MinimumAmount
- FreeHours
- ProrateUnits
- FleetingDays
- BaseAmount
- FuelSurchargeAmount
- HighWaterAmount
- InvoiceNote
- ModifyDateTime / ModifyUser
- GLAccountNum

---

### Phase 2: API Implementation

#### 2.1 Repository Interface
**File**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Abstractions\IBargeEventRepository.cs`

```csharp
public interface IBargeEventRepository
{
    // Read operations
    Task<BargeEventDto?> GetByIdAsync(int ticketEventId, CancellationToken cancellationToken = default);
    Task<IEnumerable<BargeEventDto>> GetByTicketIdAsync(int ticketId, CancellationToken cancellationToken = default);

    // Search operations
    Task<PagedResult<BargeEventSearchDto>> SearchAsync(
        BargeEventSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<BargeEventBillingDto>> BillingSearchAsync(
        BargeEventBillingSearchRequest request,
        CancellationToken cancellationToken = default);

    // Write operations
    Task<int> InsertAsync(BargeEventDto dto, CancellationToken cancellationToken = default);
    Task UpdateAsync(BargeEventDto dto, CancellationToken cancellationToken = default);
    Task SetVoidStatusAsync(int ticketEventId, byte voidStatus, CancellationToken cancellationToken = default);

    // Rebilling operations
    Task MarkForRebillAsync(IEnumerable<int> ticketEventIds, CancellationToken cancellationToken = default);
    Task UnmarkForRebillAsync(IEnumerable<int> ticketEventIds, CancellationToken cancellationToken = default);
}
```

#### 2.2 Repository Implementation
**File**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Repositories\BargeEventRepository.cs`

**Pattern**: Dapper with **embedded SQL files** (NOT stored procedures directly)

```csharp
public class BargeEventRepository : IBargeEventRepository
{
    private readonly IDbConnection _connection;

    public async Task<PagedResult<BargeEventSearchDto>> SearchAsync(
        BargeEventSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var sql = SqlText.GetSqlText("BargeEvent_Search.sql");

        var parameters = new DynamicParameters();
        parameters.Add("@FleetID", request.FleetID);
        parameters.Add("@EventTypeID", request.EventTypeId);
        // ... add all search parameters

        var results = await _connection.QueryAsync<BargeEventSearchDto>(
            sql, parameters, commandTimeout: 120);

        return results.ToPagedResult(request.Page, request.PageSize);
    }
}
```

**SQL Files Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Sql\BargeEvent\`

- `BargeEvent_GetById.sql` - Get single event
- `BargeEvent_GetByTicketId.sql` - Get all events for ticket
- `BargeEvent_Search.sql` - Main search query
- `BargeEvent_BillingSearch.sql` - Billing search
- `BargeEvent_Insert.sql` - Create new event
- `BargeEvent_Update.sql` - Update existing event
- `BargeEvent_SetVoidStatus.sql` - Soft delete

#### 2.3 Service Interface
**File**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Domain\Services\IBargeEventService.cs`

```csharp
public interface IBargeEventService
{
    Task<BargeEventDto?> GetByIdAsync(int ticketEventId, CancellationToken cancellationToken = default);
    Task<PagedResult<BargeEventSearchDto>> SearchAsync(BargeEventSearchRequest request, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(BargeEventDto dto, CancellationToken cancellationToken = default);
    Task UpdateAsync(BargeEventDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(int ticketEventId, CancellationToken cancellationToken = default);
    Task MarkForRebillAsync(IEnumerable<int> ticketEventIds, CancellationToken cancellationToken = default);
    Task UnmarkForRebillAsync(IEnumerable<int> ticketEventIds, CancellationToken cancellationToken = default);
}
```

#### 2.4 Service Implementation
**File**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Services\BargeEventService.cs`

**Pattern**: Service uses repository, adds business logic, validation

```csharp
public class BargeEventService : IBargeEventService
{
    private readonly IBargeEventRepository _repository;
    private readonly ILogger<BargeEventService> _logger;

    public async Task<int> CreateAsync(BargeEventDto dto, CancellationToken cancellationToken = default)
    {
        // Business logic validation
        if (dto.StartDateTime == default)
            throw new BusinessException("Start date/time is required");

        // Call repository
        var id = await _repository.InsertAsync(dto, cancellationToken);

        return id;
    }
}
```

#### 2.5 API Controller
**File**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Api\Controllers\BargeEventController.cs`

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BargeEventController : ControllerBase
{
    private readonly IBargeEventService _service;

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BargeEventDto), 200)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("search")]
    [ProducesResponseType(typeof(PagedResult<BargeEventSearchDto>), 200)]
    public async Task<IActionResult> Search(
        [FromBody] BargeEventSearchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.SearchAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] BargeEventDto dto,
        CancellationToken cancellationToken)
    {
        var id = await _service.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] BargeEventDto dto,
        CancellationToken cancellationToken)
    {
        if (id != dto.TicketEventID)
            return BadRequest();

        await _service.UpdateAsync(dto, cancellationToken);
        return NoContent();
    }

    [HttpPost("mark-rebill")]
    public async Task<IActionResult> MarkForRebill(
        [FromBody] IEnumerable<int> ticketEventIds,
        CancellationToken cancellationToken)
    {
        await _service.MarkForRebillAsync(ticketEventIds, cancellationToken);
        return NoContent();
    }
}
```

---

### Phase 3: UI Implementation

#### 3.1 ViewModels

**File**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\ViewModels\BargeEventSearchViewModel.cs`

```csharp
namespace BargeOpsAdmin.ViewModels;

public class BargeEventSearchViewModel
{
    // Search Criteria (properties match Shared DTO fields)
    public int? EventTypeId { get; set; }
    public SelectList EventTypeList { get; set; } = new SelectList(Enumerable.Empty<object>());

    public int? BillingCustomerId { get; set; }
    public SelectList CustomerList { get; set; } = new SelectList(Enumerable.Empty<object>());

    public int? FromLocationId { get; set; }
    public SelectList FromLocationList { get; set; } = new SelectList(Enumerable.Empty<object>());

    public int? ToLocationId { get; set; }
    public SelectList ToLocationList { get; set; } = new SelectList(Enumerable.Empty<object>());

    public int? FleetBoatId { get; set; }
    public SelectList FleetBoatList { get; set; } = new SelectList(Enumerable.Empty<object>());

    // Date range - SINGLE properties (view splits into date + time)
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public string? BargeNumberList { get; set; }

    public int? TicketCustomerId { get; set; }

    public int? FreightCustomerId { get; set; }
    public SelectList FreightCustomerList { get; set; } = new SelectList(Enumerable.Empty<object>());

    public string? ContractNumber { get; set; }

    public int? EventRateId { get; set; }

    public bool IncludeVoided { get; set; }

    // License flags (control feature visibility)
    public bool IsFreightActive { get; set; }

    // Permission flags
    public bool CanModify { get; set; }
    public bool CanViewBilling { get; set; }
}
```

**File**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\ViewModels\BargeEventEditViewModel.cs`

```csharp
namespace BargeOpsAdmin.ViewModels;

public class BargeEventEditViewModel
{
    // Entity DTO (from Shared!)
    public BargeEventDto BargeEvent { get; set; } = new();

    // Lookup Lists
    public SelectList EventTypeList { get; set; } = new SelectList(Enumerable.Empty<object>());
    public SelectList CustomerList { get; set; } = new SelectList(Enumerable.Empty<object>());
    public SelectList FleetBoatList { get; set; } = new SelectList(Enumerable.Empty<object>());
    public SelectList LocationList { get; set; } = new SelectList(Enumerable.Empty<object>());
    public SelectList CommodityList { get; set; } = new SelectList(Enumerable.Empty<object>());
    public SelectList RiverList { get; set; } = new SelectList(Enumerable.Empty<object>());
    public SelectList LoadStatusList { get; set; } = new SelectList(Enumerable.Empty<object>());
    public SelectList DivisionList { get; set; } = new SelectList(Enumerable.Empty<object>());
    public SelectList VendorList { get; set; } = new SelectList(Enumerable.Empty<object>());

    // Related Data
    public List<BargeDto> Barges { get; set; } = new();
    public List<BillingAuditDto> BillingAudits { get; set; } = new();

    // License flags
    public bool IsFreightActive { get; set; }
    public bool IsOnboardActive { get; set; }

    // Permission flags
    public bool CanModify { get; set; }
    public bool IsReadOnly => !CanModify;
    public bool CanViewBilling { get; set; }
    public bool CanModifyBilling { get; set; }

    // Validation helpers
    public bool AtLeastOneBargeRequired => true;
}
```

#### 3.2 UI Service (API Client)

**File**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Services\IBargeEventService.cs`

```csharp
public interface IBargeEventService
{
    Task<BargeEventDto?> GetByIdAsync(int ticketEventId);
    Task<PagedResult<BargeEventSearchDto>> SearchAsync(BargeEventSearchRequest request);
    Task<int> CreateAsync(BargeEventDto dto);
    Task UpdateAsync(BargeEventDto dto);
    Task DeleteAsync(int ticketEventId);
    Task MarkForRebillAsync(IEnumerable<int> ticketEventIds);
    Task UnmarkForRebillAsync(IEnumerable<int> ticketEventIds);
}
```

**File**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Services\BargeEventService.cs`

```csharp
public class BargeEventService : IBargeEventService
{
    private readonly HttpClient _httpClient;

    public BargeEventService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("BargeOpsApi");
    }

    public async Task<PagedResult<BargeEventSearchDto>> SearchAsync(BargeEventSearchRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/bargeevent/search", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PagedResult<BargeEventSearchDto>>()
            ?? new PagedResult<BargeEventSearchDto>();
    }

    public async Task<BargeEventDto?> GetByIdAsync(int ticketEventId)
    {
        return await _httpClient.GetFromJsonAsync<BargeEventDto>($"api/bargeevent/{ticketEventId}");
    }

    public async Task<int> CreateAsync(BargeEventDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/bargeevent", dto);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<BargeEventDto>();
        return created?.TicketEventID ?? 0;
    }

    public async Task UpdateAsync(BargeEventDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/bargeevent/{dto.TicketEventID}", dto);
        response.EnsureSuccessStatusCode();
    }
}
```

#### 3.3 UI Controller

**File**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Controllers\BargeEventSearchController.cs`

```csharp
[Authorize(AuthenticationSchemes = IdentityConstants.ApplicationScheme)]
public class BargeEventSearchController : AppController
{
    private readonly IBargeEventService _bargeEventService;
    private readonly ILookupService _lookupService;

    public async Task<IActionResult> Index()
    {
        var model = new BargeEventSearchViewModel
        {
            EventTypeList = await _lookupService.GetEventTypeListAsync(),
            CustomerList = await _lookupService.GetCustomerListAsync(),
            FleetBoatList = await _lookupService.GetFleetBoatListAsync(FleetID),
            IsFreightActive = await _lookupService.IsLicenseActiveAsync("Freight"),
            CanModify = User.HasPermission("BargeEventModify"),
            CanViewBilling = User.HasPermission("BargeEventBillingView")
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> EventTable([FromBody] DataTableRequest dtRequest)
    {
        var request = new BargeEventSearchRequest
        {
            FleetID = FleetID,
            EventTypeId = dtRequest.GetFilterValue<int?>("EventTypeId"),
            BillingCustomerId = dtRequest.GetFilterValue<int?>("BillingCustomerId"),
            // ... map all search criteria from dtRequest
            Page = dtRequest.Start / dtRequest.Length + 1,
            PageSize = dtRequest.Length,
            SortBy = dtRequest.GetSortColumn(),
            SortDescending = dtRequest.IsSortDescending()
        };

        var result = await _bargeEventService.SearchAsync(request);

        return Json(new DataTableResponse<BargeEventSearchDto>(
            dtRequest.Draw,
            result.TotalCount,
            result.TotalCount,
            result.Items
        ));
    }

    [HttpPost]
    public async Task<IActionResult> MarkRebill([FromBody] IEnumerable<int> ticketEventIds)
    {
        await _bargeEventService.MarkForRebillAsync(ticketEventIds);
        return Json(new { success = true });
    }
}
```

#### 3.4 Views

**File**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Views\BargeEventSearch\Index.cshtml`

Structure:
- Search criteria card with form inputs
- DataTables grid with context menu for rebilling
- All datetime fields split into date + time inputs (24-hour)
- Conditional visibility for freight-related fields

**File**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Views\BargeEventSearch\Edit.cshtml`

Structure:
- Tabbed interface (Event Details, Billing, Freight Billing, Demurrage, Audit)
- Barge list grid with toolbar
- Billing information panels
- All datetime fields split into date + time inputs (24-hour)
- Permission-based field disabling

#### 3.5 JavaScript

**File**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\wwwroot\js\barge-event-search.js`

Features:
- DataTables initialization with server-side processing
- Select2 initialization for all dropdowns
- DateTime split/combine logic
- Context menu for rebilling operations
- Export functionality

**File**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\wwwroot\js\barge-event-detail.js`

Features:
- Tab switching event handlers
- Barge list grid DataTables
- Dynamic location combo population based on type
- Billing calculation triggers
- Form submission with datetime combination

---

## Key Implementation Patterns

### 1. DateTime Handling (CRITICAL!)

**ViewModel**: Single DateTime property
```csharp
public DateTime? StartDateTime { get; set; }
```

**View**: Split into date + time inputs (24-hour format)
```html
<div class="row">
    <div class="col-md-6">
        <label asp-for="StartDateTime" class="form-label">Start Date</label>
        <input asp-for="StartDateTime" type="date" class="form-control" id="dtStartDate" />
    </div>
    <div class="col-md-6">
        <label class="form-label">Start Time (24-hour)</label>
        <input type="time" class="form-control" id="dtStartTime" />
    </div>
</div>
```

**JavaScript**: Split on load, combine on submit
```javascript
// On page load - split datetime
if (startDateTime) {
    var date = new Date(startDateTime);
    $('#dtStartDate').val(date.toISOString().split('T')[0]);
    var hours = ('0' + date.getHours()).slice(-2);
    var minutes = ('0' + date.getMinutes()).slice(-2);
    $('#dtStartTime').val(hours + ':' + minutes);
}

// On form submit - combine
$('form').on('submit', function() {
    var date = $('#dtStartDate').val();
    var time = $('#dtStartTime').val();
    var combined = (date && time) ? date + 'T' + time + ':00' : '';
    if (combined) $('#dtStartDate').val(combined);
});
```

### 2. Permission-Based Rendering

```csharp
// Controller sets permission flags
model.CanModify = User.HasPermission("BargeEventModify");
model.CanViewBilling = User.HasPermission("BargeEventBillingView");
model.CanModifyBilling = User.HasPermission("BargeEventBillingModify");
```

```html
@* View conditionally renders *@
@if (Model.CanModify)
{
    <button type="submit" class="btn btn-primary">Submit</button>
}
else
{
    <input type="text" class="form-control" asp-for="PropertyName" readonly />
}

@* Hide billing tab if no view permission *@
@if (Model.CanViewBilling)
{
    <li class="nav-item">
        <button class="nav-link" data-bs-toggle="tab" data-bs-target="#billing">Billing</button>
    </li>
}
```

### 3. License-Based Features

```csharp
// Set license flags in ViewModel
model.IsFreightActive = await _lookupService.IsLicenseActiveAsync("Freight");
```

```html
@* Conditional rendering in view *@
@if (Model.IsFreightActive)
{
    <div class="col-md-3">
        <label asp-for="FreightCustomerId" class="form-label">Freight Customer</label>
        <select asp-for="FreightCustomerId" asp-items="Model.FreightCustomerList" class="form-select">
            <option value="">-- Select --</option>
        </select>
    </div>
}
```

### 4. DataTables Server-Side Processing

```javascript
$('#bargeEventTable').DataTable({
    processing: true,
    serverSide: true,
    stateSave: true,
    ajax: {
        url: '/BargeEventSearch/EventTable',
        type: 'POST',
        data: function(d) {
            d.eventTypeId = $('#EventTypeId').val();
            d.customerId = $('#CustomerId').val();
            d.startDate = combineDateTime('dtStartDate', 'dtStartTime');
            d.endDate = combineDateTime('dtEndDate', 'dtEndTime');
            d.includeVoided = $('#IncludeVoided').is(':checked');
        }
    },
    columns: [
        { data: null, render: function(data, type, row) {
            return '<a href="/BargeEventSearch/Edit/' + row.ticketEventID + '" class="btn btn-sm btn-primary">' +
                   '<i class="fas fa-edit"></i></a>';
        }},
        { data: 'eventName' },
        { data: 'bargeNum' },
        { data: 'startDateTime', render: function(data) {
            return data ? new Date(data).toLocaleString('en-US', {
                year: 'numeric', month: '2-digit', day: '2-digit',
                hour: '2-digit', minute: '2-digit', hour12: false
            }) : '';
        }}
    ],
    order: [[3, 'desc']]
});
```

---

## Testing Strategy

### Unit Tests
- Repository tests with in-memory database
- Service validation logic tests
- ViewModel mapping tests

### Integration Tests
- API endpoint tests
- Database query performance tests
- Complex search criteria tests

### UI Tests
- Playwright/Selenium for critical user flows
- DateTime split/combine validation
- Permission-based rendering verification

---

## Migration Checklist

- [ ] Create all Shared DTOs in BargeOps.Shared/Dto
- [ ] Create SQL files for all repository operations
- [ ] Implement Repository interface and class
- [ ] Implement Service interface and class
- [ ] Create API Controller with all endpoints
- [ ] Register services in DI container
- [ ] Create UI ViewModels
- [ ] Implement UI Service (API client)
- [ ] Create UI Controller
- [ ] Create search view with DataTables
- [ ] Create edit view with tabs
- [ ] Implement JavaScript for search page
- [ ] Implement JavaScript for detail page
- [ ] Add permission checks to controllers
- [ ] Add license-based feature visibility
- [ ] Test datetime split/combine functionality
- [ ] Test rebilling operations
- [ ] Test with freight license active/inactive
- [ ] Test billing tab access levels
- [ ] Performance test search with large datasets

---

## Security Considerations

### Permissions

| Operation | Permission Required |
|-----------|-------------------|
| View search results | BargeEventView |
| Create/Edit events | BargeEventModify |
| Delete events (void) | BargeEventModify |
| View billing tabs | BargeEventBillingView |
| Edit billing information | BargeEventBillingModify |
| Mark/Unmark for Rebill | BargeEventModify |
| Export data | BargeEventView |

### Authorization Patterns

```csharp
// API Controller
[Authorize]
[RequirePermission("BargeEventView")]
public class BargeEventController : ControllerBase { }

// UI Controller
[Authorize(AuthenticationSchemes = IdentityConstants.ApplicationScheme)]
public class BargeEventSearchController : AppController
{
    public async Task<IActionResult> Edit(int id)
    {
        var hasModifyPermission = User.HasPermission("BargeEventModify");
        var hasBillingViewPermission = User.HasPermission("BargeEventBillingView");

        var model = new BargeEventEditViewModel
        {
            CanModify = hasModifyPermission,
            IsReadOnly = !hasModifyPermission,
            CanViewBilling = hasBillingViewPermission,
            CanModifyBilling = User.HasPermission("BargeEventBillingModify")
        };

        return View(model);
    }
}
```

---

## Performance Optimizations

1. **Search Query Optimization**
   - Index on TicketEvent(FleetID, StartDateTime, VoidStatus)
   - Index on TicketEvent(BillingCustomerID, StartDateTime)
   - Consider filtered indexes for frequently-used criteria

2. **DataTables Server-Side Processing**
   - Pagination reduces initial load
   - State saving in localStorage reduces server calls
   - Only load visible columns

3. **Lazy Loading Child Collections**
   - Load barge list separately via AJAX
   - Load billing audit on tab switch
   - Defer freight billing data until needed

4. **Caching**
   - Cache lookup lists (EventTypes, Customers) for 10 minutes
   - Cache license status for session duration
   - Use distributed cache for multi-instance deployments

---

## Dependencies

### Required Entities (Must Be Migrated First)
- ✅ Customer (lookup)
- ✅ EventType (lookup)
- ✅ Boat (FleetBoat lookup)
- ⚠️ Location (FromLocation, ToLocation)
- ⚠️ Commodity (lookup)
- ⚠️ River (lookup)
- ⚠️ Vendor (lookup)
- ⚠️ Division (lookup)
- ⚠️ Barge (child grid)
- ⚠️ Ticket (parent entity)

### Related Entities (Can Be Migrated Later)
- OnboardOrder
- BoatDelay
- BoatTraffic
- DeckLogActivity

---

## Next Steps

1. **Generate SHARED DTO templates** (Phase 1)
2. Ask user if they want ViewModels generated
3. Generate API repository/service/controller templates (Phase 2)
4. Generate UI service/controller/view templates (Phase 3)
5. Generate JavaScript templates (Phase 3)
6. Create implementation guide with step-by-step instructions

---

## Reference Files

### Existing Implementations to Reference

**Facility** (C:\Dev\BargeOps.Admin.Mono):
- DTOs: `src\BargeOps.Shared\Dto\FacilityDto.cs`, `FacilitySearchRequest.cs`
- API Controller: `src\BargeOps.API\src\Admin.Api\Controllers\FacilityController.cs`
- API Repository: `src\BargeOps.API\src\Admin.Infrastructure\Repositories\FacilityRepository.cs`
- API Service: `src\BargeOps.API\src\Admin.Infrastructure\Services\FacilityService.cs`

**BoatLocation** (C:\Dev\BargeOps.Admin.Mono):
- DTOs: `src\BargeOps.Shared\Dto\BoatLocationDto.cs`, `BoatLocationSearchRequest.cs`
- UI Controller: `src\BargeOps.UI\Controllers\BoatLocationSearchController.cs`
- UI ViewModels: `src\BargeOps.UI\ViewModels\BoatLocationSearchViewModel.cs`, `BoatLocationEditViewModel.cs`
- UI Views: `src\BargeOps.UI\Views\BoatLocationSearch\Index.cshtml`, `Edit.cshtml`
- JavaScript: `src\BargeOps.UI\wwwroot\js\boatLocationSearch.js`

**Crewing API** (C:\source\BargeOps.Crewing.API):
- Controllers: `src\Crewing.Api\Controllers\CrewingController.cs`
- DTOs: `src\Crewing.Domain\Dto\CrewingDto.cs`, `CrewingSearchRequest.cs`
- Repositories: `src\Crewing.Infrastructure\Repositories\CrewingRepository.cs`

**Crewing UI** (C:\source\BargeOps.Crewing.UI):
- Controllers: `Controllers\CrewingSearchController.cs`
- ViewModels: `Models\CrewingSearchViewModel.cs`, `CrewingEditViewModel.cs`
- Views: `Views\CrewingSearch\Index.cshtml`, `Edit.cshtml`
- JavaScript: `wwwroot\js\crewingSearch.js`

---

## Summary

BargeEvent is the most complex entity in the conversion with:
- 100+ database fields across TicketEvent table
- Multi-tab interface with 5 tabs
- Complex billing calculations
- License-based feature visibility
- Multiple child grids and relationships
- Cross-entity integration (Barge, Boat, Ticket, OnboardOrder)
- Extensive search capabilities with 12+ filter criteria
- Rebilling operations on multiple selected rows
- Separate access levels for operations vs. billing

**Estimated Effort**: 2-3 weeks for complete implementation including testing.

**Priority**: High - Core operational entity used extensively in day-to-day barge operations.
