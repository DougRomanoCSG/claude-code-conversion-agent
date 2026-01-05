# Facility Conversion Plan

## Executive Summary

This document provides a comprehensive plan for converting the legacy VB.NET Facility management system to modern ASP.NET Core MVC architecture using the BargeOps.Admin.Mono repository structure.

**Entity**: Facility (Location with FacilityLocation properties)
**Complexity**: High - Master-detail form with 2 child collections (Berths, Statuses), conditional field enabling, and multi-tab UI
**Estimated Effort**: 5-7 days

## Architecture Overview

### CRITICAL: Mono Shared Structure

⭐ This project uses a **MONO SHARED** architecture where DTOs and Models live in a separate **BargeOps.Shared** project:

- **BargeOps.Shared**: DTOs are the ONLY data models (no separate Models folder!)
  - DTOs in `Dto/` folder are used by BOTH API and UI
  - No mapping needed between API and UI - they share the same DTOs!

- **BargeOps.Admin.API**: API project with repositories, services, and controllers
  - Repositories return DTOs directly (no AutoMapper needed!)
  - Services work with DTOs from Shared project

- **BargeOps.Admin.UI**: UI project with controllers, views, and ViewModels
  - ViewModels contain DTOs from Shared project
  - Services (API clients) return DTOs

### Implementation Order

1. ✅ **Shared DTOs** (CREATE FIRST - these are the foundation!)
2. API Infrastructure (Repositories, Services)
3. API Controller
4. UI Services (API clients)
5. UI ViewModels
6. UI Controllers and Views
7. JavaScript and client-side logic

## Data Model

### Entity Relationships

```
Location (Parent)
├── FacilityLocation (Property Object - 1:1)
├── FacilityBerths (Child Collection - 1:Many)
├── FacilityStatuses (Child Collection - 1:Many)
└── Alert (Property Object - 1:1, lazy-loaded, optional)
```

### Primary Entities

#### Location (Parent Table)
- **Primary Key**: LocationID (int)
- **Key Fields**:
  - Name (string, required, max 100) - Facility name
  - ShortName (string, max 50)
  - Note (string, max 255)
  - IsActive (boolean)
  - River (string, max 3)
  - Mile (decimal)

#### FacilityLocation (Property Object - same LocationID)
- **Primary Key**: LocationID (int)
- **Key Fields**:
  - BargeExCode (string, max 10)
  - Bank (string, max 50, validation list: RiverBank)
  - BargeExLocationType (string, max 20, validation list: BargeExLocationType)
  - Lock/Gauge fields (conditionally enabled):
    - LockUsaceName (string, max 50)
    - LockFloodStage (decimal)
    - LockPoolStage (decimal)
    - LockLowWater (decimal)
    - LockNormalCurrent (decimal)
    - LockHighFlow (decimal)
    - LockHighWater (decimal)
    - LockCatastrophicLevel (decimal)
  - NDC Data fields (read-only):
    - NdcName, NdcLocationDescription, NdcAddress
    - NdcCounty, NdcCountyFips, NdcTown, NdcState
    - NdcWaterway, NdcPort
    - NdcLatitude, NdcLongitude
    - NdcOperator, NdcOwner, NdcPurpose, NdcRemark

#### FacilityBerth (Child Collection)
- **Primary Key**: FacilityBerthID (int)
- **Foreign Key**: LocationID (int)
- **Key Fields**:
  - Name (string, required, max 50) - Berth name
  - ShipID (int, nullable)
  - ShipName (string, read-only, calculated) - Current ship at berth

#### FacilityStatus (Child Collection)
- **Primary Key**: FacilityStatusID (int)
- **Foreign Key**: LocationID (int)
- **Key Fields**:
  - StartDateTime (DateTime, required)
  - EndDateTime (DateTime, nullable)
  - Status (string, required, max 20, validation list: FacilityStatus)
  - Note (string, max 4000)

## Business Rules

### Validation Rules

#### Location Entity
1. **Name**: Required, max 100 characters
2. **ShortName**: Max 50 characters
3. **Note**: Max 255 characters
4. **Mile**: Must be <= 2000.0
5. **River/Mile**: Cannot have River without Mile, or vice versa
6. **Mile Range**: Must be between MinMile and MaxMile for selected river

#### FacilityLocation Entity
1. **Conditional Field Validation**: Lock/Gauge fields must be blank if BargeExLocationType is not 'Lock' or 'Gauge Location'
   - Applies to: LockUsaceName, LockFloodStage, LockPoolStage, LockLowWater, LockNormalCurrent, LockHighFlow, LockHighWater, LockCatastrophicLevel
2. **BargeExCode**: Max 10 characters
3. **Bank**: Max 50 characters
4. **BargeExLocationType**: Max 20 characters

#### FacilityBerth Entity
1. **Name**: Required, max 50 characters

#### FacilityStatus Entity
1. **StartDateTime**: Required
2. **Status**: Required, max 20 characters
3. **Note**: Max 4000 characters
4. **EndDateTime**: Must be >= StartDateTime (if provided)

### Conditional Logic

#### Lock/Gauge Field Enabling
- **Trigger**: BargeExLocationType dropdown change
- **Condition**: Value = 'Lock' OR 'Gauge Location'
- **Action**:
  - If TRUE: Enable lock/gauge fields
  - If FALSE: Disable and clear lock/gauge fields

## User Interface Specifications

### Search Form (Index)

#### Search Criteria
- Name (text input)
- ShortName (text input)
- River (dropdown)
- BargeExLocationType (dropdown)
- IsActive (checkbox, default: true)

#### Search Results Grid (DataTables)
Columns:
1. Name (sortable, filterable)
2. ShortName (sortable, filterable)
3. River (sortable, filterable)
4. Mile (sortable)
5. BargeExLocationType (sortable, filterable)
6. IsActive (sortable, filterable)
7. Actions (Edit button)

Features:
- Server-side paging
- Column sorting
- Column filtering
- Export to Excel

### Edit Form

#### Tab Structure

**Tab 1: Details**
- Facility Name (required)
- Short Name
- River (dropdown)
- Mile (decimal)
- BargeEx Code
- Facility Type (dropdown) - triggers lock/gauge field visibility
- Bank (dropdown)
- Location Note (multi-line)
- Is Active (checkbox)

**Group: Lock/Gauge Location** (conditionally visible)
- USACE Name
- Flood Stage
- Pool Stage
- Low Water
- Normal Current
- High Flow
- High Water
- Catastrophic Level

**Tab 2: Status**
- DataGrid with columns: Start Date/Time, End Date/Time, Status, Note
- Toolbar: Add, Modify, Remove, Export
- Inline edit section (appears when Add/Modify clicked):
  - Start Date (required)
  - Start Time (dropdown)
  - Status (dropdown, required)
  - Note (textarea with modal editor, max 4000)
  - End Date
  - End Time (dropdown)
  - Save/Cancel buttons

**Tab 3: Berths**
- DataGrid with columns: Berth Name, Current Ship (read-only)
- Toolbar: Add, Modify, Remove
- Inline edit section (appears when Add/Modify clicked):
  - Berth Name (required)
  - Current Ship (read-only display)
  - Save/Cancel buttons

**Tab 4: NDC Data** (read-only)
- NDC Name
- Location Description
- Address information (Address, Town, County, State, County FIPS)
- Geographic info (Waterway, Port, Latitude, Longitude)
- Operator/Owner information
- Purpose and Remarks

#### Form Behaviors

1. **Tab Lazy Loading**: Berths and Status grids load data only when first visited
2. **Nested Editing**: When editing a child item (Berth or Status):
   - Main form tabs become disabled
   - Main form submit button disabled
   - Child edit section becomes visible and enabled
   - Child save/cancel buttons enabled
3. **Dirty Tracking**: Warn user if they navigate away with unsaved changes
4. **Validation Display**: Show validation errors inline and in summary

## Security & Authorization

### Functional Area
- **FunctionalArea**: frmFacilitySearch (inherited from VB.NET SubSystem)

### Access Levels
1. **None**: No access to facility management
2. **ReadOnly**: Can view facilities but cannot save changes
   - Allowed: View data, navigate tabs, cancel operations
   - Denied: Submit button, Add/Modify/Remove child items
3. **Modify**: Full access to create, edit, and delete facilities
   - Allowed: All operations

### Authorization Policy
- Read operations: `[Authorize(Policy = "FacilityRead")]`
- Modify operations: `[Authorize(Policy = "FacilityModify")]`

## API Endpoints

### Facility Controller

```
GET    /api/Facility/search                  - Search facilities (DataTables)
GET    /api/Facility/{id}                    - Get facility by ID
POST   /api/Facility                         - Create facility
PUT    /api/Facility/{id}                    - Update facility
DELETE /api/Facility/{id}                    - Delete facility
GET    /api/Facility/{id}/berths             - Get facility berths
GET    /api/Facility/{id}/statuses           - Get facility statuses
```

### FacilityBerth Controller (Child Operations)

```
POST   /api/FacilityBerth                    - Create berth
PUT    /api/FacilityBerth/{id}               - Update berth
DELETE /api/FacilityBerth/{id}               - Delete berth
```

### FacilityStatus Controller (Child Operations)

```
POST   /api/FacilityStatus                   - Create status
PUT    /api/FacilityStatus/{id}              - Update status
DELETE /api/FacilityStatus/{id}              - Delete status
GET    /api/FacilityStatus/export            - Export status grid
```

## Data Access Patterns

### Repository Layer

#### SQL Query Patterns (NOT stored procedures)

```sql
-- Search facilities (for DataTables)
SELECT
    l.LocationID,
    l.Name,
    l.ShortName,
    l.River,
    l.Mile,
    l.IsActive,
    fl.BargeExCode,
    fl.BargeExLocationType,
    fl.Bank
FROM Location l
INNER JOIN FacilityLocation fl ON l.LocationID = fl.LocationID
WHERE (@Name IS NULL OR l.Name LIKE '%' + @Name + '%')
  AND (@River IS NULL OR l.River = @River)
  AND (@IsActive IS NULL OR l.IsActive = @IsActive)
ORDER BY l.Name
OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY

-- Get facility by ID (complete with all properties)
SELECT
    l.*,
    fl.*
FROM Location l
INNER JOIN FacilityLocation fl ON l.LocationID = fl.LocationID
WHERE l.LocationID = @LocationID

-- Get facility berths
SELECT
    fb.FacilityBerthID,
    fb.LocationID,
    fb.Name,
    fb.ShipID,
    s.Name AS ShipName
FROM FacilityBerth fb
LEFT JOIN Ship s ON fb.ShipID = s.ShipID
WHERE fb.LocationID = @LocationID
ORDER BY fb.Name

-- Get facility statuses
SELECT
    fs.FacilityStatusID,
    fs.LocationID,
    fs.StartDateTime,
    fs.EndDateTime,
    fs.Status,
    fs.Note
FROM FacilityStatus fs
WHERE fs.LocationID = @LocationID
ORDER BY fs.StartDateTime DESC
```

### Repository Methods

```csharp
Task<PagedResult<FacilityDto>> SearchAsync(FacilitySearchRequest request);
Task<FacilityDto> GetByIdAsync(int id);
Task<FacilityDto> CreateAsync(FacilityDto facility);
Task<FacilityDto> UpdateAsync(FacilityDto facility);
Task DeleteAsync(int id);
Task<IEnumerable<FacilityBerthDto>> GetBerthsAsync(int facilityId);
Task<IEnumerable<FacilityStatusDto>> GetStatusesAsync(int facilityId);
```

## Technology Stack

### Backend
- **Framework**: ASP.NET Core 8.0 MVC
- **Data Access**: Dapper (direct SQL queries, NOT stored procedures)
- **Validation**: Data Annotations + FluentValidation
- **Authentication**: ASP.NET Core Identity (IdentityConstants.ApplicationScheme)
- **Authorization**: Policy-based authorization

### Frontend
- **UI Framework**: ASP.NET Core MVC (Razor views)
- **CSS Framework**: Bootstrap 5
- **JavaScript**: jQuery
- **DataTables**: jQuery DataTables with server-side processing
- **Validation**: jQuery Unobtrusive Validation
- **Icons**: Bootstrap Icons

## Implementation Steps

### Phase 1: Shared DTOs (Day 1)
⭐ **CREATE THESE FIRST** - They are the foundation!

1. Create `BargeOps.Shared/Dto/FacilityDto.cs`
   - Complete entity DTO with all Location and FacilityLocation properties
   - Add `[Sortable]` and `[Filterable]` attributes for DataTables
   - Used directly by both API and UI (no mapping!)

2. Create `BargeOps.Shared/Dto/FacilitySearchRequest.cs`
   - Search criteria DTO

3. Create `BargeOps.Shared/Dto/FacilityBerthDto.cs`
   - Child entity DTO

4. Create `BargeOps.Shared/Dto/FacilityStatusDto.cs`
   - Child entity DTO

### Phase 2: API Infrastructure (Days 1-2)

1. Create `Admin.Infrastructure/Repositories/IFacilityRepository.cs`
   - Define repository interface returning DTOs

2. Create `Admin.Infrastructure/Repositories/FacilityRepository.cs`
   - Implement repository with Dapper
   - Use DIRECT SQL QUERIES (not stored procedures)
   - Return DTOs directly from queries

3. Create `Admin.Domain/Services/IFacilityService.cs`
   - Define service interface

4. Create `Admin.Infrastructure/Services/FacilityService.cs`
   - Implement service using repository
   - Add business rule validation
   - Work with DTOs (no mapping!)

### Phase 3: API Controller (Day 2)

1. Create `Admin.Api/Controllers/FacilityController.cs`
   - Implement RESTful endpoints
   - Add authorization attributes
   - Accept and return DTOs

2. Create `Admin.Api/Controllers/FacilityBerthController.cs`
   - Child entity API endpoints

3. Create `Admin.Api/Controllers/FacilityStatusController.cs`
   - Child entity API endpoints

### Phase 4: UI Services (Day 3)

1. Create `BargeOps.UI/Services/IFacilityService.cs`
   - Define API client interface

2. Create `BargeOps.UI/Services/FacilityService.cs`
   - Implement HTTP client to call API
   - Return DTOs from Shared project

### Phase 5: UI ViewModels (Day 3)

1. Create `BargeOps.UI/ViewModels/FacilitySearchViewModel.cs`
   - Contains search criteria properties
   - Contains DTOs for search results

2. Create `BargeOps.UI/ViewModels/FacilityEditViewModel.cs`
   - Contains FacilityDto (from Shared project)
   - Contains lookup lists as IEnumerable<SelectListItem>
   - Contains collections of child DTOs

### Phase 6: UI Controllers (Day 4)

1. Create `BargeOps.UI/Controllers/FacilityController.cs`
   - Implement MVC actions (Index, Edit, Create)
   - Use ViewModels which contain DTOs
   - Add authorization attributes

### Phase 7: Razor Views (Days 4-5)

1. Create `BargeOps.UI/Views/Facility/Index.cshtml`
   - Search form
   - DataTables grid
   - Model: FacilitySearchViewModel

2. Create `BargeOps.UI/Views/Facility/Edit.cshtml`
   - Tab-based layout
   - Details tab with main fields
   - Berths tab with grid and inline edit
   - Status tab with grid and inline edit
   - NDC Data tab (read-only)
   - Model: FacilityEditViewModel

3. Create partial views for child editing:
   - `_BerthEditPartial.cshtml`
   - `_StatusEditPartial.cshtml`

### Phase 8: JavaScript (Days 5-6)

1. Create `wwwroot/js/facility-search.js`
   - DataTables initialization
   - Search form submission
   - Export functionality

2. Create `wwwroot/js/facility-detail.js`
   - Tab lazy loading
   - Conditional field enabling (Lock/Gauge fields)
   - Child collection management (Add/Edit/Delete)
   - Dirty tracking and navigation warnings
   - Modal dialogs for text editing

### Phase 9: Testing & Refinement (Days 6-7)

1. Unit tests for repositories and services
2. Integration tests for API endpoints
3. UI testing (manual and automated)
4. Security testing (authorization)
5. Performance testing (DataTables with large datasets)

## Reference Files

### Existing Implementations to Reference

⭐ **Primary References** (Admin.Mono - Canonical Patterns):

**Shared DTOs**:
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\Dto\FacilityDto.cs`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\Dto\BoatLocationDto.cs`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\Dto\LookupItemDto.cs`

**API**:
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Api\Controllers\FacilityController.cs`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Api\Controllers\BoatLocationController.cs`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Repositories\FacilityRepository.cs`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Services\FacilityService.cs`

**UI**:
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Controllers\BoatLocationSearchController.cs`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\ViewModels\BoatLocationSearchViewModel.cs`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\ViewModels\BoatLocationEditViewModel.cs`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Views\BoatLocationSearch\Index.cshtml`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\wwwroot\js\boatLocationSearch.js`

**Secondary References** (Crewing - Additional Patterns):
- `C:\source\BargeOps.Crewing.API\src\Crewing.Api\Controllers\CrewingController.cs`
- `C:\source\BargeOps.Crewing.UI\Controllers\CrewingSearchController.cs`
- `C:\source\BargeOps.Crewing.UI\wwwroot\js\crewingSearch.js`

## Risks & Mitigation

### Risk 1: Complex Conditional Logic (Lock/Gauge Fields)
**Impact**: High
**Mitigation**:
- Implement server-side validation for conditional rules
- Use JavaScript to enable/disable fields in real-time
- Clear unused fields before save

### Risk 2: Child Collection Management
**Impact**: Medium
**Mitigation**:
- Use established patterns from BoatLocation (existing implementation)
- Implement proper state management for nested editing
- Add comprehensive validation

### Risk 3: Tab Lazy Loading
**Impact**: Low
**Mitigation**:
- Follow Bootstrap tab shown.bs.tab event pattern
- Cache loaded tab data to prevent redundant API calls

### Risk 4: DateTime Handling (Status dates)
**Impact**: Medium
**Mitigation**:
- Split DateTime into separate date and time inputs
- Combine on client-side before submit
- Use consistent UTC/local time handling

## Success Criteria

- [ ] All CRUD operations working for Facility
- [ ] Child collections (Berths, Statuses) fully functional
- [ ] Conditional field enabling working correctly
- [ ] DataTables search with server-side paging
- [ ] All validation rules enforced
- [ ] Authorization working correctly
- [ ] No security vulnerabilities (XSS, SQL injection, etc.)
- [ ] Export functionality working
- [ ] Responsive UI (mobile-friendly)
- [ ] Performance: Search results < 2 seconds
- [ ] All tests passing

## Migration Notes

### Legacy VB.NET Pattern → Modern ASP.NET Core

1. **Business Objects → DTOs**: Direct mapping, no separate domain models
2. **CSLA Pattern → Repository Pattern**: Using Dapper with DTOs
3. **Infragistics Controls → Bootstrap + DataTables**: Modern UI components
4. **Event-driven Validation → Data Annotations + FluentValidation**
5. **Stored Procedures → Direct SQL Queries**: Parameterized SQL with Dapper
6. **WinForms Tabs → Bootstrap Tabs**: Same tab structure, modern implementation
7. **Nested Editing → AJAX + Modals/Inline**: Similar UX, modern technology

### Data Migration

- No data migration required - database schema remains the same
- Existing stored procedures can remain for now (will use direct SQL in new code)
- Legacy VB.NET application can coexist during transition

## Next Steps

1. Review and approve this conversion plan
2. Set up development environment
3. Begin Phase 1: Create Shared DTOs
4. Proceed through phases sequentially
5. Conduct code reviews at end of each phase
6. Deploy to test environment after Phase 9

---

**Document Version**: 1.0
**Last Updated**: 2025-12-15
**Author**: Claude Code Template Generator
