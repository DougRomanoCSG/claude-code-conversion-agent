using BargeOps.Shared.Dto;

namespace BargeOpsAdmin.Services;

/// <summary>
/// API client service interface for Facility operations
/// Returns DTOs from Shared project (same DTOs used by API)
/// </summary>
public interface IFacilityService
{
    Task<DataTableResponse<FacilityDto>> SearchAsync(FacilitySearchRequest request);
    Task<FacilityDto?> GetByIdAsync(int id);
    Task<FacilityDto> CreateAsync(FacilityDto facility);
    Task<FacilityDto> UpdateAsync(FacilityDto facility);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<FacilityBerthDto>> GetBerthsAsync(int facilityId);
    Task<IEnumerable<FacilityStatusDto>> GetStatusesAsync(int facilityId);
    Task<FacilityBerthDto> CreateBerthAsync(FacilityBerthDto berth);
    Task<FacilityBerthDto> UpdateBerthAsync(FacilityBerthDto berth);
    Task<bool> DeleteBerthAsync(int berthId);
    Task<FacilityStatusDto> CreateStatusAsync(FacilityStatusDto status);
    Task<FacilityStatusDto> UpdateStatusAsync(FacilityStatusDto status);
    Task<bool> DeleteStatusAsync(int statusId);
}
