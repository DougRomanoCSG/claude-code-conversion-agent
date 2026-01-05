using Admin.Domain.Services;
using BargeOps.Shared.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admin.Api.Controllers;

/// <summary>
/// API controller for BargeSeries operations.
/// Provides RESTful endpoints for managing barge series and draft tonnage data.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "BargeSeriesView")]
public class BargeSeriesController : ControllerBase
{
    private readonly IBargeSeriesService _service;
    private readonly ILogger<BargeSeriesController> _logger;

    public BargeSeriesController(
        IBargeSeriesService service,
        ILogger<BargeSeriesController> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Searches for barge series with filtering, sorting, and pagination.
    /// Used by DataTables server-side processing.
    /// </summary>
    /// <param name="request">Search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result set</returns>
    [HttpPost("search")]
    [ProducesResponseType(typeof(PagedResult<BargeSeriesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<BargeSeriesDto>>> Search(
        [FromBody] BargeSeriesSearchRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.SearchAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching barge series");
            return BadRequest(new { error = "Failed to search barge series." });
        }
    }

    /// <summary>
    /// Gets all barge series for dropdown/lookup lists.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of barge series</returns>
    [HttpGet("list")]
    [ProducesResponseType(typeof(IEnumerable<BargeSeriesDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BargeSeriesDto>>> GetList(
        CancellationToken cancellationToken)
    {
        var result = await _service.GetListAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a single barge series by ID with draft tonnage records.
    /// </summary>
    /// <param name="id">BargeSeries ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>BargeSeries DTO</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BargeSeriesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BargeSeriesDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);

        if (result == null)
        {
            _logger.LogWarning("BargeSeries with ID {BargeSeriesId} not found", id);
            return NotFound(new { error = $"BargeSeries with ID {id} not found." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Creates a new barge series with draft tonnage records.
    /// </summary>
    /// <param name="bargeSeries">BargeSeries DTO to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created BargeSeries DTO</returns>
    [HttpPost]
    [Authorize(Policy = "BargeSeriesCreate")]
    [ProducesResponseType(typeof(BargeSeriesDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BargeSeriesDto>> Create(
        [FromBody] BargeSeriesDto bargeSeries,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.CreateAsync(bargeSeries, cancellationToken);

            _logger.LogInformation(
                "Created BargeSeries {BargeSeriesId}: {Name}",
                result.BargeSeriesID,
                result.Name);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.BargeSeriesID },
                result);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed while creating barge series");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating barge series");
            return BadRequest(new { error = "Failed to create barge series." });
        }
    }

    /// <summary>
    /// Updates an existing barge series and its draft tonnage records.
    /// </summary>
    /// <param name="id">BargeSeries ID</param>
    /// <param name="bargeSeries">BargeSeries DTO to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated BargeSeries DTO</returns>
    [HttpPut("{id}")]
    [Authorize(Policy = "BargeSeriesModify")]
    [ProducesResponseType(typeof(BargeSeriesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BargeSeriesDto>> Update(
        int id,
        [FromBody] BargeSeriesDto bargeSeries,
        CancellationToken cancellationToken)
    {
        if (id != bargeSeries.BargeSeriesID)
        {
            return BadRequest(new { error = "ID mismatch between URL and body." });
        }

        try
        {
            var result = await _service.UpdateAsync(bargeSeries, cancellationToken);

            _logger.LogInformation(
                "Updated BargeSeries {BargeSeriesId}: {Name}",
                result.BargeSeriesID,
                result.Name);

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning(ex, "BargeSeries {BargeSeriesId} not found for update", id);
            return NotFound(new { error = ex.Message });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed while updating barge series {BargeSeriesId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating barge series {BargeSeriesId}", id);
            return BadRequest(new { error = "Failed to update barge series." });
        }
    }

    /// <summary>
    /// Soft deletes a barge series by setting IsActive = false.
    /// CRITICAL: This is a SOFT DELETE - records are not physically removed!
    /// </summary>
    /// <param name="id">BargeSeries ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [Authorize(Policy = "BargeSeriesDelete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var success = await _service.DeactivateAsync(id, cancellationToken);

            if (!success)
            {
                _logger.LogWarning("Failed to deactivate BargeSeries {BargeSeriesId}", id);
                return NotFound(new { error = $"BargeSeries with ID {id} not found." });
            }

            _logger.LogInformation("Deactivated BargeSeries {BargeSeriesId}", id);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning(ex, "BargeSeries {BargeSeriesId} not found for deletion", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating barge series {BargeSeriesId}", id);
            return BadRequest(new { error = "Failed to deactivate barge series." });
        }
    }

    /// <summary>
    /// Reactivates a previously deactivated barge series.
    /// </summary>
    /// <param name="id">BargeSeries ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpPost("{id}/reactivate")]
    [Authorize(Policy = "BargeSeriesModify")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var success = await _service.ReactivateAsync(id, cancellationToken);

            if (!success)
            {
                _logger.LogWarning("Failed to reactivate BargeSeries {BargeSeriesId}", id);
                return NotFound(new { error = $"BargeSeries with ID {id} not found." });
            }

            _logger.LogInformation("Reactivated BargeSeries {BargeSeriesId}", id);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning(ex, "BargeSeries {BargeSeriesId} not found for reactivation", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating barge series {BargeSeriesId}", id);
            return BadRequest(new { error = "Failed to reactivate barge series." });
        }
    }

    /// <summary>
    /// Gets draft tonnage records for a specific barge series.
    /// </summary>
    /// <param name="id">BargeSeries ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of draft records</returns>
    [HttpGet("{id}/drafts")]
    [ProducesResponseType(typeof(IEnumerable<BargeSeriesDraftDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BargeSeriesDraftDto>>> GetDrafts(
        int id,
        CancellationToken cancellationToken)
    {
        var bargeSeries = await _service.GetByIdAsync(id, cancellationToken);

        if (bargeSeries == null)
        {
            return NotFound(new { error = $"BargeSeries with ID {id} not found." });
        }

        return Ok(bargeSeries.Drafts);
    }

    /// <summary>
    /// Bulk updates draft tonnage records for a barge series.
    /// Used when pasting data from clipboard or bulk editing the tonnage grid.
    /// </summary>
    /// <param name="id">BargeSeries ID</param>
    /// <param name="drafts">Collection of draft records to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated draft collection</returns>
    [HttpPost("{id}/drafts")]
    [Authorize(Policy = "BargeSeriesModify")]
    [ProducesResponseType(typeof(IEnumerable<BargeSeriesDraftDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<BargeSeriesDraftDto>>> UpdateDrafts(
        int id,
        [FromBody] IEnumerable<BargeSeriesDraftDto> drafts,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.UpdateDraftsAsync(id, drafts, cancellationToken);

            _logger.LogInformation(
                "Updated draft tonnage records for BargeSeries {BargeSeriesId}",
                id);

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning(ex, "BargeSeries {BargeSeriesId} not found for draft update", id);
            return NotFound(new { error = ex.Message });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed while updating drafts for BargeSeries {BargeSeriesId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating drafts for BargeSeries {BargeSeriesId}", id);
            return BadRequest(new { error = "Failed to update draft tonnage records." });
        }
    }
}
