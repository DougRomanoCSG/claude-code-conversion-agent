using BargeOps.Shared.Dto;

namespace BargeOpsAdmin.Services;

/// <summary>
/// Service interface for Barge operations in UI
/// HTTP client to call BargeOps.Admin.API endpoints
/// Returns DTOs from BargeOps.Shared
/// </summary>
public interface IBargeService
{
    /// <summary>
    /// Search barges with paging and filtering
    /// </summary>
    /// <param name="request">Search criteria</param>
    /// <returns>Paged result set</returns>
    Task<PagedResult<BargeDto>?> SearchAsync(BargeSearchRequest request);

    /// <summary>
    /// Get barge by ID
    /// </summary>
    /// <param name="id">Barge ID</param>
    /// <returns>Barge DTO or null if not found</returns>
    Task<BargeDto?> GetByIdAsync(int id);

    /// <summary>
    /// Create new barge
    /// </summary>
    /// <param name="barge">Barge to create</param>
    /// <returns>New barge ID or null if failed</returns>
    Task<int?> CreateAsync(BargeDto barge);

    /// <summary>
    /// Update existing barge
    /// </summary>
    /// <param name="barge">Barge with updated values</param>
    /// <returns>True if successful</returns>
    Task<bool> UpdateAsync(BargeDto barge);

    /// <summary>
    /// Delete barge (soft delete)
    /// </summary>
    /// <param name="id">Barge ID to delete</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Get barge charters for a barge
    /// </summary>
    /// <param name="bargeId">Barge ID</param>
    /// <returns>List of charters</returns>
    Task<List<BargeCharterDto>?> GetChartersAsync(int bargeId);

    /// <summary>
    /// Create barge charter
    /// </summary>
    /// <param name="charter">Charter to create</param>
    /// <returns>New charter ID or null if failed</returns>
    Task<int?> CreateCharterAsync(BargeCharterDto charter);

    /// <summary>
    /// Update barge charter
    /// </summary>
    /// <param name="charter">Charter with updated values</param>
    /// <returns>True if successful</returns>
    Task<bool> UpdateCharterAsync(BargeCharterDto charter);

    /// <summary>
    /// Delete barge charter
    /// </summary>
    /// <param name="bargeId">Barge ID</param>
    /// <param name="charterId">Charter ID to delete</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteCharterAsync(int bargeId, int charterId);

    /// <summary>
    /// Update barge location with optional tier/berth coordinates
    /// </summary>
    /// <param name="bargeId">Barge ID</param>
    /// <param name="locationId">New location ID</param>
    /// <param name="locationDateTime">Location timestamp</param>
    /// <param name="tierX">Tier X coordinate (optional)</param>
    /// <param name="tierY">Tier Y coordinate (optional)</param>
    /// <param name="facilityBerthX">Facility berth X coordinate (optional)</param>
    /// <param name="facilityBerthY">Facility berth Y coordinate (optional)</param>
    /// <returns>True if successful</returns>
    Task<bool> UpdateLocationAsync(
        int bargeId,
        int locationId,
        DateTime locationDateTime,
        short? tierX = null,
        short? tierY = null,
        short? facilityBerthX = null,
        short? facilityBerthY = null);
}
