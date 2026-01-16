using Admin.Infrastructure.Abstractions;
using BargeOps.Shared.Dto;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Admin.Infrastructure.Repositories;

/// <summary>
/// Repository for Vendor operations using Dapper
/// Uses DIRECT SQL QUERIES (not stored procedures)
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

    #region Vendor CRUD

    public async Task<PagedResult<VendorDto>> SearchAsync(
        VendorSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var whereConditions = new List<string>();
        var parameters = new DynamicParameters();

        // Build WHERE clause based on search criteria
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

        if (request.IsActive.HasValue)
        {
            whereConditions.Add("IsActive = @IsActive");
            parameters.Add("IsActive", request.IsActive.Value);
        }

        if (request.IsFuelSupplier.HasValue && request.IsFuelSupplier.Value)
        {
            whereConditions.Add("IsFuelSupplier = 1");
        }

        if (request.IsInternalVendor.HasValue && request.IsInternalVendor.Value)
        {
            whereConditions.Add("IsInternalVendor = 1");
        }

        if (request.IsBargeExEnabled.HasValue && request.IsBargeExEnabled.Value)
        {
            whereConditions.Add("IsBargeExEnabled = 1");
        }

        if (request.EnablePortal.HasValue && request.EnablePortal.Value)
        {
            whereConditions.Add("EnablePortal = 1");
        }

        if (request.IsLiquidBroker.HasValue && request.IsLiquidBroker.Value)
        {
            whereConditions.Add("IsLiquidBroker = 1");
        }

        if (request.IsTankerman.HasValue && request.IsTankerman.Value)
        {
            whereConditions.Add("IsTankerman = 1");
        }

        var whereClause = whereConditions.Any()
            ? "WHERE " + string.Join(" AND ", whereConditions)
            : "";

        // Get total count (all records)
        var totalCountSql = "SELECT COUNT(*) FROM Vendor";
        var totalCount = await connection.ExecuteScalarAsync<int>(totalCountSql);

        // Get filtered count
        var filteredCountSql = $"SELECT COUNT(*) FROM Vendor {whereClause}";
        var filteredCount = await connection.ExecuteScalarAsync<int>(filteredCountSql, parameters);

        // Get paged data
        parameters.Add("Skip", request.Start);
        parameters.Add("Take", request.Length);

        var orderByColumn = request.GetSortColumn() ?? "Name";
        var orderByDirection = request.GetSortDirection();

        var dataSql = $@"
            SELECT
                VendorID,
                Name,
                LongName,
                AccountingCode,
                IsActive,
                IsFuelSupplier,
                IsBoatAssistSupplier,
                IsInternalVendor,
                IsBargeExEnabled,
                EnablePortal,
                IsLiquidBroker,
                IsTankerman,
                Address,
                City,
                State,
                Zip,
                PhoneNumber,
                FaxNumber,
                EmailAddress,
                TermsCode,
                BargeExTradingPartnerNum,
                BargeExConfigID,
                CreateDateTime,
                CreateUser,
                ModifyDateTime,
                ModifyUser
            FROM Vendor
            {whereClause}
            ORDER BY {orderByColumn} {orderByDirection}
            OFFSET @Skip ROWS
            FETCH NEXT @Take ROWS ONLY";

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
                IsActive,
                IsFuelSupplier,
                IsBoatAssistSupplier,
                IsInternalVendor,
                IsBargeExEnabled,
                EnablePortal,
                IsLiquidBroker,
                IsTankerman,
                Address,
                City,
                State,
                Zip,
                PhoneNumber,
                FaxNumber,
                EmailAddress,
                TermsCode,
                BargeExTradingPartnerNum,
                BargeExConfigID,
                CreateDateTime,
                CreateUser,
                ModifyDateTime,
                ModifyUser
            FROM Vendor
            WHERE VendorID = @VendorID";

        return await connection.QuerySingleOrDefaultAsync<VendorDto>(sql, new { VendorID = id });
    }

    public async Task<int> CreateAsync(VendorDto vendor, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            INSERT INTO Vendor (
                Name, LongName, AccountingCode, IsActive,
                IsFuelSupplier, IsBoatAssistSupplier, IsInternalVendor,
                IsBargeExEnabled, EnablePortal, IsLiquidBroker, IsTankerman,
                Address, City, State, Zip,
                PhoneNumber, FaxNumber, EmailAddress, TermsCode,
                BargeExTradingPartnerNum, BargeExConfigID,
                CreateDateTime, CreateUser
            )
            VALUES (
                @Name, @LongName, @AccountingCode, @IsActive,
                @IsFuelSupplier, @IsBoatAssistSupplier, @IsInternalVendor,
                @IsBargeExEnabled, @EnablePortal, @IsLiquidBroker, @IsTankerman,
                @Address, @City, @State, @Zip,
                @PhoneNumber, @FaxNumber, @EmailAddress, @TermsCode,
                @BargeExTradingPartnerNum, @BargeExConfigID,
                GETDATE(), @CreateUser
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        return await connection.ExecuteScalarAsync<int>(sql, vendor);
    }

    public async Task UpdateAsync(VendorDto vendor, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            UPDATE Vendor
            SET
                Name = @Name,
                LongName = @LongName,
                AccountingCode = @AccountingCode,
                IsActive = @IsActive,
                IsFuelSupplier = @IsFuelSupplier,
                IsBoatAssistSupplier = @IsBoatAssistSupplier,
                IsInternalVendor = @IsInternalVendor,
                IsBargeExEnabled = @IsBargeExEnabled,
                EnablePortal = @EnablePortal,
                IsLiquidBroker = @IsLiquidBroker,
                IsTankerman = @IsTankerman,
                Address = @Address,
                City = @City,
                State = @State,
                Zip = @Zip,
                PhoneNumber = @PhoneNumber,
                FaxNumber = @FaxNumber,
                EmailAddress = @EmailAddress,
                TermsCode = @TermsCode,
                BargeExTradingPartnerNum = @BargeExTradingPartnerNum,
                BargeExConfigID = @BargeExConfigID,
                ModifyDateTime = GETDATE(),
                ModifyUser = @ModifyUser
            WHERE VendorID = @VendorID";

        await connection.ExecuteAsync(sql, vendor);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = "DELETE FROM Vendor WHERE VendorID = @VendorID";
        var rowsAffected = await connection.ExecuteAsync(sql, new { VendorID = id });

        return rowsAffected > 0;
    }

    #endregion

    #region Vendor Contacts

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
                PortalUserID,
                CreateDateTime,
                CreateUser,
                ModifyDateTime,
                ModifyUser
            FROM VendorContact
            WHERE VendorID = @VendorID
            ORDER BY Name";

        return await connection.QueryAsync<VendorContactDto>(sql, new { VendorID = vendorId });
    }

    public async Task<int> CreateContactAsync(VendorContactDto contact, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            INSERT INTO VendorContact (
                VendorID, Name, PhoneNumber, PhoneExt, EmailAddress, FaxNumber,
                IsPrimary, IsDispatcher, IsLiquidBroker, PortalUserID,
                CreateDateTime, CreateUser
            )
            VALUES (
                @VendorID, @Name, @PhoneNumber, @PhoneExt, @EmailAddress, @FaxNumber,
                @IsPrimary, @IsDispatcher, @IsLiquidBroker, @PortalUserID,
                GETDATE(), @CreateUser
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        return await connection.ExecuteScalarAsync<int>(sql, contact);
    }

    public async Task UpdateContactAsync(VendorContactDto contact, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            UPDATE VendorContact
            SET
                Name = @Name,
                PhoneNumber = @PhoneNumber,
                PhoneExt = @PhoneExt,
                EmailAddress = @EmailAddress,
                FaxNumber = @FaxNumber,
                IsPrimary = @IsPrimary,
                IsDispatcher = @IsDispatcher,
                IsLiquidBroker = @IsLiquidBroker,
                PortalUserID = @PortalUserID,
                ModifyDateTime = GETDATE(),
                ModifyUser = @ModifyUser
            WHERE VendorContactID = @VendorContactID";

        await connection.ExecuteAsync(sql, contact);
    }

    public async Task<bool> DeleteContactAsync(int contactId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = "DELETE FROM VendorContact WHERE VendorContactID = @VendorContactID";
        var rowsAffected = await connection.ExecuteAsync(sql, new { VendorContactID = contactId });

        return rowsAffected > 0;
    }

    #endregion

    #region Vendor Business Units

    public async Task<IEnumerable<VendorBusinessUnitDto>> GetBusinessUnitsAsync(int vendorId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            SELECT
                VendorBusinessUnitID,
                VendorID,
                Name,
                AccountingCode,
                IsActive,
                River,
                Mile,
                Bank,
                IsFuelSupplier,
                IsDefaultFuelSupplier,
                IsBoatAssistSupplier,
                MinDiscountQty,
                MinDiscountFrequency,
                CreateDateTime,
                CreateUser,
                ModifyDateTime,
                ModifyUser
            FROM VendorBusinessUnit
            WHERE VendorID = @VendorID
            ORDER BY Name";

        return await connection.QueryAsync<VendorBusinessUnitDto>(sql, new { VendorID = vendorId });
    }

    public async Task<int> CreateBusinessUnitAsync(VendorBusinessUnitDto businessUnit, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            INSERT INTO VendorBusinessUnit (
                VendorID, Name, AccountingCode, IsActive,
                River, Mile, Bank,
                IsFuelSupplier, IsDefaultFuelSupplier, IsBoatAssistSupplier,
                MinDiscountQty, MinDiscountFrequency,
                CreateDateTime, CreateUser
            )
            VALUES (
                @VendorID, @Name, @AccountingCode, @IsActive,
                @River, @Mile, @Bank,
                @IsFuelSupplier, @IsDefaultFuelSupplier, @IsBoatAssistSupplier,
                @MinDiscountQty, @MinDiscountFrequency,
                GETDATE(), @CreateUser
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        return await connection.ExecuteScalarAsync<int>(sql, businessUnit);
    }

    public async Task UpdateBusinessUnitAsync(VendorBusinessUnitDto businessUnit, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            UPDATE VendorBusinessUnit
            SET
                Name = @Name,
                AccountingCode = @AccountingCode,
                IsActive = @IsActive,
                River = @River,
                Mile = @Mile,
                Bank = @Bank,
                IsFuelSupplier = @IsFuelSupplier,
                IsDefaultFuelSupplier = @IsDefaultFuelSupplier,
                IsBoatAssistSupplier = @IsBoatAssistSupplier,
                MinDiscountQty = @MinDiscountQty,
                MinDiscountFrequency = @MinDiscountFrequency,
                ModifyDateTime = GETDATE(),
                ModifyUser = @ModifyUser
            WHERE VendorBusinessUnitID = @VendorBusinessUnitID";

        await connection.ExecuteAsync(sql, businessUnit);
    }

    public async Task<bool> DeleteBusinessUnitAsync(int businessUnitId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = "DELETE FROM VendorBusinessUnit WHERE VendorBusinessUnitID = @VendorBusinessUnitID";
        var rowsAffected = await connection.ExecuteAsync(sql, new { VendorBusinessUnitID = businessUnitId });

        return rowsAffected > 0;
    }

    #endregion

    #region Helper Methods

    private static string EscapeSqlLikeWildcards(string value)
    {
        return value
            .Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]");
    }

    #endregion
}
