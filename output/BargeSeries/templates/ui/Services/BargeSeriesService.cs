using System.Net.Http.Json;
using BargeOps.Shared.Dto;

namespace BargeOpsAdmin.Services;

/// <summary>
/// UI service implementation for BargeSeries API client.
/// Uses HttpClient to call the BargeSeries API endpoints.
/// Returns DTOs from BargeOps.Shared - NO mapping needed!
/// </summary>
public class BargeSeriesService : IBargeSeriesService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BargeSeriesService> _logger;
    private const string ApiBaseUrl = "/api/bargeseries";

    public BargeSeriesService(
        HttpClient httpClient,
        ILogger<BargeSeriesService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<PagedResult<BargeSeriesDto>> SearchAsync(BargeSeriesSearchRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/search", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PagedResult<BargeSeriesDto>>();
            return result ?? new PagedResult<BargeSeriesDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching barge series");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BargeSeriesDto>> GetListAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<IEnumerable<BargeSeriesDto>>($"{ApiBaseUrl}/list");
            return result ?? Enumerable.Empty<BargeSeriesDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting barge series list");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<BargeSeriesDto?> GetByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/{id}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<BargeSeriesDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting barge series {BargeSeriesId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<BargeSeriesDto> CreateAsync(BargeSeriesDto bargeSeries)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(ApiBaseUrl, bargeSeries);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<BargeSeriesDto>();
            return result ?? throw new InvalidOperationException("Failed to create barge series.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating barge series");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<BargeSeriesDto> UpdateAsync(BargeSeriesDto bargeSeries)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{ApiBaseUrl}/{bargeSeries.BargeSeriesID}", bargeSeries);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<BargeSeriesDto>();
            return result ?? throw new InvalidOperationException("Failed to update barge series.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating barge series {BargeSeriesId}", bargeSeries.BargeSeriesID);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{ApiBaseUrl}/{id}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return false;

            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting barge series {BargeSeriesId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BargeSeriesDraftDto>> UpdateDraftsAsync(
        int bargeSeriesId,
        IEnumerable<BargeSeriesDraftDto> drafts)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/{bargeSeriesId}/drafts", drafts);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<IEnumerable<BargeSeriesDraftDto>>();
            return result ?? Enumerable.Empty<BargeSeriesDraftDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating drafts for barge series {BargeSeriesId}", bargeSeriesId);
            throw;
        }
    }
}
