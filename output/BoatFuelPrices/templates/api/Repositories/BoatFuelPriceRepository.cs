using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BargeOps.Shared.Dto;
using Admin.Infrastructure.Abstractions;
using Dapper;

namespace Admin.Infrastructure.Repositories
{
    /// <summary>
    /// Repository for BoatFuelPrice data access using Dapper
    /// Returns DTOs directly - no mapping layer needed!
    /// Uses parameterized SQL queries (NOT stored procedures)
    /// </summary>
    public class BoatFuelPriceRepository : IBoatFuelPriceRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public BoatFuelPriceRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        /// <summary>
        /// Search boat fuel prices by criteria with LEFT JOIN to get vendor name
        /// </summary>
        public async Task<IEnumerable<BoatFuelPriceDto>> SearchAsync(BoatFuelPriceSearchRequest criteria)
        {
            using var connection = _connectionFactory.CreateConnection();

            var sql = @"
                SELECT
                    bfp.BoatFuelPriceID,
                    bfp.EffectiveDate,
                    bfp.Price,
                    bfp.FuelVendorBusinessUnitID,
                    bu.Name AS FuelVendor,
                    bfp.InvoiceNumber,
                    bfp.CreateDateTime,
                    bfp.CreateUser,
                    bfp.ModifyDateTime,
                    bfp.ModifyUser
                FROM BoatFuelPrice bfp
                LEFT JOIN BusinessUnit bu ON bfp.FuelVendorBusinessUnitID = bu.BusinessUnitID
                WHERE (@EffectiveDate IS NULL OR bfp.EffectiveDate = @EffectiveDate)
                  AND (@FuelVendorBusinessUnitID IS NULL OR bfp.FuelVendorBusinessUnitID = @FuelVendorBusinessUnitID)
                ORDER BY bfp.EffectiveDate DESC, bu.Name ASC";

            var parameters = new
            {
                EffectiveDate = criteria?.EffectiveDate,
                FuelVendorBusinessUnitID = criteria?.FuelVendorBusinessUnitID
            };

            return await connection.QueryAsync<BoatFuelPriceDto>(sql, parameters);
        }

        /// <summary>
        /// Get a single boat fuel price by ID with vendor name
        /// </summary>
        public async Task<BoatFuelPriceDto> GetByIdAsync(int boatFuelPriceID)
        {
            using var connection = _connectionFactory.CreateConnection();

            var sql = @"
                SELECT
                    bfp.BoatFuelPriceID,
                    bfp.EffectiveDate,
                    bfp.Price,
                    bfp.FuelVendorBusinessUnitID,
                    bu.Name AS FuelVendor,
                    bfp.InvoiceNumber,
                    bfp.CreateDateTime,
                    bfp.CreateUser,
                    bfp.ModifyDateTime,
                    bfp.ModifyUser
                FROM BoatFuelPrice bfp
                LEFT JOIN BusinessUnit bu ON bfp.FuelVendorBusinessUnitID = bu.BusinessUnitID
                WHERE bfp.BoatFuelPriceID = @BoatFuelPriceID";

            return await connection.QuerySingleOrDefaultAsync<BoatFuelPriceDto>(
                sql,
                new { BoatFuelPriceID = boatFuelPriceID });
        }

        /// <summary>
        /// Create a new boat fuel price record
        /// Uses OUTPUT INSERTED to get the new ID
        /// </summary>
        public async Task<int> CreateAsync(BoatFuelPriceDto dto, string userName)
        {
            using var connection = _connectionFactory.CreateConnection();

            var sql = @"
                INSERT INTO BoatFuelPrice (
                    EffectiveDate,
                    Price,
                    FuelVendorBusinessUnitID,
                    InvoiceNumber,
                    CreateDateTime,
                    CreateUser,
                    ModifyDateTime,
                    ModifyUser
                )
                OUTPUT INSERTED.BoatFuelPriceID
                VALUES (
                    @EffectiveDate,
                    @Price,
                    @FuelVendorBusinessUnitID,
                    @InvoiceNumber,
                    GETDATE(),
                    @UserName,
                    GETDATE(),
                    @UserName
                )";

            var parameters = new
            {
                dto.EffectiveDate,
                dto.Price,
                dto.FuelVendorBusinessUnitID,
                dto.InvoiceNumber,
                UserName = userName
            };

            return await connection.ExecuteScalarAsync<int>(sql, parameters);
        }

        /// <summary>
        /// Update an existing boat fuel price record
        /// </summary>
        public async Task<bool> UpdateAsync(BoatFuelPriceDto dto, string userName)
        {
            using var connection = _connectionFactory.CreateConnection();

            var sql = @"
                UPDATE BoatFuelPrice
                SET
                    EffectiveDate = @EffectiveDate,
                    Price = @Price,
                    FuelVendorBusinessUnitID = @FuelVendorBusinessUnitID,
                    InvoiceNumber = @InvoiceNumber,
                    ModifyDateTime = GETDATE(),
                    ModifyUser = @UserName
                WHERE BoatFuelPriceID = @BoatFuelPriceID";

            var parameters = new
            {
                dto.BoatFuelPriceID,
                dto.EffectiveDate,
                dto.Price,
                dto.FuelVendorBusinessUnitID,
                dto.InvoiceNumber,
                UserName = userName
            };

            var rowsAffected = await connection.ExecuteAsync(sql, parameters);
            return rowsAffected > 0;
        }

        /// <summary>
        /// Delete a boat fuel price record
        /// </summary>
        public async Task<bool> DeleteAsync(int boatFuelPriceID)
        {
            using var connection = _connectionFactory.CreateConnection();

            var sql = "DELETE FROM BoatFuelPrice WHERE BoatFuelPriceID = @BoatFuelPriceID";

            var rowsAffected = await connection.ExecuteAsync(
                sql,
                new { BoatFuelPriceID = boatFuelPriceID });

            return rowsAffected > 0;
        }

        /// <summary>
        /// Check if a boat fuel price already exists for the given date and vendor
        /// Handles NULL vendor comparison correctly
        /// </summary>
        public async Task<bool> IsUniqueAsync(DateTime effectiveDate, int? fuelVendorBusinessUnitID, int? excludeBoatFuelPriceID = null)
        {
            using var connection = _connectionFactory.CreateConnection();

            var sql = @"
                SELECT COUNT(*)
                FROM BoatFuelPrice
                WHERE EffectiveDate = @EffectiveDate
                  AND (
                      (@FuelVendorBusinessUnitID IS NULL AND FuelVendorBusinessUnitID IS NULL)
                      OR (FuelVendorBusinessUnitID = @FuelVendorBusinessUnitID)
                  )
                  AND (@ExcludeBoatFuelPriceID IS NULL OR BoatFuelPriceID != @ExcludeBoatFuelPriceID)";

            var count = await connection.QuerySingleAsync<int>(
                sql,
                new
                {
                    EffectiveDate = effectiveDate,
                    FuelVendorBusinessUnitID = fuelVendorBusinessUnitID,
                    ExcludeBoatFuelPriceID = excludeBoatFuelPriceID
                });

            return count == 0; // True if unique (no duplicates found)
        }
    }
}
