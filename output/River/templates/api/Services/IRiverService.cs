using System.Collections.Generic;
using System.Threading.Tasks;
using BargeOps.Shared.Dto;
using BargeOps.Shared.Models;

namespace Admin.Domain.Services;

/// <summary>
/// Service interface for River business logic
/// </summary>
public interface IRiverService
{
    /// <summary>
    /// Get a single river by ID
    /// </summary>
    Task<RiverDto?> GetByIdAsync(int riverID);

    /// <summary>
    /// Search rivers with DataTables server-side processing
    /// </summary>
    Task<DataTableResponse<RiverDto>> SearchAsync(RiverSearchRequest request);

    /// <summary>
    /// Get active rivers for dropdown lists
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
    /// Delete a river (soft delete via IsActive)
    /// </summary>
    Task DeleteAsync(int riverID);
}
