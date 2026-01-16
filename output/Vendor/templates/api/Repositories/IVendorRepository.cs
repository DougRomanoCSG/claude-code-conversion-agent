using BargeOps.Shared.Dto;

namespace Admin.Infrastructure.Abstractions;

/// <summary>
/// Repository interface for Vendor operations
/// </summary>
public interface IVendorRepository
{
    // Vendor CRUD
    Task<PagedResult<VendorDto>> SearchAsync(VendorSearchRequest request, CancellationToken cancellationToken = default);
    Task<VendorDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(VendorDto vendor, CancellationToken cancellationToken = default);
    Task UpdateAsync(VendorDto vendor, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    // Vendor Contacts
    Task<IEnumerable<VendorContactDto>> GetContactsAsync(int vendorId, CancellationToken cancellationToken = default);
    Task<int> CreateContactAsync(VendorContactDto contact, CancellationToken cancellationToken = default);
    Task UpdateContactAsync(VendorContactDto contact, CancellationToken cancellationToken = default);
    Task<bool> DeleteContactAsync(int contactId, CancellationToken cancellationToken = default);

    // Vendor Business Units
    Task<IEnumerable<VendorBusinessUnitDto>> GetBusinessUnitsAsync(int vendorId, CancellationToken cancellationToken = default);
    Task<int> CreateBusinessUnitAsync(VendorBusinessUnitDto businessUnit, CancellationToken cancellationToken = default);
    Task UpdateBusinessUnitAsync(VendorBusinessUnitDto businessUnit, CancellationToken cancellationToken = default);
    Task<bool> DeleteBusinessUnitAsync(int businessUnitId, CancellationToken cancellationToken = default);
}
