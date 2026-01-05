using BargeOps.Shared.Dto;

namespace BargeOpsAdmin.Services;

/// <summary>
/// UI Service interface for BargeEvent operations.
/// Calls the BargeEvent API endpoints.
/// </summary>
public interface IBargeEventService
{
    // ===== READ OPERATIONS =====

    /// <summary>
    /// Get a single barge event by ID
    /// </summary>
    Task<BargeEventDto?> GetByIdAsync(int ticketEventId);

    /// <summary>
    /// Get all barge events for a specific ticket
    /// </summary>
    Task<IEnumerable<BargeEventDto>> GetByTicketIdAsync(int ticketId);

    /// <summary>
    /// Get barges associated with an event
    /// </summary>
    Task<IEnumerable<BargeDto>> GetBargesAsync(int ticketEventId);

    /// <summary>
    /// Get billing audit trail for an event
    /// </summary>
    Task<IEnumerable<BillingAuditDto>> GetBillingAuditsAsync(int ticketEventId);

    // ===== SEARCH OPERATIONS =====

    /// <summary>
    /// Search barge events with complex filtering criteria
    /// </summary>
    Task<PagedResult<BargeEventSearchDto>> SearchAsync(BargeEventSearchRequest request);

    /// <summary>
    /// Search barge events for billing purposes
    /// </summary>
    Task<PagedResult<BargeEventBillingDto>> BillingSearchAsync(BargeEventBillingSearchRequest request);

    // ===== WRITE OPERATIONS =====

    /// <summary>
    /// Create a new barge event
    /// </summary>
    Task<BargeEventDto> CreateAsync(BargeEventDto bargeEvent);

    /// <summary>
    /// Update an existing barge event
    /// </summary>
    Task UpdateAsync(BargeEventDto bargeEvent);

    /// <summary>
    /// Void (soft delete) a barge event
    /// </summary>
    Task<bool> DeleteAsync(int ticketEventId);

    // ===== REBILLING OPERATIONS =====

    /// <summary>
    /// Mark multiple events for rebilling
    /// </summary>
    Task<int> MarkForRebillAsync(IEnumerable<int> ticketEventIds);

    /// <summary>
    /// Unmark multiple events from rebilling
    /// </summary>
    Task<int> UnmarkForRebillAsync(IEnumerable<int> ticketEventIds);
}
