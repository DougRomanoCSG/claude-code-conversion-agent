using System.ComponentModel.DataAnnotations;

namespace BargeOps.Shared.Dto;

/// <summary>
/// Search criteria DTO for facility search
/// </summary>
public class FacilitySearchRequest : DataTableRequest
{
    [Display(Name = "Facility Name")]
    [StringLength(100)]
    public string? Name { get; set; }

    [Display(Name = "Short Name")]
    [StringLength(50)]
    public string? ShortName { get; set; }

    [Display(Name = "River")]
    [StringLength(3)]
    public string? River { get; set; }

    [Display(Name = "Facility Type")]
    [StringLength(20)]
    public string? BargeExLocationType { get; set; }

    [Display(Name = "Active Only")]
    public bool? IsActive { get; set; } = true;

    [Display(Name = "BargeEx Code")]
    [StringLength(10)]
    public string? BargeExCode { get; set; }
}
