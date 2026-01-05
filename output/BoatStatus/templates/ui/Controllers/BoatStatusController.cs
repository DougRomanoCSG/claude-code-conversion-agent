using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authentication.Cookies;
using BargeOps.Shared.Dto;
using BargeOpsAdmin.Services;
using BargeOpsAdmin.ViewModels;

namespace BargeOpsAdmin.Controllers;

/// <summary>
/// MVC Controller for Boat Status (BoatMaintenanceLog) management
/// ⭐ Authentication: Cookie-based with IdentityConstants.ApplicationScheme
/// ⭐ Uses ViewModels containing DTOs from BargeOps.Shared
/// </summary>
[Authorize(AuthenticationSchemes = IdentityConstants.ApplicationScheme)]
public class BoatStatusController : Controller
{
    private readonly IBoatMaintenanceLogService _maintenanceLogService;
    private readonly IBoatLocationService _boatLocationService;
    private readonly IValidationListService _validationListService;
    private readonly IFacilityService _facilityService;
    private readonly IBoatRoleService _boatRoleService;
    private readonly ILogger<BoatStatusController> _logger;

    public BoatStatusController(
        IBoatMaintenanceLogService maintenanceLogService,
        IBoatLocationService boatLocationService,
        IValidationListService validationListService,
        IFacilityService facilityService,
        IBoatRoleService boatRoleService,
        ILogger<BoatStatusController> logger)
    {
        _maintenanceLogService = maintenanceLogService ?? throw new ArgumentNullException(nameof(maintenanceLogService));
        _boatLocationService = boatLocationService ?? throw new ArgumentNullException(nameof(boatLocationService));
        _validationListService = validationListService ?? throw new ArgumentNullException(nameof(validationListService));
        _facilityService = facilityService ?? throw new ArgumentNullException(nameof(facilityService));
        _boatRoleService = boatRoleService ?? throw new ArgumentNullException(nameof(boatRoleService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Display boat status form for a boat
    /// </summary>
    /// <param name="locationId">BoatLocation ID</param>
    [HttpGet]
    public async Task<IActionResult> Edit(int locationId)
    {
        try
        {
            var boat = await _boatLocationService.GetByIdAsync(locationId);
            if (boat == null)
            {
                return NotFound($"Boat with LocationID {locationId} not found");
            }

            var model = new BoatStatusEditViewModel
            {
                LocationID = locationId,
                BoatName = boat.LocationName ?? string.Empty,
                IsFleetBoat = boat.FleetID.HasValue && boat.FleetID.Value > 0,
                FleetID = boat.FleetID,
                IsNew = true,
                MaintenanceLog = new BoatMaintenanceLogDto
                {
                    LocationID = locationId,
                    MaintenanceType = "Boat Status",
                    StartDateTime = DateTime.Now
                }
            };

            // Load maintenance logs for grid
            model.MaintenanceLogs = (await _maintenanceLogService.GetByBoatIdAsync(locationId)).ToList();

            // Load dropdowns
            await PopulateDropdownsAsync(model);

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading boat status form for LocationID {LocationId}", locationId);
            return StatusCode(500, "An error occurred while loading the boat status form");
        }
    }

    /// <summary>
    /// DataTables server-side processing endpoint
    /// Returns maintenance logs for the grid
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> GetMaintenanceLogTable([FromBody] BoatStatusDataTableRequest request)
    {
        try
        {
            var logs = await _maintenanceLogService.GetByBoatIdAsync(request.LocationID);
            var logsArray = logs.ToArray();

            // Apply sorting
            if (request.Order.Any())
            {
                var orderColumn = request.Order[0].Column;
                var orderDir = request.Order[0].Dir;

                var columnName = request.Columns[orderColumn].Data;

                logsArray = orderDir == "asc"
                    ? logsArray.OrderBy(x => GetPropertyValue(x, columnName)).ToArray()
                    : logsArray.OrderByDescending(x => GetPropertyValue(x, columnName)).ToArray();
            }
            else
            {
                // Default sort: StartDateTime descending
                logsArray = logsArray.OrderByDescending(x => x.StartDateTime).ToArray();
            }

            // Apply paging
            var pagedLogs = logsArray.Skip(request.Start).Take(request.Length);

            var response = new BoatStatusDataTableResponse<BoatMaintenanceLogDto>
            {
                Draw = request.Draw,
                RecordsTotal = logsArray.Length,
                RecordsFiltered = logsArray.Length,
                Data = pagedLogs
            };

            return Json(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting maintenance log table data for LocationID {LocationId}", request.LocationID);
            return StatusCode(500, "An error occurred while retrieving maintenance log data");
        }
    }

    /// <summary>
    /// Get a single maintenance log for editing
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var log = await _maintenanceLogService.GetByIdAsync(id);
            if (log == null)
            {
                return NotFound();
            }

            return Json(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting maintenance log {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the maintenance log");
        }
    }

    /// <summary>
    /// Save (create or update) a maintenance log
    /// ⭐ Validates conditional business rules
    /// ⭐ Clears unused fields based on MaintenanceType
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveMaintenanceLog(BoatStatusEditViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (model.MaintenanceLog.BoatMaintenanceLogID == 0)
            {
                // Create new
                var newId = await _maintenanceLogService.CreateAsync(model.MaintenanceLog);
                TempData["SuccessMessage"] = "Boat status entry created successfully";
                return Json(new { success = true, id = newId });
            }
            else
            {
                // Update existing
                await _maintenanceLogService.UpdateAsync(model.MaintenanceLog);
                TempData["SuccessMessage"] = "Boat status entry updated successfully";
                return Json(new { success = true, id = model.MaintenanceLog.BoatMaintenanceLogID });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving maintenance log");
            return StatusCode(500, new { success = false, error = "An error occurred while saving the maintenance log" });
        }
    }

    /// <summary>
    /// Delete a maintenance log
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _maintenanceLogService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Boat status entry deleted successfully";
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting maintenance log {Id}", id);
            return StatusCode(500, new { success = false, error = "An error occurred while deleting the maintenance log" });
        }
    }

    /// <summary>
    /// Get port facilities filtered by division (cascading dropdown)
    /// ⭐ Different data source based on IsFleetBoat
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPortFacilitiesByDivision(string division, int locationId)
    {
        try
        {
            var boat = await _boatLocationService.GetByIdAsync(locationId);
            if (boat == null)
            {
                return Json(new List<SelectListItem>());
            }

            IEnumerable<SelectListItem> facilities;

            if (boat.FleetID.HasValue && boat.FleetID.Value > 0)
            {
                // Fleet boat: Get facilities from fleet
                facilities = await _facilityService.GetFleetFacilitiesAsync(boat.FleetID.Value, division);
            }
            else
            {
                // Non-fleet boat: Get all facilities for division
                facilities = await _facilityService.GetByDivisionAsync(division);
            }

            return Json(facilities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting port facilities for division {Division}", division);
            return Json(new List<SelectListItem>());
        }
    }

    /// <summary>
    /// Populate all dropdown lists in the ViewModel
    /// </summary>
    private async Task PopulateDropdownsAsync(BoatStatusEditViewModel model)
    {
        // Status list (for MaintenanceType = 'Boat Status')
        var statusList = await _validationListService.GetValidationListAsync("BoatStatus");
        model.StatusList = statusList.Select(x => new SelectListItem
        {
            Value = x.Value,
            Text = x.Text
        });

        // Division list (for MaintenanceType = 'Change Division/Facility')
        // ⭐ Exclude 'Freight' division as per legacy logic
        var divisionList = await _validationListService.GetValidationListAsync("Division");
        model.Divisions = divisionList
            .Where(x => x.Text != "Freight")
            .Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text
            });

        // Port Facility list (initially empty, populated by cascading from Division)
        model.PortFacilities = new List<SelectListItem>();

        // Boat Role list (for MaintenanceType = 'Change Boat Role')
        var boatRoleList = await _boatRoleService.GetAllAsync();
        model.BoatRoles = boatRoleList.Select(x => new SelectListItem
        {
            Value = x.BoatRoleID.ToString(),
            Text = x.BoatRole
        });
    }

    /// <summary>
    /// Helper method to get property value by name for sorting
    /// </summary>
    private static object? GetPropertyValue(object obj, string propertyName)
    {
        return obj.GetType().GetProperty(propertyName)?.GetValue(obj, null);
    }
}

/// <summary>
/// IdentityConstants for authentication (matches user instructions)
/// </summary>
public static class IdentityConstants
{
    public const string ApplicationScheme = "Identity.Application";
}
