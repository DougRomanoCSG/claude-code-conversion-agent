using Dapper;
using BargeOps.Shared.Dto;
using BargeOps.Shared.Models;
using Admin.Domain.Repositories;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;

namespace Admin.Infrastructure.Repositories;

/// <summary>
/// Vendor repository using Dapper with DIRECT SQL QUERIES (NOT stored procedures)
/// Returns DTOs directly - NO mapping needed!
///
/// Reference: FacilityRepository.cs, BoatLocationRepository.cs for patterns
/// </summary>
public class VendorRepository : IVendorRepository
{
    private readonly string _connectionString;

    public VendorRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");
    }

    #region Vendor CRUD

    public async Task<PagedResult<VendorDto>> SearchAsync(VendorSearchRequest request, int page, int pageSize)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = new StringBuilder(@"
            SELECT v.*,
                   CAST(CASE WHEN EXISTS(SELECT 1 FROM VendorBusinessUnit vbu WHERE vbu.VendorID = v.VendorID AND vbu.IsFuelSupplier = 1) THEN 1 ELSE 0 END AS BIT) AS IsFuelSupplier,
                   CAST(CASE WHEN EXISTS(SELECT 1 FROM VendorBusinessUnit vbu WHERE vbu.VendorID = v.VendorID AND vbu.IsBoatAssistSupplier = 1) THEN 1 ELSE 0 END AS BIT) AS IsBoatAssistSupplier
            FROM Vendor v
            WHERE 1=1");

        var parameters = new DynamicParameters();

        // Apply search filters
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            sql.Append(" AND v.Name LIKE @Name");
            parameters.Add("Name", $"%{request.Name}%");
        }

        if (!string.IsNullOrWhiteSpace(request.AccountingCode))
        {
            sql.Append(" AND v.AccountingCode LIKE @AccountingCode");
            parameters.Add("AccountingCode", $"%{request.AccountingCode}%");
        }

        if (request.ActiveOnly)
        {
            sql.Append(" AND v.IsActive = 1");
        }

        if (request.FuelSuppliersOnly)
        {
            sql.Append(" AND EXISTS(SELECT 1 FROM VendorBusinessUnit vbu WHERE vbu.VendorID = v.VendorID AND vbu.IsFuelSupplier = 1)");
        }

        if (request.InternalVendorOnly)
        {
            sql.Append(" AND v.IsInternalVendor = 1");
        }

        if (request.IsBargeExEnabledOnly)
        {
            sql.Append(" AND v.IsBargeExEnabled = 1");
        }

        if (request.EnablePortalOnly)
        {
            sql.Append(" AND v.EnablePortal = 1");
        }

        if (request.LiquidBrokerOnly)
        {
            sql.Append(" AND v.IsLiquidBroker = 1");
        }

        if (request.TankermanOnly)
        {
            sql.Append(" AND v.IsTankerman = 1");
        }

        // Get total count
        var countSql = $"SELECT COUNT(*) FROM ({sql}) AS CountQuery";
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        // Add paging
        sql.Append(" ORDER BY v.Name OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY");
        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        var vendors = await connection.QueryAsync<VendorDto>(sql.ToString(), parameters);

        return new PagedResult<VendorDto>
        {
            Items = vendors.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<DataTableResponse<VendorDto>> GetDataTableAsync(DataTableRequest request, VendorSearchRequest searchCriteria)
    {
        using var connection = new SqlConnection(_connectionString);

        // Base query
        var sql = new StringBuilder(@"
            SELECT v.VendorID, v.Name, v.LongName, v.AccountingCode, v.IsActive, v.EnablePortal,
                   v.IsBargeExEnabled, v.IsLiquidBroker, v.IsInternalVendor, v.IsTankerman,
                   CAST(CASE WHEN EXISTS(SELECT 1 FROM VendorBusinessUnit vbu WHERE vbu.VendorID = v.VendorID AND vbu.IsFuelSupplier = 1) THEN 1 ELSE 0 END AS BIT) AS IsFuelSupplier,
                   CAST(CASE WHEN EXISTS(SELECT 1 FROM VendorBusinessUnit vbu WHERE vbu.VendorID = v.VendorID AND vbu.IsBoatAssistSupplier = 1) THEN 1 ELSE 0 END AS BIT) AS IsBoatAssistSupplier
            FROM Vendor v
            WHERE 1=1");

        var parameters = new DynamicParameters();

        // Apply search criteria filters (same as SearchAsync)
        if (!string.IsNullOrWhiteSpace(searchCriteria.Name))
        {
            sql.Append(" AND v.Name LIKE @Name");
            parameters.Add("Name", $"%{searchCriteria.Name}%");
        }

        if (!string.IsNullOrWhiteSpace(searchCriteria.AccountingCode))
        {
            sql.Append(" AND v.AccountingCode LIKE @AccountingCode");
            parameters.Add("AccountingCode", $"%{searchCriteria.AccountingCode}%");
        }

        if (searchCriteria.ActiveOnly)
        {
            sql.Append(" AND v.IsActive = 1");
        }

        if (searchCriteria.FuelSuppliersOnly)
        {
            sql.Append(" AND EXISTS(SELECT 1 FROM VendorBusinessUnit vbu WHERE vbu.VendorID = v.VendorID AND vbu.IsFuelSupplier = 1)");
        }

        if (searchCriteria.InternalVendorOnly)
        {
            sql.Append(" AND v.IsInternalVendor = 1");
        }

        if (searchCriteria.IsBargeExEnabledOnly)
        {
            sql.Append(" AND v.IsBargeExEnabled = 1");
        }

        if (searchCriteria.EnablePortalOnly)
        {
            sql.Append(" AND v.EnablePortal = 1");
        }

        if (searchCriteria.LiquidBrokerOnly)
        {
            sql.Append(" AND v.IsLiquidBroker = 1");
        }

        if (searchCriteria.TankermanOnly)
        {
            sql.Append(" AND v.IsTankerman = 1");
        }

        // Apply DataTables search
        if (!string.IsNullOrWhiteSpace(request.Search?.Value))
        {
            sql.Append(@" AND (v.Name LIKE @Search
                             OR v.LongName LIKE @Search
                             OR v.AccountingCode LIKE @Search)");
            parameters.Add("Search", $"%{request.Search.Value}%");
        }

        // Get total count before filtering
        var countSql = $"SELECT COUNT(*) FROM ({sql}) AS CountQuery";
        var recordsFiltered = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        // Apply sorting
        if (request.Order?.Any() == true)
        {
            var orderColumn = request.Order[0];
            var columnName = request.Columns[orderColumn.Column].Data;
            var direction = orderColumn.Dir == "asc" ? "ASC" : "DESC";
            sql.Append($" ORDER BY v.{columnName} {direction}");
        }
        else
        {
            sql.Append(" ORDER BY v.Name ASC");
        }

        // Apply paging
        sql.Append(" OFFSET @Offset ROWS FETCH NEXT @Length ROWS ONLY");
        parameters.Add("Offset", request.Start);
        parameters.Add("Length", request.Length);

        var vendors = await connection.QueryAsync<VendorDto>(sql.ToString(), parameters);

        return new DataTableResponse<VendorDto>
        {
            Draw = request.Draw,
            RecordsTotal = recordsFiltered,
            RecordsFiltered = recordsFiltered,
            Data = vendors.ToList()
        };
    }

    public async Task<VendorDto?> GetByIdAsync(int vendorID)
    {
        using var connection = new SqlConnection(_connectionString);

        // Get main vendor record
        var sql = @"
            SELECT v.*,
                   CAST(CASE WHEN EXISTS(SELECT 1 FROM VendorBusinessUnit vbu WHERE vbu.VendorID = v.VendorID AND vbu.IsFuelSupplier = 1) THEN 1 ELSE 0 END AS BIT) AS IsFuelSupplier,
                   CAST(CASE WHEN EXISTS(SELECT 1 FROM VendorBusinessUnit vbu WHERE vbu.VendorID = v.VendorID AND vbu.IsBoatAssistSupplier = 1) THEN 1 ELSE 0 END AS BIT) AS IsBoatAssistSupplier
            FROM Vendor v
            WHERE v.VendorID = @VendorID";

        var vendor = await connection.QueryFirstOrDefaultAsync<VendorDto>(sql, new { VendorID = vendorID });

        if (vendor == null)
            return null;

        // Load child collections
        vendor.VendorContacts = (await GetContactsAsync(vendorID)).ToList();
        vendor.VendorBusinessUnits = (await GetBusinessUnitsAsync(vendorID)).ToList();

        return vendor;
    }

    public async Task<int> CreateAsync(VendorDto vendor)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            INSERT INTO Vendor (
                Name, LongName, AccountingCode, IsActive, EnablePortal,
                Address, City, State, Zip, PhoneNumber, FaxNumber, EmailAddress,
                IsBargeExEnabled, BargeExTradingPartnerNum, BargeExConfigID,
                IsLiquidBroker, TermsCode, IsInternalVendor, IsTankerman
            )
            VALUES (
                @Name, @LongName, @AccountingCode, @IsActive, @EnablePortal,
                @Address, @City, @State, @Zip, @PhoneNumber, @FaxNumber, @EmailAddress,
                @IsBargeExEnabled, @BargeExTradingPartnerNum, @BargeExConfigID,
                @IsLiquidBroker, @TermsCode, @IsInternalVendor, @IsTankerman
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        return await connection.ExecuteScalarAsync<int>(sql, vendor);
    }

    public async Task UpdateAsync(VendorDto vendor)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            UPDATE Vendor
            SET Name = @Name,
                LongName = @LongName,
                AccountingCode = @AccountingCode,
                IsActive = @IsActive,
                EnablePortal = @EnablePortal,
                Address = @Address,
                City = @City,
                State = @State,
                Zip = @Zip,
                PhoneNumber = @PhoneNumber,
                FaxNumber = @FaxNumber,
                EmailAddress = @EmailAddress,
                IsBargeExEnabled = @IsBargeExEnabled,
                BargeExTradingPartnerNum = @BargeExTradingPartnerNum,
                BargeExConfigID = @BargeExConfigID,
                IsLiquidBroker = @IsLiquidBroker,
                TermsCode = @TermsCode,
                IsInternalVendor = @IsInternalVendor,
                IsTankerman = @IsTankerman
            WHERE VendorID = @VendorID";

        await connection.ExecuteAsync(sql, vendor);
    }

    public async Task SetActiveAsync(int vendorID, bool isActive)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = "UPDATE Vendor SET IsActive = @IsActive WHERE VendorID = @VendorID";

        await connection.ExecuteAsync(sql, new { VendorID = vendorID, IsActive = isActive });
    }

    #endregion

    #region VendorContact CRUD

    public async Task<IEnumerable<VendorContactDto>> GetContactsAsync(int vendorID)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            SELECT *
            FROM VendorContact
            WHERE VendorID = @VendorID
            ORDER BY Name";

        return await connection.QueryAsync<VendorContactDto>(sql, new { VendorID = vendorID });
    }

    public async Task<VendorContactDto?> GetContactByIdAsync(int vendorContactID)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = "SELECT * FROM VendorContact WHERE VendorContactID = @VendorContactID";

        return await connection.QueryFirstOrDefaultAsync<VendorContactDto>(sql, new { VendorContactID = vendorContactID });
    }

    public async Task<int> CreateContactAsync(VendorContactDto contact)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            INSERT INTO VendorContact (
                VendorID, Name, PhoneNumber, PhoneExt, FaxNumber, EmailAddress,
                IsPrimary, IsDispatcher, IsLiquidBroker, PortalUserID
            )
            VALUES (
                @VendorID, @Name, @PhoneNumber, @PhoneExt, @FaxNumber, @EmailAddress,
                @IsPrimary, @IsDispatcher, @IsLiquidBroker, @PortalUserID
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        return await connection.ExecuteScalarAsync<int>(sql, contact);
    }

    public async Task UpdateContactAsync(VendorContactDto contact)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            UPDATE VendorContact
            SET Name = @Name,
                PhoneNumber = @PhoneNumber,
                PhoneExt = @PhoneExt,
                FaxNumber = @FaxNumber,
                EmailAddress = @EmailAddress,
                IsPrimary = @IsPrimary,
                IsDispatcher = @IsDispatcher,
                IsLiquidBroker = @IsLiquidBroker,
                PortalUserID = @PortalUserID
            WHERE VendorContactID = @VendorContactID";

        await connection.ExecuteAsync(sql, contact);
    }

    public async Task DeleteContactAsync(int vendorContactID)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = "DELETE FROM VendorContact WHERE VendorContactID = @VendorContactID";

        await connection.ExecuteAsync(sql, new { VendorContactID = vendorContactID });
    }

    public async Task ClearPrimaryContactAsync(int vendorID, int? excludeVendorContactID = null)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = excludeVendorContactID.HasValue
            ? "UPDATE VendorContact SET IsPrimary = 0 WHERE VendorID = @VendorID AND VendorContactID <> @ExcludeVendorContactID"
            : "UPDATE VendorContact SET IsPrimary = 0 WHERE VendorID = @VendorID";

        await connection.ExecuteAsync(sql, new { VendorID = vendorID, ExcludeVendorContactID = excludeVendorContactID });
    }

    #endregion

    #region VendorBusinessUnit CRUD

    public async Task<IEnumerable<VendorBusinessUnitDto>> GetBusinessUnitsAsync(int vendorID)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            SELECT *
            FROM VendorBusinessUnit
            WHERE VendorID = @VendorID
            ORDER BY Name";

        return await connection.QueryAsync<VendorBusinessUnitDto>(sql, new { VendorID = vendorID });
    }

    public async Task<VendorBusinessUnitDto?> GetBusinessUnitByIdAsync(int vendorBusinessUnitID)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = "SELECT * FROM VendorBusinessUnit WHERE VendorBusinessUnitID = @VendorBusinessUnitID";

        return await connection.QueryFirstOrDefaultAsync<VendorBusinessUnitDto>(sql, new { VendorBusinessUnitID = vendorBusinessUnitID });
    }

    public async Task<int> CreateBusinessUnitAsync(VendorBusinessUnitDto businessUnit)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            INSERT INTO VendorBusinessUnit (
                VendorID, Name, AccountingCode, IsActive, River, Mile, Bank,
                IsFuelSupplier, IsDefaultFuelSupplier, MinDiscountQty, MinDiscountFrequency,
                IsBoatAssistSupplier
            )
            VALUES (
                @VendorID, @Name, @AccountingCode, @IsActive, @River, @Mile, @Bank,
                @IsFuelSupplier, @IsDefaultFuelSupplier, @MinDiscountQty, @MinDiscountFrequency,
                @IsBoatAssistSupplier
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        return await connection.ExecuteScalarAsync<int>(sql, businessUnit);
    }

    public async Task UpdateBusinessUnitAsync(VendorBusinessUnitDto businessUnit)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            UPDATE VendorBusinessUnit
            SET Name = @Name,
                AccountingCode = @AccountingCode,
                IsActive = @IsActive,
                River = @River,
                Mile = @Mile,
                Bank = @Bank,
                IsFuelSupplier = @IsFuelSupplier,
                IsDefaultFuelSupplier = @IsDefaultFuelSupplier,
                MinDiscountQty = @MinDiscountQty,
                MinDiscountFrequency = @MinDiscountFrequency,
                IsBoatAssistSupplier = @IsBoatAssistSupplier
            WHERE VendorBusinessUnitID = @VendorBusinessUnitID";

        await connection.ExecuteAsync(sql, businessUnit);
    }

    public async Task DeleteBusinessUnitAsync(int vendorBusinessUnitID)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = "DELETE FROM VendorBusinessUnit WHERE VendorBusinessUnitID = @VendorBusinessUnitID";

        await connection.ExecuteAsync(sql, new { VendorBusinessUnitID = vendorBusinessUnitID });
    }

    public async Task ClearDefaultFuelSupplierAsync(int vendorID, int? excludeVendorBusinessUnitID = null)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = excludeVendorBusinessUnitID.HasValue
            ? "UPDATE VendorBusinessUnit SET IsDefaultFuelSupplier = 0 WHERE VendorID = @VendorID AND VendorBusinessUnitID <> @ExcludeVendorBusinessUnitID"
            : "UPDATE VendorBusinessUnit SET IsDefaultFuelSupplier = 0 WHERE VendorID = @VendorID";

        await connection.ExecuteAsync(sql, new { VendorID = vendorID, ExcludeVendorBusinessUnitID = excludeVendorBusinessUnitID });
    }

    #endregion
}
