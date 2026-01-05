using System.Collections.Generic;
using System.Threading.Tasks;
using BargeOps.Shared.Dto;
using BargeOps.Shared.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BargeOpsAdmin.Services;

/// <summary>
/// UI service interface for River entity
/// HTTP client to call API endpoints
/// </summary>
public interface IRiverService
{
    /// <summary>
    /// Get rivers for DataTables with server-side processing
    /// </summary>
    Task<DataTableResponse<RiverDto>> GetRiversAsync(RiverSearchRequest request);

    /// <summary>
    /// Get a single river by ID
    /// </summary>
    Task<RiverDto?> GetByIdAsync(int riverID);

    /// <summary>
    /// Get active rivers for dropdown lists
    /// </summary>
    Task<List<SelectListItem>> GetRiverListAsync();

    /// <summary>
    /// Create a new river
    /// </summary>
    Task<ApiFetchResult> CreateRiverAsync(RiverDto river);

    /// <summary>
    /// Update an existing river
    /// </summary>
    Task<ApiFetchResult> UpdateRiverAsync(int riverID, RiverDto river);

    /// <summary>
    /// Delete a river (soft delete)
    /// </summary>
    Task<ApiFetchResult> DeleteRiverAsync(int riverID);
}
