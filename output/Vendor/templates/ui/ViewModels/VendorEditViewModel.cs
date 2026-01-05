using BargeOps.Shared.Dto;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace BargeOpsAdmin.ViewModels;

/// <summary>
/// ViewModel for Vendor edit/create form
/// Follows MVVM pattern - NO ViewBag/ViewData
/// Contains DTO from Shared project + lookup lists
/// Location: src/BargeOps.UI/Models/
/// </summary>
public class VendorEditViewModel
{
    /// <summary>
    /// ⭐ Vendor DTO from Shared project - used directly by both API and UI
    /// NO separate model mapping needed!
    /// </summary>
    public VendorDto Vendor { get; set; } = new();

    #region Lookup Lists

    [Display(Name = "States")]
    public IEnumerable<SelectListItem> States { get; set; } = new List<SelectListItem>();

    [Display(Name = "Payment Terms")]
    public IEnumerable<SelectListItem> TermsCodes { get; set; } = new List<SelectListItem>();

    [Display(Name = "BargeEx Configurations")]
    public IEnumerable<SelectListItem> BargeExConfigs { get; set; } = new List<SelectListItem>();

    [Display(Name = "Rivers")]
    public IEnumerable<SelectListItem> Rivers { get; set; } = new List<SelectListItem>();

    [Display(Name = "Banks")]
    public IEnumerable<SelectListItem> Banks { get; set; } = new List<SelectListItem>();

    [Display(Name = "Discount Frequencies")]
    public IEnumerable<SelectListItem> DiscountFrequencies { get; set; } = new List<SelectListItem>
    {
        new SelectListItem { Value = "Weekly", Text = "Weekly" },
        new SelectListItem { Value = "Monthly", Text = "Monthly" },
        new SelectListItem { Value = "Quarterly", Text = "Quarterly" },
        new SelectListItem { Value = "Yearly", Text = "Yearly" }
    };

    #endregion

    #region License/Feature Flags for Conditional UI

    [Display(Name = "Portal License Active")]
    public bool PortalLicenseActive { get; set; }

    [Display(Name = "UnitTow License Active")]
    public bool UnitTowLicenseActive { get; set; }

    [Display(Name = "BargeEx Global Setting Enabled")]
    public bool BargeExGlobalSettingEnabled { get; set; }

    #endregion

    #region UI State

    /// <summary>
    /// Is this a new vendor (ID = 0) or an edit?
    /// </summary>
    public bool IsNew => Vendor.VendorID == 0;

    /// <summary>
    /// Page title based on new/edit
    /// </summary>
    public string PageTitle => IsNew ? "Create Vendor" : $"Edit Vendor - {Vendor.Name}";

    #endregion
}
