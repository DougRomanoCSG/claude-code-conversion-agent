using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BargeOpsAdmin.ViewModels;

/// <summary>
/// ViewModel for Barge Position History search/list screen.
/// Target: C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\ViewModels\BargePositionHistorySearchViewModel.cs
///
/// MVVM Pattern: All screen data on ViewModel (NO ViewBag/ViewData)
/// </summary>
public class BargePositionHistorySearchViewModel
{
    /// <summary>
    /// Fleet ID passed from parent context.
    /// Required for all searches.
    /// </summary>
    public int FleetID { get; set; }

    /// <summary>
    /// Fleet name for display.
    /// </summary>
    [Display(Name = "Fleet")]
    public string FleetName { get; set; }

    /// <summary>
    /// Search date (required).
    /// Searches for all positions on this date.
    /// </summary>
    [Required(ErrorMessage = "Date is required.")]
    [Display(Name = "Date")]
    [DataType(DataType.Date)]
    public DateTime SearchDate { get; set; } = DateTime.Today;

    /// <summary>
    /// Tier Group ID for filtering (required).
    /// Filters available tiers in search results.
    /// </summary>
    [Required(ErrorMessage = "Tier Group is required.")]
    [Display(Name = "Tier Group")]
    public int? TierGroupID { get; set; }

    /// <summary>
    /// Optional barge number filter.
    /// </summary>
    [Display(Name = "Barge")]
    public string BargeNum { get; set; }

    /// <summary>
    /// Include records without tier positions.
    /// </summary>
    [Display(Name = "Include blank tier positions")]
    public bool IncludeBlankTierPos { get; set; }

    // Lookup lists
    /// <summary>
    /// Tier Groups for dropdown.
    /// Populated from API based on FleetID.
    /// </summary>
    public IEnumerable<SelectListItem> TierGroups { get; set; } = new List<SelectListItem>();

    // Permission flags
    /// <summary>
    /// User can modify position history records.
    /// Controls visibility of Add/Modify buttons.
    /// </summary>
    public bool CanModify { get; set; }

    /// <summary>
    /// User can delete position history records.
    /// Controls visibility of Remove button.
    /// </summary>
    public bool CanDelete { get; set; }
}
