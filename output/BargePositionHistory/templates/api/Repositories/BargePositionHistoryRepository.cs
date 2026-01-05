using BargeOps.Shared.Dto;
using Admin.Infrastructure.Abstractions;
using Csg.ListQuery;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Barge Position History data access using Dapper.
/// Target: C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Repositories\BargePositionHistoryRepository.cs
/// </summary>
public class BargePositionHistoryRepository : IBargePositionHistoryRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public BargePositionHistoryRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<DataTableResponse<BargePositionHistoryDto>> SearchAsync(BargePositionHistorySearchRequest request)
    {
        using var connection = _connectionFactory.CreateConnection();

        // Build dynamic SQL query based on search criteria
        var sql = @"
            SELECT
                fph.FleetPositionHistoryID,
                fph.FleetID,
                f.FleetName,
                fph.BargeID,
                b.BargeNum,
                fph.TierID,
                t.TierName,
                fph.TierX,
                fph.TierY,
                CASE
                    WHEN fph.TierX IS NOT NULL AND fph.TierY IS NOT NULL
                    THEN CONCAT('(', fph.TierX, ',', fph.TierY, ')')
                    ELSE NULL
                END AS TierPos,
                fph.PositionStartDateTime,
                fph.LeftFleet,
                fph.CreateDateTime,
                fph.CreateUser,
                fph.ModifyDateTime,
                fph.ModifyUser
            FROM FleetPositionHistory fph
            INNER JOIN Barge b ON b.BargeID = fph.BargeID
            LEFT JOIN Fleet f ON f.FleetID = fph.FleetID
            LEFT JOIN Tier t ON t.TierID = fph.TierID
            WHERE fph.FleetID = @FleetID
                AND CAST(fph.PositionStartDateTime AS DATE) = CAST(@PositionStartDate AS DATE)
                AND (@TierGroupID IS NULL OR t.TierGroupID = @TierGroupID)
                AND (@BargeNum IS NULL OR @BargeNum = '' OR b.BargeNum LIKE '%' + @BargeNum + '%')
                AND (@IncludeBlankTierPos = 1 OR fph.TierID IS NOT NULL)
            ORDER BY
                CASE WHEN @OrderColumn = 'PositionStartDateTime' AND @OrderDirection = 'ASC' THEN fph.PositionStartDateTime END ASC,
                CASE WHEN @OrderColumn = 'PositionStartDateTime' AND @OrderDirection = 'DESC' THEN fph.PositionStartDateTime END DESC,
                CASE WHEN @OrderColumn = 'BargeNum' AND @OrderDirection = 'ASC' THEN b.BargeNum END ASC,
                CASE WHEN @OrderColumn = 'BargeNum' AND @OrderDirection = 'DESC' THEN b.BargeNum END DESC,
                CASE WHEN @OrderColumn = 'TierName' AND @OrderDirection = 'ASC' THEN t.TierName END ASC,
                CASE WHEN @OrderColumn = 'TierName' AND @OrderDirection = 'DESC' THEN t.TierName END DESC,
                fph.PositionStartDateTime ASC
            OFFSET @Start ROWS
            FETCH NEXT @Length ROWS ONLY;

            -- Get total count
            SELECT COUNT(*)
            FROM FleetPositionHistory fph
            INNER JOIN Barge b ON b.BargeID = fph.BargeID
            LEFT JOIN Tier t ON t.TierID = fph.TierID
            WHERE fph.FleetID = @FleetID
                AND CAST(fph.PositionStartDateTime AS DATE) = CAST(@PositionStartDate AS DATE)
                AND (@TierGroupID IS NULL OR t.TierGroupID = @TierGroupID)
                AND (@BargeNum IS NULL OR @BargeNum = '' OR b.BargeNum LIKE '%' + @BargeNum + '%')
                AND (@IncludeBlankTierPos = 1 OR fph.TierID IS NOT NULL);
        ";

        var parameters = new DynamicParameters();
        parameters.Add("@FleetID", request.FleetID);
        parameters.Add("@PositionStartDate", request.PositionStartDate);
        parameters.Add("@TierGroupID", request.TierGroupID);
        parameters.Add("@BargeNum", request.BargeNum);
        parameters.Add("@IncludeBlankTierPos", request.IncludeBlankTierPos);
        parameters.Add("@Start", request.Start ?? 0);
        parameters.Add("@Length", request.Length ?? 25);
        parameters.Add("@OrderColumn", request.OrderColumn ?? "PositionStartDateTime");
        parameters.Add("@OrderDirection", request.OrderDirection ?? "ASC");

        using var multi = await connection.QueryMultipleAsync(sql, parameters);

        var data = (await multi.ReadAsync<BargePositionHistoryDto>()).ToList();
        var totalCount = await multi.ReadSingleAsync<int>();

        return new DataTableResponse<BargePositionHistoryDto>
        {
            Draw = request.Draw ?? 0,
            RecordsTotal = totalCount,
            RecordsFiltered = totalCount,
            Data = data
        };
    }

    public async Task<BargePositionHistoryDto> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = @"
            SELECT
                fph.FleetPositionHistoryID,
                fph.FleetID,
                f.FleetName,
                fph.BargeID,
                b.BargeNum,
                fph.TierID,
                t.TierName,
                fph.TierX,
                fph.TierY,
                CASE
                    WHEN fph.TierX IS NOT NULL AND fph.TierY IS NOT NULL
                    THEN CONCAT('(', fph.TierX, ',', fph.TierY, ')')
                    ELSE NULL
                END AS TierPos,
                fph.PositionStartDateTime,
                fph.LeftFleet,
                fph.CreateDateTime,
                fph.CreateUser,
                fph.ModifyDateTime,
                fph.ModifyUser
            FROM FleetPositionHistory fph
            INNER JOIN Barge b ON b.BargeID = fph.BargeID
            LEFT JOIN Fleet f ON f.FleetID = fph.FleetID
            LEFT JOIN Tier t ON t.TierID = fph.TierID
            WHERE fph.FleetPositionHistoryID = @FleetPositionHistoryID;
        ";

        var parameters = new DynamicParameters();
        parameters.Add("@FleetPositionHistoryID", id);

        return await connection.QuerySingleOrDefaultAsync<BargePositionHistoryDto>(sql, parameters);
    }

    public async Task<int> InsertAsync(BargePositionHistoryDto dto, string modifyUser)
    {
        using var connection = _connectionFactory.CreateConnection();

        // Validate tier coordinates if TierID is provided
        if (dto.TierID.HasValue && (dto.TierX.HasValue || dto.TierY.HasValue))
        {
            var isValid = await ValidateTierCoordinatesAsync(dto.TierID.Value, dto.TierX ?? 0, dto.TierY ?? 0);
            if (!isValid)
            {
                throw new InvalidOperationException($"Tier coordinates ({dto.TierX},{dto.TierY}) are outside the boundaries of the selected tier.");
            }
        }

        var sql = @"
            INSERT INTO FleetPositionHistory (
                FleetID,
                BargeID,
                TierID,
                TierX,
                TierY,
                PositionStartDateTime,
                LeftFleet,
                CreateDateTime,
                CreateUser,
                ModifyDateTime,
                ModifyUser
            )
            VALUES (
                @FleetID,
                @BargeID,
                @TierID,
                @TierX,
                @TierY,
                @PositionStartDateTime,
                @LeftFleet,
                GETDATE(),
                @ModifyUser,
                GETDATE(),
                @ModifyUser
            );

            SELECT CAST(SCOPE_IDENTITY() AS INT);
        ";

        var parameters = new DynamicParameters();
        parameters.Add("@FleetID", dto.FleetID);
        parameters.Add("@BargeID", dto.BargeID);
        parameters.Add("@TierID", dto.TierID);
        parameters.Add("@TierX", dto.TierX);
        parameters.Add("@TierY", dto.TierY);
        parameters.Add("@PositionStartDateTime", dto.PositionStartDateTime);
        parameters.Add("@LeftFleet", dto.LeftFleet);
        parameters.Add("@ModifyUser", modifyUser);

        return await connection.ExecuteScalarAsync<int>(sql, parameters);
    }

    public async Task UpdateAsync(BargePositionHistoryDto dto, string modifyUser)
    {
        using var connection = _connectionFactory.CreateConnection();

        // Validate tier coordinates if TierID is provided
        if (dto.TierID.HasValue && (dto.TierX.HasValue || dto.TierY.HasValue))
        {
            var isValid = await ValidateTierCoordinatesAsync(dto.TierID.Value, dto.TierX ?? 0, dto.TierY ?? 0);
            if (!isValid)
            {
                throw new InvalidOperationException($"Tier coordinates ({dto.TierX},{dto.TierY}) are outside the boundaries of the selected tier.");
            }
        }

        var sql = @"
            UPDATE FleetPositionHistory
            SET
                FleetID = @FleetID,
                BargeID = @BargeID,
                TierID = @TierID,
                TierX = @TierX,
                TierY = @TierY,
                PositionStartDateTime = @PositionStartDateTime,
                LeftFleet = @LeftFleet,
                ModifyDateTime = GETDATE(),
                ModifyUser = @ModifyUser
            WHERE FleetPositionHistoryID = @FleetPositionHistoryID
                AND (ModifyDateTime = @ModifyDateTime OR ModifyDateTime IS NULL);

            SELECT @@ROWCOUNT;
        ";

        var parameters = new DynamicParameters();
        parameters.Add("@FleetPositionHistoryID", dto.FleetPositionHistoryID);
        parameters.Add("@FleetID", dto.FleetID);
        parameters.Add("@BargeID", dto.BargeID);
        parameters.Add("@TierID", dto.TierID);
        parameters.Add("@TierX", dto.TierX);
        parameters.Add("@TierY", dto.TierY);
        parameters.Add("@PositionStartDateTime", dto.PositionStartDateTime);
        parameters.Add("@LeftFleet", dto.LeftFleet);
        parameters.Add("@ModifyDateTime", dto.ModifyDateTime);
        parameters.Add("@ModifyUser", modifyUser);

        var rowsAffected = await connection.ExecuteScalarAsync<int>(sql, parameters);

        if (rowsAffected == 0)
        {
            throw new ConcurrencyException("The record has been modified by another user. Please refresh and try again.");
        }
    }

    public async Task DeleteAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = @"
            DELETE FROM FleetPositionHistory
            WHERE FleetPositionHistoryID = @FleetPositionHistoryID;
        ";

        var parameters = new DynamicParameters();
        parameters.Add("@FleetPositionHistoryID", id);

        await connection.ExecuteAsync(sql, parameters);
    }

    public async Task<bool> ValidateTierCoordinatesAsync(int tierId, short tierX, short tierY)
    {
        using var connection = _connectionFactory.CreateConnection();

        // This would call FleetPositionHistoryCheck stored procedure or equivalent validation logic
        // For now, we'll use a simple boundary check against Tier table
        var sql = @"
            SELECT COUNT(*)
            FROM Tier
            WHERE TierID = @TierID
                AND @TierX >= MinX AND @TierX <= MaxX
                AND @TierY >= MinY AND @TierY <= MaxY;
        ";

        var parameters = new DynamicParameters();
        parameters.Add("@TierID", tierId);
        parameters.Add("@TierX", tierX);
        parameters.Add("@TierY", tierY);

        var count = await connection.ExecuteScalarAsync<int>(sql, parameters);

        return count > 0;
    }

    public async Task<int?> GetBargeIdByNumberAsync(string bargeNum)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = @"
            SELECT BargeID
            FROM Barge
            WHERE BargeNum = @BargeNum;
        ";

        var parameters = new DynamicParameters();
        parameters.Add("@BargeNum", bargeNum);

        return await connection.QuerySingleOrDefaultAsync<int?>(sql, parameters);
    }
}

/// <summary>
/// Exception thrown when optimistic concurrency check fails.
/// </summary>
public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message) { }
}
