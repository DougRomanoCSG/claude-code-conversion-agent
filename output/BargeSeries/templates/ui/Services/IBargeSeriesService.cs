using BargeOps.Shared.Dto;

namespace BargeOpsAdmin.Services;

/// <summary>
/// UI service interface for BargeSeries API client operations.
/// Provides HTTP client methods for calling the BargeSeries API.
/// </summary>
public interface IBargeSeriesService
{
    /// <summary>
    /// Searches for barge series with filtering and pagination.
    /// </summary>
    /// <param name="request">Search criteria</param>
    /// <returns>Paged result set</returns>
    Task<PagedResult<BargeSeriesDto>> SearchAsync(BargeSeriesSearchRequest request);

    /// <summary>
    /// Gets all barge series for dropdown lists.
    /// </summary>
    /// <returns>Collection of barge series</returns>
    Task<IEnumerable<BargeSeriesDto>> GetListAsync();

    /// <summary>
    /// Gets a single barge series by ID with draft tonnage records.
    /// </summary>
    /// <param name="id">BargeSeries ID</param>
    /// <returns>BargeSeries DTO or null if not found</returns>
    Task<BargeSeriesDto?> GetByIdAsync(int id);

    /// <summary>
    /// Creates a new barge series.
    /// </summary>
    /// <param name="bargeSeries">BargeSeries DTO to create</param>
    /// <returns>Created BargeSeries DTO</returns>
    Task<BargeSeriesDto> CreateAsync(BargeSeriesDto bargeSeries);

    /// <summary>
    /// Updates an existing barge series.
    /// </summary>
    /// <param name="bargeSeries">BargeSeries DTO to update</param>
    /// <returns>Updated BargeSeries DTO</returns>
    Task<BargeSeriesDto> UpdateAsync(BargeSeriesDto bargeSeries);

    /// <summary>
    /// Deactivates a barge series (soft delete).
    /// </summary>
    /// <param name="id">BargeSeries ID</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Bulk updates draft tonnage records.
    /// </summary>
    /// <param name="bargeSeriesId">BargeSeries ID</param>
    /// <param name="drafts">Draft records to update</param>
    /// <returns>Updated draft collection</returns>
    Task<IEnumerable<BargeSeriesDraftDto>> UpdateDraftsAsync(
        int bargeSeriesId,
        IEnumerable<BargeSeriesDraftDto> drafts);
}
