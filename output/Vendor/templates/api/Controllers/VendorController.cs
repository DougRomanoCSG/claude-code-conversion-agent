using Microsoft.AspNetCore.Mvc;
using BargeOps.Shared.Dto;
using BargeOps.Shared.Models;
using Admin.Domain.Services;

namespace Admin.Api.Controllers;

/// <summary>
/// Vendor API Controller
/// Authentication: [ApiKey] attribute (NOT Windows Auth)
/// Reference: FacilityController.cs, BoatLocationController.cs
/// </summary>
[ApiKey]
[ApiController]
[Route("api/[controller]")]
public class VendorController : ControllerBase
{
    private readonly IVendorService _vendorService;
    private readonly ILogger<VendorController> _logger;

    public VendorController(IVendorService vendorService, ILogger<VendorController> logger)
    {
        _vendorService = vendorService;
        _logger = logger;
    }

    #region Vendor Endpoints

    /// <summary>
    /// Search vendors with paging
    /// </summary>
    /// <param name="request">Search criteria</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>Paged list of vendors</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<VendorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<VendorDto>>> GetVendors(
        [FromQuery] VendorSearchRequest request,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        try
        {
            var result = await _vendorService.SearchVendorsAsync(request, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching vendors");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while searching vendors");
        }
    }

    /// <summary>
    /// Get vendors for DataTables server-side processing
    /// </summary>
    /// <param name="request">DataTables request</param>
    /// <param name="searchCriteria">Vendor search criteria</param>
    /// <returns>DataTables response</returns>
    [HttpPost("datatable")]
    [ProducesResponseType(typeof(DataTableResponse<VendorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<DataTableResponse<VendorDto>>> GetVendorDataTable(
        [FromBody] DataTableRequest request,
        [FromQuery] VendorSearchRequest searchCriteria)
    {
        try
        {
            var result = await _vendorService.GetVendorDataTableAsync(request, searchCriteria);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vendor DataTable");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while getting vendor data");
        }
    }

    /// <summary>
    /// Get vendor by ID with child collections
    /// </summary>
    /// <param name="id">Vendor ID</param>
    /// <returns>Vendor DTO with contacts and business units</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(VendorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VendorDto>> GetVendor(int id)
    {
        try
        {
            var vendor = await _vendorService.GetVendorByIdAsync(id);

            if (vendor == null)
                return NotFound($"Vendor with ID {id} not found");

            return Ok(vendor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vendor {VendorID}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while getting the vendor");
        }
    }

    /// <summary>
    /// Create new vendor
    /// </summary>
    /// <param name="vendor">Vendor DTO</param>
    /// <returns>Created vendor with new ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(VendorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VendorDto>> CreateVendor([FromBody] VendorDto vendor)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var newId = await _vendorService.CreateVendorAsync(vendor);
            vendor.VendorID = newId;

            return CreatedAtAction(nameof(GetVendor), new { id = newId }, vendor);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error creating vendor");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vendor");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the vendor");
        }
    }

    /// <summary>
    /// Update existing vendor
    /// </summary>
    /// <param name="id">Vendor ID</param>
    /// <param name="vendor">Vendor DTO</param>
    /// <returns>No content on success</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateVendor(int id, [FromBody] VendorDto vendor)
    {
        try
        {
            if (id != vendor.VendorID)
                return BadRequest("Vendor ID mismatch");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _vendorService.GetVendorByIdAsync(id);
            if (existing == null)
                return NotFound($"Vendor with ID {id} not found");

            await _vendorService.UpdateVendorAsync(vendor);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating vendor {VendorID}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vendor {VendorID}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the vendor");
        }
    }

    /// <summary>
    /// Set vendor active/inactive (soft delete)
    /// </summary>
    /// <param name="id">Vendor ID</param>
    /// <param name="request">Active flag</param>
    /// <returns>No content on success</returns>
    [HttpPatch("{id}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetActive(int id, [FromBody] SetActiveRequest request)
    {
        try
        {
            var existing = await _vendorService.GetVendorByIdAsync(id);
            if (existing == null)
                return NotFound($"Vendor with ID {id} not found");

            await _vendorService.SetVendorActiveAsync(id, request.IsActive);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting vendor {VendorID} active status", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the vendor");
        }
    }

    #endregion

    #region VendorContact Endpoints

    /// <summary>
    /// Get all contacts for a vendor
    /// </summary>
    /// <param name="vendorId">Vendor ID</param>
    /// <returns>List of vendor contacts</returns>
    [HttpGet("{vendorId}/contacts")]
    [ProducesResponseType(typeof(IEnumerable<VendorContactDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<VendorContactDto>>> GetContacts(int vendorId)
    {
        try
        {
            var contacts = await _vendorService.GetContactsAsync(vendorId);
            return Ok(contacts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contacts for vendor {VendorID}", vendorId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while getting contacts");
        }
    }

    /// <summary>
    /// Get contact by ID
    /// </summary>
    /// <param name="vendorId">Vendor ID</param>
    /// <param name="contactId">Contact ID</param>
    /// <returns>Vendor contact</returns>
    [HttpGet("{vendorId}/contacts/{contactId}")]
    [ProducesResponseType(typeof(VendorContactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VendorContactDto>> GetContact(int vendorId, int contactId)
    {
        try
        {
            var contact = await _vendorService.GetContactByIdAsync(contactId);

            if (contact == null || contact.VendorID != vendorId)
                return NotFound($"Contact with ID {contactId} not found for vendor {vendorId}");

            return Ok(contact);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contact {ContactID} for vendor {VendorID}", contactId, vendorId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while getting the contact");
        }
    }

    /// <summary>
    /// Create new vendor contact
    /// </summary>
    /// <param name="vendorId">Vendor ID</param>
    /// <param name="contact">Contact DTO</param>
    /// <returns>Created contact with new ID</returns>
    [HttpPost("{vendorId}/contacts")]
    [ProducesResponseType(typeof(VendorContactDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VendorContactDto>> CreateContact(int vendorId, [FromBody] VendorContactDto contact)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            contact.VendorID = vendorId;

            var newId = await _vendorService.CreateContactAsync(contact);
            contact.VendorContactID = newId;

            return CreatedAtAction(nameof(GetContact), new { vendorId, contactId = newId }, contact);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contact for vendor {VendorID}", vendorId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the contact");
        }
    }

    /// <summary>
    /// Update vendor contact
    /// </summary>
    /// <param name="vendorId">Vendor ID</param>
    /// <param name="contactId">Contact ID</param>
    /// <param name="contact">Contact DTO</param>
    /// <returns>No content on success</returns>
    [HttpPut("{vendorId}/contacts/{contactId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateContact(int vendorId, int contactId, [FromBody] VendorContactDto contact)
    {
        try
        {
            if (contactId != contact.VendorContactID)
                return BadRequest("Contact ID mismatch");

            if (vendorId != contact.VendorID)
                return BadRequest("Vendor ID mismatch");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _vendorService.GetContactByIdAsync(contactId);
            if (existing == null)
                return NotFound($"Contact with ID {contactId} not found");

            await _vendorService.UpdateContactAsync(contact);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating contact {ContactID} for vendor {VendorID}", contactId, vendorId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the contact");
        }
    }

    /// <summary>
    /// Delete vendor contact
    /// </summary>
    /// <param name="vendorId">Vendor ID</param>
    /// <param name="contactId">Contact ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{vendorId}/contacts/{contactId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteContact(int vendorId, int contactId)
    {
        try
        {
            var existing = await _vendorService.GetContactByIdAsync(contactId);
            if (existing == null || existing.VendorID != vendorId)
                return NotFound($"Contact with ID {contactId} not found for vendor {vendorId}");

            await _vendorService.DeleteContactAsync(contactId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting contact {ContactID} for vendor {VendorID}", contactId, vendorId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the contact");
        }
    }

    #endregion

    #region VendorBusinessUnit Endpoints

    /// <summary>
    /// Get all business units for a vendor
    /// </summary>
    /// <param name="vendorId">Vendor ID</param>
    /// <returns>List of vendor business units</returns>
    [HttpGet("{vendorId}/business-units")]
    [ProducesResponseType(typeof(IEnumerable<VendorBusinessUnitDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<VendorBusinessUnitDto>>> GetBusinessUnits(int vendorId)
    {
        try
        {
            var businessUnits = await _vendorService.GetBusinessUnitsAsync(vendorId);
            return Ok(businessUnits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting business units for vendor {VendorID}", vendorId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while getting business units");
        }
    }

    /// <summary>
    /// Get business unit by ID
    /// </summary>
    /// <param name="vendorId">Vendor ID</param>
    /// <param name="businessUnitId">Business Unit ID</param>
    /// <returns>Vendor business unit</returns>
    [HttpGet("{vendorId}/business-units/{businessUnitId}")]
    [ProducesResponseType(typeof(VendorBusinessUnitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VendorBusinessUnitDto>> GetBusinessUnit(int vendorId, int businessUnitId)
    {
        try
        {
            var businessUnit = await _vendorService.GetBusinessUnitByIdAsync(businessUnitId);

            if (businessUnit == null || businessUnit.VendorID != vendorId)
                return NotFound($"Business unit with ID {businessUnitId} not found for vendor {vendorId}");

            return Ok(businessUnit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting business unit {BusinessUnitID} for vendor {VendorID}", businessUnitId, vendorId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while getting the business unit");
        }
    }

    /// <summary>
    /// Create new vendor business unit
    /// </summary>
    /// <param name="vendorId">Vendor ID</param>
    /// <param name="businessUnit">Business unit DTO</param>
    /// <returns>Created business unit with new ID</returns>
    [HttpPost("{vendorId}/business-units")]
    [ProducesResponseType(typeof(VendorBusinessUnitDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VendorBusinessUnitDto>> CreateBusinessUnit(int vendorId, [FromBody] VendorBusinessUnitDto businessUnit)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            businessUnit.VendorID = vendorId;

            var newId = await _vendorService.CreateBusinessUnitAsync(businessUnit);
            businessUnit.VendorBusinessUnitID = newId;

            return CreatedAtAction(nameof(GetBusinessUnit), new { vendorId, businessUnitId = newId }, businessUnit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating business unit for vendor {VendorID}", vendorId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the business unit");
        }
    }

    /// <summary>
    /// Update vendor business unit
    /// </summary>
    /// <param name="vendorId">Vendor ID</param>
    /// <param name="businessUnitId">Business Unit ID</param>
    /// <param name="businessUnit">Business unit DTO</param>
    /// <returns>No content on success</returns>
    [HttpPut("{vendorId}/business-units/{businessUnitId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBusinessUnit(int vendorId, int businessUnitId, [FromBody] VendorBusinessUnitDto businessUnit)
    {
        try
        {
            if (businessUnitId != businessUnit.VendorBusinessUnitID)
                return BadRequest("Business unit ID mismatch");

            if (vendorId != businessUnit.VendorID)
                return BadRequest("Vendor ID mismatch");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _vendorService.GetBusinessUnitByIdAsync(businessUnitId);
            if (existing == null)
                return NotFound($"Business unit with ID {businessUnitId} not found");

            await _vendorService.UpdateBusinessUnitAsync(businessUnit);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating business unit {BusinessUnitID} for vendor {VendorID}", businessUnitId, vendorId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the business unit");
        }
    }

    /// <summary>
    /// Delete vendor business unit
    /// </summary>
    /// <param name="vendorId">Vendor ID</param>
    /// <param name="businessUnitId">Business Unit ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{vendorId}/business-units/{businessUnitId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBusinessUnit(int vendorId, int businessUnitId)
    {
        try
        {
            var existing = await _vendorService.GetBusinessUnitByIdAsync(businessUnitId);
            if (existing == null || existing.VendorID != vendorId)
                return NotFound($"Business unit with ID {businessUnitId} not found for vendor {vendorId}");

            await _vendorService.DeleteBusinessUnitAsync(businessUnitId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting business unit {BusinessUnitID} for vendor {VendorID}", businessUnitId, vendorId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the business unit");
        }
    }

    #endregion
}

/// <summary>
/// Request model for SetActive endpoint
/// </summary>
public class SetActiveRequest
{
    public bool IsActive { get; set; }
}
