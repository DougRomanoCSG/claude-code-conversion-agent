using Admin.UI.Models;
using Admin.UI.Services;
using BargeOps.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admin.UI.Controllers;

/// <summary>
/// MVC Controller for Crewing Search UI
/// Generated from: frmCrewingSearch.vb (legacy form)
/// Target: BargeOps.Admin.UI
/// Reference: BargeOps.Crewing.UI for UI patterns
/// </summary>
[Authorize]
[Route("[controller]")]
public class CrewingSearchController : AppController
{
	private readonly ICrewingService _crewingService;
	private readonly ILogger<CrewingSearchController> _logger;

	public CrewingSearchController(ICrewingService crewingService, ILogger<CrewingSearchController> logger)
	{
		_crewingService = crewingService;
		_logger = logger;
	}

	/// <summary>
	/// Display search page
	/// Maps to: frmCrewingSearch initial load
	/// </summary>
	[HttpGet]
	[HttpGet("Index")]
	[RequirePermission<AuthPermissions>(AuthPermissions.CrewingReadOnly, PermissionAccessType.ReadOnly)]
	public async Task<IActionResult> Index()
	{
		var model = new CrewingSearchViewModel
		{
			ActiveOnly = true,
			Rivers = await _crewingService.GetRiversAsync(),
			LocationTypes = await _crewingService.GetLocationTypesAsync()
		};

		return View(model);
	}

	/// <summary>
	/// DataTables AJAX endpoint for search results
	/// Maps to: grdCrewingLocations grid
	/// </summary>
	[HttpPost("CrewingTable")]
	[RequirePermission<AuthPermissions>(AuthPermissions.CrewingReadOnly, PermissionAccessType.ReadOnly)]
	public async Task<IActionResult> CrewingTable(DataTableRequest request, CrewingSearchViewModel model)
	{
		try
		{
			var result = await _crewingService.GetCrewingLocationsAsync(request, model);
			return Json(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving crewing locations");
			return Json(new { error = "An error occurred while retrieving data" });
		}
	}

	/// <summary>
	/// Display create form
	/// Maps to: ItemAdd() in legacy form
	/// </summary>
	[HttpGet("Create")]
	[RequirePermission<AuthPermissions>(AuthPermissions.CrewingModify, PermissionAccessType.Modify)]
	public async Task<IActionResult> Create()
	{
		var model = new CrewingEditViewModel
		{
			IsActive = true,
			Rivers = await _crewingService.GetRiversAsync(),
			LocationTypes = await _crewingService.GetLocationTypesAsync(),
			Banks = await _crewingService.GetBanksAsync()
		};

		return View("Edit", model);
	}

	/// <summary>
	/// Display edit form
	/// Maps to: ItemModify() in legacy form
	/// </summary>
	[HttpGet("Edit/{id}")]
	[RequirePermission<AuthPermissions>(AuthPermissions.CrewingModify, PermissionAccessType.Modify)]
	public async Task<IActionResult> Edit(int id)
	{
		try
		{
			var model = await _crewingService.GetCrewingLocationAsync(id);
			if (model == null)
			{
				TempData["ErrorMessage"] = $"Crewing location {id} not found";
				return RedirectToAction(nameof(Index));
			}

			model.Rivers = await _crewingService.GetRiversAsync();
			model.LocationTypes = await _crewingService.GetLocationTypesAsync();
			model.Banks = await _crewingService.GetBanksAsync();

			return View(model);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error loading crewing location {Id}", id);
			TempData["ErrorMessage"] = "An error occurred while loading the location";
			return RedirectToAction(nameof(Index));
		}
	}

	/// <summary>
	/// Save crewing location (create or update)
	/// Maps to: btnSubmit_Click in legacy form
	/// </summary>
	[HttpPost("Save")]
	[RequirePermission<AuthPermissions>(AuthPermissions.CrewingModify, PermissionAccessType.Modify)]
	public async Task<IActionResult> Save(CrewingEditViewModel model)
	{
		if (!ModelState.IsValid)
		{
			model.Rivers = await _crewingService.GetRiversAsync();
			model.LocationTypes = await _crewingService.GetLocationTypesAsync();
			model.Banks = await _crewingService.GetBanksAsync();
			return View("Edit", model);
		}

		try
		{
			var result = model.LocationId == 0
				? await _crewingService.CreateCrewingLocationAsync(model)
				: await _crewingService.UpdateCrewingLocationAsync(model.LocationId, model);

			if (!result.IsSuccess)
			{
				ModelState.AddModelError("", result.ErrorMessage ?? "An error occurred while saving");
				model.Rivers = await _crewingService.GetRiversAsync();
				model.LocationTypes = await _crewingService.GetLocationTypesAsync();
				model.Banks = await _crewingService.GetBanksAsync();
				return View("Edit", model);
			}

			TempData["SuccessMessage"] = model.LocationId == 0
				? "Crewing location created successfully"
				: "Crewing location updated successfully";

			return RedirectToAction(nameof(Index));
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error saving crewing location");
			ModelState.AddModelError("", "An error occurred while saving");
			model.Rivers = await _crewingService.GetRiversAsync();
			model.LocationTypes = await _crewingService.GetLocationTypesAsync();
			model.Banks = await _crewingService.GetBanksAsync();
			return View("Edit", model);
		}
	}

	/// <summary>
	/// Delete crewing location
	/// Maps to: ItemRemove() in legacy form
	/// </summary>
	[HttpPost("Delete/{id}")]
	[RequirePermission<AuthPermissions>(AuthPermissions.CrewingModify, PermissionAccessType.Modify)]
	public async Task<IActionResult> Delete(int id)
	{
		try
		{
			var result = await _crewingService.DeleteCrewingLocationAsync(id);
			if (!result.IsSuccess)
			{
				TempData["ErrorMessage"] = result.ErrorMessage ?? "An error occurred while deleting";
			}
			else
			{
				TempData["SuccessMessage"] = "Crewing location deleted successfully";
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error deleting crewing location {Id}", id);
			TempData["ErrorMessage"] = "An error occurred while deleting";
		}

		return RedirectToAction(nameof(Index));
	}

	/// <summary>
	/// Display read-only details
	/// Maps to: frmCrewingDetail in read-only mode
	/// </summary>
	[HttpGet("Details/{id}")]
	[RequirePermission<AuthPermissions>(AuthPermissions.CrewingReadOnly, PermissionAccessType.ReadOnly)]
	public async Task<IActionResult> Details(int id)
	{
		try
		{
			var model = await _crewingService.GetCrewingLocationAsync(id);
			if (model == null)
			{
				TempData["ErrorMessage"] = $"Crewing location {id} not found";
				return RedirectToAction(nameof(Index));
			}

			return View(model);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error loading crewing location {Id}", id);
			TempData["ErrorMessage"] = "An error occurred while loading the location";
			return RedirectToAction(nameof(Index));
		}
	}
}
