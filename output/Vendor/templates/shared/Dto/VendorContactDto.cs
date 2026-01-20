using System.ComponentModel.DataAnnotations;

namespace BargeOps.Shared.Dto;

/// <summary>
/// Vendor Contact DTO
/// </summary>
public class VendorContactDto
{
    // Primary Key
    public int VendorContactID { get; set; }

    // Foreign Key
    public int VendorID { get; set; }

    // Core Fields
    [Required(ErrorMessage = "Contact name is required")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(10, ErrorMessage = "Phone number cannot exceed 10 characters")]
    [Display(Name = "Phone")]
    public string? PhoneNumber { get; set; }

    [StringLength(7, ErrorMessage = "Phone extension cannot exceed 7 characters")]
    [Display(Name = "Extension")]
    public string? PhoneExt { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address")]
    [Display(Name = "Email")]
    public string? EmailAddress { get; set; }

    [StringLength(10, ErrorMessage = "Fax number cannot exceed 10 characters")]
    [Display(Name = "Fax")]
    public string? FaxNumber { get; set; }

    [Display(Name = "Primary Contact")]
    public bool IsPrimary { get; set; }

    [Display(Name = "Dispatcher")]
    public bool IsDispatcher { get; set; }

    [Display(Name = "Liquid Broker")]
    public bool IsLiquidBroker { get; set; }

    // Portal Integration
    public int? PortalUserID { get; set; }

    // Audit Fields
    public DateTime? CreateDateTime { get; set; }
    public string? CreateUser { get; set; }
    public DateTime? ModifyDateTime { get; set; }
    public string? ModifyUser { get; set; }
}
