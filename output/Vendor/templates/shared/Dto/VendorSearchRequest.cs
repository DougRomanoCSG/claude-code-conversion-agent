using System.ComponentModel.DataAnnotations;

namespace BargeOps.Shared.Dto;

/// <summary>
/// Search criteria for filtering vendors
/// Used by both API and UI for vendor search operations
/// </summary>
public class VendorSearchRequest
{
    [Display(Name = "Name")]
    [StringLength(50)]
    public string? Name { get; set; }

    [Display(Name = "Accounting #")]
    [StringLength(20)]
    public string? AccountingCode { get; set; }

    [Display(Name = "Active Only")]
    public bool ActiveOnly { get; set; } = true;

    [Display(Name = "Fuel Suppliers Only")]
    public bool FuelSuppliersOnly { get; set; }

    [Display(Name = "Internal Vendor Only")]
    public bool InternalVendorOnly { get; set; }

    [Display(Name = "BargeEx Enabled Only")]
    public bool IsBargeExEnabledOnly { get; set; }

    [Display(Name = "Portal Enabled Only")]
    public bool EnablePortalOnly { get; set; }

    [Display(Name = "Liquid Broker Only")]
    public bool LiquidBrokerOnly { get; set; }

    [Display(Name = "Tankerman Only")]
    public bool TankermanOnly { get; set; }
}
