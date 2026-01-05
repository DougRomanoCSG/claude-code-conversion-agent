using BargeOps.Shared.Dto;
using System.ComponentModel.DataAnnotations;

namespace BargeOpsAdmin.ViewModels;

/// <summary>
/// ViewModel for Vendor Contact edit/create modal
/// </summary>
public class VendorContactEditViewModel
{
    [Required]
    public VendorContactDto Contact { get; set; } = new();

    #region License/Feature Flags

    [Display(Name = "Portal License Active")]
    public bool PortalLicenseActive { get; set; }

    [Display(Name = "UnitTow License Active")]
    public bool UnitTowLicenseActive { get; set; }

    #endregion
}












