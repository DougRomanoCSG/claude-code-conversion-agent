using BargeOps.Shared.Dto;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace BargeOpsAdmin.ViewModels;

/// <summary>
/// ViewModel for Facility edit/create screen
/// Uses MVVM pattern - all data on the model (no ViewBag/ViewData)
/// DateTime properties: Single property, view splits into date + time inputs, JS combines on submit
/// </summary>
public class FacilityEditViewModel
{
    // Main facility DTO (from Shared project)
    public FacilityDto Facility { get; set; } = new();

    // Lookup lists
    public IEnumerable<SelectListItem> Rivers { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Banks { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> FacilityTypes { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> FacilityStatuses { get; set; } = new List<SelectListItem>();

    // Child collections (already on FacilityDto, but exposed for convenience)
    public List<FacilityBerthDto> Berths => Facility.Berths;
    public List<FacilityStatusDto> Statuses => Facility.Statuses;

    // UI state flags
    public bool IsEditMode => Facility.LocationID > 0;
    public bool CanEdit { get; set; } = true;
    public bool CanDelete { get; set; } = false;

    // Conditional visibility flags
    public bool ShowLockGaugeFields =>
        Facility.BargeExLocationType == "Lock" || Facility.BargeExLocationType == "Gauge Location";
}
