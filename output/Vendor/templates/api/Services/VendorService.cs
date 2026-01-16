using Admin.Infrastructure.Abstractions;
using BargeOps.Shared.Dto;
using FluentValidation;

namespace Admin.Infrastructure.Services;

/// <summary>
/// Service for Vendor business logic
/// Validates DTOs using FluentValidation before repository operations
/// </summary>
public class VendorService : IVendorService
{
    private readonly IVendorRepository _vendorRepository;
    private readonly IValidator<VendorDto> _vendorValidator;
    private readonly IValidator<VendorContactDto> _contactValidator;
    private readonly IValidator<VendorBusinessUnitDto> _businessUnitValidator;

    public VendorService(
        IVendorRepository vendorRepository,
        IValidator<VendorDto> vendorValidator,
        IValidator<VendorContactDto> contactValidator,
        IValidator<VendorBusinessUnitDto> businessUnitValidator)
    {
        _vendorRepository = vendorRepository;
        _vendorValidator = vendorValidator;
        _contactValidator = contactValidator;
        _businessUnitValidator = businessUnitValidator;
    }

    #region Vendor Operations

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

        // Apply business rules
        ApplyBusinessRules(vendor);

        var vendorId = await _vendorRepository.CreateAsync(vendor, cancellationToken);
        vendor.VendorID = vendorId;

        return vendor;
    }

    public async Task<VendorDto> UpdateAsync(VendorDto vendor, CancellationToken cancellationToken = default)
    {
        await _vendorValidator.ValidateAndThrowAsync(vendor, cancellationToken);

        // Apply business rules
        ApplyBusinessRules(vendor);

        await _vendorRepository.UpdateAsync(vendor, cancellationToken);

        return vendor;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _vendorRepository.DeleteAsync(id, cancellationToken);
    }

    #endregion

    #region Vendor Contact Operations

    public async Task<IEnumerable<VendorContactDto>> GetContactsAsync(int vendorId, CancellationToken cancellationToken = default)
    {
        return await _vendorRepository.GetContactsAsync(vendorId, cancellationToken);
    }

    public async Task<VendorContactDto> CreateContactAsync(VendorContactDto contact, CancellationToken cancellationToken = default)
    {
        await _contactValidator.ValidateAndThrowAsync(contact, cancellationToken);

        // Business rule: Ensure only one primary contact per vendor
        if (contact.IsPrimary)
        {
            await ClearPrimaryContactAsync(contact.VendorID, cancellationToken);
        }

        var contactId = await _vendorRepository.CreateContactAsync(contact, cancellationToken);
        contact.VendorContactID = contactId;

        return contact;
    }

    public async Task<VendorContactDto> UpdateContactAsync(VendorContactDto contact, CancellationToken cancellationToken = default)
    {
        await _contactValidator.ValidateAndThrowAsync(contact, cancellationToken);

        // Business rule: Ensure only one primary contact per vendor
        if (contact.IsPrimary)
        {
            await ClearPrimaryContactAsync(contact.VendorID, cancellationToken, contact.VendorContactID);
        }

        await _vendorRepository.UpdateContactAsync(contact, cancellationToken);

        return contact;
    }

    public async Task<bool> DeleteContactAsync(int contactId, CancellationToken cancellationToken = default)
    {
        return await _vendorRepository.DeleteContactAsync(contactId, cancellationToken);
    }

    #endregion

    #region Vendor Business Unit Operations

    public async Task<IEnumerable<VendorBusinessUnitDto>> GetBusinessUnitsAsync(int vendorId, CancellationToken cancellationToken = default)
    {
        return await _vendorRepository.GetBusinessUnitsAsync(vendorId, cancellationToken);
    }

    public async Task<VendorBusinessUnitDto> CreateBusinessUnitAsync(VendorBusinessUnitDto businessUnit, CancellationToken cancellationToken = default)
    {
        await _businessUnitValidator.ValidateAndThrowAsync(businessUnit, cancellationToken);

        // Apply business rules for fuel supplier settings
        ApplyBusinessUnitBusinessRules(businessUnit);

        // Business rule: Ensure only one default fuel supplier per vendor
        if (businessUnit.IsDefaultFuelSupplier)
        {
            await ClearDefaultFuelSupplierAsync(businessUnit.VendorID, cancellationToken);
        }

        var businessUnitId = await _vendorRepository.CreateBusinessUnitAsync(businessUnit, cancellationToken);
        businessUnit.VendorBusinessUnitID = businessUnitId;

        return businessUnit;
    }

    public async Task<VendorBusinessUnitDto> UpdateBusinessUnitAsync(VendorBusinessUnitDto businessUnit, CancellationToken cancellationToken = default)
    {
        await _businessUnitValidator.ValidateAndThrowAsync(businessUnit, cancellationToken);

        // Apply business rules for fuel supplier settings
        ApplyBusinessUnitBusinessRules(businessUnit);

        // Business rule: Ensure only one default fuel supplier per vendor
        if (businessUnit.IsDefaultFuelSupplier)
        {
            await ClearDefaultFuelSupplierAsync(businessUnit.VendorID, cancellationToken, businessUnit.VendorBusinessUnitID);
        }

        await _vendorRepository.UpdateBusinessUnitAsync(businessUnit, cancellationToken);

        return businessUnit;
    }

    public async Task<bool> DeleteBusinessUnitAsync(int businessUnitId, CancellationToken cancellationToken = default)
    {
        return await _vendorRepository.DeleteBusinessUnitAsync(businessUnitId, cancellationToken);
    }

    #endregion

    #region Business Rules

    private void ApplyBusinessRules(VendorDto vendor)
    {
        // Business rule: Clear BargeEx fields if BargeEx is disabled
        if (!vendor.IsBargeExEnabled)
        {
            vendor.BargeExTradingPartnerNum = null;
            vendor.BargeExConfigID = null;
        }

        // Business rule: Pad BargeEx trading partner number to 8 characters
        if (!string.IsNullOrWhiteSpace(vendor.BargeExTradingPartnerNum))
        {
            vendor.BargeExTradingPartnerNum = vendor.BargeExTradingPartnerNum.PadRight(8);
        }
    }

    private void ApplyBusinessUnitBusinessRules(VendorBusinessUnitDto businessUnit)
    {
        // Business rule: Clear fuel supplier discount fields if not a fuel supplier
        if (!businessUnit.IsFuelSupplier)
        {
            businessUnit.IsDefaultFuelSupplier = false;
            businessUnit.MinDiscountQty = null;
            businessUnit.MinDiscountFrequency = null;
        }
    }

    private async Task ClearPrimaryContactAsync(int vendorId, CancellationToken cancellationToken, int? exceptContactId = null)
    {
        // This would require a custom SQL update to clear IsPrimary for all other contacts
        // For now, this is a placeholder - implementation would need to be added to repository
        // TODO: Implement ClearPrimaryContact in repository
        await Task.CompletedTask;
    }

    private async Task ClearDefaultFuelSupplierAsync(int vendorId, CancellationToken cancellationToken, int? exceptBusinessUnitId = null)
    {
        // This would require a custom SQL update to clear IsDefaultFuelSupplier for all other business units
        // For now, this is a placeholder - implementation would need to be added to repository
        // TODO: Implement ClearDefaultFuelSupplier in repository
        await Task.CompletedTask;
    }

    #endregion
}
