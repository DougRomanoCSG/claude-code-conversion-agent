# BargeOps Crewing UI - ASP.NET Core MVC Application Architecture Documentation

## 1. CONTROLLER → SERVICE → VIEW WORKFLOW

### Controller Layer

#### Base Controller: `AppController`
**Pattern Description**: All controllers inherit from [AppClasses/AppController.cs](AppClasses/AppController.cs), which provides common functionality for session management, DataTables processing, and user context. It's a centralized location for shared controller logic across the application.

**File Examples**:
- [AppClasses/AppController.cs](AppClasses/AppController.cs) - Base controller
- [Controllers/BoatController.cs](Controllers/BoatController.cs) - Example implementation
- [Controllers/BoatSearchController.cs](Controllers/BoatSearchController.cs) - Example implementation

**Code Snippet**:
```csharp
public class BoatController : AppController
{
    private readonly IBoatService _boatService;
    private readonly AppSession _session;
    private readonly ILookupService _lookupService;

    public BoatController(IBoatService boatService, AppSession session, ILookupService lookupService)
        : base(session)
    {
        _boatService = boatService;
        _session = session;
        _lookupService = lookupService;
    }
}
```

#### Dependency Injection Pattern
**Pattern Description**: Services are injected through constructor injection. The typical services include domain-specific services (e.g., `IBoatService`), `AppSession` for session management, and `ILookupService` for dropdown data.

**Service Registration** ([Startup.cs:358-380](Startup.cs#L358-L380)):
```csharp
protected void AddRepositories(IServiceCollection services)
{
    services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    services.AddTransient<IContractService, ContractService>();
    services.AddTransient<ICrewMemberService, CrewMemberService>();
    services.AddTransient<IBoatService, BoatService>();
    services.AddTransient<ILookupService, LookupService>();
    // ... more service registrations
}
```

#### Action Method Patterns

**GET Actions for Detail Views** ([Controllers/BoatController.cs:35-48](Controllers/BoatController.cs#L35-L48)):
```csharp
[RequirePermission<AuthPermissions>(AuthPermissions.BoatManagement, PermissionAccessType.ReadOnly)]
[HttpGet("BoatViewDetail")]
public async Task<ActionResult> BoatViewDetail(int id, string returnUrl = null)
{
    await InitSessionVariables(_session);
    _session.PrevUrl = returnUrl;

    var data = await _boatService.GetBoatByLocationId(id);
    data.IsDisabled = true;
    data.IsViewDetail = true;
    data.RotationList = (await _lookupService.GetRotationList()).Where(a => a.RotationIsActive).ToList();
    data.AssignedBoatCrew = await _boatService.GetBoatCrewListById(id, true);
    data.CrewCoordinators = await _lookupService.GetCrewCoordinatorList();
    return View(data);
}
```

**POST Actions for Saving Data** ([Controllers/BoatController.cs:83-118](Controllers/BoatController.cs#L83-L118)):
```csharp
[RequirePermission<AuthPermissions>(AuthPermissions.BoatManagement, PermissionAccessType.Modify)]
[HttpPost("BoatRotationCreate")]
public async Task<ActionResult> BoatRotationCreate(BoatRotationCreateModel model, string action)
{
    if (action.Equals("cancel"))
        return RedirectToAction("BoatViewDetail", "Boat", new { id = model.BoatLocationID });

    if (model.RotationID == default)
        ModelState.AddModelError("RotationID", "The Boat Rotation is required.");

    if (!ModelState.IsValid)
    {
        model.PositionGroups = await _lookupService.GetPositionGroupList();
        model.RotationList = (await _lookupService.GetRotationList()).Where(a => a.RotationIsActive).ToList();
        return View("BoatRotationCreate", model);
    }

    var result = await _boatService.SaveBoatRotation(model);
    return RedirectToAction("BoatViewDetail", "Boat", new { id = result.viewId });
}
```

**AJAX Endpoints for DataTables** ([Controllers/BoatSearchController.cs:43-70](Controllers/BoatSearchController.cs#L43-L70)):
```csharp
[RequirePermission<AuthPermissions>(AuthPermissions.BoatManagement, PermissionAccessType.ReadOnly)]
[HttpPost("BoatTable")]
public async Task<IActionResult> BoatTable()
{
    try
    {
        var boatName = Request.Form["BoatName"].FirstOrDefault();
        var isActive = Request.Form["Active"];

        var boatSearchModel = new BoatSearchModel();
        if (!string.IsNullOrEmpty(boatName))
            boatSearchModel.BoatName = boatName.Replace("\t", "");
        if (!string.IsNullOrEmpty(isActive))
            boatSearchModel.Active = Convert.ToBoolean(isActive);

        var queryable = await _boatService.SearchBoat(boatSearchModel);
        var searchPropertySelector = SearchAllModelProperties<BoatSearchModel>();

        return await GetDataTable(queryable, searchPropertySelector);
    }
    catch (Exception ex)
    {
        return BadRequest($"Error Getting Boat Search : {ex.Message}");
    }
}
```

#### Error Handling
**Pattern**: Controllers use try-catch blocks for error handling and return appropriate HTTP status codes. ModelState validation is checked before processing POST actions. Errors are returned as `BadRequest()` with descriptive messages.

### Service Layer

#### Base Service: `CrewingBaseService`
**Pattern Description**: All services inherit from [Services/CrewingBaseService.cs](Services/CrewingBaseService.cs), which provides HTTP client configuration, authentication, and common API interaction patterns.

**File Example**: [Services/CrewingBaseService.cs](Services/CrewingBaseService.cs)

**Code Snippet** ([Services/CrewingBaseService.cs:12-27](Services/CrewingBaseService.cs#L12-L27)):
```csharp
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

    protected string UserName => _currentUserService.UserName;
}
```

#### HTTP Client Configuration ([Services/CrewingBaseService.cs:47-63](Services/CrewingBaseService.cs#L47-L63)):
```csharp
public HttpClient GetClient()
{
    var client = new HttpClient {
        BaseAddress = new Uri(_baseAddress),
        Timeout = TimeSpan.FromSeconds(30)
    };

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
```

#### Service Method Patterns

**GET Operations - Single Record** ([Services/BoatService.cs:69-84](Services/BoatService.cs#L69-L84)):
```csharp
public async Task<BoatDetailModel> GetBoatByLocationId(int id)
{
    try
    {
        var response = await GetClient().GetAsync(_baseUrl + id);
        response.EnsureSuccessStatusCode();
        var stringResult = await response.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<BoatDetailModel>(stringResult);
        return data;
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
}
```

**Search/Filter Operations with ListRequest** ([Services/BoatService.cs:27-68](Services/BoatService.cs#L27-L68)):
```csharp
public async Task<List<BoatSearchModel>> SearchBoat(BoatSearchModel BoatSearchModel)
{
    try
    {
        var listRequest = GetListRequest<BoatSearchModel>();

        if (!string.IsNullOrEmpty(BoatSearchModel.BoatName))
            listRequest.Filters.Add(new ListFilter
            {
                Name = "BoatName",
                Operator = ListFilterOperator.Like,
                Value = BoatSearchModel.BoatName
            });

        if (BoatSearchModel.Active != null)
            listRequest.Filters.Add(new ListFilter
            {
                Name = "Active",
                Operator = ListFilterOperator.Equal,
                Value = BoatSearchModel.Active
            });

        listRequest.Order.Add(new SortField
        {
            Name = "BoatName",
            SortDescending = false
        });

        var response = await GetClient().PostAsJsonAsync(_baseUrl + "filter", listRequest);
        response.EnsureSuccessStatusCode();
        var stringResult = await response.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<RootObject<BoatSearchModel>>(stringResult).Data;

        return data.ToList();
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
}
```

### View Layer

#### View-to-Controller Binding
**Pattern**: Views use strongly-typed models with `@model` directive. Form submissions use `asp-action` tag helpers for routing. Model binding occurs automatically for POST actions.

**Example** ([Views/Boat/BoatEdit.cshtml:1-12](Views/Boat/BoatEdit.cshtml#L1-L12)):
```html
@model BoatDetailModel

<form asp-action="Boat" method="post" name="boatform">
    <button type="submit" name="boat" value="save" class="btn btn-primary btn-sm">
        <i class='fas fa-window-close'></i> Save
    </button>
    <button type="submit" name="boat" value="cancel" class="btn btn-outline-primary btn-sm">
        <i class='fas fa-window-close'></i> Cancel
    </button>
</form>
```

#### Tag Helpers vs Html Helpers
**Pattern**: The application primarily uses **Tag Helpers** (`asp-for`, `asp-action`, `asp-controller`, `asp-items`) rather than Html Helpers. This provides cleaner, more readable syntax.

---

## 2. DATATABLES IMPLEMENTATION

### Configuration Pattern

**Pattern Description**: DataTables are initialized with server-side processing enabled. Configuration is done inline in view scripts with AJAX endpoints pointing to controller actions. Common options include pagination, sorting, searching, and custom column rendering.

**File Example**: [Views/BoatSearch/_BoatSearchResults.cshtml](Views/BoatSearch/_BoatSearchResults.cshtml)

**Code Snippet** ([Views/BoatSearch/_BoatSearchResults.cshtml:34-52](Views/BoatSearch/_BoatSearchResults.cshtml#L34-L52)):
```javascript
$(document).ready(function () {
    var dataTable = $("#boats").DataTable({
        "processing": true,      // Show progress bar
        "serverSide": true,      // Server-side processing
        "filter": true,          // Enable filter (search box)
        "orderMulti": false,     // Disable multiple column sorting
        "stateSave": true,       // Save table state
        "paging": true,
        "info": true,
        "ajax": {
            "url": "/BoatSearch/BoatTable",
            "type": "POST",
            "data": function (d) {
                d.BoatName = $('#BoatName').val();
                d.Active = $('#Active').val();
            },
            "dataType": "json"
        },
        "lengthMenu": [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
        // ... column definitions
    });
});
```

### Column Configuration

**Column Types** ([Views/BoatSearch/_BoatSearchResults.cshtml:53-126](Views/BoatSearch/_BoatSearchResults.cshtml#L53-L126)):

```javascript
"columnDefs": [
    {
        "targets": [0],         // Hidden column (ID)
        "visible": false,
        "searchable": false
    },
    { "className": "dt-left", "targets": [2, 3, 4, 5, 6, 7, 8, 9, 10, 11] }
],
"columns": [
    // Hidden ID column
    { "data": "boatLocationID", "autoWidth": true },

    // Button render function
    {
        "data": null,
        "sortable": false,
        "render": function (data, type, full, meta) {
            return '<a class="btn btn-outline-primary btn-xs" href="/Boat/BoatViewDetail/?id=' + full.boatLocationID + '">View</a>';
        }
    },

    // Simple text column
    { "data": "boatName", "autoWidth": true },

    // Custom render for formatting
    {
        "data": "phone",
        "render": function (data, type, row) {
            if(data){
                return mainController.formatPhoneNumber(data);
            }
            return null;
        },
        "className": "dt-left"
    },

    // Checkbox column
    {
        "data": "active",
        "render": function (data, type, row) {
            if (data === true) {
                return '<input disabled type="checkbox" checked>';
            } else {
                return '<input disabled type="checkbox">';
            }
        },
        "className": "dt-body-center"
    }
]
```

### Filter Implementation

**Pattern**: Filters are passed through the AJAX `data` function and extracted in the controller from `Request.Form`. The controller then passes filter values to the service layer.

**Client-Side** ([Views/BoatSearch/_BoatSearchResults.cshtml:43-50](Views/BoatSearch/_BoatSearchResults.cshtml#L43-L50)):
```javascript
"ajax": {
    "url": "/BoatSearch/BoatTable",
    "type": "POST",
    "data": function (d) {
        d.BoatName = $('#BoatName').val();
        d.Active = $('#Active').val();
    },
    "dataType": "json"
}
```

**Server-Side** ([Controllers/BoatSearchController.cs:49-58](Controllers/BoatSearchController.cs#L49-L58)):
```csharp
var boatName = Request.Form["BoatName"].FirstOrDefault();
var isActive = Request.Form["Active"];

var boatSearchModel = new BoatSearchModel();
if (!string.IsNullOrEmpty(boatName))
    boatSearchModel.BoatName = boatName.Replace("\t", "");
if (!string.IsNullOrEmpty(isActive))
    boatSearchModel.Active = Convert.ToBoolean(isActive);
```

### AJAX Endpoint Pattern

**Request/Response Format** ([AppClasses/AppController.cs:47-95](AppClasses/AppController.cs#L47-L95)):
```csharp
protected async Task<IActionResult> GetDataTable<TEntity>(
    IEnumerable<TEntity> queryable,
    Func<TEntity, string> searchPropertySelector)
{
    try
    {
        var draw = Request.Form["draw"].FirstOrDefault();
        var start = Request.Form["start"].FirstOrDefault();
        var length = Request.Form["length"].FirstOrDefault();
        var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][data]"].FirstOrDefault();
        var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
        var searchValue = Request.Form["search[value]"].FirstOrDefault();
        var pageSize = length != null ? Convert.ToInt32(length) : 0;
        var skip = start != null ? Convert.ToInt32(start) : 0;

        // Apply sorting
        if (sortColumn != null)
        {
            sortColumn = sortColumn.First().ToString().ToUpper() + sortColumn.Substring(1);
            if (sortColumnDirection == "asc")
                queryable = queryable.OrderBy(x => x.GetType().GetProperty(sortColumn).GetValue(x, null));
            else
                queryable = queryable.OrderByDescending(x => x.GetType().GetProperty(sortColumn).GetValue(x, null));
        }

        // Apply search filter
        if (!string.IsNullOrEmpty(searchValue))
            queryable = queryable.Where(m => searchPropertySelector(m).Contains(searchValue, StringComparison.CurrentCultureIgnoreCase));

        var recordsTotal = queryable.Count();
        var data = (pageSize != -1 ? queryable.Skip(skip).Take(pageSize).ToList() : queryable.ToList());

        // Return DataTables expected format
        var jsonData = new { draw, recordsFiltered = recordsTotal, recordsTotal, data };
        return Ok(jsonData);
    }
    catch (Exception ex)
    {
        return BadRequest($"Error retrieving data for DataTables: {ex.Message}");
    }
}
```

### Search Button Handlers ([Views/BoatSearch/_BoatSearchResults.cshtml:136-146](Views/BoatSearch/_BoatSearchResults.cshtml#L136-L146)):
```javascript
$('#btnSearch').click(function() {
    dataTable.draw();
})

$('#btnClear').click(function () {
    dataTable.search('');
    $('#BoatName').val('');
    $('#Active').val('');
    dataTable.draw();
})
```

---

## 3. UI LOOK AND FEEL

### Layout Structure

**Master Layout**: [Views/Shared/_Layout.cshtml](Views/Shared/_Layout.cshtml)

**Pattern Description**: The application uses a fixed left sidebar navigation with collapsible functionality. The main content area is fluid and responsive. The layout uses Bootstrap 4+ with custom CSS overrides.

**Structure** ([Views/Shared/_Layout.cshtml:28-248](Views/Shared/_Layout.cshtml#L28-L248)):
```html
<body>
    <section class="container-fluid">
        <div class="row">
            <!-- Fixed Sidebar -->
            <nav class="col-md-2 d-none d-md-block bg-light sidebar" id="sidebarNav">
                <div class="sidebar-sticky">
                    <table style="height:100%;">
                        <tr>
                            <td valign="top">
                                <ul class="nav flex-column">
                                    <li class="nav-item">
                                        <img alt="Barge Ops" src="~/img/BargeOps-Crews-257x74.png">
                                    </li>
                                    <!-- Navigation items with permission checks -->
                                    <require-permission permission="BoatManagement" access-level="ReadOnly">
                                        <li class="nav-item px-3">
                                            <a class="nav-link" asp-controller="Boat" asp-action="LiveCrewBoard">
                                                Crew Board
                                            </a>
                                        </li>
                                    </require-permission>
                                </ul>
                            </td>
                        </tr>
                        <tr>
                            <td valign="bottom">
                                <!-- Logout and footer -->
                            </td>
                        </tr>
                    </table>
                </div>
            </nav>
        </div>

        <!-- Main Content Area -->
        <div class="container-fluid">
            <main id="mainContentSection" role="main" class="col-md-9 ml-sm-auto col-lg-10 pt-3 px-4">
                <div class="d-flex justify-content-between flex-wrap flex-md-nowrap align-items-center pb-2 mb-3 border-bottom">
                    @RenderBody()
                </div>
            </main>
        </div>
    </section>
</body>
```

### Grid System
**CSS Framework**: Bootstrap 4+ (included via `~/lib/bootstrap/dist/css/bootstrap.min.css`)

**Responsive Classes**: Standard Bootstrap grid with `col-sm-*`, `col-md-*`, `col-lg-*` breakpoints

### Button Patterns

| Purpose | Classes | Icon Pattern | Example |
|---------|---------|--------------|---------|
| Primary Action | `btn btn-primary btn-sm` | FontAwesome icon | `<button class="btn btn-primary btn-sm"><i class='fas fa-save'></i> Save</button>` |
| Secondary Action | `btn btn-outline-primary btn-sm` | FontAwesome icon | `<button class="btn btn-outline-primary btn-sm"><i class='fas fa-window-close'></i> Cancel</button>` |
| Small Table Action | `btn btn-outline-primary btn-xs` | No icon typically | `<a class="btn btn-outline-primary btn-xs" href="...">View</a>` |
| Danger/Delete | `btn btn-danger` | FontAwesome icon | `<button class="btn btn-danger"><i class="fas fa-trash"></i> Delete</button>` |
| Warning | `btn btn-warning` | FontAwesome icon | Used for impersonation stop button |
| Info | `btn btn-info` | FontAwesome icon | Used for impersonation start button |

### Form Patterns

**Form Layout**: Grid-based with Bootstrap rows and columns ([Views/Boat/_BoatLocation.cshtml:11-28](Views/Boat/_BoatLocation.cshtml#L11-L28)):
```html
<div class="form-group">
    <div class="row">
        <div class="col-sm-4 col-md-4 col-lg-4">
            <label asp-for="BoatName">Boat Name</label>
            <input asp-for="BoatName" class="form-control" readonly="@Model.IsDisabled" />
            <span asp-validation-for="BoatName" class="text-danger"></span>
        </div>
        <div class="col-sm-4 col-md-4 col-lg-4">
            <label asp-for="Uscg">USCG</label>
            <input type="text" asp-for="Uscg" class="form-control" readonly="@Model.IsDisabled" />
            <span asp-validation-for="Uscg" class="text-danger"></span>
        </div>
    </div>
</div>
```

**Label Placement**: Above field
**Required Field Indicators**: `<span style="color: red"></span>` next to labels (though often empty)
**Validation Display**: Below input with `<span asp-validation-for="..." class="text-danger"></span>`
**Input Styling**: Standard Bootstrap `form-control` class
**Readonly/Disabled States**: Controlled via model properties like `readonly="@Model.IsDisabled"`

### Dropdown/Select Patterns

**Standard Dropdowns** ([Views/Boat/_BoatLocation.cshtml:72-84](Views/Boat/_BoatLocation.cshtml#L72-L84)):
```html
<label asp-for="CrewCoordinatorID">Crew Coordinator</label>
@if (Model.IsViewDetail)
{
    <input type="text" class="form-control"
           value="@Model.CrewCoordinators.FirstOrDefault(c => c.CrewCoordinatorID == Model.CrewCoordinatorID)?.FullName"
           readonly="readonly" />
}
else
{
    <select asp-for="CrewCoordinatorID" class="form-control"
            asp-items="@(new SelectList(Model.CrewCoordinators, "CrewCoordinatorID", "FullName"))">
        <option value="">Select a Coordinator</option>
    </select>
}
```

**Enhanced Dropdowns**: Select2 is available (`~/lib/select2/js/select2.min.js`) for enhanced dropdown functionality

### Modal Dialog Patterns
**Structure**: Standard Bootstrap modals (though no specific examples in reviewed files, Bootstrap modal markup would be used)

### Navigation Patterns

**Sidebar Navigation** ([Views/Shared/_Layout.cshtml:89-180](Views/Shared/_Layout.cshtml#L89-L180)):
- **Structure**: Grouped by functional area (Crew Scheduling, Payroll, Admin)
- **Active State**: Using `@Html.ActiveClass("Controller", "Action")` helper
- **Grouping**: Section headers with `class="font-weight-bold nav-group"`
- **Permission-Based**: Wrapped in `<require-permission>` tag helper

```html
<li class="font-weight-bold nav-group d-flex justify-content-between align-items-center px-3 mt-2 mb-1">
    Crew Scheduling
</li>
<require-permission permission="BoatManagement" access-level="ReadOnly">
    <li class="nav-item px-3">
        <a class="nav-link @Html.ActiveClass("Boat", "BoatMannings")"
           asp-controller="Boat" asp-action="LiveCrewBoard">
            Crew Board
        </a>
    </li>
</require-permission>
```

### Table Styling

**DataTables Classes**: `table table-striped table-sm dt-responsive`

**Standard Table Classes**:
- `table` - Base table class
- `table-striped` - Alternating row colors
- `table-sm` - Compact spacing
- `dt-responsive` - Responsive behavior for DataTables

### Icon Library
**Library**: FontAwesome 5+ (`~/lib/fortawesome/fontawesome-free/js/fontawesome.js`)

**Common Icon Usage**:
- `fas fa-save` - Save actions
- `fas fa-window-close` - Cancel actions
- `fas fa-user` - User display
- `fas fa-user-secret` - Impersonation
- `fas fa-sign-out-alt` - Logout
- `fas fa-cog fa-spin` - Loading indicator

### Color/Theme Patterns

**Primary Colors** (from [wwwroot/css/site.css](wwwroot/css/site.css)):
- Background: `#e5e5e5` (light gray)
- Sidebar: `bg-light` (Bootstrap light)
- Primary action: `#007bff` (Bootstrap primary blue)
- Text: `#333` (dark gray)

**Status Indicators**:
- Danger/Error: Bootstrap `btn-danger`, `text-danger`
- Warning: Bootstrap `btn-warning` (used for impersonation)
- Info: Bootstrap `btn-info`

---

## 4. VIEWMODEL PATTERNS

### ViewModel Structure

**Naming Convention**: `{Entity}{Purpose}Model` (e.g., `BoatDetailModel`, `BoatSearchModel`, `BoatRotationCreateModel`)

**Location**: [Models/](Models/) folder

### ViewModel Contents

**Example**: [Models/BoatDetailModel.cs](Models/BoatDetailModel.cs)

**Pattern Description**: ViewModels inherit from `CrewingBaseModel<T>` and contain domain properties, lookup collections for dropdowns, UI state flags, and validation attributes.

```csharp
public class BoatDetailModel : CrewingBaseModel<BoatDetailModel>
{
    // Domain Data Properties
    public int BoatLocationID { get; set; }
    public string BoatName { get; set; }
    public string Uscg { get; set; }

    // Validation Attributes
    [RegularExpression(@"^(\b([01]?[0-9][0-9]?|2[0-4][0-9]|25[0-5])\b)",
        ErrorMessage = "Value must be 0-255")]
    public int Bunks { get; set; }

    [RegularExpression(@"^\(?(\d{3})\)?[-.\s]?(\d{3})[-.\s]?(\d{4})$",
        ErrorMessage = "Invalid Phone Number.")]
    public string Phone { get; set; }

    // UI State Properties
    public bool IsDisabled { get; set; }
    public bool IsViewDetail { get; set; }

    // Lookup Properties (for dropdowns)
    public List<CrewMemberRotationModel> RotationList { get; set; }
    public List<BoatCrewListModel> AssignedBoatCrew { get; set; }
    public List<CrewCoordinatorModel> CrewCoordinators { get; set; } = new List<CrewCoordinatorModel>();

    // Required for service layer ListRequest
    public override IEnumerable<string> GetFields()
    {
        var fields = new List<string>
        {
            "BoatLocationID",
            "BoatName",
            "Uscg",
            // ... all data properties
        };
        return fields;
    }
}
```

### ViewMode Pattern
**Pattern**: UI state is controlled through boolean flags like `IsDisabled`, `IsViewDetail` rather than a ViewMode enum. These flags control readonly states and visibility of elements.

**Usage** ([Views/Boat/_BoatLocation.cshtml:15](Views/Boat/_BoatLocation.cshtml#L15)):
```html
<input asp-for="BoatName" class="form-control" readonly="@Model.IsDisabled" />
```

### Base ViewModel Pattern

**Base Class**: [Models/CrewingBaseModel.cs](Models/CrewingBaseModel.cs)
```csharp
public abstract class CrewingBaseModel<T> where T : CrewingBaseModel<T>
{
    public abstract IEnumerable<string> GetFields();
}
```

**Purpose**: The `GetFields()` method is used by the service layer to construct API requests with specific field selections.

### Lookup/Helper Models

**Pattern**: Standard lookup model with ID and display text:
```csharp
public class LookupModel
{
    public int ID { get; set; }
    public string Text { get; set; }
}
```

**Usage in ViewModels**:
```csharp
public List<CrewCoordinatorModel> CrewCoordinators { get; set; } = new List<CrewCoordinatorModel>();
```

**Usage in Views**:
```html
<select asp-for="CrewCoordinatorID" class="form-control"
        asp-items="@(new SelectList(Model.CrewCoordinators, "CrewCoordinatorID", "FullName"))">
    <option value="">Select a Coordinator</option>
</select>
```

---

## 5. PARTIAL VIEWS AND LAYOUTS

### Partial View Organization

**Naming Convention**: Prefix with underscore `_PartialName.cshtml`

**Location**:
- **Same folder as main view** for feature-specific partials (e.g., `Views/Boat/_BoatLocation.cshtml`)
- **Views/Shared/** for reusable partials (e.g., `Views/Shared/_ValidationScriptsPartial.cshtml`)

### Main View Composition

**Pattern**: Main views act as containers that compose multiple partials

**Example** ([Views/Boat/BoatEdit.cshtml:28-44](Views/Boat/BoatEdit.cshtml#L28-L44)):
```html
@model BoatDetailModel

<div class="container-fluid">
    <div class="col-md-24">
        <form asp-action="Boat" method="post">
            <div class="mt-2">
                <div class="form-group">
                    @{
                        <partial name="_BoatLocation" model="Model" />
                    }
                </div>
            </div>

            <div class="mt-2">
                <div class="form-group">
                    @{
                        <partial name="_BoatCrewing" model="Model" />
                    }
                </div>
            </div>
        </form>
    </div>
</div>
```

**Data Passing**: Partials receive the same model or a subset using `model="Model"` parameter.

### Shared Partial Views

**Common Partials**:
1. **Validation Scripts** ([Views/Shared/_ValidationScriptsPartial.cshtml](Views/Shared/_ValidationScriptsPartial.cshtml))
   ```html
   <script src="~/lib/jquery-validation/dist/jquery.validate.min.js"></script>
   <script src="~/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js"></script>
   ```

2. **Search Results Partials** (e.g., `_BoatSearchResults.cshtml`, `_CrewSearchResults.cshtml`)
   - Contains DataTables initialization and configuration

3. **Search Filter Partials** (e.g., `_BoatSearch.cshtml`, `_CrewSearch.cshtml`)
   - Contains filter input forms

4. **Detail Display Partials** (e.g., `_BoatLocation.cshtml`, `_CrewMemberDetail.cshtml`)
   - Contains readonly or editable detail forms

### Section Pattern

**Usage** ([Views/Shared/_Layout.cshtml:23](Views/Shared/_Layout.cshtml#L23)):
```html
<head>
    @RenderSection("head", false)
</head>

<body>
    @RenderBody()

    @RenderSection("scripts", false)
</body>
```

**In Views**:
```html
@section scripts {
    <script src="~/js/custom-page-script.js"></script>
}
```

### Partial View for DataTables

**Pattern**: Search results partials contain the table markup and JavaScript initialization

**Example Structure** ([Views/BoatSearch/BoatIndex.cshtml:13-20](Views/BoatSearch/BoatIndex.cshtml#L13-L20)):
```html
<div class="container-fluid">
    <div>
        @{ <partial name="_BoatSearch" model="Model" /> }
    </div>
    <div class="mt-2">
        @{ <partial name="_BoatSearchResults" model="Model" /> }
    </div>
</div>
```

---

## 6. AUTHORIZATION PATTERNS

### Filter-based Authorization

**Pattern**: CSG Authorization library with `RequirePermission` attribute on controller actions

**Permission Check** ([Controllers/BoatController.cs:34-35](Controllers/BoatController.cs#L34-L35)):
```csharp
[RequirePermission<AuthPermissions>(AuthPermissions.BoatManagement, PermissionAccessType.ReadOnly)]
[HttpGet("BoatViewDetail")]
public async Task<ActionResult> BoatViewDetail(int id, string returnUrl = null)
```

**Access Levels**:
- `PermissionAccessType.ReadOnly` - View only
- `PermissionAccessType.Modify` - Create/Edit
- `PermissionAccessType.FullControl` - Delete/Admin

### Role-based Access

**Configuration** ([Startup.cs:286-295](Startup.cs#L286-L295)):
```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy(LocalConstants.UserImpersonate, policy =>
        policy.Requirements.Add(new UserImpersonateRequirement()));

    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

### UI Element Visibility

**Tag Helper Authorization** ([Views/Shared/_Layout.cshtml:93-98](Views/Shared/_Layout.cshtml#L93-L98)):
```html
<require-permission permission="BoatManagement" access-level="ReadOnly">
    <li class="nav-item px-3">
        <a class="nav-link" asp-controller="Boat" asp-action="LiveCrewBoard">
            Crew Board
        </a>
    </li>
</require-permission>
```

### Claims Loading

**Pattern**: Claims are loaded during OpenID Connect authentication and cached in memory ([Startup.cs:142-282](Startup.cs#L142-L282)):

```csharp
OnTokenValidated = async ctx =>
{
    var userManager = ctx.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
    var memoryCache = ctx.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
    var user = await userManager.FindByEmailAsync(emailAddress);

    if (user != null)
    {
        var cacheKey = $"user_claims_{user.NormalizedUserName}_{clientId}";

        if (!memoryCache.TryGetValue<List<Claim>>(cacheKey, out var cachedClaims))
        {
            var roles = await authFacade.GetRolesByClientIdAsync(user.UserName, clientId);
            var userClaims = await authFacade.GetAllClaimsForUserAsync(user.UserName, clientId);

            cachedClaims = new List<Claim>();
            cachedClaims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            cachedClaims.AddRange(userClaims.Select(userClaim => new Claim(userClaim.Type, userClaim.Value)));

            var cacheOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(30),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
            };
            memoryCache.Set(cacheKey, cachedClaims, cacheOptions);
        }

        claims.AddRange(cachedClaims);
    }
}
```

---

## 7. SESSION MANAGEMENT

### Session Pattern

**Custom Session Class**: [AppClasses/AppSession.cs](AppClasses/AppSession.cs)

```csharp
public class AppSession
{
    private readonly IHttpContextAccessor _httpAccessor;

    public AppSession(IHttpContextAccessor httpAccessor)
    {
        _httpAccessor = httpAccessor ?? throw new ArgumentNullException(nameof(httpAccessor));
    }

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

    public string CurUrl
    {
        get => _httpAccessor.HttpContext?.Session.GetString("CurUrl");
        set
        {
            if (value == null)
                _httpAccessor.HttpContext?.Session.Remove("CurUrl");
            else
                _httpAccessor.HttpContext?.Session.SetString("CurUrl", value);
        }
    }

    public void ClearRoles()
    {
        _httpAccessor.HttpContext?.Session.Remove("Roles");
    }
}
```

**Session Configuration** ([Startup.cs:50-56](Startup.cs#L50-L56)):
```csharp
services.AddDistributedMemoryCache();
services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
```

**Session Registration** ([Startup.cs:299](Startup.cs#L299)):
```csharp
services.AddScoped<AppSession>();
```

**Session Access in Controllers**:
```csharp
public BoatController(IBoatService boatService, AppSession session, ILookupService lookupService)
    : base(session)
{
    _session = session;
}

public async Task<ActionResult> BoatViewDetail(int id, string returnUrl = null)
{
    await InitSessionVariables(_session);
    _session.PrevUrl = returnUrl;
    // ...
}
```

---

## 8. KEY FILE LOCATIONS

| Category | Path Pattern | Purpose |
|----------|--------------|---------|
| **Controllers** | `Controllers/*Controller.cs` | MVC controllers handling HTTP requests |
| **Base Controller** | `AppClasses/AppController.cs` | Base controller with shared functionality |
| **Services** | `Services/*Service.cs` | Service layer for API communication |
| **Base Service** | `Services/CrewingBaseService.cs` | Base service with HTTP client setup |
| **Interface Definitions** | `Interfaces/I*Service.cs` | Service interfaces |
| **Views - Main** | `Views/{Controller}/{Action}.cshtml` | Main view pages |
| **Views - Partials** | `Views/{Controller}/_{Partial}.cshtml` | Feature-specific partial views |
| **Views - Shared** | `Views/Shared/_*.cshtml` | Reusable shared partials |
| **Layout** | `Views/Shared/_Layout.cshtml` | Master layout template |
| **View Imports** | `Views/_ViewImports.cshtml` | Global view imports and tag helpers |
| **View Start** | `Views/_ViewStart.cshtml` | Sets default layout for all views |
| **ViewModels** | `Models/*Model.cs` | Data transfer objects for views |
| **Base ViewModel** | `Models/CrewingBaseModel.cs` | Abstract base for all models |
| **JavaScript - Site** | `wwwroot/js/site.js` | Global JavaScript functions |
| **JavaScript - Index** | `wwwroot/js/index.js` | Additional JavaScript utilities |
| **CSS - Site** | `wwwroot/css/site.css` | Custom site styles |
| **CSS - Bootstrap** | `wwwroot/lib/bootstrap/dist/css/bootstrap.min.css` | Bootstrap framework |
| **Libraries** | `wwwroot/lib/*` | Third-party libraries (jQuery, Select2, DataTables, etc.) |
| **Startup/Config** | `Startup.cs` | Application configuration and service registration |
| **App Settings** | `appsettings.json`, `appsettings.Development.json` | Configuration settings |
| **Session Management** | `AppClasses/AppSession.cs` | Custom session wrapper |
| **Authorization** | `AppClasses/*`, `Authorization/*` | Custom authorization logic |
| **Enums** | `Enums/*.cs` | Application enumerations |
| **Extensions** | `Extensions/*.cs` | Extension methods |

---

## SUMMARY OF KEY PATTERNS

### Architecture Flow
1. **Request** → Controller Action (with permission check)
2. **Controller** → Calls Service Layer with DTOs
3. **Service** → Makes HTTP API call with authentication
4. **Service** → Returns deserialized data
5. **Controller** → Populates ViewModel with data + lookups
6. **Controller** → Returns View with ViewModel
7. **View** → Renders with Razor syntax, partials, and client-side JavaScript

### Common Development Patterns
- **Inheritance**: Controllers inherit from `AppController`, Services from `CrewingBaseService`, Models from `CrewingBaseModel<T>`
- **Dependency Injection**: Constructor injection for all services
- **Authorization**: Attribute-based with CSG `[RequirePermission]` and tag helper `<require-permission>`
- **DataTables**: Server-side processing with AJAX endpoints returning standard format
- **Forms**: Bootstrap grid-based with tag helpers for binding
- **Validation**: Data annotations on models, client-side jQuery validation, server-side ModelState
- **Session**: Custom `AppSession` wrapper around HttpContext.Session
- **Partials**: Underscore prefix, composed in main views
- **Error Handling**: Try-catch in services and controllers, BadRequest for errors

This architecture provides a clean separation of concerns with clear patterns that can be replicated across new features.
