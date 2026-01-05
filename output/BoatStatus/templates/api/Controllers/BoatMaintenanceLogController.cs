using Microsoft.AspNetCore.Mvc;
using BargeOps.Shared.Dto;
using Admin.Domain.Services;
using Admin.Api.Filters;

namespace Admin.Api.Controllers;

/// <summary>
/// API Controller for BoatMaintenanceLog operations
/// ⭐ Authentication: API Key (configured in appsettings.json)
/// ⭐ Returns DTOs directly (no mapping needed in MONO SHARED architecture)
/// </summary>
[ApiController]
[Route("api/boat-maintenance-log")]
[ApiKey]
public class BoatMaintenanceLogController : ControllerBase
{
    private readonly IBoatMaintenanceLogService _service;
    private readonly ILogger<BoatMaintenanceLogController> _logger;

    public BoatMaintenanceLogController(
        IBoatMaintenanceLogService service,
        ILogger<BoatMaintenanceLogController> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all BoatMaintenanceLog records for a specific boat
    /// </summary>
    /// <param name="boatId">LocationID of the boat</param>
    /// <returns>List of maintenance log entries</returns>
    [HttpGet("boat/{boatId}")]
    [ProducesResponseType(typeof(IEnumerable<BoatMaintenanceLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<BoatMaintenanceLogDto>>> GetByBoatId(int boatId)
    {
        try
        {
            var logs = await _service.GetByLocationIdAsync(boatId);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting maintenance logs for boat {BoatId}", boatId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving maintenance logs");
        }
    }

    /// <summary>
    /// Get a specific BoatMaintenanceLog by ID
    /// </summary>
    /// <param name="id">BoatMaintenanceLogID</param>
    /// <returns>Maintenance log entry</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BoatMaintenanceLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BoatMaintenanceLogDto>> GetById(int id)
    {
        try
        {
            var log = await _service.GetByIdAsync(id);
            if (log == null)
            {
                return NotFound($"BoatMaintenanceLog {id} not found");
            }

            return Ok(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting maintenance log {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the maintenance log");
        }
    }

    /// <summary>
    /// Create a new BoatMaintenanceLog record
    /// ⭐ Validates conditional business rules
    /// ⭐ Clears unused fields based on MaintenanceType
    /// </summary>
    /// <param name="log">BoatMaintenanceLog DTO</param>
    /// <returns>Created maintenance log with new ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<int>> Create([FromBody] BoatMaintenanceLogDto log)
    {
        try
        {
            // Get current user (from API key or authentication context)
            var currentUser = User.Identity?.Name ?? "API";

            var newId = await _service.CreateAsync(log, currentUser);

            return CreatedAtAction(
                nameof(GetById),
                new { id = newId },
                newId
            );
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed creating maintenance log");
            return BadRequest(new { errors = ex.Errors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating maintenance log");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the maintenance log");
        }
    }

    /// <summary>
    /// Update an existing BoatMaintenanceLog record
    /// ⭐ Validates that MaintenanceType cannot be changed
    /// ⭐ Validates conditional business rules
    /// ⭐ Includes optimistic concurrency check
    /// </summary>
    /// <param name="id">BoatMaintenanceLogID</param>
    /// <param name="log">Updated BoatMaintenanceLog DTO</param>
    /// <returns>No content on success</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] BoatMaintenanceLogDto log)
    {
        try
        {
            if (id != log.BoatMaintenanceLogID)
            {
                return BadRequest("ID mismatch");
            }

            // Get current user
            var currentUser = User.Identity?.Name ?? "API";

            await _service.UpdateAsync(log, currentUser);

            return NoContent();
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Maintenance log {Id} not found", id);
            return NotFound(ex.Message);
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning(ex, "Business rule violation updating maintenance log {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed updating maintenance log {Id}", id);
            return BadRequest(new { errors = ex.Errors });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating maintenance log {Id}", id);
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating maintenance log {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the maintenance log");
        }
    }

    /// <summary>
    /// Delete a BoatMaintenanceLog record
    /// ⭐ Hard delete (not soft delete) with UnitTowTripDownTime cleanup
    /// </summary>
    /// <param name="id">BoatMaintenanceLogID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Maintenance log {Id} not found", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting maintenance log {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the maintenance log");
        }
    }

    /// <summary>
    /// Search BoatMaintenanceLog records with criteria
    /// </summary>
    /// <param name="request">Search criteria</param>
    /// <returns>List of matching maintenance log entries</returns>
    [HttpPost("search")]
    [ProducesResponseType(typeof(IEnumerable<BoatMaintenanceLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BoatMaintenanceLogDto>>> Search([FromBody] BoatMaintenanceLogSearchRequest request)
    {
        try
        {
            var logs = await _service.SearchAsync(request);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching maintenance logs");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while searching maintenance logs");
        }
    }
}
