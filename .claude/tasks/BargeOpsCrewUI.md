# BargeOps Crewing UI Architecture

This document summarizes the MVC architecture patterns, DataTables usage, UI conventions, view model patterns, authorization, and session management in the BargeOps Crewing UI codebase. It includes file paths, representative code snippets, and notable variations.

---

## 1. Controller → Service → View Workflow

### Controller Layer

**Pattern Description**  
Controllers inherit from a shared `AppController` that centralizes session access and shared helpers (notably DataTables server-side parsing and generic search). Controllers use constructor DI for services and session, then shape view models, validate inputs, and call service methods. Views bind via Razor Tag Helpers (`asp-for`, `asp-action`, `asp-items`) and post back strongly-typed models.

**File Examples**  
- `AppClasses/AppController.cs`  
- `Controllers/CrewMemberController.cs`  
- `Controllers/SearchController.cs`

**Code Snippet**  
```14:95:AppClasses/AppController.cs
public class AppController : Controller
{
    public AppController(AppSession session)
    {
        UserSession = session;
    }

    protected AppSession UserSession { get; set; }

    protected async Task<IActionResult> GetDataTable<TEntity>(IEnumerable<TEntity> queryable, Func<TEntity, string> searchPropertySelector)
    {
        try
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][data]"].FirstOrDefault();
            var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            // ...
            var jsonData = new { draw, recordsFiltered = recordsTotal, recordsTotal, data };
            return Ok(jsonData);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error retrieving data for DataTables: {ex.Message}");
        }
    }
}
```

**Variations**  
- GET actions commonly call `InitSessionVariables` and update `AppSession` state (e.g., `PrevUrl`).  
- POST actions validate `ModelState` and rehydrate lookup lists before re-rendering.

**Dependency Injection**  
Controllers inject services and session via constructor injection, typically storing fields for use in actions.

**File Examples**  
- `Controllers/CrewMemberController.cs`

**Code Snippet**  
```20:41:Controllers/CrewMemberController.cs
public class CrewMemberController : AppController
{
    private readonly ICrewMemberService _crewMemberService;
    private readonly IHitchService _hitchService;
    private readonly ITimeOffService _timeOffService;
    private readonly AppSession _session;
    private readonly ILookupService _lookupService;
    private readonly IBoatService _boatService;

    public CrewMemberController(ICrewMemberService crewMemberService, AppSession session,
        ILookupService lookupService, IHitchService hitchService, ITimeOffService timeOffService, IBoatService boatService) : base(session)
    {
        _crewMemberService = crewMemberService;
        _session = session;
        _lookupService = lookupService;
        _hitchService = hitchService;
        _timeOffService = timeOffService;
        _boatService = boatService;
    }
}
```

**Action Method Patterns**  
- GET actions render views with populated lookups.  
- POST actions validate `ModelState`, rehydrate lookups on error, and redirect on success.  
- AJAX endpoints use `Request.Form` and return JSON via `GetDataTable`.

**Error Handling**  
Controllers use `try/catch` around service calls and return `BadRequest` for HTTP exceptions or generic errors. Validation errors are returned via `ModelState` and re-rendered views.

---

### Service Layer

**Pattern Description**  
Services derive from `CrewingBaseService`, which handles base API configuration, forwarding user identity, and basic auth header setup. Services build `ListRequest` filters, call APIs via `HttpClient`, and map results or validation errors into `ApiFetchResult`.

**File Examples**  
- `Services/CrewingBaseService.cs`  
- `Services/CrewMemberService.cs`

**Code Snippet**  
```12:62:Services/CrewingBaseService.cs
public class CrewingBaseService
{
    protected const string ForwardedUserHeader = "X-Forwarded-User";
    private readonly string _baseAddress;
    private readonly string _clientID;
    private readonly string _secret;
    private readonly ICurrentUserService _currentUserService;

    protected CrewingBaseService(IConfiguration configuration, ICurrentUserService currentUserService)
    {
        Configuration = configuration;
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _clientID = Configuration["CrewingApi:ClientID"];
        _secret = Configuration["CrewingApi:secret"];
        _baseAddress = Configuration["CrewingApi:baseAddress"];
    }

    public HttpClient GetClient()
    {
        var client = new HttpClient { BaseAddress = new Uri(_baseAddress), Timeout = TimeSpan.FromSeconds(30) };
        var userName = UserName;
        if (!string.IsNullOrEmpty(userName) && userName.Contains(' '))
        {
            var lastIndex = userName.LastIndexOf(' ');
            userName = userName.Substring(lastIndex + 1);
        }
        client.DefaultRequestHeaders.Add(ForwardedUserHeader, userName);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Base64Encode(string.Concat(_clientID, ":", _secret)));
        return client;
    }
}
```

**Service Methods**  
- GET: `GetAsync` with query string parameters and JSON deserialization.  
- Search/Filter: build `ListRequest` and `ListFilter`, then POST to `/filter`.  
- POST/PUT: create/update based on ID, map validation errors to `ApiFetchResult`.  
- DELETE: no explicit examples found in the current scan.

**Service Registration**  
Services are registered in `Startup.AddRepositories`.

**File Examples**  
- `Startup.cs`

**Code Snippet**  
```358:377:Startup.cs
protected void AddRepositories(IServiceCollection services)
{
    services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    services.AddTransient<IContractService, ContractService>();
    services.AddTransient<ICrewMemberService, CrewMemberService>();
    services.AddTransient<ICrewMemberAssignService, CrewMemberAssignService>();
    services.AddTransient<ILookupService, LookupService>();
    services.AddTransient<IBoatService, BoatService>();
    services.AddTransient<IPositionGroupService, PositionGroupService>();
    services.AddTransient<IPositionService, PositionService>();
    services.AddTransient<IRotationService, RotationService>();
    services.AddTransient<IHitchService, HitchService>();
    services.AddTransient<ITimeOffService, TimeOffService>();
    services.AddTransient<ITravelService, TravelService>();
    services.AddTransient<ITimeCardService, TimeCardService>();
    services.AddTransient<IPayrollService, PayrollService>();
    services.AddTransient<IHolidayService, HolidayService>();
    services.AddTransient<IPayPeriodService, PayPeriodService>();
    services.AddTransient<ICrewCoordinatorService, CrewCoordinatorService>();
    services.AddTransient<IEmailService, EmailService>();
    services.AddControllersWithViews().AddRazorRuntimeCompilation();
    services.AddRazorPages();
}
```

---

### View Layer

**Pattern Description**  
Views bind to strongly-typed models and use Tag Helpers for form fields, actions, and validation. Data is passed back via form POSTs and bound to model properties.

**File Examples**  
- `Views/CrewMember/CrewEdit.cshtml`  
- `Views/CrewMember/_CrewMemberDetail.cshtml`

**Code Snippet**  
```12:34:Views/CrewMember/CrewEdit.cshtml
<form method="post" name="crewMemberform">
    <div class="card button-bar">
        <div class="row">
            <div class="ml-2">
                <button type="submit" asp-action="CrewMember" name="crewMember" value="save" class="btn btn-primary btn-sm"><i class='fas fa-save'></i> Save</button>
            </div>
            <div class="ml-2">
                <button type="submit" asp-action="CrewMember" name="crewMember" value="cancel" formnovalidate class="btn btn-outline-primary btn-sm"><i class='fas fa-window-close'></i> Cancel</button>
            </div>
        </div>
    </div>
    <div class="mt-2">
        <div asp-validation-summary="ModelOnly"> </div>
        <partial name="_CrewMemberDetail" model="Model" />
    </div>
</form>
```

**Variations**  
Tag Helpers are preferred over `Html.*` helpers, with `asp-for`, `asp-items`, and validation helpers used consistently.

---

## 2. DataTables Implementation

### Configuration Pattern

**Pattern Description**  
DataTables is initialized inline within a partial view and configured for server-side processing with custom filters. Client-side filters are appended to the `ajax.data` payload, while the controller uses `AppController.GetDataTable` to build the response.

**File Examples**  
- `Views/Search/_Results.cshtml`  
- `Controllers/SearchController.cs`  
- `AppClasses/AppController.cs`

**Code Snippet**  
```27:90:Views/Search/_Results.cshtml
$(document).ready(function () {
    var dataTable = $("#unitTowContracts").DataTable({
        "processing": true,
        "serverSide": true,
        "filter": true,
        "orderMulti": false,
        "stateSave" : true,
        "paging": true,
        "info": true,
        "ajax": {
            "url": "/Search/ContractTable",
            "type": "POST",
            "data": function (d) {
                d.customerName = $('#CustomerName').val();
                d.contractNumber = $('#ContractNumber').val();
                d.boatName = $('#BoatName').val();
                d.status = $('#Status').val();
            },
            "dataType": "json"
        },
        "lengthMenu": [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
        "columnDefs": [
            { "targets": [0], "visible": false, "searchable": false },
            { "className": "dt-left", "targets": [1, 2, 3, 4, 5] }
        ],
        "columns": [
            { "data": "unitTowContractId", "name": "UnitTowContractId", "autoWidth": true },
            {
                "data": null,
                "sortable": false,
                "render": function (data, type, full, meta) {
                    return '<a class="btn btn-primary btn-sm" href="/Contract/ViewDetail/?id=' + full.unitTowContractId + '">View</a>';
                }
            },
            { "data": "contractNumber", "name": "Contract Number", "autoWidth": true },
            { "data": "contractName", "name": "Contract Name", "autoWidth": true },
            { "data": "customerName", "name": "Customer Name", "autoWidth": true },
            {
                "data": "jobExists",
                "render": function (data, type, full, meta) {
                    if (data === true) {
                        return '<a  href="/job/ViewDetail/?id=' + full.unitTowJobID + '&returnUrl=/Search/Index">View Job</a>';
                    } else {
                        return '<a href="" data-toggle="modal" data-target="#createJobConfirmationDialog" data-id="' + full.unitTowContractId + '">Start Job</a>';
                    }
                }
            },
            { "data": "status", "name": "Status", "autoWidth": true }
        ]
    });
});
```

**Variations**  
No shared DataTables initializer file was found; configuration is inline per view.

### Filter Implementation

**Pattern Description**  
Filters are standard form inputs and selects in a partial. Their values are added to the DataTables AJAX payload and read in the controller via `Request.Form`.

**File Examples**  
- `Views/Search/_Search.cshtml`  
- `Controllers/SearchController.cs`

**Code Snippet**  
```26:41:Views/Search/_Search.cshtml
<div class="col-sm-6 col-md-6 col-lg-2">
    <label asp-for="ContractNumber">Contract Number</label>
    <input type="text" asp-for="ContractNumber" class="form-control" />
</div>
<div class="col-sm-6 col-md-6 col-lg-2">
    <label asp-for="CustomerName">Customer Name</label>
    <input asp-for="CustomerName" class="form-control" />
</div>
<div class="col-sm-6 col-md-6 col-lg-2">
    <label asp-for="Status">Status</label>
    <select class="form-control" asp-for="Status" asp-items=Model.ContractStatusList></select>
</div>
```

**Variations**  
No date filters or numeric range inputs were found in DataTables filter UIs.

### AJAX Endpoint Pattern

**Pattern Description**  
DataTable endpoints are POST actions that parse `Request.Form` fields and delegate paging/sorting/search to `GetDataTable`. Responses include `draw`, `recordsTotal`, `recordsFiltered`, and `data`.

**File Examples**  
- `Controllers/SearchController.cs`  
- `AppClasses/AppController.cs`

**Code Snippet**  
```51:87:AppClasses/AppController.cs
var draw = Request.Form["draw"].FirstOrDefault();
var start = Request.Form["start"].FirstOrDefault();
var length = Request.Form["length"].FirstOrDefault();
// ...
var jsonData = new
{
    draw,
    recordsFiltered = recordsTotal,
    recordsTotal,
    data
};
return Ok(jsonData);
```

**Variations**  
Sorting uses reflection on property names derived from DataTables column `data` values.

### Export Features

**Pattern Description**  
No DataTables export buttons or Excel/CSV download configurations were found. DataTables assets are present in `wwwroot/lib/datatables`, but export features are not configured in view scripts.

**Variations**  
An Excel import workflow exists for crew member data (ClosedXML), but this is unrelated to DataTables export.

---

## 3. UI Look and Feel

### Layout Structure

**Pattern Description**  
The app uses a fixed left sidebar layout with Bootstrap 4.6.0 and a full-width main content area. Navigation is grouped and permission-guarded, while the main content renders through `@RenderBody()` and page-level scripts via `@section Scripts`.

**File Examples**  
- `Views/Shared/_Layout.cshtml`  
- `libman.json`  
- `wwwroot/css/site.css`

**Code Snippet**  
```28:245:Views/Shared/_Layout.cshtml
<section class="container-fluid">
    <div class="row">
        <nav class="col-md-2 d-none d-md-block bg-light sidebar" id="sidebarNav">
            <div class="sidebar-sticky">
                <ul class="nav flex-column">
                    <!-- nav items -->
                </ul>
            </div>
        </nav>
    </div>
    <div class="container-fluid">
        <main id="mainContentSection" role="main" class="col-md-9 ml-sm-auto col-lg-10 pt-3 px-4">
            <div class="d-flex justify-content-between flex-wrap flex-md-nowrap align-items-center pb-2 mb-3 border-bottom">
                @RenderBody()
            </div>
        </main>
    </div>
</section>
```

**Variations**  
The sidebar supports a collapsed mode with localStorage state and custom class toggles.

### Grid System

**Pattern Description**  
Bootstrap grid classes are used for responsive forms (`col-sm-*`, `col-md-*`, `col-lg-*`). Forms are laid out in `form-group` rows and grouped by `card`.

**File Examples**  
- `Views/CrewMember/_CrewMemberDetail.cshtml`

**Code Snippet**  
```8:29:Views/CrewMember/_CrewMemberDetail.cshtml
<div class="form-group">
    <div class="row">
        <div class="col-sm-4 col-md-4 col-lg-4">
            <label asp-for="LastName">Last Name</label><span style="color: red">*</span>
            <input asp-for="LastName" class="form-control" readonly="@Model.IsDisabled" />
        </div>
        <div class="col-sm-4 col-md-4 col-lg-4">
            <label asp-for="FirstName">First Name</label><span style="color: red">*</span>
            <input type="text" asp-for="FirstName" class="form-control" readonly="@Model.IsDisabled" />
        </div>
        <div class="col-sm-4 col-md-4 col-lg-4">
            <label asp-for="PositionID">Position</label>
            <select asp-for="PositionID" class="form-control" disabled="@Model.IsDisabled"
                    asp-items="@(new SelectList(Model.PositionList, "PositionID", "Position"))">
                <option value="0">Please select one</option>
            </select>
        </div>
    </div>
</div>
```

### Button Patterns

| Purpose | Classes | Icon Pattern | Example |
|---------|---------|--------------|---------|
| Primary Action | `btn btn-primary` | `<i class="fas ..."></i>` | Save / Search |
| Secondary Action | `btn btn-outline-primary` / `btn btn-info` | `<i class="fas ..."></i>` | Cancel / Impersonate |
| Destructive Action | `btn btn-danger` | `<i class="fas ..."></i>` | Logout |
| Small buttons | `btn btn-sm` | `<i class="fas ..."></i>` | Row actions |
| Full-width buttons | `btn btn-block w-100` | `<i class="fas ..."></i>` | Sidebar actions |

**File Examples**  
- `Views/CrewMember/CrewEdit.cshtml`  
- `Views/Search/_Search.cshtml`  
- `Views/Shared/_Layout.cshtml`

### Form Patterns

**Pattern Description**  
Forms use card-based layout with labels above fields. Required fields use a red `*`, validation messages appear via `asp-validation-for`, and `asp-validation-summary` is typically placed near the top of the form.

**Variations**  
Readonly fields are implemented by toggling `readonly` and `disabled` based on `Model.IsDisabled`.

### Dropdown/Select Patterns

**Pattern Description**  
Standard `<select>` elements use Tag Helpers (`asp-for`, `asp-items`) and are populated from view model collections or enums.

### Modal Dialog Patterns

**Pattern Description**  
Bootstrap modals are opened using `data-toggle="modal"`/`data-target` and populated via jQuery in a `@section Scripts`.

### Navigation Patterns

**Pattern Description**  
Sidebar navigation uses `require-permission` tag helpers for visibility. Active state is applied with `Html.ActiveClass`.

**File Examples**  
- `Views/Shared/_Layout.cshtml`  
- `Extensions/MvcExtensions.cs`

### Table Styling (non-DataTables)

**Pattern Description**  
Standard Bootstrap table classes are used, with DataTables-specific styles in `site.css`.

### Icon Library

**Pattern Description**  
FontAwesome 5 is used for icons; Feather icons are loaded for optional use.

**File Examples**  
- `Views/Shared/_Layout.cshtml`  
- `libman.json`

### Color/Theme Patterns

**Pattern Description**  
Global background is light gray. Active nav uses Bootstrap primary blue. Status colors are defined in `site.css`.

**File Examples**  
- `wwwroot/css/site.css`

---

## 4. ViewModel Patterns

### ViewModel Structure

**Pattern Description**  
View models are primarily in `Models/` and often named `*Model` rather than `*ViewModel`. Models include data fields, lookup collections, UI state flags, and DataAnnotation validation attributes.

**File Examples**  
- `Models/CrewMemberDetailModel.cs`  
- `Models/SearchContractModel.cs`  
- `Models/LookupModel.cs`

**Code Snippet**  
```10:74:Models/CrewMemberDetailModel.cs
public class CrewMemberDetailModel : CrewingBaseModel<CrewMemberDetailModel>
{
    public int CrewMemberID { get; set; }
    [Required]
    [MinLength(1, ErrorMessage = "Please enter a Last Name")]
    [MaxLength(50, ErrorMessage = "Last Name cannot exceed 50 characters")]
    public string LastName { get; set; }
    // ...
    public List<CrewMemberPositionModel> PositionList { get; set; }
    public List<CrewMemberRotationModel> RotationList { get; set; }
    public List<UsStateModel> States { get; set; }
    public List<CrewMemberBoatLocationModel> Boats { get; set; }
    public ApiFetchResult ApiFetchResult { get; set; }
    public bool IsDisabled { get; set; }
    public bool IsReadOnly { get; set; }
}
```

### ViewMode Pattern

**Pattern Description**  
No explicit `ViewMode` enum is present. View mode is controlled via flags like `IsDisabled` and `IsReadOnly`.

**Variations**  
Some controllers set `ViewBag.IsEdit` for view behavior.

### Lookup/Helper Models

**Pattern Description**  
Lookups are represented as simple models or `SelectListItem` collections computed from enums.

**File Examples**  
- `Models/LookupModel.cs`  
- `Models/SearchContractModel.cs`

---

## 5. Partial Views and Layouts

### Partial View Organization

**Pattern Description**  
Partials use a leading underscore and reside in the same feature folder or `Views/Shared`. Views compose partials using `<partial name="...">`.

**File Examples**  
- `Views/Search/Index.cshtml`  
- `Views/Search/_Search.cshtml`  
- `Views/Search/_Results.cshtml`  
- `Views/Shared/_ValidationScriptsPartial.cshtml`

**Code Snippet**  
```12:34:Views/Search/Index.cshtml
<div>
    @{<partial name="_Search" model="Model" />}
</div>

<div class="mt-2">
    @{<partial name="_Results" model="Model" />}
</div>
```

### Section Pattern

**Pattern Description**  
Layout defines `@RenderSection("scripts", false)`, and pages opt in with `@section Scripts`.

**File Examples**  
- `Views/Shared/_Layout.cshtml`  
- `Views/Search/Index.cshtml`

---

## 6. Authorization Patterns

### Filter-based Authorization

**Pattern Description**  
Authorization is enforced via middleware that inspects `RequirePermission` attributes and user claims. Users without required claims are redirected to `AccessDenied`.

**File Examples**  
- `Authorization/RequireRoleMiddleware.cs`  
- `Controllers/CrewMemberController.cs`

**Code Snippet**  
```70:140:Authorization/RequireRoleMiddleware.cs
var requirePermissionAttr = controllerActionDescriptor.MethodInfo
    .GetCustomAttribute<RequirePermissionAttribute<AuthPermissions>>();
// ...
if (requirePermissionAttr != null)
{
    var requiredPermission = requirePermissionAttr.Permission.ToString();
    var userPermissionClaim = context.User.Claims
        .FirstOrDefault(c => c.Type == requiredPermission);
    if (userPermissionClaim == null)
    {
        context.Response.Redirect($"/Account/AccessDenied?reason=MissingPermission&returnUrl={returnUrl}&adminMessage={adminMessage}");
        return;
    }
}
```

### Role-based Access

**Pattern Description**  
Views use `require-permission` Tag Helpers to show/hide UI sections based on permissions and access levels.

**File Examples**  
- `Views/Shared/_Layout.cshtml`

---

## 7. Session Management

### Session Pattern

**Pattern Description**  
Session state is wrapped in `AppSession` for navigation state and `CurrentUserService` for user identity/ID. Controllers receive `AppSession` via DI.

**File Examples**  
- `AppClasses/AppSession.cs`  
- `Services/CurrentUserService.cs`  
- `Startup.cs`

**Code Snippet**  
```9:40:AppClasses/AppSession.cs
public class AppSession
{
    private readonly IHttpContextAccessor _httpAccessor;

    public string PrevUrl
    {
        get => _httpAccessor.HttpContext?.Session.GetString("PrevUrl");
        set
        {
            if (value == null)
                _httpAccessor.HttpContext?.Session.Remove("PrevUrl");
            else
                _httpAccessor.HttpContext?.Session.SetString("PrevUrl", value);
        }
    }
}
```

---

## 8. Key File Locations

| Category | Path Pattern | Purpose |
|----------|--------------|---------|
| Controllers | `Controllers/*.cs` | MVC endpoints, action methods, permission attributes |
| Services | `Services/*.cs` | API access, business logic, HTTP client usage |
| Views | `Views/**/Index.cshtml` | Main pages and feature views |
| Partials | `Views/**/_*.cshtml`, `Views/Shared/_*.cshtml` | Reusable UI blocks and shared scripts |
| ViewModels | `Models/*.cs` | DTOs, view data, validation attributes |
| JavaScript | `wwwroot/js/*.js`, inline in views | UI behavior, DataTables setup |
| CSS | `wwwroot/css/site.css`, `wwwroot/client/styles.css` | Styling, layout, DataTables tweaks |
| Startup/Config | `Startup.cs`, `appsettings*.json`, `libman.json` | DI registration, auth/session config, libraries |

