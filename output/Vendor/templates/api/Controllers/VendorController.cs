using Admin.Infrastructure.Services;
using BargeOps.Shared.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BargeOps.Admin.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class VendorController : ControllerBase
{
    private readonly IVendorService _vendorService;
    private readonly ILogger<VendorController> _logger;

    public VendorController(
        IVendorService vendorService,
        ILogger<VendorController> logger)
    {
        _vendorService = vendorService;
        _logger = logger;
    }

    /// <summary>
    /// Search vendors with DataTables server-side processing
    /// </summary>
    [HttpPost("search")]
    public async Task<ActionResult<DataTableResponse<VendorDto>>> Search(
        [FromBody] VendorSearchRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _vendorService.SearchAsync(request, cancellationToken);

            return Ok(new DataTableResponse<VendorDto>
            {
                Draw = request.Draw,
                RecordsTotal = result.TotalRecords,
                RecordsFiltered = result.FilteredRecords,
                Data = result.Data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching vendors");
            return StatusCode(500, "An error occurred while searching vendors");
        }
    }

    /// <summary>
    /// Get vendor by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<VendorDto>> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var vendor = await _vendorService.GetByIdAsync(id, cancellationToken);

            if (vendor == null)
            {
                return NotFound($"Vendor with ID {id} not found");
            }

            return Ok(vendor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vendor {VendorId}", id);
            return StatusCode(500, "An error occurred while retrieving the vendor");
        }
    }

    /// <summary>
    /// Create new vendor
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<VendorDto>> Create(
        [FromBody] VendorDto vendor,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _vendorService.CreateAsync(vendor, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.VendorID }, created);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vendor");
            return StatusCode(500, "An error occurred while creating the vendor");
        }
    }

    /// <summary>
    /// Update existing vendor
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<VendorDto>> Update(
        int id,
        [FromBody] VendorDto vendor,
        CancellationToken cancellationToken)
    {
        try
        {
            if (id != vendor.VendorID)
            {
                return BadRequest("ID mismatch");
            }

            var updated = await _vendorService.UpdateAsync(vendor, cancellationToken);
            return Ok(updated);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vendor {VendorId}", id);
            return StatusCode(500, "An error occurred while updating the vendor");
        }
    }

    /// <summary>
    /// Delete vendor
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _vendorService.DeleteAsync(id, cancellationToken);

            if (!deleted)
            {
                return NotFound($"Vendor with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting vendor {VendorId}", id);
            return StatusCode(500, "An error occurred while deleting the vendor");
        }
    }

    /// <summary>
    /// Get vendor contacts
    /// </summary>
    [HttpGet("{id}/contacts")]
    public async Task<ActionResult<IEnumerable<VendorContactDto>>> GetContacts(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var contacts = await _vendorService.GetContactsAsync(id, cancellationToken);
            return Ok(contacts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contacts for vendor {VendorId}", id);
            return StatusCode(500, "An error occurred while retrieving contacts");
        }
    }

    /// <summary>
    /// Create vendor contact
    /// </summary>
    [HttpPost("contacts")]
    public async Task<ActionResult<VendorContactDto>> CreateContact(
        [FromBody] VendorContactDto contact,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _vendorService.CreateContactAsync(contact, cancellationToken);
            return Ok(created);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contact");
            return StatusCode(500, "An error occurred while creating the contact");
        }
    }

    /// <summary>
    /// Update vendor contact
    /// </summary>
    [HttpPut("contacts/{id}")]
    public async Task<ActionResult<VendorContactDto>> UpdateContact(
        int id,
        [FromBody] VendorContactDto contact,
        CancellationToken cancellationToken)
    {
        try
        {
            if (id != contact.VendorContactID)
            {
                return BadRequest("ID mismatch");
            }

            var updated = await _vendorService.UpdateContactAsync(contact, cancellationToken);
            return Ok(updated);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating contact {ContactId}", id);
            return StatusCode(500, "An error occurred while updating the contact");
        }
    }

    /// <summary>
    /// Delete vendor contact
    /// </summary>
    [HttpDelete("contacts/{id}")]
    public async Task<ActionResult> DeleteContact(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _vendorService.DeleteContactAsync(id, cancellationToken);

            if (!deleted)
            {
                return NotFound($"Contact with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting contact {ContactId}", id);
            return StatusCode(500, "An error occurred while deleting the contact");
        }
    }

    /// <summary>
    /// Get vendor business units
    /// </summary>
    [HttpGet("{id}/businessunits")]
    public async Task<ActionResult<IEnumerable<VendorBusinessUnitDto>>> GetBusinessUnits(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var businessUnits = await _vendorService.GetBusinessUnitsAsync(id, cancellationToken);
            return Ok(businessUnits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting business units for vendor {VendorId}", id);
            return StatusCode(500, "An error occurred while retrieving business units");
        }
    }

    /// <summary>
    /// Create vendor business unit
    /// </summary>
    [HttpPost("businessunits")]
    public async Task<ActionResult<VendorBusinessUnitDto>> CreateBusinessUnit(
        [FromBody] VendorBusinessUnitDto businessUnit,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _vendorService.CreateBusinessUnitAsync(businessUnit, cancellationToken);
            return Ok(created);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating business unit");
            return StatusCode(500, "An error occurred while creating the business unit");
        }
    }

    /// <summary>
    /// Update vendor business unit
    /// </summary>
    [HttpPut("businessunits/{id}")]
    public async Task<ActionResult<VendorBusinessUnitDto>> UpdateBusinessUnit(
        int id,
        [FromBody] VendorBusinessUnitDto businessUnit,
        CancellationToken cancellationToken)
    {
        try
        {
            if (id != businessUnit.VendorBusinessUnitID)
            {
                return BadRequest("ID mismatch");
            }

            var updated = await _vendorService.UpdateBusinessUnitAsync(businessUnit, cancellationToken);
            return Ok(updated);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating business unit {BusinessUnitId}", id);
            return StatusCode(500, "An error occurred while updating the business unit");
        }
    }

    /// <summary>
    /// Delete vendor business unit
    /// </summary>
    [HttpDelete("businessunits/{id}")]
    public async Task<ActionResult> DeleteBusinessUnit(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _vendorService.DeleteBusinessUnitAsync(id, cancellationToken);

            if (!deleted)
            {
                return NotFound($"Business unit with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting business unit {BusinessUnitId}", id);
            return StatusCode(500, "An error occurred while deleting the business unit");
        }
    }

    /// <summary>
    /// Get vendor portal groups
    /// </summary>
    [HttpGet("{id}/portalgroups")]
    public async Task<ActionResult<IEnumerable<VendorPortalGroupDto>>> GetPortalGroups(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var portalGroups = await _vendorService.GetPortalGroupsAsync(id, cancellationToken);
            return Ok(portalGroups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting portal groups for vendor {VendorId}", id);
            return StatusCode(500, "An error occurred while retrieving portal groups");
        }
    }

    /// <summary>
    /// Create vendor portal group
    /// </summary>
    [HttpPost("portalgroups")]
    public async Task<ActionResult<VendorPortalGroupDto>> CreatePortalGroup(
        [FromBody] VendorPortalGroupDto portalGroup,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _vendorService.CreatePortalGroupAsync(portalGroup, cancellationToken);
            return Ok(created);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating portal group");
            return StatusCode(500, "An error occurred while creating the portal group");
        }
    }

    /// <summary>
    /// Update vendor portal group
    /// </summary>
    [HttpPut("portalgroups/{id}")]
    public async Task<ActionResult<VendorPortalGroupDto>> UpdatePortalGroup(
        int id,
        [FromBody] VendorPortalGroupDto portalGroup,
        CancellationToken cancellationToken)
    {
        try
        {
            if (id != portalGroup.VendorPortalGroupID)
            {
                return BadRequest("ID mismatch");
            }

            var updated = await _vendorService.UpdatePortalGroupAsync(portalGroup, cancellationToken);
            return Ok(updated);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating portal group {PortalGroupId}", id);
            return StatusCode(500, "An error occurred while updating the portal group");
        }
    }

    /// <summary>
    /// Delete vendor portal group
    /// </summary>
    [HttpDelete("portalgroups/{id}")]
    public async Task<ActionResult> DeletePortalGroup(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _vendorService.DeletePortalGroupAsync(id, cancellationToken);

            if (!deleted)
            {
                return NotFound($"Portal group with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting portal group {PortalGroupId}", id);
            return StatusCode(500, "An error occurred while deleting the portal group");
        }
    }
}
