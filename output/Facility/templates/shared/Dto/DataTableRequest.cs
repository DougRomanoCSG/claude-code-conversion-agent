namespace BargeOps.Shared.Dto;

/// <summary>
/// Base request model for jQuery DataTables server-side processing.
/// </summary>
public class DataTableRequest
{
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }

    /// <summary>
    /// Column key from DataTables "columns[i].data" (typically camelCase JSON field name).
    /// </summary>
    public string? SortColumn { get; set; }

    /// <summary>
    /// Sort direction ("asc" or "desc").
    /// </summary>
    public string? SortDirection { get; set; }
}












