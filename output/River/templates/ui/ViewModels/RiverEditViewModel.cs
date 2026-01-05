using BargeOps.Shared.Dto;

namespace BargeOpsAdmin.ViewModels;

/// <summary>
/// ViewModel for River edit/create form
/// Contains DTO from BargeOps.Shared (single source of truth)
/// No ViewBag/ViewData - all data on ViewModel (MVVM pattern)
/// </summary>
public class RiverEditViewModel
{
    /// <summary>
    /// River DTO from shared project
    /// Used directly by form - no mapping needed!
    /// </summary>
    public RiverDto River { get; set; } = new();

    /// <summary>
    /// Indicates if this is a new river (create mode)
    /// </summary>
    public bool IsNew => River.RiverID == 0;
}
