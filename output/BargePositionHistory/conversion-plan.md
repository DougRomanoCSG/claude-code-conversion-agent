# BargePositionHistory Conversion Plan

## Entity Overview

**Legacy Name**: FleetPositionHistory (Business Object) / frmBargePositionHistory (Form)
**Modern Name**: BargePositionHistory
**Type**: Search/Edit Form with Inline Detail Panel
**Complexity**: Medium

### Description
Tracks historical positions of barges within fleet tier configurations. Allows searching, adding, modifying, and removing position records with tier placement (X,Y coordinates) and timestamp tracking. Includes special handling for barges that have left the fleet.

---

## Critical Architecture Notes

⭐ **MONO SHARED STRUCTURE**:
- DTOs and Models are in **BargeOps.Shared** - SINGLE SOURCE OF TRUTH
- **NO separate Models folder** - DTOs ARE the data models
- DTOs used directly by BOTH API and UI
- No AutoMapper needed between API and UI layers

---

## Implementation Order

### Phase 1: Shared Project (FIRST!)
1. Create BargePositionHistoryDto.cs (complete entity DTO)
2. Create BargePositionHistorySearchRequest.cs (search criteria DTO)
3. Create related lookup DTOs (TierGroupDto, TierDto if not existing)

### Phase 2: API Infrastructure
1. Create IBargePositionHistoryRepository.cs interface
2. Create BargePositionHistoryRepository.cs with Dapper + direct SQL
3. Create IBargePositionHistoryService.cs interface
4. Create BargePositionHistoryService.cs implementation
5. Create BargePositionHistoryController.cs (RESTful endpoints)

### Phase 3: UI Layer
1. Create IBargePositionHistoryService.cs (API client interface)
2. Create BargePositionHistoryService.cs (HTTP client)
3. Create ViewModels (SearchViewModel, EditViewModel)
4. Create BargePositionHistoryController.cs (MVC controller)
5. Create Razor views (Index.cshtml, Edit.cshtml)
6. Create JavaScript files (bargePositionHistory-search.js, bargePositionHistory-detail.js)

---

## Data Model

### Primary Entity: FleetPositionHistory

| Property | Type | Required | Notes |
|----------|------|----------|-------|
| FleetPositionHistoryID | int | Yes (PK) | Auto-generated |
| FleetID | int | Yes (FK) | Parent fleet identifier |
| BargeID | int | Yes (FK) | Resolved from BargeNum |
| BargeNum | string | Yes (UI) | Display/input field |
| TierID | int? | No | Disabled when LeftFleet=true |
| TierName | string | No (Display) | From Tier join |
| TierX | short? | No | X coordinate, cleared when LeftFleet=true |
| TierY | short? | No | Y coordinate, cleared when LeftFleet=true |
| TierPos | string | No (Computed) | "(TierX,TierY)" for display |
| PositionStartDateTime | DateTime | Yes | Combined date+time (24-hour) |
| LeftFleet | bool | Yes | Toggles tier field availability |
| CreateDateTime | DateTime | Yes | Auto-generated |
| ModifyDateTime | DateTime? | No | For optimistic concurrency |
| CreateUser | string | Yes | From identity |
| ModifyUser | string | Yes | From identity |

### Search Criteria

| Parameter | Type | Required | Notes |
|-----------|------|----------|-------|
| FleetID | int | Yes | From parent context |
| PositionStartDate | DateTime | Yes | Search by date |
| TierGroupID | int | Yes | Filters available tiers |
| BargeNum | string | No | Optional filter |
| IncludeBlankTierPos | bool | No | Include records without tier positions |

---

## Business Rules

### Validation Rules

1. **PositionStartDateTime Required**
   - Must not be empty
   - Error: "Date/Time is required."

2. **BargeNum Lookup**
   - Must match existing barge record
   - Resolved via Barge.GetBargeID()
   - Error: "Barge number must match an existing barge record."

3. **Tier Coordinates**
   - TierX and TierY must be valid Int16 (-32,768 to 32,767)
   - When TierID provided, coordinates must be within tier boundaries
   - Validated via FleetPositionHistoryCheck stored procedure

4. **LeftFleet Conditional Logic**
   - When LeftFleet = true:
     - TierID, TierX, TierY disabled and cleared
   - When LeftFleet = false:
     - Tier fields enabled and optional

5. **Search Validation**
   - Date and TierGroup both required for search
   - Error: "Date and Tier group are required."

### Data Transformations

1. **DateTime Split/Combine** (CRITICAL!)
   - UI: Separate date and time inputs (24-hour format)
   - Storage: Combined DateTime
   - Format: MM/dd/yyyy HH:mm (military time)

2. **BargeNum → BargeID**
   - Input: BargeNum (string)
   - Lookup: Barge.GetBargeID(BargeNum)
   - Storage: BargeID (int)

3. **TierPos Computation**
   - Computed: "(" + TierX + "," + TierY + ")"
   - Display only: "(5,3)"

---

## UI Patterns

### Search Form (Top Panel)
```
┌─────────────────────────────────────────────┐
│ Search Criteria                             │
├─────────────────────────────────────────────┤
│ [Date*] [Tier Group*] [Barge] [Find][Reset]│
│ □ Include blank tier positions              │
└─────────────────────────────────────────────┘
```

### Results Grid (Middle Panel)
```
┌─────────────────────────────────────────────┐
│ Barge Positions                             │
├─────────────────────────────────────────────┤
│ [+Add][✏️Modify][🗑️Remove] | [📥Export]      │
│                                             │
│ Date/Time    | Barge | Left | Tier | Pos   │
│ 01/15/2025.. | B123  | □    | T1   | (5,3) │
│ 01/15/2025.. | B456  | ☑    |      |       │
└─────────────────────────────────────────────┘
```

### Detail Edit Panel (Bottom Panel)
```
┌─────────────────────────────────────────────┐
│ Detail                                      │
├─────────────────────────────────────────────┤
│ [Date*] [Time*] [Barge*] □ Left Fleet      │
│ [Tier] [Tier X] [Tier Y]                   │
│                        [Submit] [Cancel]    │
└─────────────────────────────────────────────┘
```

---

## API Endpoints

### RESTful API (BargeOps.Admin.API)

```csharp
[ApiKey]
[Route("api/[controller]")]
public class BargePositionHistoryController : ApiControllerBase
{
    GET    /api/BargePositionHistory/search           // Search with criteria
    GET    /api/BargePositionHistory/{id}             // Get by ID
    POST   /api/BargePositionHistory                  // Create new
    PUT    /api/BargePositionHistory/{id}             // Update existing
    DELETE /api/BargePositionHistory/{id}             // Hard delete
    GET    /api/BargePositionHistory/validate-barge   // Validate BargeNum
}
```

---

## Database Access

### SQL Queries (NOT Stored Procedures!)

**Note**: Modern implementation uses parameterized SQL queries, NOT stored procedures.

1. **Search Query** (BargePositionHistory_Search.sql)
   ```sql
   SELECT fph.FleetPositionHistoryID, fph.FleetID, fph.BargeID,
          b.BargeNum, fph.TierID, t.TierName, fph.TierX, fph.TierY,
          CONCAT('(', fph.TierX, ',', fph.TierY, ')') AS TierPos,
          fph.PositionStartDateTime, fph.LeftFleet,
          fph.CreateDateTime, fph.ModifyDateTime,
          fph.CreateUser, fph.ModifyUser
   FROM FleetPositionHistory fph
   INNER JOIN Barge b ON b.BargeID = fph.BargeID
   LEFT JOIN Tier t ON t.TierID = fph.TierID
   WHERE fph.FleetID = @FleetID
     AND CAST(fph.PositionStartDateTime AS DATE) = @PositionStartDate
     AND (@TierGroupID IS NULL OR t.TierGroupID = @TierGroupID)
     AND (@BargeNum IS NULL OR b.BargeNum LIKE '%' + @BargeNum + '%')
     AND (@IncludeBlankTierPos = 1 OR fph.TierID IS NOT NULL)
   ORDER BY fph.PositionStartDateTime ASC
   ```

2. **GetById Query** (BargePositionHistory_GetById.sql)
3. **Insert Query** (BargePositionHistory_Insert.sql)
4. **Update Query** (BargePositionHistory_Update.sql)
5. **Delete Query** (BargePositionHistory_Delete.sql)
6. **Validate Barge Query** (BargePositionHistory_ValidateBarge.sql)

---

## ViewModel Strategy

### SearchViewModel
```csharp
public class BargePositionHistorySearchViewModel
{
    public int FleetID { get; set; }

    [Required, Display(Name = "Date")]
    public DateTime SearchDate { get; set; }

    [Required, Display(Name = "Tier Group")]
    public int TierGroupID { get; set; }

    [Display(Name = "Barge")]
    public string BargeNum { get; set; }

    [Display(Name = "Include blank tier positions")]
    public bool IncludeBlankTierPos { get; set; }

    // Lookup lists
    public IEnumerable<SelectListItem> TierGroups { get; set; }

    // Permission flags
    public bool CanModify { get; set; }
    public bool CanDelete { get; set; }
}
```

### EditViewModel
```csharp
public class BargePositionHistoryEditViewModel
{
    public int FleetPositionHistoryID { get; set; }
    public int FleetID { get; set; }

    [Required, Display(Name = "Position Date/Time")]
    public DateTime PositionStartDateTime { get; set; }

    [Required, Display(Name = "Barge")]
    public string BargeNum { get; set; }

    [Display(Name = "Left Fleet")]
    public bool LeftFleet { get; set; }

    [Display(Name = "Tier")]
    public int? TierID { get; set; }

    [Range(-32768, 32767), Display(Name = "Tier X")]
    public short? TierX { get; set; }

    [Range(-32768, 32767), Display(Name = "Tier Y")]
    public short? TierY { get; set; }

    public DateTime? ModifyDateTime { get; set; }

    // Lookup lists
    public IEnumerable<SelectListItem> Tiers { get; set; }
}
```

---

## Critical Implementation Notes

### 1. DateTime Split/Combine Pattern

**CRITICAL**: All DateTime fields must use separate date and time inputs!

```javascript
// Split on load
function splitDateTime(dateTimeValue, dateFieldId, timeFieldId) {
    if (dateTimeValue) {
        const date = new Date(dateTimeValue);
        if (!isNaN(date.getTime())) {
            $('#' + dateFieldId).val(date.toISOString().split('T')[0]);
            const hours = ('0' + date.getHours()).slice(-2);
            const minutes = ('0' + date.getMinutes()).slice(-2);
            $('#' + timeFieldId).val(hours + ':' + minutes);
        }
    }
}

// Combine on submit
function combineDateTime(dateFieldId, timeFieldId) {
    const date = $('#' + dateFieldId).val();
    const time = $('#' + timeFieldId).val();
    return (date && time) ? date + 'T' + time + ':00' : '';
}
```

### 2. Conditional Field Logic

```javascript
$('#LeftFleet').on('change', function() {
    const isChecked = $(this).is(':checked');
    $('#TierID, #TierX, #TierY').prop('disabled', isChecked);
    if (isChecked) {
        $('#TierID').val('').trigger('change');
        $('#TierX, #TierY').val('');
    }
});
```

### 3. DataTables Configuration

```javascript
const dataTable = $('#bargePositionHistoryTable').DataTable({
    processing: true,
    serverSide: true,
    stateSave: true,
    ajax: {
        url: '/BargePositionHistory/SearchTable',
        type: 'POST',
        data: function(d) {
            d.fleetId = @Model.FleetID;
            d.searchDate = $('#SearchDate').val();
            d.tierGroupId = $('#TierGroupID').val();
            d.bargeNum = $('#BargeNum').val();
            d.includeBlankTierPos = $('#IncludeBlankTierPos').is(':checked');
        }
    },
    columns: [
        { data: null, orderable: false, render: renderActions },
        { data: 'positionStartDateTime', render: formatDateTime },
        { data: 'bargeNum' },
        { data: 'leftFleet', render: renderCheckbox },
        { data: 'tierName', defaultContent: '' },
        { data: 'tierPos', defaultContent: '' }
    ],
    order: [[1, 'asc']]
});
```

---

## File Structure

```
BargeOps.Shared/BargeOps.Shared/
└── Dto/
    ├── BargePositionHistoryDto.cs              ⭐ CREATE FIRST
    ├── BargePositionHistorySearchRequest.cs    ⭐ CREATE FIRST
    └── PagedResult.cs (if not existing)

BargeOps.API/
├── src/Admin.Api/Controllers/
│   └── BargePositionHistoryController.cs
├── src/Admin.Domain/Services/
│   └── IBargePositionHistoryService.cs
├── src/Admin.Infrastructure/
│   ├── Repositories/
│   │   ├── IBargePositionHistoryRepository.cs
│   │   └── BargePositionHistoryRepository.cs
│   ├── Services/
│   │   └── BargePositionHistoryService.cs
│   └── DataAccess/Sql/
│       ├── BargePositionHistory_Search.sql
│       ├── BargePositionHistory_GetById.sql
│       ├── BargePositionHistory_Insert.sql
│       ├── BargePositionHistory_Update.sql
│       ├── BargePositionHistory_Delete.sql
│       └── BargePositionHistory_ValidateBarge.sql

BargeOps.UI/
├── Controllers/
│   └── BargePositionHistoryController.cs
├── Services/
│   ├── IBargePositionHistoryService.cs
│   └── BargePositionHistoryService.cs
├── ViewModels/
│   ├── BargePositionHistorySearchViewModel.cs
│   └── BargePositionHistoryEditViewModel.cs
├── Views/BargePositionHistory/
│   ├── Index.cshtml
│   └── Edit.cshtml
└── wwwroot/js/
    ├── bargePositionHistory-search.js
    └── bargePositionHistory-detail.js
```

---

## Security & Authorization

### API (ApiKey Authentication)
```csharp
[ApiKey]
[Route("api/[controller]")]
public class BargePositionHistoryController : ApiControllerBase
{
    // All endpoints require API key
}
```

### UI (OIDC/Cookie Authentication)
```csharp
[Authorize]
public class BargePositionHistoryController : AppController
{
    [Authorize(Policy = "BargePositionHistoryView")]
    public IActionResult Index(int fleetId) { }

    [Authorize(Policy = "BargePositionHistoryModify")]
    public IActionResult Edit(int id) { }

    [HttpPost]
    [Authorize(Policy = "BargePositionHistoryDelete")]
    public async Task<IActionResult> Delete(int id) { }
}
```

### Required Permissions
- BargePositionHistoryView
- BargePositionHistoryModify
- BargePositionHistoryDelete

---

## Testing Checklist

- [ ] DateTime split/combine works correctly (24-hour format)
- [ ] LeftFleet checkbox disables/enables tier fields
- [ ] LeftFleet=true clears tier fields on save
- [ ] BargeNum lookup validation works
- [ ] Search requires Date and TierGroup
- [ ] Grid sorting, paging, filtering work
- [ ] Add/Modify/Delete operations work
- [ ] Optimistic concurrency with ModifyDateTime works
- [ ] Export functionality works
- [ ] Permissions properly restrict actions
- [ ] State save persists grid settings

---

## Migration Complexity

**Overall: Medium**

### Challenges:
1. ✅ DateTime split/combine pattern (CRITICAL)
2. ✅ Conditional field logic (LeftFleet)
3. ✅ BargeNum lookup validation
4. ✅ Optimistic concurrency
5. ✅ Grid state persistence

### Simple Patterns:
- Standard CRUD operations
- Lookup list loading
- Search form validation

---

## Next Steps

1. **Generate Shared DTOs** (Phase 1)
2. **Generate API layer** (Phase 2)
3. **Generate UI layer** (Phase 3)
4. **Test thoroughly** (Focus on DateTime and LeftFleet logic)

---

*Generated: 2025-01-11*
*Agent: Template Generator*
*Entity: BargePositionHistory*
