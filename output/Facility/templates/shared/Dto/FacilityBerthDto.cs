using System.ComponentModel.DataAnnotations;

namespace BargeOps.Shared.Dto;

/// <summary>
/// Facility Berth child entity DTO
/// </summary>
public class FacilityBerthDto
{
    public int FacilityBerthID { get; set; }

    public int LocationID { get; set; }

    [Required(ErrorMessage = "Berth name is required")]
    [StringLength(50, ErrorMessage = "Berth name cannot exceed 50 characters")]
    [Display(Name = "Berth Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Ship")]
    public int? ShipID { get; set; }

    [Display(Name = "Current Ship")]
    public string? ShipName { get; set; }

    // Audit Fields
    public DateTime? CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
}
