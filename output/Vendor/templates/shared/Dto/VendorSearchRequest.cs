using BargeOps.Shared.Dto;

namespace BargeOps.Shared.Dto;

/// <summary>
/// Search criteria for vendor search with DataTables server-side processing
/// </summary>
public class VendorSearchRequest : DataTableRequest
{
    // Search Criteria
    public string? Name { get; set; }
    public string? AccountingCode { get; set; }
    public bool? IsActive { get; set; } = true;
    public bool? IsFuelSupplier { get; set; }
    public bool? IsInternalVendor { get; set; }
    public bool? IsBargeExEnabled { get; set; }
    public bool? EnablePortal { get; set; }
    public bool? IsLiquidBroker { get; set; }
    public bool? IsTankerman { get; set; }
}
