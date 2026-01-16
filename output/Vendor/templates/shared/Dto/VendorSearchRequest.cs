namespace BargeOps.Shared.Dto;

/// <summary>
/// Search request for Vendor with DataTables server-side processing
/// </summary>
public class VendorSearchRequest : DataTableRequest
{
    // Search Criteria
    public string? Name { get; set; }
    public string? AccountingCode { get; set; }
    public bool? IsActiveOnly { get; set; } = true;
    public bool? FuelSuppliersOnly { get; set; }
    public bool? InternalVendorOnly { get; set; }
    public bool? IsBargeExEnabledOnly { get; set; }
    public bool? EnablePortalOnly { get; set; }
    public bool? LiquidBrokerOnly { get; set; }
    public bool? TankermanOnly { get; set; }
}
