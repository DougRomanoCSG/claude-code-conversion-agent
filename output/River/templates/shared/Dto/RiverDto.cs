using System.ComponentModel.DataAnnotations;
using BargeOps.Shared.Attributes;

namespace BargeOps.Shared.Dto;

/// <summary>
/// River/Waterway entity DTO
/// Used by both API and UI - single source of truth
/// </summary>
public class RiverDto
{
    [Sortable, Filterable]
    public int RiverID { get; set; }

    [Sortable, Filterable]
    [Required(ErrorMessage = "Waterway name is required")]
    [MaxLength(40, ErrorMessage = "Waterway name cannot exceed 40 characters")]
    [Display(Name = "Waterway name")]
    public string Name { get; set; } = string.Empty;

    [Sortable, Filterable]
    [Required(ErrorMessage = "Code is required")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Code must be exactly 3 characters")]
    [Display(Name = "Code")]
    [RegularExpression(@"\w{3}", ErrorMessage = "Code must be exactly 3 alphanumeric characters")]
    public string Code { get; set; } = string.Empty;

    [MaxLength(10, ErrorMessage = "BargeEx Code cannot exceed 10 characters")]
    [Display(Name = "BargeEx Code")]
    public string? BargeExCode { get; set; }

    [Sortable]
    [Range(0, 5000, ErrorMessage = "Start mile must be between 0 and 5000")]
    [Display(Name = "Start mile")]
    public decimal? StartMile { get; set; }

    [Sortable]
    [Range(0, 5000, ErrorMessage = "End mile must be between 0 and 5000")]
    [Display(Name = "End mile")]
    public decimal? EndMile { get; set; }

    [Display(Name = "Direction is low to high (like Mississippi)")]
    public bool IsLowToHighDirection { get; set; } = true;

    [Sortable, Filterable]
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Required(ErrorMessage = "Upstream label is required")]
    [MaxLength(20, ErrorMessage = "Upstream label cannot exceed 20 characters")]
    [Display(Name = "Upstream")]
    public string UpLabel { get; set; } = string.Empty;

    [Required(ErrorMessage = "Downstream label is required")]
    [MaxLength(20, ErrorMessage = "Downstream label cannot exceed 20 characters")]
    [Display(Name = "Downstream")]
    public string DownLabel { get; set; } = string.Empty;
}
