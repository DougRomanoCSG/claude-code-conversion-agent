using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BargeOpsAdmin.ViewModels;

/// <summary>
/// ViewModel for BargeEvent search/list screen.
/// Contains search criteria and lookup lists for dropdowns.
/// </summary>
public class BargeEventSearchViewModel : BargeOpsAdminBaseModel<BargeEventSearchViewModel>
{
    // ===== SEARCH CRITERIA =====

    [Display(Name = "Event Type")]
    public int? EventTypeId { get; set; }

    [Display(Name = "Billing Customer")]
    public int? BillingCustomerId { get; set; }

    [Display(Name = "From Location")]
    public int? FromLocationId { get; set; }

    [Display(Name = "To Location")]
    public int? ToLocationId { get; set; }

    [Display(Name = "Servicing Boat")]
    public int? FleetBoatId { get; set; }

    [Display(Name = "Start Date")]
    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [Display(Name = "End Date")]
    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    [Display(Name = "Barge Numbers")]
    [StringLength(1000, ErrorMessage = "Barge numbers list cannot exceed 1000 characters")]
    public string? BargeNumberList { get; set; }

    [Display(Name = "Ticket Customer")]
    public int? TicketCustomerId { get; set; }

    [Display(Name = "Freight Customer")]
    public int? FreightCustomerId { get; set; }

    [Display(Name = "Contract Number")]
    [StringLength(50)]
    public string? ContractNumber { get; set; }

    [Display(Name = "Event Rate ID")]
    public int? EventRateId { get; set; }

    [Display(Name = "Include Voided Events")]
    public bool IncludeVoided { get; set; }

    // ===== DROPDOWN LISTS =====

    public List<SelectListItem> EventTypeList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> CustomerList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> FreightCustomerList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> FromLocationList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> ToLocationList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> FleetBoatList { get; set; } = new List<SelectListItem>();

    // ===== LICENSE FLAGS (control feature visibility) =====

    /// <summary>
    /// Freight license active - controls visibility of freight-related fields
    /// </summary>
    public bool IsFreightActive { get; set; }

    /// <summary>
    /// Onboard license active - controls visibility of onboard-related features
    /// </summary>
    public bool IsOnboardActive { get; set; }

    // ===== PERMISSION FLAGS =====

    /// <summary>
    /// User can view barge events
    /// </summary>
    public bool CanView { get; set; } = true;

    /// <summary>
    /// User can modify barge events (create, edit, delete)
    /// </summary>
    public bool CanModify { get; set; }

    /// <summary>
    /// User can view billing tabs and information
    /// </summary>
    public bool CanViewBilling { get; set; }

    /// <summary>
    /// User can modify billing information
    /// </summary>
    public bool CanModifyBilling { get; set; }

    // ===== HELPER PROPERTIES =====

    /// <summary>
    /// Validation message when no search criteria provided
    /// </summary>
    public string SearchValidationMessage =>
        "At least one search criterion is required to prevent overly broad queries.";

    /// <summary>
    /// Checks if at least one search criterion is provided
    /// </summary>
    public bool HasSearchCriteria =>
        EventTypeId.HasValue ||
        BillingCustomerId.HasValue ||
        FromLocationId.HasValue ||
        ToLocationId.HasValue ||
        FleetBoatId.HasValue ||
        StartDate.HasValue ||
        EndDate.HasValue ||
        !string.IsNullOrWhiteSpace(BargeNumberList) ||
        TicketCustomerId.HasValue ||
        FreightCustomerId.HasValue ||
        !string.IsNullOrWhiteSpace(ContractNumber) ||
        EventRateId.HasValue;
}
