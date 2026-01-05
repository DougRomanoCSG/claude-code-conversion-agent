using System.ComponentModel.DataAnnotations;

namespace BargeOps.Shared.Dto;

/// <summary>
/// Search criteria DTO for BargeSeries search operations.
/// Used for filtering, sorting, and pagination.
/// </summary>
public class BargeSeriesSearchRequest
{
    /// <summary>
    /// Series name filter (partial match, case-insensitive).
    /// </summary>
    [Display(Name = "Series")]
    public string? Name { get; set; }

    /// <summary>
    /// Customer ID filter (owner of the barge series).
    /// </summary>
    [Display(Name = "Owner")]
    public int? CustomerID { get; set; }

    /// <summary>
    /// Hull type filter.
    /// </summary>
    [Display(Name = "Hull Type")]
    public string? HullType { get; set; }

    /// <summary>
    /// Cover type filter.
    /// </summary>
    [Display(Name = "Cover Type")]
    public string? CoverType { get; set; }

    /// <summary>
    /// Filter to show only active records.
    /// </summary>
    [Display(Name = "Active Only")]
    public bool ActiveOnly { get; set; } = true;

    /// <summary>
    /// Pagination: starting index (0-based).
    /// </summary>
    public int Start { get; set; } = 0;

    /// <summary>
    /// Pagination: number of records to return.
    /// </summary>
    public int Length { get; set; } = 10;

    /// <summary>
    /// Column name to sort by.
    /// </summary>
    public string? SortColumn { get; set; } = "Name";

    /// <summary>
    /// Sort direction: "asc" or "desc".
    /// </summary>
    public string? SortDirection { get; set; } = "asc";
}
