// File: BargeOps.Admin.API/src/Admin.Domain/Models/Facility.cs
// Purpose: Domain model for Facility entity
// Reference: Based on BoatLocation pattern

using System;
using System.Collections.Generic;

namespace Admin.Domain.Models
{
    /// <summary>
    /// Represents a facility location with optional berths and status history.
    /// Maps to Location table with FacilityLocation child entity.
    /// </summary>
    public class Facility
    {
        public int LocationId { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public int? RiverId { get; set; }
        public decimal? Mile { get; set; }
        public bool IsActive { get; set; }
        public string Note { get; set; }

        // FacilityLocation properties
        public int? FacilityLocationId { get; set; }
        public string BargeExCode { get; set; }
        public string Bank { get; set; }
        public int? FacilityTypeId { get; set; }

        // Lock/Gauge specific fields (conditional based on FacilityType)
        public string LockUsaceName { get; set; }
        public decimal? LockFloodStage { get; set; }
        public decimal? LockPoolStage { get; set; }
        public decimal? LockLowWater { get; set; }
        public decimal? LockNormalCurrent { get; set; }
        public decimal? LockHighFlow { get; set; }
        public decimal? LockHighWater { get; set; }
        public decimal? LockCatastrophicLevel { get; set; }

        // NDC (National Data Center) reference data - Read Only
        public string NdcName { get; set; }
        public string NdcLocationDescription { get; set; }
        public string NdcAddress { get; set; }
        public string NdcTown { get; set; }
        public string NdcState { get; set; }
        public string NdcCounty { get; set; }
        public string NdcCountyFips { get; set; }
        public string NdcWaterway { get; set; }
        public string NdcPort { get; set; }
        public string NdcLatitude { get; set; }
        public string NdcLongitude { get; set; }
        public string NdcOperator { get; set; }
        public string NdcOwner { get; set; }
        public string NdcPurpose { get; set; }
        public string NdcRemark { get; set; }

        // Navigation properties
        public string RiverName { get; set; }
        public string FacilityTypeName { get; set; }

        // Audit fields
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }

        // Child collections
        public List<FacilityBerth> Berths { get; set; } = new List<FacilityBerth>();
        public List<FacilityStatus> Statuses { get; set; } = new List<FacilityStatus>();
    }

    /// <summary>
    /// Represents a berth at a facility.
    /// </summary>
    public class FacilityBerth
    {
        public int FacilityBerthId { get; set; }
        public int FacilityLocationId { get; set; }
        public string Name { get; set; }
        public string ShipName { get; set; } // Read-only: Current ship at berth
    }

    /// <summary>
    /// Represents a status record for a facility.
    /// </summary>
    public class FacilityStatus
    {
        public int FacilityStatusId { get; set; }
        public int LocationId { get; set; }
        public int StatusId { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public string Note { get; set; }
        public string StatusName { get; set; } // Navigation property
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
