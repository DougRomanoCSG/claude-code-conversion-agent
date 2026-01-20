using Admin.Infrastructure.Abstractions;
using BargeOps.Shared.Dto;
using FluentValidation;

namespace Admin.Infrastructure.Services;

/// <summary>
/// Service for Vendor business logic and validation
/// </summary>
public class VendorService : IVendorService
{
    private readonly IVendorRepository _vendorRepository;
    private readonly IValidator<VendorDto> _vendorValidator;
    private readonly IValidator<VendorContactDto> _contactValidator;
    private readonly IValidator<VendorBusinessUnitDto> _businessUnitValidator;
    private readonly IValidator<VendorPortalGroupDto> _portalGroupValidator;

    public VendorService(
        IVendorRepository vendorRepository,
        IValidator<VendorDto> vendorValidator,
        IValidator<VendorContactDto> contactValidator,
        IValidator<VendorBusinessUnitDto> businessUnitValidator,
        IValidator<VendorPortalGroupDto> portalGroupValidator)
    {
        _vendorRepository = vendorRepository;
        _vendorValidator = vendorValidator;
        _contactValidator = contactValidator;
        _businessUnitValidator = businessUnitValidator;
        _portalGroupValidator = portalGroupValidator;
    }

    public async Task<PagedResult<VendorDto>> SearchAsync(VendorSearchRequest request, CancellationToken cancellationToken = default)
    {
        return await _vendorRepository.SearchAsync(request, cancellationToken);
    }

    public async Task<VendorDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _vendorRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<VendorDto> CreateAsync(VendorDto vendor, CancellationToken cancellationToken = default)
    {
        await _vendorValidator.ValidateAndThrowAsync(vendor, cancellationToken);

        ApplyBusinessRules(vendor);

        var createdVendor = await _vendorRepository.CreateAsync(vendor, cancellationToken);
        return createdVendor;
    }

    public async Task<VendorDto> UpdateAsync(VendorDto vendor, CancellationToken cancellationToken = default)
    {
        await _vendorValidator.ValidateAndThrowAsync(vendor, cancellationToken);

        ApplyBusinessRules(vendor);

        var updatedVendor = await _vendorRepository.UpdateAsync(vendor, cancellationToken);
        return updatedVendor;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _vendorRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<VendorContactDto>> GetContactsAsync(int vendorId, CancellationToken cancellationToken = default)
    {
        return await _vendorRepository.GetContactsAsync(vendorId, cancellationToken);
    }

    public async Task<VendorContactDto> CreateContactAsync(VendorContactDto contact, CancellationToken cancellationToken = default)
    {
        await _contactValidator.ValidateAndThrowAsync(contact, cancellationToken);

        var createdContact = await _vendorRepository.CreateContactAsync(contact, cancellationToken);
        return createdContact;
    }

    public async Task<VendorContactDto> UpdateContactAsync(VendorContactDto contact, CancellationToken cancellationToken = default)
    {
        await _contactValidator.ValidateAndThrowAsync(contact, cancellationToken);

        var updatedContact = await _vendorRepository.UpdateContactAsync(contact, cancellationToken);
        return updatedContact;
    }

    public async Task<bool> DeleteContactAsync(int contactId, CancellationToken cancellationToken = default)
    {
        return await _vendorRepository.DeleteContactAsync(contactId, cancellationToken);
    }

    public async Task<IEnumerable<VendorBusinessUnitDto>> GetBusinessUnitsAsync(int vendorId, CancellationToken cancellationToken = default)
    {
        return await _vendorRepository.GetBusinessUnitsAsync(vendorId, cancellationToken);
    }

    public async Task<VendorBusinessUnitDto> CreateBusinessUnitAsync(VendorBusinessUnitDto businessUnit, CancellationToken cancellationToken = default)
    {
        await _businessUnitValidator.ValidateAndThrowAsync(businessUnit, cancellationToken);

        ApplyBusinessUnitRules(businessUnit);

        var createdBusinessUnit = await _vendorRepository.CreateBusinessUnitAsync(businessUnit, cancellationToken);
        return createdBusinessUnit;
    }

    public async Task<VendorBusinessUnitDto> UpdateBusinessUnitAsync(VendorBusinessUnitDto businessUnit, CancellationToken cancellationToken = default)
    {
        await _businessUnitValidator.ValidateAndThrowAsync(businessUnit, cancellationToken);

        ApplyBusinessUnitRules(businessUnit);

        var updatedBusinessUnit = await _vendorRepository.UpdateBusinessUnitAsync(businessUnit, cancellationToken);
        return updatedBusinessUnit;
    }

    public async Task<bool> DeleteBusinessUnitAsync(int businessUnitId, CancellationToken cancellationToken = default)
    {
        return await _vendorRepository.DeleteBusinessUnitAsync(businessUnitId, cancellationToken);
    }

    public async Task<IEnumerable<VendorPortalGroupDto>> GetPortalGroupsAsync(int vendorId, CancellationToken cancellationToken = default)
    {
        return await _vendorRepository.GetPortalGroupsAsync(vendorId, cancellationToken);
    }

    public async Task<VendorPortalGroupDto> CreatePortalGroupAsync(VendorPortalGroupDto portalGroup, CancellationToken cancellationToken = default)
    {
        await _portalGroupValidator.ValidateAndThrowAsync(portalGroup, cancellationToken);

        var createdPortalGroup = await _vendorRepository.CreatePortalGroupAsync(portalGroup, cancellationToken);
        return createdPortalGroup;
    }

    public async Task<VendorPortalGroupDto> UpdatePortalGroupAsync(VendorPortalGroupDto portalGroup, CancellationToken cancellationToken = default)
    {
        await _portalGroupValidator.ValidateAndThrowAsync(portalGroup, cancellationToken);

        var updatedPortalGroup = await _vendorRepository.UpdatePortalGroupAsync(portalGroup, cancellationToken);
        return updatedPortalGroup;
    }

    public async Task<bool> DeletePortalGroupAsync(int portalGroupId, CancellationToken cancellationToken = default)
    {
        return await _vendorRepository.DeletePortalGroupAsync(portalGroupId, cancellationToken);
    }

    private void ApplyBusinessRules(VendorDto vendor)
    {
        if (!vendor.IsBargeExEnabled)
        {
            vendor.BargeExTradingPartnerNum = null;
            vendor.BargeExConfigID = null;
        }
        else if (!string.IsNullOrWhiteSpace(vendor.BargeExTradingPartnerNum))
        {
            vendor.BargeExTradingPartnerNum = vendor.BargeExTradingPartnerNum.PadLeft(8, '0');
        }
    }

    private void ApplyBusinessUnitRules(VendorBusinessUnitDto businessUnit)
    {
        if (!businessUnit.IsFuelSupplier)
        {
            businessUnit.IsDefaultFuelSupplier = false;
            businessUnit.MinDiscountQty = null;
            businessUnit.MinDiscountFrequency = null;
        }
    }
}
