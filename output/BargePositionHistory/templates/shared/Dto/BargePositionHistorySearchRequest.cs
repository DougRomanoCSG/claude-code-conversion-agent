using Csg.ListQuery;
using System;

namespace BargeOps.Shared.Dto;

/// <summary>
/// Search criteria for Barge Position History.
/// Extends DataTableRequest for server-side DataTables processing.
/// </summary>
public class BargePositionHistorySearchRequest : DataTableRequest
{
    /// <summary>
    /// Required: Fleet ID to search within.
    /// Passed from parent context.
    /// </summary>
    public int FleetID { get; set; }

    /// <summary>
    /// Required: Search date for position history.
    /// Searches for all positions on this date (full day).
    /// </summary>
    public DateTime? PositionStartDate { get; set; }

    /// <summary>
    /// Required: Tier Group ID for filtering.
    /// Used to filter available tiers in search results.
    /// </summary>
    public int? TierGroupID { get; set; }

    /// <summary>
    /// Optional: Barge number filter.
    /// Searches using LIKE pattern if provided.
    /// </summary>
    public string BargeNum { get; set; }

    /// <summary>
    /// Optional: Include records without tier positions.
    /// Default: false (only show records with tier positions).
    /// </summary>
    public bool IncludeBlankTierPos { get; set; }
}
