using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using BargeOps.Shared.Dto;
using BargeOps.Shared.Models;
using BargeOpsAdmin.ViewModels;
using BargeOpsAdmin.Services;

namespace BargeOpsAdmin.Controllers;

/// <summary>
/// Vendor Search and CRUD Controller
/// Authentication: OIDC (production), DevelopmentAutoSignInMiddleware (development)
/// Reference: BoatLocationSearchController.cs
/// Location: src/BargeOps.UI/Controllers/
/// </summary>
[Authorize]
public class VendorSearchController : AppController
{
    private readonly IVendorService _vendorService;
    private readonly ILookupService _lookupService;
    private readonly ILogger<VendorSearchController> _logger;

    public VendorSearchController(
        IVendorService vendorService,
        ILookupService lookupService,
        ILogger<VendorSearchController> logger)
    {
        _vendorService = vendorService;
        _lookupService = lookupService;
        _logger = logger;
    }

    #region Search/List

    /// <summary>
    /// Display vendor search page
    /// GET /VendorSearch/Index
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        var model = new VendorSearchViewModel
        {
            IsActiveOnly = true,
            // License flags - TODO: Get from configuration/license service
            PortalLicenseActive = true,  // Replace with actual license check
            UnitTowLicenseActive = true, // Replace with actual license check
            BargeExGlobalSettingEnabled = true // Replace with actual global setting check
        };

        return View(model);
    }

    /// <summary>
    /// DataTables server-side processing endpoint
    /// POST /VendorSearch/VendorTable
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> VendorTable(
        [FromBody] DataTableRequest request,
        [FromQuery] VendorSearchViewModel searchModel)
    {
        try
        {
            var searchRequest = new VendorSearchRequest
            {
                Name = searchModel.Name,
                AccountingCode = searchModel.AccountingCode,
                ActiveOnly = searchModel.IsActiveOnly,
                FuelSuppliersOnly = searchModel.FuelSuppliersOnly,
                InternalVendorOnly = searchModel.InternalVendorOnly,
                IsBargeExEnabledOnly = searchModel.IsBargeExEnabledOnly,
                EnablePortalOnly = searchModel.EnablePortalOnly,
                LiquidBrokerOnly = searchModel.LiquidBrokerOnly,
                TankermanOnly = searchModel.TankermanOnly
            };

            var result = await _vendorService.GetVendorDataTableAsync(request, searchRequest);
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading vendor DataTable");
            return StatusCode(500, "An error occurred while loading vendor data");
        }
    }

    #endregion

    #region Create/Edit/Delete

    /// <summary>
    /// Display create vendor form
    /// GET /VendorSearch/Create
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "VendorModify")]
    public async Task<IActionResult> Create()
    {
        try
        {
            var model = new VendorEditViewModel
            {
                Vendor = new VendorDto
                {
                    IsActive = true,
                    EnablePortal = false,
                    IsBargeExEnabled = false,
                    IsLiquidBroker = false,
                    IsInternalVendor = false,
                    IsTankerman = false
                }
            };

            await LoadLookupLists(model);
            LoadLicenseFlags(model);

            return View("Edit", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error displaying create vendor form");
            TempData["ErrorMessage"] = "An error occurred while loading the form";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Display edit vendor form
    /// GET /VendorSearch/Edit/{id}
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "VendorModify")]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var vendor = await _vendorService.GetVendorByIdAsync(id);

            if (vendor == null)
            {
                TempData["ErrorMessage"] = $"Vendor with ID {id} not found";
                return RedirectToAction(nameof(Index));
            }

            var model = new VendorEditViewModel
            {
                Vendor = vendor
            };

            await LoadLookupLists(model);
            LoadLicenseFlags(model);

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error displaying edit vendor form for ID {VendorID}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the vendor";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Save vendor (create or update)
    /// POST /VendorSearch/Edit
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "VendorModify")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(VendorEditViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                await LoadLookupLists(model);
                LoadLicenseFlags(model);
                return View(model);
            }

            // Business logic: Clear BargeEx fields if not enabled
            if (!model.Vendor.IsBargeExEnabled)
            {
                model.Vendor.BargeExTradingPartnerNum = null;
                model.Vendor.BargeExConfigID = null;
            }

            if (model.Vendor.VendorID == 0)
            {
                // Create new vendor
                var created = await _vendorService.CreateVendorAsync(model.Vendor);

                if (created != null)
                {
                    TempData["SuccessMessage"] = $"Vendor '{model.Vendor.Name}' created successfully";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Failed to create vendor");
                    await LoadLookupLists(model);
                    LoadLicenseFlags(model);
                    return View(model);
                }
            }
            else
            {
                // Update existing vendor
                var success = await _vendorService.UpdateVendorAsync(model.Vendor);

                if (success)
                {
                    TempData["SuccessMessage"] = $"Vendor '{model.Vendor.Name}' updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Failed to update vendor");
                    await LoadLookupLists(model);
                    LoadLicenseFlags(model);
                    return View(model);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving vendor {VendorID}", model.Vendor.VendorID);
            ModelState.AddModelError("", "An error occurred while saving the vendor");
            await LoadLookupLists(model);
            LoadLicenseFlags(model);
            return View(model);
        }
    }

    /// <summary>
    /// Set vendor active/inactive (soft delete)
    /// POST /VendorSearch/SetActive/{id}
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "VendorModify")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(int id, bool isActive)
    {
        try
        {
            var success = await _vendorService.SetVendorActiveAsync(id, isActive);

            if (success)
            {
                var action = isActive ? "activated" : "deactivated";
                TempData["SuccessMessage"] = $"Vendor {action} successfully";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update vendor status";
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting vendor {VendorID} active status to {IsActive}", id, isActive);
            TempData["ErrorMessage"] = "An error occurred while updating the vendor";
            return RedirectToAction(nameof(Index));
        }
    }

    #endregion

    #region VendorContact Modals/CRUD

    /// <summary>
    /// Render contact create/edit modal HTML
    /// GET /VendorSearch/ContactModal?vendorId=123&contactId=456
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "VendorModify")]
    public async Task<IActionResult> ContactModal(int vendorId, int? contactId)
    {
        try
        {
            if (vendorId <= 0)
                return BadRequest("VendorID is required");

            var contact = contactId.HasValue && contactId.Value > 0
                ? await _vendorService.GetContactByIdAsync(vendorId, contactId.Value)
                : new VendorContactDto { VendorID = vendorId };

            if (contactId.HasValue && contact == null)
                return NotFound($"Contact with ID {contactId.Value} not found for vendor {vendorId}");

            var model = new VendorContactEditViewModel
            {
                Contact = contact!,
                UnitTowLicenseActive = true, // TODO: replace with license check
                PortalLicenseActive = true // TODO: replace with license check
            };

            return PartialView("_ContactModal", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading contact modal for vendor {VendorID}", vendorId);
            return StatusCode(500, "An error occurred while loading the contact form");
        }
    }

    /// <summary>
    /// Save contact (create or update) from modal
    /// POST /VendorSearch/SaveContact
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "VendorModify")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveContact(VendorContactDto contact)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return PartialView("_ContactModal", new VendorContactEditViewModel
                {
                    Contact = contact,
                    UnitTowLicenseActive = true, // TODO: replace with license check
                    PortalLicenseActive = true // TODO: replace with license check
                });
            }

            if (contact.VendorContactID == 0)
            {
                var created = await _vendorService.CreateContactAsync(contact);
                if (created == null)
                    return StatusCode(500, "Failed to create contact");
            }
            else
            {
                var success = await _vendorService.UpdateContactAsync(contact);
                if (!success)
                    return StatusCode(500, "Failed to update contact");
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving contact {ContactID} for vendor {VendorID}", contact.VendorContactID, contact.VendorID);
            return StatusCode(500, "An error occurred while saving the contact");
        }
    }

    /// <summary>
    /// Delete contact (from modal grid)
    /// POST /VendorSearch/DeleteContact
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "VendorModify")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteContact(int vendorId, int contactId)
    {
        try
        {
            var success = await _vendorService.DeleteContactAsync(vendorId, contactId);
            if (!success)
                return StatusCode(500, "Failed to delete contact");

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting contact {ContactID} for vendor {VendorID}", contactId, vendorId);
            return StatusCode(500, "An error occurred while deleting the contact");
        }
    }

    #endregion

    #region VendorBusinessUnit Modals/CRUD

    /// <summary>
    /// Render business unit create/edit modal HTML
    /// GET /VendorSearch/BusinessUnitModal?vendorId=123&businessUnitId=456
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "VendorModify")]
    public async Task<IActionResult> BusinessUnitModal(int vendorId, int? businessUnitId)
    {
        try
        {
            if (vendorId <= 0)
                return BadRequest("VendorID is required");

            var unit = businessUnitId.HasValue && businessUnitId.Value > 0
                ? await _vendorService.GetBusinessUnitByIdAsync(vendorId, businessUnitId.Value)
                : new VendorBusinessUnitDto { VendorID = vendorId, IsActive = true };

            if (businessUnitId.HasValue && unit == null)
                return NotFound($"Business unit with ID {businessUnitId.Value} not found for vendor {vendorId}");

            var model = new VendorBusinessUnitEditViewModel
            {
                BusinessUnit = unit!,
                Rivers = await _lookupService.GetRiversAsync(),
                Banks = await _lookupService.GetValidationListAsync("RiverBank")
            };

            return PartialView("_BusinessUnitModal", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading business unit modal for vendor {VendorID}", vendorId);
            return StatusCode(500, "An error occurred while loading the business unit form");
        }
    }

    /// <summary>
    /// Save business unit (create or update) from modal
    /// POST /VendorSearch/SaveBusinessUnit
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "VendorModify")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBusinessUnit(VendorBusinessUnitDto businessUnit)
    {
        try
        {
            // Business rule: clear fuel fields if not a fuel supplier
            if (!businessUnit.IsFuelSupplier)
            {
                businessUnit.IsDefaultFuelSupplier = false;
                businessUnit.MinDiscountQty = null;
                businessUnit.MinDiscountFrequency = null;
            }

            if (!ModelState.IsValid)
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return PartialView("_BusinessUnitModal", new VendorBusinessUnitEditViewModel
                {
                    BusinessUnit = businessUnit,
                    Rivers = await _lookupService.GetRiversAsync(),
                    Banks = await _lookupService.GetValidationListAsync("RiverBank")
                });
            }

            if (businessUnit.VendorBusinessUnitID == 0)
            {
                var created = await _vendorService.CreateBusinessUnitAsync(businessUnit);
                if (created == null)
                    return StatusCode(500, "Failed to create business unit");
            }
            else
            {
                var success = await _vendorService.UpdateBusinessUnitAsync(businessUnit);
                if (!success)
                    return StatusCode(500, "Failed to update business unit");
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving business unit {BusinessUnitID} for vendor {VendorID}", businessUnit.VendorBusinessUnitID, businessUnit.VendorID);
            return StatusCode(500, "An error occurred while saving the business unit");
        }
    }

    /// <summary>
    /// Delete business unit (from modal grid)
    /// POST /VendorSearch/DeleteBusinessUnit
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "VendorModify")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBusinessUnit(int vendorId, int businessUnitId)
    {
        try
        {
            var success = await _vendorService.DeleteBusinessUnitAsync(vendorId, businessUnitId);
            if (!success)
                return StatusCode(500, "Failed to delete business unit");

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting business unit {BusinessUnitID} for vendor {VendorID}", businessUnitId, vendorId);
            return StatusCode(500, "An error occurred while deleting the business unit");
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Load lookup lists for dropdowns
    /// </summary>
    private async Task LoadLookupLists(VendorEditViewModel model)
    {
        // Load states
        model.States = await _lookupService.GetStatesAsync();

        // Load payment terms
        model.TermsCodes = await _lookupService.GetValidationListAsync("VendorBillTermsCode");

        // Load BargeEx configurations
        if (model.BargeExGlobalSettingEnabled)
        {
            model.BargeExConfigs = await _lookupService.GetBargeExConfigsAsync();
        }

        // Load rivers
        model.Rivers = await _lookupService.GetRiversAsync();

        // Load banks (validation list)
        model.Banks = await _lookupService.GetValidationListAsync("RiverBank");
    }

    /// <summary>
    /// Load license/feature flags for conditional UI
    /// </summary>
    private void LoadLicenseFlags(VendorEditViewModel model)
    {
        // TODO: Replace with actual license/setting checks
        model.PortalLicenseActive = true;  // Check LicenseList(Licenses.LicenseComponent.Portal).Active
        model.UnitTowLicenseActive = true; // Check LicenseList(Licenses.LicenseComponent.UnitTow).Active
        model.BargeExGlobalSettingEnabled = true; // Check Lists.GlobalSettingList.EnableBargeExBargeLineSupport
    }

    #endregion
}
