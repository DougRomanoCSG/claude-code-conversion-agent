using BargeOps.Shared.Dto;
using BargeOps.Shared.Models;

namespace Admin.Domain.Repositories;

/// <summary>
/// Repository interface for Vendor data access
/// Returns DTOs directly - NO mapping needed!
/// </summary>
public interface IVendorRepository
{
    #region Vendor CRUD

    /// <summary>
    /// Search vendors with paging
    /// </summary>
    Task<PagedResult<VendorDto>> SearchAsync(VendorSearchRequest request, int page, int pageSize);

    /// <summary>
    /// Get vendors for DataTables server-side processing
    /// </summary>
    Task<DataTableResponse<VendorDto>> GetDataTableAsync(DataTableRequest request, VendorSearchRequest searchCriteria);

    /// <summary>
    /// Get vendor by ID with child collections
    /// </summary>
    Task<VendorDto?> GetByIdAsync(int vendorID);

    /// <summary>
    /// Create new vendor
    /// Returns new VendorID
    /// </summary>
    Task<int> CreateAsync(VendorDto vendor);

    /// <summary>
    /// Update existing vendor
    /// </summary>
    Task UpdateAsync(VendorDto vendor);

    /// <summary>
    /// Soft delete - set IsActive flag
    /// </summary>
    Task SetActiveAsync(int vendorID, bool isActive);

    #endregion

    #region VendorContact CRUD

    /// <summary>
    /// Get all contacts for a vendor
    /// </summary>
    Task<IEnumerable<VendorContactDto>> GetContactsAsync(int vendorID);

    /// <summary>
    /// Get contact by ID
    /// </summary>
    Task<VendorContactDto?> GetContactByIdAsync(int vendorContactID);

    /// <summary>
    /// Create new contact
    /// Returns new VendorContactID
    /// </summary>
    Task<int> CreateContactAsync(VendorContactDto contact);

    /// <summary>
    /// Update existing contact
    /// </summary>
    Task UpdateContactAsync(VendorContactDto contact);

    /// <summary>
    /// Delete contact
    /// </summary>
    Task DeleteContactAsync(int vendorContactID);

    /// <summary>
    /// Clear primary flag on all other contacts for a vendor
    /// </summary>
    Task ClearPrimaryContactAsync(int vendorID, int? excludeVendorContactID = null);

    #endregion

    #region VendorBusinessUnit CRUD

    /// <summary>
    /// Get all business units for a vendor
    /// </summary>
    Task<IEnumerable<VendorBusinessUnitDto>> GetBusinessUnitsAsync(int vendorID);

    /// <summary>
    /// Get business unit by ID
    /// </summary>
    Task<VendorBusinessUnitDto?> GetBusinessUnitByIdAsync(int vendorBusinessUnitID);

    /// <summary>
    /// Create new business unit
    /// Returns new VendorBusinessUnitID
    /// </summary>
    Task<int> CreateBusinessUnitAsync(VendorBusinessUnitDto businessUnit);

    /// <summary>
    /// Update existing business unit
    /// </summary>
    Task UpdateBusinessUnitAsync(VendorBusinessUnitDto businessUnit);

    /// <summary>
    /// Delete business unit
    /// </summary>
    Task DeleteBusinessUnitAsync(int vendorBusinessUnitID);

    /// <summary>
    /// Clear default fuel supplier flag on all other business units for a vendor
    /// </summary>
    Task ClearDefaultFuelSupplierAsync(int vendorID, int? excludeVendorBusinessUnitID = null);

    #endregion
}
