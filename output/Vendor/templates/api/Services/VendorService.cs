using BargeOps.Shared.Dto;
using BargeOps.Shared.Models;
using Admin.Domain.Repositories;
using Admin.Domain.Services;

namespace Admin.Infrastructure.Services;

/// <summary>
/// Vendor service - handles business logic and delegates to repository
/// Reference: FacilityService.cs, BoatLocationService.cs
/// </summary>
public class VendorService : IVendorService
{
    private readonly IVendorRepository _vendorRepository;

    public VendorService(IVendorRepository vendorRepository)
    {
        _vendorRepository = vendorRepository;
    }

    #region Vendor Operations

    public async Task<PagedResult<VendorDto>> SearchVendorsAsync(VendorSearchRequest request, int page, int pageSize)
    {
        return await _vendorRepository.SearchAsync(request, page, pageSize);
    }

    public async Task<DataTableResponse<VendorDto>> GetVendorDataTableAsync(DataTableRequest request, VendorSearchRequest searchCriteria)
    {
        return await _vendorRepository.GetDataTableAsync(request, searchCriteria);
    }

    public async Task<VendorDto?> GetVendorByIdAsync(int vendorID)
    {
        return await _vendorRepository.GetByIdAsync(vendorID);
    }

    public async Task<int> CreateVendorAsync(VendorDto vendor)
    {
        // Business logic: Clear BargeEx fields if not enabled
        if (!vendor.IsBargeExEnabled)
        {
            vendor.BargeExTradingPartnerNum = null;
            vendor.BargeExConfigID = null;
        }

        // Validation: BargeEx fields required when enabled
        if (vendor.IsBargeExEnabled)
        {
            if (string.IsNullOrWhiteSpace(vendor.BargeExTradingPartnerNum) ||
                string.IsNullOrWhiteSpace(vendor.BargeExConfigID))
            {
                throw new InvalidOperationException(
                    "Both Trading partner number & Configuration required for BargeEx enabled.");
            }
        }

        return await _vendorRepository.CreateAsync(vendor);
    }

    public async Task UpdateVendorAsync(VendorDto vendor)
    {
        // Business logic: Clear BargeEx fields if not enabled
        if (!vendor.IsBargeExEnabled)
        {
            vendor.BargeExTradingPartnerNum = null;
            vendor.BargeExConfigID = null;
        }

        // Validation: BargeEx fields required when enabled
        if (vendor.IsBargeExEnabled)
        {
            if (string.IsNullOrWhiteSpace(vendor.BargeExTradingPartnerNum) ||
                string.IsNullOrWhiteSpace(vendor.BargeExConfigID))
            {
                throw new InvalidOperationException(
                    "Both Trading partner number & Configuration required for BargeEx enabled.");
            }
        }

        await _vendorRepository.UpdateAsync(vendor);
    }

    public async Task SetVendorActiveAsync(int vendorID, bool isActive)
    {
        await _vendorRepository.SetActiveAsync(vendorID, isActive);
    }

    #endregion

    #region VendorContact Operations

    public async Task<IEnumerable<VendorContactDto>> GetContactsAsync(int vendorID)
    {
        return await _vendorRepository.GetContactsAsync(vendorID);
    }

    public async Task<VendorContactDto?> GetContactByIdAsync(int vendorContactID)
    {
        return await _vendorRepository.GetContactByIdAsync(vendorContactID);
    }

    public async Task<int> CreateContactAsync(VendorContactDto contact)
    {
        // Business logic: Ensure only one primary contact
        if (contact.IsPrimary)
        {
            await _vendorRepository.ClearPrimaryContactAsync(contact.VendorID);
        }

        return await _vendorRepository.CreateContactAsync(contact);
    }

    public async Task UpdateContactAsync(VendorContactDto contact)
    {
        // Business logic: Ensure only one primary contact
        if (contact.IsPrimary)
        {
            await _vendorRepository.ClearPrimaryContactAsync(contact.VendorID, contact.VendorContactID);
        }

        await _vendorRepository.UpdateContactAsync(contact);
    }

    public async Task DeleteContactAsync(int vendorContactID)
    {
        await _vendorRepository.DeleteContactAsync(vendorContactID);
    }

    #endregion

    #region VendorBusinessUnit Operations

    public async Task<IEnumerable<VendorBusinessUnitDto>> GetBusinessUnitsAsync(int vendorID)
    {
        return await _vendorRepository.GetBusinessUnitsAsync(vendorID);
    }

    public async Task<VendorBusinessUnitDto?> GetBusinessUnitByIdAsync(int vendorBusinessUnitID)
    {
        return await _vendorRepository.GetBusinessUnitByIdAsync(vendorBusinessUnitID);
    }

    public async Task<int> CreateBusinessUnitAsync(VendorBusinessUnitDto businessUnit)
    {
        // Business logic: Clear fuel supplier fields if not a fuel supplier
        if (!businessUnit.IsFuelSupplier)
        {
            businessUnit.IsDefaultFuelSupplier = false;
            businessUnit.MinDiscountQty = null;
            businessUnit.MinDiscountFrequency = null;
        }

        // Business logic: Ensure only one default fuel supplier
        if (businessUnit.IsDefaultFuelSupplier)
        {
            await _vendorRepository.ClearDefaultFuelSupplierAsync(businessUnit.VendorID);
        }

        return await _vendorRepository.CreateBusinessUnitAsync(businessUnit);
    }

    public async Task UpdateBusinessUnitAsync(VendorBusinessUnitDto businessUnit)
    {
        // Business logic: Clear fuel supplier fields if not a fuel supplier
        if (!businessUnit.IsFuelSupplier)
        {
            businessUnit.IsDefaultFuelSupplier = false;
            businessUnit.MinDiscountQty = null;
            businessUnit.MinDiscountFrequency = null;
        }

        // Business logic: Ensure only one default fuel supplier
        if (businessUnit.IsDefaultFuelSupplier)
        {
            await _vendorRepository.ClearDefaultFuelSupplierAsync(businessUnit.VendorID, businessUnit.VendorBusinessUnitID);
        }

        await _vendorRepository.UpdateBusinessUnitAsync(businessUnit);
    }

    public async Task DeleteBusinessUnitAsync(int vendorBusinessUnitID)
    {
        await _vendorRepository.DeleteBusinessUnitAsync(vendorBusinessUnitID);
    }

    #endregion
}
