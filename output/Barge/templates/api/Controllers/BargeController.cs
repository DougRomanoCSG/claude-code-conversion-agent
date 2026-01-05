using Admin.Domain.Services;
using BargeOps.Shared.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Admin.Api.Controllers;

/// <summary>
/// API Controller for Barge operations
/// RESTful endpoints with authorization
/// Accepts and returns DTOs from BargeOps.Shared
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Apply authorization to all endpoints
public class BargeController : ControllerBase
{
    private readonly IBargeService _bargeService;
    private readonly ILogger<BargeController> _logger;

    public BargeController(
        IBargeService bargeService,
        ILogger<BargeController> logger)
    {
        _bargeService = bargeService ?? throw new ArgumentNullException(nameof(bargeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Search barges with paging and filtering
    /// </summary>
    /// <param name="request">Search criteria with paging/sorting</param>
    /// <returns>Paged result set of barges</returns>
    [HttpPost("search")]
    [ProducesResponseType(typeof(PagedResult<BargeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<BargeDto>>> Search([FromBody] BargeSearchRequest request)
    {
        try
        {
            var result = await _bargeService.SearchAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching barges");
            return BadRequest(new { error = "An error occurred while searching barges." });
        }
    }

    /// <summary>
    /// Get barge by ID
    /// </summary>
    /// <param name="id">Barge ID</param>
    /// <returns>Barge DTO</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BargeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BargeDto>> GetById(int id)
    {
        try
        {
            var barge = await _bargeService.GetByIdAsync(id);

            if (barge == null)
            {
                return NotFound(new { error = $"Barge with ID {id} not found." });
            }

            return Ok(barge);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting barge {BargeId}", id);
            return BadRequest(new { error = "An error occurred while retrieving the barge." });
        }
    }

    /// <summary>
    /// Create new barge
    /// </summary>
    /// <param name="barge">Barge DTO to create</param>
    /// <returns>New barge ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<int>> Create([FromBody] BargeDto barge)
    {
        try
        {
            var userName = GetCurrentUserName();
            var result = await _bargeService.CreateAsync(barge, userName);

            if (!result.IsSuccess)
            {
                return BadRequest(new { errors = result.Errors });
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Data }, result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating barge");
            return BadRequest(new { error = "An error occurred while creating the barge." });
        }
    }

    /// <summary>
    /// Update existing barge
    /// </summary>
    /// <param name="id">Barge ID</param>
    /// <param name="barge">Barge DTO with updated values</param>
    /// <returns>Success indicator</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] BargeDto barge)
    {
        try
        {
            if (id != barge.BargeID)
            {
                return BadRequest(new { error = "Barge ID mismatch." });
            }

            var userName = GetCurrentUserName();
            var result = await _bargeService.UpdateAsync(barge, userName);

            if (!result.IsSuccess)
            {
                return BadRequest(new { errors = result.Errors });
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating barge {BargeId}", id);
            return BadRequest(new { error = "An error occurred while updating the barge." });
        }
    }

    /// <summary>
    /// Delete barge (soft delete)
    /// </summary>
    /// <param name="id">Barge ID to delete</param>
    /// <returns>Success indicator</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var userName = GetCurrentUserName();
            var result = await _bargeService.DeleteAsync(id, userName);

            if (!result.IsSuccess)
            {
                return BadRequest(new { errors = result.Errors });
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting barge {BargeId}", id);
            return BadRequest(new { error = "An error occurred while deleting the barge." });
        }
    }

    #region Barge Charters

    /// <summary>
    /// Get barge charters for a barge
    /// </summary>
    /// <param name="id">Barge ID</param>
    /// <returns>List of charters</returns>
    [HttpGet("{id}/charters")]
    [ProducesResponseType(typeof(List<BargeCharterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<BargeCharterDto>>> GetCharters(int id)
    {
        try
        {
            var charters = await _bargeService.GetBargeChartersAsync(id);
            return Ok(charters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting charters for barge {BargeId}", id);
            return BadRequest(new { error = "An error occurred while retrieving charters." });
        }
    }

    /// <summary>
    /// Create barge charter
    /// </summary>
    /// <param name="id">Barge ID</param>
    /// <param name="charter">Charter DTO to create</param>
    /// <returns>New charter ID</returns>
    [HttpPost("{id}/charters")]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<int>> CreateCharter(int id, [FromBody] BargeCharterDto charter)
    {
        try
        {
            if (id != charter.BargeID)
            {
                return BadRequest(new { error = "Barge ID mismatch." });
            }

            var userName = GetCurrentUserName();
            var result = await _bargeService.CreateCharterAsync(charter, userName);

            if (!result.IsSuccess)
            {
                return BadRequest(new { errors = result.Errors });
            }

            return CreatedAtAction(nameof(GetCharters), new { id }, result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating charter for barge {BargeId}", id);
            return BadRequest(new { error = "An error occurred while creating the charter." });
        }
    }

    /// <summary>
    /// Update barge charter
    /// </summary>
    /// <param name="id">Barge ID</param>
    /// <param name="charterId">Charter ID</param>
    /// <param name="charter">Charter DTO with updated values</param>
    /// <returns>Success indicator</returns>
    [HttpPut("{id}/charters/{charterId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateCharter(int id, int charterId, [FromBody] BargeCharterDto charter)
    {
        try
        {
            if (id != charter.BargeID || charterId != charter.BargeCharterID)
            {
                return BadRequest(new { error = "ID mismatch." });
            }

            var userName = GetCurrentUserName();
            var result = await _bargeService.UpdateCharterAsync(charter, userName);

            if (!result.IsSuccess)
            {
                return BadRequest(new { errors = result.Errors });
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating charter {CharterId} for barge {BargeId}", charterId, id);
            return BadRequest(new { error = "An error occurred while updating the charter." });
        }
    }

    /// <summary>
    /// Delete barge charter
    /// </summary>
    /// <param name="id">Barge ID</param>
    /// <param name="charterId">Charter ID to delete</param>
    /// <returns>Success indicator</returns>
    [HttpDelete("{id}/charters/{charterId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteCharter(int id, int charterId)
    {
        try
        {
            var result = await _bargeService.DeleteCharterAsync(charterId);

            if (!result.IsSuccess)
            {
                return BadRequest(new { errors = result.Errors });
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting charter {CharterId} for barge {BargeId}", charterId, id);
            return BadRequest(new { error = "An error occurred while deleting the charter." });
        }
    }

    #endregion

    #region Update Location

    /// <summary>
    /// Update barge location with optional tier/berth coordinates
    /// </summary>
    /// <param name="id">Barge ID</param>
    /// <param name="request">Location update request</param>
    /// <returns>Success indicator</returns>
    [HttpPut("{id}/location")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateLocation(int id, [FromBody] UpdateLocationRequest request)
    {
        try
        {
            var userName = GetCurrentUserName();
            var result = await _bargeService.UpdateLocationAsync(
                id,
                request.LocationId,
                request.LocationDateTime,
                request.TierX,
                request.TierY,
                request.FacilityBerthX,
                request.FacilityBerthY,
                userName);

            if (!result.IsSuccess)
            {
                return BadRequest(new { errors = result.Errors });
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating location for barge {BargeId}", id);
            return BadRequest(new { error = "An error occurred while updating the location." });
        }
    }

    #endregion

    #region Helper Methods

    private string GetCurrentUserName()
    {
        return User.FindFirst(ClaimTypes.Name)?.Value
            ?? User.FindFirst(ClaimTypes.Email)?.Value
            ?? "Unknown";
    }

    #endregion
}

#region Request Models

/// <summary>
/// Request model for updating barge location
/// </summary>
public class UpdateLocationRequest
{
    public int LocationId { get; set; }
    public DateTime LocationDateTime { get; set; }
    public short? TierX { get; set; }
    public short? TierY { get; set; }
    public short? FacilityBerthX { get; set; }
    public short? FacilityBerthY { get; set; }
}

#endregion
