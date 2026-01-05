using BargeOps.Shared.Dto;

namespace Admin.Infrastructure.Abstractions;

/// <summary>
/// Repository interface for BoatMaintenanceLog data access
/// ⭐ Returns DTOs directly (no mapping needed in MONO SHARED architecture)
/// </summary>
public interface IBoatMaintenanceLogRepository
{
    /// <summary>
    /// Get a single BoatMaintenanceLog by ID with related data (PortFacility, BoatRole)
    /// </summary>
    Task<BoatMaintenanceLogDto?> GetByIdAsync(int boatMaintenanceLogId);

    /// <summary>
    /// Get all BoatMaintenanceLog records for a specific boat (LocationID)
    /// </summary>
    Task<IEnumerable<BoatMaintenanceLogDto>> GetByLocationIdAsync(int locationId);

    /// <summary>
    /// Get the latest BoatMaintenanceLog of a specific type for a boat
    /// Used to populate parent BoatLocation current status fields
    /// </summary>
    Task<BoatMaintenanceLogDto?> GetLatestByTypeAsync(int locationId, string maintenanceType);

    /// <summary>
    /// Create a new BoatMaintenanceLog record
    /// ⭐ Note: Legacy SP deletes existing DeckLogActivity-created status with same StartDateTime
    /// </summary>
    Task<int> CreateAsync(BoatMaintenanceLogDto log);

    /// <summary>
    /// Update an existing BoatMaintenanceLog record
    /// ⭐ Includes optimistic concurrency check using ModifyDateTime
    /// </summary>
    Task UpdateAsync(BoatMaintenanceLogDto log);

    /// <summary>
    /// Delete a BoatMaintenanceLog record
    /// ⭐ Hard delete (not soft delete) with UnitTowTripDownTime cleanup
    /// </summary>
    Task DeleteAsync(int boatMaintenanceLogId);

    /// <summary>
    /// Search BoatMaintenanceLog records with criteria
    /// </summary>
    Task<IEnumerable<BoatMaintenanceLogDto>> SearchAsync(BoatMaintenanceLogSearchRequest request);
}
