using BargeOps.Shared.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Admin.Domain.Services
{
    /// <summary>
    /// Service interface for RiverArea business logic.
    /// Sits between API Controller and Repository.
    /// Handles validation, business rules, and orchestration.
    /// </summary>
    public interface IRiverAreaService
    {
        /// <summary>
        /// Search river areas with filtering and pagination.
        /// Returns DTOs for DataTables server-side processing.
        /// </summary>
        Task<DataTableResponse<RiverAreaListDto>> SearchAsync(RiverAreaSearchRequest request);

        /// <summary>
        /// Get single river area by ID with child segments.
        /// Returns null if not found.
        /// </summary>
        Task<RiverAreaDto> GetByIdAsync(int riverAreaId);

        /// <summary>
        /// Create new river area with validation.
        /// Returns generated ID.
        /// Throws validation exception if business rules fail.
        /// </summary>
        Task<int> CreateAsync(RiverAreaDto riverArea);

        /// <summary>
        /// Update existing river area with validation.
        /// Throws validation exception if business rules fail.
        /// Throws not found exception if river area doesn't exist.
        /// </summary>
        Task UpdateAsync(RiverAreaDto riverArea);

        /// <summary>
        /// Hard delete river area (NOT soft delete).
        /// Legacy pattern maintained for compatibility.
        /// Cascades to child segments.
        /// Throws not found exception if river area doesn't exist.
        /// </summary>
        Task DeleteAsync(int riverAreaId);

        /// <summary>
        /// Get all segments for a river area.
        /// Returns empty list if none found.
        /// </summary>
        Task<IEnumerable<RiverAreaSegmentDto>> GetSegmentsByRiverAreaIdAsync(int riverAreaId);

        /// <summary>
        /// Validate river area business rules.
        /// Returns list of validation errors (empty if valid).
        /// </summary>
        Task<List<string>> ValidateAsync(RiverAreaDto riverArea);

        /// <summary>
        /// Check if pricing zone segments overlap with other pricing zones.
        /// Only applies when IsPriceZone = true.
        /// Returns list of overlapping river area names (empty if no overlaps).
        /// </summary>
        Task<List<string>> CheckOverlappingPricingZonesAsync(RiverAreaDto riverArea);
    }
}
