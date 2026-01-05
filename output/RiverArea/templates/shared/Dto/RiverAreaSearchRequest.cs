namespace BargeOps.Shared.Dto
{
    /// <summary>
    /// Search criteria DTO for RiverArea search.
    /// Extends DataTableRequest for server-side DataTables processing.
    /// </summary>
    public class RiverAreaSearchRequest : DataTableRequest
    {
        public string Name { get; set; }
        public bool ActiveOnly { get; set; } = true;
        public bool PricingZonesOnly { get; set; } = false;
        public bool PortalAreasOnly { get; set; } = false;
        public int? CustomerID { get; set; }
        public bool HighWaterAreasOnly { get; set; } = false;
    }
}
