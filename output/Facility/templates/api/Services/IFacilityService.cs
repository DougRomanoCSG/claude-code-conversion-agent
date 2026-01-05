using BargeOps.Shared.Dto;

namespace BargeOps.Admin.Domain.Services;

/// <summary>
/// Service interface for Facility business logic
/// Works with DTOs from Shared project (no mapping needed)
/// </summary>
public interface IFacilityService
{
    Task<PagedResult<FacilityDto>> SearchAsync(FacilitySearchRequest request, CancellationToken cancellationToken = default);
    Task<FacilityDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<FacilityDto> CreateAsync(FacilityDto facility, CancellationToken cancellationToken = default);
    Task<FacilityDto> UpdateAsync(FacilityDto facility, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<FacilityBerthDto>> GetBerthsAsync(int facilityId, CancellationToken cancellationToken = default);
    Task<IEnumerable<FacilityStatusDto>> GetStatusesAsync(int facilityId, CancellationToken cancellationToken = default);
    Task<FacilityBerthDto> CreateBerthAsync(FacilityBerthDto berth, CancellationToken cancellationToken = default);
    Task<FacilityBerthDto> UpdateBerthAsync(FacilityBerthDto berth, CancellationToken cancellationToken = default);
    Task<bool> DeleteBerthAsync(int berthId, CancellationToken cancellationToken = default);
    Task<FacilityStatusDto> CreateStatusAsync(FacilityStatusDto status, CancellationToken cancellationToken = default);
    Task<FacilityStatusDto> UpdateStatusAsync(FacilityStatusDto status, CancellationToken cancellationToken = default);
    Task<bool> DeleteStatusAsync(int statusId, CancellationToken cancellationToken = default);
}
