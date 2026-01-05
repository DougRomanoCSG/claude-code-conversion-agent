# BargeSeries Conversion Templates

This directory contains code templates for converting the BargeSeries entity from the legacy VB.NET WinForms application to the modern ASP.NET Core Mono architecture.

## Directory Structure

```
templates/
├── shared/              ⭐ SHARED PROJECT (Generate these FIRST!)
│   └── Dto/             DTOs are the ONLY data models (no Models/ folder!)
│       ├── BargeSeriesDto.cs
│       ├── BargeSeriesDraftDto.cs
│       └── BargeSeriesSearchRequest.cs
│
├── api/                 API Project Templates
│   ├── Controllers/
│   │   └── BargeSeriesController.cs
│   ├── Repositories/
│   │   ├── IBargeSeriesRepository.cs
│   │   └── BargeSeriesRepository.cs
│   └── Services/
│       ├── IBargeSeriesService.cs
│       └── BargeSeriesService.cs
│
└── ui/                  UI Project Templates
    ├── Controllers/
    │   └── BargeSeriesSearchController.cs
    ├── Services/
    │   ├── IBargeSeriesService.cs
    │   └── BargeSeriesService.cs
    ├── ViewModels/
    │   ├── BargeSeriesSearchViewModel.cs
    │   └── BargeSeriesEditViewModel.cs
    ├── Views/
    │   └── BargeSeriesSearch/
    │       ├── Index.cshtml
    │       └── Edit.cshtml
    └── wwwroot/
        └── js/
            ├── barge-series-search.js
            └── barge-series-detail.js
```

## Implementation Order

### ⭐ CRITICAL: Follow this order exactly!

1. **Shared DTOs (MUST BE FIRST!)**
   - Copy `shared/Dto/*.cs` to `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\Dto\`
   - These are the ONLY data models - no separate Models folder!

2. **API Repository**
   - Copy `api/Repositories/IBargeSeriesRepository.cs` to `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Repositories\`
   - Implement `BargeSeriesRepository.cs` with Dapper queries
   - Create SQL queries (see SQL/ folder in this templates directory)

3. **API Service**
   - Copy `api/Services/IBargeSeriesService.cs` to `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Domain\Services\`
   - Copy `api/Services/BargeSeriesService.cs` to `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Services\`

4. **API Controller**
   - Copy `api/Controllers/BargeSeriesController.cs` to `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Api\Controllers\`

5. **UI API Client**
   - Copy `ui/Services/*.cs` to `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Services\`

6. **UI ViewModels**
   - Copy `ui/ViewModels/*.cs` to `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\ViewModels\`

7. **UI Controller**
   - Copy `ui/Controllers/BargeSeriesSearchController.cs` to `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Controllers\`

8. **UI Views**
   - Copy `ui/Views/BargeSeriesSearch/*.cshtml` to `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Views\BargeSeriesSearch\`

9. **JavaScript Files**
   - Copy `ui/wwwroot/js/*.js` to `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\wwwroot\js\`

## Key Architecture Points

### ⭐ Mono Shared Structure

**CRITICAL: DTOs are the ONLY data models!**

```
BargeOps.Shared.Dto
  ├── BargeSeriesDto.cs          ← Used by BOTH API and UI
  ├── BargeSeriesDraftDto.cs     ← Child entity DTO
  └── BargeSeriesSearchRequest.cs ← Search criteria

NO Models/ folder - DTOs ARE the models!
```

### Data Flow

```
UI (Razor View)
  ↓ uses
ViewModels (contain DTOs from Shared)
  ↓ uses
UI API Client (returns DTOs from Shared)
  ↓ HTTP calls
API Controller (accepts/returns DTOs)
  ↓ uses
Service Layer (uses DTOs)
  ↓ uses
Repository (returns DTOs directly - NO MAPPING!)
  ↓ Dapper queries
Database
```

**Key Points:**
- NO AutoMapper needed - repositories return DTOs directly
- NO separate domain models - DTOs are used throughout
- ViewModels contain DTOs from BargeOps.Shared

### SQL Queries (NOT Stored Procedures!)

This project uses **parameterized SQL queries** instead of stored procedures.

Example:
```csharp
const string sql = @"
    SELECT BargeSeriesID, CustomerID, Name, HullType, CoverType,
           Length, Width, Depth, TonsPerInch, DraftLight, IsActive
    FROM BargeSeries
    WHERE (@Name IS NULL OR Name LIKE @Name + '%')
      AND (@CustomerID IS NULL OR CustomerID = @CustomerID)
      AND (IsActive = @ActiveOnly OR @ActiveOnly = 0)
    ORDER BY Name";

var results = await connection.QueryAsync<BargeSeriesDto>(
    sql,
    new { request.Name, request.CustomerID, request.ActiveOnly });
```

### Soft Delete Pattern

**CRITICAL: BargeSeries uses soft delete via IsActive flag!**

```csharp
// CORRECT - Soft delete via SetActive
await repository.SetActiveAsync(id, false);

// WRONG - Do NOT use hard delete!
// await repository.DeleteAsync(id); ❌
```

## Reference Examples

For implementation patterns, refer to existing entities in the mono repo:

### API References
- **Repository**: `FacilityRepository.cs`, `BoatLocationRepository.cs`
- **Service**: `FacilityService.cs`
- **Controller**: `FacilityController.cs`, `BoatLocationController.cs`

### UI References
- **Controller**: `BoatLocationSearchController.cs`
- **ViewModels**: `BoatLocationSearchViewModel.cs`, `BoatLocationEditViewModel.cs`
- **Views**: `Views/BoatLocationSearch/Index.cshtml`, `Edit.cshtml`
- **JavaScript**: `boatLocationSearch.js`

### Shared References
- **DTOs**: `FacilityDto.cs`, `BoatLocationDto.cs`
- **Attributes**: `[Sortable]`, `[Filterable]` for DataTables integration

## Special Considerations for BargeSeries

### 1. Child Collection Management

BargeSeries has a one-to-many relationship with BargeSeriesDraft:
- Parent: 1 BargeSeries record
- Children: Up to 14 BargeSeriesDraft records (feet 0-13)
- Each child has 12 tonnage columns (Tons00-Tons11 for inches 0-11)

**Transaction Handling:**
```csharp
// Create/Update must handle parent + children in single transaction
using var transaction = connection.BeginTransaction();
try
{
    // 1. Insert/Update parent BargeSeries
    await InsertOrUpdateParent(bargeSeries, transaction);

    // 2. Upsert child draft records
    foreach (var draft in bargeSeries.Drafts)
    {
        await UpsertDraft(draft, transaction);
    }

    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

### 2. Draft Tonnage Grid

The UI displays a 14×12 editable grid for draft tonnage:
- **Rows**: 14 (feet 0-13)
- **Columns**: 12 (inches 0-11)
- **Features**: Inline editing, paste from clipboard, export to Excel

**Recommended Libraries:**
- **AG Grid Community**: Best for Excel-like editing
- **Handsontable**: Best for paste functionality
- **DataTables + jeditable**: Simpler but less feature-rich

### 3. Feet/Inches Conversion

DraftLight is stored as decimal but displayed as feet + inches:

**DTO (computed properties):**
```csharp
public string? DraftLightFeet =>
    DraftLight.HasValue ? ((int)DraftLight.Value).ToString() : null;

public string? DraftLightInches =>
    DraftLight.HasValue
        ? ((int)((DraftLight.Value - (int)DraftLight.Value) * 12)).ToString()
        : null;
```

**ViewModel (input properties):**
```csharp
[Display(Name = "Light Draft (ft)")]
public int? DraftLightFeet { get; set; }

[Display(Name = "Light Draft (in)")]
[Range(0, 11)]
public int? DraftLightInches { get; set; }

// Combine in controller before saving
decimal draftLight = (draftLightFeet ?? 0) + ((draftLightInches ?? 0) / 12m);
```

### 4. Paste from Clipboard

JavaScript implementation for pasting Excel data:

```javascript
document.getElementById('btnPaste').addEventListener('click', async () => {
    try {
        const text = await navigator.clipboard.readText();
        const rows = text.split('\n');

        rows.forEach((row, rowIndex) => {
            if (rowIndex >= 14) return; // Max 14 rows

            const cells = row.split(/\t|,/); // Tab or comma delimited
            cells.forEach((value, colIndex) => {
                if (colIndex >= 12) return; // Max 12 columns

                const numValue = parseInt(value.trim());
                if (!isNaN(numValue) && numValue >= 0) {
                    // Update grid cell
                    grid.setDataAtCell(rowIndex, colIndex, numValue);
                }
            });
        });
    } catch (err) {
        alert('Failed to read clipboard: ' + err.message);
    }
});
```

## Testing Checklist

### Unit Tests
- [ ] BargeSeriesDto validation
- [ ] BargeSeriesDraftDto validation
- [ ] Repository.SearchAsync with filters
- [ ] Repository.GetByIdAsync loads child drafts
- [ ] Repository.CreateAsync saves parent + children
- [ ] Repository.UpdateAsync handles draft changes
- [ ] Repository.SetActiveAsync (soft delete)
- [ ] Service layer business logic

### Integration Tests
- [ ] API POST /api/bargeseries (create)
- [ ] API PUT /api/bargeseries/{id} (update)
- [ ] API DELETE /api/bargeseries/{id} (soft delete)
- [ ] API GET /api/bargeseries/search (DataTables)
- [ ] UI Index page loads
- [ ] UI Edit page loads with drafts
- [ ] UI Edit form submit with draft updates

### Manual Testing
- [ ] Search by series name
- [ ] Filter by customer
- [ ] Filter by hull/cover type
- [ ] Active only checkbox
- [ ] Create new barge series
- [ ] Edit existing barge series
- [ ] Inline edit draft tonnage
- [ ] Paste from Excel (CSV format)
- [ ] Paste from Excel (tab-delimited)
- [ ] Export draft grid to Excel
- [ ] Deactivate barge series
- [ ] Validation error handling

## Common Pitfalls

### ❌ DON'T DO THIS:
1. Creating separate Models in API project (DTOs ARE the models!)
2. Using AutoMapper when repositories return DTOs
3. Hard delete instead of soft delete (entity has IsActive!)
4. Forgetting to load child Drafts in GetById
5. Not using transaction for parent-child saves
6. Using stored procedures instead of SQL queries
7. Duplicating DTOs in API/UI projects

### ✅ DO THIS:
1. Use DTOs from BargeOps.Shared everywhere
2. Repository returns DTOs directly (no mapping)
3. Use SetActiveAsync for soft delete
4. Load Drafts collection in GetById
5. Wrap parent-child saves in transaction
6. Use parameterized SQL queries
7. ViewModels contain DTOs from Shared

## Additional Resources

- **Conversion Plan**: See `conversion-plan.md` for detailed implementation guide
- **Analysis Data**: See `*.json` files in parent directory for extracted legacy data
- **Mono Repo Reference**: `C:\Dev\BargeOps.Admin.Mono\`
- **Architecture Docs**: See `.claude/tasks/MONO_SHARED_STRUCTURE.md`

## Questions or Issues?

Refer to the conversion plan document or existing implementations in the mono repo for guidance. Follow the patterns established by Facility and BoatLocation entities.
