using System.ComponentModel.DataAnnotations;

namespace BargeOps.Shared.Dto;

/// <summary>
/// Vendor contact entity DTO
/// Child entity of Vendor
/// </summary>
public class VendorContactDto
{
    #region Primary/Foreign Keys

    [Display(Name = "Contact ID")]
    public int VendorContactID { get; set; }

    [Required]
    [Display(Name = "Vendor ID")]
    public int VendorID { get; set; }

    #endregion

    #region Contact Information

    [Required(ErrorMessage = "Contact name is required")]
    [StringLength(80, ErrorMessage = "Contact name cannot exceed 80 characters")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number")]
    [StringLength(10, ErrorMessage = "Phone number must be 10 digits")]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [StringLength(7, ErrorMessage = "Phone extension cannot exceed 7 characters")]
    [Display(Name = "Extension")]
    public string? PhoneExt { get; set; }

    [Phone(ErrorMessage = "Invalid fax number")]
    [StringLength(10, ErrorMessage = "Fax number must be 10 digits")]
    [Display(Name = "Fax Number")]
    public string? FaxNumber { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(100, ErrorMessage = "Email address cannot exceed 100 characters")]
    [Display(Name = "Email Address")]
    public string? EmailAddress { get; set; }

    #endregion

    #region Contact Attributes

    [Required]
    [Display(Name = "Primary Contact")]
    public bool IsPrimary { get; set; }

    [Required]
    [Display(Name = "Dispatcher")]
    public bool IsDispatcher { get; set; }

    /// <summary>
    /// UnitTow license required
    /// </summary>
    [Required]
    [Display(Name = "Liquid Broker")]
    public bool IsLiquidBroker { get; set; }

    #endregion

    #region Portal Integration

    /// <summary>
    /// Portal user ID - set when contact has portal access
    /// Used to determine if clearing email will delete portal account
    /// </summary>
    [Display(Name = "Portal User ID")]
    public string? PortalUserID { get; set; }

    #endregion
}
