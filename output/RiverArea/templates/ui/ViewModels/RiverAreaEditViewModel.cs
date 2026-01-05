using BargeOps.Shared.Dto;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BargeOpsAdmin.ViewModels
{
    /// <summary>
    /// ViewModel for RiverArea edit screen.
    /// Contains the RiverAreaDto from BargeOps.Shared (NOT duplicated!).
    /// MVVM Pattern - NO ViewBag/ViewData!
    ///
    /// Pattern Reference: BoatLocationEditViewModel.cs
    /// </summary>
    public class RiverAreaEditViewModel
    {
        // The actual DTO from BargeOps.Shared
        public RiverAreaDto RiverArea { get; set; }

        // Dropdown data for form
        public IEnumerable<SelectListItem> Customers { get; set; }
        public IEnumerable<SelectListItem> Rivers { get; set; }

        // Feature flags (set from configuration/licenses)
        public bool ShowPortalArea { get; set; }
        public bool ShowHighWaterArea { get; set; }
        public bool ShowLiquidRateArea { get; set; }

        // For view convenience - mapped to/from RiverArea properties
        [Display(Name = "River Area ID")]
        public int RiverAreaID
        {
            get => RiverArea?.RiverAreaID ?? 0;
            set { if (RiverArea != null) RiverArea.RiverAreaID = value; }
        }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
        [Display(Name = "River Area Name")]
        public string Name
        {
            get => RiverArea?.Name;
            set { if (RiverArea != null) RiverArea.Name = value; }
        }

        [Display(Name = "Active")]
        public bool IsActive
        {
            get => RiverArea?.IsActive ?? true;
            set { if (RiverArea != null) RiverArea.IsActive = value; }
        }

        [Display(Name = "Pricing Zone")]
        public bool IsPriceZone
        {
            get => RiverArea?.IsPriceZone ?? false;
            set { if (RiverArea != null) RiverArea.IsPriceZone = value; }
        }

        [Display(Name = "Portal Area")]
        public bool IsPortalArea
        {
            get => RiverArea?.IsPortalArea ?? false;
            set { if (RiverArea != null) RiverArea.IsPortalArea = value; }
        }

        [Display(Name = "High Water Area")]
        public bool IsHighWaterArea
        {
            get => RiverArea?.IsHighWaterArea ?? false;
            set { if (RiverArea != null) RiverArea.IsHighWaterArea = value; }
        }

        [Display(Name = "High Water Customer")]
        public int? CustomerID
        {
            get => RiverArea?.CustomerID;
            set { if (RiverArea != null) RiverArea.CustomerID = value; }
        }

        [Display(Name = "Fuel Tax Area")]
        public bool IsFuelTaxArea
        {
            get => RiverArea?.IsFuelTaxArea ?? false;
            set { if (RiverArea != null) RiverArea.IsFuelTaxArea = value; }
        }

        [Display(Name = "Liquid Rate Area")]
        public bool IsLiquidRateArea
        {
            get => RiverArea?.IsLiquidRateArea ?? false;
            set { if (RiverArea != null) RiverArea.IsLiquidRateArea = value; }
        }

        // Segments (child collection)
        public List<RiverAreaSegmentDto> Segments
        {
            get => RiverArea?.Segments;
            set { if (RiverArea != null) RiverArea.Segments = value; }
        }

        // Constructor
        public RiverAreaEditViewModel()
        {
            RiverArea = new RiverAreaDto
            {
                IsActive = true,
                Segments = new List<RiverAreaSegmentDto>()
            };
        }
    }
}
