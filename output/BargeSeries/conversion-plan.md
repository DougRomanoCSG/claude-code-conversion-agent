# BargeSeries Conversion Plan

## Executive Summary

This document outlines the conversion of the **BargeSeries** entity from the legacy VB.NET WinForms application to the modern ASP.NET Core Mono architecture.

### Entity Overview
- **Entity**: BargeSeries
- **Purpose**: Manage standardized barge series with dimensions, draft calculations, and tonnage specifications
- **Complexity**: Medium-High (has child collection with inline editable grid)
- **Child Entities**: BargeSeriesDraft (one-to-many relationship with 14 rows × 12 tonnage columns)
- **Estimated Effort**: 4-5 days

### Key Features
- Search and list barge series with filters (Series Name, Customer, Hull Type, Cover Type, Active status)
- Create, edit, and deactivate barge series
- Manage draft tonnage table (14 feet × 12 inches) with inline editing
- Paste-from-clipboard functionality for bulk data entry
- Export draft tonnage data
- Soft delete via IsActive flag

---

## Architecture Overview

### CRITICAL: Mono Shared Structure

⭐ **This project uses a MONO SHARED structure where DTOs are the ONLY data models!**

```
BargeOps.Shared (SINGLE SOURCE OF TRUTH)
├── Dto/
│   ├── BargeSeriesDto.cs          ← Complete entity DTO (used by API and UI)
│   ├── BargeSeriesDraftDto.cs     ← Child entity DTO
│   └── BargeSeriesSearchRequest.cs ← Search criteria DTO
│
BargeOps.Admin.API
├── Controllers/BargeSeriesController.cs
├── Repositories (Infrastructure/Repositories)
│   ├── IBargeSeriesRepository.cs
│   └── BargeSeriesRepository.cs   ← Returns DTOs directly (no mapping!)
└── Services (Infrastructure/Services)
    ├── IBargeSeriesService.cs
    └── BargeSeriesService.cs      ← Uses DTOs directly

BargeOps.Admin.UI
├── Controllers/BargeSeriesSearchController.cs
├── ViewModels/
│   ├── BargeSeriesSearchViewModel.cs  ← Contains DTOs from Shared
│   └── BargeSeriesEditViewModel.cs    ← Contains DTOs from Shared
├── Services/
│   ├── IBargeSeriesService.cs
│   └── BargeSeriesService.cs          ← API client returns DTOs
└── Views/BargeSeriesSearch/
    ├── Index.cshtml
    └── Edit.cshtml
```

**Key Points:**
- DTOs in BargeOps.Shared are used by BOTH API and UI
- NO separate domain models - DTOs ARE the data models
- Repositories return DTOs directly (no AutoMapper needed!)
- ViewModels contain DTOs from Shared project

---

## Implementation Order

### Phase 1: Shared DTOs (MUST BE FIRST!)
1. ✅ Create `BargeSeriesDto.cs` with [Sortable]/[Filterable] attributes
2. ✅ Create `BargeSeriesDraftDto.cs` with 12 tonnage columns
3. ✅ Create `BargeSeriesSearchRequest.cs` for search criteria

### Phase 2: API Infrastructure
4. ✅ Create `IBargeSeriesRepository.cs` interface
5. ✅ Implement `BargeSeriesRepository.cs` with Dapper (returns DTOs directly)
6. ✅ Create SQL queries (NOT stored procedures):
   - BargeSeries_Search.sql
   - BargeSeries_GetById.sql
   - BargeSeries_Insert.sql
   - BargeSeries_Update.sql
   - BargeSeries_SetActive.sql (soft delete)
   - BargeSeriesDraft_GetBySeriesId.sql
   - BargeSeriesDraft_Upsert.sql (insert/update child records)
7. ✅ Create `IBargeSeriesService.cs` interface
8. ✅ Implement `BargeSeriesService.cs` (uses DTOs from repository)

### Phase 3: API Controller
9. ✅ Create `BargeSeriesController.cs` with RESTful endpoints:
   - GET /api/bargeseries/search (DataTables server-side)
   - GET /api/bargeseries/{id}
   - POST /api/bargeseries
   - PUT /api/bargeseries/{id}
   - DELETE /api/bargeseries/{id} (soft delete via SetActive)
   - GET /api/bargeseries/{id}/drafts
   - POST /api/bargeseries/{id}/drafts (bulk upsert)

### Phase 4: UI Services
10. ✅ Create `IBargeSeriesService.cs` (UI API client)
11. ✅ Implement `BargeSeriesService.cs` (HTTP client to call API)

### Phase 5: UI ViewModels
12. ✅ Create `BargeSeriesSearchViewModel.cs`
13. ✅ Create `BargeSeriesEditViewModel.cs`

### Phase 6: UI Controllers and Views
14. ✅ Create `BargeSeriesSearchController.cs`
15. ✅ Create `Index.cshtml` (search/list view)
16. ✅ Create `Edit.cshtml` (edit form with draft grid)

### Phase 7: JavaScript and UI Logic
17. ✅ Create `barge-series-search.js` (DataTables initialization)
18. ✅ Create `barge-series-detail.js` (inline grid editing, paste, export)

---

## Data Model

### Primary Entity: BargeSeries

| Property | Type | Nullable | Required | Validation | Notes |
|----------|------|----------|----------|------------|-------|
| BargeSeriesID | int | No | Yes (PK) | Identity | Primary key |
| CustomerID | int | No | Yes (FK) | > 0 | Foreign key to Customer |
| CustomerName | string | Yes | No | - | Display only (joined) |
| Name | string(50) | No | Yes | MaxLength(50) | Series name |
| HullType | string(1) | No | Yes | MaxLength(1) | Validation list code |
| CoverType | string(3) | No | Yes | MaxLength(3) | Validation list code |
| Length | decimal? | Yes | Yes | ≥ 0 | Barge length (feet) |
| Width | decimal? | Yes | Yes | ≥ 0 | Barge width (feet) |
| Depth | decimal? | Yes | Yes | ≥ 0 | Barge depth (feet) |
| Dimensions | string | Yes | No (computed) | - | "Length × Width × Depth" |
| TonsPerInch | decimal? | Yes | Yes | ≥ 0 | Tonnage per inch |
| DraftLight | decimal? | Yes | Yes | 0 to 99.999 | Light draft (decimal feet) |
| DraftLightFeet | string | Yes | No (computed) | - | Feet portion (UI display) |
| DraftLightInches | string | Yes | No (computed) | - | Inches portion (UI display) |
| IsActive | bool | No | Yes | - | Soft delete flag (default: true) |
| Drafts | List&lt;BargeSeriesDraftDto&gt; | Yes | No | - | Child collection |

### Child Entity: BargeSeriesDraft

| Property | Type | Nullable | Required | Validation | Notes |
|----------|------|----------|----------|------------|-------|
| BargeSeriesDraftID | int | No | Yes (PK) | Identity | Primary key |
| BargeSeriesID | int | No | Yes (FK) | - | Foreign key to BargeSeries |
| DraftFeet | int? | Yes | Yes | ≥ 0 | Draft in feet (0-13) |
| Tons00 | int? | Yes | No | ≥ 0 | Tonnage at 0 inches |
| Tons01 | int? | Yes | No | ≥ 0 | Tonnage at 1 inch |
| Tons02 | int? | Yes | No | ≥ 0 | Tonnage at 2 inches |
| Tons03 | int? | Yes | No | ≥ 0 | Tonnage at 3 inches |
| Tons04 | int? | Yes | No | ≥ 0 | Tonnage at 4 inches |
| Tons05 | int? | Yes | No | ≥ 0 | Tonnage at 5 inches |
| Tons06 | int? | Yes | No | ≥ 0 | Tonnage at 6 inches |
| Tons07 | int? | Yes | No | ≥ 0 | Tonnage at 7 inches |
| Tons08 | int? | Yes | No | ≥ 0 | Tonnage at 8 inches |
| Tons09 | int? | Yes | No | ≥ 0 | Tonnage at 9 inches |
| Tons10 | int? | Yes | No | ≥ 0 | Tonnage at 10 inches |
| Tons11 | int? | Yes | No | ≥ 0 | Tonnage at 11 inches |

---

## Business Rules

### Parent Entity Validation
1. **CustomerID**: Required (> 0) - "Customer is required"
2. **Name**: Required, MaxLength(50) - "Series is required"
3. **HullType**: Required, MaxLength(1) - "Hull type is required"
4. **CoverType**: Required, MaxLength(3) - "Cover type is required"
5. **Length**: Required, ≥ 0 - "Length is required and must be non-negative"
6. **Width**: Required, ≥ 0 - "Width is required and must be non-negative"
7. **Depth**: Required, ≥ 0 - "Depth is required and must be non-negative"
8. **TonsPerInch**: Required, ≥ 0 - "Tons/inch is required and must be non-negative"
9. **DraftLight**: Required, 0 to 99.999 - "Light draft is required and must be between 0 and 99.999"

### Child Entity Validation
1. **DraftFeet**: Required, ≥ 0 - "Draft feet is required and must be non-negative"
2. **Tons00-Tons11**: Optional, ≥ 0 if specified - "All tonnage values must be non-negative"

### Cross-Entity Validation
- All BargeSeriesDraft tonnage values across the collection must be non-negative (validated at parent level)

---

## User Interface Patterns

### Search Screen (Index.cshtml)

**Layout:**
```
┌──────────────────────────────────────────────────────────────────┐
│ Barge Series Search                                              │
├──────────────────────────────────────────────────────────────────┤
│ [Series: _______________] [Hull Type: ▼] [☑ Active Only]        │
│ [Owner: ▼              ] [Cover Type: ▼]                        │
│                                     [Find] [Reset]               │
├──────────────────────────────────────────────────────────────────┤
│ ┌─────────────────── DataTables Grid ────────────────────────┐  │
│ │ Actions │ Series │ Customer │ Hull │ Cover │ Dims │ Active │  │
│ │ [Edit]  │ ABC-1  │ Customer │ B    │ OPN   │ 195× │   ☑    │  │
│ │ [Edit]  │ XYZ-2  │ Owner    │ H    │ CLS   │ 200× │   ☑    │  │
│ └────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────┘
```

**Search Criteria:**
- Series (text input, partial match)
- Owner (Select2 dropdown, all customers)
- Hull Type (Select2 dropdown, validation list)
- Cover Type (Select2 dropdown, validation list)
- Active Only (checkbox, default: checked)

**Grid Columns:**
1. Actions (Edit button)
2. Series Name ([Sortable], [Filterable])
3. Customer ([Sortable], [Filterable])
4. Hull Type ([Sortable], [Filterable])
5. Cover Type ([Sortable], [Filterable])
6. Dimensions (computed, [Sortable])
7. Light Draft (formatted as feet/inches, [Sortable])
8. TPI (Tons Per Inch, [Sortable])
9. Active (checkbox, [Sortable], [Filterable])

---

### Detail/Edit Screen (Edit.cshtml)

**Layout:**
```
┌──────────────────────────────────────────────────────────────────┐
│ Barge Series Detail                                              │
├──────────────────────────────────────────────────────────────────┤
│ Series: [_______________]  Hull Type: [▼]                        │
│ Owner:  [▼_____________]  Cover Type: [▼]                        │
│ Length: [_____] ft  Width: [_____] ft  Depth: [_____] ft        │
│ Tons/Inch: [_____]  Light Draft: [__] ft [__] in                │
├──────────────────────────────────────────────────────────────────┤
│ Draft Tonnage                           [Paste] [Export]         │
│ ┌────────────────────────────────────────────────────────────┐  │
│ │ Ft │ 0" │ 1" │ 2" │ 3" │ 4" │ 5" │ 6" │ 7" │ 8" │ 9" │10"│11"│  │
│ │  0 │ __ │ __ │ __ │ __ │ __ │ __ │ __ │ __ │ __ │ __ │ __│ __│  │
│ │  1 │ __ │ __ │ __ │ __ │ __ │ __ │ __ │ __ │ __ │ __ │ __│ __│  │
│ │  2 │ __ │ __ │ __ │ __ │ __ │ __ │ __ │ __ │ __ │ __ │ __│ __│  │
│ │... │ .. │ .. │ .. │ .. │ .. │ .. │ .. │ .. │ .. │ .. │ ..│ ..│  │
│ │ 13 │ __ │ __ │ __ │ __ │ __ │ __ │ __ │ __ │ __ │ __ │ __│ __│  │
│ └────────────────────────────────────────────────────────────┘  │
├──────────────────────────────────────────────────────────────────┤
│ ☑ Active                                    [Submit] [Cancel]    │
└──────────────────────────────────────────────────────────────────┘
```

**Form Sections:**
1. **Main Details** (top card)
   - Series (text input)
   - Owner (Select2 dropdown)
   - Hull Type (Select2 dropdown)
   - Cover Type (Select2 dropdown)
   - Length, Width, Depth (decimal inputs)
   - Tons/Inch (decimal input)
   - Light Draft - split into Feet and Inches inputs

2. **Draft Tonnage Grid** (middle card)
   - 14 rows (feet 0-13)
   - 13 columns (ID column hidden + 12 inch columns)
   - Inline editing
   - Paste-from-clipboard functionality
   - Export to Excel functionality

3. **Action Buttons** (bottom)
   - Active checkbox
   - Submit button
   - Cancel button

---

## Special Features

### 1. Draft Tonnage Grid

**Pattern**: Inline editable grid with 14 fixed rows × 12 tonnage columns

**Implementation Options:**
- **Option A**: DataTables with inline editing plugin (jeditable)
- **Option B**: AG Grid Community Edition (Excel-like editing)
- **Option C**: Handsontable (recommended for Excel-like paste functionality)

**Features:**
- Fixed rows (0-13 feet) - DraftFeet column is read-only
- Editable tonnage cells (Tons00-Tons11)
- Tab navigation between cells
- Validation: must be integer or empty
- Null values displayed as empty (not "xxx" like legacy)

### 2. Paste from Clipboard

**Purpose**: Allow users to paste tonnage data from Excel/CSV

**Implementation:**
```javascript
// Using Clipboard API
navigator.clipboard.readText().then(text => {
    // Parse CSV/TSV data
    const rows = text.split('\n');
    rows.forEach((row, rowIndex) => {
        const cells = row.split(/\t|,/); // Tab or comma delimited
        cells.forEach((value, colIndex) => {
            // Validate and insert into grid
            const numValue = parseInt(value.trim());
            if (!isNaN(numValue) && numValue >= 0) {
                grid.setCell(rowIndex, colIndex, numValue);
            }
        });
    });
});
```

**Requirements:**
- Support CSV and tab-delimited formats
- Validate all values as non-negative integers
- Show error summary for invalid values
- Update grid UI after paste

### 3. Export to Excel

**Purpose**: Export draft tonnage grid to Excel

**Implementation:**
```javascript
// Using DataTables buttons extension or custom export
$('#btnExport').click(() => {
    const data = grid.getData();
    // Convert to CSV or use ExcelJS library
    // Trigger download
});
```

### 4. Feet/Inches Conversion

**DraftLight Property:**
- Stored in DB as decimal (e.g., 2.5 feet)
- Displayed in UI as feet + inches inputs
- JavaScript combines on form submit
- Server-side splits for display

**UI Pattern:**
```html
<div class="row">
  <div class="col-md-6">
    <label>Light Draft (ft)</label>
    <input type="number" asp-for="DraftLightFeet" class="form-control" />
  </div>
  <div class="col-md-6">
    <label>Light Draft (in)</label>
    <input type="number" asp-for="DraftLightInches" class="form-control" max="11" />
  </div>
</div>
```

---

## API Endpoints

### Search and List
- **GET** `/api/bargeseries/search`
  - Query params: name, customerId, hullType, coverType, activeOnly, start, length, sortColumn, sortDirection
  - Returns: `PagedResult<BargeSeriesDto>`
  - Used by DataTables server-side processing

### CRUD Operations
- **GET** `/api/bargeseries/{id}`
  - Returns: `BargeSeriesDto` with child Drafts collection

- **POST** `/api/bargeseries`
  - Body: `BargeSeriesDto`
  - Returns: Created `BargeSeriesDto` with new ID

- **PUT** `/api/bargeseries/{id}`
  - Body: `BargeSeriesDto`
  - Returns: Updated `BargeSeriesDto`

- **DELETE** `/api/bargeseries/{id}`
  - Soft delete: Sets IsActive = false
  - Returns: 204 No Content

### Child Collection Management
- **GET** `/api/bargeseries/{id}/drafts`
  - Returns: `List<BargeSeriesDraftDto>`

- **POST** `/api/bargeseries/{id}/drafts`
  - Body: `List<BargeSeriesDraftDto>` (bulk upsert)
  - Updates all 14 rows in single transaction
  - Returns: Updated draft collection

---

## Database Schema

### BargeSeries Table
```sql
CREATE TABLE BargeSeries (
    BargeSeriesID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID INT NOT NULL,
    Name NVARCHAR(50) NOT NULL,
    HullType NCHAR(1) NOT NULL,
    CoverType NVARCHAR(3) NOT NULL,
    Length DECIMAL(18,3) NOT NULL,
    Width DECIMAL(18,3) NOT NULL,
    Depth DECIMAL(18,3) NOT NULL,
    TonsPerInch DECIMAL(18,3) NOT NULL,
    DraftLight DECIMAL(18,3) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_BargeSeries_Customer FOREIGN KEY (CustomerID) REFERENCES Customer(CustomerID)
);
```

### BargeSeriesDraft Table
```sql
CREATE TABLE BargeSeriesDraft (
    BargeSeriesDraftID INT IDENTITY(1,1) PRIMARY KEY,
    BargeSeriesID INT NOT NULL,
    DraftFeet INT NOT NULL,
    Tons00 INT NULL,
    Tons01 INT NULL,
    Tons02 INT NULL,
    Tons03 INT NULL,
    Tons04 INT NULL,
    Tons05 INT NULL,
    Tons06 INT NULL,
    Tons07 INT NULL,
    Tons08 INT NULL,
    Tons09 INT NULL,
    Tons10 INT NULL,
    Tons11 INT NULL,
    CONSTRAINT FK_BargeSeriesDraft_BargeSeries FOREIGN KEY (BargeSeriesID)
        REFERENCES BargeSeries(BargeSeriesID) ON DELETE CASCADE
);
```

---

## Security and Permissions

### Authorization
- **Search/View**: `BargeSeriesView` permission
- **Create**: `BargeSeriesCreate` permission
- **Edit**: `BargeSeriesModify` permission
- **Delete**: `BargeSeriesDelete` permission

### Controller Authorization
```csharp
[Authorize(Policy = "BargeSeriesView")]
public class BargeSeriesController : ControllerBase
{
    [HttpGet("search")]
    public async Task<IActionResult> Search(...) { }

    [HttpPost]
    [Authorize(Policy = "BargeSeriesCreate")]
    public async Task<IActionResult> Create(...) { }

    [HttpPut("{id}")]
    [Authorize(Policy = "BargeSeriesModify")]
    public async Task<IActionResult> Update(...) { }

    [HttpDelete("{id}")]
    [Authorize(Policy = "BargeSeriesDelete")]
    public async Task<IActionResult> Delete(...) { }
}
```

---

## Testing Strategy

### Unit Tests
1. **BargeSeriesDto Validation**
   - Test all required field validations
   - Test range validations (DraftLight, numeric fields)
   - Test max length validations

2. **BargeSeriesRepository**
   - Test Search with various filter combinations
   - Test GetById with child collection loading
   - Test Create with child drafts
   - Test Update with child drafts (add/modify/delete)
   - Test SetActive (soft delete)

3. **BargeSeriesService**
   - Test business logic
   - Test transaction handling for parent-child saves

### Integration Tests
1. **API Endpoints**
   - Test GET /api/bargeseries/search with pagination
   - Test POST /api/bargeseries (create new series with drafts)
   - Test PUT /api/bargeseries/{id} (update with draft changes)
   - Test DELETE /api/bargeseries/{id} (soft delete)

2. **UI Controllers**
   - Test Index action (loads search page)
   - Test Edit GET action (loads entity with drafts)
   - Test Edit POST action (saves entity with drafts)

### Manual Testing Scenarios
1. **Search Functionality**
   - Search by series name (partial match)
   - Filter by customer
   - Filter by hull type
   - Filter by cover type
   - Toggle active only filter
   - Sort by various columns
   - Pagination

2. **CRUD Operations**
   - Create new barge series with draft tonnage
   - Edit existing barge series
   - Update draft tonnage values
   - Deactivate barge series
   - Cancel edit without saving

3. **Draft Tonnage Grid**
   - Inline edit individual cells
   - Paste from Excel (CSV format)
   - Paste from Excel (tab-delimited format)
   - Export to Excel
   - Validate non-negative integers
   - Handle null/empty values

---

## Migration Notes

### Key Differences from Legacy
1. **Soft Delete**: Use IsActive flag instead of hard delete
2. **DTOs as Data Models**: No separate domain models - DTOs are used throughout
3. **SQL Queries**: Use parameterized SQL queries instead of stored procedures
4. **Grid Pattern**: DataTables/AG Grid instead of UltraGrid
5. **Dropdowns**: Select2 instead of UltraCombo
6. **Paste Functionality**: Clipboard API instead of VB clipboard
7. **Validation**: FluentValidation + client-side jQuery Validation instead of ValidatorExtender
8. **No "xxx" Placeholder**: Use empty/null instead of "xxx" string for null tonnage values

### Common Pitfalls to Avoid
1. ❌ Creating separate Models in API project (DTOs ARE the models!)
2. ❌ Using AutoMapper when repositories return DTOs directly
3. ❌ Hard delete instead of soft delete (entity has IsActive)
4. ❌ Forgetting to load child Drafts collection in GetById
5. ❌ Not handling transaction for parent-child saves
6. ❌ Using stored procedures instead of parameterized SQL queries
7. ❌ Not validating all tonnage values as non-negative
8. ❌ Forgetting to validate DraftLight range (0-99.999)

---

## Implementation Checklist

### Shared Project (BargeOps.Shared)
- [ ] Create `BargeSeriesDto.cs` with [Sortable]/[Filterable] attributes
- [ ] Create `BargeSeriesDraftDto.cs` with 12 tonnage columns
- [ ] Create `BargeSeriesSearchRequest.cs`

### API Project (BargeOps.Admin.API)
- [ ] Create `IBargeSeriesRepository.cs` interface
- [ ] Implement `BargeSeriesRepository.cs` with Dapper
- [ ] Create SQL queries (BargeSeries_Search, GetById, Insert, Update, SetActive)
- [ ] Create SQL queries for drafts (GetBySeriesId, Upsert)
- [ ] Create `IBargeSeriesService.cs` interface
- [ ] Implement `BargeSeriesService.cs`
- [ ] Create `BargeSeriesController.cs` with all CRUD endpoints
- [ ] Add authorization policies
- [ ] Write unit tests

### UI Project (BargeOps.Admin.UI)
- [ ] Create `IBargeSeriesService.cs` (API client)
- [ ] Implement `BargeSeriesService.cs` (HTTP client)
- [ ] Create `BargeSeriesSearchViewModel.cs`
- [ ] Create `BargeSeriesEditViewModel.cs`
- [ ] Create `BargeSeriesSearchController.cs`
- [ ] Create `Index.cshtml` (search view)
- [ ] Create `Edit.cshtml` (detail view)
- [ ] Create `barge-series-search.js` (DataTables)
- [ ] Create `barge-series-detail.js` (grid editing, paste, export)
- [ ] Create `barge-series.css` (custom styles)
- [ ] Test all UI functionality

### Database
- [ ] Verify BargeSeries table schema
- [ ] Verify BargeSeriesDraft table schema
- [ ] Add indexes for performance
- [ ] Test cascade delete on FK

### Documentation
- [ ] Update API documentation (Swagger)
- [ ] Create user guide for paste functionality
- [ ] Document permission requirements
- [ ] Create deployment checklist

---

## Success Criteria

✅ **Functional Requirements Met:**
1. Users can search barge series by all criteria
2. Users can create new barge series with draft tonnage
3. Users can edit existing barge series
4. Users can deactivate barge series (soft delete)
5. Users can inline-edit draft tonnage values
6. Users can paste tonnage data from Excel
7. Users can export tonnage data to Excel
8. All validation rules enforced
9. Active/inactive filtering works correctly

✅ **Technical Requirements Met:**
1. DTOs used as data models (no separate domain models)
2. Repositories return DTOs directly
3. API uses parameterized SQL queries (not SPs)
4. Soft delete via IsActive flag
5. Parent-child saves in single transaction
6. Authorization enforced on all endpoints
7. DataTables server-side processing for search
8. Responsive UI (mobile-friendly)

✅ **Quality Standards Met:**
1. Code follows mono architecture patterns
2. Unit tests achieve >80% coverage
3. Integration tests pass
4. UI tested in Chrome, Edge, Firefox
5. Accessibility standards met (WCAG 2.1 AA)
6. Performance acceptable (<2s page load)

---

## Next Steps

After completing this conversion:
1. Review with stakeholders
2. Conduct user acceptance testing
3. Deploy to staging environment
4. Train end users on new UI
5. Monitor for issues post-deployment
6. Gather feedback for improvements

---

**Document Version**: 1.0
**Created**: 2025-12-17
**Last Updated**: 2025-12-17
**Author**: Claude Code Conversion Agent
