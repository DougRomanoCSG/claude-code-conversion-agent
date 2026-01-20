using BargeOps.Shared.Dto;
using System.ComponentModel.DataAnnotations;

namespace BargeOps.Shared.Dto;

/// <summary>
/// Search request for Vendor search with DataTables support
/// Note: Boolean filters use *Only suffix (e.g., IsActiveOnly)
/// </summary>
public class VendorSearchRequest : DataTableRequest
{
    [Display(Name = "Name")]
    [StringLength(100)]
    public string? Name { get; set; }

    [Display(Name = "Accounting Code")]
    [StringLength(50)]
    public string? AccountingCode { get; set; }

    [Display(Name = "Active Only")]
    public bool? IsActiveOnly { get; set; } = true;

    [Display(Name = "Fuel Suppliers Only")]
    public bool? FuelSuppliersOnly { get; set; }

    [Display(Name = "Internal Vendor Only")]
    public bool? InternalVendorOnly { get; set; }

    [Display(Name = "BargeEx Enabled Only")]
    public bool? IsBargeExEnabledOnly { get; set; }

    [Display(Name = "Portal Enabled Only")]
    public bool? EnablePortalOnly { get; set; }

    [Display(Name = "Liquid Broker Only")]
    public bool? LiquidBrokerOnly { get; set; }

    [Display(Name = "Tankerman Only")]
    public bool? TankermanOnly { get; set; }
}
