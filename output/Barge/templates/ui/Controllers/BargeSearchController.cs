using BargeOpsAdmin.ViewModels;
using BargeOpsAdmin.AppClasses;
using BargeOpsAdmin.Enums;
using BargeOps.Shared.Dto;
using BargeOpsAdmin.Services;
using CsgAuthorization.AspNetCore.Handlers;
using CsgAuthorization.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BargeOpsAdmin.Controllers;

/// <summary>
/// MVC Controller for Barge search and edit operations
/// Uses ViewModels that contain DTOs from BargeOps.Shared
/// </summary>
[Authorize]
[Route("[controller]")]
public class BargeSearchController : AppController
{
    private readonly IBargeService _bargeService;
    private readonly ILogger<BargeSearchController> _logger;
    private readonly AppSession _appSession;
    private readonly IConfiguration _configuration;

    // TODO: Add lookup services for dropdowns
    // private readonly ICustomerService _customerService;
    // private readonly ILocationService _locationService;
    // etc.

    public BargeSearchController(
        IBargeService bargeService,
        ILogger<BargeSearchController> logger,
        AppSession appSession,
        IConfiguration configuration) : base(appSession)
    {
        _bargeService = bargeService ?? throw new ArgumentNullException(nameof(bargeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appSession = appSession ?? throw new ArgumentNullException(nameof(appSession));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    #region Search

    /// <summary>
    /// Display search page
    /// </summary>
    [HttpGet("Index")]
    [RequirePermission<AuthPermissions>(AuthPermissions.Barge, PermissionAccessType.ReadOnly)]
    public async Task<IActionResult> Index()
    {
        try
        {
            await InitSessionVariables(_appSession);

            var model = new BargeSearchViewModel
            {
                ActiveOnly = true,
                OpenTicketsOnly = true,
                SelectedFleetID = _appSession.SelectedFleetId,
                CanModify = HasPermission(AuthPermissions.Barge, PermissionAccessType.Modify)
            };

            // Populate dropdowns
            await PopulateSearchDropdownsAsync(model);

            // Set feature flags
            model.IsFreightLicenseActive = _configuration.GetValue<bool>("Licenses:Freight");
            model.IsTerminalLicenseActive = _configuration.GetValue<bool>("Licenses:Terminal");
            model.IsCommodityInfoCustomizationActive = _configuration.GetValue<bool>("Customizations:CommodityInfo");

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading barge search page");
            return View("Error");
        }
    }

    /// <summary>
    /// DataTables endpoint for search results
    /// </summary>
    [HttpPost("BargeTable")]
    [RequirePermission<AuthPermissions>(AuthPermissions.Barge, PermissionAccessType.ReadOnly)]
    public async Task<IActionResult> BargeTable(DataTableRequest request, BargeSearchViewModel model)
    {
        try
        {
            // Convert ViewModel to SearchRequest DTO
            var searchRequest = model.ToSearchRequest();

            // Add DataTables paging/sorting parameters
            searchRequest.Start = request.Start;
            searchRequest.Length = request.Length;
            searchRequest.Draw = request.Draw;

            // Map DataTables column index to column name
            if (request.Order != null && request.Order.Any())
            {
                var orderColumn = request.Order.First();
                searchRequest.SortColumn = GetColumnName(orderColumn.Column);
                searchRequest.SortDirection = orderColumn.Dir;
            }

            // Call API service
            var result = await _bargeService.SearchAsync(searchRequest);

            if (result == null)
            {
                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = Array.Empty<object>()
                });
            }

            // Return DataTables JSON response
            return Json(new
            {
                draw = result.Draw,
                recordsTotal = result.TotalCount,
                recordsFiltered = result.FilteredCount,
                data = result.Data.Select(b => new
                {
                    bargeID = b.BargeID,
                    bargeNum = b.BargeNum ?? "",
                    hullType = b.HullType ?? "",
                    coverType = b.CoverType ?? "",
                    sizeCategory = b.SizeCategory ?? "",
                    loadStatus = b.LoadStatus ?? "",
                    status = b.Status ?? "",
                    equipmentType = b.EquipmentType ?? "",
                    locationName = b.LocationName ?? "",
                    customerName = b.CustomerName ?? "",
                    ownerName = b.OwnerName ?? "",
                    fleetName = b.FleetName ?? "",
                    commodityName = b.CommodityName ?? "",
                    isActive = b.IsActive,
                    isLeaker = b.IsLeaker,
                    damageLevel = b.DamageLevel ?? ""
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching barges");
            return Json(new
            {
                draw = request.Draw,
                recordsTotal = 0,
                recordsFiltered = 0,
                data = Array.Empty<object>(),
                error = "An error occurred while searching barges."
            });
        }
    }

    #endregion

    #region Edit

    /// <summary>
    /// Display edit page (new or existing barge)
    /// </summary>
    [HttpGet("Edit/{id?}")]
    [RequirePermission<AuthPermissions>(AuthPermissions.Barge, PermissionAccessType.Modify)]
    public async Task<IActionResult> Edit(int? id)
    {
        try
        {
            await InitSessionVariables(_appSession);

            var model = new BargeEditViewModel
            {
                CanModify = HasPermission(AuthPermissions.Barge, PermissionAccessType.Modify),
                IsReadOnly = false
            };

            if (id.HasValue)
            {
                // Editing existing barge
                var barge = await _bargeService.GetByIdAsync(id.Value);

                if (barge == null)
                {
                    return NotFound();
                }

                model.LoadFromBarge(barge);
            }
            else
            {
                // Creating new barge
                model.Barge = new BargeDto
                {
                    IsActive = true,
                    EquipmentType = "F" // Default to Fleet-owned
                };
            }

            // Populate dropdowns
            await PopulateEditDropdownsAsync(model);

            // Set feature flags
            model.IsBargeSeriesCustomizationActive = _configuration.GetValue<bool>("Customizations:BargeSeries");
            model.IsBargeCharterSupportCustomizationActive = _configuration.GetValue<bool>("Customizations:BargeCharterSupport");
            model.IsFreightActive = _configuration.GetValue<bool>("Licenses:Freight");
            model.IsTerminalMode = _configuration.GetValue<bool>("Licenses:Terminal");
            model.EnableCoverTypeSpecialLogic = _configuration.GetValue<bool>("GlobalSettings:EnableCoverTypeSpecialLogic");
            model.RequireBargeCoverType = _configuration.GetValue<bool>("GlobalSettings:RequireBargeCoverType");

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading barge edit page for ID {BargeId}", id);
            return View("Error");
        }
    }

    /// <summary>
    /// Save barge (create or update)
    /// </summary>
    [HttpPost("Edit")]
    [RequirePermission<AuthPermissions>(AuthPermissions.Barge, PermissionAccessType.Modify)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BargeEditViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                // Repopulate dropdowns on validation failure
                await PopulateEditDropdownsAsync(model);
                return View(model);
            }

            // Convert ViewModel to DTO
            model.SaveToBarge();

            bool success;
            if (model.IsNew)
            {
                // Create new barge
                var bargeId = await _bargeService.CreateAsync(model.Barge);
                success = bargeId.HasValue;

                if (success)
                {
                    TempData["SuccessMessage"] = "Barge created successfully.";
                    return RedirectToAction(nameof(Edit), new { id = bargeId });
                }
            }
            else
            {
                // Update existing barge
                success = await _bargeService.UpdateAsync(model.Barge);

                if (success)
                {
                    TempData["SuccessMessage"] = "Barge updated successfully.";
                    return RedirectToAction(nameof(Edit), new { id = model.Barge.BargeID });
                }
            }

            ModelState.AddModelError("", "Failed to save barge. Please try again.");
            await PopulateEditDropdownsAsync(model);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving barge");
            ModelState.AddModelError("", "An error occurred while saving the barge.");
            await PopulateEditDropdownsAsync(model);
            return View(model);
        }
    }

    #endregion

    #region Delete

    /// <summary>
    /// Delete barge (soft delete)
    /// </summary>
    [HttpPost("Delete/{id}")]
    [RequirePermission<AuthPermissions>(AuthPermissions.Barge, PermissionAccessType.Delete)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var success = await _bargeService.DeleteAsync(id);

            if (success)
            {
                TempData["SuccessMessage"] = "Barge deleted successfully.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Failed to delete barge.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting barge {BargeId}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the barge.";
            return RedirectToAction(nameof(Edit), new { id });
        }
    }

    #endregion

    #region Barge Charters (AJAX endpoints)

    /// <summary>
    /// Get barge charters for a barge
    /// </summary>
    [HttpGet("GetCharters/{bargeId}")]
    [RequirePermission<AuthPermissions>(AuthPermissions.Barge, PermissionAccessType.ReadOnly)]
    public async Task<IActionResult> GetCharters(int bargeId)
    {
        try
        {
            var charters = await _bargeService.GetChartersAsync(bargeId);

            if (charters == null)
            {
                return Json(new { success = false, error = "Failed to load charters." });
            }

            return Json(new { success = true, data = charters });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting charters for barge {BargeId}", bargeId);
            return Json(new { success = false, error = "An error occurred while loading charters." });
        }
    }

    /// <summary>
    /// Save barge charter (create or update)
    /// </summary>
    [HttpPost("SaveCharter")]
    [RequirePermission<AuthPermissions>(AuthPermissions.Barge, PermissionAccessType.Modify)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCharter([FromBody] BargeCharterDto charter)
    {
        try
        {
            if (charter.BargeCharterID == 0)
            {
                // Create new charter
                var charterId = await _bargeService.CreateCharterAsync(charter);

                if (charterId.HasValue)
                {
                    return Json(new { success = true, charterId });
                }
            }
            else
            {
                // Update existing charter
                var success = await _bargeService.UpdateCharterAsync(charter);

                if (success)
                {
                    return Json(new { success = true });
                }
            }

            return Json(new { success = false, error = "Failed to save charter." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving charter for barge {BargeId}", charter.BargeID);
            return Json(new { success = false, error = "An error occurred while saving the charter." });
        }
    }

    /// <summary>
    /// Delete barge charter
    /// </summary>
    [HttpPost("DeleteCharter/{bargeId}/{charterId}")]
    [RequirePermission<AuthPermissions>(AuthPermissions.Barge, PermissionAccessType.Delete)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCharter(int bargeId, int charterId)
    {
        try
        {
            var success = await _bargeService.DeleteCharterAsync(bargeId, charterId);

            if (success)
            {
                return Json(new { success = true });
            }

            return Json(new { success = false, error = "Failed to delete charter." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting charter {CharterId} for barge {BargeId}", charterId, bargeId);
            return Json(new { success = false, error = "An error occurred while deleting the charter." });
        }
    }

    #endregion

    #region Helper Methods

    private async Task PopulateSearchDropdownsAsync(BargeSearchViewModel model)
    {
        // TODO: Call lookup services to populate dropdowns
        // model.Operators = await _customerService.GetOperatorsAsync();
        // model.Customers = await _customerService.GetCustomersAsync();
        // model.HullTypes = await _validationService.GetHullTypesAsync();
        // etc.

        // Placeholder - populate from configuration or hardcode
        model.Operators = new List<SelectListItem>();
        model.Customers = new List<SelectListItem>();
        model.HullTypes = new List<SelectListItem>();
        model.CoverTypes = new List<SelectListItem>();
        model.LoadStatuses = new List<SelectListItem>();
        model.Statuses = new List<SelectListItem>();
        model.EquipmentTypes = new List<SelectListItem>();
        model.SizeCategories = new List<SelectListItem>();
        model.Rivers = new List<SelectListItem>();
        model.Commodities = new List<SelectListItem>();
        model.BoatLocations = new List<SelectListItem>();
        model.FacilityLocations = new List<SelectListItem>();
        model.ShipLocations = new List<SelectListItem>();

        await Task.CompletedTask;
    }

    private async Task PopulateEditDropdownsAsync(BargeEditViewModel model)
    {
        // TODO: Call lookup services to populate dropdowns
        // model.Owners = await _customerService.GetOwnersAsync();
        // model.Operators = await _customerService.GetOperatorsAsync();
        // etc.

        // Placeholder - populate from configuration or hardcode
        model.Owners = new List<SelectListItem>();
        model.Operators = new List<SelectListItem>();
        model.HullTypes = new List<SelectListItem>();
        model.BargeSeries = new List<SelectListItem>();
        model.CoverTypes = new List<SelectListItem>();
        model.CoverConfigs = new List<SelectListItem>();
        model.CoverSubTypes = new List<SelectListItem>();
        model.Commodities = new List<SelectListItem>();
        model.LoadStatuses = new List<SelectListItem>();
        model.CleanStatuses = new List<SelectListItem>();
        model.RepairStatuses = new List<SelectListItem>();
        model.DamageLevels = new List<SelectListItem>();
        model.FreeboardRanges = new List<SelectListItem>();
        model.Fleets = new List<SelectListItem>();
        model.EquipmentTypes = new List<SelectListItem>();
        model.RakeDirections = new List<SelectListItem>();
        model.ColorPairs = new List<SelectListItem>();
        model.BargeTypes = new List<SelectListItem>();
        model.Locations = new List<SelectListItem>();

        await Task.CompletedTask;
    }

    private string GetColumnName(int columnIndex)
    {
        // Map DataTables column index to DTO property name for sorting
        return columnIndex switch
        {
            0 => "bargeNum",
            1 => "hullType",
            2 => "coverType",
            3 => "sizeCategory",
            4 => "loadStatus",
            5 => "status",
            6 => "equipmentType",
            7 => "locationName",
            8 => "customerName",
            _ => "bargeNum" // Default sort column
        };
    }

    private bool HasPermission(AuthPermissions permission, PermissionAccessType accessType)
    {
        // TODO: Implement permission check using authorization service
        return true;
    }

    #endregion
}

#region DataTable Request Model

/// <summary>
/// DataTables request parameters
/// </summary>
public class DataTableRequest
{
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public List<DataTableOrder>? Order { get; set; }
}

/// <summary>
/// DataTables order/sort parameters
/// </summary>
public class DataTableOrder
{
    public int Column { get; set; }
    public string Dir { get; set; } = "asc";
}

#endregion
