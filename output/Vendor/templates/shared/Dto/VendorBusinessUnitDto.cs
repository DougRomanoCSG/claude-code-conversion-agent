using System.ComponentModel.DataAnnotations;

namespace BargeOps.Shared.Dto;

/// <summary>
/// Vendor Business Unit child entity DTO
/// </summary>
public class VendorBusinessUnitDto
{
    // Primary Key
    public int VendorBusinessUnitID { get; set; }

    // Foreign Key
    public int VendorID { get; set; }

    // Business Unit Properties
    [Required(ErrorMessage = "Business unit name is required")]
    [StringLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Accounting code cannot exceed 20 characters")]
    [Display(Name = "Accounting #")]
    public string? AccountingCode { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    // Location Properties
    [StringLength(3, ErrorMessage = "River code cannot exceed 3 characters")]
    [Display(Name = "River")]
    public string? River { get; set; }

    [Display(Name = "Mile")]
    [Range(0, 2000, ErrorMessage = "Mile must be between 0 and 2000")]
    public decimal? Mile { get; set; }

    [StringLength(20, ErrorMessage = "Bank cannot exceed 20 characters")]
    [Display(Name = "Bank")]
    public string? Bank { get; set; }

    // Supplier Type Flags
    [Display(Name = "Fuel Supplier")]
    public bool IsFuelSupplier { get; set; }

    [Display(Name = "Default Fuel Supplier")]
    public bool IsDefaultFuelSupplier { get; set; }

    [Display(Name = "Boat Assist Supplier")]
    public bool IsBoatAssistSupplier { get; set; }

    // Fuel Supplier Settings (only when IsFuelSupplier = true)
    [Display(Name = "Minimum Discount Quantity")]
    public decimal? MinDiscountQty { get; set; }

    [StringLength(20, ErrorMessage = "Discount frequency cannot exceed 20 characters")]
    [Display(Name = "Minimum Discount Frequency")]
    public string? MinDiscountFrequency { get; set; }

    // Audit Fields
    public DateTime? CreateDateTime { get; set; }
    public string? CreateUser { get; set; }
    public DateTime? ModifyDateTime { get; set; }
    public string? ModifyUser { get; set; }
}
