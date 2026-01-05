using System;
using System.Linq;
using System.Threading.Tasks;
using BargeOps.Shared.Dto;
using BargeOpsAdmin.AppClasses;
using BargeOpsAdmin.ViewModels;
using BargeOpsAdmin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BargeOpsAdmin.Controllers;

/// <summary>
/// MVC Controller for River search and edit
/// Uses IdentityConstants.ApplicationScheme (not Cookies)
/// </summary>
[Authorize]
[Route("[controller]")]
public class RiverController : AppController
{
    private readonly IRiverService _riverService;
    private readonly ILogger<RiverController> _logger;
    private readonly AppSession _appSession;

    public RiverController(
        IRiverService riverService,
        ILogger<RiverController> logger,
        AppSession appSession) : base(appSession)
    {
        _riverService = riverService;
        _logger = logger;
        _appSession = appSession;
    }

    /// <summary>
    /// Index/Search page
    /// </summary>
    [HttpGet]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        try
        {
            await InitSessionVariables(_appSession);

            var model = new RiverSearchViewModel
            {
                ActiveOnly = true
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading river search page");
            return View("Error");
        }
    }

    /// <summary>
    /// DataTables server-side processing endpoint
    /// </summary>
    [HttpPost("RiverTable")]
    public async Task<IActionResult> RiverTable([FromBody] RiverSearchRequest request)
    {
        try
        {
            var result = await _riverService.GetRiversAsync(request);

            return Json(new
            {
                draw = result.Draw,
                recordsTotal = result.RecordsTotal,
                recordsFiltered = result.RecordsFiltered,
                data = result.Data.Select(r => new
                {
                    riverId = r.RiverID,
                    code = r.Code ?? "",
                    name = r.Name ?? "",
                    startMile = r.StartMile?.ToString("0.00") ?? "",
                    endMile = r.EndMile?.ToString("0.00") ?? "",
                    isLowToHighDirection = r.IsLowToHighDirection,
                    isActive = r.IsActive,
                    upLabel = r.UpLabel ?? "",
                    downLabel = r.DownLabel ?? "",
                    bargeExCode = r.BargeExCode ?? ""
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting river table data");
            return Json(new { error = "Failed to load river data" });
        }
    }

    /// <summary>
    /// Create new river (GET)
    /// </summary>
    [HttpGet("Create")]
    public IActionResult Create()
    {
        try
        {
            var model = new RiverEditViewModel
            {
                River = new RiverDto
                {
                    IsActive = true,
                    IsLowToHighDirection = true
                }
            };

            return View("Edit", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading create river page");
            return View("Error");
        }
    }

    /// <summary>
    /// Edit existing river (GET)
    /// </summary>
    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var river = await _riverService.GetByIdAsync(id);

            if (river == null)
            {
                TempData["ErrorMessage"] = "River not found";
                return RedirectToAction(nameof(Index));
            }

            var model = new RiverEditViewModel
            {
                River = river
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading edit river page for ID {RiverId}", id);
            return View("Error");
        }
    }

    /// <summary>
    /// Save river (POST) - handles both create and update
    /// </summary>
    [HttpPost("Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RiverEditViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Ensure uppercase Code
            model.River.Code = model.River.Code?.ToUpper();

            ApiFetchResult result;

            if (model.IsNew)
            {
                result = await _riverService.CreateRiverAsync(model.River);
            }
            else
            {
                result = await _riverService.UpdateRiverAsync(model.River.RiverID, model.River);
            }

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving river");
            ModelState.AddModelError(string.Empty, "An error occurred while saving the river");
            return View(model);
        }
    }

    /// <summary>
    /// Delete river (POST) - soft delete
    /// </summary>
    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _riverService.DeleteRiverAsync(id);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting river {RiverId}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the river";
            return RedirectToAction(nameof(Index));
        }
    }
}
