using System.ComponentModel.DataAnnotations;

namespace BargeOps.Shared.Dto;

/// <summary>
/// Vendor Contact DTO - child entity of Vendor
/// </summary>
public class VendorContactDto
{
    // Primary Key
    public int VendorContactID { get; set; }

    // Foreign Key
    public int VendorID { get; set; }

    // Contact Information
    [Required(ErrorMessage = "Contact name is required")]
    [StringLength(80, ErrorMessage = "Name cannot exceed 80 characters")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(10, ErrorMessage = "Phone number cannot exceed 10 characters")]
    [Display(Name = "Phone")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone must be 10 digits")]
    public string? PhoneNumber { get; set; }

    [StringLength(7, ErrorMessage = "Phone extension cannot exceed 7 characters")]
    [Display(Name = "Ext")]
    public string? PhoneExt { get; set; }

    [StringLength(100, ErrorMessage = "Email address cannot exceed 100 characters")]
    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string? EmailAddress { get; set; }

    [StringLength(10, ErrorMessage = "Fax number cannot exceed 10 characters")]
    [Display(Name = "Fax")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Fax must be 10 digits")]
    public string? FaxNumber { get; set; }

    // Flags
    [Display(Name = "Primary Contact")]
    public bool IsPrimary { get; set; }

    [Display(Name = "Dispatcher")]
    public bool IsDispatcher { get; set; }

    [Display(Name = "Liquid Broker")]
    public bool IsLiquidBroker { get; set; }

    // Portal User ID (nullable - assigned when portal account is created)
    public string? PortalUserID { get; set; }

    // Audit Fields
    public DateTime? CreateDateTime { get; set; }
    public string? CreateUser { get; set; }
    public DateTime? ModifyDateTime { get; set; }
    public string? ModifyUser { get; set; }
}
