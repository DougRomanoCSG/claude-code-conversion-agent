using Admin.Domain.Services;
using BargeOps.Shared.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Admin.Api.Controllers
{
    /// <summary>
    /// RESTful API controller for RiverArea management.
    /// Handles river areas with geographic segments for pricing, portal, and high water areas.
    ///
    /// Pattern Reference: FacilityController.cs, BoatLocationController.cs
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RiverAreaController : ControllerBase
    {
        private readonly IRiverAreaService _riverAreaService;
        private readonly ILogger<RiverAreaController> _logger;

        public RiverAreaController(
            IRiverAreaService riverAreaService,
            ILogger<RiverAreaController> logger)
        {
            _riverAreaService = riverAreaService;
            _logger = logger;
        }

        /// <summary>
        /// Search river areas with filtering and pagination.
        /// Supports DataTables server-side processing.
        /// </summary>
        /// <param name="request">Search criteria and pagination parameters</param>
        /// <returns>DataTables response with filtered river areas</returns>
        /// <response code="200">Returns the search results</response>
        /// <response code="400">If the request is invalid</response>
        /// <response code="401">If not authenticated</response>
        [HttpPost("search")]
        [ProducesResponseType(typeof(DataTableResponse<RiverAreaListDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<DataTableResponse<RiverAreaListDto>>> Search([FromBody] RiverAreaSearchRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Search request cannot be null");
                }

                var result = await _riverAreaService.SearchAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching river areas");
                return StatusCode(500, "An error occurred while searching river areas");
            }
        }

        /// <summary>
        /// Get river area by ID with child segments.
        /// </summary>
        /// <param name="id">River area ID</param>
        /// <returns>River area with segments</returns>
        /// <response code="200">Returns the river area</response>
        /// <response code="404">If river area not found</response>
        /// <response code="401">If not authenticated</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(RiverAreaDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<RiverAreaDto>> GetById(int id)
        {
            try
            {
                var riverArea = await _riverAreaService.GetByIdAsync(id);

                if (riverArea == null)
                {
                    return NotFound($"River area with ID {id} not found");
                }

                return Ok(riverArea);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving river area {RiverAreaId}", id);
                return StatusCode(500, "An error occurred while retrieving the river area");
            }
        }

        /// <summary>
        /// Create new river area.
        /// </summary>
        /// <param name="riverArea">River area to create</param>
        /// <returns>Created river area with generated ID</returns>
        /// <response code="201">Returns the created river area</response>
        /// <response code="400">If validation fails</response>
        /// <response code="401">If not authenticated</response>
        [HttpPost]
        [ProducesResponseType(typeof(RiverAreaDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<RiverAreaDto>> Create([FromBody] RiverAreaDto riverArea)
        {
            try
            {
                if (riverArea == null)
                {
                    return BadRequest("River area cannot be null");
                }

                // Create river area
                var id = await _riverAreaService.CreateAsync(riverArea);

                // Retrieve created river area
                riverArea.RiverAreaID = id;
                var created = await _riverAreaService.GetByIdAsync(id);

                return CreatedAtAction(nameof(GetById), new { id }, created);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error creating river area");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating river area");
                return StatusCode(500, "An error occurred while creating the river area");
            }
        }

        /// <summary>
        /// Update existing river area.
        /// </summary>
        /// <param name="id">River area ID</param>
        /// <param name="riverArea">Updated river area data</param>
        /// <returns>No content on success</returns>
        /// <response code="204">River area updated successfully</response>
        /// <response code="400">If validation fails or IDs don't match</response>
        /// <response code="404">If river area not found</response>
        /// <response code="401">If not authenticated</response>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Update(int id, [FromBody] RiverAreaDto riverArea)
        {
            try
            {
                if (riverArea == null)
                {
                    return BadRequest("River area cannot be null");
                }

                if (id != riverArea.RiverAreaID)
                {
                    return BadRequest("ID in URL does not match ID in body");
                }

                await _riverAreaService.UpdateAsync(riverArea);

                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "River area {RiverAreaId} not found", id);
                return NotFound(ex.Message);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error updating river area {RiverAreaId}", id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating river area {RiverAreaId}", id);
                return StatusCode(500, "An error occurred while updating the river area");
            }
        }

        /// <summary>
        /// Delete river area (hard delete).
        /// Cascades to child segments.
        /// </summary>
        /// <param name="id">River area ID</param>
        /// <returns>No content on success</returns>
        /// <response code="204">River area deleted successfully</response>
        /// <response code="404">If river area not found</response>
        /// <response code="401">If not authenticated</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _riverAreaService.DeleteAsync(id);

                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "River area {RiverAreaId} not found", id);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting river area {RiverAreaId}", id);
                return StatusCode(500, "An error occurred while deleting the river area");
            }
        }

        /// <summary>
        /// Get all segments for a river area.
        /// </summary>
        /// <param name="id">River area ID</param>
        /// <returns>List of river area segments</returns>
        /// <response code="200">Returns the segments</response>
        /// <response code="404">If river area not found</response>
        /// <response code="401">If not authenticated</response>
        [HttpGet("{id}/segments")]
        [ProducesResponseType(typeof(IEnumerable<RiverAreaSegmentDto>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<IEnumerable<RiverAreaSegmentDto>>> GetSegments(int id)
        {
            try
            {
                // Verify river area exists
                var riverArea = await _riverAreaService.GetByIdAsync(id);
                if (riverArea == null)
                {
                    return NotFound($"River area with ID {id} not found");
                }

                var segments = await _riverAreaService.GetSegmentsByRiverAreaIdAsync(id);
                return Ok(segments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving segments for river area {RiverAreaId}", id);
                return StatusCode(500, "An error occurred while retrieving segments");
            }
        }

        /// <summary>
        /// Create new river area segment.
        /// </summary>
        /// <param name="id">River area ID</param>
        /// <param name="segment">Segment to create</param>
        /// <returns>Created segment with generated ID</returns>
        /// <response code="201">Returns the created segment</response>
        /// <response code="400">If validation fails or river area IDs don't match</response>
        /// <response code="404">If river area not found</response>
        /// <response code="401">If not authenticated</response>
        [HttpPost("{id}/segments")]
        [ProducesResponseType(typeof(RiverAreaSegmentDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<RiverAreaSegmentDto>> CreateSegment(int id, [FromBody] RiverAreaSegmentDto segment)
        {
            try
            {
                if (segment == null)
                {
                    return BadRequest("Segment cannot be null");
                }

                // Verify river area exists
                var riverArea = await _riverAreaService.GetByIdAsync(id);
                if (riverArea == null)
                {
                    return NotFound($"River area with ID {id} not found");
                }

                // Ensure segment is associated with correct river area
                segment.RiverAreaID = id;

                // Validate segment
                var errors = new List<string>();

                if (string.IsNullOrWhiteSpace(segment.River))
                {
                    errors.Add("River is required");
                }
                else if (segment.River.Length > 3)
                {
                    errors.Add("River cannot exceed 3 characters");
                }

                if (!segment.StartMile.HasValue)
                {
                    errors.Add("Start mile is required");
                }

                if (!segment.EndMile.HasValue)
                {
                    errors.Add("End mile is required");
                }

                if (segment.StartMile.HasValue && segment.EndMile.HasValue
                    && segment.StartMile.Value >= segment.EndMile.Value)
                {
                    errors.Add("Start mile must be less than end mile");
                }

                if (errors.Any())
                {
                    return BadRequest(string.Join("; ", errors));
                }

                // Note: Segment creation would typically go through the repository
                // For now, segments are managed as part of the parent RiverArea update
                return StatusCode(501, "Segment creation should be done through parent river area update");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating segment for river area {RiverAreaId}", id);
                return StatusCode(500, "An error occurred while creating the segment");
            }
        }

        /// <summary>
        /// Update existing river area segment.
        /// </summary>
        /// <param name="segmentId">Segment ID</param>
        /// <param name="segment">Updated segment data</param>
        /// <returns>No content on success</returns>
        /// <response code="204">Segment updated successfully</response>
        /// <response code="400">If validation fails or IDs don't match</response>
        /// <response code="404">If segment not found</response>
        /// <response code="401">If not authenticated</response>
        [HttpPut("segments/{segmentId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> UpdateSegment(int segmentId, [FromBody] RiverAreaSegmentDto segment)
        {
            try
            {
                if (segment == null)
                {
                    return BadRequest("Segment cannot be null");
                }

                if (segmentId != segment.RiverAreaSegmentID)
                {
                    return BadRequest("Segment ID in URL does not match ID in body");
                }

                // Note: Segment updates would typically go through the repository
                // For now, segments are managed as part of the parent RiverArea update
                return StatusCode(501, "Segment updates should be done through parent river area update");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating segment {SegmentId}", segmentId);
                return StatusCode(500, "An error occurred while updating the segment");
            }
        }

        /// <summary>
        /// Delete river area segment.
        /// </summary>
        /// <param name="segmentId">Segment ID</param>
        /// <returns>No content on success</returns>
        /// <response code="204">Segment deleted successfully</response>
        /// <response code="404">If segment not found</response>
        /// <response code="401">If not authenticated</response>
        [HttpDelete("segments/{segmentId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> DeleteSegment(int segmentId)
        {
            try
            {
                // Note: Segment deletion would typically go through the repository
                // For now, segments are managed as part of the parent RiverArea update
                return StatusCode(501, "Segment deletion should be done through parent river area update");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting segment {SegmentId}", segmentId);
                return StatusCode(500, "An error occurred while deleting the segment");
            }
        }

        /// <summary>
        /// Validate river area business rules.
        /// Useful for client-side pre-validation before submission.
        /// </summary>
        /// <param name="riverArea">River area to validate</param>
        /// <returns>List of validation errors (empty if valid)</returns>
        /// <response code="200">Returns validation results</response>
        /// <response code="400">If river area is null</response>
        /// <response code="401">If not authenticated</response>
        [HttpPost("validate")]
        [ProducesResponseType(typeof(List<string>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<List<string>>> Validate([FromBody] RiverAreaDto riverArea)
        {
            try
            {
                if (riverArea == null)
                {
                    return BadRequest("River area cannot be null");
                }

                var errors = await _riverAreaService.ValidateAsync(riverArea);
                return Ok(errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating river area");
                return StatusCode(500, "An error occurred while validating the river area");
            }
        }
    }
}
