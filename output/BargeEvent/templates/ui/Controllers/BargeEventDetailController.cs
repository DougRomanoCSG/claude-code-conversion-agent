using BargeOpsAdmin.ViewModels;
using BargeOpsAdmin.AppClasses;
using BargeOpsAdmin.Enums;
using BargeOps.Shared.Dto;
using BargeOpsAdmin.Services;
using CsgAuthorization.AspNetCore.Handlers;
using CsgAuthorization.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

namespace BargeOpsAdmin.Controllers;

/// <summary>
/// UI Controller for BargeEvent create/edit/detail operations.
/// Handles individual barge event CRUD operations.
/// </summary>
[Authorize(AuthenticationSchemes = IdentityConstants.ApplicationScheme)]
[Route("[controller]")]
public class BargeEventDetailController : AppController
{
    private readonly IBargeEventService _bargeEventService;
    private readonly ILookupService _lookupService;
    private readonly ILogger<BargeEventDetailController> _logger;
    private readonly AppSession _appSession;

    public BargeEventDetailController(
        IBargeEventService bargeEventService,
        ILookupService lookupService,
        ILogger<BargeEventDetailController> logger,
        AppSession appSession) : base(appSession)
    {
        _bargeEventService = bargeEventService ?? throw new ArgumentNullException(nameof(bargeEventService));
        _lookupService = lookupService ?? throw new ArgumentNullException(nameof(lookupService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appSession = appSession ?? throw new ArgumentNullException(nameof(appSession));
    }

    /// <summary>
    /// Display edit form for existing event or read-only details
    /// </summary>
    [HttpGet("Edit/{id}")]
    [RequirePermission<AuthPermissions>(AuthPermissions.BargeEventView, PermissionAccessType.ReadOnly)]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            await InitSessionVariables(_appSession);

            var bargeEvent = await _bargeEventService.GetByIdAsync(id);

            if (bargeEvent == null)
            {
                TempData["ErrorMessage"] = $"Barge event {id} not found";
                return RedirectToAction("Index", "BargeEventSearch");
            }

            var model = await BuildEditViewModel(bargeEvent);

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading barge event {Id}", id);
            TempData["ErrorMessage"] = "Error loading barge event";
            return RedirectToAction("Index", "BargeEventSearch");
        }
    }

    /// <summary>
    /// Display create form for new event
    /// </summary>
    [HttpGet("Create")]
    [RequirePermission<AuthPermissions>(AuthPermissions.BargeEventModify, PermissionAccessType.Modify)]
    public async Task<IActionResult> Create(int? ticketId = null)
    {
        try
        {
            await InitSessionVariables(_appSession);

            var model = await BuildEditViewModel(new BargeEventDto
            {
                TicketID = ticketId ?? 0,
                FleetID = FleetID,
                StartDateTime = DateTime.Now,
                VoidStatus = 0
            });

            return View("Edit", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading create barge event form");
            TempData["ErrorMessage"] = "Error loading create form";
            return RedirectToAction("Index", "BargeEventSearch");
        }
    }

    /// <summary>
    /// Save event (create or update)
    /// </summary>
    [HttpPost("Save")]
    [RequirePermission<AuthPermissions>(AuthPermissions.BargeEventModify, PermissionAccessType.Modify)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(BargeEventEditViewModel model)
    {
        try
        {
            // Combine split date/time fields
            CombineDateTimeFields(model);

            if (!ModelState.IsValid)
            {
                // Reload dropdown lists
                await PopulateDropdowns(model);
                return View("Edit", model);
            }

            BargeEventDto result;

            if (model.BargeEvent.TicketEventID == 0)
            {
                // Create new event
                result = await _bargeEventService.CreateAsync(model.BargeEvent);
                TempData["SuccessMessage"] = $"Barge event created successfully: {result.EventTypeName}";
            }
            else
            {
                // Update existing event
                await _bargeEventService.UpdateAsync(model.BargeEvent);
                result = model.BargeEvent;
                TempData["SuccessMessage"] = "Barge event updated successfully";
            }

            return RedirectToAction("Edit", new { id = result.TicketEventID });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation failed for barge event save");
            ModelState.AddModelError("", ex.Message);

            await PopulateDropdowns(model);
            return View("Edit", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving barge event");
            ModelState.AddModelError("", "An error occurred while saving the barge event");

            await PopulateDropdowns(model);
            return View("Edit", model);
        }
    }

    /// <summary>
    /// Delete (void) event
    /// </summary>
    [HttpPost("Delete/{id}")]
    [RequirePermission<AuthPermissions>(AuthPermissions.BargeEventModify, PermissionAccessType.Modify)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var success = await _bargeEventService.DeleteAsync(id);

            if (success)
            {
                TempData["SuccessMessage"] = "Barge event voided successfully";
            }
            else
            {
                TempData["ErrorMessage"] = "Barge event not found";
            }

            return RedirectToAction("Index", "BargeEventSearch");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot void barge event {Id}", id);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("Edit", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error voiding barge event {Id}", id);
            TempData["ErrorMessage"] = "Error voiding barge event";
            return RedirectToAction("Edit", new { id });
        }
    }

    /// <summary>
    /// Get barges for event (AJAX endpoint for barge list grid)
    /// </summary>
    [HttpGet("GetBarges/{id}")]
    [RequirePermission<AuthPermissions>(AuthPermissions.BargeEventView, PermissionAccessType.ReadOnly)]
    public async Task<IActionResult> GetBarges(int id)
    {
        try
        {
            var barges = await _bargeEventService.GetBargesAsync(id);

            return Json(new { success = true, data = barges });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting barges for event {Id}", id);
            return Json(new { success = false, message = "Error loading barges" });
        }
    }

    /// <summary>
    /// Get billing audits for event (AJAX endpoint for billing audit tab)
    /// </summary>
    [HttpGet("GetBillingAudits/{id}")]
    [RequirePermission<AuthPermissions>(AuthPermissions.BargeEventBillingView, PermissionAccessType.ReadOnly)]
    public async Task<IActionResult> GetBillingAudits(int id)
    {
        try
        {
            var audits = await _bargeEventService.GetBillingAuditsAsync(id);

            return Json(new { success = true, data = audits });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting billing audits for event {Id}", id);
            return Json(new { success = false, message = "Error loading billing audits" });
        }
    }

    // ===== HELPER METHODS =====

    private async Task<BargeEventEditViewModel> BuildEditViewModel(BargeEventDto bargeEvent)
    {
        var model = new BargeEventEditViewModel
        {
            BargeEvent = bargeEvent,

            // License flags
            IsFreightActive = await _lookupService.IsLicenseActiveAsync("Freight"),
            IsOnboardActive = await _lookupService.IsLicenseActiveAsync("Onboard"),
            IsTerminalActive = await _lookupService.IsLicenseActiveAsync("Terminal"),
            IsTowingActive = await _lookupService.IsLicenseActiveAsync("Towing"),

            // Permission flags
            CanModify = User.HasPermission(AuthPermissions.BargeEventModify),
            CanViewBilling = User.HasPermission(AuthPermissions.BargeEventBillingView),
            CanModifyBilling = User.HasPermission(AuthPermissions.BargeEventBillingModify)
        };

        // Split datetime fields for UI (24-hour time inputs)
        SplitDateTimeFields(model);

        // Load dropdown lists
        await PopulateDropdowns(model);

        // Load related data if editing existing event
        if (bargeEvent.TicketEventID > 0)
        {
            model.Barges = (await _bargeEventService.GetBargesAsync(bargeEvent.TicketEventID)).ToList();

            if (model.CanViewBilling)
            {
                model.BillingAudits = (await _bargeEventService.GetBillingAuditsAsync(bargeEvent.TicketEventID)).ToList();
            }
        }

        return model;
    }

    private async Task PopulateDropdowns(BargeEventEditViewModel model)
    {
        model.EventTypeList = await _lookupService.GetEventTypesAsync();
        model.CustomerList = await _lookupService.GetCustomersAsync();
        model.FleetBoatList = await _lookupService.GetFleetBoatsAsync(FleetID);
        model.FromLocationList = await _lookupService.GetLocationsAsync();
        model.ToLocationList = await _lookupService.GetLocationsAsync();
        model.CommodityList = await _lookupService.GetCommoditiesAsync();
        model.RiverList = await _lookupService.GetRiversAsync();
        model.LoadStatusList = await _lookupService.GetLoadStatusesAsync();
        model.DivisionList = await _lookupService.GetDivisionsAsync();
        model.VendorList = await _lookupService.GetVendorsAsync();

        if (model.IsFreightActive)
        {
            model.FreightCustomerList = await _lookupService.GetFreightCustomersAsync();
            model.FreightRateTypeList = await _lookupService.GetFreightRateTypesAsync();
        }

        // Additional dropdowns as needed
        model.RateTypeList = await _lookupService.GetRateTypesAsync();
        model.ChargeTypeList = await _lookupService.GetChargeTypesAsync();
    }

    private static void SplitDateTimeFields(BargeEventEditViewModel model)
    {
        // Split StartDateTime
        if (model.BargeEvent.StartDateTime != default)
        {
            model.StartDate = model.BargeEvent.StartDateTime.Date;
            model.StartTime = model.BargeEvent.StartDateTime.TimeOfDay;
        }

        // Split CompleteDateTime
        if (model.BargeEvent.CompleteDateTime.HasValue)
        {
            model.CompleteDate = model.BargeEvent.CompleteDateTime.Value.Date;
            model.CompleteTime = model.BargeEvent.CompleteDateTime.Value.TimeOfDay;
        }

        // Split other datetime fields as needed...
    }

    private static void CombineDateTimeFields(BargeEventEditViewModel model)
    {
        // Combine StartDateTime
        if (model.StartDate.HasValue)
        {
            model.BargeEvent.StartDateTime = model.StartDate.Value.Date;
            if (model.StartTime.HasValue)
            {
                model.BargeEvent.StartDateTime = model.BargeEvent.StartDateTime.Add(model.StartTime.Value);
            }
        }

        // Combine CompleteDateTime
        if (model.CompleteDate.HasValue)
        {
            model.BargeEvent.CompleteDateTime = model.CompleteDate.Value.Date;
            if (model.CompleteTime.HasValue)
            {
                model.BargeEvent.CompleteDateTime = model.BargeEvent.CompleteDateTime.Value.Add(model.CompleteTime.Value);
            }
        }
        else
        {
            model.BargeEvent.CompleteDateTime = null;
        }

        // Combine other datetime fields as needed...
    }
}
