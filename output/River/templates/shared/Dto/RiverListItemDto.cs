namespace BargeOps.Shared.Dto;

/// <summary>
/// Lightweight River DTO for dropdown lists and lookups
/// </summary>
public class RiverListItemDto
{
    public int RiverID { get; set; }

    /// <summary>
    /// Three-character river code (DisplayMember for dropdowns)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Full river/waterway name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Starting mile marker (formatted with 2 decimal places)
    /// </summary>
    public string? StartMile { get; set; }

    /// <summary>
    /// Ending mile marker (formatted with 2 decimal places)
    /// </summary>
    public string? EndMile { get; set; }

    /// <summary>
    /// Direction flag: true = low to high (like Mississippi)
    /// </summary>
    public bool IsLowToHighDirection { get; set; }
}
