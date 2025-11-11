using Admin.Domain.Dto;
using Admin.Domain.Services;
using BargeOps.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admin.Api.Controllers;

/// <summary>
/// API Controller for Crewing location search and management
/// Generated from: frmCrewingSearch.vb (legacy form)
/// Target: BargeOps.Admin.API
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CrewingController : ControllerBase
{
	private readonly ICrewingService _crewingService;
	private readonly ILogger<CrewingController> _logger;

	public CrewingController(ICrewingService crewingService, ILogger<CrewingController> logger)
	{
		_crewingService = crewingService;
		_logger = logger;
	}

	/// <summary>
	/// Search crewing locations with pagination
	/// Maps to: CrewingLocationSearch stored procedure
	/// </summary>
	[HttpPost("search")]
	[ProducesResponseType(typeof(CrewingSearchResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> Search([FromBody] CrewingSearchRequest request)
	{
		try
		{
			var result = await _crewingService.SearchCrewingLocationsAsync(request);
			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error searching crewing locations");
			return BadRequest(new { message = "An error occurred while searching" });
		}
	}

	/// <summary>
	/// Get crewing location by ID
	/// Maps to: Location.GetLocation(LocationID)
	/// </summary>
	[HttpGet("{id}")]
	[ProducesResponseType(typeof(CrewingDetailDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Get(int id)
	{
		try
		{
			var result = await _crewingService.GetCrewingLocationAsync(id);
			if (result == null)
			{
				return NotFound(new { message = $"Crewing location {id} not found" });
			}
			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving crewing location {LocationId}", id);
			return BadRequest(new { message = "An error occurred while retrieving the location" });
		}
	}

	/// <summary>
	/// Create new crewing location
	/// Maps to: Location.NewLocation(Crewing)
	/// </summary>
	[HttpPost]
	[ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> Create([FromBody] CrewingCreateDto dto)
	{
		try
		{
			var result = await _crewingService.CreateCrewingLocationAsync(dto);
			if (!result.IsSuccess)
			{
				return BadRequest(new { message = result.ErrorMessage });
			}
			return CreatedAtAction(nameof(Get), new { id = result.Value }, result.Value);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating crewing location");
			return BadRequest(new { message = "An error occurred while creating the location" });
		}
	}

	/// <summary>
	/// Update existing crewing location
	/// Maps to: Location.ApplyEdit()
	/// </summary>
	[HttpPut("{id}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Update(int id, [FromBody] CrewingUpdateDto dto)
	{
		try
		{
			var result = await _crewingService.UpdateCrewingLocationAsync(id, dto);
			if (!result.IsSuccess)
			{
				return result.ErrorMessage?.Contains("not found") == true
					? NotFound(new { message = result.ErrorMessage })
					: BadRequest(new { message = result.ErrorMessage });
			}
			return NoContent();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating crewing location {LocationId}", id);
			return BadRequest(new { message = "An error occurred while updating the location" });
		}
	}

	/// <summary>
	/// Delete crewing location
	/// Maps to: Location.DeleteLocation(LocationID)
	/// </summary>
	[HttpDelete("{id}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Delete(int id)
	{
		try
		{
			var result = await _crewingService.DeleteCrewingLocationAsync(id);
			if (!result.IsSuccess)
			{
				return result.ErrorMessage?.Contains("not found") == true
					? NotFound(new { message = result.ErrorMessage })
					: BadRequest(new { message = result.ErrorMessage });
			}
			return NoContent();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error deleting crewing location {LocationId}", id);
			return BadRequest(new { message = "An error occurred while deleting the location" });
		}
	}

	// Lookup Endpoints

	/// <summary>
	/// Get all rivers for dropdown
	/// Maps to: PopulateRiverCombo()
	/// </summary>
	[HttpGet("rivers")]
	[ProducesResponseType(typeof(IEnumerable<SelectListItem>), StatusCodes.Status200OK)]
	public async Task<IActionResult> GetRivers()
	{
		try
		{
			var result = await _crewingService.GetRiversAsync();
			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving rivers");
			return BadRequest(new { message = "An error occurred while retrieving rivers" });
		}
	}

	/// <summary>
	/// Get all location types for dropdown
	/// Maps to: PopulateValidationListCombo(BargeExLocationType)
	/// </summary>
	[HttpGet("location-types")]
	[ProducesResponseType(typeof(IEnumerable<SelectListItem>), StatusCodes.Status200OK)]
	public async Task<IActionResult> GetLocationTypes()
	{
		try
		{
			var result = await _crewingService.GetLocationTypesAsync();
			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving location types");
			return BadRequest(new { message = "An error occurred while retrieving location types" });
		}
	}
}
