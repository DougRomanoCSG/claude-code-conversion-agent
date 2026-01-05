using System.Data;
using Admin.Infrastructure.Abstractions;
using BargeOps.Shared.Dto;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for BargeSeries data access using Dapper.
/// Returns DTOs directly - NO mapping layer needed!
/// </summary>
public class BargeSeriesRepository : IBargeSeriesRepository
{
    private readonly string _connectionString;

    public BargeSeriesRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    /// <inheritdoc />
    public async Task<PagedResult<BargeSeriesDto>> SearchAsync(
        BargeSeriesSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Build dynamic WHERE clause based on filters
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            whereClauses.Add("bs.Name LIKE @Name");
            parameters.Add("Name", $"%{request.Name}%");
        }

        if (request.CustomerID.HasValue && request.CustomerID.Value > 0)
        {
            whereClauses.Add("bs.CustomerID = @CustomerID");
            parameters.Add("CustomerID", request.CustomerID.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.HullType))
        {
            whereClauses.Add("bs.HullType = @HullType");
            parameters.Add("HullType", request.HullType);
        }

        if (!string.IsNullOrWhiteSpace(request.CoverType))
        {
            whereClauses.Add("bs.CoverType = @CoverType");
            parameters.Add("CoverType", request.CoverType);
        }

        if (request.ActiveOnly)
        {
            whereClauses.Add("bs.IsActive = 1");
        }

        var whereClause = whereClauses.Any()
            ? "WHERE " + string.Join(" AND ", whereClauses)
            : "";

        // Count total records
        var countSql = $@"
            SELECT COUNT(*)
            FROM BargeSeries bs
            {whereClause}";

        var totalRecords = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        // Get paged data with sorting
        var sortColumn = GetSortColumn(request.SortColumn);
        var sortDirection = request.SortDirection?.ToLower() == "desc" ? "DESC" : "ASC";

        parameters.Add("Offset", request.Start);
        parameters.Add("PageSize", request.Length);

        var dataSql = $@"
            SELECT
                bs.BargeSeriesID,
                bs.CustomerID,
                c.CustomerName,
                bs.Name,
                bs.HullType,
                bs.CoverType,
                bs.Length,
                bs.Width,
                bs.Depth,
                bs.TonsPerInch,
                bs.DraftLight,
                bs.IsActive
            FROM BargeSeries bs
            LEFT JOIN Customer c ON bs.CustomerID = c.CustomerID
            {whereClause}
            ORDER BY {sortColumn} {sortDirection}
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY";

        var data = await connection.QueryAsync<BargeSeriesDto>(dataSql, parameters);

        return new PagedResult<BargeSeriesDto>
        {
            Data = data.ToList(),
            RecordsTotal = totalRecords,
            RecordsFiltered = totalRecords
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BargeSeriesDto>> GetListAsync(
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT
                BargeSeriesID,
                CustomerID,
                Name,
                HullType,
                CoverType,
                Length,
                Width,
                Depth,
                TonsPerInch,
                DraftLight,
                IsActive
            FROM BargeSeries
            WHERE IsActive = 1
            ORDER BY Name";

        return await connection.QueryAsync<BargeSeriesDto>(sql);
    }

    /// <inheritdoc />
    public async Task<BargeSeriesDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Get parent BargeSeries
        const string parentSql = @"
            SELECT
                bs.BargeSeriesID,
                bs.CustomerID,
                c.CustomerName,
                bs.Name,
                bs.HullType,
                bs.CoverType,
                bs.Length,
                bs.Width,
                bs.Depth,
                bs.TonsPerInch,
                bs.DraftLight,
                bs.IsActive
            FROM BargeSeries bs
            LEFT JOIN Customer c ON bs.CustomerID = c.CustomerID
            WHERE bs.BargeSeriesID = @BargeSeriesID";

        var bargeSeries = await connection.QuerySingleOrDefaultAsync<BargeSeriesDto>(
            parentSql,
            new { BargeSeriesID = id });

        if (bargeSeries == null)
            return null;

        // Get child draft records
        const string draftsSql = @"
            SELECT
                BargeSeriesDraftID,
                BargeSeriesID,
                DraftFeet,
                Tons00, Tons01, Tons02, Tons03, Tons04, Tons05,
                Tons06, Tons07, Tons08, Tons09, Tons10, Tons11
            FROM BargeSeriesDraft
            WHERE BargeSeriesID = @BargeSeriesID
            ORDER BY DraftFeet";

        var drafts = await connection.QueryAsync<BargeSeriesDraftDto>(
            draftsSql,
            new { BargeSeriesID = id });

        bargeSeries.Drafts = drafts.ToList();

        return bargeSeries;
    }

    /// <inheritdoc />
    public async Task<BargeSeriesDto> CreateAsync(
        BargeSeriesDto bargeSeries,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var transaction = connection.BeginTransaction();
        try
        {
            // Insert parent BargeSeries
            const string insertParentSql = @"
                INSERT INTO BargeSeries (
                    CustomerID, Name, HullType, CoverType,
                    Length, Width, Depth, TonsPerInch, DraftLight, IsActive
                )
                VALUES (
                    @CustomerID, @Name, @HullType, @CoverType,
                    @Length, @Width, @Depth, @TonsPerInch, @DraftLight, @IsActive
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var newId = await connection.ExecuteScalarAsync<int>(
                insertParentSql,
                bargeSeries,
                transaction);

            bargeSeries.BargeSeriesID = newId;

            // Insert child draft records
            if (bargeSeries.Drafts?.Any() == true)
            {
                foreach (var draft in bargeSeries.Drafts)
                {
                    draft.BargeSeriesID = newId;
                    await InsertDraftAsync(connection, transaction, draft);
                }
            }

            transaction.Commit();

            // Return the created entity with all data
            return await GetByIdAsync(newId, cancellationToken)
                ?? throw new InvalidOperationException("Failed to retrieve created BargeSeries.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<BargeSeriesDto> UpdateAsync(
        BargeSeriesDto bargeSeries,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var transaction = connection.BeginTransaction();
        try
        {
            // Update parent BargeSeries
            const string updateParentSql = @"
                UPDATE BargeSeries
                SET CustomerID = @CustomerID,
                    Name = @Name,
                    HullType = @HullType,
                    CoverType = @CoverType,
                    Length = @Length,
                    Width = @Width,
                    Depth = @Depth,
                    TonsPerInch = @TonsPerInch,
                    DraftLight = @DraftLight,
                    IsActive = @IsActive
                WHERE BargeSeriesID = @BargeSeriesID";

            await connection.ExecuteAsync(updateParentSql, bargeSeries, transaction);

            // Delete existing draft records and re-insert
            // (Simpler than trying to determine which records changed)
            const string deleteDraftsSql = @"
                DELETE FROM BargeSeriesDraft
                WHERE BargeSeriesID = @BargeSeriesID";

            await connection.ExecuteAsync(
                deleteDraftsSql,
                new { bargeSeries.BargeSeriesID },
                transaction);

            // Insert updated draft records
            if (bargeSeries.Drafts?.Any() == true)
            {
                foreach (var draft in bargeSeries.Drafts)
                {
                    draft.BargeSeriesID = bargeSeries.BargeSeriesID;
                    await InsertDraftAsync(connection, transaction, draft);
                }
            }

            transaction.Commit();

            // Return the updated entity
            return await GetByIdAsync(bargeSeries.BargeSeriesID, cancellationToken)
                ?? throw new InvalidOperationException("Failed to retrieve updated BargeSeries.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            UPDATE BargeSeries
            SET IsActive = @IsActive
            WHERE BargeSeriesID = @BargeSeriesID";

        var rowsAffected = await connection.ExecuteAsync(
            sql,
            new { BargeSeriesID = id, IsActive = isActive });

        return rowsAffected > 0;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BargeSeriesDraftDto>> GetDraftsAsync(
        int bargeSeriesId,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT
                BargeSeriesDraftID,
                BargeSeriesID,
                DraftFeet,
                Tons00, Tons01, Tons02, Tons03, Tons04, Tons05,
                Tons06, Tons07, Tons08, Tons09, Tons10, Tons11
            FROM BargeSeriesDraft
            WHERE BargeSeriesID = @BargeSeriesID
            ORDER BY DraftFeet";

        return await connection.QueryAsync<BargeSeriesDraftDto>(
            sql,
            new { BargeSeriesID = bargeSeriesId });
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BargeSeriesDraftDto>> UpsertDraftsAsync(
        int bargeSeriesId,
        IEnumerable<BargeSeriesDraftDto> drafts,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var transaction = connection.BeginTransaction();
        try
        {
            // Delete all existing drafts for this series
            const string deleteSql = @"
                DELETE FROM BargeSeriesDraft
                WHERE BargeSeriesID = @BargeSeriesID";

            await connection.ExecuteAsync(
                deleteSql,
                new { BargeSeriesID = bargeSeriesId },
                transaction);

            // Insert all drafts
            foreach (var draft in drafts)
            {
                draft.BargeSeriesID = bargeSeriesId;
                await InsertDraftAsync(connection, transaction, draft);
            }

            transaction.Commit();

            // Return updated drafts
            return await GetDraftsAsync(bargeSeriesId, cancellationToken);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    #region Private Helper Methods

    private static async Task InsertDraftAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        BargeSeriesDraftDto draft)
    {
        const string sql = @"
            INSERT INTO BargeSeriesDraft (
                BargeSeriesID, DraftFeet,
                Tons00, Tons01, Tons02, Tons03, Tons04, Tons05,
                Tons06, Tons07, Tons08, Tons09, Tons10, Tons11
            )
            VALUES (
                @BargeSeriesID, @DraftFeet,
                @Tons00, @Tons01, @Tons02, @Tons03, @Tons04, @Tons05,
                @Tons06, @Tons07, @Tons08, @Tons09, @Tons10, @Tons11
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var newId = await connection.ExecuteScalarAsync<int>(sql, draft, transaction);
        draft.BargeSeriesDraftID = newId;
    }

    private static string GetSortColumn(string? sortColumn)
    {
        // Map client-side column names to database column names
        return sortColumn?.ToLower() switch
        {
            "name" => "bs.Name",
            "customername" => "c.CustomerName",
            "customerid" => "bs.CustomerID",
            "hulltype" => "bs.HullType",
            "covertype" => "bs.CoverType",
            "length" => "bs.Length",
            "width" => "bs.Width",
            "depth" => "bs.Depth",
            "tonsperinch" => "bs.TonsPerInch",
            "draftlight" => "bs.DraftLight",
            "isactive" => "bs.IsActive",
            _ => "bs.Name" // Default sort
        };
    }

    #endregion
}
