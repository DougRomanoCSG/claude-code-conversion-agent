using BargeOps.Shared.Common;
using System.ComponentModel.DataAnnotations;

namespace BargeOps.Shared.Dto;

/// <summary>
/// Search request DTO for BargeEvent search with ListQuery support
/// Used by both API and UI for consistent search behavior
/// </summary>
public class BargeEventSearchRequest : ListQueryRequest
{
    /// <summary>
    /// Fleet ID - REQUIRED (all searches are fleet-scoped)
    /// </summary>
    [Required(ErrorMessage = "Fleet ID is required")]
    public int FleetID { get; set; }

    /// <summary>
    /// Event type filter (Load, Unload, Shift, etc.)
    /// </summary>
    public int? EventTypeId { get; set; }

    /// <summary>
    /// Billing customer filter
    /// </summary>
    public int? BillingCustomerId { get; set; }

    /// <summary>
    /// From location filter (Facility or Boat)
    /// </summary>
    public int? FromLocationId { get; set; }

    /// <summary>
    /// To location filter (Facility or Boat)
    /// </summary>
    public int? ToLocationId { get; set; }

    /// <summary>
    /// Fleet boat filter (servicing boat)
    /// </summary>
    public int? FleetBoatId { get; set; }

    /// <summary>
    /// Start date range filter (searches events >= this date)
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date range filter (searches events <= this date)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Include voided/cancelled events in results
    /// Default: false (exclude voided events)
    /// </summary>
    public bool IncludeVoided { get; set; }

    /// <summary>
    /// Contract number filter (contains search)
    /// Freight license required for this filter
    /// </summary>
    public string? ContractNumber { get; set; }

    /// <summary>
    /// Comma-separated list of barge numbers ('begins with' matching)
    /// Example: "1234,5678,9012"
    /// </summary>
    public string? BargeNumberList { get; set; }

    /// <summary>
    /// Ticket customer filter (customer on parent ticket)
    /// </summary>
    public int? TicketCustomerId { get; set; }

    /// <summary>
    /// Freight customer filter
    /// Freight license required for this filter
    /// </summary>
    public int? FreightCustomerId { get; set; }

    /// <summary>
    /// Event rate ID filter (exact match)
    /// </summary>
    public int? EventRateId { get; set; }

    /// <summary>
    /// Validates that at least one search criterion is provided
    /// Prevents overly broad queries that could impact performance
    /// </summary>
    public bool HasAtLeastOneCriterion()
    {
        return EventTypeId.HasValue
            || BillingCustomerId.HasValue
            || FromLocationId.HasValue
            || ToLocationId.HasValue
            || FleetBoatId.HasValue
            || StartDate.HasValue
            || EndDate.HasValue
            || !string.IsNullOrWhiteSpace(ContractNumber)
            || !string.IsNullOrWhiteSpace(BargeNumberList)
            || TicketCustomerId.HasValue
            || FreightCustomerId.HasValue
            || EventRateId.HasValue;
    }
}
