using BargeOps.Shared.Dto;
using BargeOpsAdmin.Services;
using BargeOpsAdmin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BargeOpsAdmin.Controllers;

/// <summary>
/// MVC Controller for BargeSeries search and edit operations.
/// Uses ViewModels that contain DTOs from BargeOps.Shared.
/// </summary>
[Authorize(Policy = "BargeSeriesView")]
public class BargeSeriesSearchController : Controller
{
    private readonly IBargeSeriesService _bargeSeriesService;
    private readonly ICustomerService _customerService;
    private readonly IValidationListService _validationListService;
    private readonly ILogger<BargeSeriesSearchController> _logger;

    public BargeSeriesSearchController(
        IBargeSeriesService bargeSeriesService,
        ICustomerService customerService,
        IValidationListService validationListService,
        ILogger<BargeSeriesSearchController> logger)
    {
        _bargeSeriesService = bargeSeriesService ?? throw new ArgumentNullException(nameof(bargeSeriesService));
        _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
        _validationListService = validationListService ?? throw new ArgumentNullException(nameof(validationListService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Display the search/list page.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var viewModel = new BargeSeriesSearchViewModel
        {
            Customers = await GetCustomerSelectListAsync(),
            HullTypes = await GetHullTypeSelectListAsync(),
            CoverTypes = await GetCoverTypeSelectListAsync(),
            CanCreate = User.HasClaim("Permission", "BargeSeriesCreate"),
            CanModify = User.HasClaim("Permission", "BargeSeriesModify"),
            CanDelete = User.HasClaim("Permission", "BargeSeriesDelete")
        };

        return View(viewModel);
    }

    /// <summary>
    /// DataTables server-side processing endpoint.
    /// Called via AJAX from barge-series-search.js.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> BargeSeriesTable([FromBody] DataTablesRequest request)
    {
        try
        {
            var searchRequest = new BargeSeriesSearchRequest
            {
                Name = request.SearchValue,
                CustomerID = request.CustomerID,
                HullType = request.HullType,
                CoverType = request.CoverType,
                ActiveOnly = request.ActiveOnly,
                Start = request.Start,
                Length = request.Length,
                SortColumn = request.SortColumn,
                SortDirection = request.SortDirection
            };

            var result = await _bargeSeriesService.SearchAsync(searchRequest);

            return Json(new
            {
                draw = request.Draw,
                recordsTotal = result.RecordsTotal,
                recordsFiltered = result.RecordsFiltered,
                data = result.Data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading barge series table");
            return StatusCode(500, new { error = "Failed to load data." });
        }
    }

    /// <summary>
    /// Display the create form.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "BargeSeriesCreate")]
    public async Task<IActionResult> Create()
    {
        var viewModel = new BargeSeriesEditViewModel
        {
            IsActive = true,
            Customers = await GetCustomerSelectListAsync(),
            HullTypes = await GetHullTypeSelectListAsync(),
            CoverTypes = await GetCoverTypeSelectListAsync(),
            CanModify = true,
            IsReadOnly = false,
            Drafts = CreateEmptyDraftList() // Initialize 14 rows
        };

        return View("Edit", viewModel);
    }

    /// <summary>
    /// Display the edit form.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var dto = await _bargeSeriesService.GetByIdAsync(id);

            if (dto == null)
            {
                TempData["Error"] = $"Barge series with ID {id} not found.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = BargeSeriesEditViewModel.FromDto(dto);
            viewModel.Customers = await GetCustomerSelectListAsync();
            viewModel.HullTypes = await GetHullTypeSelectListAsync();
            viewModel.CoverTypes = await GetCoverTypeSelectListAsync();
            viewModel.CanModify = User.HasClaim("Permission", "BargeSeriesModify");
            viewModel.IsReadOnly = !viewModel.CanModify;

            // Ensure we have 14 draft rows (0-13 feet)
            EnsureDraftRows(viewModel.Drafts);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading barge series {BargeSeriesId}", id);
            TempData["Error"] = "Failed to load barge series.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Handle form submission (create or update).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "BargeSeriesModify")]
    public async Task<IActionResult> Edit(BargeSeriesEditViewModel viewModel)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                // Reload dropdowns
                viewModel.Customers = await GetCustomerSelectListAsync();
                viewModel.HullTypes = await GetHullTypeSelectListAsync();
                viewModel.CoverTypes = await GetCoverTypeSelectListAsync();
                return View(viewModel);
            }

            // Convert ViewModel to DTO
            var dto = viewModel.ToDto();

            if (viewModel.BargeSeriesID == 0)
            {
                // Create new
                var created = await _bargeSeriesService.CreateAsync(dto);
                TempData["Success"] = $"Barge series '{created.Name}' created successfully.";
            }
            else
            {
                // Update existing
                var updated = await _bargeSeriesService.UpdateAsync(dto);
                TempData["Success"] = $"Barge series '{updated.Name}' updated successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving barge series");
            ModelState.AddModelError("", "Failed to save barge series. Please check your input and try again.");

            // Reload dropdowns
            viewModel.Customers = await GetCustomerSelectListAsync();
            viewModel.HullTypes = await GetHullTypeSelectListAsync();
            viewModel.CoverTypes = await GetCoverTypeSelectListAsync();

            return View(viewModel);
        }
    }

    /// <summary>
    /// Delete (deactivate) a barge series.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "BargeSeriesDelete")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var success = await _bargeSeriesService.DeleteAsync(id);

            if (success)
            {
                TempData["Success"] = "Barge series deactivated successfully.";
            }
            else
            {
                TempData["Error"] = "Barge series not found.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting barge series {BargeSeriesId}", id);
            TempData["Error"] = "Failed to deactivate barge series.";
        }

        return RedirectToAction(nameof(Index));
    }

    #region Private Helper Methods

    private async Task<IEnumerable<SelectListItem>> GetCustomerSelectListAsync()
    {
        try
        {
            var customers = await _customerService.GetListAsync();
            return customers.Select(c => new SelectListItem
            {
                Value = c.CustomerID.ToString(),
                Text = c.CustomerName
            }).Prepend(new SelectListItem { Value = "", Text = "-- Select Owner --" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customer list");
            return Enumerable.Empty<SelectListItem>();
        }
    }

    private async Task<IEnumerable<SelectListItem>> GetHullTypeSelectListAsync()
    {
        try
        {
            var hullTypes = await _validationListService.GetValidationListAsync("HullType");
            return hullTypes.Select(ht => new SelectListItem
            {
                Value = ht.Code,
                Text = ht.Description
            }).Prepend(new SelectListItem { Value = "", Text = "-- Select Hull Type --" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading hull type list");
            return Enumerable.Empty<SelectListItem>();
        }
    }

    private async Task<IEnumerable<SelectListItem>> GetCoverTypeSelectListAsync()
    {
        try
        {
            var coverTypes = await _validationListService.GetValidationListAsync("CoverType");
            return coverTypes.Select(ct => new SelectListItem
            {
                Value = ct.Code,
                Text = ct.Description
            }).Prepend(new SelectListItem { Value = "", Text = "-- Select Cover Type --" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading cover type list");
            return Enumerable.Empty<SelectListItem>();
        }
    }

    private static List<BargeSeriesDraftDto> CreateEmptyDraftList()
    {
        var drafts = new List<BargeSeriesDraftDto>();
        for (int feet = 0; feet <= 13; feet++)
        {
            drafts.Add(new BargeSeriesDraftDto
            {
                DraftFeet = feet
            });
        }
        return drafts;
    }

    private static void EnsureDraftRows(List<BargeSeriesDraftDto> drafts)
    {
        // Ensure we have exactly 14 rows (feet 0-13)
        for (int feet = 0; feet <= 13; feet++)
        {
            if (!drafts.Any(d => d.DraftFeet == feet))
            {
                drafts.Add(new BargeSeriesDraftDto { DraftFeet = feet });
            }
        }

        // Sort by DraftFeet
        drafts.Sort((a, b) => (a.DraftFeet ?? 0).CompareTo(b.DraftFeet ?? 0));
    }

    #endregion
}

/// <summary>
/// DataTables request model for server-side processing.
/// </summary>
public class DataTablesRequest
{
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public string? SearchValue { get; set; }
    public int? CustomerID { get; set; }
    public string? HullType { get; set; }
    public string? CoverType { get; set; }
    public bool ActiveOnly { get; set; } = true;
    public string? SortColumn { get; set; }
    public string? SortDirection { get; set; }
}
