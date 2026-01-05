using BargeOps.Shared.Models;

namespace BargeOps.Shared.Dto;

/// <summary>
/// Search request DTO for River entity
/// Extends DataTableRequest for server-side DataTables processing
/// </summary>
public class RiverSearchRequest : DataTableRequest
{
    /// <summary>
    /// Filter by river code (partial match)
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Filter by river/waterway name (partial match)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Filter by active status only
    /// </summary>
    public bool ActiveOnly { get; set; } = true;
}
