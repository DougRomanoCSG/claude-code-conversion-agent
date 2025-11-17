// File: BargeOps.Admin.API/src/Admin.Domain/Dto/FacilityDto.cs
// Purpose: DTOs for Facility API endpoints
// Reference: Based on BoatLocationDto pattern

using System;
using System.Collections.Generic;

namespace Admin.Domain.Dto
{
    /// <summary>
    /// DTO for Facility search results (list view).
    /// </summary>
    public class FacilityListDto
    {
        public int LocationId { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string RiverName { get; set; }
        public decimal? Mile { get; set; }
        public string Bank { get; set; }
        public string FacilityType { get; set; }
        public string BargeExCode { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// DTO for complete Facility details (edit view).
    /// </summary>
    public class FacilityDto
    {
        public int LocationId { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public int? RiverId { get; set; }
        public string RiverName { get; set; }
        public decimal? Mile { get; set; }
        public string Bank { get; set; }
        public int? FacilityTypeId { get; set; }
        public string FacilityTypeName { get; set; }
        public string BargeExCode { get; set; }
        public bool IsActive { get; set; }
        public string Note { get; set; }

        // Lock/Gauge fields (conditional)
        public string LockUsaceName { get; set; }
        public decimal? LockFloodStage { get; set; }
        public decimal? LockPoolStage { get; set; }
        public decimal? LockLowWater { get; set; }
        public decimal? LockNormalCurrent { get; set; }
        public decimal? LockHighFlow { get; set; }
        public decimal? LockHighWater { get; set; }
        public decimal? LockCatastrophicLevel { get; set; }

        // NDC fields (read-only)
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

        // Audit
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }

        // Child collections
        public List<FacilityBerthDto> Berths { get; set; }
        public List<FacilityStatusDto> Statuses { get; set; }
    }

    /// <summary>
    /// DTO for Facility search request with DataTables parameters.
    /// </summary>
    public class FacilitySearchRequest : DataTableRequest
    {
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string BargeExCode { get; set; }
        public int? RiverId { get; set; }
        public int? FacilityTypeId { get; set; }
        public decimal? StartMile { get; set; }
        public decimal? EndMile { get; set; }
        public bool ActiveOnly { get; set; } = true;
    }

    /// <summary>
    /// DTO for Facility Berth.
    /// </summary>
    public class FacilityBerthDto
    {
        public int FacilityBerthId { get; set; }
        public int FacilityLocationId { get; set; }
        public string Name { get; set; }
        public string ShipName { get; set; } // Read-only
    }

    /// <summary>
    /// DTO for Facility Status.
    /// </summary>
    public class FacilityStatusDto
    {
        public int FacilityStatusId { get; set; }
        public int LocationId { get; set; }
        public int StatusId { get; set; }
        public string StatusName { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public string Note { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    /// <summary>
    /// Base class for DataTables server-side processing requests.
    /// </summary>
    public class DataTableRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public string OrderColumn { get; set; }
        public string OrderDirection { get; set; }
    }

    /// <summary>
    /// Response format for DataTables server-side processing.
    /// </summary>
    public class DataTableResponse<T>
    {
        public int Draw { get; set; }
        public int RecordsTotal { get; set; }
        public int RecordsFiltered { get; set; }
        public List<T> Data { get; set; }
    }
}
