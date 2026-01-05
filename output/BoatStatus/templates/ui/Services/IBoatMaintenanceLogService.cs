using BargeOps.Shared.Dto;

namespace BargeOpsAdmin.Services;

/// <summary>
/// UI service interface for calling BoatMaintenanceLog API endpoints
/// ⭐ Returns DTOs from BargeOps.Shared directly
/// </summary>
public interface IBoatMaintenanceLogService
{
    /// <summary>
    /// Get all maintenance logs for a boat
    /// </summary>
    Task<IEnumerable<BoatMaintenanceLogDto>> GetByBoatIdAsync(int boatId);

    /// <summary>
    /// Get a single maintenance log by ID
    /// </summary>
    Task<BoatMaintenanceLogDto?> GetByIdAsync(int id);

    /// <summary>
    /// Create a new maintenance log
    /// </summary>
    Task<int> CreateAsync(BoatMaintenanceLogDto log);

    /// <summary>
    /// Update an existing maintenance log
    /// </summary>
    Task UpdateAsync(BoatMaintenanceLogDto log);

    /// <summary>
    /// Delete a maintenance log
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Search maintenance logs with criteria
    /// </summary>
    Task<IEnumerable<BoatMaintenanceLogDto>> SearchAsync(BoatMaintenanceLogSearchRequest request);
}
