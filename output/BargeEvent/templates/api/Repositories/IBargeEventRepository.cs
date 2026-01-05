using BargeOps.Shared.Dto;

namespace Admin.Infrastructure.Abstractions;

/// <summary>
/// Repository interface for BargeEvent (TicketEvent) operations.
/// BargeEvent uses DTOs directly - no separate domain models.
/// NOTE: BargeEvent is a composite wrapper around TicketEvent table.
/// </summary>
public interface IBargeEventRepository
{
    // ===== READ OPERATIONS =====

    /// <summary>
    /// Get a single barge event by ID.
    /// Includes joined data from related tables (EventType, Customer, Boat, Location, etc.)
    /// </summary>
    /// <param name="ticketEventId">Primary key of the ticket event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>BargeEventDto or null if not found</returns>
    Task<BargeEventDto?> GetByIdAsync(int ticketEventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all barge events for a specific ticket.
    /// A ticket can have multiple events (Load, Unload, Shift, etc.)
    /// </summary>
    /// <param name="ticketId">Parent ticket ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of BargeEventDto</returns>
    Task<IEnumerable<BargeEventDto>> GetByTicketIdAsync(int ticketId, CancellationToken cancellationToken = default);

    // ===== SEARCH OPERATIONS =====

    /// <summary>
    /// Search barge events with complex filtering criteria.
    /// Supports multi-criteria search with date ranges, customers, locations, boats, etc.
    /// Returns flattened DTOs optimized for grid display.
    /// </summary>
    /// <param name="request">Search criteria with ListQuery support (sorting, paging)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of BargeEventSearchDto</returns>
    Task<PagedResult<BargeEventSearchDto>> SearchAsync(
        BargeEventSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search barge events for billing purposes.
    /// Used for Ready to Invoice screens and billing reports.
    /// Includes detailed financial and rate information.
    /// </summary>
    /// <param name="request">Billing search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of BargeEventBillingDto</returns>
    Task<PagedResult<BargeEventBillingDto>> BillingSearchAsync(
        BargeEventBillingSearchRequest request,
        CancellationToken cancellationToken = default);

    // ===== WRITE OPERATIONS =====

    /// <summary>
    /// Create a new barge event.
    /// Inserts into TicketEvent table and returns generated TicketEventID.
    /// </summary>
    /// <param name="bargeEvent">BargeEvent DTO to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Newly created BargeEventDto with generated ID</returns>
    Task<BargeEventDto> CreateAsync(BargeEventDto bargeEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing barge event.
    /// Updates TicketEvent table with all modified fields.
    /// Sets ModifyDateTime and ModifyUser automatically.
    /// </summary>
    /// <param name="bargeEvent">BargeEvent DTO with updated values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated BargeEventDto</returns>
    Task<BargeEventDto> UpdateAsync(BargeEventDto bargeEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft delete a barge event by setting VoidStatus.
    /// Does NOT physically delete the record - sets VoidStatus > 0.
    /// </summary>
    /// <param name="ticketEventId">Primary key of event to void</param>
    /// <param name="voidStatus">Void status value (typically 1)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> SetVoidStatusAsync(int ticketEventId, byte voidStatus, CancellationToken cancellationToken = default);

    // ===== REBILLING OPERATIONS =====

    /// <summary>
    /// Mark multiple events for rebilling.
    /// Sets Rebill flag on selected TicketEvents.
    /// </summary>
    /// <param name="ticketEventIds">Collection of TicketEventIDs to mark</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of events marked</returns>
    Task<int> MarkForRebillAsync(IEnumerable<int> ticketEventIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unmark multiple events from rebilling.
    /// Clears Rebill flag on selected TicketEvents.
    /// </summary>
    /// <param name="ticketEventIds">Collection of TicketEventIDs to unmark</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of events unmarked</returns>
    Task<int> UnmarkForRebillAsync(IEnumerable<int> ticketEventIds, CancellationToken cancellationToken = default);

    // ===== CHILD ENTITY OPERATIONS =====

    /// <summary>
    /// Get barges associated with a specific event.
    /// Note: In modern implementation, load barges separately via BargeRepository.
    /// This method is for backward compatibility.
    /// </summary>
    /// <param name="ticketEventId">TicketEvent ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of BargeDto</returns>
    Task<IEnumerable<BargeDto>> GetBargesAsync(int ticketEventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get billing audit trail for an event.
    /// Shows history of billing field changes.
    /// </summary>
    /// <param name="ticketEventId">TicketEvent ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of BillingAuditDto</returns>
    Task<IEnumerable<BillingAuditDto>> GetBillingAuditsAsync(int ticketEventId, CancellationToken cancellationToken = default);
}
