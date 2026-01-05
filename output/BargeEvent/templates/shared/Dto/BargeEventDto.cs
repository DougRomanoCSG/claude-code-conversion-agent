using BargeOps.Shared.Attributes;
using System.ComponentModel.DataAnnotations;

namespace BargeOps.Shared.Dto;

/// <summary>
/// BargeEvent entity DTO (wraps TicketEvent table) - used by both API and UI
/// Represents a barge event including loads, unloads, shifts, fleeting, midstream operations
/// NOTE: This is a composite object that coordinates TicketEvent and multiple child entities
/// </summary>
public class BargeEventDto
{
    // ===== PRIMARY KEY =====
    [Sortable]
    [Filterable]
    public int TicketEventID { get; set; }

    // ===== FOREIGN KEYS =====
    [Required(ErrorMessage = "Ticket ID is required")]
    [Sortable]
    [Filterable]
    public int TicketID { get; set; }

    [Required(ErrorMessage = "Event type is required")]
    [Display(Name = "Event Type")]
    [Sortable]
    [Filterable]
    public int EventTypeID { get; set; }

    [Display(Name = "Fleet")]
    [Sortable]
    [Filterable]
    public int? FleetID { get; set; }

    [Display(Name = "Billing Customer")]
    [Sortable]
    [Filterable]
    public int? BillingCustomerID { get; set; }

    [Display(Name = "From Location")]
    [Sortable]
    [Filterable]
    public int? FromLocationID { get; set; }

    [Display(Name = "To Location")]
    [Sortable]
    [Filterable]
    public int? ToLocationID { get; set; }

    [Display(Name = "Fleet Boat")]
    [Sortable]
    [Filterable]
    public int? FleetBoatID { get; set; }

    [Display(Name = "Commodity")]
    [Sortable]
    [Filterable]
    public int? CommodityID { get; set; }

    [Display(Name = "Event Rate")]
    [Sortable]
    [Filterable]
    public int? EventRateID { get; set; }

    [Display(Name = "Vendor")]
    [Sortable]
    [Filterable]
    public int? VendorID { get; set; }

    [Display(Name = "Vendor Business Unit")]
    [Sortable]
    [Filterable]
    public int? VendorBusinessUnitID { get; set; }

    [Display(Name = "Destination Location")]
    public int? DestinationLocationID { get; set; }

    [Display(Name = "Destination River Area")]
    public int? DestinationRiverAreaID { get; set; }

    [Display(Name = "Ship")]
    public int? ShipID { get; set; }

    [Display(Name = "Rig")]
    public int? RigID { get; set; }

    [Display(Name = "Cover Sub-Type")]
    public int? CoverSubTypeID { get; set; }

    [Display(Name = "Document")]
    public int? DocumentID { get; set; }

    [Display(Name = "Invoice")]
    public int? InvoiceID { get; set; }

    // ===== STATUS FIELDS =====
    [Display(Name = "Status")]
    [Sortable]
    [Filterable]
    public short Status { get; set; }

    [Display(Name = "Billable Status")]
    [Sortable]
    [Filterable]
    public byte BillableStatus { get; set; }

    [Display(Name = "Void Status")]
    [Sortable]
    [Filterable]
    public byte VoidStatus { get; set; }  // Soft delete field

    [StringLength(50)]
    [Display(Name = "Load Status")]
    [Sortable]
    [Filterable]
    public string? LoadStatus { get; set; }

    // ===== DATE/TIME FIELDS =====
    // NOTE: UI splits these into separate date + time inputs (24-hour format)
    [Required(ErrorMessage = "Start date/time is required")]
    [Display(Name = "Start Date/Time")]
    [Sortable]
    public DateTime StartDateTime { get; set; }

    [Display(Name = "Complete Date/Time")]
    [Sortable]
    public DateTime? CompleteDateTime { get; set; }

    [Display(Name = "Scheduled Start")]
    [Sortable]
    public DateTime? SchedStartDateTime { get; set; }

    [Display(Name = "Scheduled Complete")]
    [Sortable]
    public DateTime? SchedCompleteDateTime { get; set; }

    [Display(Name = "C/P Date/Time")]
    [Sortable]
    public DateTime? CpDateTime { get; set; }  // Charter Party - freight license

    [Display(Name = "Release Date/Time")]
    [Sortable]
    public DateTime? ReleaseDateTime { get; set; }  // Freight license

    [Display(Name = "Wait Date/Time")]
    public DateTime? WaitDateTime { get; set; }

    [Display(Name = "Tank Docked")]
    public DateTime? TankDockedDateTime { get; set; }

    [Display(Name = "Tank Cargo Hose On")]
    public DateTime? TankCargoHoseOnDateTime { get; set; }

    [Display(Name = "Tank Start Transfer")]
    public DateTime? TankStartTransferDateTime { get; set; }

    [Display(Name = "Tank Finished Transfer")]
    public DateTime? TankFinishedTransferDateTime { get; set; }

    [Display(Name = "Tank Cargo Hose Off")]
    public DateTime? TankCargoHoseOffDateTime { get; set; }

    [Display(Name = "Tank Barge Released")]
    public DateTime? TankBargeReleasedDateTime { get; set; }

    [Display(Name = "External Invoice Date")]
    public DateTime? ExternalInvoiceDate { get; set; }

    // ===== BILLING FIELDS =====
    [StringLength(50)]
    [Display(Name = "Rate Type")]
    [Sortable]
    public string? RateType { get; set; }

    [Display(Name = "Base Rate")]
    [Range(0, 999999.99, ErrorMessage = "Base rate must be between 0 and 999,999.99")]
    public decimal? BaseRate { get; set; }

    [Display(Name = "Charge Type")]
    public byte? ChargeType { get; set; }  // Per Barge, Per Hour, Per Ton

    [Display(Name = "Minimum")]
    public int? Minimum { get; set; }

    [Display(Name = "Minimum Amount")]
    public decimal? MinimumAmount { get; set; }

    [Display(Name = "Prorate Units")]
    public int? ProrateUnits { get; set; }

    [Display(Name = "Free Hours")]
    public decimal? FreeHours { get; set; }

    [Display(Name = "High Water Rate")]
    public decimal? HighWaterRate { get; set; }

    [Display(Name = "High Water Amount")]
    public decimal? HighWaterAmount { get; set; }

    [Display(Name = "Fuel Escalation Rate")]
    public decimal? FuelEscalationRate { get; set; }

    [Display(Name = "Fuel Escalation Amount")]
    public decimal? FuelEscalationAmount { get; set; }

    [Display(Name = "Labor Escalation Rate")]
    public decimal? LaborEscalationRate { get; set; }

    [Display(Name = "Labor Escalation Amount")]
    public decimal? LaborEscalationAmount { get; set; }

    [Display(Name = "Base Amount")]
    public decimal? BaseAmount { get; set; }

    [Display(Name = "Base Fleet Days")]
    public int? BaseFleetDays { get; set; }

    [Display(Name = "Extended Fleet Days 1")]
    public int? ExtFleetDays1 { get; set; }

    [Display(Name = "Extended Fleet Rate 1")]
    public decimal? ExtFleetRate1 { get; set; }

    [Display(Name = "Extended Fleet Amount 1")]
    public decimal? ExtFleetAmount1 { get; set; }

    [Display(Name = "Extended Fleet Days 2")]
    public int? ExtFleetDays2 { get; set; }

    [Display(Name = "Extended Fleet Rate 2")]
    public decimal? ExtFleetRate2 { get; set; }

    [Display(Name = "Extended Fleet Amount 2")]
    public decimal? ExtFleetAmount2 { get; set; }

    [Display(Name = "Total Amount")]
    public decimal? TotalAmount { get; set; }

    [Display(Name = "Max Fleeting Days")]
    public int? MaxFleetingDays { get; set; }

    [Display(Name = "Apply Free Max Fleeting Days Per Event")]
    public bool ApplyFreeMaxFleetingDaysPerEvent { get; set; }

    [StringLength(1000)]
    [Display(Name = "Invoice Note")]
    public string? InvoiceNote { get; set; }

    // ===== FREIGHT BILLING FIELDS (License-dependent) =====
    [Display(Name = "Freight Rate")]
    public decimal? FreightRate { get; set; }

    [Display(Name = "Freight Quantity")]
    public decimal? FreightQuantity { get; set; }

    [Display(Name = "Freight Amount")]
    public decimal? FreightAmount { get; set; }

    [Display(Name = "Freight Allowance Rate")]
    public decimal? FreightAllowanceRate { get; set; }

    [Display(Name = "Freight Allowance Quantity")]
    public decimal? FreightAllowanceQuantity { get; set; }

    [Display(Name = "Freight Allowance Amount")]
    public decimal? FreightAllowanceAmount { get; set; }

    [Display(Name = "Freight Total Amount")]
    public decimal? FreightTotalAmount { get; set; }

    [Display(Name = "Freight Minimum Quantity")]
    public decimal? FreightMinimumQuantity { get; set; }

    [Display(Name = "Is Manual Freight Rate")]
    public bool IsManualFreightRate { get; set; }

    [StringLength(50)]
    [Display(Name = "Freight Rate Type")]
    public string? FreightRateType { get; set; }

    [StringLength(1000)]
    [Display(Name = "Freight Invoice Note")]
    public string? FreightInvoiceNote { get; set; }

    // ===== DRAFT FIELDS =====
    [Display(Name = "Barge Draft")]
    public decimal? BargeDraft { get; set; }

    [Display(Name = "Port Bow Draft")]
    public decimal? BargeDraftPortBow { get; set; }

    [Display(Name = "Port Stern Draft")]
    public decimal? BargeDraftPortStern { get; set; }

    [Display(Name = "Starboard Bow Draft")]
    public decimal? BargeDraftStarboardBow { get; set; }

    [Display(Name = "Starboard Stern Draft")]
    public decimal? BargeDraftStarboardStern { get; set; }

    [Display(Name = "Draft Requested")]
    public decimal? DraftRequested { get; set; }

    [Display(Name = "Draft Calculated")]
    public decimal? DraftCalculated { get; set; }

    [Display(Name = "Draft Average")]
    public decimal? DraftAverage { get; set; }

    // ===== OTHER NUMERIC FIELDS =====
    [Display(Name = "Adjustment Tons")]
    public decimal? AdjustmentTons { get; set; }

    [Display(Name = "Barges In Tow Count")]
    public int? BargesInTowCount { get; set; }

    [Display(Name = "Divide By Barges In Tow")]
    public bool DivideByBargesInTow { get; set; }

    [Display(Name = "Towing Tons")]
    public decimal? TowingTons { get; set; }

    [Display(Name = "Towing Distance (Miles)")]
    public decimal? TowingDistanceMiles { get; set; }

    [Display(Name = "Apply Tow Averaging")]
    public bool ApplyTowAveraging { get; set; }

    [Display(Name = "Tank Quantity Transferred")]
    public decimal? TankQuantityTransferred { get; set; }

    [StringLength(50)]
    [Display(Name = "Tank Quantity Units")]
    public string? TankQuantityUnits { get; set; }

    // ===== STRING FIELDS =====
    [StringLength(50)]
    [Display(Name = "Division")]
    [Sortable]
    [Filterable]
    public string? Division { get; set; }

    [StringLength(50)]
    [Display(Name = "Cover Type")]
    public string? CoverType { get; set; }

    [StringLength(50)]
    [Display(Name = "Cover Config")]
    public string? CoverConfig { get; set; }

    [StringLength(100)]
    [Display(Name = "Train Number")]
    public string? TrainNum { get; set; }

    [StringLength(100)]
    [Display(Name = "Surveyor")]
    public string? Surveyor { get; set; }

    [StringLength(100)]
    [Display(Name = "BargeEx Vendor Control Number")]
    public string? BargeExVendorControlNumber { get; set; }

    [StringLength(100)]
    [Display(Name = "External Invoice Number")]
    public string? ExternalInvoiceNumber { get; set; }

    // ===== BOOLEAN FLAGS =====
    [Display(Name = "Is Tramp Tow")]
    public bool IsTrampTow { get; set; }

    [Display(Name = "Is Default Tons")]
    public bool IsDefaultTons { get; set; }

    [Display(Name = "Rebill")]
    public bool Rebill { get; set; }

    [Display(Name = "Rebill Override")]
    public bool RebillOverride { get; set; }

    [Display(Name = "Is Invoiced Externally")]
    public bool IsInvoicedExternally { get; set; }

    [Display(Name = "Is Port Shift")]
    public bool IsPortShift { get; set; }

    // ===== AUDIT FIELDS =====
    [Display(Name = "Created")]
    [Sortable]
    public DateTime CreateDateTime { get; set; }

    [StringLength(50)]
    [Display(Name = "Created By")]
    public string? CreateUser { get; set; }

    [Display(Name = "Modified")]
    [Sortable]
    public DateTime ModifyDateTime { get; set; }

    [StringLength(50)]
    [Display(Name = "Modified By")]
    public string? ModifyUser { get; set; }

    // ===== COMPUTED/DISPLAY FIELDS (not in database) =====
    /// <summary>
    /// Event type name (from EventType lookup) - populated by repository
    /// </summary>
    [Display(Name = "Event Type")]
    public string? EventTypeName { get; set; }

    /// <summary>
    /// Billing customer name (from Customer lookup) - populated by repository
    /// </summary>
    [Display(Name = "Billing Customer")]
    public string? BillingCustomerName { get; set; }

    /// <summary>
    /// Fleet boat name (from Boat lookup) - populated by repository
    /// </summary>
    [Display(Name = "Fleet Boat")]
    public string? FleetBoatName { get; set; }

    /// <summary>
    /// From location name - populated by repository
    /// </summary>
    [Display(Name = "From Location")]
    public string? FromLocationName { get; set; }

    /// <summary>
    /// To location name - populated by repository
    /// </summary>
    [Display(Name = "To Location")]
    public string? ToLocationName { get; set; }

    /// <summary>
    /// Commodity name - populated by repository
    /// </summary>
    [Display(Name = "Commodity")]
    public string? CommodityName { get; set; }

    /// <summary>
    /// Vendor name - populated by repository
    /// </summary>
    [Display(Name = "Vendor")]
    public string? VendorName { get; set; }

    /// <summary>
    /// Indicates if this event is void/cancelled
    /// </summary>
    public bool IsVoid => VoidStatus > 0;

    /// <summary>
    /// Indicates if this event is invoiced
    /// </summary>
    public bool IsInvoiced => InvoiceID.HasValue && InvoiceID.Value > 0;
}
