using System;
using System.Threading.Tasks;
using BargeOps.Shared.Dto;
using Admin.Domain.Services;
using Csg.ListQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Admin.Api.Controllers;

/// <summary>
/// API Controller for Barge Position History operations.
/// Target: C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Api\Controllers\BargePositionHistoryController.cs
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BargePositionHistoryController : ControllerBase
{
    private readonly IBargePositionHistoryService _service;
    private readonly ILogger<BargePositionHistoryController> _logger;

    public BargePositionHistoryController(
        IBargePositionHistoryService service,
        ILogger<BargePositionHistoryController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Search for barge position history records.
    /// </summary>
    /// <param name="request">Search criteria including FleetID, Date, TierGroupID</param>
    /// <returns>Paged result set for DataTables</returns>
    [HttpPost("search")]
    [Authorize(Policy = "BargePositionHistoryView")]
    [ProducesResponseType(typeof(DataTableResponse<BargePositionHistoryDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Search([FromBody] BargePositionHistorySearchRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest("Search request cannot be null");
            }

            var result = await _service.SearchAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid search request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching barge position history");
            return StatusCode(500, "An error occurred while searching barge position history");
        }
    }

    /// <summary>
    /// Get a single barge position history record by ID.
    /// </summary>
    /// <param name="id">FleetPositionHistoryID</param>
    /// <returns>BargePositionHistoryDto</returns>
    [HttpGet("{id}")]
    [Authorize(Policy = "BargePositionHistoryView")]
    [ProducesResponseType(typeof(BargePositionHistoryDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
            {
                return NotFound($"Barge position history with ID {id} not found");
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting barge position history with ID: {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the barge position history");
        }
    }

    /// <summary>
    /// Create a new barge position history record.
    /// </summary>
    /// <param name="dto">BargePositionHistoryDto with new record data</param>
    /// <returns>New FleetPositionHistoryID</returns>
    [HttpPost]
    [Authorize(Policy = "BargePositionHistoryModify")]
    [ProducesResponseType(typeof(int), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Create([FromBody] BargePositionHistoryDto dto)
    {
        try
        {
            if (dto == null)
            {
                return BadRequest("Barge position history data cannot be null");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var username = User.Identity?.Name ?? "System";
            var newId = await _service.CreateAsync(dto, username);

            return CreatedAtAction(nameof(GetById), new { id = newId }, newId);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid create request");
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating barge position history");
            return StatusCode(500, "An error occurred while creating the barge position history");
        }
    }

    /// <summary>
    /// Update an existing barge position history record.
    /// </summary>
    /// <param name="id">FleetPositionHistoryID</param>
    /// <param name="dto">BargePositionHistoryDto with updated data</param>
    /// <returns>NoContent on success</returns>
    [HttpPut("{id}")]
    [Authorize(Policy = "BargePositionHistoryModify")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(409)] // Conflict for concurrency
    public async Task<IActionResult> Update(int id, [FromBody] BargePositionHistoryDto dto)
    {
        try
        {
            if (dto == null)
            {
                return BadRequest("Barge position history data cannot be null");
            }

            if (id != dto.FleetPositionHistoryID)
            {
                return BadRequest("ID in URL does not match ID in body");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var username = User.Identity?.Name ?? "System";
            await _service.UpdateAsync(dto, username);

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid update request");
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation");
            return BadRequest(ex.Message);
        }
        catch (ConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict for ID: {Id}", id);
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating barge position history with ID: {Id}", id);
            return StatusCode(500, "An error occurred while updating the barge position history");
        }
    }

    /// <summary>
    /// Delete a barge position history record (hard delete).
    /// </summary>
    /// <param name="id">FleetPositionHistoryID</param>
    /// <returns>NoContent on success</returns>
    [HttpDelete("{id}")]
    [Authorize(Policy = "BargePositionHistoryDelete")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid delete request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting barge position history with ID: {Id}", id);
            return StatusCode(500, "An error occurred while deleting the barge position history");
        }
    }

    /// <summary>
    /// Validate that a barge number exists.
    /// Used for remote validation in UI.
    /// </summary>
    /// <param name="bargeNum">Barge number to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    [HttpGet("validate-barge")]
    [Authorize(Policy = "BargePositionHistoryView")]
    [ProducesResponseType(typeof(bool), 200)]
    public async Task<IActionResult> ValidateBarge([FromQuery] string bargeNum)
    {
        try
        {
            var isValid = await _service.ValidateBargeNumAsync(bargeNum);
            return Ok(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating barge number: {BargeNum}", bargeNum);
            return Ok(false); // Return false on error for validation endpoint
        }
    }
}

/// <summary>
/// Exception thrown when optimistic concurrency check fails.
/// </summary>
public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message) { }
}
