using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using BargeOps.Shared.Dto;

namespace BargeOpsAdmin.ViewModels;

/// <summary>
/// ViewModel for Barge search screen
/// Contains search criteria and dropdown lists
/// ⭐ Uses BargeSearchRequest DTO from BargeOps.Shared directly
/// </summary>
public class BargeSearchViewModel
{
    #region Basic Search Criteria

    [Display(Name = "Barge Number")]
    public string? BargeNum { get; set; }

    [Display(Name = "Operator")]
    public int? OperatorID { get; set; }

    [Display(Name = "Status")]
    public string? Status { get; set; }

    [Display(Name = "Hull Type")]
    public string? HullType { get; set; }

    [Display(Name = "Cover Type")]
    public string? CoverType { get; set; }

    [Display(Name = "Ticket ID")]
    public int? TicketID { get; set; }

    [Display(Name = "Customer")]
    public int? CustomerID { get; set; }

    [Display(Name = "Load Status")]
    public string? LoadStatus { get; set; }

    [Display(Name = "Open Tickets Only")]
    public bool OpenTicketsOnly { get; set; } = true;

    [Display(Name = "Active Only")]
    public bool ActiveOnly { get; set; } = true;

    #endregion

    #region Advanced Search Criteria

    [Display(Name = "Boat Search Type")]
    public string? BoatSearchType { get; set; }

    [Display(Name = "Boat Location")]
    public int? BoatLocationID { get; set; }

    [Display(Name = "Facility Search Type")]
    public string? FacilitySearchType { get; set; }

    [Display(Name = "Facility Location")]
    public int? FacilityLocationID { get; set; }

    [Display(Name = "Ship Search Type")]
    public string? ShipSearchType { get; set; }

    [Display(Name = "Ship Location")]
    public int? ShipLocationID { get; set; }

    [Display(Name = "Equipment Type")]
    public string? EquipmentType { get; set; }

    [Display(Name = "USCG Number")]
    public string? UscgNum { get; set; }

    [Display(Name = "Size Category")]
    public string? SizeCategory { get; set; }

    [Display(Name = "River")]
    public string? RiverID { get; set; }

    [Display(Name = "Start Mile")]
    public decimal? StartMile { get; set; }

    [Display(Name = "End Mile")]
    public decimal? EndMile { get; set; }

    [Display(Name = "Commodity")]
    public int? CommodityID { get; set; }

    [Display(Name = "Contract Number")]
    public string? ContractNumber { get; set; }

    #endregion

    #region Dropdowns (SelectListItem collections)

    public IEnumerable<SelectListItem> Operators { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Customers { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> HullTypes { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> CoverTypes { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> LoadStatuses { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Statuses { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> EquipmentTypes { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> SizeCategories { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Rivers { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Commodities { get; set; } = new List<SelectListItem>();

    // Boat search dropdowns
    public IEnumerable<SelectListItem> BoatSearchTypes { get; set; } = new List<SelectListItem>
    {
        new SelectListItem { Value = "InTow", Text = "In Tow" },
        new SelectListItem { Value = "ScheduledIn", Text = "Scheduled In" },
        new SelectListItem { Value = "ScheduledOut", Text = "Scheduled Out" }
    };
    public IEnumerable<SelectListItem> BoatLocations { get; set; } = new List<SelectListItem>();

    // Facility search dropdowns
    public IEnumerable<SelectListItem> FacilitySearchTypes { get; set; } = new List<SelectListItem>
    {
        new SelectListItem { Value = "AtFacility", Text = "At Facility" },
        new SelectListItem { Value = "ConsignedToFacility", Text = "Consigned to Facility" },
        new SelectListItem { Value = "DestinationIn", Text = "Destination In" },
        new SelectListItem { Value = "DestinationOut", Text = "Destination Out" },
        new SelectListItem { Value = "OnOrderToFacility", Text = "On Order to Facility" }
    };
    public IEnumerable<SelectListItem> FacilityLocations { get; set; } = new List<SelectListItem>();

    // Ship search dropdowns
    public IEnumerable<SelectListItem> ShipSearchTypes { get; set; } = new List<SelectListItem>
    {
        new SelectListItem { Value = "ConsignedToShip", Text = "Consigned to Ship" },
        new SelectListItem { Value = "OnOrderToShip", Text = "On Order to Ship" }
    };
    public IEnumerable<SelectListItem> ShipLocations { get; set; } = new List<SelectListItem>();

    #endregion

    #region Feature Flags and Context

    /// <summary>
    /// Freight license active flag (shows calculated draft fields)
    /// </summary>
    public bool IsFreightLicenseActive { get; set; }

    /// <summary>
    /// Terminal license active flag (affects required field validation)
    /// </summary>
    public bool IsTerminalLicenseActive { get; set; }

    /// <summary>
    /// Commodity info customization active flag
    /// </summary>
    public bool IsCommodityInfoCustomizationActive { get; set; }

    /// <summary>
    /// Selected fleet ID from user context
    /// </summary>
    public int SelectedFleetID { get; set; }

    /// <summary>
    /// User has permission to modify barges
    /// </summary>
    public bool CanModify { get; set; }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Convert ViewModel to BargeSearchRequest DTO for API call
    /// </summary>
    public BargeSearchRequest ToSearchRequest()
    {
        return new BargeSearchRequest
        {
            SelectedFleetID = SelectedFleetID,
            BargeNum = BargeNum,
            HullType = HullType,
            CoverType = CoverType,
            OperatorID = OperatorID,
            CustomerID = CustomerID,
            ActiveOnly = ActiveOnly,
            TicketID = TicketID,
            LoadStatus = LoadStatus,
            Status = Status,
            OpenTicketsOnly = OpenTicketsOnly,
            EquipmentType = EquipmentType,
            UscgNum = UscgNum,
            SizeCategory = SizeCategory,
            River = RiverID,
            StartMile = StartMile,
            EndMile = EndMile,
            ContractNumber = ContractNumber,
            CommodityID = CommodityID,
            BoatSearchType = BoatSearchType,
            BoatLocationID = BoatLocationID,
            FacilitySearchType = FacilitySearchType,
            FacilityLocationID = FacilityLocationID,
            ShipSearchType = ShipSearchType,
            ShipLocationID = ShipLocationID
        };
    }

    #endregion
}
