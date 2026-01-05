using BargeOps.Shared.Dto;

namespace Admin.Domain.Services;

/// <summary>
/// Service interface for Barge business logic
/// Orchestrates repository calls and applies business rules
/// </summary>
public interface IBargeService
{
    /// <summary>
    /// Search barges with paging and filtering
    /// Applies business rules before calling repository
    /// </summary>
    /// <param name="request">Search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result set</returns>
    Task<PagedResult<BargeDto>> SearchAsync(
        BargeSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get barge by ID
    /// Calculates computed fields (DraftFeet/Inches, SizeCategory)
    /// </summary>
    /// <param name="bargeId">Barge ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Barge DTO or null if not found</returns>
    Task<BargeDto?> GetByIdAsync(
        int bargeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create new barge
    /// Validates business rules and calculates computed fields
    /// </summary>
    /// <param name="barge">Barge to create</param>
    /// <param name="userName">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with new barge ID or validation errors</returns>
    Task<ServiceResult<int>> CreateAsync(
        BargeDto barge,
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing barge
    /// Validates business rules and calculates computed fields
    /// Clears unused fields based on EquipmentType
    /// </summary>
    /// <param name="barge">Barge with updated values</param>
    /// <param name="userName">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with success indicator or validation errors</returns>
    Task<ServiceResult<bool>> UpdateAsync(
        BargeDto barge,
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete barge (soft delete)
    /// Validates barge can be deleted
    /// </summary>
    /// <param name="bargeId">Barge ID to delete</param>
    /// <param name="userName">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with success indicator or validation errors</returns>
    Task<ServiceResult<bool>> DeleteAsync(
        int bargeId,
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get barge charters for a barge
    /// </summary>
    /// <param name="bargeId">Barge ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of charters</returns>
    Task<List<BargeCharterDto>> GetBargeChartersAsync(
        int bargeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create barge charter
    /// Validates date range overlaps
    /// </summary>
    /// <param name="charter">Charter to create</param>
    /// <param name="userName">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with new charter ID or validation errors</returns>
    Task<ServiceResult<int>> CreateCharterAsync(
        BargeCharterDto charter,
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update barge charter
    /// Validates date range overlaps
    /// </summary>
    /// <param name="charter">Charter with updated values</param>
    /// <param name="userName">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with success indicator or validation errors</returns>
    Task<ServiceResult<bool>> UpdateCharterAsync(
        BargeCharterDto charter,
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete barge charter
    /// </summary>
    /// <param name="charterId">Charter ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with success indicator or validation errors</returns>
    Task<ServiceResult<bool>> DeleteCharterAsync(
        int charterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update barge location with optional tier/berth coordinates
    /// Calculates and updates Status field
    /// </summary>
    /// <param name="bargeId">Barge ID</param>
    /// <param name="locationId">New location ID</param>
    /// <param name="locationDateTime">Location timestamp</param>
    /// <param name="tierX">Tier X coordinate (optional)</param>
    /// <param name="tierY">Tier Y coordinate (optional)</param>
    /// <param name="facilityBerthX">Facility berth X coordinate (optional)</param>
    /// <param name="facilityBerthY">Facility berth Y coordinate (optional)</param>
    /// <param name="userName">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with success indicator or validation errors</returns>
    Task<ServiceResult<bool>> UpdateLocationAsync(
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
    /// Validate barge business rules
    /// Equipment type logic, cover type special logic, conditional requirements
    /// </summary>
    /// <param name="barge">Barge to validate</param>
    /// <returns>Validation result with errors</returns>
    Task<ValidationResult> ValidateAsync(BargeDto barge);
}
