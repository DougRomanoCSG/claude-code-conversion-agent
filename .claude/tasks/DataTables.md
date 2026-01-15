# DataTables Usage Guide (BargeOps Crewing UI)

This document expands the DataTables usage patterns in the application, focusing on the client-side initialization, server-side processing, and common configuration conventions.

## Library & Asset References

DataTables is provided via LibMan and referenced per-view rather than globally in the layout.

- Library registration: `libman.json` includes `datatables@1.10.21`.
- View-level asset inclusion example:

```cshtml
<link href="~/lib/datatables/dist/datatables.css" rel="stylesheet" />
<script src="~/lib/datatables/dist/datatables.js"></script>
```

Source: `Views/Search/_Results.cshtml`.

## Known Usage Locations

### Client-side initializations

- `Views/Search/_Results.cshtml` — Contract search results table initialization.

### Server-side endpoints

The shared `AppController.GetDataTable` helper is used by several controllers to return DataTables-compatible JSON:

- `Controllers/SearchController.cs`
- `Controllers/BoatController.cs`
- `Controllers/BoatSearchController.cs`
- `Controllers/CrewCoordinatorController.cs`
- `Controllers/CrewMemberController.cs`
- `Controllers/CrewMemberAssignController.cs`
- `Controllers/CrewingSearchController.cs`
- `Controllers/HitchController.cs`
- `Controllers/HolidayController.cs`
- `Controllers/PayCodeController.cs`
- `Controllers/PayPeriodController.cs`
- `Controllers/PositionGroupSearchController.cs`
- `Controllers/PositionSearchController.cs`
- `Controllers/RotationSearchController.cs`
- `Controllers/TimeOffController.cs`

These controllers follow the same pattern: gather inputs, query the service, and delegate sorting/paging/search to `GetDataTable`.

## Initialization Pattern (Client-Side)

Initialization is typically inline in a view or partial, with `serverSide: true` and custom filters appended to the request payload.

```javascript
$(document).ready(function () {
    var dataTable = $("#unitTowContracts").DataTable({
        "processing": true,
        "serverSide": true,
        "filter": true,
        "orderMulti": false,
        "stateSave": true,
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
            }
        ]
    });
});
```

Source: `Views/Search/_Results.cshtml`.

### Common Options

- `processing`, `serverSide`, `paging`, `info` for server-driven tables.
- `stateSave` to preserve sort/page/filter state.
- `lengthMenu` with an `"All"` option (`-1`).
- `columnDefs` to hide ID columns and apply alignment classes.

## Filter Inputs & Payload Mapping

Filters are standard form controls rendered in a companion partial view. The client-side script reads these by ID and sends them in the DataTables AJAX payload.

```cshtml
<label asp-for="ContractNumber">Contract Number</label>
<input type="text" asp-for="ContractNumber" class="form-control" />

<label asp-for="CustomerName">Customer Name</label>
<input asp-for="CustomerName" class="form-control" />

<label asp-for="Status">Status</label>
<select class="form-control" asp-for="Status" asp-items=Model.ContractStatusList></select>
```

Source: `Views/Search/_Search.cshtml`.

Server-side, these are retrieved from `Request.Form` and used to build a search model:

```csharp
var customerName = Request.Form["customerName"].FirstOrDefault();
var contractNumber = Request.Form["contractNumber"].FirstOrDefault();
var status = Request.Form["status"].FirstOrDefault();

var searchContractModel = new SearchContractModel();
if (customerName != null) searchContractModel.CustomerName = customerName;
if (contractNumber != null) searchContractModel.ContractNumber = contractNumber;
if (status != null) searchContractModel.Status = status;
```

Source: `Controllers/SearchController.cs`.

## Server-Side Parsing & Response Format

`AppController.GetDataTable` centralizes parsing of DataTables parameters and response shaping. It reads `draw`, `start`, `length`, `order`, and `search` fields, then applies sorting, filtering, and paging.

```csharp
var draw = Request.Form["draw"].FirstOrDefault();
var start = Request.Form["start"].FirstOrDefault();
var length = Request.Form["length"].FirstOrDefault();
var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][data]"].FirstOrDefault();
var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
var searchValue = Request.Form["search[value]"].FirstOrDefault();

if (sortColumn != null)
{
    sortColumn = sortColumn.First().ToString().ToUpper() + sortColumn.Substring(1);
    if (sortColumnDirection == "asc")
        queryable = queryable.OrderBy(x => x.GetType().GetProperty(sortColumn).GetValue(x, null));
    else
        queryable = queryable.OrderByDescending(x => x.GetType().GetProperty(sortColumn).GetValue(x, null));
}

if (!string.IsNullOrEmpty(searchValue))
{
    queryable = queryable.Where(m => searchPropertySelector(m).Contains(searchValue, StringComparison.CurrentCultureIgnoreCase));
}

var recordsTotal = queryable.Count();
var data = (pageSize != -1 ? queryable.Skip(skip).Take(pageSize).ToList() : queryable.ToList());

var jsonData = new
{
    draw,
    recordsFiltered = recordsTotal,
    recordsTotal,
    data
};
return Ok(jsonData);
```

Source: `AppClasses/AppController.cs`.

### Search Helper

`SearchAllModelProperties<T>` builds a string concatenation of all non-indexed properties, including nested objects and lists, enabling a coarse "global" search when DataTables `search[value]` is provided.

## Column Rendering Patterns

Custom renders are used to build links or conditional actions, usually via string concatenation:

```javascript
{
    "data": null,
    "sortable": false,
    "render": function (data, type, full, meta) {
        return '<a class="btn btn-primary btn-sm" href="/Contract/ViewDetail/?id=' + full.unitTowContractId + '">View</a>';
    }
}
```

Source: `Views/Search/_Results.cshtml`.

## Controller Endpoint Pattern

Controllers typically:

1. Read filter values from `Request.Form`.
2. Call a service to fetch the full result set (or filtered set).
3. Invoke `GetDataTable` to apply pagination, sort, and search.

```csharp
var model = await _contractService.SearchContract(searchContractModel);
var contracts = model.Results.ToList();
var searchPropertySelector = SearchAllModelProperties<ContractDetailModel>();

return await GetDataTable(contracts, searchPropertySelector);
```

Source: `Controllers/SearchController.cs`.

## How to Add a New DataTable

Use this checklist to align with existing patterns in the app:

1. **Add table markup in a view or partial**
   - Create a table with a stable `id` and Bootstrap table classes.
2. **Include DataTables assets in that view**
   - Add the `datatables.css` and `datatables.js` references.
3. **Add filter inputs (optional)**
   - Use Tag Helpers (`asp-for`) and standard `form-control` classes.
4. **Initialize the DataTable in a `<script>` block**
   - Use `serverSide: true` and wire `ajax.data` to read filter values.
5. **Add a controller action**
   - `[HttpPost]` endpoint that reads `Request.Form` and calls `GetDataTable`.
6. **Call a service to fetch data**
   - Use existing service patterns and return a list for paging.

### Minimal Example

**View/Partial**

```cshtml
<link href="~/lib/datatables/dist/datatables.css" rel="stylesheet" />
<script src="~/lib/datatables/dist/datatables.js"></script>

<table id="myTable" class="table table-striped table-sm" width="100%">
    <thead>
        <tr>
            <th>Id</th>
            <th>Name</th>
        </tr>
    </thead>
    <tbody></tbody>
</table>

<script>
    $(document).ready(function () {
        $("#myTable").DataTable({
            processing: true,
            serverSide: true,
            ajax: {
                url: "/MyController/MyTable",
                type: "POST"
            },
            columns: [
                { data: "id", name: "Id" },
                { data: "name", name: "Name" }
            ]
        });
    });
</script>
```

**Controller**

```csharp
[HttpPost("MyTable")]
public async Task<IActionResult> MyTable()
{
    var data = await _myService.GetMyRows();
    var searchPropertySelector = SearchAllModelProperties<MyRowModel>();
    return await GetDataTable(data, searchPropertySelector);
}
```

## Export Features

No DataTables Buttons export configuration was found in the current views or scripts. The DataTables Buttons assets exist in `wwwroot/lib/datatables`, but there are no references to `buttons`, `excel`, or `csv` in the view scripts.

## Notable Variations & Considerations

- Sorting uses reflection based on the column `data` field, with a string capitalization step before property lookup.
- Global searching uses `SearchAllModelProperties` to compose a search string across fields (including nested models and lists).
- For "All" page size, `length = -1` returns the full list without paging.
- Error handling returns `BadRequest` with a simple error message in controller actions and in `GetDataTable`.

