using System.ComponentModel.DataAnnotations;
using BargeOps.Shared.Attributes;

namespace BargeOps.Shared.Dto;

/// <summary>
/// DTO for BargeSeries entity.
/// Represents a standardized barge series with dimensions, draft calculations, and tonnage specifications.
/// </summary>
public class BargeSeriesDto
{
    /// <summary>
    /// Primary key identifier for the barge series.
    /// </summary>
    public int BargeSeriesID { get; set; }

    /// <summary>
    /// Foreign key to Customer table (owner of the barge series).
    /// </summary>
    [Required(ErrorMessage = "Customer is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Customer is required.")]
    [Filterable]
    public int CustomerID { get; set; }

    /// <summary>
    /// Customer name (joined from Customer table for display).
    /// </summary>
    [Sortable]
    [Filterable]
    public string? CustomerName { get; set; }

    /// <summary>
    /// Name/identifier of the barge series.
    /// </summary>
    [Required(ErrorMessage = "Series is required.")]
    [StringLength(50, ErrorMessage = "Series exceeds maximum length of 50.")]
    [Display(Name = "Series")]
    [Sortable]
    [Filterable]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Single character code identifying the hull type.
    /// </summary>
    [Required(ErrorMessage = "Hull type is required.")]
    [StringLength(1, ErrorMessage = "Hull type exceeds maximum length of 1.")]
    [Display(Name = "Hull Type")]
    [Sortable]
    [Filterable]
    public string HullType { get; set; } = string.Empty;

    /// <summary>
    /// Code identifying the cover type for the barge.
    /// </summary>
    [Required(ErrorMessage = "Cover type is required.")]
    [StringLength(3, ErrorMessage = "Cover type exceeds maximum length of 3.")]
    [Display(Name = "Cover Type")]
    [Sortable]
    [Filterable]
    public string CoverType { get; set; } = string.Empty;

    /// <summary>
    /// Length of the barge in feet.
    /// </summary>
    [Required(ErrorMessage = "Length is required.")]
    [Range(0, double.MaxValue, ErrorMessage = "Length must be non-negative.")]
    [Display(Name = "Length")]
    [Sortable]
    public decimal? Length { get; set; }

    /// <summary>
    /// Width of the barge in feet.
    /// </summary>
    [Required(ErrorMessage = "Width is required.")]
    [Range(0, double.MaxValue, ErrorMessage = "Width must be non-negative.")]
    [Display(Name = "Width")]
    [Sortable]
    public decimal? Width { get; set; }

    /// <summary>
    /// Depth of the barge in feet.
    /// </summary>
    [Required(ErrorMessage = "Depth is required.")]
    [Range(0, double.MaxValue, ErrorMessage = "Depth must be non-negative.")]
    [Display(Name = "Depth")]
    [Sortable]
    public decimal? Depth { get; set; }

    /// <summary>
    /// Computed dimensions string for display (Length x Width x Depth).
    /// </summary>
    [Display(Name = "Dimensions")]
    [Sortable]
    public string? Dimensions =>
        Length.HasValue && Width.HasValue && Depth.HasValue
            ? $"{Length:F1} × {Width:F1} × {Depth:F1}"
            : null;

    /// <summary>
    /// Tonnage capacity per inch of draft.
    /// </summary>
    [Required(ErrorMessage = "Tons/inch is required.")]
    [Range(0, double.MaxValue, ErrorMessage = "Tons/inch must be non-negative.")]
    [Display(Name = "Tons/Inch")]
    [Sortable]
    public decimal? TonsPerInch { get; set; }

    /// <summary>
    /// Light draft of the barge (draft when empty) in decimal feet.
    /// </summary>
    [Required(ErrorMessage = "Light draft is required.")]
    [Range(0, 99.999, ErrorMessage = "Light draft must be between 0 and 99.999.")]
    [Display(Name = "Light Draft")]
    [Sortable]
    public decimal? DraftLight { get; set; }

    /// <summary>
    /// Feet portion of DraftLight (UI display only).
    /// Calculated from DraftLight decimal value.
    /// </summary>
    [Display(Name = "Light Draft (ft)")]
    public string? DraftLightFeet =>
        DraftLight.HasValue
            ? ((int)DraftLight.Value).ToString()
            : null;

    /// <summary>
    /// Inches portion of DraftLight (UI display only).
    /// Calculated from DraftLight decimal value.
    /// </summary>
    [Display(Name = "Light Draft (in)")]
    public string? DraftLightInches =>
        DraftLight.HasValue
            ? ((int)((DraftLight.Value - (int)DraftLight.Value) * 12)).ToString()
            : null;

    /// <summary>
    /// Indicates whether this barge series is active.
    /// </summary>
    [Display(Name = "Active")]
    [Sortable]
    [Filterable]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Collection of draft tonnage specifications for this barge series.
    /// Contains up to 14 draft records (0-13 feet) with tonnage values at different inch increments.
    /// </summary>
    public List<BargeSeriesDraftDto> Drafts { get; set; } = new();
}
