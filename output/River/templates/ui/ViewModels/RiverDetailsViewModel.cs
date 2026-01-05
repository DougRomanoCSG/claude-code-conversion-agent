using System.ComponentModel.DataAnnotations;
using BargeOps.Shared.Dto;

namespace BargeOpsAdmin.ViewModels;

/// <summary>
/// ViewModel for River read-only details screen
/// Contains DTO from BargeOps.Shared (single source of truth)
/// No ViewBag/ViewData - all data on ViewModel (MVVM pattern)
/// </summary>
public class RiverDetailsViewModel
{
    /// <summary>
    /// River DTO from shared project
    /// Used for read-only display - no editing
    /// </summary>
    public RiverDto River { get; set; } = new();

    /// <summary>
    /// Formatted start mile for display (with 2 decimal places)
    /// </summary>
    [Display(Name = "Start Mile")]
    public string StartMileFormatted => River.StartMile?.ToString("0.00") ?? "N/A";

    /// <summary>
    /// Formatted end mile for display (with 2 decimal places)
    /// </summary>
    [Display(Name = "End Mile")]
    public string EndMileFormatted => River.EndMile?.ToString("0.00") ?? "N/A";

    /// <summary>
    /// Mile range display (e.g., "100.00 - 200.00")
    /// </summary>
    [Display(Name = "Mile Range")]
    public string MileRange
    {
        get
        {
            if (River.StartMile.HasValue && River.EndMile.HasValue)
            {
                return $"{River.StartMile:0.00} - {River.EndMile:0.00}";
            }
            else if (River.StartMile.HasValue)
            {
                return $"From {River.StartMile:0.00}";
            }
            else if (River.EndMile.HasValue)
            {
                return $"To {River.EndMile:0.00}";
            }
            return "Not specified";
        }
    }

    /// <summary>
    /// Direction display text
    /// </summary>
    [Display(Name = "Direction")]
    public string DirectionDisplay => River.IsLowToHighDirection
        ? "Low to High (like Mississippi)"
        : "High to Low";

    /// <summary>
    /// Active status display text
    /// </summary>
    [Display(Name = "Status")]
    public string StatusDisplay => River.IsActive ? "Active" : "Inactive";

    /// <summary>
    /// Status badge CSS class for Bootstrap
    /// </summary>
    public string StatusBadgeClass => River.IsActive ? "badge bg-success" : "badge bg-secondary";
}
