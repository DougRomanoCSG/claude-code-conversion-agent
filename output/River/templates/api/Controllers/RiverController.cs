using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BargeOps.Shared.Dto;
using BargeOps.Shared.Models;
using Admin.Api.Attributes;
using Admin.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace Admin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiKey]
public class RiverController : ControllerBase
{
    private readonly IRiverService _riverService;

    public RiverController(IRiverService riverService)
    {
        _riverService = riverService;
    }

    /// <summary>
    /// Get a single river by ID
    /// </summary>
    /// <param name="id">River ID</param>
    /// <returns>River DTO or 404 if not found</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(RiverDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<RiverDto>> GetRiver(int id)
    {
        var river = await _riverService.GetByIdAsync(id);

        if (river == null)
        {
            return NotFound();
        }

        return Ok(river);
    }

    /// <summary>
    /// Search rivers with DataTables server-side processing
    /// </summary>
    /// <param name="request">Search criteria with pagination</param>
    /// <returns>Paginated river results</returns>
    [HttpPost("search")]
    [ProducesResponseType(typeof(DataTableResponse<RiverDto>), 200)]
    public async Task<ActionResult<DataTableResponse<RiverDto>>> Search([FromBody] RiverSearchRequest request)
    {
        var result = await _riverService.SearchAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Get active rivers for dropdown lists
    /// </summary>
    /// <returns>List of active rivers</returns>
    [HttpGet("list")]
    [ProducesResponseType(typeof(IEnumerable<RiverListItemDto>), 200)]
    public async Task<ActionResult<IEnumerable<RiverListItemDto>>> GetList()
    {
        var result = await _riverService.GetListAsync();
        return Ok(result);
    }

    /// <summary>
    /// Create a new river
    /// </summary>
    /// <param name="river">River DTO</param>
    /// <returns>New River ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(int), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<int>> Create([FromBody] RiverDto river)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var newId = await _riverService.CreateAsync(river);

        return CreatedAtAction(nameof(GetRiver), new { id = newId }, newId);
    }

    /// <summary>
    /// Update an existing river
    /// </summary>
    /// <param name="id">River ID</param>
    /// <param name="river">River DTO</param>
    /// <returns>No content on success</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int id, [FromBody] RiverDto river)
    {
        if (id != river.RiverID)
        {
            return BadRequest("ID mismatch");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _riverService.UpdateAsync(river);
            return NoContent();
        }
        catch (Exception ex) when (ex.Message.Contains("not found"))
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Delete a river (soft delete via IsActive)
    /// </summary>
    /// <param name="id">River ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _riverService.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex) when (ex.Message.Contains("not found"))
        {
            return NotFound();
        }
    }
}
