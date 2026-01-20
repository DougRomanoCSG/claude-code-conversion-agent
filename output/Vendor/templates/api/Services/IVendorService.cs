using BargeOps.Shared.Dto;

namespace Admin.Infrastructure.Services;

/// <summary>
/// Service interface for Vendor business logic
/// </summary>
public interface IVendorService
{
    Task<PagedResult<VendorDto>> SearchAsync(VendorSearchRequest request, CancellationToken cancellationToken = default);
    Task<VendorDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<VendorDto> CreateAsync(VendorDto vendor, CancellationToken cancellationToken = default);
    Task<VendorDto> UpdateAsync(VendorDto vendor, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<IEnumerable<VendorContactDto>> GetContactsAsync(int vendorId, CancellationToken cancellationToken = default);
    Task<VendorContactDto> CreateContactAsync(VendorContactDto contact, CancellationToken cancellationToken = default);
    Task<VendorContactDto> UpdateContactAsync(VendorContactDto contact, CancellationToken cancellationToken = default);
    Task<bool> DeleteContactAsync(int contactId, CancellationToken cancellationToken = default);

    Task<IEnumerable<VendorBusinessUnitDto>> GetBusinessUnitsAsync(int vendorId, CancellationToken cancellationToken = default);
    Task<VendorBusinessUnitDto> CreateBusinessUnitAsync(VendorBusinessUnitDto businessUnit, CancellationToken cancellationToken = default);
    Task<VendorBusinessUnitDto> UpdateBusinessUnitAsync(VendorBusinessUnitDto businessUnit, CancellationToken cancellationToken = default);
    Task<bool> DeleteBusinessUnitAsync(int businessUnitId, CancellationToken cancellationToken = default);

    Task<IEnumerable<VendorPortalGroupDto>> GetPortalGroupsAsync(int vendorId, CancellationToken cancellationToken = default);
    Task<VendorPortalGroupDto> CreatePortalGroupAsync(VendorPortalGroupDto portalGroup, CancellationToken cancellationToken = default);
    Task<VendorPortalGroupDto> UpdatePortalGroupAsync(VendorPortalGroupDto portalGroup, CancellationToken cancellationToken = default);
    Task<bool> DeletePortalGroupAsync(int portalGroupId, CancellationToken cancellationToken = default);
}
