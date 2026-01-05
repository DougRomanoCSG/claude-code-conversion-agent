using System.ComponentModel.DataAnnotations;

namespace BargeOps.Shared.Dto;

/// <summary>
/// DTO for BargeSeriesDraft entity.
/// Represents tonnage values at different draft depths (in feet and inches) for a barge series.
/// Each row represents one foot of draft with 12 columns for inches (0-11).
/// </summary>
public class BargeSeriesDraftDto
{
    /// <summary>
    /// Primary key identifier for the draft record.
    /// </summary>
    public int BargeSeriesDraftID { get; set; }

    /// <summary>
    /// Foreign key to BargeSeries table.
    /// </summary>
    [Required]
    public int BargeSeriesID { get; set; }

    /// <summary>
    /// Draft in feet (typically 0-13).
    /// </summary>
    [Required(ErrorMessage = "Draft feet is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "Draft feet must be non-negative.")]
    [Display(Name = "Feet")]
    public int? DraftFeet { get; set; }

    /// <summary>
    /// Tonnage at 0 inches.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Tonnage must be non-negative.")]
    [Display(Name = "0\"")]
    public int? Tons00 { get; set; }

    /// <summary>
    /// Tonnage at 1 inch.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Tonnage must be non-negative.")]
    [Display(Name = "1\"")]
    public int? Tons01 { get; set; }

    /// <summary>
    /// Tonnage at 2 inches.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Tonnage must be non-negative.")]
    [Display(Name = "2\"")]
    public int? Tons02 { get; set; }

    /// <summary>
    /// Tonnage at 3 inches.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Tonnage must be non-negative.")]
    [Display(Name = "3\"")]
    public int? Tons03 { get; set; }

    /// <summary>
    /// Tonnage at 4 inches.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Tonnage must be non-negative.")]
    [Display(Name = "4\"")]
    public int? Tons04 { get; set; }

    /// <summary>
    /// Tonnage at 5 inches.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Tonnage must be non-negative.")]
    [Display(Name = "5\"")]
    public int? Tons05 { get; set; }

    /// <summary>
    /// Tonnage at 6 inches.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Tonnage must be non-negative.")]
    [Display(Name = "6\"")]
    public int? Tons06 { get; set; }

    /// <summary>
    /// Tonnage at 7 inches.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Tonnage must be non-negative.")]
    [Display(Name = "7\"")]
    public int? Tons07 { get; set; }

    /// <summary>
    /// Tonnage at 8 inches.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Tonnage must be non-negative.")]
    [Display(Name = "8\"")]
    public int? Tons08 { get; set; }

    /// <summary>
    /// Tonnage at 9 inches.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Tonnage must be non-negative.")]
    [Display(Name = "9\"")]
    public int? Tons09 { get; set; }

    /// <summary>
    /// Tonnage at 10 inches.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Tonnage must be non-negative.")]
    [Display(Name = "10\"")]
    public int? Tons10 { get; set; }

    /// <summary>
    /// Tonnage at 11 inches.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Tonnage must be non-negative.")]
    [Display(Name = "11\"")]
    public int? Tons11 { get; set; }
}
