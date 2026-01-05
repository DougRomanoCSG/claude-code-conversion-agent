using BargeOps.Shared.Dto;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace BargeOpsAdmin.ViewModels;

/// <summary>
/// ViewModel for Vendor Business Unit edit/create modal
/// </summary>
public class VendorBusinessUnitEditViewModel
{
    [Required]
    public VendorBusinessUnitDto BusinessUnit { get; set; } = new();

    [Display(Name = "Rivers")]
    public IEnumerable<SelectListItem> Rivers { get; set; } = new List<SelectListItem>();

    [Display(Name = "Banks")]
    public IEnumerable<SelectListItem> Banks { get; set; } = new List<SelectListItem>();

    [Display(Name = "Discount Frequencies")]
    public IEnumerable<SelectListItem> DiscountFrequencies { get; set; } = new List<SelectListItem>
    {
        new() { Value = "Weekly", Text = "Weekly" },
        new() { Value = "Monthly", Text = "Monthly" },
        new() { Value = "Quarterly", Text = "Quarterly" },
        new() { Value = "Yearly", Text = "Yearly" }
    };
}












