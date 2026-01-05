using System;

namespace BargeOps.Shared.Dto;

/// <summary>
/// Search criteria for querying BoatMaintenanceLog records
/// </summary>
public class BoatMaintenanceLogSearchRequest
{
    /// <summary>
    /// Filter by parent BoatLocation ID
    /// </summary>
    public int? LocationID { get; set; }

    /// <summary>
    /// Filter by maintenance type: 'Boat Status', 'Change Division/Facility', or 'Change Boat Role'
    /// </summary>
    public string? MaintenanceType { get; set; }

    /// <summary>
    /// Filter by status value (only applicable when MaintenanceType = 'Boat Status')
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Filter by division (only applicable when MaintenanceType = 'Change Division/Facility')
    /// </summary>
    public string? Division { get; set; }

    /// <summary>
    /// Filter by start date range - from
    /// </summary>
    public DateTime? StartDateFrom { get; set; }

    /// <summary>
    /// Filter by start date range - to
    /// </summary>
    public DateTime? StartDateTo { get; set; }
}
