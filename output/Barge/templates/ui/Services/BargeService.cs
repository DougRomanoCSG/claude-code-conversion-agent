using BargeOps.Shared.Dto;
using System.Net.Http.Json;
using System.Text.Json;

namespace BargeOpsAdmin.Services;

/// <summary>
/// Service for Barge operations in UI
/// HTTP client to call BargeOps.Admin.API endpoints
/// ⭐ Returns DTOs from BargeOps.Shared directly
/// </summary>
public class BargeService : IBargeService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BargeService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public BargeService(
        HttpClient httpClient,
        ILogger<BargeService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    #region Search and Get

    public async Task<PagedResult<BargeDto>?> SearchAsync(BargeSearchRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/barge/search", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PagedResult<BargeDto>>(_jsonOptions);
            }

            _logger.LogWarning("Search barges failed with status code {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching barges");
            return null;
        }
    }

    public async Task<BargeDto?> GetByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/barge/{id}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<BargeDto>(_jsonOptions);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogWarning("Get barge {BargeId} failed with status code {StatusCode}", id, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting barge {BargeId}", id);
            return null;
        }
    }

    #endregion

    #region Create, Update, Delete

    public async Task<int?> CreateAsync(BargeDto barge)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/barge", barge);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<int>(_jsonOptions);
                return result;
            }

            _logger.LogWarning("Create barge failed with status code {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating barge");
            return null;
        }
    }

    public async Task<bool> UpdateAsync(BargeDto barge)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/barge/{barge.BargeID}", barge);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            _logger.LogWarning("Update barge {BargeId} failed with status code {StatusCode}", barge.BargeID, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating barge {BargeId}", barge.BargeID);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/barge/{id}");

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            _logger.LogWarning("Delete barge {BargeId} failed with status code {StatusCode}", id, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting barge {BargeId}", id);
            return false;
        }
    }

    #endregion

    #region Barge Charters

    public async Task<List<BargeCharterDto>?> GetChartersAsync(int bargeId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/barge/{bargeId}/charters");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<BargeCharterDto>>(_jsonOptions);
            }

            _logger.LogWarning("Get charters for barge {BargeId} failed with status code {StatusCode}", bargeId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting charters for barge {BargeId}", bargeId);
            return null;
        }
    }

    public async Task<int?> CreateCharterAsync(BargeCharterDto charter)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/barge/{charter.BargeID}/charters", charter);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<int>(_jsonOptions);
                return result;
            }

            _logger.LogWarning("Create charter for barge {BargeId} failed with status code {StatusCode}", charter.BargeID, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating charter for barge {BargeId}", charter.BargeID);
            return null;
        }
    }

    public async Task<bool> UpdateCharterAsync(BargeCharterDto charter)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/barge/{charter.BargeID}/charters/{charter.BargeCharterID}", charter);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            _logger.LogWarning("Update charter {CharterId} for barge {BargeId} failed with status code {StatusCode}",
                charter.BargeCharterID, charter.BargeID, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating charter {CharterId} for barge {BargeId}", charter.BargeCharterID, charter.BargeID);
            return false;
        }
    }

    public async Task<bool> DeleteCharterAsync(int bargeId, int charterId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/barge/{bargeId}/charters/{charterId}");

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            _logger.LogWarning("Delete charter {CharterId} for barge {BargeId} failed with status code {StatusCode}",
                charterId, bargeId, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting charter {CharterId} for barge {BargeId}", charterId, bargeId);
            return false;
        }
    }

    #endregion

    #region Update Location

    public async Task<bool> UpdateLocationAsync(
        int bargeId,
        int locationId,
        DateTime locationDateTime,
        short? tierX = null,
        short? tierY = null,
        short? facilityBerthX = null,
        short? facilityBerthY = null)
    {
        try
        {
            var request = new
            {
                LocationId = locationId,
                LocationDateTime = locationDateTime,
                TierX = tierX,
                TierY = tierY,
                FacilityBerthX = facilityBerthX,
                FacilityBerthY = facilityBerthY
            };

            var response = await _httpClient.PutAsJsonAsync($"api/barge/{bargeId}/location", request);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            _logger.LogWarning("Update location for barge {BargeId} failed with status code {StatusCode}",
                bargeId, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating location for barge {BargeId}", bargeId);
            return false;
        }
    }

    #endregion
}
