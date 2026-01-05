using BargeOps.Shared.Dto;
using BargeOpsAdmin.Services;
using BargeOpsAdmin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;
using System.Text;

namespace BargeOpsAdmin.Controllers;

[Authorize(AuthenticationSchemes = IdentityConstants.ApplicationScheme)]
public class FacilityController : Controller
{
    private readonly IFacilityService _facilityService;
    private readonly ILookupService _lookupService;
    private readonly ILogger<FacilityController> _logger;

    public FacilityController(
        IFacilityService facilityService,
        ILookupService lookupService,
        ILogger<FacilityController> logger)
    {
        _facilityService = facilityService;
        _lookupService = lookupService;
        _logger = logger;
    }

    /// <summary>
    /// Search/List view (Index)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "FacilityRead")]
    public async Task<IActionResult> Index()
    {
        var viewModel = new FacilitySearchViewModel
        {
            Rivers = await GetRiverSelectListAsync(),
            FacilityTypes = await GetFacilityTypeSelectListAsync()
        };

        return View(viewModel);
    }

    /// <summary>
    /// DataTables AJAX endpoint for search results
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "FacilityRead")]
    public async Task<IActionResult> Search([FromBody] FacilitySearchRequest request)
    {
        try
        {
            var result = await _facilityService.SearchAsync(request);
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching facilities");
            return StatusCode(500, new { error = "An error occurred while searching facilities" });
        }
    }

    /// <summary>
    /// Create new facility (GET)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "FacilityModify")]
    public async Task<IActionResult> Create()
    {
        var viewModel = new FacilityEditViewModel
        {
            Facility = new FacilityDto { IsActive = true },
            Rivers = await GetRiverSelectListAsync(),
            Banks = await GetBankSelectListAsync(),
            FacilityTypes = await GetFacilityTypeSelectListAsync(),
            FacilityStatuses = await GetFacilityStatusSelectListAsync(),
            CanEdit = true
        };

        return View("Edit", viewModel);
    }

    /// <summary>
    /// Edit existing facility (GET)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "FacilityRead")]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var facility = await _facilityService.GetByIdAsync(id);

            if (facility == null)
            {
                TempData["Error"] = $"Facility with ID {id} not found";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new FacilityEditViewModel
            {
                Facility = facility,
                Rivers = await GetRiverSelectListAsync(),
                Banks = await GetBankSelectListAsync(),
                FacilityTypes = await GetFacilityTypeSelectListAsync(),
                FacilityStatuses = await GetFacilityStatusSelectListAsync(),
                CanEdit = User.HasClaim("Policy", "FacilityModify"),
                CanDelete = User.HasClaim("Policy", "FacilityModify")
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading facility {FacilityId}", id);
            TempData["Error"] = "An error occurred while loading the facility";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Save facility (POST)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "FacilityModify")]
    public async Task<IActionResult> Save(FacilityEditViewModel viewModel)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                // Reload lookup lists
                viewModel.Rivers = await GetRiverSelectListAsync();
                viewModel.Banks = await GetBankSelectListAsync();
                viewModel.FacilityTypes = await GetFacilityTypeSelectListAsync();
                viewModel.FacilityStatuses = await GetFacilityStatusSelectListAsync();
                return View("Edit", viewModel);
            }

            FacilityDto result;

            if (viewModel.Facility.LocationID > 0)
            {
                // Update existing
                result = await _facilityService.UpdateAsync(viewModel.Facility);
                TempData["Success"] = "Facility updated successfully";
            }
            else
            {
                // Create new
                result = await _facilityService.CreateAsync(viewModel.Facility);
                TempData["Success"] = "Facility created successfully";
            }

            return RedirectToAction(nameof(Edit), new { id = result.LocationID });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving facility");
            ModelState.AddModelError(string.Empty, "An error occurred while saving the facility");

            // Reload lookup lists
            viewModel.Rivers = await GetRiverSelectListAsync();
            viewModel.Banks = await GetBankSelectListAsync();
            viewModel.FacilityTypes = await GetFacilityTypeSelectListAsync();
            viewModel.FacilityStatuses = await GetFacilityStatusSelectListAsync();

            return View("Edit", viewModel);
        }
    }

    /// <summary>
    /// Delete facility
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "FacilityModify")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _facilityService.DeleteAsync(id);

            if (deleted)
            {
                TempData["Success"] = "Facility deleted successfully";
            }
            else
            {
                TempData["Error"] = "Failed to delete facility";
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting facility {FacilityId}", id);
            TempData["Error"] = "An error occurred while deleting the facility";
            return RedirectToAction(nameof(Edit), new { id });
        }
    }

    #region Child Collection Operations

    /// <summary>
    /// Get berths for a facility (AJAX)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "FacilityRead")]
    public async Task<IActionResult> GetBerths(int facilityId)
    {
        try
        {
            var berths = await _facilityService.GetBerthsAsync(facilityId);
            return Json(berths);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting berths for facility {FacilityId}", facilityId);
            return StatusCode(500, new { error = "An error occurred while retrieving berths" });
        }
    }

    /// <summary>
    /// Get statuses for a facility (AJAX)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "FacilityRead")]
    public async Task<IActionResult> GetStatuses(int facilityId)
    {
        try
        {
            var statuses = await _facilityService.GetStatusesAsync(facilityId);
            return Json(statuses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statuses for facility {FacilityId}", facilityId);
            return StatusCode(500, new { error = "An error occurred while retrieving statuses" });
        }
    }

    /// <summary>
    /// Create or update berth (AJAX)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "FacilityModify")]
    public async Task<IActionResult> SaveBerth([FromBody] FacilityBerthDto berth)
    {
        try
        {
            FacilityBerthDto result;

            if (berth.FacilityBerthID > 0)
            {
                result = await _facilityService.UpdateBerthAsync(berth);
            }
            else
            {
                result = await _facilityService.CreateBerthAsync(berth);
            }

            return Json(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving berth");
            return Json(new { success = false, error = "An error occurred while saving the berth" });
        }
    }

    /// <summary>
    /// Delete berth (AJAX)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "FacilityModify")]
    public async Task<IActionResult> DeleteBerth(int id)
    {
        try
        {
            var deleted = await _facilityService.DeleteBerthAsync(id);
            return Json(new { success = deleted });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting berth {BerthId}", id);
            return Json(new { success = false, error = "An error occurred while deleting the berth" });
        }
    }

    /// <summary>
    /// Create or update status (AJAX)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "FacilityModify")]
    public async Task<IActionResult> SaveStatus([FromBody] FacilityStatusDto status)
    {
        try
        {
            FacilityStatusDto result;

            if (status.FacilityStatusID > 0)
            {
                result = await _facilityService.UpdateStatusAsync(status);
            }
            else
            {
                result = await _facilityService.CreateStatusAsync(status);
            }

            return Json(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving status");
            return Json(new { success = false, error = "An error occurred while saving the status" });
        }
    }

    /// <summary>
    /// Delete status (AJAX)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "FacilityModify")]
    public async Task<IActionResult> DeleteStatus(int id)
    {
        try
        {
            var deleted = await _facilityService.DeleteStatusAsync(id);
            return Json(new { success = deleted });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting status {StatusId}", id);
            return Json(new { success = false, error = "An error occurred while deleting the status" });
        }
    }

    #endregion

    #region Helper Methods

    private async Task<IEnumerable<SelectListItem>> GetRiverSelectListAsync()
    {
        var rivers = await _lookupService.GetRiversAsync();
        return rivers.Select(r => new SelectListItem
        {
            Value = r.Code,
            Text = r.Name
        }).Prepend(new SelectListItem { Value = "", Text = "-- Select River --" });
    }

    private async Task<IEnumerable<SelectListItem>> GetBankSelectListAsync()
    {
        var banks = await _lookupService.GetValidationListAsync("RiverBank");
        return banks.Select(b => new SelectListItem
        {
            Value = b.Value,
            Text = b.Text
        }).Prepend(new SelectListItem { Value = "", Text = "-- Select Bank --" });
    }

    private async Task<IEnumerable<SelectListItem>> GetFacilityTypeSelectListAsync()
    {
        var types = await _lookupService.GetValidationListAsync("BargeExLocationType");
        return types.Select(t => new SelectListItem
        {
            Value = t.Value,
            Text = t.Text
        }).Prepend(new SelectListItem { Value = "", Text = "-- Select Type --" });
    }

    private async Task<IEnumerable<SelectListItem>> GetFacilityStatusSelectListAsync()
    {
        var statuses = await _lookupService.GetValidationListAsync("FacilityStatus");
        return statuses.Select(s => new SelectListItem
        {
            Value = s.Value,
            Text = s.Text
        }).Prepend(new SelectListItem { Value = "", Text = "-- Select Status --" });
    }

    #endregion

    /// <summary>
    /// Export facility statuses as CSV (used by the Status tab Export button)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "FacilityRead")]
    public async Task<IActionResult> ExportStatus(int facilityId)
    {
        var statuses = await _facilityService.GetStatusesAsync(facilityId);

        var sb = new StringBuilder();
        sb.AppendLine("FacilityStatusID,LocationID,StartDateTime,EndDateTime,Status,Note");

        foreach (var s in statuses)
        {
            sb.Append(s.FacilityStatusID).Append(',');
            sb.Append(s.LocationID).Append(',');
            sb.Append(FormatCsvDateTime(s.StartDateTime)).Append(',');
            sb.Append(FormatCsvDateTime(s.EndDateTime)).Append(',');
            sb.Append(EscapeCsv(s.Status)).Append(',');
            sb.AppendLine(EscapeCsv(s.Note));
        }

        var fileName = $"facility-{facilityId}-statuses-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());

        return File(bytes, "text/csv", fileName);
    }

    private static string FormatCsvDateTime(DateTime? value)
    {
        return value.HasValue
            ? value.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            : string.Empty;
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
