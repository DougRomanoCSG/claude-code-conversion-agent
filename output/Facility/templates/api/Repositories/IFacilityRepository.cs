using BargeOps.Shared.Dto;

namespace BargeOps.Admin.Infrastructure.Repositories;

/// <summary>
/// Repository interface for Facility operations
/// Returns DTOs directly (no separate domain models)
/// </summary>
public interface IFacilityRepository
{
    /// <summary>
    /// Search facilities with DataTables server-side processing
    /// </summary>
    Task<PagedResult<FacilityDto>> SearchAsync(FacilitySearchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get facility by ID with all properties
    /// </summary>
    Task<FacilityDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create new facility
    /// </summary>
    Task<FacilityDto> CreateAsync(FacilityDto facility, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing facility
    /// </summary>
    Task<FacilityDto> UpdateAsync(FacilityDto facility, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete facility by ID
    /// </summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get facility berths
    /// </summary>
    Task<IEnumerable<FacilityBerthDto>> GetBerthsAsync(int facilityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get facility statuses
    /// </summary>
    Task<IEnumerable<FacilityStatusDto>> GetStatusesAsync(int facilityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create facility berth
    /// </summary>
    Task<FacilityBerthDto> CreateBerthAsync(FacilityBerthDto berth, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update facility berth
    /// </summary>
    Task<FacilityBerthDto> UpdateBerthAsync(FacilityBerthDto berth, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete facility berth
    /// </summary>
    Task<bool> DeleteBerthAsync(int berthId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create facility status
    /// </summary>
    Task<FacilityStatusDto> CreateStatusAsync(FacilityStatusDto status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update facility status
    /// </summary>
    Task<FacilityStatusDto> UpdateStatusAsync(FacilityStatusDto status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete facility status
    /// </summary>
    Task<bool> DeleteStatusAsync(int statusId, CancellationToken cancellationToken = default);
}
