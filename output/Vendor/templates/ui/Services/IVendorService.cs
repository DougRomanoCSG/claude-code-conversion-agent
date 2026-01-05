using BargeOps.Shared.Dto;
using BargeOps.Shared.Models;

namespace BargeOpsAdmin.Services;

/// <summary>
/// Vendor service interface for UI layer
/// HTTP client to call Vendor API endpoints
/// Location: src/BargeOps.UI/Services/
/// </summary>
public interface IVendorService
{
    #region Vendor Operations

    /// <summary>
    /// Search vendors with paging
    /// </summary>
    Task<PagedResult<VendorDto>?> SearchVendorsAsync(VendorSearchRequest request, int page = 1, int pageSize = 25);

    /// <summary>
    /// Get vendors for DataTables server-side processing
    /// </summary>
    Task<DataTableResponse<VendorDto>?> GetVendorDataTableAsync(DataTableRequest request, VendorSearchRequest searchCriteria);

    /// <summary>
    /// Get vendor by ID with child collections
    /// </summary>
    Task<VendorDto?> GetVendorByIdAsync(int vendorID);

    /// <summary>
    /// Create new vendor
    /// </summary>
    Task<VendorDto?> CreateVendorAsync(VendorDto vendor);

    /// <summary>
    /// Update existing vendor
    /// </summary>
    Task<bool> UpdateVendorAsync(VendorDto vendor);

    /// <summary>
    /// Set vendor active/inactive (soft delete)
    /// </summary>
    Task<bool> SetVendorActiveAsync(int vendorID, bool isActive);

    #endregion

    #region VendorContact Operations

    /// <summary>
    /// Get all contacts for a vendor
    /// </summary>
    Task<IEnumerable<VendorContactDto>?> GetContactsAsync(int vendorID);

    /// <summary>
    /// Get contact by ID (nested resource)
    /// </summary>
    Task<VendorContactDto?> GetContactByIdAsync(int vendorID, int vendorContactID);

    /// <summary>
    /// Create new contact
    /// </summary>
    Task<VendorContactDto?> CreateContactAsync(VendorContactDto contact);

    /// <summary>
    /// Update existing contact
    /// </summary>
    Task<bool> UpdateContactAsync(VendorContactDto contact);

    /// <summary>
    /// Delete contact (nested resource)
    /// </summary>
    Task<bool> DeleteContactAsync(int vendorID, int vendorContactID);

    #endregion

    #region VendorBusinessUnit Operations

    /// <summary>
    /// Get all business units for a vendor
    /// </summary>
    Task<IEnumerable<VendorBusinessUnitDto>?> GetBusinessUnitsAsync(int vendorID);

    /// <summary>
    /// Get business unit by ID (nested resource)
    /// </summary>
    Task<VendorBusinessUnitDto?> GetBusinessUnitByIdAsync(int vendorID, int vendorBusinessUnitID);

    /// <summary>
    /// Create new business unit
    /// </summary>
    Task<VendorBusinessUnitDto?> CreateBusinessUnitAsync(VendorBusinessUnitDto businessUnit);

    /// <summary>
    /// Update existing business unit
    /// </summary>
    Task<bool> UpdateBusinessUnitAsync(VendorBusinessUnitDto businessUnit);

    /// <summary>
    /// Delete business unit (nested resource)
    /// </summary>
    Task<bool> DeleteBusinessUnitAsync(int vendorID, int vendorBusinessUnitID);

    #endregion
}
