using Dapper;
using BargeOps.Shared.Dto;
using Admin.Infrastructure.Abstractions;
using Admin.Infrastructure.DataAccess;
using System.Data;

namespace Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for BoatMaintenanceLog data access using Dapper
/// ⭐ Uses DIRECT SQL QUERIES (NOT stored procedures) as per modern architecture
/// ⭐ Returns DTOs directly (no mapping needed)
/// </summary>
public class BoatMaintenanceLogRepository : IBoatMaintenanceLogRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public BoatMaintenanceLogRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<BoatMaintenanceLogDto?> GetByIdAsync(int boatMaintenanceLogId)
    {
        const string sql = @"
            SELECT
                bml.BoatMaintenanceLogID,
                bml.LocationID,
                bml.Division,
                bml.PortFacilityID,
                pf.LocationName AS PortFacility,
                bml.MaintenanceType,
                bml.StartDateTime,
                bml.Status,
                bml.Note,
                bml.BoatRoleID,
                br.BoatRole,
                bml.DeckLogActivityID,
                bml.ModifyDateTime,
                bml.ModifyUser
            FROM BoatMaintenanceLog bml
            INNER JOIN BoatLocation bl ON bl.LocationID = bml.LocationID
            LEFT JOIN Location pf ON pf.LocationID = bml.PortFacilityID
            LEFT JOIN BoatRole br ON br.BoatRoleID = bml.BoatRoleID
            WHERE bml.BoatMaintenanceLogID = @BoatMaintenanceLogId";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<BoatMaintenanceLogDto>(
            sql,
            new { BoatMaintenanceLogId = boatMaintenanceLogId }
        );
    }

    public async Task<IEnumerable<BoatMaintenanceLogDto>> GetByLocationIdAsync(int locationId)
    {
        const string sql = @"
            SELECT
                bml.BoatMaintenanceLogID,
                bml.LocationID,
                bml.Division,
                bml.PortFacilityID,
                pf.LocationName AS PortFacility,
                bml.MaintenanceType,
                bml.StartDateTime,
                bml.Status,
                bml.Note,
                bml.BoatRoleID,
                br.BoatRole,
                bml.DeckLogActivityID,
                bml.ModifyDateTime,
                bml.ModifyUser
            FROM BoatMaintenanceLog bml
            INNER JOIN BoatLocation bl ON bl.LocationID = bml.LocationID
            LEFT JOIN Location pf ON pf.LocationID = bml.PortFacilityID
            LEFT JOIN BoatRole br ON br.BoatRoleID = bml.BoatRoleID
            WHERE bml.LocationID = @LocationId
            ORDER BY bml.StartDateTime DESC, bml.BoatMaintenanceLogID DESC";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryAsync<BoatMaintenanceLogDto>(
            sql,
            new { LocationId = locationId }
        );
    }

    public async Task<BoatMaintenanceLogDto?> GetLatestByTypeAsync(int locationId, string maintenanceType)
    {
        const string sql = @"
            SELECT TOP 1
                bml.BoatMaintenanceLogID,
                bml.LocationID,
                bml.Division,
                bml.PortFacilityID,
                pf.LocationName AS PortFacility,
                bml.MaintenanceType,
                bml.StartDateTime,
                bml.Status,
                bml.Note,
                bml.BoatRoleID,
                br.BoatRole,
                bml.DeckLogActivityID,
                bml.ModifyDateTime,
                bml.ModifyUser
            FROM BoatMaintenanceLog bml
            LEFT JOIN Location pf ON pf.LocationID = bml.PortFacilityID
            LEFT JOIN BoatRole br ON br.BoatRoleID = bml.BoatRoleID
            WHERE bml.LocationID = @LocationId
              AND bml.MaintenanceType = @MaintenanceType
            ORDER BY bml.StartDateTime DESC, bml.BoatMaintenanceLogID DESC";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<BoatMaintenanceLogDto>(
            sql,
            new { LocationId = locationId, MaintenanceType = maintenanceType }
        );
    }

    public async Task<int> CreateAsync(BoatMaintenanceLogDto log)
    {
        // ⭐ Legacy behavior: Delete existing DeckLogActivity-created status with same StartDateTime
        const string deleteExistingSql = @"
            DELETE FROM BoatMaintenanceLog
            WHERE LocationID = @LocationId
              AND MaintenanceType = 'Boat Status'
              AND StartDateTime = @StartDateTime
              AND DeckLogActivityID IS NOT NULL
              AND @DeckLogActivityId IS NOT NULL";

        const string insertSql = @"
            INSERT INTO BoatMaintenanceLog (
                LocationID,
                Division,
                PortFacilityID,
                MaintenanceType,
                StartDateTime,
                Status,
                Note,
                BoatRoleID,
                DeckLogActivityID,
                CreateDateTime,
                CreateUser,
                ModifyDateTime,
                ModifyUser
            )
            VALUES (
                @LocationID,
                @Division,
                @PortFacilityID,
                @MaintenanceType,
                @StartDateTime,
                @Status,
                @Note,
                @BoatRoleID,
                @DeckLogActivityID,
                dbo.GetTenantDate(),
                @ModifyUser,
                dbo.GetTenantDate(),
                @ModifyUser
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        using var connection = await _connectionFactory.CreateConnectionAsync();

        // Delete existing if needed
        await connection.ExecuteAsync(deleteExistingSql, log);

        // Insert new record
        var newId = await connection.ExecuteScalarAsync<int>(insertSql, log);
        return newId;
    }

    public async Task UpdateAsync(BoatMaintenanceLogDto log)
    {
        // ⭐ Optimistic concurrency check using ModifyDateTime
        const string sql = @"
            UPDATE BoatMaintenanceLog
            SET
                LocationID = @LocationID,
                Division = @Division,
                PortFacilityID = @PortFacilityID,
                MaintenanceType = @MaintenanceType,
                StartDateTime = @StartDateTime,
                Status = @Status,
                Note = @Note,
                BoatRoleID = @BoatRoleID,
                DeckLogActivityID = @DeckLogActivityID,
                ModifyDateTime = dbo.GetTenantDate(),
                ModifyUser = @ModifyUser
            WHERE BoatMaintenanceLogID = @BoatMaintenanceLogID
              AND (ModifyDateTime = @ModifyDateTime OR ModifyDateTime IS NULL);

            SELECT @@ROWCOUNT;";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var rowsAffected = await connection.ExecuteScalarAsync<int>(sql, log);

        if (rowsAffected == 0)
        {
            throw new DbUpdateConcurrencyException(
                $"BoatMaintenanceLog {log.BoatMaintenanceLogID} was modified by another user. Please refresh and try again.");
        }
    }

    public async Task DeleteAsync(int boatMaintenanceLogId)
    {
        // ⭐ Hard delete with UnitTowTripDownTime cleanup (transaction)
        const string sql = @"
            BEGIN TRANSACTION;
            BEGIN TRY
                -- Nullify references in UnitTowTripDownTime
                UPDATE UnitTowTripDownTime
                SET BoatMaintenanceLogID = NULL
                WHERE BoatMaintenanceLogID = @BoatMaintenanceLogId;

                -- Delete the record
                DELETE FROM BoatMaintenanceLog
                WHERE BoatMaintenanceLogID = @BoatMaintenanceLogId;

                COMMIT TRANSACTION;
            END TRY
            BEGIN CATCH
                ROLLBACK TRANSACTION;
                THROW;
            END CATCH;";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync(sql, new { BoatMaintenanceLogId = boatMaintenanceLogId });
    }

    public async Task<IEnumerable<BoatMaintenanceLogDto>> SearchAsync(BoatMaintenanceLogSearchRequest request)
    {
        var sql = @"
            SELECT
                bml.BoatMaintenanceLogID,
                bml.LocationID,
                bml.Division,
                bml.PortFacilityID,
                pf.LocationName AS PortFacility,
                bml.MaintenanceType,
                bml.StartDateTime,
                bml.Status,
                bml.Note,
                bml.BoatRoleID,
                br.BoatRole,
                bml.DeckLogActivityID,
                bml.ModifyDateTime,
                bml.ModifyUser
            FROM BoatMaintenanceLog bml
            INNER JOIN BoatLocation bl ON bl.LocationID = bml.LocationID
            LEFT JOIN Location pf ON pf.LocationID = bml.PortFacilityID
            LEFT JOIN BoatRole br ON br.BoatRoleID = bml.BoatRoleID
            WHERE 1=1";

        var parameters = new DynamicParameters();

        if (request.LocationID.HasValue)
        {
            sql += " AND bml.LocationID = @LocationID";
            parameters.Add("LocationID", request.LocationID.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.MaintenanceType))
        {
            sql += " AND bml.MaintenanceType = @MaintenanceType";
            parameters.Add("MaintenanceType", request.MaintenanceType);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            sql += " AND bml.Status = @Status";
            parameters.Add("Status", request.Status);
        }

        if (!string.IsNullOrWhiteSpace(request.Division))
        {
            sql += " AND bml.Division = @Division";
            parameters.Add("Division", request.Division);
        }

        if (request.StartDateFrom.HasValue)
        {
            sql += " AND bml.StartDateTime >= @StartDateFrom";
            parameters.Add("StartDateFrom", request.StartDateFrom.Value);
        }

        if (request.StartDateTo.HasValue)
        {
            sql += " AND bml.StartDateTime <= @StartDateTo";
            parameters.Add("StartDateTo", request.StartDateTo.Value);
        }

        sql += " ORDER BY bml.StartDateTime DESC, bml.BoatMaintenanceLogID DESC";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryAsync<BoatMaintenanceLogDto>(sql, parameters);
    }
}
