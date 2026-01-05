using System.ComponentModel.DataAnnotations;

namespace BargeOpsAdmin.ViewModels;

/// <summary>
/// ViewModel for River search/list screen
/// No ViewBag/ViewData - all data on ViewModel (MVVM pattern)
/// </summary>
public class RiverSearchViewModel
{
    [Display(Name = "Code")]
    public string? Code { get; set; }

    [Display(Name = "River/Waterway name")]
    public string? Name { get; set; }

    [Display(Name = "Active only")]
    public bool ActiveOnly { get; set; } = true;
}
