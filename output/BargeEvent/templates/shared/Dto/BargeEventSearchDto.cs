using BargeOps.Shared.Attributes;
using System.ComponentModel.DataAnnotations;

namespace BargeOps.Shared.Dto;

/// <summary>
/// Flattened DTO for BargeEvent search grid results
/// Includes joined data from related tables (EventType, Customer, Boat, Location, etc.)
/// Used by both API and UI for search grid display
/// </summary>
public class BargeEventSearchDto
{
    // ===== PRIMARY KEYS =====
    [Sortable]
    [Filterable]
    public int TicketEventID { get; set; }

    [Sortable]
    [Filterable]
    public int EventTypeID { get; set; }

    [Sortable]
    [Filterable]
    public int BargeID { get; set; }  // For navigation to barge detail

    [Sortable]
    [Filterable]
    public int TicketID { get; set; }

    public int FleetID { get; set; }

    // ===== DISPLAY FIELDS =====
    [Display(Name = "Event")]
    [Sortable]
    [Filterable]
    public string EventName { get; set; } = string.Empty;

    [Display(Name = "Barge")]
    [Sortable]
    [Filterable]
    public string BargeNum { get; set; } = string.Empty;

    [Display(Name = "Start Time")]
    [Sortable]
    public DateTime StartDateTime { get; set; }

    [Display(Name = "Complete Time")]
    [Sortable]
    public DateTime? CompleteDateTime { get; set; }

    [Display(Name = "C/P Time")]
    [Sortable]
    public DateTime? CpDateTime { get; set; }  // Freight license only

    [Display(Name = "Release Time")]
    [Sortable]
    public DateTime? ReleaseDateTime { get; set; }  // Freight license only

    [Display(Name = "From Location")]
    [Sortable]
    [Filterable]
    public string? StartLocation { get; set; }

    [Display(Name = "To Location")]
    [Sortable]
    [Filterable]
    public string? EndLocation { get; set; }

    [Display(Name = "Load Status")]
    [Sortable]
    [Filterable]
    public string? LoadStatus { get; set; }

    [Display(Name = "Commodity")]
    [Sortable]
    [Filterable]
    public string? CommodityName { get; set; }

    [Display(Name = "Quantity")]
    [Sortable]
    public decimal? LoadUnloadTons { get; set; }  // Freight license only

    [Display(Name = "DQ")]  // Default Quantity indicator
    [Sortable]
    public bool IsDefaultTons { get; set; }  // Freight license only

    [Display(Name = "Event Customer")]
    [Sortable]
    [Filterable]
    public string? CustomerName { get; set; }  // Billing customer

    [Display(Name = "Ticket Customer")]
    [Sortable]
    [Filterable]
    public string? TicketCustomerName { get; set; }

    [Display(Name = "Freight Customer")]
    [Sortable]
    [Filterable]
    public string? FreightCustomerName { get; set; }  // Freight license only

    [Display(Name = "Contract #")]
    [Sortable]
    [Filterable]
    public string? ContractNumber { get; set; }  // Freight license only

    [Display(Name = "Freight Origin")]
    [Sortable]
    [Filterable]
    public string? FreightOrigin { get; set; }  // Freight license only

    [Display(Name = "Freight Destination")]
    [Sortable]
    [Filterable]
    public string? FreightDestination { get; set; }  // Freight license only

    [Display(Name = "Servicing Boat")]
    [Sortable]
    [Filterable]
    public string? ServicingBoat { get; set; }

    [Display(Name = "Division")]
    [Sortable]
    [Filterable]
    public string? Division { get; set; }

    [Display(Name = "Vendor")]
    [Sortable]
    [Filterable]
    public string? Vendor { get; set; }

    [Display(Name = "Bus Unit")]
    [Sortable]
    [Filterable]
    public string? VendorBusinessUnit { get; set; }

    [Display(Name = "Sched Time")]
    [Sortable]
    public DateTime? SchedTime { get; set; }

    [Display(Name = "Event Time")]
    [Sortable]
    public DateTime? EventTime { get; set; }

    [Display(Name = "Rate ID")]
    [Sortable]
    [Filterable]
    public int? EventRateID { get; set; }

    // ===== BOOLEAN FLAGS =====
    [Display(Name = "Inv")]  // Invoiced indicator
    [Sortable]
    public bool IsInvoiced { get; set; }

    [Display(Name = "Rebill")]
    [Sortable]
    public bool Rebill { get; set; }

    [Display(Name = "Void")]
    [Sortable]
    public bool Void { get; set; }

    [Display(Name = "Port Shift")]
    [Sortable]
    public bool IsPortShift { get; set; }

    // ===== ADDITIONAL METADATA FOR UI =====
    /// <summary>
    /// Indicates if row should be formatted with strikethrough (voided events)
    /// </summary>
    public bool ShouldStrikethrough => Void;

    /// <summary>
    /// CSS class for conditional row formatting
    /// </summary>
    public string RowClass => Void ? "text-decoration-line-through text-muted" : string.Empty;
}
