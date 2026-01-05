# Barge Entity - Conversion Plan

## Executive Summary

This document provides a comprehensive plan for converting the Barge entity from the legacy OnShore VB.NET application to the modern BargeOps.Admin.Mono stack using MONO SHARED architecture.

**Entity:** Barge
**Complexity:** Very High
**Estimated Effort:** 10-15 days
**Priority:** High

---

## Architecture Overview

### MONO SHARED Structure

⭐ **CRITICAL**: This project uses a MONO SHARED structure where DTOs and Models are in a SHARED project!

```
BargeOps.Admin.Mono/
├── src/BargeOps.Shared/          ⭐ SINGLE SOURCE OF TRUTH
│   └── Dto/                       ⭐ DTOs are the ONLY data models
│       ├── BargeDto.cs            (Complete entity DTO)
│       ├── BargeSearchRequest.cs  (Search criteria)
│       └── Related DTOs...
├── src/BargeOps.API/
│   └── src/
│       ├── Admin.Api/Controllers/
│       ├── Admin.Domain/Services/
│       └── Admin.Infrastructure/
│           ├── Repositories/
│           └── DataAccess/Sql/
└── src/BargeOps.UI/
    ├── Controllers/
    ├── ViewModels/
    ├── Services/
    ├── Views/
    └── wwwroot/
```

### Key Principles

1. **DTOs First**: Create Shared DTOs BEFORE any other code
2. **No Duplication**: DTOs are used by BOTH API and UI - no separate Models folder
3. **Direct SQL**: Repositories use Dapper with direct SQL queries (NOT stored procedures)
4. **No Mapping**: Repositories return DTOs directly (no AutoMapper needed)
5. **ViewModels**: UI uses ViewModels that CONTAIN DTOs (not map to them)

---

## Entity Analysis Summary

### Form Structure

**Search Form:** frmBargeSearch
- **Basic Criteria** (always visible): BargeNum, Operator, Status, HullType, CoverType, LoadStatus, Tickets, Customer
- **Advanced Criteria** (collapsible): Boat/Facility/Ship search, Equipment Type, USCG Number, Size Category, River, Mile Range, Commodity, Contract Number
- **Results Grid**: 58 columns with conditional visibility, row formatting, multi-select

**Detail Form:** frmBargeDetail
- **Physical Characteristics**: Series, Hull Type, Dimensions, Size Category, Cover Type/Config/SubType, Barge Type
- **Status**: Equipment Type, Fleet, Owner, Customer, Color, Location, Load/Clean/Repair Status, Commodity, Draft, Freeboard, In/Out of Service Dates
- **Damage/Repair**: Repair Status, Damage flags, Leaker, Dry Dock
- **Ticket Details**: Read-only ticket information (inspection, hold, destinations, schedules, consign)
- **Tabs**: General, Charters (child grid)

### Data Model

**Primary Table:** Barge
**Primary Key:** BargeID (int)
**Soft Delete:** Yes (IsActive bit)
**Audit Fields:** CreateDateTime, ModifyDateTime, CreateUser, ModifyUser

**Key Fields:**
- Identifiers: BargeNum (required), UscgNum, HullNumber, GlAccountNum
- Physical: ExternalLength, ExternalWidth, ExternalDepth, HullType, CoverType, CoverConfig, CoverSubTypeID, SizeCategory, BargeType
- Draft: Draft (decimal), DraftPortBow, DraftPortStern, DraftStarboardBow, DraftStarboardStern, DraftCalculated
- Status: EquipmentType, FleetID, OwnerID, CustomerID, LocationID, LocationDateTime, LoadStatus, CleanStatus, RepairStatus
- Damage: DamageLevel, DamageNote, IsDamaged, IsCargoDamaged, IsLeaker, IsDryDocked, IsRepairScheduled
- Other: ColorPairID, CommodityID, HasInsufficientFreeboard, FreeboardRange, InServiceDate, OutOfServiceDate, BargeSeriesID

**Related Entities:**
- BargeSeries (FK: BargeSeriesID) - Auto-populates fields
- Customer (FK: CustomerID, OwnerID) - Operator/Owner
- Fleet (FK: FleetID) - For fleet-owned equipment
- Location (FK: LocationID) - Current location
- Commodity (FK: CommodityID) - Current commodity
- CoverSubType (FK: CoverSubTypeID) - Cover sub-type
- ColorPair (FK: ColorPairID) - Barge token colors
- BargeCharters (1:N) - Charter history

### Business Logic

**Complex Validations:**
1. **Equipment Type Logic**: Fleet-owned vs. customer-owned affects required fields
2. **Cover Type Logic**: Complex conditional requirements based on commodity, operator type, and global settings
3. **Draft Validation**: Overall draft must be >= all corner drafts; range 0-99.999 ft
4. **Charter Date Ranges**: No overlapping charter periods
5. **Berth Location Matching**: Facility berth must match barge location
6. **Conditional Requirements**: CustomerID, SizeCategory, FleetID, CoverConfig, FreeboardRange based on context

**Auto-Calculations:**
- **SizeCategory**: Auto-calculated from ExternalLength and ExternalWidth
- **Status**: Auto-updated when LocationID changes
- **Draft Conversions**: Feet/inches ↔ decimal representations

**Auto-Population:**
- **BargeSeriesID**: Selecting a series auto-fills OwnerID, CustomerID, HullType, ExternalDepth, ExternalLength, ExternalWidth, SizeCategory, CoverType

---

## Implementation Order

### Phase 1: SHARED Project (BargeOps.Shared) ⭐ START HERE

**Priority: CRITICAL - Create these FIRST!**

1. **BargeDto.cs** - Complete entity DTO with [Sortable]/[Filterable] attributes
2. **BargeSearchRequest.cs** - Search criteria DTO
3. **BargeSearchResultDto.cs** - Grid row DTO (58 columns from search SP)
4. **BargeCharterDto.cs** - Child entity DTO
5. **PagedResult.cs** / **DataTableResponse.cs** - Generic wrappers (if not already exist)

### Phase 2: API Infrastructure (BargeOps.API)

1. **SQL Files** (Admin.Infrastructure/DataAccess/Sql/)
   - Barge_GetById.sql
   - Barge_Search.sql (complex with boat/facility/ship filters)
   - Barge_Insert.sql
   - Barge_Update.sql
   - Barge_SetActive.sql (soft delete)
   - Barge_GetLocationList.sql

2. **Repository** (Admin.Infrastructure/Repositories/)
   - IBargeRepository.cs - Interface returning DTOs
   - BargeRepository.cs - Dapper implementation with direct SQL queries

3. **Service** (Admin.Domain/Services/ and Admin.Infrastructure/Services/)
   - IBargeService.cs - Service interface
   - BargeService.cs - Business logic layer

4. **Controller** (Admin.Api/Controllers/)
   - BargeController.cs - RESTful API endpoints with authorization

### Phase 3: UI Layer (BargeOps.UI)

1. **ViewModels** (ViewModels/)
   - BargeSearchViewModel.cs - Search screen
   - BargeEditViewModel.cs - Edit/create form
   - BargeCharterViewModel.cs - Child charter data

2. **API Client Service** (Services/)
   - IBargeService.cs - HTTP client interface
   - BargeService.cs - Calls API endpoints, returns DTOs

3. **MVC Controller** (Controllers/)
   - BargeSearchController.cs - Index, Edit, Create, Delete, BargeTable (DataTables endpoint)

4. **Razor Views** (Views/BargeSearch/)
   - Index.cshtml - Search form + results grid
   - Edit.cshtml - Detail form with tabs
   - _Partials/ - Modal dialogs for charters

5. **JavaScript** (wwwroot/js/)
   - barge-search.js - DataTables initialization, search logic
   - barge-edit.js - Form logic, auto-population, validations

6. **CSS** (wwwroot/css/)
   - barge-search.css - Custom styling

---

## Data Transfer Objects (DTOs)

### BargeDto.cs

**Location:** `src/BargeOps.Shared/Dto/BargeDto.cs`
**Namespace:** `BargeOps.Shared.Dto`

**Purpose:** Complete entity DTO used by BOTH API and UI

**Properties:**
```csharp
public class BargeDto
{
    // Primary Key
    [Sortable] [Filterable] public int BargeID { get; set; }

    // Identifiers
    [Required] [StringLength(50)] [Sortable] [Filterable]
    public string BargeNum { get; set; } = string.Empty;

    [StringLength(50)] [Sortable] [Filterable]
    public string? UscgNum { get; set; }

    [StringLength(50)]
    public string? HullNumber { get; set; }

    [StringLength(50)]
    public string? GlAccountNum { get; set; }

    // Physical Characteristics
    [Sortable] [Filterable]
    public char? HullType { get; set; }

    [Range(0.0, 50000.0)]
    public decimal? ExternalLength { get; set; }

    [Range(0.0, 20000.0)]
    public decimal? ExternalWidth { get; set; }

    [Range(0.0, 10000.0)]
    public decimal? ExternalDepth { get; set; }

    [Sortable] [Filterable]
    public string? SizeCategory { get; set; }

    [Sortable] [Filterable]
    public string? CoverType { get; set; }

    [Filterable]
    public string? CoverConfig { get; set; }

    public int? CoverSubTypeID { get; set; }

    public string? BargeType { get; set; }

    // Draft
    [Range(0.0, 99.999)]
    public decimal? Draft { get; set; }

    [Range(0.0, 99.999)]
    public decimal? DraftPortBow { get; set; }

    [Range(0.0, 99.999)]
    public decimal? DraftPortStern { get; set; }

    [Range(0.0, 99.999)]
    public decimal? DraftStarboardBow { get; set; }

    [Range(0.0, 99.999)]
    public decimal? DraftStarboardStern { get; set; }

    [Range(0.0, 99.999)]
    public decimal? DraftCalculated { get; set; }

    // Status
    [Sortable] [Filterable]
    public string? EquipmentType { get; set; }

    [Filterable]
    public int? FleetID { get; set; }

    [Filterable]
    public int? OwnerID { get; set; }

    [Filterable]
    public int? CustomerID { get; set; }

    [Filterable]
    public int? LocationID { get; set; }

    public DateTime? LocationDateTime { get; set; }

    [Sortable] [Filterable]
    public string? LoadStatus { get; set; }

    [Filterable]
    public string? CleanStatus { get; set; }

    [Filterable]
    public string? RepairStatus { get; set; }

    [Filterable]
    public int? CommodityID { get; set; }

    public int? ColorPairID { get; set; }

    // Damage/Repair
    [Filterable]
    public string? DamageLevel { get; set; }

    [StringLength(500)]
    public string? DamageNote { get; set; }

    public bool IsDamaged { get; set; }
    public bool IsCargoDamaged { get; set; }

    [Filterable]
    public bool IsLeaker { get; set; }

    public bool IsDryDocked { get; set; }
    public bool IsRepairScheduled { get; set; }

    // Freeboard
    public bool HasInsufficientFreeboard { get; set; }
    public string? FreeboardRange { get; set; }

    // Service Dates
    public DateTime? InServiceDate { get; set; }
    public DateTime? OutOfServiceDate { get; set; }

    // Barge Series
    public int? BargeSeriesID { get; set; }

    // Position
    public int? TierID { get; set; }
    public short? TierX { get; set; }
    public short? TierY { get; set; }
    public int? FacilityBerthID { get; set; }
    public short? FacilityBerthX { get; set; }
    public short? FacilityBerthY { get; set; }
    public int? FleetBoatID { get; set; }

    // Other
    public string? RakeDirection { get; set; }
    public int? TowString { get; set; }
    public int? TowCut { get; set; }
    public Guid BargeSyncIdentifier { get; set; }
    public bool HasAnchorWire { get; set; }
    public int? ConvFesBargeID { get; set; }

    // Audit
    [Sortable] [Filterable]
    public bool IsActive { get; set; } = true;

    public DateTime CreateDateTime { get; set; }
    public DateTime ModifyDateTime { get; set; }
    public string CreateUser { get; set; } = string.Empty;
    public string ModifyUser { get; set; } = string.Empty;

    // Navigation (optional, for UI display)
    public string? FleetBoatName { get; set; }
    public DateTime? FleetBoatActivityDateTime { get; set; }
}
```

### BargeSearchRequest.cs

**Location:** `src/BargeOps.Shared/Dto/BargeSearchRequest.cs`

```csharp
public class BargeSearchRequest
{
    // Basic Criteria
    public int? SelectedFleetID { get; set; }
    public string? BargeNum { get; set; }
    public string? HullType { get; set; }
    public string? CoverType { get; set; }
    public int? OperatorID { get; set; }
    public int? CustomerID { get; set; }
    public bool ActiveOnly { get; set; } = true;
    public int? TicketID { get; set; }
    public string? LoadStatus { get; set; }
    public string? Status { get; set; }
    public bool OpenTicketsOnly { get; set; } = true;
    public string? EquipmentType { get; set; }
    public string? UscgNum { get; set; }
    public string? SizeCategory { get; set; }

    // River/Mile Range
    public string? River { get; set; }
    public decimal? StartMile { get; set; }
    public decimal? EndMile { get; set; }

    // Other Criteria
    public string? ContractNumber { get; set; }
    public int? CommodityID { get; set; }

    // Boat Search Filters
    public bool? IsInTow { get; set; }
    public bool? IsScheduledIn { get; set; }
    public bool? IsScheduledOut { get; set; }
    public int? BoatLocationID { get; set; }

    // Facility Search Filters
    public bool? IsAtFacility { get; set; }
    public bool? IsConsignedToFacility { get; set; }
    public bool? IsDestinationIn { get; set; }
    public bool? IsDestinationOut { get; set; }
    public bool? IsOnOrderToFacility { get; set; }
    public int? FacilityLocationID { get; set; }

    // Ship Search Filters
    public bool? IsConsignedToShip { get; set; }
    public bool? IsOnOrderToShip { get; set; }
    public int? ShipLocationID { get; set; }

    // Paging/Sorting (DataTables)
    public int Start { get; set; }
    public int Length { get; set; }
    public int Draw { get; set; }
    public string? SortColumn { get; set; }
    public string? SortDirection { get; set; }
}
```

### BargeSearchResultDto.cs

**Location:** `src/BargeOps.Shared/Dto/BargeSearchResultDto.cs`

```csharp
public class BargeSearchResultDto
{
    // 58 columns from BargeSearch stored procedure
    public int BargeID { get; set; }
    public string BargeNum { get; set; } = string.Empty;
    public string? HullType { get; set; }
    public string? CoverType { get; set; }
    public string? SizeCategory { get; set; }
    public string? CoverConfig { get; set; }
    public string? LoadStatus { get; set; }
    public string? CleanStatus { get; set; }
    public string? RepairStatus { get; set; }
    public string? DamageLevel { get; set; }
    public bool IsLeaker { get; set; }
    public string? LocationName { get; set; }
    public string? LocationRiver { get; set; }
    public string? LocationMile { get; set; }
    public string? Destination { get; set; }
    public string? Commodity { get; set; }
    public string? Customer { get; set; }
    public string? Status { get; set; }
    public string? TierName { get; set; }
    public string? TierXY { get; set; }
    public string? ConsignLocation { get; set; }
    public string? ConsignedShip { get; set; }
    public bool IsOnOrder { get; set; }
    public DateTime? OnOrderScheduleDateTime { get; set; }
    public string? OnOrderTripNumber { get; set; }
    public bool IsAwaitingPickup { get; set; }
    public DateTime? AwaitingPickupReadyDateTime { get; set; }
    public bool IsOnHold { get; set; }
    public string? ScheduleInLoc { get; set; }
    public string? ScheduleOutLoc { get; set; }
    public string? TicketID { get; set; }
    public string? TicketStatus { get; set; }
    public DateTime? LastEventDateTime { get; set; }
    public string? EquipmentType { get; set; }
    public int? ColorPairID { get; set; }
    public bool NewCargoTransferPossible { get; set; }
    public string? CargoHandlingStatus { get; set; }
    public int? CargoID { get; set; }
    public string? Cargo { get; set; }
    public string? CargoCustomer { get; set; }
    public string? SurveyTons { get; set; }
    public DateTime? EstimatedArrivalDateTime { get; set; }
    public DateTime? PositionUpdatedDateTime { get; set; }
    public string? ConsignBargeID { get; set; }
    public string? ConsignStockpileID { get; set; }
    public string? ConsignShipID { get; set; }
    public string? ConsignTrainTicketID { get; set; }
    public string? ConsignTruckTicketID { get; set; }
    public string? ShipHold { get; set; }
    public string? ContractNumber { get; set; }
    public string? PurchaseOrderNum { get; set; }
    public string? Instructions { get; set; }
    public string? FreightStatus { get; set; }
    public string? CustomerComment { get; set; }
    public string? TicketCustomer { get; set; }
    public string? FreightCustomer { get; set; }
    public bool IsHeadBarge { get; set; }
    public bool HasWaterInCargoBox { get; set; }
    public bool IsRakeDown { get; set; }
    public bool IsHeavy { get; set; }
    public string? BargeDimensions { get; set; }
    public string? BargeType { get; set; }
    public bool IsDefaultTons { get; set; }
    public string? FreightOrigin { get; set; }
    public string? FreightDestination { get; set; }
    public string? CrossCharterCode { get; set; }
}
```

### BargeCharterDto.cs

**Location:** `src/BargeOps.Shared/Dto/BargeCharterDto.cs`

```csharp
public class BargeCharterDto
{
    public int BargeCharterID { get; set; }
    public int BargeID { get; set; }
    public int ChartererCustomerID { get; set; }
    public string? ChartererCustomerName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Notes { get; set; }
}
```

---

## ViewModels

### BargeSearchViewModel.cs

**Location:** `src/BargeOps.UI/ViewModels/BargeSearchViewModel.cs`
**Namespace:** `BargeOpsAdmin.ViewModels`

```csharp
using Microsoft.AspNetCore.Mvc.Rendering;
using BargeOps.Shared.Dto;

namespace BargeOpsAdmin.ViewModels;

public class BargeSearchViewModel
{
    // Search Criteria
    public string? BargeNum { get; set; }
    public string? OperatorID { get; set; }
    public string? Status { get; set; }
    public string? HullType { get; set; }
    public string? CoverType { get; set; }
    public int? TicketID { get; set; }
    public string? CustomerID { get; set; }
    public string? LoadStatus { get; set; }
    public bool OpenTicketsOnly { get; set; } = true;
    public bool ActiveOnly { get; set; } = true;

    // Advanced Search
    public string? BoatSearchType { get; set; }
    public string? BoatLocationID { get; set; }
    public string? FacilitySearchType { get; set; }
    public string? FacilityLocationID { get; set; }
    public string? ShipSearchType { get; set; }
    public string? ShipLocationID { get; set; }
    public string? EquipmentType { get; set; }
    public string? UscgNum { get; set; }
    public string? SizeCategory { get; set; }
    public string? RiverID { get; set; }
    public decimal? StartMile { get; set; }
    public decimal? EndMile { get; set; }
    public string? CommodityID { get; set; }
    public string? ContractNumber { get; set; }

    // Dropdowns (SelectListItem collections)
    public IEnumerable<SelectListItem> Operators { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Customers { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> HullTypes { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> CoverTypes { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> LoadStatuses { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Statuses { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> EquipmentTypes { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> SizeCategories { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Rivers { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Commodities { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> BoatSearchTypes { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> BoatLocations { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> FacilitySearchTypes { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> FacilityLocations { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> ShipSearchTypes { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> ShipLocations { get; set; } = new List<SelectListItem>();

    // Feature Flags
    public bool IsFreightLicenseActive { get; set; }
    public bool IsTerminalLicenseActive { get; set; }
    public bool IsCommodityInfoCustomizationActive { get; set; }

    // Context
    public int SelectedFleetID { get; set; }
    public bool CanModify { get; set; }
}
```

### BargeEditViewModel.cs

**Location:** `src/BargeOps.UI/ViewModels/BargeEditViewModel.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using BargeOps.Shared.Dto;

namespace BargeOpsAdmin.ViewModels;

public class BargeEditViewModel
{
    // ⭐ Contains the DTO directly from BargeOps.Shared
    public BargeDto Barge { get; set; } = new();

    // DateTime Splitting (24-hour format)
    [Display(Name = "In Service Date")]
    [DataType(DataType.Date)]
    public DateTime? InServiceDate { get; set; }

    [Display(Name = "In Service Time")]
    public string? InServiceTime { get; set; }

    [Display(Name = "Out of Service Date")]
    [DataType(DataType.Date)]
    public DateTime? OutOfServiceDate { get; set; }

    [Display(Name = "Out of Service Time")]
    public string? OutOfServiceTime { get; set; }

    [Display(Name = "Location Date/Time")]
    [DataType(DataType.Date)]
    public DateTime? LocationDate { get; set; }

    public string? LocationTime { get; set; }

    // Draft Conversions
    [Display(Name = "Draft (Feet)")]
    [Range(0, 99)]
    public int? DraftFeet { get; set; }

    [Display(Name = "Draft (Inches)")]
    [Range(0, 11)]
    public int? DraftInches { get; set; }

    [Display(Name = "Calculated Draft (Feet)")]
    public int? DraftCalculatedFeet { get; set; }

    [Display(Name = "Calculated Draft (Inches)")]
    public int? DraftCalculatedInches { get; set; }

    // Dropdowns
    public IEnumerable<SelectListItem> Owners { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Operators { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> HullTypes { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> BargeSeries { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> CoverTypes { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> CoverConfigs { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> CoverSubTypes { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Commodities { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> LoadStatuses { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> CleanStatuses { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> RepairStatuses { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> DamageLevels { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> FreeboardRanges { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Fleets { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> EquipmentTypes { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> RakeDirections { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> ColorPairs { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> BargeTypes { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Locations { get; set; } = new List<SelectListItem>();

    // Child Entities
    public List<BargeCharterDto> Charters { get; set; } = new();

    // Feature Flags
    public bool IsBargeSeriesCustomizationActive { get; set; }
    public bool IsBargeCharterSupportCustomizationActive { get; set; }
    public bool IsFreightActive { get; set; }
    public bool IsTerminalMode { get; set; }
    public bool EnableCoverTypeSpecialLogic { get; set; }
    public bool RequireBargeCoverType { get; set; }

    // Permissions
    public bool CanModify { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsNew => Barge.BargeID == 0;
}
```

---

## Next Steps

After generating this conversion plan, I will generate:

1. **Shared DTOs** - Complete DTO classes for BargeOps.Shared
2. **API Layer** - Repository, Service, Controller
3. **UI Layer** - ViewModels, Services, Controller, Views, JavaScript

Would you like me to proceed with generating all the code templates now?
