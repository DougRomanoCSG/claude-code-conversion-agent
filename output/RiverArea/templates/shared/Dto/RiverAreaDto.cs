using Csg.ListQuery.Server;
using System;
using System.Collections.Generic;

namespace BargeOps.Shared.Dto
{
    /// <summary>
    /// Complete DTO for RiverArea entity.
    /// Used by both API and UI - NO separate domain models!
    /// </summary>
    [Sortable]
    [Filterable]
    public class RiverAreaDto
    {
        public int RiverAreaID { get; set; }

        [Filterable]
        [Sortable]
        public string Name { get; set; }

        [Filterable]
        [Sortable]
        public bool IsActive { get; set; }

        [Filterable]
        [Sortable]
        public bool IsPriceZone { get; set; }

        [Filterable]
        [Sortable]
        public bool IsPortalArea { get; set; }

        [Filterable]
        [Sortable]
        public bool IsHighWaterArea { get; set; }

        [Filterable]
        public int? CustomerID { get; set; }

        [Filterable]
        public bool IsFuelTaxArea { get; set; }

        [Filterable]
        public bool IsLiquidRateArea { get; set; }

        // Child collection
        public List<RiverAreaSegmentDto> Segments { get; set; }
    }
}
