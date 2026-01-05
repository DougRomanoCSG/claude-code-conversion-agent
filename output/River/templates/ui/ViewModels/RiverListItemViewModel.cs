using System.ComponentModel.DataAnnotations;

namespace BargeOpsAdmin.ViewModels;

/// <summary>
/// ViewModel for River grid row data
/// Alternative to using DTOs directly in DataTables
/// Use this if you need computed properties or different formatting for grid display
/// </summary>
public class RiverListItemViewModel
{
    public int RiverID { get; set; }

    [Display(Name = "Code")]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "River/Waterway")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Start Mile")]
    public string StartMile { get; set; } = string.Empty;

    [Display(Name = "End Mile")]
    public string EndMile { get; set; } = string.Empty;

    [Display(Name = "Low to High")]
    public bool IsLowToHighDirection { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; }

    [Display(Name = "Upstream")]
    public string UpLabel { get; set; } = string.Empty;

    [Display(Name = "Downstream")]
    public string DownLabel { get; set; } = string.Empty;

    [Display(Name = "BargeEx Code")]
    public string? BargeExCode { get; set; }

    /// <summary>
    /// Mile range display for grid (e.g., "100.00 - 200.00")
    /// </summary>
    [Display(Name = "Mile Range")]
    public string MileRange
    {
        get
        {
            var hasStart = !string.IsNullOrEmpty(StartMile);
            var hasEnd = !string.IsNullOrEmpty(EndMile);

            if (hasStart && hasEnd)
            {
                return $"{StartMile} - {EndMile}";
            }
            else if (hasStart)
            {
                return $"From {StartMile}";
            }
            else if (hasEnd)
            {
                return $"To {EndMile}";
            }
            return "-";
        }
    }

    /// <summary>
    /// Status display for grid
    /// </summary>
    [Display(Name = "Status")]
    public string StatusText => IsActive ? "Active" : "Inactive";

    /// <summary>
    /// CSS class for status badge
    /// </summary>
    public string StatusBadgeClass => IsActive ? "badge bg-success" : "badge bg-secondary";

    /// <summary>
    /// Direction display text
    /// </summary>
    [Display(Name = "Direction")]
    public string DirectionText => IsLowToHighDirection ? "Low → High" : "High → Low";
}
