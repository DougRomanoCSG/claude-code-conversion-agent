using BargeOps.Shared.Dto;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace BargeOpsAdmin.ViewModels;

/// <summary>
/// ViewModel for Facility search/list screen
/// Uses MVVM pattern - all data on the model (no ViewBag/ViewData)
/// </summary>
public class FacilitySearchViewModel
{
    [Display(Name = "Facility Name")]
    public string? Name { get; set; }

    [Display(Name = "Short Name")]
    public string? ShortName { get; set; }

    [Display(Name = "River")]
    public string? River { get; set; }

    [Display(Name = "Facility Type")]
    public string? BargeExLocationType { get; set; }

    [Display(Name = "Active Only")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "BargeEx Code")]
    public string? BargeExCode { get; set; }

    // Dropdown lists
    public IEnumerable<SelectListItem> Rivers { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> FacilityTypes { get; set; } = new List<SelectListItem>();

    // Search results (populated by DataTables AJAX)
    public List<FacilityDto> Results { get; set; } = new();
}
