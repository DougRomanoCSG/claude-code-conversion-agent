# Barge Entity - Conversion Templates Generated

## Summary

Complete set of conversion templates for migrating the Barge entity from legacy VB.NET to ASP.NET Core MVC with separated API and UI layers using the **BargeOps.Admin.Mono** shared structure.

**Generation Date**: 2025-12-18
**Entity Complexity**: VERY HIGH (66+ form controls, 27 validation rules, 3 related entities)
**Total Templates**: 14 files + 1 comprehensive plan document
**Total Lines of Code**: ~5,000+ lines

---

## Architecture Pattern: Mono Shared Structure ⭐

```
BargeOps.Admin.Mono/
├── src/BargeOps.Shared/          ⭐ SINGLE SOURCE OF TRUTH
│   └── Dto/                       ⭐ DTOs are the ONLY data models
│       ├── BargeDto.cs
│       ├── BargeSearchRequest.cs
│       └── BargeCharterDto.cs
├── src/BargeOps.API/              (API Layer)
└── src/BargeOps.UI/               (UI Layer)
```

**Key Principles**:
- ⭐ DTOs in BargeOps.Shared used by BOTH API and UI
- ⭐ NO separate domain models or AutoMapper
- ⭐ Repositories return DTOs directly
- ⭐ ViewModels CONTAIN DTOs (not duplicate them)

---

## Generated Files

### 1. Conversion Plan Document

**File**: `conversion-plan.md`
**Lines**: 669
**Purpose**: Comprehensive implementation guide

**Contents**:
- Executive summary with complexity assessment
- Architecture overview and data flow diagrams
- Key conversion challenges (Equipment Type logic, Cover Type special logic)
- Template file manifest with complexity ratings
- 7-phase implementation sequence
- Validation strategy (server + client)
- Testing considerations
- Reference patterns from Facility and BoatLocation

---

### 2. Shared DTOs (BargeOps.Shared/Dto/) ⭐ **GENERATE FIRST**

#### BargeDto.cs
- **Lines**: 550
- **Properties**: 66+ (all form fields)
- **Features**:
  - [Sortable]/[Filterable] attributes for DataTables
  - Data annotations for validation
  - Comprehensive XML documentation
  - Read-only ticket information
  - Draft measurements (feet/inches conversions)
  - Position tracking (Tier, FacilityBerth)
  - Damage/repair fields
  - Audit fields

#### BargeSearchRequest.cs
- **Lines**: 180
- **Properties**: 26 search criteria
- **Features**:
  - Basic criteria (BargeNum, HullType, CoverType, etc.)
  - Advanced criteria (Equipment Type, USCG Number, Size Category)
  - Boat/Facility/Ship search filters
  - DataTables paging/sorting parameters

#### BargeCharterDto.cs
- **Lines**: 90
- **Purpose**: Child entity for charter management
- **Properties**: BargeCharterID, BargeID, ChartererCustomerID, StartDate, EndDate, Rate, Notes

---

### 3. API Repository Layer (Admin.Infrastructure/Repositories/)

#### IBargeRepository.cs
- **Lines**: 150
- **Methods**: 12
  - SearchAsync (complex filtering with boat/facility/ship criteria)
  - GetByIdAsync (with all navigation properties)
  - CreateAsync, UpdateAsync, DeleteAsync (soft delete)
  - Charter CRUD methods (GetBargeChartersAsync, CreateCharterAsync, etc.)
  - UpdateLocationAsync (special method for complex location property)
  - BargeNumExistsAsync (duplicate validation)

#### BargeRepository.cs
- **Lines**: 800+
- **Technology**: Dapper with direct SQL queries (NOT stored procedures)
- **Features**:
  - Dynamic WHERE clause building for 26+ search criteria
  - Proper SQL injection protection (parameterized queries)
  - SQL LIKE wildcard escaping
  - Safe ORDER BY clause mapping
  - Returns DTOs directly (no mapping needed)
  - Full CRUD operations for barge and charters
  - Soft delete implementation
  - Left joins for navigation properties

**Key SQL Patterns**:
```csharp
// Dynamic WHERE clause
var whereConditions = new List<string>();
if (!string.IsNullOrWhiteSpace(request.BargeNum))
{
    whereConditions.Add("b.BargeNum LIKE @BargeNum");
    parameters.Add("BargeNum", $"{EscapeSqlLikeWildcards(request.BargeNum)}%");
}

// Safe ORDER BY mapping
var allowedColumns = new Dictionary<string, string>
{
    { "bargeNum", "b.BargeNum" },
    { "hullType", "b.HullType" },
    ...
};
```

---

### 4. API Service Layer (Admin.Domain/Services/ and Admin.Infrastructure/Services/)

#### IBargeService.cs
- **Lines**: 120
- **Methods**: 12 service operations
- **Return Type**: ServiceResult<T> (success/failure with errors)

#### BargeService.cs
- **Lines**: 500+
- **Features**:
  - 27 business validation rules
  - Equipment type conditional logic (fleet-owned vs. customer-owned)
  - Cover type special logic (global settings)
  - Field clearing based on EquipmentType
  - SizeCategory auto-calculation
  - Charter date overlap validation
  - Draft conversion validation
  - Configuration-based feature flags

**Key Validation Rules**:
- BargeNum required
- EquipmentType required
- CustomerID required for non-fleet-owned (unless terminal mode)
- FleetID required for fleet-owned
- SizeCategory required for non-fleet-owned (unless terminal mode)
- Range validations: ExternalLength (0-50000), ExternalWidth (0-20000), Draft (0-99.999)
- Cover Type special logic with conditional requirements
- Charter date range overlap detection

---

### 5. API Controller (Admin.Api/Controllers/)

#### BargeController.cs
- **Lines**: 350
- **Endpoints**: 11 RESTful API endpoints
- **Authorization**: [Authorize] attribute on all endpoints
- **Features**:
  - Proper HTTP status codes (200, 201, 400, 404)
  - Error logging with ILogger
  - Claims-based user name extraction
  - ServiceResult error handling

**API Endpoints**:
```
POST   /api/barge/search                    - Search with paging
GET    /api/barge/{id}                      - Get by ID
POST   /api/barge                           - Create new
PUT    /api/barge/{id}                      - Update existing
DELETE /api/barge/{id}                      - Delete (soft)
GET    /api/barge/{id}/charters             - Get charters
POST   /api/barge/{id}/charters             - Create charter
PUT    /api/barge/{id}/charters/{charterId} - Update charter
DELETE /api/barge/{id}/charters/{charterId} - Delete charter
PUT    /api/barge/{id}/location             - Update location with coords
```

---

### 6. UI ViewModel Layer (BargeOps.UI/ViewModels/)

#### BargeSearchViewModel.cs
- **Lines**: 250
- **Properties**: 26 search criteria + 15 dropdown lists
- **Features**:
  - Search criteria properties
  - SelectListItem collections for all dropdowns
  - Feature flag properties
  - ToSearchRequest() method to convert to DTO
  - Boat/Facility/Ship search type dropdowns

#### BargeEditViewModel.cs
- **Lines**: 350
- **Contains**: ⭐ BargeDto from BargeOps.Shared (no duplication!)
- **Features**:
  - DateTime splitting for UI (Date + Time inputs)
  - Draft conversion (Feet + Inches to Decimal)
  - 17 dropdown SelectListItem collections
  - Feature flag properties
  - Helper properties: IsFleetOwned, IsCustomerOwned, CanEditStatus
  - LoadFromBarge() and SaveToBarge() methods

**DateTime Splitting Pattern**:
```csharp
// Single property in DTO
public DateTime? InServiceDate { get; set; }

// Split into two properties for UI
public DateTime? InServiceDate { get; set; }  // Date input
public string? InServiceTime { get; set; }    // Time input (24-hour)
```

**Draft Conversion Pattern**:
```csharp
// Single decimal in DTO (12.5 = 12 feet 6 inches)
public decimal? Draft { get; set; }

// Split into two properties for UI
public int? DraftFeet { get; set; }   // Feet input
public int? DraftInches { get; set; } // Inches input
```

---

### 7. UI Service Layer (BargeOps.UI/Services/)

#### IBargeService.cs
- **Lines**: 80
- **Methods**: 10 operations
- **Purpose**: HTTP client interface to call API

#### BargeService.cs
- **Lines**: 300
- **Technology**: HttpClient with JSON serialization
- **Features**:
  - POST/GET/PUT/DELETE HTTP methods
  - JSON serialization with case-insensitive options
  - Error logging with ILogger
  - Proper null handling
  - Returns DTOs from API

**HTTP Client Pattern**:
```csharp
var response = await _httpClient.PostAsJsonAsync("api/barge/search", request);
if (response.IsSuccessStatusCode)
{
    return await response.Content.ReadFromJsonAsync<PagedResult<BargeDto>>(_jsonOptions);
}
```

---

### 8. UI Controller (BargeOps.UI/Controllers/)

#### BargeSearchController.cs
- **Lines**: 450
- **Inherits**: AppController (base controller)
- **Authorization**: RequirePermission attributes
- **Actions**: 10 controller actions

**Key Actions**:
- `Index()` - Display search page with dropdowns
- `BargeTable()` - DataTables AJAX endpoint
- `Edit(int? id)` - Display edit page (new or existing)
- `Edit(BargeEditViewModel)` - Save barge (POST)
- `Delete(int id)` - Soft delete barge
- `GetCharters(int bargeId)` - AJAX get charters
- `SaveCharter()` - AJAX save charter
- `DeleteCharter()` - AJAX delete charter

**DataTables Integration**:
```csharp
[HttpPost("BargeTable")]
public async Task<IActionResult> BargeTable(DataTableRequest request, BargeSearchViewModel model)
{
    // Convert ViewModel to SearchRequest DTO
    var searchRequest = model.ToSearchRequest();
    searchRequest.Start = request.Start;
    searchRequest.Length = request.Length;

    var result = await _bargeService.SearchAsync(searchRequest);

    return Json(new {
        draw = result.Draw,
        recordsTotal = result.TotalCount,
        recordsFiltered = result.FilteredCount,
        data = result.Data
    });
}
```

---

## Implementation Sequence

### Phase 1: SHARED DTOs ⭐ **START HERE**
1. Create `BargeDto.cs` in BargeOps.Shared/Dto/
2. Create `BargeSearchRequest.cs`
3. Create `BargeCharterDto.cs`
4. Compile BargeOps.Shared project successfully

### Phase 2: API Infrastructure
1. Create `IBargeRepository.cs` and `BargeRepository.cs`
2. Create `IBargeService.cs` and `BargeService.cs`
3. Test repository methods with unit tests
4. Test service layer with integration tests

### Phase 3: API Controller
1. Create `BargeController.cs`
2. Configure dependency injection
3. Test API endpoints with Swagger

### Phase 4: UI Services
1. Create `IBargeService.cs` and `BargeService.cs` (HTTP client)
2. Configure HttpClient in DI
3. Test API calls from UI service

### Phase 5: UI ViewModels & Controllers
1. Create `BargeSearchViewModel.cs`
2. Create `BargeEditViewModel.cs`
3. Create `BargeSearchController.cs`
4. Wire up DI and routing

### Phase 6: UI Views
1. Create `Index.cshtml` - Search page
2. Create `Edit.cshtml` - Detail form
3. Create `_CharterModal.cshtml` - Charter modal
4. Test rendering with sample data

### Phase 7: JavaScript
1. Create `barge-search.js` - DataTables initialization
2. Create `barge-detail.js` - Form logic
3. Test all client-side interactions

---

## Key Patterns and Best Practices

### 1. DTO Reuse (Mono Shared Structure)
```csharp
// ❌ WRONG: Duplicating properties
public class BargeViewModel
{
    public int BargeID { get; set; }
    public string BargeNum { get; set; }
    // ... 66 more properties
}

// ✅ CORRECT: Contains DTO from Shared
public class BargeEditViewModel
{
    public BargeDto Barge { get; set; } = new(); // From BargeOps.Shared
    // Only UI-specific properties here
}
```

### 2. Repository Returns DTOs Directly
```csharp
// ❌ WRONG: Repository returns domain model, then maps to DTO
public async Task<Barge> GetByIdAsync(int id)
{
    var entity = await _db.Barges.FindAsync(id);
    return _mapper.Map<Barge>(entity);
}

// ✅ CORRECT: Repository returns DTO directly (no mapping)
public async Task<BargeDto> GetByIdAsync(int id)
{
    var sql = "SELECT b.*, cust.Name AS CustomerName ... FROM Barge b ...";
    return await _connection.QuerySingleOrDefaultAsync<BargeDto>(sql, new { BargeID = id });
}
```

### 3. ViewModel Contains DTO
```csharp
// ViewModel just wraps the DTO and adds UI concerns
public class BargeEditViewModel
{
    public BargeDto Barge { get; set; } = new(); // The entity

    // UI-specific concerns
    public DateTime? InServiceDate { get; set; }  // Split from Barge.InServiceDate
    public string? InServiceTime { get; set; }

    public IEnumerable<SelectListItem> Owners { get; set; } // Dropdown
}
```

### 4. SQL Injection Prevention
```csharp
// ❌ WRONG: String interpolation
var sql = $"SELECT * FROM Barge WHERE BargeNum = '{bargeNum}'";

// ✅ CORRECT: Parameterized query
var sql = "SELECT * FROM Barge WHERE BargeNum = @BargeNum";
parameters.Add("BargeNum", bargeNum);
```

### 5. Safe ORDER BY
```csharp
// ❌ WRONG: Direct user input in ORDER BY
var sql = $"ORDER BY {sortColumn} {sortDirection}";

// ✅ CORRECT: Whitelist mapping
var allowedColumns = new Dictionary<string, string> {
    { "bargeNum", "b.BargeNum" }
};
var column = allowedColumns.ContainsKey(sortColumn) ? allowedColumns[sortColumn] : "b.BargeNum";
```

---

## Testing Strategy

### Unit Tests
- Repository methods (mock Dapper connections)
- Service layer business logic
- All 27 validation rules
- Computed field calculations (SizeCategory, Draft conversions)

### Integration Tests
- API endpoints (full request/response)
- Database operations (use test database)
- FluentValidation rules with real DTOs

### UI Tests
- Controller actions return correct ViewModels
- Views render without errors
- JavaScript validation rules
- Equipment type conditional logic
- Cover type special logic

### End-to-End Tests
- Complete search → edit → save workflow
- Equipment type changes affect form correctly
- Charter management (add/edit/delete with validation)
- Location update with coordinates
- Draft conversion calculations

---

## Reference Implementations

### Facility Entity (Canonical API Example)
- **Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\`
- **DTO**: `BargeOps.Shared/Dto/FacilityDto.cs`
- **Repository**: `Admin.Infrastructure/Repositories/FacilityRepository.cs`
- **Controller**: `Admin.Api/Controllers/FacilityController.cs`

### BoatLocation Entity (Canonical UI Example)
- **Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\`
- **ViewModel**: `ViewModels/BoatLocationSearchViewModel.cs`
- **Controller**: `Controllers/BoatLocationSearchController.cs`
- **View**: `Views/BoatLocationSearch/Index.cshtml`
- **JavaScript**: `wwwroot/js/boatLocationSearch.js`

### Crewing Entity (Additional Patterns)
- **Location**: `C:\source\BargeOps.Crewing.API\` and `C:\source\BargeOps.Crewing.UI\`
- **DataTables**: `crewingSearch.js` - canonical DataTables implementation

---

## Next Steps for Implementation

1. **Review Templates**: Read through all generated templates
2. **Customize SQL**: Update SQL queries to match actual database schema
3. **Add Lookup Services**: Implement services to populate dropdown lists
4. **Implement Validation**: Translate all 27 validation rules to FluentValidation
5. **Create Views**: Generate Razor views (Index.cshtml, Edit.cshtml)
6. **Add JavaScript**: Implement client-side logic (DataTables, form validation)
7. **Configure DI**: Register all services in Program.cs
8. **Test API**: Use Swagger to test all endpoints
9. **Test UI**: Manual testing of search and edit workflows
10. **Write Tests**: Unit, integration, and E2E tests

---

## Notes and Considerations

### Equipment Type Logic
Equipment type controls large portions of form behavior:
- **Fleet-owned**: Disables Status, Damage/Repair, Ticket Details sections
- **Customer-owned**: Enables all sections, affects ColorPairID
- **Other**: Enables all sections

### Cover Type Special Logic
Complex conditional requirements based on:
- Global setting: `EnableCoverTypeSpecialLogic`
- CoverType value (not null, not 'OT')
- Operator type (company-operated vs. customer)
- Commodity requirements

### Computed Fields
Auto-calculated fields require special handling:
- **SizeCategory**: From ExternalLength + ExternalWidth
- **Status**: From LocationID
- **LocationDateTime**: Tracks when location was updated
- **Draft conversions**: Feet/inches ↔ decimal

### Child Entity Management
BargeCharters require:
- Separate AJAX endpoints for CRUD
- Modal dialog for add/edit
- Date range overlap validation
- Configuration check for feature visibility

---

## File Checklist

### Shared (BargeOps.Shared/Dto/)
- [x] BargeDto.cs (550 lines)
- [x] BargeSearchRequest.cs (180 lines)
- [x] BargeCharterDto.cs (90 lines)

### API (BargeOps.API/)
- [x] IBargeRepository.cs (150 lines)
- [x] BargeRepository.cs (800+ lines)
- [x] IBargeService.cs (120 lines)
- [x] BargeService.cs (500+ lines)
- [x] BargeController.cs (350 lines)

### UI (BargeOps.UI/)
- [x] BargeSearchViewModel.cs (250 lines)
- [x] BargeEditViewModel.cs (350 lines)
- [x] IBargeService.cs (80 lines)
- [x] BargeService.cs (300 lines)
- [x] BargeSearchController.cs (450 lines)

### Documentation
- [x] conversion-plan.md (669 lines)
- [x] TEMPLATES_GENERATED.md (this file)

### Pending (Not Generated - HTML/JS)
- [ ] Index.cshtml - Search/list view
- [ ] Edit.cshtml - Detail form
- [ ] _CharterModal.cshtml - Charter modal
- [ ] barge-search.js - DataTables initialization
- [ ] barge-detail.js - Form logic

**Total Generated**: 14 files, ~5,000+ lines of C# code

---

## Summary

All major C# templates have been successfully generated for the Barge entity conversion. The templates follow the Mono Shared architecture pattern where DTOs in BargeOps.Shared are the single source of truth used by both API and UI layers.

The remaining work consists of creating Razor views and JavaScript files, which are more straightforward HTML/JS templates that can be based on existing Facility and BoatLocation implementations.

**Estimated Implementation Time**: 10-15 days for complete conversion including testing.
