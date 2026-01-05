using BargeOps.Shared.Dto;

namespace Admin.Domain.Services;

/// <summary>
/// Service interface for BargeSeries business logic.
/// Provides high-level operations for managing barge series and draft tonnage data.
/// </summary>
public interface IBargeSeriesService
{
    /// <summary>
    /// Searches for barge series based on filter criteria.
    /// </summary>
    /// <param name="request">Search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result set of BargeSeries DTOs</returns>
    Task<PagedResult<BargeSeriesDto>> SearchAsync(
        BargeSeriesSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all barge series for dropdown/lookup lists.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of BargeSeries DTOs</returns>
    Task<IEnumerable<BargeSeriesDto>> GetListAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single barge series by ID with draft tonnage records.
    /// </summary>
    /// <param name="id">BargeSeries ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>BargeSeries DTO or null if not found</returns>
    Task<BargeSeriesDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new barge series with draft tonnage records.
    /// Validates business rules before saving.
    /// </summary>
    /// <param name="bargeSeries">BargeSeries DTO to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created BargeSeries DTO</returns>
    Task<BargeSeriesDto> CreateAsync(
        BargeSeriesDto bargeSeries,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing barge series and its draft tonnage records.
    /// Validates business rules before saving.
    /// </summary>
    /// <param name="bargeSeries">BargeSeries DTO to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated BargeSeries DTO</returns>
    Task<BargeSeriesDto> UpdateAsync(
        BargeSeriesDto bargeSeries,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a barge series (soft delete).
    /// </summary>
    /// <param name="id">BargeSeries ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> DeactivateAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reactivates a previously deactivated barge series.
    /// </summary>
    /// <param name="id">BargeSeries ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> ReactivateAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk updates draft tonnage records for a barge series.
    /// Used when pasting data from clipboard or bulk editing.
    /// </summary>
    /// <param name="bargeSeriesId">BargeSeries ID</param>
    /// <param name="drafts">Collection of draft records to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated draft collection</returns>
    Task<IEnumerable<BargeSeriesDraftDto>> UpdateDraftsAsync(
        int bargeSeriesId,
        IEnumerable<BargeSeriesDraftDto> drafts,
        CancellationToken cancellationToken = default);
}
