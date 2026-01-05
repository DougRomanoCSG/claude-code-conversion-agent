using System.ComponentModel.DataAnnotations;

namespace BargeOps.Shared.Dto;

/// <summary>
/// Barge charter entity DTO - Child entity of Barge
/// Represents charter periods for barges (date ranges with customer)
/// Used by BOTH API and UI
/// </summary>
public class BargeCharterDto
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int BargeCharterID { get; set; }

    /// <summary>
    /// Parent barge ID (FK to Barge)
    /// </summary>
    [Required(ErrorMessage = "Barge ID is required")]
    public int BargeID { get; set; }

    /// <summary>
    /// Charter customer ID (FK to Customer)
    /// Customer who chartered the barge
    /// </summary>
    [Required(ErrorMessage = "Charter company is required")]
    public int ChartererCustomerID { get; set; }

    /// <summary>
    /// Charter customer name (navigation property for display)
    /// </summary>
    public string? ChartererCustomerName { get; set; }

    /// <summary>
    /// Charter start date (Required)
    /// </summary>
    [Required(ErrorMessage = "Start date is required")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Charter end date (Optional)
    /// If null, charter is ongoing
    /// </summary>
    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Daily charter rate
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Daily rate must be a positive number")]
    public decimal? Rate { get; set; }

    /// <summary>
    /// Charter code (indicates charter status)
    /// Example: 'Y' = Yes, 'N' = No, etc.
    /// </summary>
    [StringLength(1)]
    public string? CharterCode { get; set; }

    /// <summary>
    /// Charter code description (navigation property for display)
    /// Example: "Chartered", "Not Chartered"
    /// </summary>
    public string? CharterCodeDesc { get; set; }

    /// <summary>
    /// Charter notes (free text)
    /// </summary>
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string? Notes { get; set; }

    /// <summary>
    /// Audit: Record creation timestamp
    /// </summary>
    public DateTime CreateDateTime { get; set; }

    /// <summary>
    /// Audit: Record last modification timestamp
    /// </summary>
    public DateTime ModifyDateTime { get; set; }

    /// <summary>
    /// Audit: User who created the record
    /// </summary>
    [StringLength(100)]
    public string CreateUser { get; set; } = string.Empty;

    /// <summary>
    /// Audit: User who last modified the record
    /// </summary>
    [StringLength(100)]
    public string ModifyUser { get; set; } = string.Empty;
}
