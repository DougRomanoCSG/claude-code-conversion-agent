using System.Collections.Generic;
using System.Threading.Tasks;
using BargeOps.Shared.Dto;

namespace BargeOpsAdmin.Services
{
    /// <summary>
    /// UI Service interface for calling BoatFuelPrice API endpoints
    /// HTTP client that communicates with the API
    /// </summary>
    public interface IBoatFuelPriceService
    {
        /// <summary>
        /// Search boat fuel prices by criteria
        /// </summary>
        Task<IEnumerable<BoatFuelPriceDto>> SearchAsync(BoatFuelPriceSearchRequest request);

        /// <summary>
        /// Get a single boat fuel price by ID
        /// </summary>
        Task<BoatFuelPriceDto> GetByIdAsync(int boatFuelPriceID);

        /// <summary>
        /// Create a new boat fuel price
        /// </summary>
        Task<BoatFuelPriceDto> CreateAsync(BoatFuelPriceDto dto);

        /// <summary>
        /// Update an existing boat fuel price
        /// </summary>
        Task<BoatFuelPriceDto> UpdateAsync(BoatFuelPriceDto dto);

        /// <summary>
        /// Delete a boat fuel price
        /// </summary>
        Task<bool> DeleteAsync(int boatFuelPriceID);
    }
}
