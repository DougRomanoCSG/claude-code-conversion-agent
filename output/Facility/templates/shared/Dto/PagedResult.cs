namespace BargeOps.Shared.Dto;

/// <summary>
/// Paged result used by repositories/services.
/// </summary>
public class PagedResult<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalRecords { get; set; }
    public int FilteredRecords { get; set; }
}












