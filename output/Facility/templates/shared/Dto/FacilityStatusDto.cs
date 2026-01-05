using System.ComponentModel.DataAnnotations;

namespace BargeOps.Shared.Dto;

/// <summary>
/// Facility Status child entity DTO
/// Tracks facility operational status over time
/// </summary>
public class FacilityStatusDto
{
    public int FacilityStatusID { get; set; }

    public int LocationID { get; set; }

    [Required(ErrorMessage = "Start date/time is required")]
    [Display(Name = "Start Date/Time")]
    public DateTime StartDateTime { get; set; }

    [Display(Name = "End Date/Time")]
    public DateTime? EndDateTime { get; set; }

    [Required(ErrorMessage = "Status is required")]
    [StringLength(20, ErrorMessage = "Status cannot exceed 20 characters")]
    [Display(Name = "Status")]
    public string Status { get; set; } = string.Empty;

    [StringLength(4000, ErrorMessage = "Note cannot exceed 4000 characters")]
    [Display(Name = "Note")]
    public string? Note { get; set; }

    // Audit Fields
    public DateTime? CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
}
