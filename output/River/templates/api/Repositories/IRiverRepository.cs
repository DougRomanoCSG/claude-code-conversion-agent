using System.Collections.Generic;
using System.Threading.Tasks;
using BargeOps.Shared.Dto;
using BargeOps.Shared.Models;

namespace Admin.Infrastructure.Abstractions;

/// <summary>
/// Repository interface for River entity
/// Returns DTOs directly from BargeOps.Shared
/// </summary>
public interface IRiverRepository
{
    /// <summary>
    /// Get a single river by ID
    /// </summary>
    Task<RiverDto?> GetByIdAsync(int riverID);

    /// <summary>
    /// Get all rivers
    /// </summary>
    Task<IEnumerable<RiverDto>> GetAllAsync();

    /// <summary>
    /// Search rivers with DataTables server-side processing
    /// </summary>
    Task<DataTableResponse<RiverDto>> SearchAsync(RiverSearchRequest request);

    /// <summary>
    /// Get active rivers for dropdown lists (cached)
    /// </summary>
    Task<IEnumerable<RiverListItemDto>> GetListAsync();

    /// <summary>
    /// Create a new river
    /// </summary>
    /// <returns>New RiverID</returns>
    Task<int> CreateAsync(RiverDto river);

    /// <summary>
    /// Update an existing river
    /// </summary>
    Task UpdateAsync(RiverDto river);

    /// <summary>
    /// Soft delete or activate a river
    /// </summary>
    Task SetActiveAsync(int riverID, bool isActive);
}
