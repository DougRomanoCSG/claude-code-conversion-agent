using System;

namespace BargeOps.Shared.Dto
{
    /// <summary>
    /// Search criteria DTO for BoatFuelPrice DataTables integration
    /// Extends DataTableRequest for server-side paging/sorting
    /// </summary>
    public class BoatFuelPriceSearchRequest : DataTableRequest
    {
        /// <summary>
        /// Filter by effective date (optional)
        /// Default: Today's date
        /// </summary>
        public DateTime? EffectiveDate { get; set; }

        /// <summary>
        /// Filter by fuel vendor business unit ID (optional)
        /// </summary>
        public int? FuelVendorBusinessUnitID { get; set; }
    }
}
