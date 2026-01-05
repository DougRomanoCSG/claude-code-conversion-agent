using BargeOps.Shared.Dto;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace BargeOps.Admin.Infrastructure.Repositories;

/// <summary>
/// Repository for Facility operations using Dapper
/// Uses DIRECT SQL QUERIES (not stored procedures)
/// Returns DTOs directly (no mapping needed)
/// </summary>
public class FacilityRepository : IFacilityRepository
{
    private readonly string _connectionString;

    public FacilityRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<PagedResult<FacilityDto>> SearchAsync(
        FacilitySearchRequest request,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var whereConditions = new List<string>();
        var parameters = new DynamicParameters();

        // Build WHERE clause
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            whereConditions.Add("l.Name LIKE '%' + @Name + '%'");
            parameters.Add("Name", request.Name);
        }

        if (!string.IsNullOrWhiteSpace(request.ShortName))
        {
            whereConditions.Add("l.ShortName LIKE '%' + @ShortName + '%'");
            parameters.Add("ShortName", request.ShortName);
        }

        if (!string.IsNullOrWhiteSpace(request.River))
        {
            whereConditions.Add("l.River = @River");
            parameters.Add("River", request.River);
        }

        if (!string.IsNullOrWhiteSpace(request.BargeExLocationType))
        {
            whereConditions.Add("fl.BargeExLocationType = @BargeExLocationType");
            parameters.Add("BargeExLocationType", request.BargeExLocationType);
        }

        if (!string.IsNullOrWhiteSpace(request.BargeExCode))
        {
            whereConditions.Add("fl.BargeExCode LIKE '%' + @BargeExCode + '%'");
            parameters.Add("BargeExCode", request.BargeExCode);
        }

        if (request.IsActive.HasValue)
        {
            whereConditions.Add("l.IsActive = @IsActive");
            parameters.Add("IsActive", request.IsActive.Value);
        }

        var whereClause = whereConditions.Any()
            ? "WHERE " + string.Join(" AND ", whereConditions)
            : "";

        // DataTables counts:
        // - totalCount: total rows without filtering
        // - filteredCount: rows after applying filters
        var totalCountSql = @"
            SELECT COUNT(*)
            FROM Location l
            INNER JOIN FacilityLocation fl ON l.LocationID = fl.LocationID";

        var totalCount = await connection.ExecuteScalarAsync<int>(totalCountSql);

        var filteredCountSql = $@"
            SELECT COUNT(*)
            FROM Location l
            INNER JOIN FacilityLocation fl ON l.LocationID = fl.LocationID
            {whereClause}";

        var filteredCount = await connection.ExecuteScalarAsync<int>(filteredCountSql, parameters);

        // Get paged data
        parameters.Add("Skip", request.Start);
        parameters.Add("Take", request.Length);

        // IMPORTANT: Never interpolate user input directly into ORDER BY.
        // Map DataTables column keys (typically camelCase JSON names) to safe SQL expressions.
        var orderBy = BuildSafeOrderByClause(request.SortColumn, request.SortDirection);

        var dataSql = $@"
            SELECT
                l.LocationID,
                l.Name,
                l.ShortName,
                l.Note,
                l.IsActive,
                l.River,
                l.Mile,
                fl.BargeExCode,
                fl.Bank,
                fl.BargeExLocationType,
                fl.LockUsaceName,
                fl.LockFloodStage,
                fl.LockPoolStage,
                fl.LockLowWater,
                fl.LockNormalCurrent,
                fl.LockHighFlow,
                fl.LockHighWater,
                fl.LockCatastrophicLevel,
                fl.NdcName,
                fl.NdcLocationDescription,
                fl.NdcAddress,
                fl.NdcCounty,
                fl.NdcCountyFips,
                fl.NdcTown,
                fl.NdcState,
                fl.NdcWaterway,
                fl.NdcPort,
                fl.NdcLatitude,
                fl.NdcLongitude,
                fl.NdcOperator,
                fl.NdcOwner,
                fl.NdcPurpose,
                fl.NdcRemark
            FROM Location l
            INNER JOIN FacilityLocation fl ON l.LocationID = fl.LocationID
            {whereClause}
            {orderBy}
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

        var data = await connection.QueryAsync<FacilityDto>(dataSql, parameters);

        return new PagedResult<FacilityDto>
        {
            Data = data.ToList(),
            TotalRecords = totalCount,
            FilteredRecords = filteredCount
        };
    }

    public async Task<FacilityDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            SELECT
                l.LocationID,
                l.Name,
                l.ShortName,
                l.Note,
                l.IsActive,
                l.River,
                l.Mile,
                fl.BargeExCode,
                fl.Bank,
                fl.BargeExLocationType,
                fl.LockUsaceName,
                fl.LockFloodStage,
                fl.LockPoolStage,
                fl.LockLowWater,
                fl.LockNormalCurrent,
                fl.LockHighFlow,
                fl.LockHighWater,
                fl.LockCatastrophicLevel,
                fl.NdcName,
                fl.NdcLocationDescription,
                fl.NdcAddress,
                fl.NdcCounty,
                fl.NdcCountyFips,
                fl.NdcTown,
                fl.NdcState,
                fl.NdcWaterway,
                fl.NdcPort,
                fl.NdcLatitude,
                fl.NdcLongitude,
                fl.NdcOperator,
                fl.NdcOwner,
                fl.NdcPurpose,
                fl.NdcRemark
            FROM Location l
            INNER JOIN FacilityLocation fl ON l.LocationID = fl.LocationID
            WHERE l.LocationID = @LocationID";

        var facility = await connection.QueryFirstOrDefaultAsync<FacilityDto>(sql, new { LocationID = id });

        if (facility != null)
        {
            // Load child collections
            facility.Berths = (await GetBerthsAsync(id, cancellationToken)).ToList();
            facility.Statuses = (await GetStatusesAsync(id, cancellationToken)).ToList();
        }

        return facility;
    }

    public async Task<FacilityDto> CreateAsync(FacilityDto facility, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            // Insert Location
            var locationSql = @"
                INSERT INTO Location (Name, ShortName, Note, IsActive, River, Mile)
                VALUES (@Name, @ShortName, @Note, @IsActive, @River, @Mile);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var locationId = await connection.ExecuteScalarAsync<int>(locationSql, facility, transaction);

            // Insert FacilityLocation
            var facilitySql = @"
                INSERT INTO FacilityLocation (
                    LocationID, BargeExCode, Bank, BargeExLocationType,
                    LockUsaceName, LockFloodStage, LockPoolStage, LockLowWater,
                    LockNormalCurrent, LockHighFlow, LockHighWater, LockCatastrophicLevel
                )
                VALUES (
                    @LocationID, @BargeExCode, @Bank, @BargeExLocationType,
                    @LockUsaceName, @LockFloodStage, @LockPoolStage, @LockLowWater,
                    @LockNormalCurrent, @LockHighFlow, @LockHighWater, @LockCatastrophicLevel
                )";

            await connection.ExecuteAsync(facilitySql, new
            {
                LocationID = locationId,
                facility.BargeExCode,
                facility.Bank,
                facility.BargeExLocationType,
                facility.LockUsaceName,
                facility.LockFloodStage,
                facility.LockPoolStage,
                facility.LockLowWater,
                facility.LockNormalCurrent,
                facility.LockHighFlow,
                facility.LockHighWater,
                facility.LockCatastrophicLevel
            }, transaction);

            transaction.Commit();

            facility.LocationID = locationId;
            return facility;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<FacilityDto> UpdateAsync(FacilityDto facility, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            // Update Location
            var locationSql = @"
                UPDATE Location
                SET Name = @Name,
                    ShortName = @ShortName,
                    Note = @Note,
                    IsActive = @IsActive,
                    River = @River,
                    Mile = @Mile
                WHERE LocationID = @LocationID";

            await connection.ExecuteAsync(locationSql, facility, transaction);

            // Update FacilityLocation
            var facilitySql = @"
                UPDATE FacilityLocation
                SET BargeExCode = @BargeExCode,
                    Bank = @Bank,
                    BargeExLocationType = @BargeExLocationType,
                    LockUsaceName = @LockUsaceName,
                    LockFloodStage = @LockFloodStage,
                    LockPoolStage = @LockPoolStage,
                    LockLowWater = @LockLowWater,
                    LockNormalCurrent = @LockNormalCurrent,
                    LockHighFlow = @LockHighFlow,
                    LockHighWater = @LockHighWater,
                    LockCatastrophicLevel = @LockCatastrophicLevel
                WHERE LocationID = @LocationID";

            await connection.ExecuteAsync(facilitySql, facility, transaction);

            transaction.Commit();

            return facility;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            // Delete child berths
            await connection.ExecuteAsync(
                "DELETE FROM FacilityBerth WHERE LocationID = @LocationID",
                new { LocationID = id },
                transaction);

            // Delete child statuses
            await connection.ExecuteAsync(
                "DELETE FROM FacilityStatus WHERE LocationID = @LocationID",
                new { LocationID = id },
                transaction);

            // Delete FacilityLocation
            await connection.ExecuteAsync(
                "DELETE FROM FacilityLocation WHERE LocationID = @LocationID",
                new { LocationID = id },
                transaction);

            // Delete Location
            var rowsAffected = await connection.ExecuteAsync(
                "DELETE FROM Location WHERE LocationID = @LocationID",
                new { LocationID = id },
                transaction);

            transaction.Commit();

            return rowsAffected > 0;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<IEnumerable<FacilityBerthDto>> GetBerthsAsync(int facilityId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            SELECT
                fb.FacilityBerthID,
                fb.LocationID,
                fb.Name,
                fb.ShipID,
                s.Name AS ShipName
            FROM FacilityBerth fb
            LEFT JOIN Ship s ON fb.ShipID = s.ShipID
            WHERE fb.LocationID = @LocationID
            ORDER BY fb.Name";

        return await connection.QueryAsync<FacilityBerthDto>(sql, new { LocationID = facilityId });
    }

    public async Task<IEnumerable<FacilityStatusDto>> GetStatusesAsync(int facilityId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            SELECT
                FacilityStatusID,
                LocationID,
                StartDateTime,
                EndDateTime,
                Status,
                Note
            FROM FacilityStatus
            WHERE LocationID = @LocationID
            ORDER BY StartDateTime DESC";

        return await connection.QueryAsync<FacilityStatusDto>(sql, new { LocationID = facilityId });
    }

    public async Task<FacilityBerthDto> CreateBerthAsync(FacilityBerthDto berth, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            INSERT INTO FacilityBerth (LocationID, Name, ShipID)
            VALUES (@LocationID, @Name, @ShipID);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        berth.FacilityBerthID = await connection.ExecuteScalarAsync<int>(sql, berth);

        return berth;
    }

    public async Task<FacilityBerthDto> UpdateBerthAsync(FacilityBerthDto berth, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            UPDATE FacilityBerth
            SET Name = @Name,
                ShipID = @ShipID
            WHERE FacilityBerthID = @FacilityBerthID";

        await connection.ExecuteAsync(sql, berth);

        return berth;
    }

    public async Task<bool> DeleteBerthAsync(int berthId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var rowsAffected = await connection.ExecuteAsync(
            "DELETE FROM FacilityBerth WHERE FacilityBerthID = @FacilityBerthID",
            new { FacilityBerthID = berthId });

        return rowsAffected > 0;
    }

    public async Task<FacilityStatusDto> CreateStatusAsync(FacilityStatusDto status, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            INSERT INTO FacilityStatus (LocationID, StartDateTime, EndDateTime, Status, Note)
            VALUES (@LocationID, @StartDateTime, @EndDateTime, @Status, @Note);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        status.FacilityStatusID = await connection.ExecuteScalarAsync<int>(sql, status);

        return status;
    }

    public async Task<FacilityStatusDto> UpdateStatusAsync(FacilityStatusDto status, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            UPDATE FacilityStatus
            SET StartDateTime = @StartDateTime,
                EndDateTime = @EndDateTime,
                Status = @Status,
                Note = @Note
            WHERE FacilityStatusID = @FacilityStatusID";

        await connection.ExecuteAsync(sql, status);

        return status;
    }

    public async Task<bool> DeleteStatusAsync(int statusId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var rowsAffected = await connection.ExecuteAsync(
            "DELETE FROM FacilityStatus WHERE FacilityStatusID = @FacilityStatusID",
            new { FacilityStatusID = statusId });

        return rowsAffected > 0;
    }

    private static string BuildSafeOrderByClause(string? sortColumn, string? sortDirection)
    {
        // Default sort.
        var direction = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";

        // UI DataTables sends column "data" values like: name, shortName, river, mile, bargeExLocationType, bargeExCode, isActive, locationID
        // Whitelist only known columns and map to proper aliases.
        var key = sortColumn?.Trim();
        var keyLower = key?.ToLowerInvariant();

        var columnExpr = keyLower switch
        {
            "locationid" => "l.LocationID",
            "name" => "l.Name",
            "shortname" => "l.ShortName",
            "river" => "l.River",
            "mile" => "l.Mile",
            "bargeexlocationtype" => "fl.BargeExLocationType",
            "bargeexcode" => "fl.BargeExCode",
            "isactive" => "l.IsActive",
            _ => "l.Name"
        };

        return $"ORDER BY {columnExpr} {direction}";
    }
}
