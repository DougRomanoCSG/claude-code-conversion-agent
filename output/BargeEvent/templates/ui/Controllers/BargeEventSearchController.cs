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
/// UI Controller for BargeEvent search and list operations.
/// Uses IdentityConstants.ApplicationScheme for authentication.
/// </summary>
[Authorize(AuthenticationSchemes = IdentityConstants.ApplicationScheme)]
[Route("[controller]")]
public class BargeEventSearchController : AppController
{
    private readonly IBargeEventService _bargeEventService;
    private readonly ILookupService _lookupService;
    private readonly ILogger<BargeEventSearchController> _logger;
    private readonly AppSession _appSession;

    public BargeEventSearchController(
        IBargeEventService bargeEventService,
        ILookupService lookupService,
        ILogger<BargeEventSearchController> logger,
        AppSession appSession) : base(appSession)
    {
        _bargeEventService = bargeEventService ?? throw new ArgumentNullException(nameof(bargeEventService));
        _lookupService = lookupService ?? throw new ArgumentNullException(nameof(lookupService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appSession = appSession ?? throw new ArgumentNullException(nameof(appSession));
    }

    /// <summary>
    /// Display the BargeEvent search page
    /// </summary>
    [HttpGet("Index")]
    [RequirePermission<AuthPermissions>(AuthPermissions.BargeEventView, PermissionAccessType.ReadOnly)]
    public async Task<IActionResult> Index()
    {
        try
        {
            await InitSessionVariables(_appSession);

            var model = new BargeEventSearchViewModel
            {
                // Default search criteria
                IncludeVoided = false,

                // Load dropdown lists
                EventTypeList = await _lookupService.GetEventTypesAsync(),
                CustomerList = await _lookupService.GetCustomersAsync(),
                FromLocationList = await _lookupService.GetLocationsAsync(),
                ToLocationList = await _lookupService.GetLocationsAsync(),
                FleetBoatList = await _lookupService.GetFleetBoatsAsync(FleetID),

                // License flags (control feature visibility)
                IsFreightActive = await _lookupService.IsLicenseActiveAsync("Freight"),
                IsOnboardActive = await _lookupService.IsLicenseActiveAsync("Onboard"),

                // Permission flags
                CanView = true, // User has read-only permission (required to reach this page)
                CanModify = User.HasPermission(AuthPermissions.BargeEventModify),
                CanViewBilling = User.HasPermission(AuthPermissions.BargeEventBillingView),
                CanModifyBilling = User.HasPermission(AuthPermissions.BargeEventBillingModify)
            };

            // Freight-specific dropdowns (only if license active)
            if (model.IsFreightActive)
            {
                model.FreightCustomerList = await _lookupService.GetFreightCustomersAsync();
            }

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading BargeEvent search page");
            return View("Error");
        }
    }

    /// <summary>
    /// DataTables server-side processing endpoint for BargeEvent search grid
    /// </summary>
    [HttpPost("EventTable")]
    [RequirePermission<AuthPermissions>(AuthPermissions.BargeEventView, PermissionAccessType.ReadOnly)]
    public async Task<IActionResult> EventTable(
        [FromBody] DataTableRequest dtRequest,
        [FromForm] BargeEventSearchViewModel searchModel)
    {
        try
        {
            // Build search request from ViewModel and DataTable request
            var request = new BargeEventSearchRequest
            {
                FleetID = FleetID, // From AppController base

                // Search criteria from ViewModel
                EventTypeId = searchModel.EventTypeId,
                BillingCustomerId = searchModel.BillingCustomerId,
                FromLocationId = searchModel.FromLocationId,
                ToLocationId = searchModel.ToLocationId,
                FleetBoatId = searchModel.FleetBoatId,
                StartDate = searchModel.StartDate,
                EndDate = searchModel.EndDate,
                BargeNumberList = searchModel.BargeNumberList,
                TicketCustomerId = searchModel.TicketCustomerId,
                FreightCustomerId = searchModel.FreightCustomerId,
                ContractNumber = searchModel.ContractNumber,
                EventRateId = searchModel.EventRateId,
                IncludeVoided = searchModel.IncludeVoided,

                // DataTables pagination and sorting
                Start = dtRequest.Start,
                Length = dtRequest.Length,
                SortBy = dtRequest.GetSortColumn(),
                SortDescending = dtRequest.IsSortDescending()
            };

            var result = await _bargeEventService.SearchAsync(request);

            // Return DataTables-compatible JSON
            return Json(new DataTableResponse<BargeEventSearchDto>(
                dtRequest.Draw,
                result.TotalCount,
                result.FilteredCount,
                result.Items
            ));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("search criterion"))
        {
            _logger.LogWarning("No search criteria provided for BargeEvent search");

            return Json(new
            {
                draw = dtRequest.Draw,
                recordsTotal = 0,
                recordsFiltered = 0,
                data = Array.Empty<object>(),
                error = "At least one search criterion is required."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting BargeEvent search results");

            return Json(new
            {
                draw = dtRequest.Draw,
                recordsTotal = 0,
                recordsFiltered = 0,
                data = Array.Empty<object>(),
                error = "An error occurred while searching barge events."
            });
        }
    }

    /// <summary>
    /// Mark multiple events for rebilling (context menu operation)
    /// </summary>
    [HttpPost("MarkRebill")]
    [RequirePermission<AuthPermissions>(AuthPermissions.BargeEventBillingModify, PermissionAccessType.Modify)]
    public async Task<IActionResult> MarkRebill([FromBody] IEnumerable<int> ticketEventIds)
    {
        try
        {
            if (!ticketEventIds.Any())
            {
                return BadRequest(new { success = false, message = "No events selected" });
            }

            var markedCount = await _bargeEventService.MarkForRebillAsync(ticketEventIds);

            return Json(new
            {
                success = true,
                markedCount,
                message = $"Marked {markedCount} event(s) for rebill"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking events for rebill");
            return Json(new { success = false, message = "Error marking events for rebill" });
        }
    }

    /// <summary>
    /// Unmark multiple events from rebilling (context menu operation)
    /// </summary>
    [HttpPost("UnmarkRebill")]
    [RequirePermission<AuthPermissions>(AuthPermissions.BargeEventBillingModify, PermissionAccessType.Modify)]
    public async Task<IActionResult> UnmarkRebill([FromBody] IEnumerable<int> ticketEventIds)
    {
        try
        {
            if (!ticketEventIds.Any())
            {
                return BadRequest(new { success = false, message = "No events selected" });
            }

            var unmarkedCount = await _bargeEventService.UnmarkForRebillAsync(ticketEventIds);

            return Json(new
            {
                success = true,
                unmarkedCount,
                message = $"Unmarked {unmarkedCount} event(s) from rebill"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unmarking events from rebill");
            return Json(new { success = false, message = "Error unmarking events from rebill" });
        }
    }

    /// <summary>
    /// Export search results to Excel
    /// </summary>
    [HttpPost("Export")]
    [RequirePermission<AuthPermissions>(AuthPermissions.BargeEventView, PermissionAccessType.ReadOnly)]
    public async Task<IActionResult> Export([FromForm] BargeEventSearchViewModel searchModel)
    {
        try
        {
            // Build search request (get all results, not paged)
            var request = new BargeEventSearchRequest
            {
                FleetID = FleetID,
                EventTypeId = searchModel.EventTypeId,
                BillingCustomerId = searchModel.BillingCustomerId,
                FromLocationId = searchModel.FromLocationId,
                ToLocationId = searchModel.ToLocationId,
                FleetBoatId = searchModel.FleetBoatId,
                StartDate = searchModel.StartDate,
                EndDate = searchModel.EndDate,
                BargeNumberList = searchModel.BargeNumberList,
                TicketCustomerId = searchModel.TicketCustomerId,
                FreightCustomerId = searchModel.FreightCustomerId,
                ContractNumber = searchModel.ContractNumber,
                EventRateId = searchModel.EventRateId,
                IncludeVoided = searchModel.IncludeVoided,

                // Get all results for export
                Start = 0,
                Length = int.MaxValue,
                SortBy = "StartDateTime",
                SortDescending = true
            };

            var result = await _bargeEventService.SearchAsync(request);

            // TODO: Implement Excel generation using EPPlus or similar
            // For now, return CSV

            var csv = GenerateCsv(result.Items);

            return File(
                System.Text.Encoding.UTF8.GetBytes(csv),
                "text/csv",
                $"BargeEvents_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting BargeEvent search results");
            return BadRequest("Error exporting search results");
        }
    }

    // ===== HELPER METHODS =====

    private static string GenerateCsv(IEnumerable<BargeEventSearchDto> items)
    {
        var sb = new System.Text.StringBuilder();

        // Header
        sb.AppendLine("Event,Barge,Ticket,Start Time,Complete Time,From,To,Load Status," +
                     "Commodity,Quantity,Customer,Servicing Boat,Division,Invoiced,Rebill,Void");

        // Data rows
        foreach (var item in items)
        {
            sb.AppendLine($"{EscapeCsv(item.EventName)}," +
                         $"{EscapeCsv(item.BargeNum)}," +
                         $"{item.TicketID}," +
                         $"{item.StartDateTime:MM/dd/yyyy HH:mm}," +
                         $"{item.CompleteDateTime?.ToString("MM/dd/yyyy HH:mm")}," +
                         $"{EscapeCsv(item.StartLocation)}," +
                         $"{EscapeCsv(item.EndLocation)}," +
                         $"{EscapeCsv(item.LoadStatus)}," +
                         $"{EscapeCsv(item.CommodityName)}," +
                         $"{item.LoadUnloadTons}," +
                         $"{EscapeCsv(item.CustomerName)}," +
                         $"{EscapeCsv(item.ServicingBoat)}," +
                         $"{EscapeCsv(item.Division)}," +
                         $"{item.IsInvoiced}," +
                         $"{item.Rebill}," +
                         $"{item.Void}");
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
