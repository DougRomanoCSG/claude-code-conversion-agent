using System.ComponentModel.DataAnnotations;
using BargeOps.Shared.Attributes;

namespace BargeOps.Shared.Dto;

/// <summary>
/// Vendor entity DTO - Complete data model used by BOTH API and UI
/// NO separate Models folder - this DTO is the single source of truth!
/// </summary>
public class VendorDto
{
    #region Primary Key

    [Display(Name = "Vendor ID")]
    public int VendorID { get; set; }

    #endregion

    #region Basic Information

    [Required(ErrorMessage = "Name is required")]
    [StringLength(20, ErrorMessage = "Name cannot exceed 20 characters")]
    [Display(Name = "Name")]
    [Sortable]
    [Filterable]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Long name is required")]
    [StringLength(50, ErrorMessage = "Long name cannot exceed 50 characters")]
    [Display(Name = "Long Name")]
    [Sortable]
    [Filterable]
    public string LongName { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Accounting code cannot exceed 20 characters")]
    [Display(Name = "Accounting #")]
    [Sortable]
    [Filterable]
    public string? AccountingCode { get; set; }

    [Required]
    [Display(Name = "Active")]
    [Sortable]
    [Filterable]
    public bool IsActive { get; set; } = true;

    #endregion

    #region Address Information

    [StringLength(80, ErrorMessage = "Address cannot exceed 80 characters")]
    [Display(Name = "Address")]
    public string? Address { get; set; }

    [StringLength(30, ErrorMessage = "City cannot exceed 30 characters")]
    [Display(Name = "City")]
    [Sortable]
    [Filterable]
    public string? City { get; set; }

    [StringLength(2, ErrorMessage = "State cannot exceed 2 characters")]
    [Display(Name = "State")]
    [Sortable]
    [Filterable]
    public string? State { get; set; }

    [StringLength(10, ErrorMessage = "ZIP code cannot exceed 10 characters")]
    [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Invalid ZIP code format")]
    [Display(Name = "ZIP Code")]
    public string? Zip { get; set; }

    #endregion

    #region Contact Information

    [Phone(ErrorMessage = "Invalid phone number")]
    [StringLength(10, ErrorMessage = "Phone number must be 10 digits")]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [Phone(ErrorMessage = "Invalid fax number")]
    [StringLength(10, ErrorMessage = "Fax number must be 10 digits")]
    [Display(Name = "Fax Number")]
    public string? FaxNumber { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(100, ErrorMessage = "Email address cannot exceed 100 characters")]
    [Display(Name = "Email Address")]
    public string? EmailAddress { get; set; }

    #endregion

    #region Business Settings

    [StringLength(8, ErrorMessage = "Terms code cannot exceed 8 characters")]
    [Display(Name = "Payment Terms")]
    [Sortable]
    [Filterable]
    public string? TermsCode { get; set; }

    [Required]
    [Display(Name = "Internal Vendor")]
    [Sortable]
    [Filterable]
    public bool IsInternalVendor { get; set; }

    #endregion

    #region Feature Flags - License Dependent

    /// <summary>
    /// Portal license required
    /// </summary>
    [Required]
    [Display(Name = "Portal Enabled")]
    [Sortable]
    [Filterable]
    public bool EnablePortal { get; set; }

    /// <summary>
    /// UnitTow license required
    /// </summary>
    [Required]
    [Display(Name = "Liquid Broker")]
    [Sortable]
    [Filterable]
    public bool IsLiquidBroker { get; set; }

    /// <summary>
    /// UnitTow license required
    /// </summary>
    [Required]
    [Display(Name = "Tankerman")]
    [Sortable]
    [Filterable]
    public bool IsTankerman { get; set; }

    #endregion

    #region BargeEx Settings - Global Setting Dependent

    /// <summary>
    /// EnableBargeExBargeLineSupport global setting required
    /// </summary>
    [Required]
    [Display(Name = "BargeEx Enabled")]
    [Sortable]
    [Filterable]
    public bool IsBargeExEnabled { get; set; }

    /// <summary>
    /// Required when IsBargeExEnabled is true
    /// Padded to 8 characters for char(8) database field
    /// </summary>
    [StringLength(8, ErrorMessage = "Trading partner number cannot exceed 8 characters")]
    [Display(Name = "BargeEx Trading Partner Number")]
    public string? BargeExTradingPartnerNum { get; set; }

    /// <summary>
    /// Required when IsBargeExEnabled is true
    /// </summary>
    [Display(Name = "BargeEx Configuration")]
    public string? BargeExConfigID { get; set; }

    #endregion

    #region Computed Properties - From VendorBusinessUnits

    /// <summary>
    /// Computed from VendorBusinessUnits collection
    /// Set by repository when loading
    /// </summary>
    [Display(Name = "Fuel Supplier")]
    [Sortable]
    [Filterable]
    public bool IsFuelSupplier { get; set; }

    /// <summary>
    /// Computed from VendorBusinessUnits collection
    /// Set by repository when loading
    /// </summary>
    [Display(Name = "Boat Assist Supplier")]
    [Sortable]
    [Filterable]
    public bool IsBoatAssistSupplier { get; set; }

    #endregion

    #region Child Collections

    /// <summary>
    /// Vendor contacts - loaded immediately with parent
    /// </summary>
    public List<VendorContactDto> VendorContacts { get; set; } = new();

    /// <summary>
    /// Vendor business units - loaded immediately with parent
    /// </summary>
    public List<VendorBusinessUnitDto> VendorBusinessUnits { get; set; } = new();

    #endregion

    #region Audit Fields (Optional - if needed)

    // Uncomment if audit fields are needed
    // public DateTime CreatedDate { get; set; }
    // public string? CreatedBy { get; set; }
    // public DateTime? ModifiedDate { get; set; }
    // public string? ModifiedBy { get; set; }

    #endregion
}
