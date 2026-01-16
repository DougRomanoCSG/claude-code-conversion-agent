using System.ComponentModel.DataAnnotations;

namespace BargeOps.Shared.Dto;

/// <summary>
/// Vendor Contact child entity DTO
/// </summary>
public class VendorContactDto
{
    // Primary Key
    public int VendorContactID { get; set; }

    // Foreign Key
    public int VendorID { get; set; }

    // Contact Properties
    [Required(ErrorMessage = "Contact name is required")]
    [StringLength(80, ErrorMessage = "Name cannot exceed 80 characters")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(10, ErrorMessage = "Phone number cannot exceed 10 characters")]
    [Display(Name = "Phone")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    public string? PhoneNumber { get; set; }

    [StringLength(7, ErrorMessage = "Phone extension cannot exceed 7 characters")]
    [Display(Name = "Extension")]
    public string? PhoneExt { get; set; }

    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string? EmailAddress { get; set; }

    [StringLength(10, ErrorMessage = "Fax number cannot exceed 10 characters")]
    [Display(Name = "Fax")]
    [Phone(ErrorMessage = "Invalid fax number format")]
    public string? FaxNumber { get; set; }

    // Contact Flags
    [Display(Name = "Primary Contact")]
    public bool IsPrimary { get; set; }

    [Display(Name = "Dispatcher")]
    public bool IsDispatcher { get; set; }

    [Display(Name = "Liquid Broker")]
    public bool IsLiquidBroker { get; set; }

    // Portal User Reference
    public string? PortalUserID { get; set; }

    // Audit Fields
    public DateTime? CreateDateTime { get; set; }
    public string? CreateUser { get; set; }
    public DateTime? ModifyDateTime { get; set; }
    public string? ModifyUser { get; set; }
}
