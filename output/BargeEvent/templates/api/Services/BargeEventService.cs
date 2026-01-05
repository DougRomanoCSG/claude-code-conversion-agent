using Admin.Domain.Services;
using Admin.Infrastructure.Abstractions;
using BargeOps.Shared.Dto;
using Microsoft.Extensions.Logging;

namespace Admin.Infrastructure.Services;

/// <summary>
/// Service implementation for BargeEvent business logic.
/// Adds validation and business rules on top of repository operations.
/// </summary>
public class BargeEventService : IBargeEventService
{
    private readonly IBargeEventRepository _repository;
    private readonly ILogger<BargeEventService> _logger;

    public BargeEventService(
        IBargeEventRepository repository,
        ILogger<BargeEventService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ===== READ OPERATIONS =====

    public async Task<BargeEventDto?> GetByIdAsync(int ticketEventId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting barge event by ID: {TicketEventId}", ticketEventId);

        var result = await _repository.GetByIdAsync(ticketEventId, cancellationToken);

        if (result == null)
        {
            _logger.LogWarning("Barge event not found: {TicketEventId}", ticketEventId);
        }

        return result;
    }

    public async Task<IEnumerable<BargeEventDto>> GetByTicketIdAsync(int ticketId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting barge events for ticket: {TicketId}", ticketId);

        return await _repository.GetByTicketIdAsync(ticketId, cancellationToken);
    }

    // ===== SEARCH OPERATIONS =====

    public async Task<PagedResult<BargeEventSearchDto>> SearchAsync(
        BargeEventSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching barge events with criteria: {@Request}", request);

        // Validate at least one search criterion (prevent overly broad queries)
        if (!request.HasAtLeastOneCriterion())
        {
            throw new BusinessException(
                "At least one search criterion is required to prevent overly broad queries. " +
                "Please specify event type, customer, location, date range, or other filter.");
        }

        return await _repository.SearchAsync(request, cancellationToken);
    }

    public async Task<PagedResult<BargeEventBillingDto>> BillingSearchAsync(
        BargeEventBillingSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching billing events with criteria: {@Request}", request);

        return await _repository.BillingSearchAsync(request, cancellationToken);
    }

    // ===== WRITE OPERATIONS =====

    public async Task<BargeEventDto> CreateAsync(BargeEventDto bargeEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new barge event for Ticket: {TicketId}, EventType: {EventTypeId}",
            bargeEvent.TicketID, bargeEvent.EventTypeID);

        // Business validation
        ValidateBargeEvent(bargeEvent);

        // Additional create-specific validation
        if (bargeEvent.TicketEventID != 0)
        {
            throw new BusinessException("TicketEventID must be 0 for new events.");
        }

        if (bargeEvent.StartDateTime == default)
        {
            throw new BusinessException("Start date/time is required.");
        }

        // TODO: Add business-specific validation
        // - Check if barge is available at start time
        // - Validate event type compatibility with ticket
        // - Ensure billing customer is specified for billable events
        // - Validate location requirements based on event type

        try
        {
            var result = await _repository.CreateAsync(bargeEvent, cancellationToken);

            _logger.LogInformation("Created barge event: {TicketEventId}", result.TicketEventID);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating barge event for Ticket: {TicketId}", bargeEvent.TicketID);
            throw;
        }
    }

    public async Task<BargeEventDto> UpdateAsync(BargeEventDto bargeEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating barge event: {TicketEventId}", bargeEvent.TicketEventID);

        // Business validation
        ValidateBargeEvent(bargeEvent);

        // Additional update-specific validation
        if (bargeEvent.TicketEventID <= 0)
        {
            throw new BusinessException("TicketEventID is required for updates.");
        }

        // Check if event exists
        var existing = await _repository.GetByIdAsync(bargeEvent.TicketEventID, cancellationToken);
        if (existing == null)
        {
            throw new BusinessException($"Barge event {bargeEvent.TicketEventID} not found.");
        }

        // Validate invoiced events
        if (existing.IsInvoiced && !bargeEvent.Rebill)
        {
            throw new BusinessException(
                "Cannot modify invoiced event without rebill flag. " +
                "Mark for rebill first or contact billing department.");
        }

        // TODO: Add business-specific validation
        // - Prevent changes to completed events without special permission
        // - Validate billing changes don't affect finalized invoices
        // - Check freight contract consistency

        try
        {
            var result = await _repository.UpdateAsync(bargeEvent, cancellationToken);

            _logger.LogInformation("Updated barge event: {TicketEventId}", bargeEvent.TicketEventID);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating barge event: {TicketEventId}", bargeEvent.TicketEventID);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int ticketEventId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Voiding barge event: {TicketEventId}", ticketEventId);

        // Check if event exists
        var existing = await _repository.GetByIdAsync(ticketEventId, cancellationToken);
        if (existing == null)
        {
            throw new BusinessException($"Barge event {ticketEventId} not found.");
        }

        // Validate can void
        if (existing.IsInvoiced)
        {
            throw new BusinessException(
                "Cannot void invoiced event. Contact billing department.");
        }

        // TODO: Add business-specific validation
        // - Check if voiding this event leaves ticket in invalid state
        // - Validate no dependent child records (onboard orders, etc.)
        // - Ensure user has permission to void

        try
        {
            var result = await _repository.SetVoidStatusAsync(ticketEventId, 1, cancellationToken);

            _logger.LogInformation("Voided barge event: {TicketEventId}", ticketEventId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error voiding barge event: {TicketEventId}", ticketEventId);
            throw;
        }
    }

    // ===== REBILLING OPERATIONS =====

    public async Task<int> MarkForRebillAsync(IEnumerable<int> ticketEventIds, CancellationToken cancellationToken = default)
    {
        var ids = ticketEventIds.ToList();
        _logger.LogInformation("Marking {Count} events for rebill", ids.Count);

        if (!ids.Any())
        {
            return 0;
        }

        // TODO: Validate user has billing modify permission

        try
        {
            var result = await _repository.MarkForRebillAsync(ids, cancellationToken);

            _logger.LogInformation("Marked {Count} events for rebill", result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking events for rebill");
            throw;
        }
    }

    public async Task<int> UnmarkForRebillAsync(IEnumerable<int> ticketEventIds, CancellationToken cancellationToken = default)
    {
        var ids = ticketEventIds.ToList();
        _logger.LogInformation("Unmarking {Count} events from rebill", ids.Count);

        if (!ids.Any())
        {
            return 0;
        }

        try
        {
            var result = await _repository.UnmarkForRebillAsync(ids, cancellationToken);

            _logger.LogInformation("Unmarked {Count} events from rebill", result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unmarking events from rebill");
            throw;
        }
    }

    // ===== CHILD ENTITY OPERATIONS =====

    public async Task<IEnumerable<BargeDto>> GetBargesAsync(int ticketEventId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting barges for event: {TicketEventId}", ticketEventId);

        return await _repository.GetBargesAsync(ticketEventId, cancellationToken);
    }

    public async Task<IEnumerable<BillingAuditDto>> GetBillingAuditsAsync(int ticketEventId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting billing audits for event: {TicketEventId}", ticketEventId);

        return await _repository.GetBillingAuditsAsync(ticketEventId, cancellationToken);
    }

    // ===== PRIVATE VALIDATION METHODS =====

    private static void ValidateBargeEvent(BargeEventDto bargeEvent)
    {
        if (bargeEvent == null)
        {
            throw new ArgumentNullException(nameof(bargeEvent));
        }

        // Required fields
        if (bargeEvent.TicketID <= 0)
        {
            throw new BusinessException("Ticket ID is required.");
        }

        if (bargeEvent.EventTypeID <= 0)
        {
            throw new BusinessException("Event type is required.");
        }

        // TODO: Add comprehensive business validation
        // - Validate billing customer when BillableStatus > 0
        // - Validate FromLocation and ToLocation based on EventType
        // - Validate freight fields when freight license active
        // - Validate draft fields for draft survey events
        // - Enforce business rules from business-logic.json
    }
}

/// <summary>
/// Business exception for validation failures
/// </summary>
public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
    public BusinessException(string message, Exception innerException) : base(message, innerException) { }
}
