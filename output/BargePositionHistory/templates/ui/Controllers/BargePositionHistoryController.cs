using System;
using System.Linq;
using System.Threading.Tasks;
using BargeOps.Shared.Dto;
using BargeOpsAdmin.AppClasses;
using BargeOpsAdmin.Enums;
using BargeOpsAdmin.Services;
using BargeOpsAdmin.ViewModels;
using CsgAuthorization.AspNetCore.Handlers;
using CsgAuthorization.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

namespace BargeOpsAdmin.Controllers;

/// <summary>
/// MVC Controller for Barge Position History UI.
/// Target: C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Controllers\BargePositionHistoryController.cs
///
/// Uses IdentityConstants.ApplicationScheme for authentication (NOT "Cookies").
/// ViewModels contain all screen data (NO ViewBag/ViewData).
/// </summary>
[Authorize]
[Route("[controller]")]
public class BargePositionHistoryController : AppController
{
    private readonly IBargePositionHistoryService _bargePositionHistoryService;
    private readonly IFleetService _fleetService;
    private readonly ITierService _tierService;
    private readonly ILogger<BargePositionHistoryController> _logger;
    private readonly AppSession _appSession;

    public BargePositionHistoryController(
        IBargePositionHistoryService bargePositionHistoryService,
        IFleetService fleetService,
        ITierService tierService,
        ILogger<BargePositionHistoryController> logger,
        AppSession appSession) : base(appSession)
    {
        _bargePositionHistoryService = bargePositionHistoryService;
        _fleetService = fleetService;
        _tierService = tierService;
        _logger = logger;
        _appSession = appSession;
    }

    /// <summary>
    /// Index action - displays search form and grid.
    /// </summary>
    /// <param name="fleetId">Fleet ID from parent context (required)</param>
    [HttpGet]
    [HttpGet("Index")]
    [RequirePermission<AuthPermissions>(AuthPermissions.BargePositionHistoryView, PermissionAccessType.ReadOnly)]
    public async Task<IActionResult> Index(int fleetId)
    {
        try
        {
            if (fleetId <= 0)
            {
                _logger.LogWarning("Invalid FleetID: {FleetID}", fleetId);
                return BadRequest("FleetID is required.");
            }

            await InitSessionVariables(_appSession);

            // Get fleet name for display
            var fleet = await _fleetService.GetByIdAsync(fleetId);
            if (fleet == null)
            {
                _logger.LogWarning("Fleet not found: {FleetID}", fleetId);
                return NotFound($"Fleet with ID {fleetId} not found.");
            }

            // Get tier groups for dropdown
            var tierGroups = await _tierService.GetTierGroupsByFleetAsync(fleetId);

            var model = new BargePositionHistorySearchViewModel
            {
                FleetID = fleetId,
                FleetName = fleet.FleetName,
                SearchDate = DateTime.Today,
                TierGroups = tierGroups.Select(tg => new SelectListItem
                {
                    Value = tg.TierGroupID.ToString(),
                    Text = tg.TierGroupName
                }),
                CanModify = User.HasPermission(AuthPermissions.BargePositionHistoryModify),
                CanDelete = User.HasPermission(AuthPermissions.BargePositionHistoryDelete)
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading barge position history search page for FleetID: {FleetID}", fleetId);
            return View("Error");
        }
    }

    /// <summary>
    /// DataTables server-side processing endpoint.
    /// </summary>
    [HttpPost("SearchTable")]
    [RequirePermission<AuthPermissions>(AuthPermissions.BargePositionHistoryView, PermissionAccessType.ReadOnly)]
    public async Task<IActionResult> SearchTable([FromBody] BargePositionHistorySearchRequest request)
    {
        try
        {
            var result = await _bargePositionHistoryService.SearchAsync(request);

            return Json(new
            {
                draw = result.Draw,
                recordsTotal = result.RecordsTotal,
                recordsFiltered = result.RecordsFiltered,
                data = result.Data.Select(bph => new
                {
                    fleetPositionHistoryID = bph.FleetPositionHistoryID,
                    positionStartDateTime = bph.PositionStartDateTime.ToString("MM/dd/yyyy HH:mm"),
                    bargeNum = bph.BargeNum ?? "",
                    leftFleet = bph.LeftFleet,
                    tierName = bph.TierName ?? "",
                    tierPos = bph.TierPos ?? ""
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting barge position history search results");
            return Json(new
            {
                draw = request.Draw,
                recordsTotal = 0,
                recordsFiltered = 0,
                data = Array.Empty<object>(),
                error = "An error occurred while searching barge position history"
            });
        }
    }

    /// <summary>
    /// Create GET action - displays empty form.
    /// </summary>
    [HttpGet("Create")]
    [RequirePermission<AuthPermissions>(AuthPermissions.BargePositionHistoryModify, PermissionAccessType.Modify)]
    public async Task<IActionResult> Create(int fleetId, int? tierGroupId)
    {
        try
        {
            if (fleetId <= 0)
            {
                _logger.LogWarning("Invalid FleetID: {FleetID}", fleetId);
                return BadRequest("FleetID is required.");
            }

            // Get fleet name
            var fleet = await _fleetService.GetByIdAsync(fleetId);

            // Get tiers for dropdown (filtered by tier group if provided)
            var tiers = tierGroupId.HasValue
                ? await _tierService.GetTiersByGroupAsync(tierGroupId.Value)
                : await _tierService.GetTiersByFleetAsync(fleetId);

            var model = new BargePositionHistoryEditViewModel
            {
                FleetID = fleetId,
                FleetName = fleet?.FleetName,
                TierGroupID = tierGroupId,
                PositionStartDateTime = DateTime.Now,
                Tiers = tiers.Select(t => new SelectListItem
                {
                    Value = t.TierID.ToString(),
                    Text = t.TierName
                })
            };

            return View("Edit", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading create form for FleetID: {FleetID}", fleetId);
            return View("Error");
        }
    }

    /// <summary>
    /// Edit GET action - loads existing record.
    /// </summary>
    [HttpGet("Edit/{id}")]
    [RequirePermission<AuthPermissions>(AuthPermissions.BargePositionHistoryModify, PermissionAccessType.Modify)]
    public async Task<IActionResult> Edit(int id, int? tierGroupId)
    {
        try
        {
            var dto = await _bargePositionHistoryService.GetByIdAsync(id);
            if (dto == null)
            {
                _logger.LogWarning("Barge position history not found: {ID}", id);
                return NotFound($"Barge position history with ID {id} not found.");
            }

            // Get fleet name
            var fleet = await _fleetService.GetByIdAsync(dto.FleetID);

            // Get tiers for dropdown (filtered by tier group if provided)
            var tiers = tierGroupId.HasValue
                ? await _tierService.GetTiersByGroupAsync(tierGroupId.Value)
                : await _tierService.GetTiersByFleetAsync(dto.FleetID);

            var model = new BargePositionHistoryEditViewModel
            {
                FleetPositionHistoryID = dto.FleetPositionHistoryID,
                FleetID = dto.FleetID,
                FleetName = fleet?.FleetName,
                TierGroupID = tierGroupId,
                PositionStartDateTime = dto.PositionStartDateTime,
                BargeNum = dto.BargeNum,
                LeftFleet = dto.LeftFleet,
                TierID = dto.TierID,
                TierX = dto.TierX,
                TierY = dto.TierY,
                ModifyDateTime = dto.ModifyDateTime,
                Tiers = tiers.Select(t => new SelectListItem
                {
                    Value = t.TierID.ToString(),
                    Text = t.TierName
                })
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading edit form for ID: {ID}", id);
            return View("Error");
        }
    }

    /// <summary>
    /// Create/Update POST action - saves changes.
    /// </summary>
    [HttpPost("Save")]
    [RequirePermission<AuthPermissions>(AuthPermissions.BargePositionHistoryModify, PermissionAccessType.Modify)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(BargePositionHistoryEditViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                // Reload dropdowns
                var tiers = model.TierGroupID.HasValue
                    ? await _tierService.GetTiersByGroupAsync(model.TierGroupID.Value)
                    : await _tierService.GetTiersByFleetAsync(model.FleetID);

                model.Tiers = tiers.Select(t => new SelectListItem
                {
                    Value = t.TierID.ToString(),
                    Text = t.TierName
                });

                return View("Edit", model);
            }

            // Map ViewModel to DTO
            var dto = new BargePositionHistoryDto
            {
                FleetPositionHistoryID = model.FleetPositionHistoryID,
                FleetID = model.FleetID,
                BargeNum = model.BargeNum,
                PositionStartDateTime = model.PositionStartDateTime,
                LeftFleet = model.LeftFleet,
                TierID = model.LeftFleet ? null : model.TierID,
                TierX = model.LeftFleet ? null : model.TierX,
                TierY = model.LeftFleet ? null : model.TierY,
                ModifyDateTime = model.ModifyDateTime
            };

            if (model.FleetPositionHistoryID == 0)
            {
                // Create
                await _bargePositionHistoryService.CreateAsync(dto);
                TempData["SuccessMessage"] = "Barge position history created successfully.";
            }
            else
            {
                // Update
                await _bargePositionHistoryService.UpdateAsync(dto);
                TempData["SuccessMessage"] = "Barge position history updated successfully.";
            }

            return RedirectToAction(nameof(Index), new { fleetId = model.FleetID });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving barge position history");
            ModelState.AddModelError("", ex.Message);

            // Reload dropdowns
            var tiers = model.TierGroupID.HasValue
                ? await _tierService.GetTiersByGroupAsync(model.TierGroupID.Value)
                : await _tierService.GetTiersByFleetAsync(model.FleetID);

            model.Tiers = tiers.Select(t => new SelectListItem
            {
                Value = t.TierID.ToString(),
                Text = t.TierName
            });

            return View("Edit", model);
        }
    }

    /// <summary>
    /// Delete POST action - removes record.
    /// </summary>
    [HttpPost("Delete/{id}")]
    [RequirePermission<AuthPermissions>(AuthPermissions.BargePositionHistoryDelete, PermissionAccessType.FullControl)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _bargePositionHistoryService.DeleteAsync(id);
            return Json(new { success = true, message = "Barge position history deleted successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting barge position history with ID: {ID}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Validate barge number endpoint (for remote validation).
    /// </summary>
    [HttpGet("ValidateBarge")]
    public async Task<IActionResult> ValidateBarge(string bargeNum)
    {
        try
        {
            var isValid = await _bargePositionHistoryService.ValidateBargeNumAsync(bargeNum);
            return Json(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating barge number: {BargeNum}", bargeNum);
            return Json(false);
        }
    }
}
