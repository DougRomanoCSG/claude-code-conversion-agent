using BargeOps.Shared.Dto;

namespace Admin.Infrastructure.Abstractions;

/// <summary>
/// Repository interface for Vendor operations
/// </summary>
public interface IVendorRepository
{
    // Vendor Operations
    Task<PagedResult<VendorDto>> SearchAsync(VendorSearchRequest request, CancellationToken cancellationToken = default);
    Task<VendorDto?> GetByIdAsync(int vendorId, CancellationToken cancellationToken = default);
    Task<VendorDto> CreateAsync(VendorDto vendor, CancellationToken cancellationToken = default);
    Task<VendorDto> UpdateAsync(VendorDto vendor, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int vendorId, CancellationToken cancellationToken = default);

    // VendorContact Operations
    Task<IEnumerable<VendorContactDto>> GetContactsByVendorIdAsync(int vendorId, CancellationToken cancellationToken = default);
    Task<VendorContactDto> CreateContactAsync(VendorContactDto contact, CancellationToken cancellationToken = default);
    Task<VendorContactDto> UpdateContactAsync(VendorContactDto contact, CancellationToken cancellationToken = default);
    Task<bool> DeleteContactAsync(int vendorContactId, CancellationToken cancellationToken = default);

    // VendorBusinessUnit Operations
    Task<IEnumerable<VendorBusinessUnitDto>> GetBusinessUnitsByVendorIdAsync(int vendorId, CancellationToken cancellationToken = default);
    Task<VendorBusinessUnitDto> CreateBusinessUnitAsync(VendorBusinessUnitDto businessUnit, CancellationToken cancellationToken = default);
    Task<VendorBusinessUnitDto> UpdateBusinessUnitAsync(VendorBusinessUnitDto businessUnit, CancellationToken cancellationToken = default);
    Task<bool> DeleteBusinessUnitAsync(int vendorBusinessUnitId, CancellationToken cancellationToken = default);
}
