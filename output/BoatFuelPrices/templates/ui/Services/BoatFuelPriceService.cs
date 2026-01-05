using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BargeOps.Shared.Dto;

namespace BargeOpsAdmin.Services
{
    /// <summary>
    /// UI Service for calling BoatFuelPrice API endpoints
    /// HTTP client implementation
    /// </summary>
    public class BoatFuelPriceService : IBoatFuelPriceService
    {
        private readonly HttpClient _httpClient;

        public BoatFuelPriceService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<IEnumerable<BoatFuelPriceDto>> SearchAsync(BoatFuelPriceSearchRequest request)
        {
            var queryString = $"?effectiveDate={request?.EffectiveDate:yyyy-MM-dd}&fuelVendorBusinessUnitID={request?.FuelVendorBusinessUnitID}";
            var response = await _httpClient.GetAsync($"api/boatfuelprice{queryString}");
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<IEnumerable<BoatFuelPriceDto>>();
        }

        public async Task<BoatFuelPriceDto> GetByIdAsync(int boatFuelPriceID)
        {
            var response = await _httpClient.GetAsync($"api/boatfuelprice/{boatFuelPriceID}");
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<BoatFuelPriceDto>();
        }

        public async Task<BoatFuelPriceDto> CreateAsync(BoatFuelPriceDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/boatfuelprice", dto);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<BoatFuelPriceDto>();
        }

        public async Task<BoatFuelPriceDto> UpdateAsync(BoatFuelPriceDto dto)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/boatfuelprice/{dto.BoatFuelPriceID}", dto);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<BoatFuelPriceDto>();
        }

        public async Task<bool> DeleteAsync(int boatFuelPriceID)
        {
            var response = await _httpClient.DeleteAsync($"api/boatfuelprice/{boatFuelPriceID}");
            return response.IsSuccessStatusCode;
        }
    }
}
