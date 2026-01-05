using BargeOps.Shared.Dto;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace BargeOpsAdmin.ViewModels;

/// <summary>
/// ViewModel for BargeSeries edit/create screen.
/// Follows MVVM pattern - all data on the ViewModel (no ViewBag/ViewData).
/// Contains the BargeSeries DTO from BargeOps.Shared (NO mapping needed!).
/// </summary>
public class BargeSeriesEditViewModel
{
    /// <summary>
    /// BargeSeries ID (0 for new records).
    /// </summary>
    public int BargeSeriesID { get; set; }

    /// <summary>
    /// Series name.
    /// </summary>
    [Required(ErrorMessage = "Series is required.")]
    [StringLength(50, ErrorMessage = "Series exceeds maximum length of 50.")]
    [Display(Name = "Series")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Customer ID (owner).
    /// </summary>
    [Required(ErrorMessage = "Owner is required.")]
    [Display(Name = "Owner")]
    public int CustomerID { get; set; }

    /// <summary>
    /// Hull type code.
    /// </summary>
    [Required(ErrorMessage = "Hull type is required.")]
    [StringLength(1, ErrorMessage = "Hull type exceeds maximum length of 1.")]
    [Display(Name = "Hull Type")]
    public string HullType { get; set; } = string.Empty;

    /// <summary>
    /// Cover type code.
    /// </summary>
    [Required(ErrorMessage = "Cover type is required.")]
    [StringLength(3, ErrorMessage = "Cover type exceeds maximum length of 3.")]
    [Display(Name = "Cover Type")]
    public string CoverType { get; set; } = string.Empty;

    /// <summary>
    /// Barge length in feet.
    /// </summary>
    [Required(ErrorMessage = "Length is required.")]
    [Range(0, double.MaxValue, ErrorMessage = "Length must be non-negative.")]
    [Display(Name = "Length (ft)")]
    public decimal? Length { get; set; }

    /// <summary>
    /// Barge width in feet.
    /// </summary>
    [Required(ErrorMessage = "Width is required.")]
    [Range(0, double.MaxValue, ErrorMessage = "Width must be non-negative.")]
    [Display(Name = "Width (ft)")]
    public decimal? Width { get; set; }

    /// <summary>
    /// Barge depth in feet.
    /// </summary>
    [Required(ErrorMessage = "Depth is required.")]
    [Range(0, double.MaxValue, ErrorMessage = "Depth must be non-negative.")]
    [Display(Name = "Depth (ft)")]
    public decimal? Depth { get; set; }

    /// <summary>
    /// Tonnage per inch of draft.
    /// </summary>
    [Required(ErrorMessage = "Tons/inch is required.")]
    [Range(0, double.MaxValue, ErrorMessage = "Tons/inch must be non-negative.")]
    [Display(Name = "Tons/Inch")]
    public decimal? TonsPerInch { get; set; }

    /// <summary>
    /// Light draft - feet portion.
    /// UI splits DraftLight into feet + inches inputs.
    /// </summary>
    [Required(ErrorMessage = "Light draft feet is required.")]
    [Range(0, 99, ErrorMessage = "Light draft feet must be between 0 and 99.")]
    [Display(Name = "Light Draft (ft)")]
    public int? DraftLightFeet { get; set; }

    /// <summary>
    /// Light draft - inches portion.
    /// UI splits DraftLight into feet + inches inputs.
    /// </summary>
    [Required(ErrorMessage = "Light draft inches is required.")]
    [Range(0, 11, ErrorMessage = "Light draft inches must be between 0 and 11.")]
    [Display(Name = "Light Draft (in)")]
    public int? DraftLightInches { get; set; }

    /// <summary>
    /// Active status.
    /// </summary>
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Draft tonnage records (typically 14 rows for feet 0-13).
    /// Contains DTOs from BargeOps.Shared.
    /// </summary>
    public List<BargeSeriesDraftDto> Drafts { get; set; } = new();

    /// <summary>
    /// List of customers for dropdown.
    /// </summary>
    public IEnumerable<SelectListItem> Customers { get; set; } = Enumerable.Empty<SelectListItem>();

    /// <summary>
    /// List of hull types for dropdown.
    /// </summary>
    public IEnumerable<SelectListItem> HullTypes { get; set; } = Enumerable.Empty<SelectListItem>();

    /// <summary>
    /// List of cover types for dropdown.
    /// </summary>
    public IEnumerable<SelectListItem> CoverTypes { get; set; } = Enumerable.Empty<SelectListItem>();

    /// <summary>
    /// User permission: can the user modify this record?
    /// </summary>
    public bool CanModify { get; set; }

    /// <summary>
    /// Is this a read-only view?
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Helper method to convert feet/inches back to decimal for saving.
    /// Called in controller before sending to API.
    /// </summary>
    public decimal GetDraftLightDecimal()
    {
        var feet = DraftLightFeet ?? 0;
        var inches = DraftLightInches ?? 0;
        return feet + (inches / 12m);
    }

    /// <summary>
    /// Helper method to set feet/inches from decimal.
    /// Called in controller after loading from API.
    /// </summary>
    public void SetDraftLightFromDecimal(decimal? draftLight)
    {
        if (draftLight.HasValue)
        {
            DraftLightFeet = (int)draftLight.Value;
            DraftLightInches = (int)((draftLight.Value - DraftLightFeet.Value) * 12);
        }
    }

    /// <summary>
    /// Converts ViewModel to DTO for API calls.
    /// Maps ViewModel properties to BargeSeriesDto from Shared project.
    /// </summary>
    public BargeSeriesDto ToDto()
    {
        return new BargeSeriesDto
        {
            BargeSeriesID = BargeSeriesID,
            CustomerID = CustomerID,
            Name = Name,
            HullType = HullType,
            CoverType = CoverType,
            Length = Length,
            Width = Width,
            Depth = Depth,
            TonsPerInch = TonsPerInch,
            DraftLight = GetDraftLightDecimal(),
            IsActive = IsActive,
            Drafts = Drafts
        };
    }

    /// <summary>
    /// Populates ViewModel from DTO.
    /// Maps BargeSeriesDto properties to ViewModel.
    /// </summary>
    public static BargeSeriesEditViewModel FromDto(BargeSeriesDto dto)
    {
        var viewModel = new BargeSeriesEditViewModel
        {
            BargeSeriesID = dto.BargeSeriesID,
            CustomerID = dto.CustomerID,
            Name = dto.Name,
            HullType = dto.HullType,
            CoverType = dto.CoverType,
            Length = dto.Length,
            Width = dto.Width,
            Depth = dto.Depth,
            TonsPerInch = dto.TonsPerInch,
            IsActive = dto.IsActive,
            Drafts = dto.Drafts
        };

        viewModel.SetDraftLightFromDecimal(dto.DraftLight);

        return viewModel;
    }
}
