using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BargeOpsAdmin.ViewModels
{
    /// <summary>
    /// ViewModel for RiverArea search screen.
    /// MVVM Pattern - NO ViewBag/ViewData!
    /// </summary>
    public class RiverAreaSearchViewModel
    {
        [Display(Name = "River Area Name")]
        public string Name { get; set; }

        [Display(Name = "Active Only")]
        public bool ActiveOnly { get; set; } = true;

        [Display(Name = "Pricing Zones Only")]
        public bool PricingZonesOnly { get; set; } = false;

        [Display(Name = "Portal Areas Only")]
        public bool PortalAreasOnly { get; set; } = false;

        [Display(Name = "High Water Customer")]
        public int? CustomerID { get; set; }

        [Display(Name = "High Water Areas Only")]
        public bool HighWaterAreasOnly { get; set; } = false;

        // Dropdown data
        public IEnumerable<SelectListItem> Customers { get; set; }

        // Feature flags (set from configuration/licenses)
        public bool ShowPortalAreas { get; set; }
        public bool ShowHighWaterFilters { get; set; }
        public bool ShowLiquidRateColumn { get; set; }
    }
}
