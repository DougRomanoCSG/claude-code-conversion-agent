# Facility Conversion Plan

## Executive Summary

This document outlines the complete conversion plan for migrating the Facility management module from the legacy VB.NET WinForms application to a modern ASP.NET Core architecture split between:
- **BargeOps.Admin.API**: RESTful API backend
- **BargeOps.Admin.UI**: ASP.NET Core MVC frontend

The Facility module manages facility locations with search capabilities, detailed editing including tabs for Status history and Berths, and read-only NDC (National Data Center) reference data.

## Source Analysis Summary

### Legacy Components
- **Forms**: `frmFacilitySearch`, `frmFacilityDetail`
- **Business Objects**: `Location`, `FacilityLocation`, `FacilityBerth`, `FacilityStatus`
- **Stored Procedures**:
  - Search: `sp_FacilityLocationSearch`
  - CRUD: `sp_FacilityLocation_GetByID`, `_Insert`, `_Update`, `_Delete`
  - Child entities: `sp_FacilityBerth_*`, `sp_FacilityStatus_*`

### Key Features
1. **Search Form**:
   - Criteria: Name, ShortName, BargeEx Code, River, Facility Type, Mile Range, Active Only
   - Results grid with pagination
   - Actions: New, Edit, Delete

2. **Detail Form** with 4 tabs:
   - **Details Tab**: Basic facility info, conditional Lock/Gauge fields
   - **Status Tab**: Facility status history with inline editing
   - **Berths Tab**: Facility berths with inline editing
   - **NDC Data Tab**: Read-only National Data Center reference data

3. **Complex Business Rules**:
   - Conditional field visibility based on Facility Type (Lock/Gauge Location)
   - Mile range validation (EndMile >= StartMile)
   - River required when Mile specified
   - Parent entity must be saved before adding child records

## Target Architecture

### Project Structure

#### BargeOps.Admin.API
```
BargeOps.Admin.API/
├── src/
│   ├── Admin.Api/
│   │   └── Controllers/
│   │       └── FacilityController.cs
│   ├── Admin.Domain/
│   │   ├── Models/
│   │   │   ├── Facility.cs
│   │   │   ├── FacilityBerth.cs
│   │   │   └── FacilityStatus.cs
│   │   ├── Dto/
│   │   │   ├── FacilityDto.cs
│   │   │   ├── FacilitySearchRequest.cs
│   │   │   ├── FacilityBerthDto.cs
│   │   │   └── FacilityStatusDto.cs
│   │   └── Services/
│   │       ├── IFacilityService.cs
│   │       └── FacilityService.cs
│   └── Admin.Infrastructure/
│       ├── Repositories/
│       │   ├── IFacilityRepository.cs
│       │   └── FacilityRepository.cs
│       └── Mappings/
│           └── FacilityMappingProfile.cs
```

#### BargeOps.Admin.UI
```
BargeOps.Admin.UI/
├── Controllers/
│   └── FacilitySearchController.cs
├── Models/
│   ├── FacilitySearchViewModel.cs
│   ├── FacilityEditViewModel.cs
│   ├── FacilityBerthViewModel.cs
│   └── FacilityStatusViewModel.cs
├── Views/
│   └── FacilitySearch/
│       ├── Index.cshtml
│       ├── Edit.cshtml
│       ├── _StatusModal.cshtml
│       └── _BerthModal.cshtml
├── Services/
│   ├── IFacilityService.cs
│   └── FacilityService.cs
└── wwwroot/
    ├── js/
    │   ├── facilitySearch.js
    │   └── facilityDetail.js
    └── css/
        └── facilitySearch.css (optional)
```

## Reference Patterns

### Primary References
- **BoatLocation** (BargeOps.Admin.API): Canonical pattern for Admin API controllers
  - Location: `C:\source\BargeOps\BargeOps.Admin.API\src\Admin.Api\Controllers\BoatLocationController.cs`
- **BoatLocationSearch** (BargeOps.Admin.UI): Canonical pattern for Admin UI controllers
  - Location: `C:\source\BargeOps\BargeOps.Admin.UI\Controllers\BoatLocationSearchController.cs`

### Secondary References
- **Crewing API** (BargeOps.Crewing.API): Additional patterns for complex entities
- **Crewing UI** (BargeOps.Crewing.UI): DataTables and Select2 implementation examples

## Data Model

### Entity Relationships

```
Location (Parent)
├── FacilityLocation (1:1)
│   ├── FacilityBerths (1:N)
│   └── FacilityStatuses (1:N)
└── Properties:
    ├── LocationID (PK)
    ├── Name
    ├── ShortName
    ├── RiverID (FK)
    ├── Mile
    ├── IsActive
    └── Note

FacilityLocation (Detail)
├── FacilityLocationID (PK)
├── LocationID (FK)
├── BargeExCode
├── Bank
├── BargeExLocationTypeID (FK)
├── Lock Fields (conditional)
│   ├── LockUsaceName
│   ├── LockFloodStage
│   ├── LockPoolStage
│   ├── LockLowWater
│   ├── LockNormalCurrent
│   ├── LockHighFlow
│   ├── LockHighWater
│   └── LockCatastrophicLevel
└── NDC Fields (read-only)
    ├── NdcName
    ├── NdcAddress
    ├── NdcTown
    ├── NdcState
    ├── NdcCounty
    ├── NdcCountyFips
    ├── NdcWaterway
    ├── NdcPort
    ├── NdcLatitude
    ├── NdcLongitude
    ├── NdcOperator
    ├── NdcOwner
    ├── NdcPurpose
    └── NdcRemark

FacilityBerth
├── FacilityBerthID (PK)
├── FacilityLocationID (FK)
├── Name
└── ShipName (read-only)

FacilityStatus
├── FacilityStatusID (PK)
├── LocationID (FK)
├── StartDateTime
├── EndDateTime
├── StatusID (FK)
└── Note
```

## API Design

### Endpoints

#### Facility CRUD
```http
GET    /api/facility/search          # Search facilities (DataTables server-side)
GET    /api/facility/{id}            # Get facility by ID
POST   /api/facility                 # Create facility
PUT    /api/facility/{id}            # Update facility
DELETE /api/facility/{id}            # Soft delete facility
```

#### Lookups
```http
GET    /api/facility/rivers          # Get rivers for dropdown
GET    /api/facility/facility-types  # Get facility types for dropdown
GET    /api/facility/banks           # Get banks (static: Left/Right/Both)
```

#### Child Entities
```http
GET    /api/facility/{id}/berths             # Get berths for facility
POST   /api/facility/{id}/berths             # Add berth to facility
PUT    /api/facility/{id}/berths/{berthId}   # Update berth
DELETE /api/facility/{id}/berths/{berthId}   # Delete berth

GET    /api/facility/{id}/statuses           # Get statuses for facility
POST   /api/facility/{id}/statuses           # Add status to facility
PUT    /api/facility/{id}/statuses/{statusId} # Update status
DELETE /api/facility/{id}/statuses/{statusId} # Delete status
```

### Request/Response Models

#### FacilitySearchRequest
```csharp
public class FacilitySearchRequest : DataTableRequest
{
    public string Name { get; set; }
    public string ShortName { get; set; }
    public string BargeExCode { get; set; }
    public int? RiverId { get; set; }
    public int? FacilityTypeId { get; set; }
    public decimal? StartMile { get; set; }
    public decimal? EndMile { get; set; }
    public bool ActiveOnly { get; set; } = true;
}
```

#### FacilityDto
```csharp
public class FacilityDto
{
    public int LocationId { get; set; }
    public string Name { get; set; }
    public string ShortName { get; set; }
    public int? RiverId { get; set; }
    public string RiverName { get; set; }
    public decimal? Mile { get; set; }
    public string Bank { get; set; }
    public int? FacilityTypeId { get; set; }
    public string FacilityTypeName { get; set; }
    public string BargeExCode { get; set; }
    public bool IsActive { get; set; }
    public string Note { get; set; }

    // Lock/Gauge fields (conditional)
    public string LockUsaceName { get; set; }
    public decimal? LockFloodStage { get; set; }
    public decimal? LockPoolStage { get; set; }
    public decimal? LockLowWater { get; set; }
    public decimal? LockNormalCurrent { get; set; }
    public decimal? LockHighFlow { get; set; }
    public decimal? LockHighWater { get; set; }
    public decimal? LockCatastrophicLevel { get; set; }

    // NDC fields (read-only)
    public string NdcName { get; set; }
    public string NdcLocationDescription { get; set; }
    public string NdcAddress { get; set; }
    public string NdcTown { get; set; }
    public string NdcState { get; set; }
    public string NdcCounty { get; set; }
    public string NdcCountyFips { get; set; }
    public string NdcWaterway { get; set; }
    public string NdcPort { get; set; }
    public string NdcLatitude { get; set; }
    public string NdcLongitude { get; set; }
    public string NdcOperator { get; set; }
    public string NdcOwner { get; set; }
    public string NdcPurpose { get; set; }
    public string NdcRemark { get; set; }

    // Child collections
    public List<FacilityBerthDto> Berths { get; set; }
    public List<FacilityStatusDto> Statuses { get; set; }
}
```

## UI Design

### Pages

#### 1. Search Page (Index.cshtml)
**Route**: `/FacilitySearch` or `/FacilitySearch/Index`

**Layout**:
```
┌─────────────────────────────────────────────┐
│ Search Criteria (Collapsible Card)         │
│ ┌─────────────────────────────────────────┐ │
│ │ Name: [________] Short Name: [_______] │ │
│ │ BargeEx Code: [________]                │ │
│ │ River: [Select2▼] Type: [Select2▼]     │ │
│ │ Start Mile: [____] End Mile: [____]    │ │
│ │ [x] Active Only                         │ │
│ │ [Search] [Clear]                        │ │
│ └─────────────────────────────────────────┘ │
├─────────────────────────────────────────────┤
│ Actions: [New] [Edit] [Delete]             │
├─────────────────────────────────────────────┤
│ Search Results (DataTables)                 │
│ ┌─────────────────────────────────────────┐ │
│ │ Name│Short│River│Mile│Type│BargeEx│Act│ │
│ │ ... │ ... │ ... │... │... │  ...  │ ✓ │ │
│ │ [Pagination] [Export]                   │ │
│ └─────────────────────────────────────────┘ │
└─────────────────────────────────────────────┘
```

**Features**:
- Collapsible search criteria panel
- Select2 dropdowns with AJAX loading
- DataTables with server-side processing
- Row selection for Edit/Delete
- Double-click row to edit
- Permission-based button visibility

#### 2. Edit Page (Edit.cshtml)
**Route**: `/FacilitySearch/Edit/{id?}`

**Layout**:
```
┌─────────────────────────────────────────────┐
│ Facility Details / New Facility            │
├─────────────────────────────────────────────┤
│ [Details] [Status] [Berths] [NDC Data]     │  <- Bootstrap Tabs
├─────────────────────────────────────────────┤
│ Tab Content Area                            │
│                                             │
│ Details Tab:                                │
│   Name: [________________] Required         │
│   Short Name: [___________]                 │
│   River: [Select2▼] Mile: [____]           │
│   Bank: [Select2▼]                          │
│   Facility Type: [Select2▼] Required       │
│   BargeEx Code: [________]                  │
│   [x] Is Active                             │
│                                             │
│   Lock/Gauge Information (conditional)      │
│   ┌───────────────────────────────────────┐ │
│   │ Lock USACE Name: [_____________]      │ │
│   │ Lock Flood Stage: [____]              │ │
│   │ Lock Pool Stage: [____]               │ │
│   │ Lock Low Water: [____]                │ │
│   │ ... (other lock fields)               │ │
│   └───────────────────────────────────────┘ │
│                                             │
│ Status/Berths Tabs:                         │
│   DataTable with [Add] [Edit] [Delete]     │
│   Modal dialogs for add/edit               │
│                                             │
│ NDC Data Tab:                               │
│   Read-only display of NDC reference data  │
│                                             │
├─────────────────────────────────────────────┤
│ [Save] [Cancel] [Delete]                    │
└─────────────────────────────────────────────┘
```

**Features**:
- Bootstrap tabs navigation
- Conditional Lock/Gauge panel (JavaScript toggle)
- Modal dialogs for berths and statuses
- Inline validation with jQuery Validation
- Tab state preserved on validation errors
- Disable Status/Berths tabs until parent saved

### Component Mapping

| Legacy Control | Modern Component | Notes |
|----------------|------------------|-------|
| UltraGrid | DataTables | Server-side processing |
| UltraCombo | Select2 | Bootstrap 5 theme |
| UltraTextEditor | Bootstrap input | HTML5 validation |
| UltraNumericEditor | Bootstrap input[number] | step="0.01" for decimals |
| UltraCheckEditor | Bootstrap checkbox | form-check styling |
| UltraPanel | Bootstrap Card | card/card-body |
| UltraTabControl | Bootstrap Tabs | nav-tabs/tab-content |
| UltraButton | Bootstrap Button | btn/btn-primary, etc. |

## Security Model

### Permissions
- **FacilityReadOnly**: View/search facilities
- **FacilityModify**: Create/update/delete facilities

### Implementation

#### API Level
```csharp
[Authorize(Policy = "FacilityReadOnly")]
public class FacilityController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FacilityDto>>> Search(...)

    [HttpPost]
    [Authorize(Policy = "FacilityModify")]
    public async Task<ActionResult<FacilityDto>> Create(...)

    [HttpPut("{id}")]
    [Authorize(Policy = "FacilityModify")]
    public async Task<ActionResult> Update(...)

    [HttpDelete("{id}")]
    [Authorize(Policy = "FacilityModify")]
    public async Task<ActionResult> Delete(...)
}
```

#### UI Level
```csharp
[Authorize(Policy = "FacilityReadOnly")]
public class FacilitySearchController : Controller
{
    public IActionResult Index() => View();

    [Authorize(Policy = "FacilityModify")]
    public async Task<IActionResult> Edit(int? id) => ...
}
```

#### Razor Views
```html
@if (User.HasClaim("Permission", "FacilityModify"))
{
    <button class="btn btn-success" onclick="newFacility()">New</button>
    <button class="btn btn-primary" onclick="editFacility()">Edit</button>
    <button class="btn btn-danger" onclick="deleteFacility()">Delete</button>
}
```

## Validation Rules

### Search Form

| Rule | Field(s) | Validation | Error Message |
|------|----------|------------|---------------|
| SEARCH-001 | RiverId | Required when StartMile OR EndMile has value | "A beginning or ending mile was specified, but no river was selected." |
| SEARCH-002 | EndMile | Must be >= StartMile when both specified | "The ending mile is less than the beginning mile." |

### Detail Form

| Rule | Field | Validation | Error Message |
|------|-------|------------|---------------|
| LOC-001 | Name | Required, MaxLength(100) | "Facility Name is required" |
| LOC-002 | ShortName | MaxLength(50) | - |
| LOC-004 | Mile | Range(0, 9999.99) | "Mile must be between 0 and 9999.99" |
| LOC-005 | RiverId | Required when Mile specified | "River is required when Mile is specified" |
| FLOC-004-011 | Lock/Gauge fields | Cleared when FacilityType != Lock/Gauge | - |
| BERTH-001 | BerthName | Required, MaxLength(100) | "Berth Name is required" |
| STATUS-001 | StartDateTime | Required | "Start Date/Time is required" |
| STATUS-002 | Status | Required | "Status is required" |
| STATUS-004 | EndDateTime | Must be >= StartDateTime if specified | "End Date/Time must be after Start Date/Time" |

### Business Rules

| Rule | Description | Implementation |
|------|-------------|----------------|
| BR-001 | Lock/Gauge fields cleared when type changes | Server-side: Clear before save; Client-side: Disable/hide fields |
| BR-002 | Lock/Gauge fields only enabled for Lock/Gauge types | JavaScript: Toggle visibility on facility type change |
| BR-003 | Child entities require parent saved | Disable Status/Berths tabs until LocationId > 0 |

## Implementation Steps

### Phase 1: API Development (BargeOps.Admin.API)

1. **Domain Models** (`Admin.Domain/Models/`)
   - Create `Facility.cs` (domain model)
   - Create `FacilityBerth.cs`
   - Create `FacilityStatus.cs`

2. **DTOs** (`Admin.Domain/Dto/`)
   - Create `FacilityDto.cs`
   - Create `FacilitySearchRequest.cs`
   - Create `FacilityBerthDto.cs`
   - Create `FacilityStatusDto.cs`

3. **Repository** (`Admin.Infrastructure/Repositories/`)
   - Create `IFacilityRepository.cs` interface
   - Implement `FacilityRepository.cs` with Dapper
   - Methods: `SearchAsync`, `GetByIdAsync`, `InsertAsync`, `UpdateAsync`, `DeleteAsync`
   - Child entity methods: `GetBerthsAsync`, `AddBerthAsync`, `UpdateBerthAsync`, `DeleteBerthAsync`
   - Child entity methods: `GetStatusesAsync`, `AddStatusAsync`, `UpdateStatusAsync`, `DeleteStatusAsync`

4. **Service Layer** (`Admin.Domain/Services/`)
   - Create `IFacilityService.cs` interface
   - Implement `FacilityService.cs`
   - Business logic validation
   - Transaction management for parent-child operations

5. **AutoMapper** (`Admin.Infrastructure/Mappings/`)
   - Create `FacilityMappingProfile.cs`
   - Map between domain models and DTOs

6. **Controller** (`Admin.Api/Controllers/`)
   - Create `FacilityController.cs`
   - Implement all CRUD endpoints
   - Add authorization attributes
   - Handle DataTables request/response format

### Phase 2: UI Development (BargeOps.Admin.UI)

7. **ViewModels** (`Models/`)
   - Create `FacilitySearchViewModel.cs`
   - Create `FacilityEditViewModel.cs`
   - Create `FacilityBerthViewModel.cs`
   - Create `FacilityStatusViewModel.cs`
   - Add validation attributes (DataAnnotations)

8. **UI Service** (`Services/`)
   - Create `IFacilityService.cs` (UI service interface)
   - Implement `FacilityService.cs` (calls Admin API)

9. **Controller** (`Controllers/`)
   - Create `FacilitySearchController.cs`
   - Actions: `Index`, `Search`, `Edit`, `Create`, `Update`, `Delete`
   - Handle DataTables requests
   - Return views with proper ViewModels

10. **Views** (`Views/FacilitySearch/`)
    - Create `Index.cshtml` (search page)
    - Create `Edit.cshtml` (detail page with tabs)
    - Create `_StatusModal.cshtml` (partial view for status modal)
    - Create `_BerthModal.cshtml` (partial view for berth modal)

11. **JavaScript** (`wwwroot/js/`)
    - Create `facilitySearch.js`
      - DataTables initialization
      - Search functionality
      - Row selection
      - New/Edit/Delete handlers
    - Create `facilityDetail.js`
      - Tab management
      - Facility type change handler (show/hide lock/gauge panel)
      - Status modal operations
      - Berth modal operations
      - Form validation
      - Save/Cancel/Delete handlers

12. **CSS** (`wwwroot/css/`)
    - Create `facilitySearch.css` (optional, if custom styling needed)

### Phase 3: Testing & Validation

13. **Unit Tests**
    - Test repository methods
    - Test service layer business logic
    - Test validation rules

14. **Integration Tests**
    - Test API endpoints
    - Test controller actions
    - Test UI service calls

15. **UI Testing**
    - Test search functionality
    - Test CRUD operations
    - Test conditional field visibility
    - Test validation messages
    - Test permission-based access

### Phase 4: Deployment

16. **Database**
    - Verify stored procedures exist
    - Verify permissions configured
    - Test data migration if needed

17. **Configuration**
    - Update API connection strings
    - Configure authentication policies
    - Set up logging

18. **Documentation**
    - API documentation (Swagger)
    - User guide for new UI
    - Developer notes for maintenance

## Database Schema

### Tables

#### Location
```sql
CREATE TABLE Location (
    LocationID INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100) NOT NULL,
    ShortName NVARCHAR(50),
    RiverID INT,
    Mile DECIMAL(10,2),
    IsActive BIT NOT NULL DEFAULT 1,
    Note NVARCHAR(500),
    CreatedBy NVARCHAR(50) NOT NULL,
    CreatedDate DATETIME NOT NULL,
    ModifiedBy NVARCHAR(50),
    ModifiedDate DATETIME,
    FOREIGN KEY (RiverID) REFERENCES River(RiverID)
)
```

#### FacilityLocation
```sql
CREATE TABLE FacilityLocation (
    FacilityLocationID INT PRIMARY KEY IDENTITY,
    LocationID INT NOT NULL,
    BargeExCode NVARCHAR(20),
    Bank NVARCHAR(20),
    BargeExLocationTypeID INT,
    LockUsaceName NVARCHAR(100),
    LockFloodStage DECIMAL(10,2),
    LockPoolStage DECIMAL(10,2),
    LockLowWater DECIMAL(10,2),
    LockNormalCurrent DECIMAL(10,2),
    LockHighFlow DECIMAL(10,2),
    LockHighWater DECIMAL(10,2),
    LockCatastrophicLevel DECIMAL(10,2),
    NdcName NVARCHAR(100),
    NdcLocationDescription NVARCHAR(500),
    NdcAddress NVARCHAR(200),
    NdcTown NVARCHAR(100),
    NdcState NVARCHAR(2),
    NdcCounty NVARCHAR(100),
    NdcCountyFips NVARCHAR(10),
    NdcWaterway NVARCHAR(100),
    NdcPort NVARCHAR(100),
    NdcLatitude NVARCHAR(50),
    NdcLongitude NVARCHAR(50),
    NdcOperator NVARCHAR(100),
    NdcOwner NVARCHAR(100),
    NdcPurpose NVARCHAR(500),
    NdcRemark NVARCHAR(500),
    FOREIGN KEY (LocationID) REFERENCES Location(LocationID),
    FOREIGN KEY (BargeExLocationTypeID) REFERENCES BargeExLocationType(BargeExLocationTypeID)
)
```

#### FacilityBerth
```sql
CREATE TABLE FacilityBerth (
    FacilityBerthID INT PRIMARY KEY IDENTITY,
    FacilityLocationID INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    ShipName NVARCHAR(100),  -- Computed/lookup from current ship assignments
    FOREIGN KEY (FacilityLocationID) REFERENCES FacilityLocation(FacilityLocationID)
)
```

#### FacilityStatus
```sql
CREATE TABLE FacilityStatus (
    FacilityStatusID INT PRIMARY KEY IDENTITY,
    LocationID INT NOT NULL,
    StatusID INT NOT NULL,
    StartDateTime DATETIME NOT NULL,
    EndDateTime DATETIME,
    Note NVARCHAR(5000),
    CreatedBy NVARCHAR(50) NOT NULL,
    CreatedDate DATETIME NOT NULL,
    ModifiedBy NVARCHAR(50),
    ModifiedDate DATETIME,
    FOREIGN KEY (LocationID) REFERENCES Location(LocationID),
    FOREIGN KEY (StatusID) REFERENCES ValidationList(ValidationListID)
)
```

### Stored Procedures

Reference the `data-access.json` file for complete stored procedure definitions. Key procedures:

- **sp_FacilityLocationSearch**: Search facilities with filtering and pagination
- **sp_FacilityLocation_GetByID**: Retrieve complete facility details
- **sp_FacilityLocation_Insert**: Insert new facility
- **sp_FacilityLocation_Update**: Update existing facility
- **sp_FacilityLocation_Delete**: Soft delete facility (set IsActive = 0)
- **sp_FacilityBerth_GetByFacilityID**: Get berths for facility
- **sp_FacilityStatus_GetByFacilityID**: Get statuses for facility
- **sp_River_GetAll**: Get rivers for dropdown
- **sp_BargeExLocationType_GetAll**: Get facility types for dropdown

## Technology Stack

### API (BargeOps.Admin.API)
- **Framework**: ASP.NET Core 6.0+
- **ORM**: Dapper
- **Mapping**: AutoMapper
- **Authentication**: Windows Authentication
- **Authorization**: CSG Authorization Library
- **Validation**: FluentValidation

### UI (BargeOps.Admin.UI)
- **Framework**: ASP.NET Core MVC 6.0+
- **Authentication**: IdentityConstants.ApplicationScheme
- **Authorization**: Policy-based authorization
- **Frontend Libraries**:
  - jQuery 3.6+
  - Bootstrap 5.3+
  - DataTables 1.13+
  - Select2 4.1+
  - Moment.js 2.29+
  - Bootstrap Icons 1.10+

### Development Tools
- **IDE**: Visual Studio 2022
- **Version Control**: Git
- **Package Manager**: NuGet, npm
- **Testing**: xUnit, Moq

## Migration Checklist

### Pre-Migration
- [ ] Review all stored procedures and verify they exist in target database
- [ ] Verify permissions (FacilityReadOnly, FacilityModify) are configured
- [ ] Back up existing data
- [ ] Set up development/test environments

### API Development
- [ ] Create domain models (Facility, FacilityBerth, FacilityStatus)
- [ ] Create DTOs (FacilityDto, FacilitySearchRequest, etc.)
- [ ] Implement IFacilityRepository interface
- [ ] Implement FacilityRepository with Dapper
- [ ] Implement IFacilityService interface
- [ ] Implement FacilityService with business logic
- [ ] Create AutoMapper profiles
- [ ] Implement FacilityController
- [ ] Add authorization policies
- [ ] Test all API endpoints

### UI Development
- [ ] Create ViewModels with validation attributes
- [ ] Implement UI service to call API
- [ ] Create FacilitySearchController
- [ ] Create Index.cshtml (search page)
- [ ] Create Edit.cshtml (detail page with tabs)
- [ ] Create modal partial views
- [ ] Implement facilitySearch.js (DataTables, search logic)
- [ ] Implement facilityDetail.js (tabs, modals, validation)
- [ ] Add CSS styling if needed
- [ ] Test UI functionality

### Integration & Testing
- [ ] Test search with various criteria
- [ ] Test create new facility
- [ ] Test edit existing facility
- [ ] Test delete facility
- [ ] Test adding/editing/deleting berths
- [ ] Test adding/editing/deleting statuses
- [ ] Test conditional Lock/Gauge field visibility
- [ ] Test validation rules
- [ ] Test permission-based access control
- [ ] Test with different user roles

### Deployment
- [ ] Deploy API to test environment
- [ ] Deploy UI to test environment
- [ ] Perform UAT (User Acceptance Testing)
- [ ] Fix any issues found during UAT
- [ ] Deploy to production
- [ ] Monitor for errors
- [ ] Gather user feedback

## Known Considerations

### Conditional Lock/Gauge Fields
- **Legacy**: Fields are enabled/disabled based on Facility Type
- **Modern**: Fields are shown/hidden via JavaScript
- **Implementation**:
  - Client-side: Toggle visibility on facility type change
  - Server-side: Clear values before save if type changes

### Composite DateTime Fields
- **Legacy**: StartDateTime/EndDateTime use separate date picker and time combo controls
- **Modern Options**:
  1. Use HTML5 `datetime-local` input
  2. Use separate date picker + time dropdown (like legacy)
  3. Use third-party datetime picker (e.g., Flatpickr)
- **Recommendation**: Option 2 (separate controls) for consistency with legacy UX

### NDC Reference Data
- **Nature**: Read-only reference data from National Data Center
- **Implementation**: Display-only fields, no editing
- **Tab**: Separate "NDC Data" tab with read-only display

### Parent-Child Relationship Management
- **Legacy**: Child tabs disabled until parent saved
- **Modern**:
  - Option 1: Disable tabs until LocationId > 0
  - Option 2: Show message "Save facility before adding berths/statuses"
  - **Recommendation**: Option 1 (disable tabs) for better UX

### Export Functionality
- **Legacy**: "Export" button on Status grid
- **Modern**: DataTables export buttons (Excel, CSV, PDF, Print)
- **Implementation**: Use DataTables Buttons extension

## Next Steps

1. Review this conversion plan with the development team
2. Clarify any ambiguities or questions
3. Begin Phase 1: API Development
4. Follow the implementation steps sequentially
5. Use the generated templates as starting points
6. Reference BoatLocation patterns for consistency
7. Test thoroughly at each phase

## Support & References

### Documentation
- ASP.NET Core MVC: https://docs.microsoft.com/aspnet/core/mvc
- DataTables: https://datatables.net/
- Select2: https://select2.org/
- Bootstrap 5: https://getbootstrap.com/docs/5.3/

### Internal References
- BoatLocation API Controller: `C:\source\BargeOps\BargeOps.Admin.API\src\Admin.Api\Controllers\BoatLocationController.cs`
- BoatLocation UI Controller: `C:\source\BargeOps\BargeOps.Admin.UI\Controllers\BoatLocationSearchController.cs`
- Crewing API Examples: `C:\source\BargeOps\Crewing.API\src\Crewing.Api\Controllers\`
- Crewing UI Examples: `C:\source\BargeOps\Crewing.UI\`

### Team Contacts
- Architecture questions: [Architecture Team]
- Database questions: [DBA Team]
- UI/UX questions: [Frontend Team]
- Security/Authorization: [Security Team]

---

**Document Version**: 1.0
**Created**: 2025-11-11
**Author**: Claude Conversion Agent
**Status**: Ready for Review
