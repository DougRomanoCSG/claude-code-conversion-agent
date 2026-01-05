using BargeOps.Shared.Dto;

namespace Admin.Infrastructure.Abstractions;

/// <summary>
/// Repository interface for BargeSeries data access operations.
/// Handles CRUD operations and search functionality for barge series and their draft tonnage data.
/// </summary>
public interface IBargeSeriesRepository
{
    /// <summary>
    /// Searches for barge series based on filter criteria with pagination and sorting.
    /// </summary>
    /// <param name="request">Search criteria including filters, sorting, and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result set of BargeSeries DTOs</returns>
    Task<PagedResult<BargeSeriesDto>> SearchAsync(
        BargeSeriesSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a complete list of all barge series for dropdown/lookup purposes.
    /// This method is typically used for cached dropdown lists.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of all BargeSeries DTOs</returns>
    Task<IEnumerable<BargeSeriesDto>> GetListAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single barge series by ID, including its child draft tonnage records.
    /// </summary>
    /// <param name="id">BargeSeries ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>BargeSeries DTO with child drafts, or null if not found</returns>
    Task<BargeSeriesDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new barge series with draft tonnage records.
    /// Saves both parent and child records in a single transaction.
    /// </summary>
    /// <param name="bargeSeries">BargeSeries DTO to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created BargeSeries DTO with generated ID</returns>
    Task<BargeSeriesDto> CreateAsync(
        BargeSeriesDto bargeSeries,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing barge series and its draft tonnage records.
    /// Handles insert, update, and delete of child draft records in a single transaction.
    /// </summary>
    /// <param name="bargeSeries">BargeSeries DTO to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated BargeSeries DTO</returns>
    Task<BargeSeriesDto> UpdateAsync(
        BargeSeriesDto bargeSeries,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a barge series by setting its IsActive flag.
    /// CRITICAL: This entity uses soft delete - do NOT perform hard delete!
    /// </summary>
    /// <param name="id">BargeSeries ID</param>
    /// <param name="isActive">Active status (false = soft deleted)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> SetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all draft tonnage records for a specific barge series.
    /// </summary>
    /// <param name="bargeSeriesId">BargeSeries ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of BargeSeriesDraft DTOs (typically 14 rows for feet 0-13)</returns>
    Task<IEnumerable<BargeSeriesDraftDto>> GetDraftsAsync(
        int bargeSeriesId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk upserts (insert or update) draft tonnage records for a barge series.
    /// Used when pasting data from clipboard or bulk editing the tonnage grid.
    /// </summary>
    /// <param name="bargeSeriesId">BargeSeries ID</param>
    /// <param name="drafts">Collection of draft tonnage records to upsert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated collection of draft records</returns>
    Task<IEnumerable<BargeSeriesDraftDto>> UpsertDraftsAsync(
        int bargeSeriesId,
        IEnumerable<BargeSeriesDraftDto> drafts,
        CancellationToken cancellationToken = default);
}
