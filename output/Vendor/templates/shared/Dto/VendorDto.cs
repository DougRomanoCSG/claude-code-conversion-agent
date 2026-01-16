using BargeOps.Shared.Attributes;
using System.ComponentModel.DataAnnotations;

namespace BargeOps.Shared.Dto;

/// <summary>
/// Vendor entity DTO - used by both API and UI (no separate domain models)
/// </summary>
public class VendorDto
{
    // Primary Key
    [Sortable]
    [Filterable]
    public int VendorID { get; set; }

    // Basic Vendor Information
    [Required(ErrorMessage = "Vendor name is required")]
    [StringLength(80, ErrorMessage = "Name cannot exceed 80 characters")]
    [Display(Name = "Name")]
    [Sortable]
    [Filterable]
    public string Name { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Long name cannot exceed 100 characters")]
    [Display(Name = "Long Name")]
    [Sortable]
    [Filterable]
    public string? LongName { get; set; }

    [StringLength(20, ErrorMessage = "Accounting code cannot exceed 20 characters")]
    [Display(Name = "Accounting #")]
    [Sortable]
    [Filterable]
    public string? AccountingCode { get; set; }

    [Display(Name = "Active")]
    [Sortable]
    [Filterable]
    public bool IsActive { get; set; } = true;

    // Contact Information
    [StringLength(100, ErrorMessage = "Address cannot exceed 100 characters")]
    [Display(Name = "Address")]
    public string? Address { get; set; }

    [StringLength(30, ErrorMessage = "City cannot exceed 30 characters")]
    [Display(Name = "City")]
    public string? City { get; set; }

    [StringLength(2, ErrorMessage = "State must be 2 characters")]
    [Display(Name = "State")]
    public string? State { get; set; }

    [StringLength(10, ErrorMessage = "Zip cannot exceed 10 characters")]
    [Display(Name = "Zip")]
    [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Zip must be in format 12345 or 12345-6789")]
    public string? Zip { get; set; }

    [StringLength(10, ErrorMessage = "Phone number cannot exceed 10 characters")]
    [Display(Name = "Phone")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone must be 10 digits")]
    public string? PhoneNumber { get; set; }

    [StringLength(10, ErrorMessage = "Fax number cannot exceed 10 characters")]
    [Display(Name = "Fax")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Fax must be 10 digits")]
    public string? FaxNumber { get; set; }

    [StringLength(100, ErrorMessage = "Email address cannot exceed 100 characters")]
    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string? EmailAddress { get; set; }

    [StringLength(20, ErrorMessage = "Terms code cannot exceed 20 characters")]
    [Display(Name = "Payment Terms")]
    public string? TermsCode { get; set; }

    // Vendor Flags
    [Display(Name = "Internal Vendor")]
    [Sortable]
    [Filterable]
    public bool IsInternalVendor { get; set; }

    [Display(Name = "Fuel Supplier")]
    [Sortable]
    [Filterable]
    public bool IsFuelSupplier { get; set; }

    [Display(Name = "Boat Assist Supplier")]
    [Sortable]
    [Filterable]
    public bool IsBoatAssistSupplier { get; set; }

    [Display(Name = "Liquid Broker")]
    [Sortable]
    [Filterable]
    public bool IsLiquidBroker { get; set; }

    [Display(Name = "Tankerman")]
    [Sortable]
    [Filterable]
    public bool IsTankerman { get; set; }

    // Portal Settings
    [Display(Name = "Portal Enabled")]
    [Sortable]
    [Filterable]
    public bool EnablePortal { get; set; }

    // BargeEx Settings
    [Display(Name = "BargeEx Enabled")]
    [Sortable]
    [Filterable]
    public bool IsBargeExEnabled { get; set; }

    [StringLength(8, ErrorMessage = "BargeEx trading partner number cannot exceed 8 characters")]
    [Display(Name = "BargeEx Trading Partner #")]
    public string? BargeExTradingPartnerNum { get; set; }

    [Display(Name = "BargeEx Config")]
    public int? BargeExConfigID { get; set; }

    // Child Collections (populated separately via API endpoints)
    public List<VendorContactDto> Contacts { get; set; } = new();
    public List<VendorBusinessUnitDto> BusinessUnits { get; set; } = new();

    // Audit Fields
    public DateTime? CreateDateTime { get; set; }
    public string? CreateUser { get; set; }
    public DateTime? ModifyDateTime { get; set; }
    public string? ModifyUser { get; set; }
}
