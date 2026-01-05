using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace BargeOpsAdmin.ViewModels;

/// <summary>
/// ViewModel for BargeSeries search/list screen.
/// Follows MVVM pattern - all data on the ViewModel (no ViewBag/ViewData).
/// </summary>
public class BargeSeriesSearchViewModel
{
    /// <summary>
    /// Series name filter (partial match).
    /// </summary>
    [Display(Name = "Series")]
    public string? SeriesName { get; set; }

    /// <summary>
    /// Customer ID filter (owner of the barge series).
    /// </summary>
    [Display(Name = "Owner")]
    public int? CustomerID { get; set; }

    /// <summary>
    /// Hull type filter.
    /// </summary>
    [Display(Name = "Hull Type")]
    public string? HullType { get; set; }

    /// <summary>
    /// Cover type filter.
    /// </summary>
    [Display(Name = "Cover Type")]
    public string? CoverType { get; set; }

    /// <summary>
    /// Filter to show only active records.
    /// </summary>
    [Display(Name = "Active Only")]
    public bool ActiveOnly { get; set; } = true;

    /// <summary>
    /// List of customers for dropdown.
    /// </summary>
    public IEnumerable<SelectListItem> Customers { get; set; } = Enumerable.Empty<SelectListItem>();

    /// <summary>
    /// List of hull types for dropdown.
    /// </summary>
    public IEnumerable<SelectListItem> HullTypes { get; set; } = Enumerable.Empty<SelectListItem>();

    /// <summary>
    /// List of cover types for dropdown.
    /// </summary>
    public IEnumerable<SelectListItem> CoverTypes { get; set; } = Enumerable.Empty<SelectListItem>();

    /// <summary>
    /// User permission: can the user create new barge series?
    /// </summary>
    public bool CanCreate { get; set; }

    /// <summary>
    /// User permission: can the user modify barge series?
    /// </summary>
    public bool CanModify { get; set; }

    /// <summary>
    /// User permission: can the user delete barge series?
    /// </summary>
    public bool CanDelete { get; set; }
}
