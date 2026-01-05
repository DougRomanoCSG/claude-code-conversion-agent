using System.ComponentModel.DataAnnotations;

namespace BargeOpsAdmin.ViewModels;

/// <summary>
/// ViewModel for Vendor search/list screen
/// Follows MVVM pattern - NO ViewBag/ViewData
/// Location: src/BargeOps.UI/Models/
/// </summary>
public class VendorSearchViewModel
{
    [Display(Name = "Name")]
    public string? Name { get; set; }

    [Display(Name = "Accounting #")]
    public string? AccountingCode { get; set; }

    [Display(Name = "Active Only")]
    public bool IsActiveOnly { get; set; } = true;

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

    // License flags for conditional UI rendering
    public bool PortalLicenseActive { get; set; }
    public bool UnitTowLicenseActive { get; set; }
    public bool BargeExGlobalSettingEnabled { get; set; }
}
