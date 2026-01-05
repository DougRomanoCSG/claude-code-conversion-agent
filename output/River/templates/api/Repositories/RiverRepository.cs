using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BargeOps.Shared.Dto;
using BargeOps.Shared.Models;
using Admin.Infrastructure.Abstractions;
using Admin.Infrastructure.DataAccess;
using Admin.Infrastructure.Exceptions;
using Dapper;

namespace Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for River entity
/// Uses embedded SQL queries (NOT stored procedures)
/// Returns DTOs directly from BargeOps.Shared
/// </summary>
public class RiverRepository : IRiverRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RiverRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<RiverDto?> GetByIdAsync(int riverID)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = SqlText.GetRiverById;
        var result = await connection.QuerySingleOrDefaultAsync<RiverDto>(
            sql,
            new { RiverID = riverID });

        return result;
    }

    public async Task<IEnumerable<RiverDto>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = SqlText.GetAllRivers;
        var result = await connection.QueryAsync<RiverDto>(sql);

        return result;
    }

    public async Task<DataTableResponse<RiverDto>> SearchAsync(RiverSearchRequest request)
    {
        using var connection = _connectionFactory.CreateConnection();

        var parameters = new
        {
            Code = request?.Code,
            Name = request?.Name,
            ActiveOnly = request?.ActiveOnly ?? true,
            Start = request?.Start ?? 0,
            Length = request?.Length ?? 25,
            OrderColumn = request?.OrderColumn ?? "Code",
            OrderDirection = request?.OrderDirection ?? "ASC"
        };

        var sql = SqlText.SearchRivers;
        using var multi = await connection.QueryMultipleAsync(sql, parameters);

        var data = (await multi.ReadAsync<RiverDto>()).ToList();
        var totalCount = await multi.ReadSingleAsync<int>();

        return new DataTableResponse<RiverDto>
        {
            Draw = request?.Draw ?? 0,
            RecordsTotal = totalCount,
            RecordsFiltered = totalCount,
            Data = data
        };
    }

    public async Task<IEnumerable<RiverListItemDto>> GetListAsync()
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = SqlText.GetRiverList;
        var result = await connection.QueryAsync<RiverListItemDto>(sql);

        return result;
    }

    public async Task<int> CreateAsync(RiverDto river)
    {
        using var connection = _connectionFactory.CreateConnection();

        var parameters = new DynamicParameters();
        MapRiverToParameters(river, parameters);

        var sql = SqlText.CreateRiver;
        var newId = await connection.ExecuteScalarAsync<int>(sql, parameters);

        return newId;
    }

    public async Task UpdateAsync(RiverDto river)
    {
        using var connection = _connectionFactory.CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@RiverID", river.RiverID);
        MapRiverToParameters(river, parameters);

        var sql = SqlText.UpdateRiver;
        var rowCount = await connection.ExecuteAsync(sql, parameters);

        if (rowCount == 0)
        {
            throw new RepositoryItemNotFoundException();
        }
    }

    public async Task SetActiveAsync(int riverID, bool isActive)
    {
        using var connection = _connectionFactory.CreateConnection();

        var parameters = new
        {
            RiverID = riverID,
            IsActive = isActive
        };

        var sql = SqlText.SetRiverActive;
        var rowCount = await connection.ExecuteAsync(sql, parameters);

        if (rowCount == 0)
        {
            throw new RepositoryItemNotFoundException();
        }
    }

    private static void MapRiverToParameters(RiverDto river, DynamicParameters parameters)
    {
        parameters.Add("@Name", river.Name);
        parameters.Add("@Code", river.Code?.ToUpper());
        parameters.Add("@BargeExCode", river.BargeExCode);
        parameters.Add("@StartMile", river.StartMile);
        parameters.Add("@EndMile", river.EndMile);
        parameters.Add("@IsLowToHighDirection", river.IsLowToHighDirection);
        parameters.Add("@IsActive", river.IsActive);
        parameters.Add("@UpLabel", river.UpLabel);
        parameters.Add("@DownLabel", river.DownLabel);
    }
}
