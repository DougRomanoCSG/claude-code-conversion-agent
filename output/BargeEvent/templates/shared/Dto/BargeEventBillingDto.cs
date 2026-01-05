using BargeOps.Shared.Attributes;
using System.ComponentModel.DataAnnotations;

namespace BargeOps.Shared.Dto;

/// <summary>
/// DTO for BargeEvent billing search results
/// Used for Ready to Invoice screens and billing reports
/// Includes detailed financial and rate information
/// </summary>
public class BargeEventBillingDto
{
    // ===== PRIMARY KEYS =====
    [Sortable]
    public int TicketEventID { get; set; }

    public int? TicketEventAdjustmentID { get; set; }

    // ===== STATUS FIELDS =====
    [Display(Name = "Ready")]
    [Sortable]
    public bool IsReadyToInvoice { get; set; }

    [Display(Name = "Customer")]
    [Sortable]
    [Filterable]
    public string CustomerName { get; set; } = string.Empty;

    [Display(Name = "Barge")]
    [Sortable]
    [Filterable]
    public string BargeNum { get; set; } = string.Empty;

    [Display(Name = "Size")]
    [Sortable]
    [Filterable]
    public string? SizeCategory { get; set; }

    [Display(Name = "Ticket")]
    [Sortable]
    [Filterable]
    public string TicketID { get; set; } = string.Empty;

    [Display(Name = "Event")]
    [Sortable]
    [Filterable]
    public string EventTypeName { get; set; } = string.Empty;

    [Display(Name = "Start")]
    [Sortable]
    public DateTime? StartDateTime { get; set; }

    [Display(Name = "Complete")]
    [Sortable]
    public DateTime? CompleteDateTime { get; set; }

    [Display(Name = "From")]
    [Sortable]
    [Filterable]
    public string? FromLocationName { get; set; }

    [Display(Name = "To")]
    [Sortable]
    [Filterable]
    public string? ToLocationName { get; set; }

    [Display(Name = "Load Status")]
    [Sortable]
    [Filterable]
    public string? LoadStatus { get; set; }

    [Display(Name = "Ticket Status")]
    [Sortable]
    [Filterable]
    public string? TicketStatus { get; set; }

    [Display(Name = "Adjustment")]
    [Sortable]
    public bool IsAdjustment { get; set; }

    [Display(Name = "Commodity")]
    [Sortable]
    [Filterable]
    public string? CommodityName { get; set; }

    // ===== BILLING FLAGS =====
    [Display(Name = "Not Default")]
    [Sortable]
    public bool NotDefault { get; set; }  // Rate is not using default values

    [Display(Name = "Rate Missing")]
    [Sortable]
    public bool RateMissing { get; set; }  // No rate found for this event

    // ===== BILLING AMOUNTS =====
    [Display(Name = "Total")]
    [Sortable]
    public decimal? TotalAmount { get; set; }

    [Display(Name = "Division")]
    [Sortable]
    [Filterable]
    public string? Division { get; set; }

    [Display(Name = "Base Rate")]
    [Sortable]
    public decimal? BaseRate { get; set; }

    [Display(Name = "Rate Type")]
    [Sortable]
    [Filterable]
    public string? RateType { get; set; }

    [Display(Name = "Minimum")]
    [Sortable]
    public int? Minimum { get; set; }

    [Display(Name = "Min Amount")]
    [Sortable]
    public decimal? MinimumAmount { get; set; }

    [Display(Name = "Free Hours")]
    [Sortable]
    public decimal? FreeHours { get; set; }

    [Display(Name = "Prorate")]
    [Sortable]
    public int? ProrateUnits { get; set; }

    [Display(Name = "Fleet Days")]
    [Sortable]
    public int? FleetingDays { get; set; }

    [Display(Name = "Base Amount")]
    [Sortable]
    public decimal? BaseAmount { get; set; }

    [Display(Name = "Fuel Surcharge")]
    [Sortable]
    public decimal? FuelSurchargeAmount { get; set; }

    [Display(Name = "High Water")]
    [Sortable]
    public decimal? HighWaterAmount { get; set; }

    [Display(Name = "Invoice Note")]
    public string? InvoiceNote { get; set; }

    // ===== AUDIT FIELDS =====
    [Display(Name = "Modified")]
    [Sortable]
    public DateTime? ModifyDateTime { get; set; }

    [Display(Name = "Modified By")]
    [Sortable]
    public string? ModifyUser { get; set; }

    [Display(Name = "GL Account")]
    [Sortable]
    [Filterable]
    public string? GLAccountNum { get; set; }

    // ===== UI HELPERS =====
    /// <summary>
    /// CSS class for highlighting missing rates
    /// </summary>
    public string RowClass
    {
        get
        {
            if (RateMissing)
                return "table-danger";  // Red background
            if (NotDefault)
                return "table-warning"; // Yellow background
            return string.Empty;
        }
    }

    /// <summary>
    /// Icon for ready to invoice status
    /// </summary>
    public string ReadyIcon => IsReadyToInvoice ? "fa-check-circle text-success" : "fa-times-circle text-muted";
}
