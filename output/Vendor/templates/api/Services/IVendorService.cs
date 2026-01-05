using BargeOps.Shared.Dto;
using BargeOps.Shared.Models;

namespace Admin.Domain.Services;

/// <summary>
/// Service interface for Vendor business logic
/// Uses DTOs from Shared project
/// </summary>
public interface IVendorService
{
    #region Vendor Operations

    Task<PagedResult<VendorDto>> SearchVendorsAsync(VendorSearchRequest request, int page, int pageSize);
    Task<DataTableResponse<VendorDto>> GetVendorDataTableAsync(DataTableRequest request, VendorSearchRequest searchCriteria);
    Task<VendorDto?> GetVendorByIdAsync(int vendorID);
    Task<int> CreateVendorAsync(VendorDto vendor);
    Task UpdateVendorAsync(VendorDto vendor);
    Task SetVendorActiveAsync(int vendorID, bool isActive);

    #endregion

    #region VendorContact Operations

    Task<IEnumerable<VendorContactDto>> GetContactsAsync(int vendorID);
    Task<VendorContactDto?> GetContactByIdAsync(int vendorContactID);
    Task<int> CreateContactAsync(VendorContactDto contact);
    Task UpdateContactAsync(VendorContactDto contact);
    Task DeleteContactAsync(int vendorContactID);

    #endregion

    #region VendorBusinessUnit Operations

    Task<IEnumerable<VendorBusinessUnitDto>> GetBusinessUnitsAsync(int vendorID);
    Task<VendorBusinessUnitDto?> GetBusinessUnitByIdAsync(int vendorBusinessUnitID);
    Task<int> CreateBusinessUnitAsync(VendorBusinessUnitDto businessUnit);
    Task UpdateBusinessUnitAsync(VendorBusinessUnitDto businessUnit);
    Task DeleteBusinessUnitAsync(int vendorBusinessUnitID);

    #endregion
}
