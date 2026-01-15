using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using BargeOps.Shared.Dto;

namespace BargeOpsAdmin.ViewModels;

/// <summary>
/// ViewModel for Barge edit/create screen
/// ⭐ Contains the BargeDto directly from BargeOps.Shared (no duplication!)
/// </summary>
public class BargeEditViewModel
{
    #region Entity DTO

    /// <summary>
    /// ⭐ The barge entity DTO from BargeOps.Shared - SINGLE SOURCE OF TRUTH
    /// Used directly by both API and UI
    /// </summary>
    public BargeDto Barge { get; set; } = new();

    #endregion

    #region DateTime Splitting for UI (24-hour format)

    // InServiceDate splitting
    [Display(Name = "In Service Date")]
    [DataType(DataType.Date)]
    public DateTime? InServiceDate { get; set; }

    [Display(Name = "In Service Time (24-hour)")]
    public string? InServiceTime { get; set; }

    // OutOfServiceDate splitting
    [Display(Name = "Out of Service Date")]
    [DataType(DataType.Date)]
    public DateTime? OutOfServiceDate { get; set; }

    [Display(Name = "Out of Service Time (24-hour)")]
    public string? OutOfServiceTime { get; set; }

    // LocationDateTime splitting
    [Display(Name = "Location Date")]
    [DataType(DataType.Date)]
    public DateTime? LocationDate { get; set; }

    [Display(Name = "Location Time (24-hour)")]
    public string? LocationTime { get; set; }

    #endregion

    #region Draft Conversions (Feet and Inches UI Fields)

    [Display(Name = "Draft (Feet)")]
    [Range(0, 99, ErrorMessage = "Draft feet must be between 0 and 99")]
    public int? DraftFeet { get; set; }

    [Display(Name = "Draft (Inches)")]
    [Range(0, 11, ErrorMessage = "Draft inches must be between 0 and 11")]
    public int? DraftInches { get; set; }

    [Display(Name = "Calculated Draft (Feet)")]
    public int? DraftCalculatedFeet { get; set; }

    [Display(Name = "Calculated Draft (Inches)")]
    public int? DraftCalculatedInches { get; set; }

    #endregion

    #region Dropdowns (SelectListItem collections)

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

    #endregion

    #region Child Entities

    /// <summary>
    /// Barge charters (loaded separately via AJAX)
    /// Displayed in modal dialog for add/edit
    /// </summary>
    public List<BargeCharterDto> Charters { get; set; } = new();

    #endregion

    #region Read-Only Ticket Information

    /// <summary>
    /// Inspection status from active ticket (read-only)
    /// </summary>
    [Display(Name = "Inspection Status")]
    public string? InspectionStatus { get; set; }

    /// <summary>
    /// Hold flag - Do not release barge to boat (read-only)
    /// </summary>
    [Display(Name = "On Hold")]
    public bool IsOnHold { get; set; }

    /// <summary>
    /// Destination In location name (read-only)
    /// </summary>
    [Display(Name = "Destination In")]
    public string? DestinationInName { get; set; }

    /// <summary>
    /// Load first flag (read-only)
    /// </summary>
    [Display(Name = "Load First")]
    public bool LoadFirst { get; set; }

    /// <summary>
    /// Load last flag (read-only)
    /// </summary>
    [Display(Name = "Load Last")]
    public bool LoadLast { get; set; }

    /// <summary>
    /// Load in multiple lots flag (read-only)
    /// </summary>
    [Display(Name = "Load Multiple Lots")]
    public bool LoadMultipleLots { get; set; }

    /// <summary>
    /// Destination Out location name (read-only)
    /// </summary>
    [Display(Name = "Destination Out")]
    public string? DestinationOutName { get; set; }

    /// <summary>
    /// Scheduled In location name (read-only)
    /// </summary>
    [Display(Name = "Scheduled In")]
    public string? ScheduleInName { get; set; }

    /// <summary>
    /// On order flag (read-only)
    /// </summary>
    [Display(Name = "On Order")]
    public bool IsOnOrder { get; set; }

    /// <summary>
    /// On order schedule date/time (read-only)
    /// </summary>
    [Display(Name = "On Order Schedule")]
    public DateTime? OnOrderScheduleDateTime { get; set; }

    /// <summary>
    /// On order trip number (read-only)
    /// </summary>
    [Display(Name = "On Order Trip Number")]
    public string? OnOrderTripNumber { get; set; }

    /// <summary>
    /// Scheduled Out location name (read-only)
    /// </summary>
    [Display(Name = "Scheduled Out")]
    public string? ScheduleOutName { get; set; }

    /// <summary>
    /// Awaiting pickup flag (read-only)
    /// </summary>
    [Display(Name = "Awaiting Pickup")]
    public bool IsAwaitingPickup { get; set; }

    /// <summary>
    /// Awaiting pickup ready date/time (read-only)
    /// </summary>
    [Display(Name = "Pickup Ready Time")]
    public DateTime? AwaitingPickupReadyDateTime { get; set; }

    /// <summary>
    /// Consigned to location name - combines facility and ship (read-only)
    /// </summary>
    [Display(Name = "Consigned To")]
    public string? ConsignLocationName { get; set; }

    #endregion

    #region Feature Flags and Configuration

    /// <summary>
    /// Barge series customization active (shows series dropdown)
    /// </summary>
    public bool IsBargeSeriesCustomizationActive { get; set; }

    /// <summary>
    /// Barge charter support customization active (shows charters button)
    /// </summary>
    public bool IsBargeCharterSupportCustomizationActive { get; set; }

    /// <summary>
    /// Freight license active (shows calculated draft fields)
    /// </summary>
    public bool IsFreightActive { get; set; }

    /// <summary>
    /// Terminal mode active (affects required field validation)
    /// </summary>
    public bool IsTerminalMode { get; set; }

    /// <summary>
    /// Enable cover type special logic (complex CoverConfig requirements)
    /// </summary>
    public bool EnableCoverTypeSpecialLogic { get; set; }

    /// <summary>
    /// Require barge cover type (makes CoverType required for all barges)
    /// </summary>
    public bool RequireBargeCoverType { get; set; }

    #endregion

    #region Permissions and State

    /// <summary>
    /// User has permission to modify this barge
    /// </summary>
    public bool CanModify { get; set; }

    /// <summary>
    /// Form is in read-only mode
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Indicates if this is a new barge (not yet saved)
    /// </summary>
    public bool IsNew => Barge.BargeID == 0;

    #endregion

    #region Helper Properties for UI Logic

    /// <summary>
    /// Equipment type is Fleet-owned
    /// Controls which sections are enabled/disabled
    /// </summary>
    public bool IsFleetOwned => Barge.EquipmentType?.ToLower() == "fleet-owned";

    /// <summary>
    /// Equipment type is Customer-owned
    /// Affects ColorPairID enablement
    /// </summary>
    public bool IsCustomerOwned => Barge.EquipmentType?.ToLower() == "customer-owned";

    /// <summary>
    /// Can edit status fields
    /// Disabled for fleet-owned equipment
    /// </summary>
    public bool CanEditStatus => !IsFleetOwned;

    /// <summary>
    /// Can edit damage/repair fields
    /// Disabled for fleet-owned equipment
    /// </summary>
    public bool CanEditDamageRepair => !IsFleetOwned;

    /// <summary>
    /// Can edit ticket detail fields
    /// Always read-only (from Ticket entity)
    /// </summary>
    public bool CanEditTicketDetails => false;

    /// <summary>
    /// CoverConfig is required based on special logic
    /// </summary>
    public bool IsCoverConfigRequired =>
        EnableCoverTypeSpecialLogic &&
        !string.IsNullOrWhiteSpace(Barge.CoverType) &&
        Barge.CoverType.ToUpper() != "OT" &&
        IsCompanyOperated;

    /// <summary>
    /// Barge is company-operated (affects cover type logic)
    /// TODO: This would check if CustomerID matches company customer
    /// </summary>
    public bool IsCompanyOperated => Barge.CustomerID.HasValue;

    #endregion

    #region Helper Methods

    /// <summary>
    /// Load ViewModel from BargeDto
    /// Splits combined DateTime fields into Date + Time
    /// Splits Draft decimal into Feet + Inches
    /// </summary>
    public void LoadFromBarge(BargeDto barge)
    {
        Barge = barge;

        // Split InServiceDate
        if (barge.InServiceDate.HasValue)
        {
            InServiceDate = barge.InServiceDate.Value.Date;
            InServiceTime = barge.InServiceDate.Value.ToString("HH:mm");
        }

        // Split OutOfServiceDate
        if (barge.OutOfServiceDate.HasValue)
        {
            OutOfServiceDate = barge.OutOfServiceDate.Value.Date;
            OutOfServiceTime = barge.OutOfServiceDate.Value.ToString("HH:mm");
        }

        // Split LocationDateTime
        if (barge.LocationDateTime.HasValue)
        {
            LocationDate = barge.LocationDateTime.Value.Date;
            LocationTime = barge.LocationDateTime.Value.ToString("HH:mm");
        }

        // Split Draft into feet and inches
        if (barge.Draft.HasValue)
        {
            DraftFeet = (int)Math.Floor(barge.Draft.Value);
            DraftInches = (int)Math.Round((barge.Draft.Value - DraftFeet.Value) * 12);
        }

        // Split DraftCalculated into feet and inches
        if (barge.DraftCalculated.HasValue)
        {
            DraftCalculatedFeet = (int)Math.Floor(barge.DraftCalculated.Value);
            DraftCalculatedInches = (int)Math.Round((barge.DraftCalculated.Value - DraftCalculatedFeet.Value) * 12);
        }
    }

    /// <summary>
    /// Save ViewModel to BargeDto
    /// Combines Date + Time fields into DateTime
    /// Combines Feet + Inches into Draft decimal
    /// </summary>
    public void SaveToBarge()
    {
        // Combine InServiceDate + InServiceTime
        if (InServiceDate.HasValue)
        {
            var time = TimeSpan.Zero;
            if (!string.IsNullOrWhiteSpace(InServiceTime) && TimeSpan.TryParse(InServiceTime, out var parsedTime))
            {
                time = parsedTime;
            }
            Barge.InServiceDate = InServiceDate.Value.Date + time;
        }
        else
        {
            Barge.InServiceDate = null;
        }

        // Combine OutOfServiceDate + OutOfServiceTime
        if (OutOfServiceDate.HasValue)
        {
            var time = TimeSpan.Zero;
            if (!string.IsNullOrWhiteSpace(OutOfServiceTime) && TimeSpan.TryParse(OutOfServiceTime, out var parsedTime))
            {
                time = parsedTime;
            }
            Barge.OutOfServiceDate = OutOfServiceDate.Value.Date + time;
        }
        else
        {
            Barge.OutOfServiceDate = null;
        }

        // Combine LocationDate + LocationTime
        if (LocationDate.HasValue)
        {
            var time = TimeSpan.Zero;
            if (!string.IsNullOrWhiteSpace(LocationTime) && TimeSpan.TryParse(LocationTime, out var parsedTime))
            {
                time = parsedTime;
            }
            Barge.LocationDateTime = LocationDate.Value.Date + time;
        }
        else
        {
            Barge.LocationDateTime = null;
        }

        // Combine DraftFeet + DraftInches into Draft decimal
        if (DraftFeet.HasValue)
        {
            var inches = DraftInches ?? 0;
            Barge.Draft = DraftFeet.Value + (inches / 12.0m);
        }
        else
        {
            Barge.Draft = null;
        }
    }

    #endregion
}
