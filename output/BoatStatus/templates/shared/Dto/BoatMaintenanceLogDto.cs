using System;
using System.ComponentModel.DataAnnotations;

namespace BargeOps.Shared.Dto;

/// <summary>
/// DTO for BoatMaintenanceLog entity - tracks boat status changes, division/facility changes, and boat role changes.
/// ⭐ This DTO is used by BOTH API and UI (no separate domain models in MONO SHARED architecture).
/// </summary>
public class BoatMaintenanceLogDto
{
    /// <summary>
    /// Primary key
    /// </summary>
    [Sortable]
    public int BoatMaintenanceLogID { get; set; }

    /// <summary>
    /// Parent BoatLocation ID
    /// </summary>
    [Required]
    public int LocationID { get; set; }

    /// <summary>
    /// Division (required when MaintenanceType = 'Change Division/Facility', must be blank otherwise)
    /// </summary>
    [StringLength(14)]
    [Filterable]
    [Sortable]
    public string? Division { get; set; }

    /// <summary>
    /// Port Facility ID (optional when MaintenanceType = 'Change Division/Facility', must be blank otherwise)
    /// </summary>
    public int? PortFacilityID { get; set; }

    /// <summary>
    /// Port Facility display name (from Location.LocationName join)
    /// </summary>
    [Sortable]
    public string? PortFacility { get; set; }

    /// <summary>
    /// Maintenance Type: 'Boat Status', 'Change Division/Facility', or 'Change Boat Role'
    /// ⭐ CRITICAL: Cannot be changed once record is created (readonly on edit)
    /// </summary>
    [Required]
    [StringLength(50)]
    [Filterable]
    [Sortable]
    public string MaintenanceType { get; set; } = "Boat Status";

    /// <summary>
    /// Start date/time of the maintenance event
    /// ⭐ CRITICAL: In UI, split into separate date and time inputs (24-hour format)
    /// </summary>
    [Required]
    [Sortable]
    public DateTime StartDateTime { get; set; }

    /// <summary>
    /// Status value (required when MaintenanceType = 'Boat Status', must be blank otherwise)
    /// </summary>
    [StringLength(50)]
    [Filterable]
    [Sortable]
    public string? Status { get; set; }

    /// <summary>
    /// Optional note
    /// </summary>
    [StringLength(500)]
    public string? Note { get; set; }

    /// <summary>
    /// Boat Role ID (required when MaintenanceType = 'Change Boat Role', must be blank otherwise)
    /// </summary>
    public int? BoatRoleID { get; set; }

    /// <summary>
    /// Boat Role display name (from BoatRole.BoatRole join)
    /// </summary>
    [Sortable]
    public string? BoatRole { get; set; }

    /// <summary>
    /// Reference to DeckLogActivity if this record was created from deck log
    /// </summary>
    public int? DeckLogActivityID { get; set; }

    /// <summary>
    /// Last modified date/time (set by database)
    /// Used for optimistic concurrency control
    /// </summary>
    public DateTime? ModifyDateTime { get; set; }

    /// <summary>
    /// User who last modified (set by application)
    /// </summary>
    [StringLength(50)]
    public string? ModifyUser { get; set; }
}
