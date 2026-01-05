using Admin.Infrastructure.Abstractions;
using BargeOps.Shared.Dto;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Admin.Infrastructure.Repositories;

/// <summary>
/// Repository for BargeEvent (TicketEvent) operations using Dapper.
/// Uses DIRECT SQL QUERIES (not stored procedures).
/// Returns DTOs directly (no mapping needed).
/// </summary>
public class BargeEventRepository : IBargeEventRepository
{
    private readonly string _connectionString;
    private const string ConnectionStringName = "ServiceData";

    public BargeEventRepository(IConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        _connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Missing connection string '{ConnectionStringName}' for BargeEventRepository.");
    }

    // ===== READ OPERATIONS =====

    public async Task<BargeEventDto?> GetByIdAsync(int ticketEventId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            SELECT
                te.*,
                et.EventName as EventTypeName,
                c.CustomerName as BillingCustomerName,
                b.BoatName as FleetBoatName,
                fromLoc.Name as FromLocationName,
                toLoc.Name as ToLocationName,
                cmd.CommodityName,
                v.VendorName
            FROM TicketEvent te
            LEFT JOIN EventType et ON te.EventTypeID = et.EventTypeID
            LEFT JOIN Customer c ON te.BillingCustomerID = c.CustomerID
            LEFT JOIN Boat b ON te.FleetBoatID = b.BoatID
            LEFT JOIN Location fromLoc ON te.FromLocationID = fromLoc.LocationID
            LEFT JOIN Location toLoc ON te.ToLocationID = toLoc.LocationID
            LEFT JOIN Commodity cmd ON te.CommodityID = cmd.CommodityID
            LEFT JOIN Vendor v ON te.VendorID = v.VendorID
            WHERE te.TicketEventID = @TicketEventID";

        return await connection.QuerySingleOrDefaultAsync<BargeEventDto>(
            sql,
            new { TicketEventID = ticketEventId });
    }

    public async Task<IEnumerable<BargeEventDto>> GetByTicketIdAsync(int ticketId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            SELECT
                te.*,
                et.EventName as EventTypeName,
                c.CustomerName as BillingCustomerName,
                b.BoatName as FleetBoatName,
                fromLoc.Name as FromLocationName,
                toLoc.Name as ToLocationName,
                cmd.CommodityName,
                v.VendorName
            FROM TicketEvent te
            LEFT JOIN EventType et ON te.EventTypeID = et.EventTypeID
            LEFT JOIN Customer c ON te.BillingCustomerID = c.CustomerID
            LEFT JOIN Boat b ON te.FleetBoatID = b.BoatID
            LEFT JOIN Location fromLoc ON te.FromLocationID = fromLoc.LocationID
            LEFT JOIN Location toLoc ON te.ToLocationID = toLoc.LocationID
            LEFT JOIN Commodity cmd ON te.CommodityID = cmd.CommodityID
            LEFT JOIN Vendor v ON te.VendorID = v.VendorID
            WHERE te.TicketID = @TicketID
            ORDER BY te.StartDateTime";

        return await connection.QueryAsync<BargeEventDto>(
            sql,
            new { TicketID = ticketId });
    }

    // ===== SEARCH OPERATIONS =====

    public async Task<PagedResult<BargeEventSearchDto>> SearchAsync(
        BargeEventSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var whereConditions = new List<string> { "t.FleetID = @FleetID" };
        var parameters = new DynamicParameters();
        parameters.Add("FleetID", request.FleetID);

        // Build WHERE clause dynamically
        if (request.EventTypeId.HasValue)
        {
            whereConditions.Add("te.EventTypeID = @EventTypeId");
            parameters.Add("EventTypeId", request.EventTypeId.Value);
        }

        if (request.BillingCustomerId.HasValue)
        {
            whereConditions.Add("te.BillingCustomerID = @BillingCustomerId");
            parameters.Add("BillingCustomerId", request.BillingCustomerId.Value);
        }

        if (request.FromLocationId.HasValue)
        {
            whereConditions.Add("te.FromLocationID = @FromLocationId");
            parameters.Add("FromLocationId", request.FromLocationId.Value);
        }

        if (request.ToLocationId.HasValue)
        {
            whereConditions.Add("te.ToLocationID = @ToLocationId");
            parameters.Add("ToLocationId", request.ToLocationId.Value);
        }

        if (request.FleetBoatId.HasValue)
        {
            whereConditions.Add("te.FleetBoatID = @FleetBoatId");
            parameters.Add("FleetBoatId", request.FleetBoatId.Value);
        }

        if (request.StartDate.HasValue)
        {
            whereConditions.Add("te.StartDateTime >= @StartDate");
            parameters.Add("StartDate", request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            whereConditions.Add("te.StartDateTime <= @EndDate");
            parameters.Add("EndDate", request.EndDate.Value);
        }

        if (!request.IncludeVoided)
        {
            whereConditions.Add("te.VoidStatus = 0");
        }

        if (!string.IsNullOrWhiteSpace(request.ContractNumber))
        {
            whereConditions.Add("fc.ContractNumber LIKE @ContractNumber");
            parameters.Add("ContractNumber", $"%{EscapeSqlLikeWildcards(request.ContractNumber)}%");
        }

        if (!string.IsNullOrWhiteSpace(request.BargeNumberList))
        {
            // Split comma-separated barge numbers and create 'begins with' conditions
            var bargeNumbers = request.BargeNumberList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (bargeNumbers.Any())
            {
                var bargeConditions = bargeNumbers.Select((num, idx) => $"b.BargeNum LIKE @BargeNum{idx}").ToList();
                whereConditions.Add($"({string.Join(" OR ", bargeConditions)})");

                for (int i = 0; i < bargeNumbers.Length; i++)
                {
                    parameters.Add($"BargeNum{i}", $"{EscapeSqlLikeWildcards(bargeNumbers[i])}%");
                }
            }
        }

        if (request.TicketCustomerId.HasValue)
        {
            whereConditions.Add("t.CustomerID = @TicketCustomerId");
            parameters.Add("TicketCustomerId", request.TicketCustomerId.Value);
        }

        if (request.FreightCustomerId.HasValue)
        {
            whereConditions.Add("fc.FreightCustomerID = @FreightCustomerId");
            parameters.Add("FreightCustomerId", request.FreightCustomerId.Value);
        }

        if (request.EventRateId.HasValue)
        {
            whereConditions.Add("te.EventRateID = @EventRateId");
            parameters.Add("EventRateId", request.EventRateId.Value);
        }

        var whereClause = "WHERE " + string.Join(" AND ", whereConditions);

        // Count queries for DataTables
        var totalCountSql = @"
            SELECT COUNT(DISTINCT te.TicketEventID)
            FROM TicketEvent te
            INNER JOIN Ticket t ON te.TicketID = t.TicketID
            INNER JOIN Barge b ON t.BargeID = b.BargeID
            LEFT JOIN FreightContract fc ON b.FreightContractID = fc.FreightContractID
            WHERE t.FleetID = @FleetID";

        var totalCount = await connection.ExecuteScalarAsync<int>(totalCountSql, new { request.FleetID });

        var filteredCountSql = $@"
            SELECT COUNT(DISTINCT te.TicketEventID)
            FROM TicketEvent te
            INNER JOIN Ticket t ON te.TicketID = t.TicketID
            INNER JOIN Barge b ON t.BargeID = b.BargeID
            LEFT JOIN FreightContract fc ON b.FreightContractID = fc.FreightContractID
            {whereClause}";

        var filteredCount = await connection.ExecuteScalarAsync<int>(filteredCountSql, parameters);

        // Build ORDER BY clause
        var orderBy = !string.IsNullOrWhiteSpace(request.SortBy)
            ? $"ORDER BY {SanitizeSortColumn(request.SortBy)} {(request.SortDescending ? "DESC" : "ASC")}"
            : "ORDER BY te.StartDateTime DESC";

        // Paged data query
        parameters.Add("Skip", request.Start);
        parameters.Add("Take", request.Length);

        var dataSql = $@"
            SELECT
                te.TicketEventID,
                te.EventTypeID,
                et.EventName,
                b.BargeID,
                b.BargeNum,
                te.TicketID,
                t.FleetID,
                te.StartDateTime,
                te.CompleteDateTime,
                te.CpDateTime,
                te.ReleaseDateTime,
                fromLoc.Name as StartLocation,
                toLoc.Name as EndLocation,
                te.LoadStatus,
                cmd.CommodityName,
                CASE
                    WHEN fc.LoadUnloadTons IS NOT NULL THEN fc.LoadUnloadTons
                    ELSE te.LoadUnloadTons
                END as LoadUnloadTons,
                te.IsDefaultTons,
                billingCust.CustomerName as CustomerName,
                ticketCust.CustomerName as TicketCustomerName,
                freightCust.CustomerName as FreightCustomerName,
                fc.ContractNumber,
                fcOrigin.Name as FreightOrigin,
                fcDest.Name as FreightDestination,
                boat.BoatName as ServicingBoat,
                te.Division,
                v.VendorName as Vendor,
                vbu.BusinessUnitName as VendorBusinessUnit,
                te.SchedStartDateTime as SchedTime,
                te.StartDateTime as EventTime,
                te.EventRateID,
                CASE WHEN te.InvoiceID IS NOT NULL THEN 1 ELSE 0 END as IsInvoiced,
                te.Rebill,
                CASE WHEN te.VoidStatus > 0 THEN 1 ELSE 0 END as Void,
                te.IsPortShift
            FROM TicketEvent te
            INNER JOIN Ticket t ON te.TicketID = t.TicketID
            INNER JOIN Barge b ON t.BargeID = b.BargeID
            INNER JOIN EventType et ON te.EventTypeID = et.EventTypeID
            LEFT JOIN Customer billingCust ON te.BillingCustomerID = billingCust.CustomerID
            LEFT JOIN Customer ticketCust ON t.CustomerID = ticketCust.CustomerID
            LEFT JOIN FreightContract fc ON b.FreightContractID = fc.FreightContractID
            LEFT JOIN Customer freightCust ON fc.FreightCustomerID = freightCust.CustomerID
            LEFT JOIN Location fromLoc ON te.FromLocationID = fromLoc.LocationID
            LEFT JOIN Location toLoc ON te.ToLocationID = toLoc.LocationID
            LEFT JOIN Location fcOrigin ON fc.OriginLocationID = fcOrigin.LocationID
            LEFT JOIN Location fcDest ON fc.DestinationLocationID = fcDest.LocationID
            LEFT JOIN Boat boat ON te.FleetBoatID = boat.BoatID
            LEFT JOIN Commodity cmd ON te.CommodityID = cmd.CommodityID
            LEFT JOIN Vendor v ON te.VendorID = v.VendorID
            LEFT JOIN VendorBusinessUnit vbu ON te.VendorBusinessUnitID = vbu.BusinessUnitID
            {whereClause}
            {orderBy}
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

        var items = await connection.QueryAsync<BargeEventSearchDto>(dataSql, parameters);

        return new PagedResult<BargeEventSearchDto>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            FilteredCount = filteredCount,
            Page = request.Start / request.Length + 1,
            PageSize = request.Length
        };
    }

    public async Task<PagedResult<BargeEventBillingDto>> BillingSearchAsync(
        BargeEventBillingSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        // TODO: Implement billing search with financial data
        // This is a complex query that joins multiple billing-related tables
        // See conversion-plan.md for detailed requirements

        throw new NotImplementedException("BillingSearchAsync - implement based on business requirements");
    }

    // ===== WRITE OPERATIONS =====

    public async Task<BargeEventDto> CreateAsync(BargeEventDto bargeEvent, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            INSERT INTO TicketEvent (
                TicketID, EventTypeID, Status, BillableStatus, VoidStatus,
                StartDateTime, CompleteDateTime, SchedStartDateTime, SchedCompleteDateTime,
                FromLocationID, ToLocationID, BillingCustomerID, FleetBoatID, CommodityID,
                LoadStatus, EventRateID, RateType, BaseRate, ChargeType, Minimum, MinimumAmount,
                ProrateUnits, FreeHours, HighWaterRate, HighWaterAmount,
                FuelEscalationRate, FuelEscalationAmount, LaborEscalationRate, LaborEscalationAmount,
                BaseAmount, TotalAmount, InvoiceNote, VendorID, VendorBusinessUnitID, Division,
                CpDateTime, ReleaseDateTime, FreightRate, FreightQuantity, FreightAmount,
                IsManualFreightRate, FreightTotalAmount, FreightInvoiceNote,
                BargeDraft, DraftRequested, DraftCalculated, DraftAverage, AdjustmentTons,
                BargesInTowCount, DivideByBargesInTow, TowingTons, TowingDistanceMiles,
                CoverType, CoverConfig, CoverSubTypeID, TrainNum, Surveyor, ShipID, RigID,
                IsDefaultTons, Rebill, RebillOverride, IsPortShift,
                CreateDateTime, CreateUser, ModifyDateTime, ModifyUser
            )
            VALUES (
                @TicketID, @EventTypeID, @Status, @BillableStatus, @VoidStatus,
                @StartDateTime, @CompleteDateTime, @SchedStartDateTime, @SchedCompleteDateTime,
                @FromLocationID, @ToLocationID, @BillingCustomerID, @FleetBoatID, @CommodityID,
                @LoadStatus, @EventRateID, @RateType, @BaseRate, @ChargeType, @Minimum, @MinimumAmount,
                @ProrateUnits, @FreeHours, @HighWaterRate, @HighWaterAmount,
                @FuelEscalationRate, @FuelEscalationAmount, @LaborEscalationRate, @LaborEscalationAmount,
                @BaseAmount, @TotalAmount, @InvoiceNote, @VendorID, @VendorBusinessUnitID, @Division,
                @CpDateTime, @ReleaseDateTime, @FreightRate, @FreightQuantity, @FreightAmount,
                @IsManualFreightRate, @FreightTotalAmount, @FreightInvoiceNote,
                @BargeDraft, @DraftRequested, @DraftCalculated, @DraftAverage, @AdjustmentTons,
                @BargesInTowCount, @DivideByBargesInTow, @TowingTons, @TowingDistanceMiles,
                @CoverType, @CoverConfig, @CoverSubTypeID, @TrainNum, @Surveyor, @ShipID, @RigID,
                @IsDefaultTons, @Rebill, @RebillOverride, @IsPortShift,
                GETDATE(), @CreateUser, GETDATE(), @ModifyUser
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var id = await connection.ExecuteScalarAsync<int>(sql, bargeEvent);
        bargeEvent.TicketEventID = id;

        return bargeEvent;
    }

    public async Task<BargeEventDto> UpdateAsync(BargeEventDto bargeEvent, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            UPDATE TicketEvent SET
                TicketID = @TicketID,
                EventTypeID = @EventTypeID,
                Status = @Status,
                BillableStatus = @BillableStatus,
                VoidStatus = @VoidStatus,
                StartDateTime = @StartDateTime,
                CompleteDateTime = @CompleteDateTime,
                SchedStartDateTime = @SchedStartDateTime,
                SchedCompleteDateTime = @SchedCompleteDateTime,
                FromLocationID = @FromLocationID,
                ToLocationID = @ToLocationID,
                BillingCustomerID = @BillingCustomerID,
                FleetBoatID = @FleetBoatID,
                CommodityID = @CommodityID,
                LoadStatus = @LoadStatus,
                EventRateID = @EventRateID,
                RateType = @RateType,
                BaseRate = @BaseRate,
                ChargeType = @ChargeType,
                Minimum = @Minimum,
                MinimumAmount = @MinimumAmount,
                ProrateUnits = @ProrateUnits,
                FreeHours = @FreeHours,
                HighWaterRate = @HighWaterRate,
                HighWaterAmount = @HighWaterAmount,
                FuelEscalationRate = @FuelEscalationRate,
                FuelEscalationAmount = @FuelEscalationAmount,
                LaborEscalationRate = @LaborEscalationRate,
                LaborEscalationAmount = @LaborEscalationAmount,
                BaseAmount = @BaseAmount,
                TotalAmount = @TotalAmount,
                InvoiceNote = @InvoiceNote,
                VendorID = @VendorID,
                VendorBusinessUnitID = @VendorBusinessUnitID,
                Division = @Division,
                CpDateTime = @CpDateTime,
                ReleaseDateTime = @ReleaseDateTime,
                FreightRate = @FreightRate,
                FreightQuantity = @FreightQuantity,
                FreightAmount = @FreightAmount,
                IsManualFreightRate = @IsManualFreightRate,
                FreightTotalAmount = @FreightTotalAmount,
                FreightInvoiceNote = @FreightInvoiceNote,
                BargeDraft = @BargeDraft,
                DraftRequested = @DraftRequested,
                DraftCalculated = @DraftCalculated,
                DraftAverage = @DraftAverage,
                AdjustmentTons = @AdjustmentTons,
                BargesInTowCount = @BargesInTowCount,
                DivideByBargesInTow = @DivideByBargesInTow,
                TowingTons = @TowingTons,
                TowingDistanceMiles = @TowingDistanceMiles,
                CoverType = @CoverType,
                CoverConfig = @CoverConfig,
                CoverSubTypeID = @CoverSubTypeID,
                TrainNum = @TrainNum,
                Surveyor = @Surveyor,
                ShipID = @ShipID,
                RigID = @RigID,
                IsDefaultTons = @IsDefaultTons,
                Rebill = @Rebill,
                RebillOverride = @RebillOverride,
                IsPortShift = @IsPortShift,
                ModifyDateTime = GETDATE(),
                ModifyUser = @ModifyUser
            WHERE TicketEventID = @TicketEventID";

        await connection.ExecuteAsync(sql, bargeEvent);

        return bargeEvent;
    }

    public async Task<bool> SetVoidStatusAsync(int ticketEventId, byte voidStatus, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            UPDATE TicketEvent
            SET VoidStatus = @VoidStatus,
                ModifyDateTime = GETDATE()
            WHERE TicketEventID = @TicketEventID";

        var rowsAffected = await connection.ExecuteAsync(sql, new { TicketEventID = ticketEventId, VoidStatus = voidStatus });

        return rowsAffected > 0;
    }

    // ===== REBILLING OPERATIONS =====

    public async Task<int> MarkForRebillAsync(IEnumerable<int> ticketEventIds, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            UPDATE TicketEvent
            SET Rebill = 1,
                ModifyDateTime = GETDATE()
            WHERE TicketEventID IN @TicketEventIds";

        return await connection.ExecuteAsync(sql, new { TicketEventIds = ticketEventIds });
    }

    public async Task<int> UnmarkForRebillAsync(IEnumerable<int> ticketEventIds, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            UPDATE TicketEvent
            SET Rebill = 0,
                ModifyDateTime = GETDATE()
            WHERE TicketEventID IN @TicketEventIds";

        return await connection.ExecuteAsync(sql, new { TicketEventIds = ticketEventIds });
    }

    // ===== CHILD ENTITY OPERATIONS =====

    public async Task<IEnumerable<BargeDto>> GetBargesAsync(int ticketEventId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            SELECT b.*
            FROM Barge b
            INNER JOIN Ticket t ON b.BargeID = t.BargeID
            INNER JOIN TicketEvent te ON t.TicketID = te.TicketID
            WHERE te.TicketEventID = @TicketEventID";

        return await connection.QueryAsync<BargeDto>(sql, new { TicketEventID = ticketEventId });
    }

    public async Task<IEnumerable<BillingAuditDto>> GetBillingAuditsAsync(int ticketEventId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            SELECT
                ba.AuditID,
                ba.ChangeDateTime as ChangeDate,
                ba.UserName,
                ba.FieldName,
                ba.OldValue,
                ba.NewValue
            FROM BillingAudit ba
            WHERE ba.TicketEventID = @TicketEventID
            ORDER BY ba.ChangeDateTime DESC";

        return await connection.QueryAsync<BillingAuditDto>(sql, new { TicketEventID = ticketEventId });
    }

    // ===== HELPER METHODS =====

    private static string EscapeSqlLikeWildcards(string input)
    {
        return input.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
    }

    private static string SanitizeSortColumn(string sortBy)
    {
        // Whitelist allowed sort columns to prevent SQL injection
        var allowedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "EventName", "BargeNum", "TicketID", "StartDateTime", "CompleteDateTime",
            "CustomerName", "Division", "ServicingBoat", "LoadStatus", "CommodityName"
        };

        return allowedColumns.Contains(sortBy) ? sortBy : "StartDateTime";
    }
}
