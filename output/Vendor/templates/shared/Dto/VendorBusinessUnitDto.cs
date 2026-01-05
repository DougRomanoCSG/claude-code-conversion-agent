using System.ComponentModel.DataAnnotations;

namespace BargeOps.Shared.Dto;

/// <summary>
/// Vendor business unit entity DTO
/// Child entity of Vendor
/// Represents a physical location/facility for a vendor
/// </summary>
public class VendorBusinessUnitDto
{
    #region Primary/Foreign Keys

    [Display(Name = "Business Unit ID")]
    public int VendorBusinessUnitID { get; set; }

    [Required]
    [Display(Name = "Vendor ID")]
    public int VendorID { get; set; }

    #endregion

    #region Basic Information

    [Required(ErrorMessage = "Business unit name is required")]
    [StringLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Accounting code cannot exceed 50 characters")]
    [Display(Name = "Accounting #")]
    public string? AccountingCode { get; set; }

    [Required]
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    #endregion

    #region Location Information

    [StringLength(3, ErrorMessage = "River code cannot exceed 3 characters")]
    [Display(Name = "River")]
    public string? River { get; set; }

    [Display(Name = "Mile")]
    public decimal? Mile { get; set; }

    [StringLength(50, ErrorMessage = "Bank cannot exceed 50 characters")]
    [Display(Name = "Bank")]
    public string? Bank { get; set; }

    #endregion

    #region Fuel Supplier Settings

    [Required]
    [Display(Name = "Fuel Supplier")]
    public bool IsFuelSupplier { get; set; }

    /// <summary>
    /// Only one VBU can be default fuel supplier
    /// Automatically cleared if IsFuelSupplier is false
    /// </summary>
    [Required]
    [Display(Name = "Default Fuel Supplier")]
    public bool IsDefaultFuelSupplier { get; set; }

    /// <summary>
    /// Automatically cleared if IsFuelSupplier is false
    /// </summary>
    [Display(Name = "Minimum Discount Quantity")]
    public decimal? MinDiscountQty { get; set; }

    /// <summary>
    /// Values: Weekly, Monthly, Quarterly, Yearly
    /// Automatically cleared if IsFuelSupplier is false
    /// </summary>
    [StringLength(20, ErrorMessage = "Frequency cannot exceed 20 characters")]
    [Display(Name = "Minimum Discount Frequency")]
    public string? MinDiscountFrequency { get; set; }

    #endregion

    #region Other Services

    [Required]
    [Display(Name = "Boat Assist Supplier")]
    public bool IsBoatAssistSupplier { get; set; }

    #endregion
}
