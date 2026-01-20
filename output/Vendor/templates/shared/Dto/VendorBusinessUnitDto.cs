using System.ComponentModel.DataAnnotations;

namespace BargeOps.Shared.Dto;

/// <summary>
/// Vendor Business Unit DTO
/// </summary>
public class VendorBusinessUnitDto
{
    // Primary Key
    public int VendorBusinessUnitID { get; set; }

    // Foreign Key
    public int VendorID { get; set; }

    // Core Fields
    [Required(ErrorMessage = "Business unit name is required")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Accounting code cannot exceed 20 characters")]
    [Display(Name = "Accounting #")]
    public string? AccountingCode { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    // Location Fields
    [StringLength(3, ErrorMessage = "River code cannot exceed 3 characters")]
    [Display(Name = "River")]
    public string? River { get; set; }

    [Display(Name = "Mile")]
    public decimal? Mile { get; set; }

    [StringLength(50, ErrorMessage = "Bank cannot exceed 50 characters")]
    [Display(Name = "Bank")]
    public string? Bank { get; set; }

    // Supplier Type Fields
    [Display(Name = "Fuel Supplier")]
    public bool IsFuelSupplier { get; set; }

    [Display(Name = "Default Fuel Supplier")]
    public bool IsDefaultFuelSupplier { get; set; }

    [Display(Name = "Boat Assist Supplier")]
    public bool IsBoatAssistSupplier { get; set; }

    // Fuel Discount Fields (enabled when IsFuelSupplier = true)
    [Display(Name = "Min. Discount Qty")]
    public decimal? MinDiscountQty { get; set; }

    [StringLength(50, ErrorMessage = "Min discount frequency cannot exceed 50 characters")]
    [Display(Name = "Min. Discount Frequency")]
    public string? MinDiscountFrequency { get; set; }

    // Audit Fields
    public DateTime? CreateDateTime { get; set; }
    public string? CreateUser { get; set; }
    public DateTime? ModifyDateTime { get; set; }
    public string? ModifyUser { get; set; }
}
