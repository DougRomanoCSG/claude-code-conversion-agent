using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BargeOps.Shared.Dto;

namespace Admin.Domain.Services
{
    /// <summary>
    /// Service interface for BoatFuelPrice business logic
    /// Uses DTOs directly from BargeOps.Shared
    /// </summary>
    public interface IBoatFuelPriceService
    {
        /// <summary>
        /// Search boat fuel prices by criteria
        /// </summary>
        Task<IEnumerable<BoatFuelPriceDto>> SearchAsync(BoatFuelPriceSearchRequest criteria);

        /// <summary>
        /// Get a single boat fuel price by ID
        /// </summary>
        Task<BoatFuelPriceDto> GetByIdAsync(int boatFuelPriceID);

        /// <summary>
        /// Create a new boat fuel price
        /// Validates business rules before creation
        /// </summary>
        Task<BoatFuelPriceDto> CreateAsync(BoatFuelPriceDto dto, string userName);

        /// <summary>
        /// Update an existing boat fuel price
        /// Validates business rules before update
        /// </summary>
        Task<BoatFuelPriceDto> UpdateAsync(BoatFuelPriceDto dto, string userName);

        /// <summary>
        /// Delete a boat fuel price
        /// </summary>
        Task<bool> DeleteAsync(int boatFuelPriceID);
    }
}
