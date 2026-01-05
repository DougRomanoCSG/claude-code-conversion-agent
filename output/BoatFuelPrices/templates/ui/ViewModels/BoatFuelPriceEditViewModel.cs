using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BargeOpsAdmin.ViewModels;

/// <summary>
/// ViewModel for BoatFuelPrice edit/create form
/// Follows MVVM pattern - all data on the model, no ViewBag/ViewData
/// </summary>
public class BoatFuelPriceEditViewModel : BargeOpsAdminBaseModel<BoatFuelPriceEditViewModel>
{
    /// <summary>
    /// Primary key (0 for new records)
    /// </summary>
    public int BoatFuelPriceID { get; set; }

    /// <summary>
    /// Date when the fuel price becomes effective
    /// Required field with bold label in UI
    /// </summary>
    [Required(ErrorMessage = "Effective date is required.")]
    [Display(Name = "Effective Date")]
    [DataType(DataType.Date)]
    public DateTime EffectiveDate { get; set; }

    /// <summary>
    /// Fuel price amount with 4 decimal places precision
    /// Required field with bold label in UI
    /// </summary>
    [Required(ErrorMessage = "Fuel price is required.")]
    [Display(Name = "Fuel Price")]
    [DataType(DataType.Currency)]
    [Range(0.0001, 999999.9999, ErrorMessage = "Fuel price must be greater than 0.")]
    public decimal Price { get; set; }

    /// <summary>
    /// Foreign key to VendorBusinessUnit (optional)
    /// When this has a value, InvoiceNumber field is enabled
    /// When empty, InvoiceNumber must be blank
    /// </summary>
    [Display(Name = "Fuel Vendor")]
    public int? FuelVendorBusinessUnitID { get; set; }

    /// <summary>
    /// Invoice number from the fuel vendor (optional, max 50 chars)
    /// Conditionally enabled based on FuelVendorBusinessUnitID
    /// JavaScript handles enable/disable logic
    /// </summary>
    [MaxLength(50, ErrorMessage = "Vendor inv# must be less than or equal to 50 characters.")]
    [Display(Name = "Vendor Inv #")]
    public string InvoiceNumber { get; set; }

    /// <summary>
    /// Dropdown list for fuel vendor selection
    /// Populated from VendorBusinessUnit where IsFuelSupplier = true
    /// Includes blank option for "no vendor"
    /// </summary>
    public IEnumerable<SelectListItem> FuelVendors { get; set; } = new List<SelectListItem>();

    // Audit fields (read-only, displayed in UI for existing records)
    [Display(Name = "Created By")]
    public string CreatedBy { get; set; }

    [Display(Name = "Created Date")]
    [DataType(DataType.DateTime)]
    public DateTime? CreatedDate { get; set; }

    [Display(Name = "Modified By")]
    public string ModifiedBy { get; set; }

    [Display(Name = "Modified Date")]
    [DataType(DataType.DateTime)]
    public DateTime? ModifiedDate { get; set; }
}
