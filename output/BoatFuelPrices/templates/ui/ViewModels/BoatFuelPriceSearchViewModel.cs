using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BargeOpsAdmin.ViewModels;

/// <summary>
/// ViewModel for BoatFuelPrice search/list screen
/// Follows MVVM pattern - all data on the model, no ViewBag/ViewData
/// </summary>
public class BoatFuelPriceSearchViewModel : BargeOpsAdminBaseModel<BoatFuelPriceSearchViewModel>
{
    /// <summary>
    /// Search criterion: Effective date
    /// Defaults to today's date
    /// </summary>
    [Display(Name = "Effective Date")]
    [DataType(DataType.Date)]
    public DateTime? EffectiveDateSearch { get; set; } = DateTime.Today;

    /// <summary>
    /// Search criterion: Fuel vendor filter
    /// Optional - allows filtering by specific vendor
    /// </summary>
    [Display(Name = "Fuel Vendor")]
    public int? FuelVendorIDSearch { get; set; }

    /// <summary>
    /// Dropdown list for fuel vendor selection
    /// Populated from VendorBusinessUnit where IsFuelSupplier = true
    /// </summary>
    public IEnumerable<SelectListItem> FuelVendors { get; set; } = new List<SelectListItem>();
}
