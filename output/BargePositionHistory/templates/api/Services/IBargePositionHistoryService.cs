using BargeOps.Shared.Dto;
using Csg.ListQuery;
using System.Threading.Tasks;

namespace Admin.Domain.Services;

/// <summary>
/// Service interface for Barge Position History business logic.
/// Target: C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Domain\Services\IBargePositionHistoryService.cs
/// </summary>
public interface IBargePositionHistoryService
{
    /// <summary>
    /// Search for barge position history records with filtering and paging.
    /// </summary>
    Task<DataTableResponse<BargePositionHistoryDto>> SearchAsync(BargePositionHistorySearchRequest request);

    /// <summary>
    /// Get a single barge position history record by ID.
    /// </summary>
    Task<BargePositionHistoryDto> GetByIdAsync(int id);

    /// <summary>
    /// Create a new barge position history record.
    /// Validates BargeNum and applies business rules.
    /// </summary>
    Task<int> CreateAsync(BargePositionHistoryDto dto, string modifyUser);

    /// <summary>
    /// Update an existing barge position history record.
    /// Validates BargeNum and applies business rules.
    /// Uses optimistic concurrency.
    /// </summary>
    Task UpdateAsync(BargePositionHistoryDto dto, string modifyUser);

    /// <summary>
    /// Delete a barge position history record.
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Validate that a barge number exists.
    /// </summary>
    Task<bool> ValidateBargeNumAsync(string bargeNum);
}
