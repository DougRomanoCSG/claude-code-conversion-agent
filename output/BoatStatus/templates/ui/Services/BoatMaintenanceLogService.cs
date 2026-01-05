using BargeOps.Shared.Dto;
using System.Net.Http.Json;

namespace BargeOpsAdmin.Services;

/// <summary>
/// UI service implementation for calling BoatMaintenanceLog API endpoints
/// ⭐ HTTP client service that returns DTOs from BargeOps.Shared
/// </summary>
public class BoatMaintenanceLogService : IBoatMaintenanceLogService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BoatMaintenanceLogService> _logger;
    private const string BaseUrl = "api/boat-maintenance-log";

    public BoatMaintenanceLogService(
        HttpClient httpClient,
        ILogger<BoatMaintenanceLogService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<BoatMaintenanceLogDto>> GetByBoatIdAsync(int boatId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/boat/{boatId}");
            response.EnsureSuccessStatusCode();

            var logs = await response.Content.ReadFromJsonAsync<IEnumerable<BoatMaintenanceLogDto>>();
            return logs ?? new List<BoatMaintenanceLogDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting maintenance logs for boat {BoatId}", boatId);
            throw;
        }
    }

    public async Task<BoatMaintenanceLogDto?> GetByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<BoatMaintenanceLogDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting maintenance log {Id}", id);
            throw;
        }
    }

    public async Task<int> CreateAsync(BoatMaintenanceLogDto log)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(BaseUrl, log);
            response.EnsureSuccessStatusCode();

            var newId = await response.Content.ReadFromJsonAsync<int>();
            return newId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating maintenance log");
            throw;
        }
    }

    public async Task UpdateAsync(BoatMaintenanceLogDto log)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/{log.BoatMaintenanceLogID}", log);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating maintenance log {Id}", log.BoatMaintenanceLogID);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/{id}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting maintenance log {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<BoatMaintenanceLogDto>> SearchAsync(BoatMaintenanceLogSearchRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/search", request);
            response.EnsureSuccessStatusCode();

            var logs = await response.Content.ReadFromJsonAsync<IEnumerable<BoatMaintenanceLogDto>>();
            return logs ?? new List<BoatMaintenanceLogDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching maintenance logs");
            throw;
        }
    }
}
