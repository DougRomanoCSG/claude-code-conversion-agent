using BargeOps.Shared.Attributes;
using System.ComponentModel.DataAnnotations;

namespace BargeOps.Shared.Dto;

/// <summary>
/// Facility entity DTO - used by both API and UI (no separate domain models)
/// Combines Location and FacilityLocation properties
/// </summary>
public class FacilityDto
{
    // Primary Key
    [Sortable]
    [Filterable]
    public int LocationID { get; set; }

    // Location Properties
    [Required(ErrorMessage = "Facility name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    [Display(Name = "Facility Name")]
    [Sortable]
    [Filterable]
    public string Name { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Short name cannot exceed 50 characters")]
    [Display(Name = "Short Name")]
    [Sortable]
    [Filterable]
    public string? ShortName { get; set; }

    [StringLength(255, ErrorMessage = "Note cannot exceed 255 characters")]
    [Display(Name = "Location Note")]
    public string? Note { get; set; }

    [Display(Name = "Active")]
    [Sortable]
    [Filterable]
    public bool IsActive { get; set; } = true;

    [StringLength(3, ErrorMessage = "River code cannot exceed 3 characters")]
    [Display(Name = "River")]
    [Sortable]
    [Filterable]
    public string? River { get; set; }

    [Display(Name = "Mile")]
    [Range(0, 2000, ErrorMessage = "Mile must be between 0 and 2000")]
    [Sortable]
    public decimal? Mile { get; set; }

    // FacilityLocation Properties
    [StringLength(10, ErrorMessage = "BargeEx code cannot exceed 10 characters")]
    [Display(Name = "BargeEx Code")]
    [Sortable]
    [Filterable]
    public string? BargeExCode { get; set; }

    [StringLength(50, ErrorMessage = "Bank cannot exceed 50 characters")]
    [Display(Name = "Bank")]
    [Sortable]
    [Filterable]
    public string? Bank { get; set; }

    [StringLength(20, ErrorMessage = "Facility type cannot exceed 20 characters")]
    [Display(Name = "Facility Type")]
    [Sortable]
    [Filterable]
    public string? BargeExLocationType { get; set; }

    // Lock/Gauge Location Properties (conditionally enabled based on BargeExLocationType)
    [StringLength(50, ErrorMessage = "USACE name cannot exceed 50 characters")]
    [Display(Name = "USACE Name")]
    public string? LockUsaceName { get; set; }

    [Display(Name = "Flood Stage")]
    public decimal? LockFloodStage { get; set; }

    [Display(Name = "Pool Stage")]
    public decimal? LockPoolStage { get; set; }

    [Display(Name = "Low Water")]
    public decimal? LockLowWater { get; set; }

    [Display(Name = "Normal Current")]
    public decimal? LockNormalCurrent { get; set; }

    [Display(Name = "High Flow")]
    public decimal? LockHighFlow { get; set; }

    [Display(Name = "High Water")]
    public decimal? LockHighWater { get; set; }

    [Display(Name = "Catastrophic Level")]
    public decimal? LockCatastrophicLevel { get; set; }

    // NDC Data Properties (read-only)
    [StringLength(200)]
    [Display(Name = "NDC Name")]
    public string? NdcName { get; set; }

    [StringLength(200)]
    [Display(Name = "Location Description")]
    public string? NdcLocationDescription { get; set; }

    [StringLength(50)]
    [Display(Name = "Address")]
    public string? NdcAddress { get; set; }

    [StringLength(30)]
    [Display(Name = "County")]
    public string? NdcCounty { get; set; }

    [StringLength(3)]
    [Display(Name = "County FIPS")]
    public string? NdcCountyFips { get; set; }

    [StringLength(30)]
    [Display(Name = "Town")]
    public string? NdcTown { get; set; }

    [StringLength(2)]
    [Display(Name = "State")]
    public string? NdcState { get; set; }

    [StringLength(50)]
    [Display(Name = "Waterway")]
    public string? NdcWaterway { get; set; }

    [StringLength(25)]
    [Display(Name = "Port")]
    public string? NdcPort { get; set; }

    [Display(Name = "Latitude")]
    public double? NdcLatitude { get; set; }

    [Display(Name = "Longitude")]
    public double? NdcLongitude { get; set; }

    [StringLength(150)]
    [Display(Name = "Operator")]
    public string? NdcOperator { get; set; }

    [StringLength(150)]
    [Display(Name = "Owner")]
    public string? NdcOwner { get; set; }

    [StringLength(255)]
    [Display(Name = "Purpose")]
    public string? NdcPurpose { get; set; }

    [StringLength(1200)]
    [Display(Name = "Remark")]
    public string? NdcRemark { get; set; }

    // Child Collections (populated separately)
    public List<FacilityBerthDto> Berths { get; set; } = new();
    public List<FacilityStatusDto> Statuses { get; set; } = new();

    // Audit Fields
    public DateTime? CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
}
