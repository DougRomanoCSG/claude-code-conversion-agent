using BargeOps.Shared.Dto;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BargeOpsAdmin.ViewModels;

/// <summary>
/// ViewModel for BargeEvent edit/create screen.
/// Contains the BargeEventDto (from Shared) plus UI-specific properties.
/// </summary>
public class BargeEventEditViewModel : BargeOpsAdminBaseModel<BargeEventEditViewModel>, IValidatableObject
{
    // ===== ENTITY DTO (from Shared) =====

    /// <summary>
    /// BargeEvent entity DTO from BargeOps.Shared.
    /// This is the SINGLE SOURCE OF TRUTH - no duplication!
    /// </summary>
    public BargeEventDto BargeEvent { get; set; } = new();

    // ===== DATETIME HELPER PROPERTIES (split for 24-hour time input) =====
    // View splits datetime into separate date + time inputs
    // JavaScript combines them on form submit

    [NotMapped]
    [Display(Name = "Start Date")]
    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [NotMapped]
    [Display(Name = "Start Time")]
    [DataType(DataType.Time)]
    public TimeSpan? StartTime { get; set; }

    [NotMapped]
    [Display(Name = "Complete Date")]
    [DataType(DataType.Date)]
    public DateTime? CompleteDate { get; set; }

    [NotMapped]
    [Display(Name = "Complete Time")]
    [DataType(DataType.Time)]
    public TimeSpan? CompleteTime { get; set; }

    [NotMapped]
    [Display(Name = "Sched Start Date")]
    [DataType(DataType.Date)]
    public DateTime? SchedStartDate { get; set; }

    [NotMapped]
    [Display(Name = "Sched Start Time")]
    [DataType(DataType.Time)]
    public TimeSpan? SchedStartTime { get; set; }

    [NotMapped]
    [Display(Name = "Sched Complete Date")]
    [DataType(DataType.Date)]
    public DateTime? SchedCompleteDate { get; set; }

    [NotMapped]
    [Display(Name = "Sched Complete Time")]
    [DataType(DataType.Time)]
    public TimeSpan? SchedCompleteTime { get; set; }

    [NotMapped]
    [Display(Name = "C/P Date")]
    [DataType(DataType.Date)]
    public DateTime? CpDate { get; set; }

    [NotMapped]
    [Display(Name = "C/P Time")]
    [DataType(DataType.Time)]
    public TimeSpan? CpTime { get; set; }

    [NotMapped]
    [Display(Name = "Release Date")]
    [DataType(DataType.Date)]
    public DateTime? ReleaseDate { get; set; }

    [NotMapped]
    [Display(Name = "Release Time")]
    [DataType(DataType.Time)]
    public TimeSpan? ReleaseTime { get; set; }

    // ===== RELATED DATA (child entities) =====

    /// <summary>
    /// Barges associated with this event
    /// </summary>
    public List<BargeDto> Barges { get; set; } = new();

    /// <summary>
    /// Billing audit trail for this event
    /// </summary>
    public List<BillingAuditDto> BillingAudits { get; set; } = new();

    // ===== DROPDOWN LISTS =====

    public List<SelectListItem> EventTypeList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> CustomerList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> FreightCustomerList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> FleetBoatList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> FromLocationList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> ToLocationList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> CommodityList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> RiverList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> LoadStatusList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> DivisionList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> VendorList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> VendorBusinessUnitList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> ShipList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> RigList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> CoverTypeList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> CoverSubTypeList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> RateTypeList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> ChargeTypeList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> FreightRateTypeList { get; set; } = new List<SelectListItem>();

    // ===== LICENSE FLAGS (control feature visibility) =====

    /// <summary>
    /// Freight license active - controls visibility of freight billing tab
    /// </summary>
    public bool IsFreightActive { get; set; }

    /// <summary>
    /// Onboard license active - controls visibility of onboard order features
    /// </summary>
    public bool IsOnboardActive { get; set; }

    /// <summary>
    /// Terminal license active
    /// </summary>
    public bool IsTerminalActive { get; set; }

    /// <summary>
    /// Towing license active
    /// </summary>
    public bool IsTowingActive { get; set; }

    // ===== PERMISSION FLAGS =====

    /// <summary>
    /// User can modify barge events (create, edit)
    /// </summary>
    public bool CanModify { get; set; }

    /// <summary>
    /// User can only view (read-only mode)
    /// </summary>
    public bool IsReadOnly => !CanModify;

    /// <summary>
    /// User can view billing tabs
    /// </summary>
    public bool CanViewBilling { get; set; }

    /// <summary>
    /// User can modify billing information
    /// </summary>
    public bool CanModifyBilling { get; set; }

    /// <summary>
    /// Special case: Read-only operations with writable billing
    /// Allows billing modifications even when operations are read-only
    /// </summary>
    public bool IsEditingReadonlyOperationsWithWritableBilling =>
        IsReadOnly && CanModifyBilling;

    // ===== VALIDATION HELPERS =====

    /// <summary>
    /// Indicates if this is a new event (not saved yet)
    /// </summary>
    public bool IsNewEvent => BargeEvent.TicketEventID == 0;

    /// <summary>
    /// Indicates if event is already invoiced
    /// </summary>
    public bool IsInvoiced => BargeEvent.IsInvoiced;

    /// <summary>
    /// Indicates if event is voided/cancelled
    /// </summary>
    public bool IsVoid => BargeEvent.IsVoid;

    /// <summary>
    /// At least one barge is required for the event
    /// </summary>
    public bool AtLeastOneBargeRequired => true;

    /// <summary>
    /// Number of barges currently associated with event
    /// </summary>
    public int BargeCount => Barges.Count;

    // ===== UI DISPLAY HELPERS =====

    /// <summary>
    /// Page title based on create/edit mode
    /// </summary>
    public string PageTitle => IsNewEvent ? "Create Barge Event" : $"Edit Barge Event - {BargeEvent.EventTypeName}";

    /// <summary>
    /// Submit button text based on mode
    /// </summary>
    public string SubmitButtonText => IsNewEvent ? "Create Event" : "Update Event";

    /// <summary>
    /// CSS class for submit button
    /// </summary>
    public string SubmitButtonClass => IsNewEvent ? "btn-success" : "btn-primary";

    // ===== COMPLEX VALIDATION =====

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Ticket ID required
        if (BargeEvent.TicketID <= 0)
        {
            yield return new ValidationResult(
                "Ticket ID is required.",
                new[] { nameof(BargeEvent.TicketID) });
        }

        // Event Type required
        if (BargeEvent.EventTypeID <= 0)
        {
            yield return new ValidationResult(
                "Event type is required.",
                new[] { nameof(BargeEvent.EventTypeID) });
        }

        // Start date/time required
        if (BargeEvent.StartDateTime == default)
        {
            yield return new ValidationResult(
                "Start date/time is required.",
                new[] { nameof(BargeEvent.StartDateTime) });
        }

        // Complete date/time must be after start (if provided)
        if (BargeEvent.CompleteDateTime.HasValue &&
            BargeEvent.CompleteDateTime.Value < BargeEvent.StartDateTime)
        {
            yield return new ValidationResult(
                "Complete date/time must be after start date/time.",
                new[] { nameof(BargeEvent.CompleteDateTime) });
        }

        // Billing customer required for billable events
        if (BargeEvent.BillableStatus > 0 && !BargeEvent.BillingCustomerID.HasValue)
        {
            yield return new ValidationResult(
                "Billing customer is required for billable events.",
                new[] { nameof(BargeEvent.BillingCustomerID) });
        }

        // Freight license validations
        if (IsFreightActive)
        {
            // C/P date should be before or equal to release date
            if (BargeEvent.CpDateTime.HasValue &&
                BargeEvent.ReleaseDateTime.HasValue &&
                BargeEvent.CpDateTime.Value > BargeEvent.ReleaseDateTime.Value)
            {
                yield return new ValidationResult(
                    "C/P date/time must be before or equal to release date/time.",
                    new[] { nameof(BargeEvent.CpDateTime) });
            }
        }

        // Division = Outside Vendor requires Vendor
        if (!string.IsNullOrWhiteSpace(BargeEvent.Division) &&
            BargeEvent.Division == "Outside Vendor" &&
            !BargeEvent.VendorID.HasValue)
        {
            yield return new ValidationResult(
                "Vendor is required when Division is 'Outside Vendor'.",
                new[] { nameof(BargeEvent.VendorID) });
        }

        // At least one barge required (checked in controller before save)
        if (AtLeastOneBargeRequired && BargeCount == 0)
        {
            yield return new ValidationResult(
                "At least one barge must be associated with this event.",
                new[] { "Barges" });
        }

        // Prevent modification of invoiced events without rebill flag
        if (!IsNewEvent && IsInvoiced && !BargeEvent.Rebill && CanModify)
        {
            yield return new ValidationResult(
                "Cannot modify invoiced event without rebill flag. Mark for rebill first.",
                new[] { nameof(BargeEvent.Rebill) });
        }
    }
}
