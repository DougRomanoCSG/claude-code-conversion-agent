using Admin.Infrastructure.Abstractions;
using BargeOps.Shared.Dto;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Admin.Infrastructure.Repositories
{
    /// <summary>
    /// Dapper-based repository for RiverArea entity.
    /// Returns DTOs directly - NO mapping to domain models!
    /// Uses direct SQL queries embedded as resources (NOT stored procedures).
    ///
    /// Pattern Reference: FacilityRepository.cs, BoatLocationRepository.cs
    /// </summary>
    public class RiverAreaRepository : IRiverAreaRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public RiverAreaRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<DataTableResponse<RiverAreaListDto>> SearchAsync(RiverAreaSearchRequest request)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Build WHERE clause dynamically based on search criteria
            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                whereClauses.Add("ra.Name LIKE @Name");
                parameters.Add("Name", $"%{request.Name}%");
            }

            if (request.ActiveOnly)
            {
                whereClauses.Add("ra.IsActive = 1");
            }

            if (request.PricingZonesOnly)
            {
                whereClauses.Add("ra.IsPriceZone = 1");
            }

            if (request.PortalAreasOnly)
            {
                whereClauses.Add("ra.IsPortalArea = 1");
            }

            if (request.CustomerID.HasValue)
            {
                whereClauses.Add("ra.CustomerID = @CustomerID");
                parameters.Add("CustomerID", request.CustomerID.Value);
            }

            if (request.HighWaterAreasOnly)
            {
                whereClauses.Add("ra.IsHighWaterArea = 1");
            }

            var whereClause = whereClauses.Any() ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            // Count query for total filtered records
            var countSql = $@"
                SELECT COUNT(*)
                FROM RiverArea ra
                {whereClause}";

            var totalFiltered = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            // Data query with pagination and sorting
            var orderColumn = request.OrderColumn ?? "Name";
            var orderDirection = request.OrderDirection ?? "ASC";

            var dataSql = $@"
                SELECT
                    ra.RiverAreaID,
                    ra.Name,
                    ra.IsActive,
                    ra.IsPriceZone,
                    ra.IsPortalArea,
                    ra.IsHighWaterArea,
                    c.CustomerName,
                    ra.IsFuelTaxArea,
                    ra.IsLiquidRateArea
                FROM RiverArea ra
                LEFT JOIN Customer c ON ra.CustomerID = c.CustomerID
                {whereClause}
                ORDER BY {orderColumn} {orderDirection}
                OFFSET @Start ROWS
                FETCH NEXT @Length ROWS ONLY";

            parameters.Add("Start", request.Start);
            parameters.Add("Length", request.Length);

            var data = await connection.QueryAsync<RiverAreaListDto>(dataSql, parameters);

            return new DataTableResponse<RiverAreaListDto>
            {
                Draw = request.Draw,
                RecordsTotal = totalFiltered,
                RecordsFiltered = totalFiltered,
                Data = data.ToList()
            };
        }

        public async Task<RiverAreaDto> GetByIdAsync(int riverAreaId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT
                    RiverAreaID,
                    Name,
                    IsActive,
                    IsPriceZone,
                    IsPortalArea,
                    IsHighWaterArea,
                    CustomerID,
                    IsFuelTaxArea,
                    IsLiquidRateArea
                FROM RiverArea
                WHERE RiverAreaID = @RiverAreaID;

                SELECT
                    RiverAreaSegmentID,
                    RiverAreaID,
                    River,
                    StartMile,
                    EndMile
                FROM RiverAreaSegment
                WHERE RiverAreaID = @RiverAreaID
                ORDER BY River, StartMile;";

            using var multi = await connection.QueryMultipleAsync(sql, new { RiverAreaID = riverAreaId });

            var riverArea = await multi.ReadSingleOrDefaultAsync<RiverAreaDto>();
            if (riverArea != null)
            {
                riverArea.Segments = (await multi.ReadAsync<RiverAreaSegmentDto>()).ToList();
            }

            return riverArea;
        }

        public async Task<int> CreateAsync(RiverAreaDto riverArea)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                var sql = @"
                    INSERT INTO RiverArea (Name, IsActive, IsPriceZone, IsPortalArea, IsHighWaterArea, CustomerID, IsFuelTaxArea, IsLiquidRateArea)
                    VALUES (@Name, @IsActive, @IsPriceZone, @IsPortalArea, @IsHighWaterArea, @CustomerID, @IsFuelTaxArea, @IsLiquidRateArea);
                    SELECT CAST(SCOPE_IDENTITY() as int);";

                var riverAreaId = await connection.ExecuteScalarAsync<int>(sql, riverArea, transaction);

                // Insert segments
                if (riverArea.Segments != null && riverArea.Segments.Any())
                {
                    foreach (var segment in riverArea.Segments)
                    {
                        segment.RiverAreaID = riverAreaId;
                        await CreateSegmentAsync(connection, transaction, segment);
                    }
                }

                transaction.Commit();
                return riverAreaId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task UpdateAsync(RiverAreaDto riverArea)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                var sql = @"
                    UPDATE RiverArea
                    SET Name = @Name,
                        IsActive = @IsActive,
                        IsPriceZone = @IsPriceZone,
                        IsPortalArea = @IsPortalArea,
                        IsHighWaterArea = @IsHighWaterArea,
                        CustomerID = @CustomerID,
                        IsFuelTaxArea = @IsFuelTaxArea,
                        IsLiquidRateArea = @IsLiquidRateArea
                    WHERE RiverAreaID = @RiverAreaID";

                await connection.ExecuteAsync(sql, riverArea, transaction);

                // Delete existing segments and re-insert (simplest approach for child collection)
                await connection.ExecuteAsync(
                    "DELETE FROM RiverAreaSegment WHERE RiverAreaID = @RiverAreaID",
                    new { riverArea.RiverAreaID },
                    transaction);

                // Insert segments
                if (riverArea.Segments != null && riverArea.Segments.Any())
                {
                    foreach (var segment in riverArea.Segments)
                    {
                        segment.RiverAreaID = riverArea.RiverAreaID;
                        await CreateSegmentAsync(connection, transaction, segment);
                    }
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task DeleteAsync(int riverAreaId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                // Delete segments first (cascade delete)
                await connection.ExecuteAsync(
                    "DELETE FROM RiverAreaSegment WHERE RiverAreaID = @RiverAreaID",
                    new { RiverAreaID = riverAreaId },
                    transaction);

                // Hard delete river area
                await connection.ExecuteAsync(
                    "DELETE FROM RiverArea WHERE RiverAreaID = @RiverAreaID",
                    new { RiverAreaID = riverAreaId },
                    transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<IEnumerable<RiverAreaSegmentDto>> GetSegmentsByRiverAreaIdAsync(int riverAreaId)
        {
            using var connection = new SqlConnection(_connectionString);

            var sql = @"
                SELECT
                    RiverAreaSegmentID,
                    RiverAreaID,
                    River,
                    StartMile,
                    EndMile
                FROM RiverAreaSegment
                WHERE RiverAreaID = @RiverAreaID
                ORDER BY River, StartMile";

            return await connection.QueryAsync<RiverAreaSegmentDto>(sql, new { RiverAreaID = riverAreaId });
        }

        public async Task<int> CreateSegmentAsync(RiverAreaSegmentDto segment)
        {
            using var connection = new SqlConnection(_connectionString);
            return await CreateSegmentAsync(connection, null, segment);
        }

        private async Task<int> CreateSegmentAsync(IDbConnection connection, IDbTransaction transaction, RiverAreaSegmentDto segment)
        {
            var sql = @"
                INSERT INTO RiverAreaSegment (RiverAreaID, River, StartMile, EndMile)
                VALUES (@RiverAreaID, @River, @StartMile, @EndMile);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            return await connection.ExecuteScalarAsync<int>(sql, segment, transaction);
        }

        public async Task UpdateSegmentAsync(RiverAreaSegmentDto segment)
        {
            using var connection = new SqlConnection(_connectionString);

            var sql = @"
                UPDATE RiverAreaSegment
                SET RiverAreaID = @RiverAreaID,
                    River = @River,
                    StartMile = @StartMile,
                    EndMile = @EndMile
                WHERE RiverAreaSegmentID = @RiverAreaSegmentID";

            await connection.ExecuteAsync(sql, segment);
        }

        public async Task DeleteSegmentAsync(int riverAreaSegmentId)
        {
            using var connection = new SqlConnection(_connectionString);

            await connection.ExecuteAsync(
                "DELETE FROM RiverAreaSegment WHERE RiverAreaSegmentID = @RiverAreaSegmentID",
                new { RiverAreaSegmentID = riverAreaSegmentId });
        }
    }
}
