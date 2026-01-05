using Admin.Infrastructure.Abstractions;
using BargeOps.Shared.Dto;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Text;

namespace Admin.Infrastructure.Repositories;

/// <summary>
/// Repository for Barge operations using Dapper
/// ⭐ Uses DIRECT SQL QUERIES (not stored procedures)
/// ⭐ Returns DTOs directly (no mapping needed)
/// </summary>
public class BargeRepository : IBargeRepository
{
    private readonly string _connectionString;
    private const string ConnectionStringName = "ServiceData";

    public BargeRepository(IConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        _connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Missing connection string '{ConnectionStringName}' for BargeRepository.");
    }

    #region Search

    public async Task<PagedResult<BargeDto>> SearchAsync(
        BargeSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var whereConditions = new List<string>();
        var parameters = new DynamicParameters();

        // Build WHERE clause dynamically based on search criteria

        // Basic criteria
        if (!string.IsNullOrWhiteSpace(request.BargeNum))
        {
            whereConditions.Add("b.BargeNum LIKE @BargeNum");
            parameters.Add("BargeNum", $"{EscapeSqlLikeWildcards(request.BargeNum)}%");
        }

        if (!string.IsNullOrWhiteSpace(request.HullType))
        {
            whereConditions.Add("b.HullType = @HullType");
            parameters.Add("HullType", request.HullType);
        }

        if (!string.IsNullOrWhiteSpace(request.CoverType))
        {
            whereConditions.Add("b.CoverType = @CoverType");
            parameters.Add("CoverType", request.CoverType);
        }

        if (request.OperatorID.HasValue)
        {
            whereConditions.Add("b.CustomerID = @OperatorID");
            parameters.Add("OperatorID", request.OperatorID.Value);
        }

        if (request.CustomerID.HasValue)
        {
            whereConditions.Add("t.CustomerID = @CustomerID");
            parameters.Add("CustomerID", request.CustomerID.Value);
        }

        if (request.ActiveOnly)
        {
            whereConditions.Add("b.IsActive = 1");
        }

        if (request.TicketID.HasValue)
        {
            whereConditions.Add("t.TicketID = @TicketID");
            parameters.Add("TicketID", request.TicketID.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.LoadStatus))
        {
            whereConditions.Add("b.LoadStatus = @LoadStatus");
            parameters.Add("LoadStatus", request.LoadStatus);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            whereConditions.Add("b.Status = @Status");
            parameters.Add("Status", request.Status);
        }

        if (request.OpenTicketsOnly)
        {
            whereConditions.Add("(t.TicketID IS NOT NULL AND t.IsActive = 1)");
        }

        // Advanced criteria
        if (!string.IsNullOrWhiteSpace(request.EquipmentType))
        {
            whereConditions.Add("b.EquipmentType = @EquipmentType");
            parameters.Add("EquipmentType", request.EquipmentType);
        }

        if (!string.IsNullOrWhiteSpace(request.UscgNum))
        {
            whereConditions.Add("b.UscgNum LIKE @UscgNum");
            parameters.Add("UscgNum", $"{EscapeSqlLikeWildcards(request.UscgNum)}%");
        }

        if (!string.IsNullOrWhiteSpace(request.SizeCategory))
        {
            whereConditions.Add("b.SizeCategory = @SizeCategory");
            parameters.Add("SizeCategory", request.SizeCategory);
        }

        if (!string.IsNullOrWhiteSpace(request.River))
        {
            whereConditions.Add("bl.River = @River");
            parameters.Add("River", request.River);
        }

        if (request.StartMile.HasValue && request.EndMile.HasValue)
        {
            whereConditions.Add("bl.Mile BETWEEN @StartMile AND @EndMile");
            parameters.Add("StartMile", request.StartMile.Value);
            parameters.Add("EndMile", request.EndMile.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.ContractNumber))
        {
            whereConditions.Add("t.ContractNumber LIKE @ContractNumber");
            parameters.Add("ContractNumber", $"{EscapeSqlLikeWildcards(request.ContractNumber)}%");
        }

        if (request.CommodityID.HasValue)
        {
            whereConditions.Add("b.CommodityID = @CommodityID");
            parameters.Add("CommodityID", request.CommodityID.Value);
        }

        // Boat search filters
        if (request.IsInTow.HasValue && request.IsInTow.Value)
        {
            whereConditions.Add("EXISTS (SELECT 1 FROM BoatBarge bb WHERE bb.BargeID = b.BargeID AND bb.IsActive = 1)");
        }

        if (request.IsScheduledIn.HasValue && request.IsScheduledIn.Value)
        {
            whereConditions.Add("t.ScheduleInID IS NOT NULL");
        }

        if (request.IsScheduledOut.HasValue && request.IsScheduledOut.Value)
        {
            whereConditions.Add("t.ScheduleOutID IS NOT NULL");
        }

        if (request.BoatLocationID.HasValue)
        {
            whereConditions.Add(@"
                (b.LocationID = @BoatLocationID
                 OR EXISTS (SELECT 1 FROM BoatBarge bb
                            INNER JOIN Boat bt ON bb.BoatID = bt.BoatID
                            WHERE bb.BargeID = b.BargeID
                            AND bt.LocationID = @BoatLocationID
                            AND bb.IsActive = 1))");
            parameters.Add("BoatLocationID", request.BoatLocationID.Value);
        }

        // Facility search filters
        if (request.IsAtFacility.HasValue && request.IsAtFacility.Value)
        {
            whereConditions.Add("EXISTS (SELECT 1 FROM FacilityLocation fl WHERE fl.LocationID = b.LocationID)");
        }

        if (request.IsConsignedToFacility.HasValue && request.IsConsignedToFacility.Value)
        {
            whereConditions.Add("t.ConsignLocationID IS NOT NULL AND EXISTS (SELECT 1 FROM FacilityLocation fl WHERE fl.LocationID = t.ConsignLocationID)");
        }

        if (request.IsDestinationIn.HasValue && request.IsDestinationIn.Value)
        {
            whereConditions.Add("t.DestinationInID IS NOT NULL");
        }

        if (request.IsDestinationOut.HasValue && request.IsDestinationOut.Value)
        {
            whereConditions.Add("t.DestinationOutID IS NOT NULL");
        }

        if (request.IsOnOrderToFacility.HasValue && request.IsOnOrderToFacility.Value)
        {
            whereConditions.Add("t.IsOnOrder = 1");
        }

        if (request.FacilityLocationID.HasValue)
        {
            whereConditions.Add(@"
                (b.LocationID = @FacilityLocationID
                 OR t.ConsignLocationID = @FacilityLocationID
                 OR t.DestinationInID = @FacilityLocationID
                 OR t.DestinationOutID = @FacilityLocationID)");
            parameters.Add("FacilityLocationID", request.FacilityLocationID.Value);
        }

        // Ship search filters
        if (request.IsConsignedToShip.HasValue && request.IsConsignedToShip.Value)
        {
            whereConditions.Add("t.ConsignShipID IS NOT NULL");
        }

        if (request.IsOnOrderToShip.HasValue && request.IsOnOrderToShip.Value)
        {
            whereConditions.Add("t.IsOnOrder = 1 AND t.ConsignShipID IS NOT NULL");
        }

        if (request.ShipLocationID.HasValue)
        {
            whereConditions.Add("EXISTS (SELECT 1 FROM Ship s WHERE s.ShipID = t.ConsignShipID AND s.LocationID = @ShipLocationID)");
            parameters.Add("ShipLocationID", request.ShipLocationID.Value);
        }

        var whereClause = whereConditions.Any()
            ? "WHERE " + string.Join(" AND ", whereConditions)
            : "";

        // Count queries for DataTables
        var totalCountSql = "SELECT COUNT(*) FROM Barge b";
        var totalCount = await connection.ExecuteScalarAsync<int>(totalCountSql);

        var filteredCountSql = $@"
            SELECT COUNT(*)
            FROM Barge b
            LEFT JOIN Ticket t ON b.BargeID = t.BargeID AND t.IsActive = 1
            LEFT JOIN Location bl ON b.LocationID = bl.LocationID
            {whereClause}";

        var filteredCount = await connection.ExecuteScalarAsync<int>(filteredCountSql, parameters);

        // Get paged data
        parameters.Add("Skip", request.Start);
        parameters.Add("Take", request.Length);

        var orderBy = BuildSafeOrderByClause(request.SortColumn, request.SortDirection);

        var dataSql = $@"
            SELECT
                b.BargeID,
                b.BargeNum,
                b.HullType,
                b.CoverType,
                b.SizeCategory,
                b.CoverConfig,
                b.LoadStatus,
                b.CleanStatus,
                b.RepairStatus,
                b.DamageLevel,
                b.IsLeaker,
                b.EquipmentType,
                b.Status,
                b.IsActive,
                b.LocationID,
                bl.Name AS LocationName,
                bl.River AS LocationRiver,
                bl.Mile AS LocationMile,
                cust.Name AS CustomerName,
                owner.Name AS OwnerName,
                fleet.Name AS FleetName,
                comm.Name AS CommodityName,
                t.TicketID,
                t.Status AS TicketStatus,
                t.InspectionStatus,
                t.IsOnHold,
                t.IsOnOrder,
                t.OnOrderScheduleDateTime,
                t.OnOrderTripNumber,
                t.IsAwaitingPickup,
                t.AwaitingPickupReadyDateTime,
                destIn.Name AS DestinationInName,
                destOut.Name AS DestinationOutName,
                schedIn.Name AS ScheduleInName,
                schedOut.Name AS ScheduleOutName,
                consignLoc.Name AS ConsignLocationName,
                consignShip.Name AS ConsignShipName,
                tier.Name AS TierName,
                CASE WHEN b.TierX IS NOT NULL AND b.TierY IS NOT NULL
                     THEN CAST(b.TierX AS VARCHAR) + ',' + CAST(b.TierY AS VARCHAR)
                     ELSE NULL END AS TierXY
            FROM Barge b
            LEFT JOIN Ticket t ON b.BargeID = t.BargeID AND t.IsActive = 1
            LEFT JOIN Location bl ON b.LocationID = bl.LocationID
            LEFT JOIN Customer cust ON b.CustomerID = cust.CustomerID
            LEFT JOIN Customer owner ON b.OwnerID = owner.CustomerID
            LEFT JOIN Fleet fleet ON b.FleetID = fleet.FleetID
            LEFT JOIN Commodity comm ON b.CommodityID = comm.CommodityID
            LEFT JOIN Location destIn ON t.DestinationInID = destIn.LocationID
            LEFT JOIN Location destOut ON t.DestinationOutID = destOut.LocationID
            LEFT JOIN Location schedIn ON t.ScheduleInID = schedIn.LocationID
            LEFT JOIN Location schedOut ON t.ScheduleOutID = schedOut.LocationID
            LEFT JOIN Location consignLoc ON t.ConsignLocationID = consignLoc.LocationID
            LEFT JOIN Ship consignShip ON t.ConsignShipID = consignShip.ShipID
            LEFT JOIN Tier tier ON b.TierID = tier.TierID
            {whereClause}
            {orderBy}
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

        var data = await connection.QueryAsync<BargeDto>(dataSql, parameters);

        return new PagedResult<BargeDto>
        {
            Data = data.ToList(),
            TotalCount = totalCount,
            FilteredCount = filteredCount,
            Draw = request.Draw
        };
    }

    #endregion

    #region Get By ID

    public async Task<BargeDto?> GetByIdAsync(
        int bargeId,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            SELECT
                b.*,
                cust.Name AS CustomerName,
                owner.Name AS OwnerName,
                fleet.Name AS FleetName,
                loc.Name AS LocationName,
                comm.Name AS CommodityName,
                bs.Name AS BargeSeriesName,
                cp.Description AS ColorPairDescription,
                cst.Name AS CoverSubTypeName,
                tier.Name AS TierName,
                fb.Name AS FacilityBerthName,
                fleetBoat.Name AS FleetBoatName,
                b.FleetBoatActivityDateTime,
                t.InspectionStatus,
                t.IsOnHold,
                t.DestinationInName,
                t.LoadFirst,
                t.LoadLast,
                t.LoadMultipleLots,
                t.DestinationOutName,
                t.ScheduleInName,
                t.IsOnOrder,
                t.OnOrderScheduleDateTime,
                t.OnOrderTripNumber,
                t.ScheduleOutName,
                t.IsAwaitingPickup,
                t.AwaitingPickupReadyDateTime,
                CONCAT(COALESCE(consignLoc.Name, ''),
                       CASE WHEN consignLoc.Name IS NOT NULL AND t.ConsignedTo IS NOT NULL THEN ', ' ELSE '' END,
                       COALESCE(t.ConsignedTo, '')) AS ConsignLocationName
            FROM Barge b
            LEFT JOIN Customer cust ON b.CustomerID = cust.CustomerID
            LEFT JOIN Customer owner ON b.OwnerID = owner.CustomerID
            LEFT JOIN Fleet fleet ON b.FleetID = fleet.FleetID
            LEFT JOIN Location loc ON b.LocationID = loc.LocationID
            LEFT JOIN Commodity comm ON b.CommodityID = comm.CommodityID
            LEFT JOIN BargeSeries bs ON b.BargeSeriesID = bs.BargeSeriesID
            LEFT JOIN ColorPair cp ON b.ColorPairID = cp.ColorPairID
            LEFT JOIN CoverSubType cst ON b.CoverSubTypeID = cst.CoverSubTypeID
            LEFT JOIN Tier tier ON b.TierID = tier.TierID
            LEFT JOIN FacilityBerth fb ON b.FacilityBerthID = fb.FacilityBerthID
            LEFT JOIN FleetBoat fleetBoat ON b.FleetBoatID = fleetBoat.FleetBoatID
            LEFT JOIN Ticket t ON b.BargeID = t.BargeID AND t.IsActive = 1
            LEFT JOIN Location consignLoc ON t.ConsignLocationID = consignLoc.LocationID
            WHERE b.BargeID = @BargeID";

        return await connection.QuerySingleOrDefaultAsync<BargeDto>(sql, new { BargeID = bargeId });
    }

    #endregion

    #region Create

    public async Task<int> CreateAsync(
        BargeDto barge,
        string userName,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            INSERT INTO Barge (
                BargeNum, UscgNum, HullNumber, GlAccountNum,
                HullType, ExternalLength, ExternalWidth, ExternalDepth, SizeCategory,
                CoverType, CoverConfig, CoverSubTypeID, BargeType,
                Draft, DraftPortBow, DraftPortStern, DraftStarboardBow, DraftStarboardStern, DraftCalculated,
                EquipmentType, FleetID, OwnerID, CustomerID, LocationID, LocationDateTime,
                LoadStatus, CommodityID, CleanStatus, ColorPairID,
                HasInsufficientFreeboard, FreeboardRange,
                InServiceDate, OutOfServiceDate,
                RepairStatus, DamageLevel, DamageNote, IsDamaged, IsCargoDamaged, IsLeaker, IsDryDocked, IsRepairScheduled,
                BargeSeriesID,
                TierID, TierX, TierY, FacilityBerthID, FacilityBerthX, FacilityBerthY,
                RakeDirection, TowString, TowCut, BargeSyncIdentifier, HasAnchorWire, ConvFesBargeID,
                IsActive, CreateDateTime, ModifyDateTime, CreateUser, ModifyUser
            )
            VALUES (
                @BargeNum, @UscgNum, @HullNumber, @GlAccountNum,
                @HullType, @ExternalLength, @ExternalWidth, @ExternalDepth, @SizeCategory,
                @CoverType, @CoverConfig, @CoverSubTypeID, @BargeType,
                @Draft, @DraftPortBow, @DraftPortStern, @DraftStarboardBow, @DraftStarboardStern, @DraftCalculated,
                @EquipmentType, @FleetID, @OwnerID, @CustomerID, @LocationID, @LocationDateTime,
                @LoadStatus, @CommodityID, @CleanStatus, @ColorPairID,
                @HasInsufficientFreeboard, @FreeboardRange,
                @InServiceDate, @OutOfServiceDate,
                @RepairStatus, @DamageLevel, @DamageNote, @IsDamaged, @IsCargoDamaged, @IsLeaker, @IsDryDocked, @IsRepairScheduled,
                @BargeSeriesID,
                @TierID, @TierX, @TierY, @FacilityBerthID, @FacilityBerthX, @FacilityBerthY,
                @RakeDirection, @TowString, @TowCut, @BargeSyncIdentifier, @HasAnchorWire, @ConvFesBargeID,
                @IsActive, GETDATE(), GETDATE(), @CreateUser, @ModifyUser
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var parameters = new DynamicParameters(barge);
        parameters.Add("CreateUser", userName);
        parameters.Add("ModifyUser", userName);

        return await connection.ExecuteScalarAsync<int>(sql, parameters);
    }

    #endregion

    #region Update

    public async Task<bool> UpdateAsync(
        BargeDto barge,
        string userName,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            UPDATE Barge SET
                BargeNum = @BargeNum,
                UscgNum = @UscgNum,
                HullNumber = @HullNumber,
                GlAccountNum = @GlAccountNum,
                HullType = @HullType,
                ExternalLength = @ExternalLength,
                ExternalWidth = @ExternalWidth,
                ExternalDepth = @ExternalDepth,
                SizeCategory = @SizeCategory,
                CoverType = @CoverType,
                CoverConfig = @CoverConfig,
                CoverSubTypeID = @CoverSubTypeID,
                BargeType = @BargeType,
                Draft = @Draft,
                DraftPortBow = @DraftPortBow,
                DraftPortStern = @DraftPortStern,
                DraftStarboardBow = @DraftStarboardBow,
                DraftStarboardStern = @DraftStarboardStern,
                DraftCalculated = @DraftCalculated,
                EquipmentType = @EquipmentType,
                FleetID = @FleetID,
                OwnerID = @OwnerID,
                CustomerID = @CustomerID,
                LocationID = @LocationID,
                LocationDateTime = @LocationDateTime,
                LoadStatus = @LoadStatus,
                CommodityID = @CommodityID,
                CleanStatus = @CleanStatus,
                ColorPairID = @ColorPairID,
                HasInsufficientFreeboard = @HasInsufficientFreeboard,
                FreeboardRange = @FreeboardRange,
                InServiceDate = @InServiceDate,
                OutOfServiceDate = @OutOfServiceDate,
                RepairStatus = @RepairStatus,
                DamageLevel = @DamageLevel,
                DamageNote = @DamageNote,
                IsDamaged = @IsDamaged,
                IsCargoDamaged = @IsCargoDamaged,
                IsLeaker = @IsLeaker,
                IsDryDocked = @IsDryDocked,
                IsRepairScheduled = @IsRepairScheduled,
                BargeSeriesID = @BargeSeriesID,
                TierID = @TierID,
                TierX = @TierX,
                TierY = @TierY,
                FacilityBerthID = @FacilityBerthID,
                FacilityBerthX = @FacilityBerthX,
                FacilityBerthY = @FacilityBerthY,
                RakeDirection = @RakeDirection,
                TowString = @TowString,
                TowCut = @TowCut,
                HasAnchorWire = @HasAnchorWire,
                IsActive = @IsActive,
                ModifyDateTime = GETDATE(),
                ModifyUser = @ModifyUser
            WHERE BargeID = @BargeID";

        var parameters = new DynamicParameters(barge);
        parameters.Add("ModifyUser", userName);

        var rowsAffected = await connection.ExecuteAsync(sql, parameters);
        return rowsAffected > 0;
    }

    #endregion

    #region Delete

    public async Task<bool> DeleteAsync(
        int bargeId,
        string userName,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            UPDATE Barge
            SET IsActive = 0,
                ModifyDateTime = GETDATE(),
                ModifyUser = @ModifyUser
            WHERE BargeID = @BargeID";

        var rowsAffected = await connection.ExecuteAsync(sql, new { BargeID = bargeId, ModifyUser = userName });
        return rowsAffected > 0;
    }

    #endregion

    #region Barge Charters

    public async Task<List<BargeCharterDto>> GetBargeChartersAsync(
        int bargeId,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            SELECT
                bc.*,
                c.Name AS ChartererCustomerName
            FROM BargeCharter bc
            INNER JOIN Customer c ON bc.ChartererCustomerID = c.CustomerID
            WHERE bc.BargeID = @BargeID
            ORDER BY bc.StartDate DESC";

        var result = await connection.QueryAsync<BargeCharterDto>(sql, new { BargeID = bargeId });
        return result.ToList();
    }

    public async Task<int> CreateCharterAsync(
        BargeCharterDto charter,
        string userName,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            INSERT INTO BargeCharter (
                BargeID, ChartererCustomerID, StartDate, EndDate, Rate, CharterCode, Notes,
                CreateDateTime, ModifyDateTime, CreateUser, ModifyUser
            )
            VALUES (
                @BargeID, @ChartererCustomerID, @StartDate, @EndDate, @Rate, @CharterCode, @Notes,
                GETDATE(), GETDATE(), @CreateUser, @ModifyUser
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var parameters = new DynamicParameters(charter);
        parameters.Add("CreateUser", userName);
        parameters.Add("ModifyUser", userName);

        return await connection.ExecuteScalarAsync<int>(sql, parameters);
    }

    public async Task<bool> UpdateCharterAsync(
        BargeCharterDto charter,
        string userName,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            UPDATE BargeCharter SET
                ChartererCustomerID = @ChartererCustomerID,
                StartDate = @StartDate,
                EndDate = @EndDate,
                Rate = @Rate,
                CharterCode = @CharterCode,
                Notes = @Notes,
                ModifyDateTime = GETDATE(),
                ModifyUser = @ModifyUser
            WHERE BargeCharterID = @BargeCharterID";

        var parameters = new DynamicParameters(charter);
        parameters.Add("ModifyUser", userName);

        var rowsAffected = await connection.ExecuteAsync(sql, parameters);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteCharterAsync(
        int charterId,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = "DELETE FROM BargeCharter WHERE BargeCharterID = @BargeCharterID";
        var rowsAffected = await connection.ExecuteAsync(sql, new { BargeCharterID = charterId });
        return rowsAffected > 0;
    }

    #endregion

    #region Update Location

    public async Task<bool> UpdateLocationAsync(
        int bargeId,
        int locationId,
        DateTime locationDateTime,
        short? tierX = null,
        short? tierY = null,
        short? facilityBerthX = null,
        short? facilityBerthY = null,
        string? userName = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        // Update location, clear tier/berth if location changed
        var sql = @"
            UPDATE Barge SET
                LocationID = @LocationID,
                LocationDateTime = @LocationDateTime,
                TierX = @TierX,
                TierY = @TierY,
                FacilityBerthX = @FacilityBerthX,
                FacilityBerthY = @FacilityBerthY,
                ModifyDateTime = GETDATE(),
                ModifyUser = @ModifyUser
            WHERE BargeID = @BargeID";

        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            BargeID = bargeId,
            LocationID = locationId,
            LocationDateTime = locationDateTime,
            TierX = tierX,
            TierY = tierY,
            FacilityBerthX = facilityBerthX,
            FacilityBerthY = facilityBerthY,
            ModifyUser = userName ?? "System"
        });

        return rowsAffected > 0;
    }

    #endregion

    #region Helper Methods

    public async Task<bool> BargeNumExistsAsync(
        string bargeNum,
        int? excludeBargeId = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = excludeBargeId.HasValue
            ? "SELECT COUNT(*) FROM Barge WHERE LOWER(BargeNum) = LOWER(@BargeNum) AND BargeID != @ExcludeBargeID"
            : "SELECT COUNT(*) FROM Barge WHERE LOWER(BargeNum) = LOWER(@BargeNum)";

        var count = await connection.ExecuteScalarAsync<int>(sql, new { BargeNum = bargeNum, ExcludeBargeID = excludeBargeId });
        return count > 0;
    }

    private string BuildSafeOrderByClause(string? sortColumn, string? sortDirection)
    {
        // Map DataTables column keys to safe SQL column names
        var allowedColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "bargeNum", "b.BargeNum" },
            { "hullType", "b.HullType" },
            { "coverType", "b.CoverType" },
            { "sizeCategory", "b.SizeCategory" },
            { "loadStatus", "b.LoadStatus" },
            { "status", "b.Status" },
            { "equipmentType", "b.EquipmentType" },
            { "locationName", "bl.Name" },
            { "customerName", "cust.Name" },
            { "isActive", "b.IsActive" },
            { "createDateTime", "b.CreateDateTime" },
            { "modifyDateTime", "b.ModifyDateTime" }
        };

        var columnToSort = "b.BargeNum"; // Default sort
        if (!string.IsNullOrWhiteSpace(sortColumn) && allowedColumns.ContainsKey(sortColumn))
        {
            columnToSort = allowedColumns[sortColumn];
        }

        var direction = sortDirection?.ToLower() == "desc" ? "DESC" : "ASC";
        return $"ORDER BY {columnToSort} {direction}";
    }

    private static string EscapeSqlLikeWildcards(string input)
    {
        return input
            .Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]");
    }

    #endregion
}
