# BargeEvent API Templates

## Overview

Complete API layer templates for the BargeEvent entity following the **MONO SHARED** architecture pattern.

---

## Generated Files

### ✅ Repository Layer

**IBargeEventRepository.cs** (Interface)
- Location: `Admin.Infrastructure.Abstractions/IBargeEventRepository.cs`
- Methods:
  - `GetByIdAsync` - Get single event by ID
  - `GetByTicketIdAsync` - Get all events for a ticket
  - `SearchAsync` - Complex search with filtering
  - `BillingSearchAsync` - Billing-specific search
  - `CreateAsync` - Create new event
  - `UpdateAsync` - Update existing event
  - `SetVoidStatusAsync` - Soft delete
  - `MarkForRebillAsync` / `UnmarkForRebillAsync` - Rebilling operations
  - `GetBargesAsync` - Get child barges
  - `GetBillingAuditsAsync` - Get audit trail

**BargeEventRepository.cs** (Implementation)
- Location: `Admin.Infrastructure/Repositories/BargeEventRepository.cs`
- Uses **Dapper** with **direct SQL queries** (NOT stored procedures)
- Returns **DTOs directly** (no mapping needed)
- Dynamic WHERE clause building for search
- Pagination support for DataTables
- SQL injection protection with parameterized queries

---

### ✅ Service Layer

**IBargeEventService.cs** (Interface)
- Location: `Admin.Domain/Services/IBargeEventService.cs`
- Same methods as repository with business logic

**BargeEventService.cs** (Implementation)
- Location: `Admin.Infrastructure/Services/BargeEventService.cs`
- Business validation:
  - Required field validation
  - Search criteria validation (at least one criterion required)
  - Invoiced event protection
  - Rebill flag checks
- Exception handling and logging
- TODO markers for additional business rules

---

### ✅ API Controller

**BargeEventController.cs**
- Location: `Admin.Api/Controllers/BargeEventController.cs`
- RESTful API design
- Swagger/OpenAPI documentation
- Authorization required (`[Authorize]`)

**Endpoints**:

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/bargeevent/{id}` | Get single event |
| GET | `/api/bargeevent/ticket/{ticketId}` | Get events by ticket |
| GET | `/api/bargeevent/{id}/barges` | Get barges for event |
| GET | `/api/bargeevent/{id}/billing-audits` | Get billing audit trail |
| POST | `/api/bargeevent/search` | Search events with criteria |
| POST | `/api/bargeevent/billing-search` | Billing search |
| POST | `/api/bargeevent` | Create new event |
| PUT | `/api/bargeevent/{id}` | Update existing event |
| DELETE | `/api/bargeevent/{id}` | Void (soft delete) event |
| POST | `/api/bargeevent/mark-rebill` | Mark events for rebilling |
| POST | `/api/bargeevent/unmark-rebill` | Unmark events from rebilling |

---

## Implementation Steps

### 1. Copy Shared DTOs First ⭐

```bash
# Copy DTOs from templates/shared/Dto/ to:
C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\Dto\
```

Copy these files:
- BargeEventDto.cs
- BargeEventSearchRequest.cs
- BargeEventSearchDto.cs
- BargeEventBillingDto.cs

### 2. Copy Repository Files

```bash
# Interface
templates/api/Repositories/IBargeEventRepository.cs
→ C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Abstractions\

# Implementation
templates/api/Repositories/BargeEventRepository.cs
→ C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Repositories\
```

### 3. Copy Service Files

```bash
# Interface
templates/api/Services/IBargeEventService.cs
→ C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Domain\Services\

# Implementation
templates/api/Services/BargeEventService.cs
→ C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Services\
```

### 4. Copy Controller

```bash
templates/api/Controllers/BargeEventController.cs
→ C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Api\Controllers\
```

### 5. Register Services in DI Container

**File**: `Program.cs` or `Startup.cs`

```csharp
// Add repository
builder.Services.AddScoped<IBargeEventRepository, BargeEventRepository>();

// Add service
builder.Services.AddScoped<IBargeEventService, BargeEventService>();
```

### 6. Add Connection String

**File**: `appsettings.json`

```json
{
  "ConnectionStrings": {
    "ServiceData": "Server=...;Database=BargeOps;..."
  }
}
```

### 7. Test API Endpoints

Use Swagger UI at: `https://localhost:{port}/swagger`

Or test with curl:
```bash
# Search events
curl -X POST https://localhost:5001/api/bargeevent/search \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "fleetID": 1,
    "eventTypeId": 5,
    "startDate": "2025-01-01",
    "page": 1,
    "pageSize": 25
  }'

# Get single event
curl https://localhost:5001/api/bargeevent/123 \
  -H "Authorization: Bearer {token}"

# Create event
curl -X POST https://localhost:5001/api/bargeevent \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "ticketID": 456,
    "eventTypeID": 5,
    "startDateTime": "2025-01-15T14:30:00"
  }'
```

---

## Key Patterns

### 1. Dapper with Direct SQL (NOT Stored Procedures)

```csharp
var sql = @"
    SELECT
        te.*,
        et.EventName as EventTypeName,
        c.CustomerName as BillingCustomerName
    FROM TicketEvent te
    LEFT JOIN EventType et ON te.EventTypeID = et.EventTypeID
    LEFT JOIN Customer c ON te.BillingCustomerID = c.CustomerID
    WHERE te.TicketEventID = @TicketEventID";

return await connection.QuerySingleOrDefaultAsync<BargeEventDto>(
    sql,
    new { TicketEventID = ticketEventId });
```

### 2. Dynamic WHERE Clause Building

```csharp
var whereConditions = new List<string> { "t.FleetID = @FleetID" };
var parameters = new DynamicParameters();
parameters.Add("FleetID", request.FleetID);

if (request.EventTypeId.HasValue)
{
    whereConditions.Add("te.EventTypeID = @EventTypeId");
    parameters.Add("EventTypeId", request.EventTypeId.Value);
}

var whereClause = "WHERE " + string.Join(" AND ", whereConditions);
```

### 3. Business Validation in Service Layer

```csharp
// Validate at least one search criterion
if (!request.HasAtLeastOneCriterion())
{
    throw new BusinessException(
        "At least one search criterion is required to prevent overly broad queries.");
}

// Validate invoiced events
if (existing.IsInvoiced && !bargeEvent.Rebill)
{
    throw new BusinessException(
        "Cannot modify invoiced event without rebill flag.");
}
```

### 4. RESTful API Design

```csharp
// Create returns 201 Created with Location header
return CreatedAtAction(
    nameof(GetById),
    new { id = result.TicketEventID },
    result);

// Update returns 200 OK with updated entity
return Ok(result);

// Delete returns 204 No Content
return NoContent();
```

---

## Business Rules to Implement

The following business rules are marked with TODO comments in the service layer:

### Create Validation
- [ ] Check if barge is available at start time
- [ ] Validate event type compatibility with ticket
- [ ] Ensure billing customer is specified for billable events
- [ ] Validate location requirements based on event type
- [ ] Validate freight contract requirements (if freight license active)

### Update Validation
- [ ] Prevent changes to completed events without special permission
- [ ] Validate billing changes don't affect finalized invoices
- [ ] Check freight contract consistency
- [ ] Validate draft fields for draft survey events

### Delete Validation
- [ ] Check if voiding this event leaves ticket in invalid state
- [ ] Validate no dependent child records (onboard orders, etc.)
- [ ] Ensure user has permission to void

### Freight License Rules
- [ ] Validate C/P and Release dates are populated
- [ ] Ensure freight customer is specified
- [ ] Validate freight contract coverage
- [ ] Check load/unload sequencing (Load before Unload)

---

## Testing

### Unit Tests

Create tests for:
- Repository CRUD operations
- Service business validation
- Search query building
- Rebilling operations

**Example**:
```csharp
[Fact]
public async Task CreateAsync_ValidEvent_ReturnsCreatedEvent()
{
    // Arrange
    var dto = new BargeEventDto
    {
        TicketID = 1,
        EventTypeID = 5,
        StartDateTime = DateTime.Now
    };

    // Act
    var result = await _service.CreateAsync(dto);

    // Assert
    Assert.NotEqual(0, result.TicketEventID);
}

[Fact]
public async Task CreateAsync_MissingTicketID_ThrowsBusinessException()
{
    // Arrange
    var dto = new BargeEventDto
    {
        EventTypeID = 5,
        StartDateTime = DateTime.Now
    };

    // Act & Assert
    await Assert.ThrowsAsync<BusinessException>(() =>
        _service.CreateAsync(dto));
}
```

### Integration Tests

Test full API flow:
```csharp
[Fact]
public async Task SearchAsync_WithValidCriteria_ReturnsResults()
{
    // Arrange
    var request = new BargeEventSearchRequest
    {
        FleetID = 1,
        EventTypeId = 5,
        Page = 1,
        PageSize = 25
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/bargeevent/search", request);

    // Assert
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<PagedResult<BargeEventSearchDto>>();
    Assert.NotNull(result);
}
```

---

## Performance Considerations

1. **Index Recommendations**:
   ```sql
   CREATE NONCLUSTERED INDEX IX_TicketEvent_FleetID_StartDateTime
   ON TicketEvent (FleetID, StartDateTime)
   INCLUDE (EventTypeID, BillingCustomerID, VoidStatus);

   CREATE NONCLUSTERED INDEX IX_TicketEvent_TicketID
   ON TicketEvent (TicketID);

   CREATE NONCLUSTERED INDEX IX_TicketEvent_BillingCustomerID_StartDateTime
   ON TicketEvent (BillingCustomerID, StartDateTime)
   WHERE VoidStatus = 0;
   ```

2. **Query Optimization**:
   - Use OFFSET/FETCH for pagination (not ROW_NUMBER)
   - Separate total count and filtered count queries
   - Include only necessary columns in SELECT
   - Use LEFT JOIN only when needed

3. **Caching**:
   - Cache lookup data (EventTypes, Customers) for 10 minutes
   - Use distributed cache for multi-instance deployments
   - Consider response caching for frequently-accessed events

---

## Security

### Authorization

All endpoints require authentication (`[Authorize]` attribute).

**TODO**: Add permission-based authorization:
```csharp
[Authorize(Policy = "BargeEventView")]
public async Task<IActionResult> GetById(int id) { }

[Authorize(Policy = "BargeEventModify")]
public async Task<IActionResult> Create([FromBody] BargeEventDto dto) { }

[Authorize(Policy = "BargeEventBillingModify")]
public async Task<IActionResult> MarkForRebill(...) { }
```

### Input Validation

- Model validation with DataAnnotations
- Business validation in service layer
- SQL injection protection with parameterized queries
- XSS protection (automatic with JSON serialization)

---

## Next Steps

1. ✅ **Shared DTOs created**
2. ✅ **API layer completed**
3. ⏳ **UI layer pending** - Would you like me to generate UI templates next?

---

## Support

- See `../../conversion-plan.md` for detailed implementation guide
- Reference `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\` for existing Facility implementation
- Check `.claude/tasks/MONO_SHARED_STRUCTURE.md` for architecture details

---

## Summary

All API templates are ready! The implementation follows these principles:

✅ Uses Shared DTOs (no separate domain models)
✅ Dapper with direct SQL (not stored procedures)
✅ RESTful API design with proper HTTP status codes
✅ Business validation in service layer
✅ Comprehensive Swagger documentation
✅ Exception handling and logging
✅ Ready for dependency injection

**Next**: Generate UI layer (ViewModels, Controllers, Views, JavaScript)?
