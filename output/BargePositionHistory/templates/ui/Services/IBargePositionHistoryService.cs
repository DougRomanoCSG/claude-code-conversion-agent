using BargeOps.Shared.Dto;
using Csg.ListQuery;
using System.Threading.Tasks;

namespace BargeOpsAdmin.Services;

/// <summary>
/// Service interface for Barge Position History API client.
/// Target: C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Services\IBargePositionHistoryService.cs
/// </summary>
public interface IBargePositionHistoryService
{
    /// <summary>
    /// Search for barge position history records.
    /// </summary>
    Task<DataTableResponse<BargePositionHistoryDto>> SearchAsync(BargePositionHistorySearchRequest request);

    /// <summary>
    /// Get a single barge position history record by ID.
    /// </summary>
    Task<BargePositionHistoryDto> GetByIdAsync(int id);

    /// <summary>
    /// Create a new barge position history record.
    /// </summary>
    Task<int> CreateAsync(BargePositionHistoryDto dto);

    /// <summary>
    /// Update an existing barge position history record.
    /// </summary>
    Task UpdateAsync(BargePositionHistoryDto dto);

    /// <summary>
    /// Delete a barge position history record.
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Validate that a barge number exists.
    /// </summary>
    Task<bool> ValidateBargeNumAsync(string bargeNum);
}
