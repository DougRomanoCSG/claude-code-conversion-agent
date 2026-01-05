namespace BargeOps.Shared.Dto;

/// <summary>
/// Standard DataTables response payload.
/// </summary>
public class DataTableResponse<T>
{
    public int Draw { get; set; }
    public int RecordsTotal { get; set; }
    public int RecordsFiltered { get; set; }
    public IEnumerable<T> Data { get; set; } = Array.Empty<T>();
}












