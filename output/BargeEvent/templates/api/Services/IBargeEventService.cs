using BargeOps.Shared.Dto;

namespace Admin.Domain.Services;

/// <summary>
/// Service interface for BargeEvent business logic.
/// Adds validation and business rules on top of repository operations.
/// </summary>
public interface IBargeEventService
{
    // ===== READ OPERATIONS =====

    /// <summary>
    /// Get a single barge event by ID.
    /// </summary>
    /// <param name="ticketEventId">Primary key of the ticket event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>BargeEventDto or null if not found</returns>
    Task<BargeEventDto?> GetByIdAsync(int ticketEventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all barge events for a specific ticket.
    /// </summary>
    /// <param name="ticketId">Parent ticket ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of BargeEventDto</returns>
    Task<IEnumerable<BargeEventDto>> GetByTicketIdAsync(int ticketId, CancellationToken cancellationToken = default);

    // ===== SEARCH OPERATIONS =====

    /// <summary>
    /// Search barge events with complex filtering criteria.
    /// Validates that at least one search criterion is provided.
    /// </summary>
    /// <param name="request">Search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of BargeEventSearchDto</returns>
    /// <exception cref="BusinessException">If no search criteria provided</exception>
    Task<PagedResult<BargeEventSearchDto>> SearchAsync(
        BargeEventSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search barge events for billing purposes.
    /// </summary>
    /// <param name="request">Billing search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of BargeEventBillingDto</returns>
    Task<PagedResult<BargeEventBillingDto>> BillingSearchAsync(
        BargeEventBillingSearchRequest request,
        CancellationToken cancellationToken = default);

    // ===== WRITE OPERATIONS =====

    /// <summary>
    /// Create a new barge event with business validation.
    /// Validates:
    /// - Required fields (TicketID, EventTypeID, StartDateTime)
    /// - Event type compatibility
    /// - Barge availability
    /// - Billing customer requirements
    /// </summary>
    /// <param name="bargeEvent">BargeEvent to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created BargeEventDto with generated ID</returns>
    /// <exception cref="BusinessException">If validation fails</exception>
    Task<BargeEventDto> CreateAsync(BargeEventDto bargeEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing barge event with business validation.
    /// Validates:
    /// - Event exists
    /// - Not already invoiced (or has rebill flag)
    /// - Required fields
    /// - Business rules
    /// </summary>
    /// <param name="bargeEvent">BargeEvent with updated values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated BargeEventDto</returns>
    /// <exception cref="BusinessException">If validation fails</exception>
    Task<BargeEventDto> UpdateAsync(BargeEventDto bargeEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Void (soft delete) a barge event.
    /// Validates:
    /// - Event exists
    /// - Not already invoiced (or special permission)
    /// - Ticket remains valid after voiding
    /// </summary>
    /// <param name="ticketEventId">Event to void</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    /// <exception cref="BusinessException">If validation fails</exception>
    Task<bool> DeleteAsync(int ticketEventId, CancellationToken cancellationToken = default);

    // ===== REBILLING OPERATIONS =====

    /// <summary>
    /// Mark multiple events for rebilling.
    /// Validates:
    /// - Events exist
    /// - User has billing modify permission
    /// </summary>
    /// <param name="ticketEventIds">Events to mark</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of events marked</returns>
    Task<int> MarkForRebillAsync(IEnumerable<int> ticketEventIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unmark multiple events from rebilling.
    /// </summary>
    /// <param name="ticketEventIds">Events to unmark</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of events unmarked</returns>
    Task<int> UnmarkForRebillAsync(IEnumerable<int> ticketEventIds, CancellationToken cancellationToken = default);

    // ===== CHILD ENTITY OPERATIONS =====

    /// <summary>
    /// Get barges associated with an event.
    /// </summary>
    /// <param name="ticketEventId">TicketEvent ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of BargeDto</returns>
    Task<IEnumerable<BargeDto>> GetBargesAsync(int ticketEventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get billing audit trail for an event.
    /// </summary>
    /// <param name="ticketEventId">TicketEvent ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of BillingAuditDto</returns>
    Task<IEnumerable<BillingAuditDto>> GetBillingAuditsAsync(int ticketEventId, CancellationToken cancellationToken = default);
}
