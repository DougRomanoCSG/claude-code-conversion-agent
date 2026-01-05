using Admin.Domain.Services;
using BargeOps.Shared.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admin.Api.Controllers;

/// <summary>
/// API controller for BargeEvent operations.
/// Handles barge events including loads, unloads, shifts, fleeting, and midstream operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class BargeEventController : ControllerBase
{
    private readonly IBargeEventService _service;
    private readonly ILogger<BargeEventController> _logger;

    public BargeEventController(
        IBargeEventService service,
        ILogger<BargeEventController> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ===== GET OPERATIONS =====

    /// <summary>
    /// Get a single barge event by ID
    /// </summary>
    /// <param name="id">TicketEvent ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>BargeEventDto or 404 if not found</returns>
    /// <response code="200">Returns the barge event</response>
    /// <response code="404">Event not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BargeEventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        _logger.LogDebug("API GET BargeEvent/{Id}", id);

        var result = await _service.GetByIdAsync(id, cancellationToken);

        if (result == null)
        {
            return NotFound(new { message = $"Barge event {id} not found" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all barge events for a specific ticket
    /// </summary>
    /// <param name="ticketId">Ticket ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of BargeEventDto</returns>
    /// <response code="200">Returns the barge events for the ticket</response>
    [HttpGet("ticket/{ticketId}")]
    [ProducesResponseType(typeof(IEnumerable<BargeEventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByTicketId(int ticketId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("API GET BargeEvent/ticket/{TicketId}", ticketId);

        var result = await _service.GetByTicketIdAsync(ticketId, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get barges associated with an event
    /// </summary>
    /// <param name="id">TicketEvent ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of BargeDto</returns>
    /// <response code="200">Returns the barges for the event</response>
    [HttpGet("{id}/barges")]
    [ProducesResponseType(typeof(IEnumerable<BargeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBarges(int id, CancellationToken cancellationToken)
    {
        _logger.LogDebug("API GET BargeEvent/{Id}/barges", id);

        var result = await _service.GetBargesAsync(id, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get billing audit trail for an event
    /// </summary>
    /// <param name="id">TicketEvent ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of BillingAuditDto</returns>
    /// <response code="200">Returns the billing audit trail</response>
    [HttpGet("{id}/billing-audits")]
    [ProducesResponseType(typeof(IEnumerable<BillingAuditDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBillingAudits(int id, CancellationToken cancellationToken)
    {
        _logger.LogDebug("API GET BargeEvent/{Id}/billing-audits", id);

        var result = await _service.GetBillingAuditsAsync(id, cancellationToken);

        return Ok(result);
    }

    // ===== SEARCH OPERATIONS =====

    /// <summary>
    /// Search barge events with complex filtering criteria
    /// </summary>
    /// <param name="request">Search criteria with ListQuery support</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of BargeEventSearchDto</returns>
    /// <response code="200">Returns the search results</response>
    /// <response code="400">Invalid search criteria</response>
    /// <remarks>
    /// At least one search criterion is required to prevent overly broad queries.
    /// Supported criteria: EventType, Customer, Location, Boat, DateRange, BargeNumbers, etc.
    /// </remarks>
    [HttpPost("search")]
    [ProducesResponseType(typeof(PagedResult<BargeEventSearchDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromBody] BargeEventSearchRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("API POST BargeEvent/search with {@Request}", request);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _service.SearchAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Business validation failed for search");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Search barge events for billing purposes
    /// </summary>
    /// <param name="request">Billing search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of BargeEventBillingDto</returns>
    /// <response code="200">Returns the billing search results</response>
    /// <response code="400">Invalid search criteria</response>
    [HttpPost("billing-search")]
    [ProducesResponseType(typeof(PagedResult<BargeEventBillingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BillingSearch(
        [FromBody] BargeEventBillingSearchRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("API POST BargeEvent/billing-search with {@Request}", request);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _service.BillingSearchAsync(request, cancellationToken);
        return Ok(result);
    }

    // ===== CREATE/UPDATE/DELETE OPERATIONS =====

    /// <summary>
    /// Create a new barge event
    /// </summary>
    /// <param name="bargeEvent">BargeEvent to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created BargeEventDto with generated ID</returns>
    /// <response code="201">Event created successfully</response>
    /// <response code="400">Invalid input or business validation failed</response>
    [HttpPost]
    [ProducesResponseType(typeof(BargeEventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] BargeEventDto bargeEvent,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("API POST BargeEvent - Creating event for Ticket {TicketId}", bargeEvent.TicketID);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _service.CreateAsync(bargeEvent, cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.TicketEventID },
                result);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Business validation failed for create");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing barge event
    /// </summary>
    /// <param name="id">TicketEvent ID</param>
    /// <param name="bargeEvent">Updated BargeEvent data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated BargeEventDto</returns>
    /// <response code="200">Event updated successfully</response>
    /// <response code="400">Invalid input or business validation failed</response>
    /// <response code="404">Event not found</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(BargeEventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] BargeEventDto bargeEvent,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("API PUT BargeEvent/{Id}", id);

        if (id != bargeEvent.TicketEventID)
        {
            return BadRequest(new { message = "ID mismatch between URL and body" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _service.UpdateAsync(bargeEvent, cancellationToken);
            return Ok(result);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Business validation failed for update");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Void (soft delete) a barge event
    /// </summary>
    /// <param name="id">TicketEvent ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Event voided successfully</response>
    /// <response code="400">Cannot void invoiced event</response>
    /// <response code="404">Event not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("API DELETE BargeEvent/{Id}", id);

        try
        {
            var success = await _service.DeleteAsync(id, cancellationToken);

            if (!success)
            {
                return NotFound(new { message = $"Barge event {id} not found" });
            }

            return NoContent();
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Business validation failed for delete");
            return BadRequest(new { message = ex.Message });
        }
    }

    // ===== REBILLING OPERATIONS =====

    /// <summary>
    /// Mark multiple events for rebilling
    /// </summary>
    /// <param name="ticketEventIds">Collection of TicketEvent IDs to mark</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of events marked</returns>
    /// <response code="200">Events marked successfully</response>
    /// <response code="400">Invalid input</response>
    [HttpPost("mark-rebill")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MarkForRebill(
        [FromBody] IEnumerable<int> ticketEventIds,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("API POST BargeEvent/mark-rebill - {Count} events", ticketEventIds.Count());

        if (!ticketEventIds.Any())
        {
            return BadRequest(new { message = "No event IDs provided" });
        }

        var result = await _service.MarkForRebillAsync(ticketEventIds, cancellationToken);

        return Ok(new { markedCount = result });
    }

    /// <summary>
    /// Unmark multiple events from rebilling
    /// </summary>
    /// <param name="ticketEventIds">Collection of TicketEvent IDs to unmark</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of events unmarked</returns>
    /// <response code="200">Events unmarked successfully</response>
    /// <response code="400">Invalid input</response>
    [HttpPost("unmark-rebill")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnmarkForRebill(
        [FromBody] IEnumerable<int> ticketEventIds,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("API POST BargeEvent/unmark-rebill - {Count} events", ticketEventIds.Count());

        if (!ticketEventIds.Any())
        {
            return BadRequest(new { message = "No event IDs provided" });
        }

        var result = await _service.UnmarkForRebillAsync(ticketEventIds, cancellationToken);

        return Ok(new { unmarkedCount = result });
    }
}
