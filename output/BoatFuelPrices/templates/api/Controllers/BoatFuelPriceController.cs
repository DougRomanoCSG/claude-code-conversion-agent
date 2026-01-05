using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BargeOps.Shared.Dto;
using Admin.Domain.Services;

namespace Admin.Api.Controllers
{
    /// <summary>
    /// API Controller for BoatFuelPrice operations
    /// All endpoints use IdentityConstants.ApplicationScheme for authentication
    /// </summary>
    [Authorize(Policy = "BoatFuelPrices.View")]
    [ApiController]
    [Route("api/[controller]")]
    public class BoatFuelPriceController : ControllerBase
    {
        private readonly IBoatFuelPriceService _service;

        public BoatFuelPriceController(IBoatFuelPriceService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Search boat fuel prices by criteria
        /// </summary>
        /// <param name="request">Search criteria (effectiveDate, fuelVendorBusinessUnitID)</param>
        /// <returns>List of boat fuel prices</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<BoatFuelPriceDto>), 200)]
        public async Task<ActionResult<IEnumerable<BoatFuelPriceDto>>> Search([FromQuery] BoatFuelPriceSearchRequest request)
        {
            try
            {
                var results = await _service.SearchAsync(request);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while searching boat fuel prices", error = ex.Message });
            }
        }

        /// <summary>
        /// Get a single boat fuel price by ID
        /// </summary>
        /// <param name="id">BoatFuelPriceID</param>
        /// <returns>Boat fuel price DTO</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BoatFuelPriceDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<BoatFuelPriceDto>> GetById(int id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);

                if (result == null)
                    return NotFound(new { message = $"BoatFuelPrice with ID {id} not found" });

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the boat fuel price", error = ex.Message });
            }
        }

        /// <summary>
        /// Create a new boat fuel price
        /// </summary>
        /// <param name="dto">Boat fuel price data</param>
        /// <returns>Created boat fuel price DTO</returns>
        [HttpPost]
        [Authorize(Policy = "BoatFuelPrices.Create")]
        [ProducesResponseType(typeof(BoatFuelPriceDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<BoatFuelPriceDto>> Create([FromBody] BoatFuelPriceDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userName = User.Identity?.Name ?? "System";
                var created = await _service.CreateAsync(dto, userName);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = created.BoatFuelPriceID },
                    created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the boat fuel price", error = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing boat fuel price
        /// </summary>
        /// <param name="id">BoatFuelPriceID</param>
        /// <param name="dto">Updated boat fuel price data</param>
        /// <returns>Updated boat fuel price DTO</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = "BoatFuelPrices.Edit")]
        [ProducesResponseType(typeof(BoatFuelPriceDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<BoatFuelPriceDto>> Update(int id, [FromBody] BoatFuelPriceDto dto)
        {
            if (id != dto.BoatFuelPriceID)
                return BadRequest(new { message = "ID in URL does not match ID in request body" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userName = User.Identity?.Name ?? "System";
                var updated = await _service.UpdateAsync(dto, userName);

                return Ok(updated);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the boat fuel price", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a boat fuel price
        /// </summary>
        /// <param name="id">BoatFuelPriceID</param>
        /// <returns>No content on success</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = "BoatFuelPrices.Delete")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var deleted = await _service.DeleteAsync(id);

                if (!deleted)
                    return NotFound(new { message = $"BoatFuelPrice with ID {id} not found" });

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the boat fuel price", error = ex.Message });
            }
        }
    }
}
