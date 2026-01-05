using System.ComponentModel.DataAnnotations;
using BargeOps.Shared.Attributes;

namespace BargeOps.Shared.Dto;

/// <summary>
/// Barge entity DTO - Complete data transfer object used by BOTH API and UI.
/// ⭐ This DTO is the ONLY data model for Barge - no separate domain model exists.
/// </summary>
public class BargeDto
{
    #region Primary Key

    [Sortable]
    [Filterable]
    public int BargeID { get; set; }

    #endregion

    #region Identifiers

    /// <summary>
    /// Barge number - Primary identifier (Required)
    /// </summary>
    [Required(ErrorMessage = "Barge number is required")]
    [StringLength(50, ErrorMessage = "Barge number cannot exceed 50 characters")]
    [Sortable]
    [Filterable]
    public string BargeNum { get; set; } = string.Empty;

    /// <summary>
    /// USCG (US Coast Guard) registration number
    /// </summary>
    [StringLength(50, ErrorMessage = "USCG number cannot exceed 50 characters")]
    [Sortable]
    [Filterable]
    public string? UscgNum { get; set; }

    /// <summary>
    /// Hull identification number
    /// </summary>
    [StringLength(50, ErrorMessage = "Hull number cannot exceed 50 characters")]
    public string? HullNumber { get; set; }

    /// <summary>
    /// General ledger account number
    /// </summary>
    [StringLength(50, ErrorMessage = "GL account number cannot exceed 50 characters")]
    public string? GlAccountNum { get; set; }

    #endregion

    #region Physical Characteristics

    /// <summary>
    /// Hull type (single character code from ValidationList)
    /// Example: 'B' = Box, 'D' = Deck, 'H' = Hopper, 'O' = Open, 'T' = Tank
    /// </summary>
    [Sortable]
    [Filterable]
    [StringLength(1)]
    public string? HullType { get; set; }

    /// <summary>
    /// External length in feet
    /// Auto-triggers SizeCategory calculation when changed with ExternalWidth
    /// </summary>
    [Range(0.0, 50000.0, ErrorMessage = "External length must be between 0 and 50,000 feet")]
    public decimal? ExternalLength { get; set; }

    /// <summary>
    /// External width in feet
    /// Auto-triggers SizeCategory calculation when changed with ExternalLength
    /// </summary>
    [Range(0.0, 20000.0, ErrorMessage = "External width must be between 0 and 20,000 feet")]
    public decimal? ExternalWidth { get; set; }

    /// <summary>
    /// External depth in feet
    /// </summary>
    [Range(0.0, 10000.0, ErrorMessage = "External depth must be between 0 and 10,000 feet")]
    public decimal? ExternalDepth { get; set; }

    /// <summary>
    /// Size category (auto-calculated from ExternalLength and ExternalWidth)
    /// Required when EquipmentType != 'fleet-owned' AND NOT IsTerminalActive
    /// </summary>
    [Sortable]
    [Filterable]
    [StringLength(2)]
    public string? SizeCategory { get; set; }

    /// <summary>
    /// Cover type code from ValidationList
    /// Example: 'R' = Roll, 'H' = Hinged, 'OT' = Open Top, 'S' = Sliding
    /// Required when RequireBargeCoverType OR (IsRequiredCoverForCommodity AND IsCompanyOperatedBarge)
    /// </summary>
    [Sortable]
    [Filterable]
    [StringLength(2)]
    public string? CoverType { get; set; }

    /// <summary>
    /// Cover configuration code from ValidationList
    /// Example: 'F' = Full, 'P' = Partial, 'N' = None
    /// Required when EnableCoverTypeSpecialLogic AND IsCompanyOperatedBarge AND CoverType is not null AND CoverType != 'OT'
    /// </summary>
    [Filterable]
    [StringLength(1)]
    public string? CoverConfig { get; set; }

    /// <summary>
    /// Cover sub-type (FK to CoverSubType entity)
    /// Dynamically required based on commodity and cover type
    /// </summary>
    public int? CoverSubTypeID { get; set; }

    /// <summary>
    /// Cover sub-type name (navigation property for display)
    /// </summary>
    public string? CoverSubTypeName { get; set; }

    /// <summary>
    /// Barge type code from ValidationList
    /// Example: 'J' = Jumbo, 'S' = Standard, 'M' = Mini
    /// </summary>
    [StringLength(1)]
    public string? BargeType { get; set; }

    #endregion

    #region Draft Measurements

    /// <summary>
    /// Overall draft in feet (decimal format: 12.5 = 12 feet 6 inches)
    /// Read-only if corner drafts are entered
    /// </summary>
    [Range(0.0, 99.999, ErrorMessage = "Draft must be between 0 and 99.999 feet")]
    public decimal? Draft { get; set; }

    /// <summary>
    /// Port bow corner draft in feet
    /// </summary>
    [Range(0.0, 99.999, ErrorMessage = "Draft must be between 0 and 99.999 feet")]
    public decimal? DraftPortBow { get; set; }

    /// <summary>
    /// Port stern corner draft in feet
    /// </summary>
    [Range(0.0, 99.999, ErrorMessage = "Draft must be between 0 and 99.999 feet")]
    public decimal? DraftPortStern { get; set; }

    /// <summary>
    /// Starboard bow corner draft in feet
    /// </summary>
    [Range(0.0, 99.999, ErrorMessage = "Draft must be between 0 and 99.999 feet")]
    public decimal? DraftStarboardBow { get; set; }

    /// <summary>
    /// Starboard stern corner draft in feet
    /// </summary>
    [Range(0.0, 99.999, ErrorMessage = "Draft must be between 0 and 99.999 feet")]
    public decimal? DraftStarboardStern { get; set; }

    /// <summary>
    /// Calculated draft from corner drafts (freight license feature)
    /// Read-only calculated field
    /// </summary>
    [Range(0.0, 99.999, ErrorMessage = "Draft must be between 0 and 99.999 feet")]
    public decimal? DraftCalculated { get; set; }

    #endregion

    #region Status

    /// <summary>
    /// Equipment type code from ValidationList (Required)
    /// Example: 'F' = Fleet-owned, 'C' = Customer-owned, 'O' = Other
    /// Controls which fields are enabled/disabled (Status, Damage/Repair groups)
    /// </summary>
    [Required(ErrorMessage = "Equipment type is required")]
    [Sortable]
    [Filterable]
    [StringLength(1)]
    public string EquipmentType { get; set; } = string.Empty;

    /// <summary>
    /// Fleet ID (FK to Fleet entity)
    /// Required when EquipmentType = 'Fleet-owned'
    /// Auto-set to SelectedFleetID when EquipmentType changes to 'fleet-owned'
    /// </summary>
    [Filterable]
    public int? FleetID { get; set; }

    /// <summary>
    /// Fleet name (navigation property for display)
    /// </summary>
    public string? FleetName { get; set; }

    /// <summary>
    /// Owner ID (FK to Customer entity)
    /// Equipment owner
    /// </summary>
    [Filterable]
    public int? OwnerID { get; set; }

    /// <summary>
    /// Owner name (navigation property for display)
    /// </summary>
    public string? OwnerName { get; set; }

    /// <summary>
    /// Customer ID (FK to Customer entity)
    /// Customer operator - affects cover type/config validation logic
    /// Required when EquipmentType != 'fleet-owned' AND NOT IsTerminalActive
    /// </summary>
    [Filterable]
    public int? CustomerID { get; set; }

    /// <summary>
    /// Customer name (navigation property for display)
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Location ID (FK to BargeLocation entity)
    /// Current barge location - automatically updates Status when changed
    /// </summary>
    [Filterable]
    public int? LocationID { get; set; }

    /// <summary>
    /// Location name (navigation property for display)
    /// </summary>
    public string? LocationName { get; set; }

    /// <summary>
    /// Timestamp when location was last updated
    /// Auto-set when LocationID changes
    /// </summary>
    public DateTime? LocationDateTime { get; set; }

    /// <summary>
    /// Status code (computed from LocationID)
    /// Read-only calculated field
    /// </summary>
    [Sortable]
    [Filterable]
    [StringLength(2)]
    public string? Status { get; set; }

    /// <summary>
    /// Latest fleet boat ID (FK to FleetBoat entity)
    /// Latest fleet boat activity - auto-updates FleetBoatActivityDateTime when set
    /// </summary>
    public int? FleetBoatID { get; set; }

    /// <summary>
    /// Fleet boat name (navigation property for display)
    /// </summary>
    public string? FleetBoatName { get; set; }

    /// <summary>
    /// Fleet boat activity timestamp
    /// Auto-set to DateTime.Now when FleetBoatID changes
    /// </summary>
    public DateTime? FleetBoatActivityDateTime { get; set; }

    /// <summary>
    /// Load status code from ValidationList
    /// Example: 'E' = Empty, 'L' = Loaded, 'P' = Partial
    /// Disabled for fleet-owned equipment
    /// </summary>
    [Sortable]
    [Filterable]
    [StringLength(1)]
    public string? LoadStatus { get; set; }

    /// <summary>
    /// Commodity ID (FK to Commodity entity)
    /// Current commodity - affects cover type/subtype requirements
    /// Cleared on submit for fleet-owned equipment
    /// </summary>
    [Filterable]
    public int? CommodityID { get; set; }

    /// <summary>
    /// Commodity name (navigation property for display)
    /// </summary>
    public string? CommodityName { get; set; }

    /// <summary>
    /// Clean status code from ValidationList
    /// Example: 'C' = Clean, 'D' = Dirty, 'U' = Unknown
    /// Cleared on submit for fleet-owned equipment
    /// </summary>
    [Filterable]
    [StringLength(1)]
    public string? CleanStatus { get; set; }

    /// <summary>
    /// Color pair ID (FK to ColorPair entity)
    /// Barge token colors for fleet
    /// Enabled only for 'Fleet-owned' or 'Customer-owned' equipment types
    /// </summary>
    public int? ColorPairID { get; set; }

    /// <summary>
    /// Color pair description (navigation property for display)
    /// </summary>
    public string? ColorPairDescription { get; set; }

    #endregion

    #region Freeboard

    /// <summary>
    /// Indicates if barge has insufficient freeboard
    /// Enables FreeboardRange dropdown when checked
    /// </summary>
    public bool HasInsufficientFreeboard { get; set; }

    /// <summary>
    /// Freeboard range code from ValidationList
    /// Enabled only when HasInsufficientFreeboard is true
    /// </summary>
    [StringLength(1)]
    public string? FreeboardRange { get; set; }

    #endregion

    #region Service Dates

    /// <summary>
    /// Date barge was put into service
    /// </summary>
    public DateTime? InServiceDate { get; set; }

    /// <summary>
    /// Date barge was taken out of service
    /// </summary>
    public DateTime? OutOfServiceDate { get; set; }

    #endregion

    #region Damage and Repair

    /// <summary>
    /// Repair status code from ValidationList
    /// Example: 'N' = None, 'S' = Scheduled, 'I' = In Progress, 'C' = Complete
    /// Cleared on submit for fleet-owned equipment
    /// </summary>
    [Filterable]
    [StringLength(1)]
    public string? RepairStatus { get; set; }

    /// <summary>
    /// Damage level code from ValidationList
    /// Example: 'N' = None, 'L' = Light, 'M' = Medium, 'H' = Heavy
    /// Cleared on submit for fleet-owned equipment
    /// </summary>
    [Filterable]
    [StringLength(1)]
    public string? DamageLevel { get; set; }

    /// <summary>
    /// Damage note (free text)
    /// Enabled only when IsDamaged is true
    /// Cleared on submit if IsDamaged is false or for fleet-owned equipment
    /// </summary>
    [StringLength(500, ErrorMessage = "Damage note cannot exceed 500 characters")]
    public string? DamageNote { get; set; }

    /// <summary>
    /// Indicates if barge is damaged
    /// Enables DamageNote textbox when checked
    /// Cleared on submit for fleet-owned equipment
    /// </summary>
    public bool IsDamaged { get; set; }

    /// <summary>
    /// Indicates if cargo is damaged
    /// Cleared on submit for fleet-owned equipment
    /// </summary>
    public bool IsCargoDamaged { get; set; }

    /// <summary>
    /// Indicates if barge is a leaker
    /// Cleared on submit for fleet-owned equipment
    /// </summary>
    [Filterable]
    public bool IsLeaker { get; set; }

    /// <summary>
    /// Indicates if barge is dry docked
    /// Cleared on submit for fleet-owned equipment
    /// </summary>
    public bool IsDryDocked { get; set; }

    /// <summary>
    /// Indicates if repair is scheduled
    /// Cleared on submit for fleet-owned equipment
    /// </summary>
    public bool IsRepairScheduled { get; set; }

    #endregion

    #region Barge Series

    /// <summary>
    /// Barge series ID (FK to BargeSeries entity)
    /// Setting this auto-populates: OwnerID, CustomerID, HullType, ExternalDepth, ExternalLength, ExternalWidth, SizeCategory, CoverType
    /// </summary>
    public int? BargeSeriesID { get; set; }

    /// <summary>
    /// Barge series name (navigation property for display)
    /// </summary>
    public string? BargeSeriesName { get; set; }

    #endregion

    #region Position (Tier and Berth)

    /// <summary>
    /// Tier ID (FK to Tier entity)
    /// Tier position for fleet-owned equipment
    /// Set via LocationID property with tier coordinates (TierX, TierY)
    /// Cleared when barge moves locations
    /// </summary>
    public int? TierID { get; set; }

    /// <summary>
    /// Tier name (navigation property for display)
    /// </summary>
    public string? TierName { get; set; }

    /// <summary>
    /// Tier X coordinate
    /// </summary>
    public short? TierX { get; set; }

    /// <summary>
    /// Tier Y coordinate
    /// </summary>
    public short? TierY { get; set; }

    /// <summary>
    /// Facility berth ID (FK to FacilityBerth entity)
    /// Berth position with coordinates (FacilityBerthX, FacilityBerthY)
    /// Must match barge's current LocationID or gets cleared
    /// </summary>
    public int? FacilityBerthID { get; set; }

    /// <summary>
    /// Facility berth name (navigation property for display)
    /// </summary>
    public string? FacilityBerthName { get; set; }

    /// <summary>
    /// Facility berth X coordinate
    /// </summary>
    public short? FacilityBerthX { get; set; }

    /// <summary>
    /// Facility berth Y coordinate
    /// </summary>
    public short? FacilityBerthY { get; set; }

    #endregion

    #region Other Properties

    /// <summary>
    /// Rake direction
    /// </summary>
    [StringLength(10)]
    public string? RakeDirection { get; set; }

    /// <summary>
    /// Tow string position
    /// </summary>
    public int? TowString { get; set; }

    /// <summary>
    /// Tow cut position
    /// </summary>
    public int? TowCut { get; set; }

    /// <summary>
    /// Sync identifier for mobile/external systems
    /// </summary>
    public Guid BargeSyncIdentifier { get; set; }

    /// <summary>
    /// Indicates if barge has anchor wire
    /// </summary>
    public bool HasAnchorWire { get; set; }

    /// <summary>
    /// Conversion FES barge ID (for data migration)
    /// </summary>
    public int? ConvFesBargeID { get; set; }

    #endregion

    #region Audit Fields

    /// <summary>
    /// Indicates if record is active (soft delete)
    /// </summary>
    [Sortable]
    [Filterable]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Record creation timestamp
    /// </summary>
    [Sortable]
    [Filterable]
    public DateTime CreateDateTime { get; set; }

    /// <summary>
    /// Record last modification timestamp
    /// </summary>
    [Sortable]
    [Filterable]
    public DateTime ModifyDateTime { get; set; }

    /// <summary>
    /// User who created the record
    /// </summary>
    [StringLength(100)]
    public string CreateUser { get; set; } = string.Empty;

    /// <summary>
    /// User who last modified the record
    /// </summary>
    [StringLength(100)]
    public string ModifyUser { get; set; } = string.Empty;

    #endregion

    #region Read-Only Ticket Information (from Ticket relationship)

    /// <summary>
    /// Inspection status from ticket (read-only)
    /// </summary>
    public string? InspectionStatus { get; set; }

    /// <summary>
    /// On hold flag from ticket (read-only)
    /// </summary>
    public bool IsOnHold { get; set; }

    /// <summary>
    /// Destination in name from ticket (read-only)
    /// </summary>
    public string? DestinationInName { get; set; }

    /// <summary>
    /// Load first flag from ticket (read-only)
    /// </summary>
    public bool LoadFirst { get; set; }

    /// <summary>
    /// Load last flag from ticket (read-only)
    /// </summary>
    public bool LoadLast { get; set; }

    /// <summary>
    /// Load multiple lots flag from ticket (read-only)
    /// </summary>
    public bool LoadMultipleLots { get; set; }

    /// <summary>
    /// Destination out name from ticket (read-only)
    /// </summary>
    public string? DestinationOutName { get; set; }

    /// <summary>
    /// Schedule in name from ticket (read-only)
    /// </summary>
    public string? ScheduleInName { get; set; }

    /// <summary>
    /// On order flag from ticket (read-only)
    /// </summary>
    public bool IsOnOrder { get; set; }

    /// <summary>
    /// On order schedule date/time from ticket (read-only)
    /// </summary>
    public DateTime? OnOrderScheduleDateTime { get; set; }

    /// <summary>
    /// On order trip number from ticket (read-only)
    /// </summary>
    public string? OnOrderTripNumber { get; set; }

    /// <summary>
    /// Schedule out name from ticket (read-only)
    /// </summary>
    public string? ScheduleOutName { get; set; }

    /// <summary>
    /// Awaiting pickup flag from ticket (read-only)
    /// </summary>
    public bool IsAwaitingPickup { get; set; }

    /// <summary>
    /// Awaiting pickup ready date/time from ticket (read-only)
    /// </summary>
    public DateTime? AwaitingPickupReadyDateTime { get; set; }

    /// <summary>
    /// Consign location name from ticket (read-only)
    /// Formatted as: '{ConsignedFacility}, {ConsignedTo}' or just one if the other is empty
    /// </summary>
    public string? ConsignLocationName { get; set; }

    #endregion
}
