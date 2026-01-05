using Csg.ListQuery.Server;
using System;
using System.ComponentModel.DataAnnotations;

namespace BargeOps.Shared.Dto
{
    /// <summary>
    /// DTO for Boat Fuel Price entity.
    /// This DTO is used by both API and UI projects - no separate Models!
    /// </summary>
    [Sortable]
    [Filterable]
    public class BoatFuelPriceDto
    {
        /// <summary>
        /// Primary key, auto-generated identity field
        /// </summary>
        public int BoatFuelPriceID { get; set; }

        /// <summary>
        /// Date when the fuel price becomes effective (Required)
        /// </summary>
        [Required(ErrorMessage = "Effective date is required.")]
        [Display(Name = "Effective Date")]
        [Sortable]
        [Filterable]
        public DateTime EffectiveDate { get; set; }

        /// <summary>
        /// Fuel price amount with 4 decimal places precision (Required)
        /// </summary>
        [Required(ErrorMessage = "Fuel price is required.")]
        [Range(0.0001, 999999.9999, ErrorMessage = "Fuel price must be greater than 0.")]
        [Display(Name = "Fuel Price")]
        [Sortable]
        [Filterable]
        public decimal Price { get; set; }

        /// <summary>
        /// Foreign key to VendorBusinessUnit table (Optional)
        /// Identifies the fuel vendor
        /// </summary>
        [Display(Name = "Fuel Vendor")]
        [Filterable]
        public int? FuelVendorBusinessUnitID { get; set; }

        /// <summary>
        /// Display name from VendorBusinessUnit relationship
        /// Populated from BusinessUnit.Name where IsFuelSupplier = true
        /// </summary>
        [Display(Name = "Fuel Vendor")]
        [Sortable]
        [Filterable]
        public string FuelVendor { get; set; }

        /// <summary>
        /// Invoice number from the fuel vendor (Optional, max 50 chars)
        /// Business rule: Must be blank when FuelVendorBusinessUnitID is blank
        /// </summary>
        [MaxLength(50, ErrorMessage = "Vendor inv# must be less than or equal to 50 characters.")]
        [Display(Name = "Vendor Inv #")]
        [Filterable]
        public string InvoiceNumber { get; set; }

        /// <summary>
        /// Audit field: Date/time record was created
        /// </summary>
        public DateTime? CreateDateTime { get; set; }

        /// <summary>
        /// Audit field: User who created the record
        /// </summary>
        [Display(Name = "Created By")]
        public string CreateUser { get; set; }

        /// <summary>
        /// Audit field: Date/time record was last modified
        /// </summary>
        public DateTime? ModifyDateTime { get; set; }

        /// <summary>
        /// Audit field: User who last modified the record
        /// </summary>
        [Display(Name = "Modified By")]
        public string ModifyUser { get; set; }
    }

    /// <summary>
    /// Search request DTO for filtering and sorting BoatFuelPrice records
    /// Inherits from DataTableRequest for server-side DataTables support
    /// </summary>
    public class BoatFuelPriceSearchRequest : DataTableRequest
    {
        /// <summary>
        /// Filter by effective date
        /// Defaults to today's date in the UI
        /// </summary>
        [Display(Name = "Effective Date")]
        public DateTime? EffectiveDate { get; set; }

        /// <summary>
        /// Filter by fuel vendor business unit ID
        /// </summary>
        [Display(Name = "Fuel Vendor")]
        public int? FuelVendorBusinessUnitID { get; set; }

        /// <summary>
        /// Include only active records
        /// </summary>
        public bool ActiveOnly { get; set; } = true;
    }

    /// <summary>
    /// Base class for DataTables server-side processing requests
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
    /// Generic response wrapper for DataTables server-side processing
    /// </summary>
    /// <typeparam name="T">The DTO type</typeparam>
    public class DataTableResponse<T>
    {
        public int Draw { get; set; }
        public int RecordsTotal { get; set; }
        public int RecordsFiltered { get; set; }
        public System.Collections.Generic.List<T> Data { get; set; }
    }
}
