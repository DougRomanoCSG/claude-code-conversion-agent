using BargeOps.Shared.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Admin.Infrastructure.Abstractions
{
    /// <summary>
    /// Repository interface for RiverArea data access.
    /// Returns DTOs directly - NO mapping layer needed!
    /// </summary>
    public interface IRiverAreaRepository
    {
        /// <summary>
        /// Search river areas with filtering and pagination.
        /// Returns DTOs for DataTables server-side processing.
        /// </summary>
        Task<DataTableResponse<RiverAreaListDto>> SearchAsync(RiverAreaSearchRequest request);

        /// <summary>
        /// Get single river area by ID with child segments.
        /// Returns DTO directly from database.
        /// </summary>
        Task<RiverAreaDto> GetByIdAsync(int riverAreaId);

        /// <summary>
        /// Create new river area.
        /// Returns generated ID.
        /// </summary>
        Task<int> CreateAsync(RiverAreaDto riverArea);

        /// <summary>
        /// Update existing river area.
        /// </summary>
        Task UpdateAsync(RiverAreaDto riverArea);

        /// <summary>
        /// Hard delete river area (NOT soft delete).
        /// Legacy pattern maintained for compatibility.
        /// </summary>
        Task DeleteAsync(int riverAreaId);

        /// <summary>
        /// Get all segments for a river area.
        /// </summary>
        Task<IEnumerable<RiverAreaSegmentDto>> GetSegmentsByRiverAreaIdAsync(int riverAreaId);

        /// <summary>
        /// Create new river area segment.
        /// Returns generated ID.
        /// </summary>
        Task<int> CreateSegmentAsync(RiverAreaSegmentDto segment);

        /// <summary>
        /// Update existing river area segment.
        /// </summary>
        Task UpdateSegmentAsync(RiverAreaSegmentDto segment);

        /// <summary>
        /// Delete river area segment.
        /// </summary>
        Task DeleteSegmentAsync(int riverAreaSegmentId);
    }
}
