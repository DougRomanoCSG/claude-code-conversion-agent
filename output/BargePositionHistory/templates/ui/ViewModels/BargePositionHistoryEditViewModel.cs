using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BargeOpsAdmin.ViewModels;

/// <summary>
/// ViewModel for Barge Position History edit/create screen.
/// Target: C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\ViewModels\BargePositionHistoryEditViewModel.cs
///
/// MVVM Pattern: All screen data on ViewModel (NO ViewBag/ViewData)
/// DateTime Pattern: Single PositionStartDateTime property (view splits into date+time inputs via JavaScript)
/// </summary>
public class BargePositionHistoryEditViewModel
{
    /// <summary>
    /// Primary key. 0 for new records.
    /// </summary>
    public int FleetPositionHistoryID { get; set; }

    /// <summary>
    /// Fleet ID (required, from parent context).
    /// </summary>
    [Required]
    public int FleetID { get; set; }

    /// <summary>
    /// Fleet name for display.
    /// </summary>
    [Display(Name = "Fleet")]
    public string FleetName { get; set; }

    /// <summary>
    /// CRITICAL: DateTime property - view splits into date and time inputs (24-hour format).
    /// JavaScript combines on submit.
    /// </summary>
    [Required(ErrorMessage = "Position Date/Time is required.")]
    [Display(Name = "Position Date/Time")]
    public DateTime PositionStartDateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// Barge number (required for UI input).
    /// Resolved to BargeID via API validation.
    /// </summary>
    [Required(ErrorMessage = "Barge is required.")]
    [Display(Name = "Barge")]
    public string BargeNum { get; set; }

    /// <summary>
    /// Indicates if barge left the fleet.
    /// When true, tier fields disabled/cleared.
    /// </summary>
    [Display(Name = "Left Fleet")]
    public bool LeftFleet { get; set; }

    /// <summary>
    /// Tier ID (optional, disabled when LeftFleet = true).
    /// </summary>
    [Display(Name = "Tier")]
    public int? TierID { get; set; }

    /// <summary>
    /// Tier X coordinate (optional, disabled when LeftFleet = true).
    /// Range: -32768 to 32767.
    /// </summary>
    [Display(Name = "Tier X")]
    [Range(-32768, 32767, ErrorMessage = "Tier X must be a valid 16-bit integer.")]
    public short? TierX { get; set; }

    /// <summary>
    /// Tier Y coordinate (optional, disabled when LeftFleet = true).
    /// Range: -32768 to 32767.
    /// </summary>
    [Display(Name = "Tier Y")]
    [Range(-32768, 32767, ErrorMessage = "Tier Y must be a valid 16-bit integer.")]
    public short? TierY { get; set; }

    /// <summary>
    /// ModifyDateTime for optimistic concurrency.
    /// </summary>
    public DateTime? ModifyDateTime { get; set; }

    // Lookup lists
    /// <summary>
    /// Tiers for dropdown (filtered by TierGroupID from search).
    /// </summary>
    public IEnumerable<SelectListItem> Tiers { get; set; } = new List<SelectListItem>();

    /// <summary>
    /// Tier Group ID from search criteria (for filtering Tiers).
    /// </summary>
    public int? TierGroupID { get; set; }
}
