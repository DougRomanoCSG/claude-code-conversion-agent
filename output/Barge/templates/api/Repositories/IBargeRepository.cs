using BargeOps.Shared.Dto;

namespace Admin.Infrastructure.Repositories;

/// <summary>
/// Repository interface for Barge operations
/// Returns DTOs directly from BargeOps.Shared (no mapping needed)
/// </summary>
public interface IBargeRepository
{
    /// <summary>
    /// Search barges with paging and filtering
    /// Returns complete search results with ticket and location information
    /// </summary>
    /// <param name="request">Search criteria including filters, paging, and sorting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result set of barges matching criteria</returns>
    Task<PagedResult<BargeDto>> SearchAsync(
        BargeSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get barge by ID with all related data
    /// Includes navigation properties (names) for display
    /// Does NOT include child collections (BargeCharters, Ticket) - use separate methods
    /// </summary>
    /// <param name="bargeId">Barge ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Barge DTO or null if not found</returns>
    Task<BargeDto?> GetByIdAsync(
        int bargeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create new barge
    /// Auto-populates audit fields (CreateDateTime, CreateUser, ModifyDateTime, ModifyUser)
    /// </summary>
    /// <param name="barge">Barge DTO to create</param>
    /// <param name="userName">Current user name for audit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New barge ID</returns>
    Task<int> CreateAsync(
        BargeDto barge,
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing barge
    /// Auto-updates ModifyDateTime and ModifyUser audit fields
    /// </summary>
    /// <param name="barge">Barge DTO with updated values</param>
    /// <param name="userName">Current user name for audit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateAsync(
        BargeDto barge,
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft delete barge (sets IsActive = false)
    /// Auto-updates ModifyDateTime and ModifyUser audit fields
    /// </summary>
    /// <param name="bargeId">Barge ID to delete</param>
    /// <param name="userName">Current user name for audit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteAsync(
        int bargeId,
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get barge charters for a barge
    /// Returns collection of BargeCharterDto with customer names
    /// </summary>
    /// <param name="bargeId">Barge ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of barge charters</returns>
    Task<List<BargeCharterDto>> GetBargeChartersAsync(
        int bargeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create new barge charter
    /// Validates date range overlaps in repository
    /// </summary>
    /// <param name="charter">Barge charter DTO to create</param>
    /// <param name="userName">Current user name for audit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New charter ID</returns>
    Task<int> CreateCharterAsync(
        BargeCharterDto charter,
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing barge charter
    /// Validates date range overlaps in repository
    /// </summary>
    /// <param name="charter">Barge charter DTO with updated values</param>
    /// <param name="userName">Current user name for audit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateCharterAsync(
        BargeCharterDto charter,
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete barge charter (hard delete)
    /// </summary>
    /// <param name="charterId">Charter ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteCharterAsync(
        int charterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update barge location with optional tier/berth coordinates
    /// Special method for LocationID property with complex signature
    /// Auto-updates LocationDateTime, Status, and clears tier/berth if location changes
    /// </summary>
    /// <param name="bargeId">Barge ID</param>
    /// <param name="locationId">New location ID</param>
    /// <param name="locationDateTime">Timestamp for location update</param>
    /// <param name="tierX">Tier X coordinate (optional)</param>
    /// <param name="tierY">Tier Y coordinate (optional)</param>
    /// <param name="facilityBerthX">Facility berth X coordinate (optional)</param>
    /// <param name="facilityBerthY">Facility berth Y coordinate (optional)</param>
    /// <param name="userName">Current user name for audit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateLocationAsync(
        int bargeId,
        int locationId,
        DateTime locationDateTime,
        short? tierX = null,
        short? tierY = null,
        short? facilityBerthX = null,
        short? facilityBerthY = null,
        string? userName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if barge number already exists (for duplicate validation)
    /// Case-insensitive comparison
    /// </summary>
    /// <param name="bargeNum">Barge number to check</param>
    /// <param name="excludeBargeId">Barge ID to exclude from check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if barge number exists</returns>
    Task<bool> BargeNumExistsAsync(
        string bargeNum,
        int? excludeBargeId = null,
        CancellationToken cancellationToken = default);
}
