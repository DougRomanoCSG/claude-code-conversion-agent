using BargeOps.Admin.Domain.Services;
using BargeOps.Shared.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BargeOps.Admin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = IdentityConstants.ApplicationScheme)]
public class FacilityController : ControllerBase
{
    private readonly IFacilityService _facilityService;
    private readonly ILogger<FacilityController> _logger;

    public FacilityController(
        IFacilityService facilityService,
        ILogger<FacilityController> logger)
    {
        _facilityService = facilityService;
        _logger = logger;
    }

    /// <summary>
    /// Search facilities with DataTables server-side processing
    /// </summary>
    [HttpPost("search")]
    [Authorize(Policy = "FacilityRead")]
    public async Task<ActionResult<DataTableResponse<FacilityDto>>> Search(
        [FromBody] FacilitySearchRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _facilityService.SearchAsync(request, cancellationToken);

            return Ok(new DataTableResponse<FacilityDto>
            {
                Draw = request.Draw,
                RecordsTotal = result.TotalRecords,
                RecordsFiltered = result.FilteredRecords,
                Data = result.Data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching facilities");
            return StatusCode(500, "An error occurred while searching facilities");
        }
    }

    /// <summary>
    /// Get facility by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "FacilityRead")]
    public async Task<ActionResult<FacilityDto>> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var facility = await _facilityService.GetByIdAsync(id, cancellationToken);

            if (facility == null)
            {
                return NotFound($"Facility with ID {id} not found");
            }

            return Ok(facility);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting facility {FacilityId}", id);
            return StatusCode(500, "An error occurred while retrieving the facility");
        }
    }

    /// <summary>
    /// Create new facility
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "FacilityModify")]
    public async Task<ActionResult<FacilityDto>> Create(
        [FromBody] FacilityDto facility,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _facilityService.CreateAsync(facility, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.LocationID }, created);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating facility");
            return StatusCode(500, "An error occurred while creating the facility");
        }
    }

    /// <summary>
    /// Update existing facility
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "FacilityModify")]
    public async Task<ActionResult<FacilityDto>> Update(
        int id,
        [FromBody] FacilityDto facility,
        CancellationToken cancellationToken)
    {
        try
        {
            if (id != facility.LocationID)
            {
                return BadRequest("ID mismatch");
            }

            var updated = await _facilityService.UpdateAsync(facility, cancellationToken);
            return Ok(updated);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating facility {FacilityId}", id);
            return StatusCode(500, "An error occurred while updating the facility");
        }
    }

    /// <summary>
    /// Delete facility
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "FacilityModify")]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _facilityService.DeleteAsync(id, cancellationToken);

            if (!deleted)
            {
                return NotFound($"Facility with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting facility {FacilityId}", id);
            return StatusCode(500, "An error occurred while deleting the facility");
        }
    }

    /// <summary>
    /// Get facility berths
    /// </summary>
    [HttpGet("{id}/berths")]
    [Authorize(Policy = "FacilityRead")]
    public async Task<ActionResult<IEnumerable<FacilityBerthDto>>> GetBerths(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var berths = await _facilityService.GetBerthsAsync(id, cancellationToken);
            return Ok(berths);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting berths for facility {FacilityId}", id);
            return StatusCode(500, "An error occurred while retrieving berths");
        }
    }

    /// <summary>
    /// Get facility statuses
    /// </summary>
    [HttpGet("{id}/statuses")]
    [Authorize(Policy = "FacilityRead")]
    public async Task<ActionResult<IEnumerable<FacilityStatusDto>>> GetStatuses(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var statuses = await _facilityService.GetStatusesAsync(id, cancellationToken);
            return Ok(statuses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statuses for facility {FacilityId}", id);
            return StatusCode(500, "An error occurred while retrieving statuses");
        }
    }

    /// <summary>
    /// Create facility berth
    /// </summary>
    [HttpPost("berths")]
    [Authorize(Policy = "FacilityModify")]
    public async Task<ActionResult<FacilityBerthDto>> CreateBerth(
        [FromBody] FacilityBerthDto berth,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _facilityService.CreateBerthAsync(berth, cancellationToken);
            return Ok(created);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating berth");
            return StatusCode(500, "An error occurred while creating the berth");
        }
    }

    /// <summary>
    /// Update facility berth
    /// </summary>
    [HttpPut("berths/{id}")]
    [Authorize(Policy = "FacilityModify")]
    public async Task<ActionResult<FacilityBerthDto>> UpdateBerth(
        int id,
        [FromBody] FacilityBerthDto berth,
        CancellationToken cancellationToken)
    {
        try
        {
            if (id != berth.FacilityBerthID)
            {
                return BadRequest("ID mismatch");
            }

            var updated = await _facilityService.UpdateBerthAsync(berth, cancellationToken);
            return Ok(updated);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating berth {BerthId}", id);
            return StatusCode(500, "An error occurred while updating the berth");
        }
    }

    /// <summary>
    /// Delete facility berth
    /// </summary>
    [HttpDelete("berths/{id}")]
    [Authorize(Policy = "FacilityModify")]
    public async Task<ActionResult> DeleteBerth(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _facilityService.DeleteBerthAsync(id, cancellationToken);

            if (!deleted)
            {
                return NotFound($"Berth with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting berth {BerthId}", id);
            return StatusCode(500, "An error occurred while deleting the berth");
        }
    }

    /// <summary>
    /// Create facility status
    /// </summary>
    [HttpPost("statuses")]
    [Authorize(Policy = "FacilityModify")]
    public async Task<ActionResult<FacilityStatusDto>> CreateStatus(
        [FromBody] FacilityStatusDto status,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _facilityService.CreateStatusAsync(status, cancellationToken);
            return Ok(created);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating status");
            return StatusCode(500, "An error occurred while creating the status");
        }
    }

    /// <summary>
    /// Update facility status
    /// </summary>
    [HttpPut("statuses/{id}")]
    [Authorize(Policy = "FacilityModify")]
    public async Task<ActionResult<FacilityStatusDto>> UpdateStatus(
        int id,
        [FromBody] FacilityStatusDto status,
        CancellationToken cancellationToken)
    {
        try
        {
            if (id != status.FacilityStatusID)
            {
                return BadRequest("ID mismatch");
            }

            var updated = await _facilityService.UpdateStatusAsync(status, cancellationToken);
            return Ok(updated);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status {StatusId}", id);
            return StatusCode(500, "An error occurred while updating the status");
        }
    }

    /// <summary>
    /// Delete facility status
    /// </summary>
    [HttpDelete("statuses/{id}")]
    [Authorize(Policy = "FacilityModify")]
    public async Task<ActionResult> DeleteStatus(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _facilityService.DeleteStatusAsync(id, cancellationToken);

            if (!deleted)
            {
                return NotFound($"Status with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting status {StatusId}", id);
            return StatusCode(500, "An error occurred while deleting the status");
        }
    }
}
