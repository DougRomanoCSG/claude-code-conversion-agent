using Admin.Infrastructure.Abstractions;
using BargeOps.Shared.Dto;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Admin.Infrastructure.Repositories;

/// <summary>
/// Repository for Vendor operations using Dapper
/// Returns DTOs directly (no mapping needed)
/// </summary>
public class VendorRepository : IVendorRepository
{
    private readonly string _connectionString;
    private const string ConnectionStringName = "ServiceData";

    public VendorRepository(IConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        _connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Missing connection string '{ConnectionStringName}' for VendorRepository.");
    }

    public async Task<PagedResult<VendorDto>> SearchAsync(
        VendorSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var whereConditions = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            whereConditions.Add("Name LIKE @Name");
            parameters.Add("Name", $"%{EscapeSqlLikeWildcards(request.Name)}%");
        }

        if (!string.IsNullOrWhiteSpace(request.AccountingCode))
        {
            whereConditions.Add("AccountingCode LIKE @AccountingCode");
            parameters.Add("AccountingCode", $"%{EscapeSqlLikeWildcards(request.AccountingCode)}%");
        }

        if (request.IsActiveOnly.HasValue)
        {
            whereConditions.Add("IsActive = @IsActive");
            parameters.Add("IsActive", request.IsActiveOnly.Value);
        }

        if (request.FuelSuppliersOnly.HasValue && request.FuelSuppliersOnly.Value)
        {
            whereConditions.Add("IsFuelSupplier = 1");
        }

        if (request.InternalVendorOnly.HasValue && request.InternalVendorOnly.Value)
        {
            whereConditions.Add("IsInternalVendor = 1");
        }

        if (request.IsBargeExEnabledOnly.HasValue && request.IsBargeExEnabledOnly.Value)
        {
            whereConditions.Add("IsBargeExEnabled = 1");
        }

        if (request.EnablePortalOnly.HasValue && request.EnablePortalOnly.Value)
        {
            whereConditions.Add("EnablePortal = 1");
        }

        if (request.LiquidBrokerOnly.HasValue && request.LiquidBrokerOnly.Value)
        {
            whereConditions.Add("IsLiquidBroker = 1");
        }

        if (request.TankermanOnly.HasValue && request.TankermanOnly.Value)
        {
            whereConditions.Add("IsTankerman = 1");
        }

        var whereClause = whereConditions.Any()
            ? "WHERE " + string.Join(" AND ", whereConditions)
            : "";

        var totalCountSql = "SELECT COUNT(*) FROM Vendor";
        var totalCount = await connection.ExecuteScalarAsync<int>(totalCountSql);

        var filteredCountSql = $"SELECT COUNT(*) FROM Vendor {whereClause}";
        var filteredCount = await connection.ExecuteScalarAsync<int>(filteredCountSql, parameters);

        parameters.Add("Skip", request.Start);
        parameters.Add("Take", request.Length);

        var orderBy = BuildSafeOrderByClause(request.SortColumn, request.SortDirection);

        var dataSql = $@"
            SELECT
                VendorID,
                Name,
                LongName,
                AccountingCode,
                Address,
                City,
                State,
                Zip,
                PhoneNumber,
                FaxNumber,
                EmailAddress,
                TermsCode,
                IsActive,
                IsInternalVendor,
                IsLiquidBroker,
                IsTankerman,
                IsFuelSupplier,
                IsBoatAssistSupplier,
                EnablePortal,
                IsBargeExEnabled,
                BargeExTradingPartnerNum,
                BargeExConfigID
            FROM Vendor
            {whereClause}
            {orderBy}
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

        var data = await connection.QueryAsync<VendorDto>(dataSql, parameters);

        return new PagedResult<VendorDto>
        {
            Data = data.ToList(),
            TotalRecords = totalCount,
            FilteredRecords = filteredCount
        };
    }

    public async Task<VendorDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            SELECT
                VendorID,
                Name,
                LongName,
                AccountingCode,
                Address,
                City,
                State,
                Zip,
                PhoneNumber,
                FaxNumber,
                EmailAddress,
                TermsCode,
                IsActive,
                IsInternalVendor,
                IsLiquidBroker,
                IsTankerman,
                IsFuelSupplier,
                IsBoatAssistSupplier,
                EnablePortal,
                IsBargeExEnabled,
                BargeExTradingPartnerNum,
                BargeExConfigID
            FROM Vendor
            WHERE VendorID = @VendorID";

        var vendor = await connection.QueryFirstOrDefaultAsync<VendorDto>(sql, new { VendorID = id });

        if (vendor != null)
        {
            vendor.Contacts = (await GetContactsAsync(id, cancellationToken)).ToList();
            vendor.BusinessUnits = (await GetBusinessUnitsAsync(id, cancellationToken)).ToList();
            vendor.PortalGroups = (await GetPortalGroupsAsync(id, cancellationToken)).ToList();
        }

        return vendor;
    }

    public async Task<VendorDto> CreateAsync(VendorDto vendor, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            INSERT INTO Vendor (
                Name, LongName, AccountingCode, Address, City, State, Zip,
                PhoneNumber, FaxNumber, EmailAddress, TermsCode, IsActive,
                IsInternalVendor, IsLiquidBroker, IsTankerman, IsFuelSupplier,
                IsBoatAssistSupplier, EnablePortal, IsBargeExEnabled,
                BargeExTradingPartnerNum, BargeExConfigID
            )
            VALUES (
                @Name, @LongName, @AccountingCode, @Address, @City, @State, @Zip,
                @PhoneNumber, @FaxNumber, @EmailAddress, @TermsCode, @IsActive,
                @IsInternalVendor, @IsLiquidBroker, @IsTankerman, @IsFuelSupplier,
                @IsBoatAssistSupplier, @EnablePortal, @IsBargeExEnabled,
                @BargeExTradingPartnerNum, @BargeExConfigID
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var newId = await connection.ExecuteScalarAsync<int>(sql, vendor);

        return await GetByIdAsync(newId, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve created vendor");
    }

    public async Task<VendorDto> UpdateAsync(VendorDto vendor, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            UPDATE Vendor
            SET Name = @Name,
                LongName = @LongName,
                AccountingCode = @AccountingCode,
                Address = @Address,
                City = @City,
                State = @State,
                Zip = @Zip,
                PhoneNumber = @PhoneNumber,
                FaxNumber = @FaxNumber,
                EmailAddress = @EmailAddress,
                TermsCode = @TermsCode,
                IsActive = @IsActive,
                IsInternalVendor = @IsInternalVendor,
                IsLiquidBroker = @IsLiquidBroker,
                IsTankerman = @IsTankerman,
                IsFuelSupplier = @IsFuelSupplier,
                IsBoatAssistSupplier = @IsBoatAssistSupplier,
                EnablePortal = @EnablePortal,
                IsBargeExEnabled = @IsBargeExEnabled,
                BargeExTradingPartnerNum = @BargeExTradingPartnerNum,
                BargeExConfigID = @BargeExConfigID
            WHERE VendorID = @VendorID";

        await connection.ExecuteAsync(sql, vendor);

        return await GetByIdAsync(vendor.VendorID, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve updated vendor");
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            await connection.ExecuteAsync(
                "DELETE FROM VendorPortalGroup WHERE VendorID = @VendorID",
                new { VendorID = id },
                transaction);

            await connection.ExecuteAsync(
                "DELETE FROM VendorBusinessUnit WHERE VendorID = @VendorID",
                new { VendorID = id },
                transaction);

            await connection.ExecuteAsync(
                "DELETE FROM VendorContact WHERE VendorID = @VendorID",
                new { VendorID = id },
                transaction);

            var rowsAffected = await connection.ExecuteAsync(
                "DELETE FROM Vendor WHERE VendorID = @VendorID",
                new { VendorID = id },
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

    public async Task<IEnumerable<VendorContactDto>> GetContactsAsync(int vendorId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            SELECT
                VendorContactID,
                VendorID,
                Name,
                PhoneNumber,
                PhoneExt,
                EmailAddress,
                FaxNumber,
                IsPrimary,
                IsDispatcher,
                IsLiquidBroker,
                PortalUserID
            FROM VendorContact
            WHERE VendorID = @VendorID
            ORDER BY Name";

        return await connection.QueryAsync<VendorContactDto>(sql, new { VendorID = vendorId });
    }

    public async Task<VendorContactDto> CreateContactAsync(VendorContactDto contact, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            INSERT INTO VendorContact (
                VendorID, Name, PhoneNumber, PhoneExt, EmailAddress, FaxNumber,
                IsPrimary, IsDispatcher, IsLiquidBroker, PortalUserID
            )
            VALUES (
                @VendorID, @Name, @PhoneNumber, @PhoneExt, @EmailAddress, @FaxNumber,
                @IsPrimary, @IsDispatcher, @IsLiquidBroker, @PortalUserID
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var newId = await connection.ExecuteScalarAsync<int>(sql, contact);

        var selectSql = "SELECT * FROM VendorContact WHERE VendorContactID = @VendorContactID";
        return await connection.QuerySingleAsync<VendorContactDto>(selectSql, new { VendorContactID = newId });
    }

    public async Task<VendorContactDto> UpdateContactAsync(VendorContactDto contact, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            UPDATE VendorContact
            SET Name = @Name,
                PhoneNumber = @PhoneNumber,
                PhoneExt = @PhoneExt,
                EmailAddress = @EmailAddress,
                FaxNumber = @FaxNumber,
                IsPrimary = @IsPrimary,
                IsDispatcher = @IsDispatcher,
                IsLiquidBroker = @IsLiquidBroker,
                PortalUserID = @PortalUserID
            WHERE VendorContactID = @VendorContactID";

        await connection.ExecuteAsync(sql, contact);

        var selectSql = "SELECT * FROM VendorContact WHERE VendorContactID = @VendorContactID";
        return await connection.QuerySingleAsync<VendorContactDto>(selectSql, new { contact.VendorContactID });
    }

    public async Task<bool> DeleteContactAsync(int contactId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var rowsAffected = await connection.ExecuteAsync(
            "DELETE FROM VendorContact WHERE VendorContactID = @VendorContactID",
            new { VendorContactID = contactId });

        return rowsAffected > 0;
    }

    public async Task<IEnumerable<VendorBusinessUnitDto>> GetBusinessUnitsAsync(int vendorId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            SELECT
                VendorBusinessUnitID,
                VendorID,
                Name,
                AccountingCode,
                River,
                Mile,
                Bank,
                IsFuelSupplier,
                IsDefaultFuelSupplier,
                IsBoatAssistSupplier,
                MinDiscountQty,
                MinDiscountFrequency,
                IsActive
            FROM VendorBusinessUnit
            WHERE VendorID = @VendorID
            ORDER BY Name";

        return await connection.QueryAsync<VendorBusinessUnitDto>(sql, new { VendorID = vendorId });
    }

    public async Task<VendorBusinessUnitDto> CreateBusinessUnitAsync(VendorBusinessUnitDto businessUnit, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            INSERT INTO VendorBusinessUnit (
                VendorID, Name, AccountingCode, River, Mile, Bank,
                IsFuelSupplier, IsDefaultFuelSupplier, IsBoatAssistSupplier,
                MinDiscountQty, MinDiscountFrequency, IsActive
            )
            VALUES (
                @VendorID, @Name, @AccountingCode, @River, @Mile, @Bank,
                @IsFuelSupplier, @IsDefaultFuelSupplier, @IsBoatAssistSupplier,
                @MinDiscountQty, @MinDiscountFrequency, @IsActive
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var newId = await connection.ExecuteScalarAsync<int>(sql, businessUnit);

        var selectSql = "SELECT * FROM VendorBusinessUnit WHERE VendorBusinessUnitID = @VendorBusinessUnitID";
        return await connection.QuerySingleAsync<VendorBusinessUnitDto>(selectSql, new { VendorBusinessUnitID = newId });
    }

    public async Task<VendorBusinessUnitDto> UpdateBusinessUnitAsync(VendorBusinessUnitDto businessUnit, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            UPDATE VendorBusinessUnit
            SET Name = @Name,
                AccountingCode = @AccountingCode,
                River = @River,
                Mile = @Mile,
                Bank = @Bank,
                IsFuelSupplier = @IsFuelSupplier,
                IsDefaultFuelSupplier = @IsDefaultFuelSupplier,
                IsBoatAssistSupplier = @IsBoatAssistSupplier,
                MinDiscountQty = @MinDiscountQty,
                MinDiscountFrequency = @MinDiscountFrequency,
                IsActive = @IsActive
            WHERE VendorBusinessUnitID = @VendorBusinessUnitID";

        await connection.ExecuteAsync(sql, businessUnit);

        var selectSql = "SELECT * FROM VendorBusinessUnit WHERE VendorBusinessUnitID = @VendorBusinessUnitID";
        return await connection.QuerySingleAsync<VendorBusinessUnitDto>(selectSql, new { businessUnit.VendorBusinessUnitID });
    }

    public async Task<bool> DeleteBusinessUnitAsync(int businessUnitId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var rowsAffected = await connection.ExecuteAsync(
            "DELETE FROM VendorBusinessUnit WHERE VendorBusinessUnitID = @VendorBusinessUnitID",
            new { VendorBusinessUnitID = businessUnitId });

        return rowsAffected > 0;
    }

    public async Task<IEnumerable<VendorPortalGroupDto>> GetPortalGroupsAsync(int vendorId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            SELECT
                VendorPortalGroupID,
                VendorID,
                Name,
                EventHistoryDays,
                LimitBargeEvents,
                LimitDestinations,
                LimitCustomers,
                IsActive
            FROM VendorPortalGroup
            WHERE VendorID = @VendorID
            ORDER BY Name";

        return await connection.QueryAsync<VendorPortalGroupDto>(sql, new { VendorID = vendorId });
    }

    public async Task<VendorPortalGroupDto> CreatePortalGroupAsync(VendorPortalGroupDto portalGroup, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            INSERT INTO VendorPortalGroup (
                VendorID, Name, EventHistoryDays, LimitBargeEvents,
                LimitDestinations, LimitCustomers, IsActive
            )
            VALUES (
                @VendorID, @Name, @EventHistoryDays, @LimitBargeEvents,
                @LimitDestinations, @LimitCustomers, @IsActive
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var newId = await connection.ExecuteScalarAsync<int>(sql, portalGroup);

        var selectSql = "SELECT * FROM VendorPortalGroup WHERE VendorPortalGroupID = @VendorPortalGroupID";
        return await connection.QuerySingleAsync<VendorPortalGroupDto>(selectSql, new { VendorPortalGroupID = newId });
    }

    public async Task<VendorPortalGroupDto> UpdatePortalGroupAsync(VendorPortalGroupDto portalGroup, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            UPDATE VendorPortalGroup
            SET Name = @Name,
                EventHistoryDays = @EventHistoryDays,
                LimitBargeEvents = @LimitBargeEvents,
                LimitDestinations = @LimitDestinations,
                LimitCustomers = @LimitCustomers,
                IsActive = @IsActive
            WHERE VendorPortalGroupID = @VendorPortalGroupID";

        await connection.ExecuteAsync(sql, portalGroup);

        var selectSql = "SELECT * FROM VendorPortalGroup WHERE VendorPortalGroupID = @VendorPortalGroupID";
        return await connection.QuerySingleAsync<VendorPortalGroupDto>(selectSql, new { portalGroup.VendorPortalGroupID });
    }

    public async Task<bool> DeletePortalGroupAsync(int portalGroupId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var rowsAffected = await connection.ExecuteAsync(
            "DELETE FROM VendorPortalGroup WHERE VendorPortalGroupID = @VendorPortalGroupID",
            new { VendorPortalGroupID = portalGroupId });

        return rowsAffected > 0;
    }

    private static string BuildSafeOrderByClause(string? sortColumn, string? sortDirection)
    {
        var direction = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";

        var key = sortColumn?.Trim();
        var keyLower = key?.ToLowerInvariant();

        var columnExpr = keyLower switch
        {
            "vendorid" => "VendorID",
            "name" => "Name",
            "longname" => "LongName",
            "accountingcode" => "AccountingCode",
            "isactive" => "IsActive",
            "isinternalvendor" => "IsInternalVendor",
            "isfuelsupplier" => "IsFuelSupplier",
            "isboatassistsupplier" => "IsBoatAssistSupplier",
            "isliquidbroker" => "IsLiquidBroker",
            "istankerman" => "IsTankerman",
            "isbargeexenabled" => "IsBargeExEnabled",
            "enableportal" => "EnablePortal",
            _ => "Name"
        };

        return $"ORDER BY {columnExpr} {direction}";
    }

    private static string EscapeSqlLikeWildcards(string input)
    {
        return input
            .Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]");
    }
}
