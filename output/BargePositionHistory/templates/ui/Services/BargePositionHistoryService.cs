using BargeOps.Shared.Dto;
using Csg.ListQuery;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BargeOpsAdmin.Services;

/// <summary>
/// Service implementation for Barge Position History API client.
/// Target: C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Services\BargePositionHistoryService.cs
///
/// Uses HttpClient to call BargeOps.Admin.API endpoints.
/// Returns DTOs from BargeOps.Shared directly (no mapping needed).
/// </summary>
public class BargePositionHistoryService : IBargePositionHistoryService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "api/BargePositionHistory";

    public BargePositionHistoryService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DataTableResponse<BargePositionHistoryDto>> SearchAsync(BargePositionHistorySearchRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/search", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DataTableResponse<BargePositionHistoryDto>>();
    }

    public async Task<BargePositionHistoryDto> GetByIdAsync(int id)
    {
        var response = await _httpClient.GetAsync($"{BaseUrl}/{id}");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BargePositionHistoryDto>();
    }

    public async Task<int> CreateAsync(BargePositionHistoryDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync(BaseUrl, dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<int>();
    }

    public async Task UpdateAsync(BargePositionHistoryDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/{dto.FleetPositionHistoryID}", dto);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"{BaseUrl}/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> ValidateBargeNumAsync(string bargeNum)
    {
        if (string.IsNullOrWhiteSpace(bargeNum))
        {
            return false;
        }

        var response = await _httpClient.GetAsync($"{BaseUrl}/validate-barge?bargeNum={Uri.EscapeDataString(bargeNum)}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }
}
