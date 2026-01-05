using BargeOps.Shared.Dto;
using Csg.ListQuery;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Admin.Infrastructure.Abstractions;

/// <summary>
/// Repository interface for Barge Position History data access.
/// Target: C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Abstractions\IBargePositionHistoryRepository.cs
/// </summary>
public interface IBargePositionHistoryRepository
{
    /// <summary>
    /// Search for barge position history records with filtering and paging.
    /// </summary>
    /// <param name="request">Search criteria including FleetID, Date, TierGroupID, BargeNum, IncludeBlankTierPos</param>
    /// <returns>Paged result set for DataTables</returns>
    Task<DataTableResponse<BargePositionHistoryDto>> SearchAsync(BargePositionHistorySearchRequest request);

    /// <summary>
    /// Get a single barge position history record by ID.
    /// </summary>
    /// <param name="id">FleetPositionHistoryID</param>
    /// <returns>BargePositionHistoryDto or null if not found</returns>
    Task<BargePositionHistoryDto> GetByIdAsync(int id);

    /// <summary>
    /// Insert a new barge position history record.
    /// </summary>
    /// <param name="dto">BargePositionHistoryDto with new record data</param>
    /// <param name="modifyUser">Username for audit fields</param>
    /// <returns>New FleetPositionHistoryID</returns>
    Task<int> InsertAsync(BargePositionHistoryDto dto, string modifyUser);

    /// <summary>
    /// Update an existing barge position history record.
    /// Uses optimistic concurrency with ModifyDateTime check.
    /// </summary>
    /// <param name="dto">BargePositionHistoryDto with updated data</param>
    /// <param name="modifyUser">Username for audit fields</param>
    /// <returns>Task</returns>
    /// <exception cref="ConcurrencyException">Thrown if ModifyDateTime does not match current value</exception>
    Task UpdateAsync(BargePositionHistoryDto dto, string modifyUser);

    /// <summary>
    /// Hard delete a barge position history record.
    /// NOTE: This is NOT a soft delete.
    /// </summary>
    /// <param name="id">FleetPositionHistoryID to delete</param>
    /// <returns>Task</returns>
    Task DeleteAsync(int id);

    /// <summary>
    /// Validate that tier coordinates are within tier boundaries.
    /// </summary>
    /// <param name="tierId">TierID to validate against</param>
    /// <param name="tierX">X coordinate</param>
    /// <param name="tierY">Y coordinate</param>
    /// <returns>True if coordinates are valid, false otherwise</returns>
    Task<bool> ValidateTierCoordinatesAsync(int tierId, short tierX, short tierY);

    /// <summary>
    /// Validate that a barge number exists and return its BargeID.
    /// </summary>
    /// <param name="bargeNum">Barge number to validate</param>
    /// <returns>BargeID if found, null otherwise</returns>
    Task<int?> GetBargeIdByNumberAsync(string bargeNum);
}
