using BargeOps.Shared.Dto;
using System.Net.Http.Json;
using System.Text.Json;

namespace BargeOpsAdmin.Services;

/// <summary>
/// HTTP client service for Facility API operations
/// Returns DTOs from Shared project (no mapping needed!)
/// </summary>
public class FacilityService : IFacilityService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FacilityService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public FacilityService(HttpClient httpClient, ILogger<FacilityService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<DataTableResponse<FacilityDto>> SearchAsync(FacilitySearchRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/Facility/search", request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<DataTableResponse<FacilityDto>>(_jsonOptions)
                ?? new DataTableResponse<FacilityDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching facilities");
            throw;
        }
    }

    public async Task<FacilityDto?> GetByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/Facility/{id}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<FacilityDto>(_jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting facility {FacilityId}", id);
            throw;
        }
    }

    public async Task<FacilityDto> CreateAsync(FacilityDto facility)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/Facility", facility);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<FacilityDto>(_jsonOptions)
                ?? throw new InvalidOperationException("Failed to create facility");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating facility");
            throw;
        }
    }

    public async Task<FacilityDto> UpdateAsync(FacilityDto facility)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/Facility/{facility.LocationID}", facility);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<FacilityDto>(_jsonOptions)
                ?? throw new InvalidOperationException("Failed to update facility");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating facility {FacilityId}", facility.LocationID);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/Facility/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting facility {FacilityId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<FacilityBerthDto>> GetBerthsAsync(int facilityId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/Facility/{facilityId}/berths");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<IEnumerable<FacilityBerthDto>>(_jsonOptions)
                ?? Enumerable.Empty<FacilityBerthDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting berths for facility {FacilityId}", facilityId);
            throw;
        }
    }

    public async Task<IEnumerable<FacilityStatusDto>> GetStatusesAsync(int facilityId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/Facility/{facilityId}/statuses");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<IEnumerable<FacilityStatusDto>>(_jsonOptions)
                ?? Enumerable.Empty<FacilityStatusDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statuses for facility {FacilityId}", facilityId);
            throw;
        }
    }

    public async Task<FacilityBerthDto> CreateBerthAsync(FacilityBerthDto berth)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/Facility/berths", berth);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<FacilityBerthDto>(_jsonOptions)
                ?? throw new InvalidOperationException("Failed to create berth");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating berth");
            throw;
        }
    }

    public async Task<FacilityBerthDto> UpdateBerthAsync(FacilityBerthDto berth)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/Facility/berths/{berth.FacilityBerthID}", berth);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<FacilityBerthDto>(_jsonOptions)
                ?? throw new InvalidOperationException("Failed to update berth");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating berth {BerthId}", berth.FacilityBerthID);
            throw;
        }
    }

    public async Task<bool> DeleteBerthAsync(int berthId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/Facility/berths/{berthId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting berth {BerthId}", berthId);
            throw;
        }
    }

    public async Task<FacilityStatusDto> CreateStatusAsync(FacilityStatusDto status)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/Facility/statuses", status);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<FacilityStatusDto>(_jsonOptions)
                ?? throw new InvalidOperationException("Failed to create status");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating status");
            throw;
        }
    }

    public async Task<FacilityStatusDto> UpdateStatusAsync(FacilityStatusDto status)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/Facility/statuses/{status.FacilityStatusID}", status);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<FacilityStatusDto>(_jsonOptions)
                ?? throw new InvalidOperationException("Failed to update status");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status {StatusId}", status.FacilityStatusID);
            throw;
        }
    }

    public async Task<bool> DeleteStatusAsync(int statusId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/Facility/statuses/{statusId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting status {StatusId}", statusId);
            throw;
        }
    }
}
