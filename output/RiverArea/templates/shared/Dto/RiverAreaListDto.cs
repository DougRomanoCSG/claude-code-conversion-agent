namespace BargeOps.Shared.Dto
{
    /// <summary>
    /// DTO for RiverArea search results grid.
    /// Optimized for DataTables display with denormalized customer name.
    /// </summary>
    public class RiverAreaListDto
    {
        public int RiverAreaID { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public bool IsPriceZone { get; set; }
        public bool IsPortalArea { get; set; }
        public bool IsHighWaterArea { get; set; }
        public string CustomerName { get; set; }  // Denormalized for display
        public bool IsFuelTaxArea { get; set; }
        public bool IsLiquidRateArea { get; set; }
    }
}
