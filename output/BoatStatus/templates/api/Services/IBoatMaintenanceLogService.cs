using BargeOps.Shared.Dto;

namespace Admin.Domain.Services;

/// <summary>
/// Service interface for BoatMaintenanceLog business logic
/// ⭐ Uses DTOs directly (no mapping needed in MONO SHARED architecture)
/// </summary>
public interface IBoatMaintenanceLogService
{
    /// <summary>
    /// Get a single BoatMaintenanceLog by ID
    /// </summary>
    Task<BoatMaintenanceLogDto?> GetByIdAsync(int boatMaintenanceLogId);

    /// <summary>
    /// Get all BoatMaintenanceLog records for a specific boat
    /// </summary>
    Task<IEnumerable<BoatMaintenanceLogDto>> GetByLocationIdAsync(int locationId);

    /// <summary>
    /// Create a new BoatMaintenanceLog record
    /// ⭐ Validates conditional business rules before creating
    /// ⭐ Clears unused fields based on MaintenanceType
    /// </summary>
    Task<int> CreateAsync(BoatMaintenanceLogDto log, string currentUser);

    /// <summary>
    /// Update an existing BoatMaintenanceLog record
    /// ⭐ Validates conditional business rules before updating
    /// ⭐ Clears unused fields based on MaintenanceType
    /// ⭐ Validates that MaintenanceType cannot be changed
    /// </summary>
    Task UpdateAsync(BoatMaintenanceLogDto log, string currentUser);

    /// <summary>
    /// Delete a BoatMaintenanceLog record
    /// </summary>
    Task DeleteAsync(int boatMaintenanceLogId);

    /// <summary>
    /// Search BoatMaintenanceLog records with criteria
    /// </summary>
    Task<IEnumerable<BoatMaintenanceLogDto>> SearchAsync(BoatMaintenanceLogSearchRequest request);

    /// <summary>
    /// Validate BoatMaintenanceLog business rules
    /// ⭐ Conditional validation based on MaintenanceType
    /// </summary>
    Task<ValidationResult> ValidateAsync(BoatMaintenanceLogDto log, bool isUpdate = false);
}
