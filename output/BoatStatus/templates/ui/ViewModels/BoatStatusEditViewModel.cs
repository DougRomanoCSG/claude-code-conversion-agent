using Microsoft.AspNetCore.Mvc.Rendering;
using BargeOps.Shared.Dto;
using System.ComponentModel.DataAnnotations;

namespace BargeOpsAdmin.ViewModels;

/// <summary>
/// ViewModel for Boat Status (BoatMaintenanceLog) edit/detail form
/// ⭐ MVVM Pattern: NO ViewBag/ViewData - all data on ViewModel
/// ⭐ Contains DTOs from BargeOps.Shared directly
/// </summary>
public class BoatStatusEditViewModel
{
    /// <summary>
    /// Main maintenance log DTO (from BargeOps.Shared)
    /// ⭐ Used directly by view - no mapping needed!
    /// </summary>
    public BoatMaintenanceLogDto MaintenanceLog { get; set; } = new();

    /// <summary>
    /// Parent BoatLocation ID
    /// ⭐ CRITICAL: Use uppercase ID (LocationID, NOT LocationId)
    /// </summary>
    public int LocationID { get; set; }

    /// <summary>
    /// Boat name for display in form title
    /// </summary>
    [Display(Name = "Boat Name")]
    public string BoatName { get; set; } = string.Empty;

    /// <summary>
    /// All maintenance logs for this boat (for DataTables grid)
    /// </summary>
    public List<BoatMaintenanceLogDto> MaintenanceLogs { get; set; } = new();

    /// <summary>
    /// Status dropdown list (for MaintenanceType = 'Boat Status')
    /// ⭐ IEnumerable<SelectListItem> on ViewModel (MVVM pattern)
    /// </summary>
    public IEnumerable<SelectListItem> StatusList { get; set; } = new List<SelectListItem>();

    /// <summary>
    /// Division dropdown list (for MaintenanceType = 'Change Division/Facility')
    /// ⭐ Excludes 'Freight' division as per legacy logic
    /// </summary>
    public IEnumerable<SelectListItem> Divisions { get; set; } = new List<SelectListItem>();

    /// <summary>
    /// Port Facility dropdown list (for MaintenanceType = 'Change Division/Facility')
    /// ⭐ Populated dynamically based on Division selection (cascading dropdown)
    /// ⭐ Different data source based on IsFleetBoat
    /// </summary>
    public IEnumerable<SelectListItem> PortFacilities { get; set; } = new List<SelectListItem>();

    /// <summary>
    /// Boat Role dropdown list (for MaintenanceType = 'Change Boat Role')
    /// </summary>
    public IEnumerable<SelectListItem> BoatRoles { get; set; } = new List<SelectListItem>();

    /// <summary>
    /// Flag indicating if this is a new record
    /// ⭐ Used to determine if MaintenanceType can be changed (readonly on edit)
    /// </summary>
    public bool IsNew { get; set; }

    /// <summary>
    /// Flag indicating if boat is a fleet boat
    /// ⭐ Affects PortFacility dropdown population logic
    /// </summary>
    public bool IsFleetBoat { get; set; }

    /// <summary>
    /// Fleet ID (if IsFleetBoat = true)
    /// Used for filtering PortFacility dropdown
    /// </summary>
    public int? FleetID { get; set; }

    /// <summary>
    /// Current maintenance type for tracking
    /// Used for conditional field display
    /// </summary>
    public string CurrentMaintenanceType => MaintenanceLog.MaintenanceType;

    /// <summary>
    /// Validation error messages (if any)
    /// </summary>
    public List<string> ValidationErrors { get; set; } = new();
}

/// <summary>
/// ViewModel for DataTables server-side processing request
/// Used by BoatStatusController.GetMaintenanceLogTable action
/// </summary>
public class BoatStatusDataTableRequest
{
    public int LocationID { get; set; }
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public DataTableSearch Search { get; set; } = new();
    public List<DataTableOrder> Order { get; set; } = new();
    public List<DataTableColumn> Columns { get; set; } = new();
}

public class DataTableSearch
{
    public string Value { get; set; } = string.Empty;
    public bool Regex { get; set; }
}

public class DataTableOrder
{
    public int Column { get; set; }
    public string Dir { get; set; } = "asc";
}

public class DataTableColumn
{
    public string Data { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Searchable { get; set; }
    public bool Orderable { get; set; }
    public DataTableSearch Search { get; set; } = new();
}

/// <summary>
/// ViewModel for DataTables server-side processing response
/// </summary>
/// <typeparam name="T">DTO type (BoatMaintenanceLogDto)</typeparam>
public class BoatStatusDataTableResponse<T>
{
    public int Draw { get; set; }
    public int RecordsTotal { get; set; }
    public int RecordsFiltered { get; set; }
    public IEnumerable<T> Data { get; set; } = new List<T>();
}
