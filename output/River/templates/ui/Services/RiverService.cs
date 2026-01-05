using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BargeOps.Shared.Dto;
using BargeOps.Shared.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BargeOpsAdmin.Services;

/// <summary>
/// UI service implementation for River entity
/// HTTP client to call API endpoints
/// Returns DTOs from BargeOps.Shared
/// </summary>
public class RiverService : BargeOpsAdminBaseService, IRiverService
{
    private readonly ILogger<RiverService> _logger;

    public RiverService(IConfiguration configuration, string userName, ILogger<RiverService> logger)
        : base(configuration, userName)
    {
        _logger = logger;
    }

    public async Task<DataTableResponse<RiverDto>> GetRiversAsync(RiverSearchRequest request)
    {
        try
        {
            using var client = GetClient();

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/river/search", content);

            if (response.IsSuccessStatusCode)
            {
                var jsonResult = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<DataTableResponse<RiverDto>>(jsonResult);

                return result ?? new DataTableResponse<RiverDto>
                {
                    Draw = request.Draw,
                    RecordsTotal = 0,
                    RecordsFiltered = 0,
                    Data = new List<RiverDto>()
                };
            }

            _logger.LogError("Failed to get rivers: {StatusCode}", response.StatusCode);
            return new DataTableResponse<RiverDto>
            {
                Draw = request.Draw,
                RecordsTotal = 0,
                RecordsFiltered = 0,
                Data = new List<RiverDto>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rivers");
            throw;
        }
    }

    public async Task<RiverDto?> GetByIdAsync(int riverID)
    {
        try
        {
            using var client = GetClient();

            var response = await client.GetAsync($"api/river/{riverID}");

            if (response.IsSuccessStatusCode)
            {
                var jsonResult = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<RiverDto>(jsonResult);
                return result;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogError("Failed to get river {RiverID}: {StatusCode}", riverID, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting river {RiverID}", riverID);
            throw;
        }
    }

    public async Task<List<SelectListItem>> GetRiverListAsync()
    {
        try
        {
            using var client = GetClient();

            var response = await client.GetAsync("api/river/list");

            if (response.IsSuccessStatusCode)
            {
                var jsonResult = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<RiverListItemDto>>(jsonResult);

                return result?.Select(r => new SelectListItem
                {
                    Value = r.RiverID.ToString(),
                    Text = r.Code
                }).ToList() ?? new List<SelectListItem>();
            }

            _logger.LogError("Failed to get river list: {StatusCode}", response.StatusCode);
            return new List<SelectListItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting river list");
            throw;
        }
    }

    public async Task<ApiFetchResult> CreateRiverAsync(RiverDto river)
    {
        try
        {
            using var client = GetClient();

            var json = JsonConvert.SerializeObject(river);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/river", content);

            if (response.IsSuccessStatusCode)
            {
                var jsonResult = await response.Content.ReadAsStringAsync();
                var newId = JsonConvert.DeserializeObject<int>(jsonResult);

                return new ApiFetchResult
                {
                    Success = true,
                    Message = "River created successfully",
                    Data = newId
                };
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to create river: {StatusCode} - {Error}", response.StatusCode, errorContent);

            return new ApiFetchResult
            {
                Success = false,
                Message = $"Failed to create river: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating river");
            return new ApiFetchResult
            {
                Success = false,
                Message = $"Error creating river: {ex.Message}"
            };
        }
    }

    public async Task<ApiFetchResult> UpdateRiverAsync(int riverID, RiverDto river)
    {
        try
        {
            using var client = GetClient();

            var json = JsonConvert.SerializeObject(river);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"api/river/{riverID}", content);

            if (response.IsSuccessStatusCode)
            {
                return new ApiFetchResult
                {
                    Success = true,
                    Message = "River updated successfully"
                };
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to update river {RiverID}: {StatusCode} - {Error}", riverID, response.StatusCode, errorContent);

            return new ApiFetchResult
            {
                Success = false,
                Message = $"Failed to update river: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating river {RiverID}", riverID);
            return new ApiFetchResult
            {
                Success = false,
                Message = $"Error updating river: {ex.Message}"
            };
        }
    }

    public async Task<ApiFetchResult> DeleteRiverAsync(int riverID)
    {
        try
        {
            using var client = GetClient();

            var response = await client.DeleteAsync($"api/river/{riverID}");

            if (response.IsSuccessStatusCode)
            {
                return new ApiFetchResult
                {
                    Success = true,
                    Message = "River deleted successfully"
                };
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to delete river {RiverID}: {StatusCode} - {Error}", riverID, response.StatusCode, errorContent);

            return new ApiFetchResult
            {
                Success = false,
                Message = $"Failed to delete river: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting river {RiverID}", riverID);
            return new ApiFetchResult
            {
                Success = false,
                Message = $"Error deleting river: {ex.Message}"
            };
        }
    }
}
