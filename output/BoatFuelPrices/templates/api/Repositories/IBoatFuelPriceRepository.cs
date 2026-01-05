using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BargeOps.Shared.Dto;

namespace Admin.Infrastructure.Repositories
{
    /// <summary>
    /// Repository interface for BoatFuelPrice data access
    /// Returns DTOs directly - no mapping needed!
    /// </summary>
    public interface IBoatFuelPriceRepository
    {
        /// <summary>
        /// Search boat fuel prices by criteria
        /// </summary>
        /// <param name="criteria">Search criteria including effective date and vendor filters</param>
        /// <returns>List of boat fuel prices matching criteria</returns>
        Task<IEnumerable<BoatFuelPriceDto>> SearchAsync(BoatFuelPriceSearchRequest criteria);

        /// <summary>
        /// Get a single boat fuel price by ID
        /// </summary>
        /// <param name="boatFuelPriceID">Primary key</param>
        /// <returns>Boat fuel price DTO or null if not found</returns>
        Task<BoatFuelPriceDto> GetByIdAsync(int boatFuelPriceID);

        /// <summary>
        /// Create a new boat fuel price record
        /// </summary>
        /// <param name="dto">Boat fuel price data</param>
        /// <param name="userName">Current user name for audit fields</param>
        /// <returns>Newly created boat fuel price ID</returns>
        Task<int> CreateAsync(BoatFuelPriceDto dto, string userName);

        /// <summary>
        /// Update an existing boat fuel price record
        /// </summary>
        /// <param name="dto">Boat fuel price data</param>
        /// <param name="userName">Current user name for audit fields</param>
        /// <returns>True if updated successfully, false if not found</returns>
        Task<bool> UpdateAsync(BoatFuelPriceDto dto, string userName);

        /// <summary>
        /// Delete a boat fuel price record
        /// </summary>
        /// <param name="boatFuelPriceID">Primary key</param>
        /// <returns>True if deleted successfully, false if not found</returns>
        Task<bool> DeleteAsync(int boatFuelPriceID);

        /// <summary>
        /// Check if a boat fuel price already exists for the given date and vendor
        /// Used for unique constraint validation (EffectiveDate + FuelVendor must be unique)
        /// </summary>
        /// <param name="effectiveDate">Effective date</param>
        /// <param name="fuelVendorBusinessUnitID">Vendor business unit ID (can be null)</param>
        /// <param name="excludeBoatFuelPriceID">Boat fuel price ID to exclude from check (for updates)</param>
        /// <returns>True if unique (no duplicate exists), false if duplicate found</returns>
        Task<bool> IsUniqueAsync(DateTime effectiveDate, int? fuelVendorBusinessUnitID, int? excludeBoatFuelPriceID = null);
    }
}
