using Csg.ListQuery.Server;
using System;

namespace BargeOps.Shared.Dto;

/// <summary>
/// DTO for Barge Position History (Fleet Position History).
/// Tracks historical positions of barges within fleet tier configurations.
/// </summary>
[Sortable]
[Filterable]
public class BargePositionHistoryDto
{
    /// <summary>
    /// Primary key identifier for the position history record.
    /// </summary>
    public int FleetPositionHistoryID { get; set; }

    /// <summary>
    /// Foreign key to Fleet table.
    /// Required - identifies which fleet this position belongs to.
    /// </summary>
    [Filterable]
    public int FleetID { get; set; }

    /// <summary>
    /// Fleet name for display purposes (from Fleet join).
    /// </summary>
    [Filterable]
    [Sortable]
    public string FleetName { get; set; }

    /// <summary>
    /// Foreign key to Barge table.
    /// Required - resolved from BargeNum via lookup.
    /// </summary>
    public int BargeID { get; set; }

    /// <summary>
    /// Barge number for display and input (from Barge join).
    /// Required field in UI.
    /// </summary>
    [Filterable]
    [Sortable]
    public string BargeNum { get; set; }

    /// <summary>
    /// Foreign key to Tier table.
    /// Optional - null when LeftFleet is true.
    /// </summary>
    [Filterable]
    public int? TierID { get; set; }

    /// <summary>
    /// Tier name for display (from Tier join).
    /// </summary>
    [Filterable]
    [Sortable]
    public string TierName { get; set; }

    /// <summary>
    /// X coordinate within the tier.
    /// Optional - cleared when LeftFleet is true.
    /// Range: -32768 to 32767 (Int16).
    /// </summary>
    public short? TierX { get; set; }

    /// <summary>
    /// Y coordinate within the tier.
    /// Optional - cleared when LeftFleet is true.
    /// Range: -32768 to 32767 (Int16).
    /// </summary>
    public short? TierY { get; set; }

    /// <summary>
    /// Computed tier position string for display.
    /// Format: "(X,Y)" e.g., "(5,3)".
    /// Null when no tier position.
    /// </summary>
    [Sortable]
    public string TierPos { get; set; }

    /// <summary>
    /// Date and time when the position started.
    /// Required field.
    /// Format: MM/dd/yyyy HH:mm (24-hour military time).
    /// </summary>
    [Filterable]
    [Sortable]
    public DateTime PositionStartDateTime { get; set; }

    /// <summary>
    /// Indicates if the barge has left the fleet.
    /// When true, tier position fields (TierID, TierX, TierY) are disabled/cleared.
    /// </summary>
    [Filterable]
    public bool LeftFleet { get; set; }

    /// <summary>
    /// Audit field: Date/time the record was created.
    /// </summary>
    [Sortable]
    public DateTime CreateDateTime { get; set; }

    /// <summary>
    /// Audit field: User who created the record.
    /// </summary>
    public string CreateUser { get; set; }

    /// <summary>
    /// Audit field: Date/time the record was last modified.
    /// Used for optimistic concurrency control.
    /// </summary>
    [Sortable]
    public DateTime? ModifyDateTime { get; set; }

    /// <summary>
    /// Audit field: User who last modified the record.
    /// </summary>
    public string ModifyUser { get; set; }
}
