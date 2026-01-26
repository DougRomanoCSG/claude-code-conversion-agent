# Template Generation Fixes and Best Practices (CORRECTED)

## Overview
This document captures critical fixes identified during Vendor entity generation (January 2026) and corrects them to match actual BargeOps application patterns.

---

## CORRECTION: UI Service Patterns

### ApiFetchResult Pattern (CORRECT - This DOES exist!)

**ApiFetchResult is the STANDARD return type for UI services Create/Update/Delete operations.**

```csharp
public class ApiFetchResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int? Id { get; set; }  // Optional - returned from Create operations
}
```

**Location**: Currently in `BargeOps.UI/Services/BoatLocationService.cs` (lines 1136-1141)
**TODO**: Move to `BargeOps.Shared/Models/` or `BargeOps.UI/Models/` for reuse

### UI Service Interface Pattern (CORRECT)

```csharp
public interface IVendorService
{
    // DataTables search - returns DataTableResponse
    Task<DataTableResponse<VendorListModel>> GetVendorsAsync(DataTableRequest request, VendorSearchCriteria criteria);

    // Get single entity - returns ViewModel
    Task<VendorEditViewModel> GetVendorAsync(int id);

    // Create - returns ApiFetchResult with new ID
    Task<ApiFetchResult> CreateVendorAsync(VendorEditViewModel model);

    // Update - returns ApiFetchResult with success/error message
    Task<ApiFetchResult> UpdateVendorAsync(int id, VendorEditViewModel model);

    // Set Active/Inactive - returns ApiFetchResult
    Task<ApiFetchResult> SetVendorActiveAsync(int id, bool isActive);

    // Lookups - return SelectListItem collections
    Task<List<SelectListItem>> GetStatesAsync();
}
```

### UI Service Implementation Pattern (CORRECT)

```csharp
public async Task<ApiFetchResult> CreateVendorAsync(VendorEditViewModel model)
{
    try
    {
        using var client = GetClient();

        var dto = MapEditViewModelToCreateDto(model);
        var json = JsonConvert.SerializeObject(dto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("api/vendor", content);

        if (response.IsSuccessStatusCode)
        {
            var jsonResult = await response.Content.ReadAsStringAsync();
            var vendorId = JsonConvert.DeserializeObject<int>(jsonResult);

            return new ApiFetchResult
            {
                Success = true,
                Message = "Vendor created successfully",
                Id = vendorId
            };
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        return new ApiFetchResult
        {
            Success = false,
            Message = errorContent
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating vendor");
        return new ApiFetchResult
        {
            Success = false,
            Message = ex.Message
        };
    }
}
```

---

## CORRECTION: DataTableRequest Structure

### Full DataTableRequest Structure (CORRECT)

**The application SHOULD use the full jQuery DataTables structure:**

```csharp
namespace BargeOps.Shared.Dto;

/// <summary>
/// Full request model for jQuery DataTables server-side processing.
/// Supports DataTables' Columns, Order, and Search collections.
/// </summary>
public class DataTableRequest
{
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public DataTableSearch Search { get; set; }
    public List<DataTableColumn> Columns { get; set; }
    public List<DataTableOrder> Order { get; set; }
}

public class DataTableSearch
{
    public string Value { get; set; }
    public bool Regex { get; set; }
}

public class DataTableColumn
{
    public string Data { get; set; }
    public string Name { get; set; }
    public bool Searchable { get; set; }
    public bool Orderable { get; set; }
    public DataTableSearch Search { get; set; }
}

public class DataTableOrder
{
    public int Column { get; set; }
    public string Dir { get; set; }  // "asc" or "desc"
}

public class DataTableResponse<T>
{
    public int Draw { get; set; }
    public int RecordsTotal { get; set; }
    public int RecordsFiltered { get; set; }
    public IEnumerable<T> Data { get; set; } = Array.Empty<T>();
    public string Error { get; set; }  // Optional error message
}
```

### Parsing DataTableRequest (Helper Methods)

**Services should include helper methods to parse the DataTableRequest:**

```csharp
private string GetSortColumn(DataTableRequest request)
{
    if (request.Order?.Any() == true && request.Columns?.Any() == true)
    {
        var columnIndex = request.Order[0].Column;
        if (columnIndex < request.Columns.Count)
        {
            return request.Columns[columnIndex].Data;
        }
    }
    return "Name";  // Default sort column
}

private string GetSortDirection(DataTableRequest request)
{
    if (request.Order?.Any() == true)
    {
        return request.Order[0].Dir;
    }
    return "asc";
}
```

### Using DataTableRequest in Services

```csharp
public async Task<DataTableResponse<VendorListModel>> GetVendorsAsync(DataTableRequest request, VendorSearchCriteria criteria)
{
    try
    {
        using var client = GetClient();

        var listRequest = new ListRequest
        {
            Offset = request.Start,
            Limit = request.Length
        };

        // Parse sort from DataTableRequest
        var sortColumn = ToPascalCase(GetSortColumn(request));
        var sortDirection = GetSortDirection(request);
        listRequest.Order.Add(new SortField
        {
            Name = sortColumn,
            SortDescending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase)
        });

        // Add filters from criteria
        AddFilters(listRequest, criteria);

        var response = await client.PostAsJsonAsync("api/vendor/list", listRequest);

        if (response.IsSuccessStatusCode)
        {
            var jsonResult = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RootObject<VendorDto>>(jsonResult);

            var rows = result?.Data?.ToList() ?? new List<VendorDto>();
            var models = MapDtoToListModel(rows);
            var total = result?.Meta?.TotalCount ?? models.Count;

            return new DataTableResponse<VendorListModel>
            {
                Draw = request.Draw,
                RecordsTotal = total,
                RecordsFiltered = total,
                Data = models
            };
        }

        _logger.LogError("Failed to get vendors. Status: {StatusCode}", response.StatusCode);
        return new DataTableResponse<VendorListModel>
        {
            Draw = request.Draw,
            RecordsTotal = 0,
            RecordsFiltered = 0,
            Data = new List<VendorListModel>()
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting vendors");
        return new DataTableResponse<VendorListModel>
        {
            Draw = request.Draw,
            RecordsTotal = 0,
            RecordsFiltered = 0,
            Data = new List<VendorListModel>(),
            Error = ex.Message
        };
    }
}
```

---

## API Template Fixes (UNCHANGED - These are still correct)

### 1. Repository Method Return Types

**ALL Create and Update methods MUST return the full DTO:**

```csharp
// ✅ CORRECT
public async Task<VendorDto> CreateAsync(VendorDto vendor, CancellationToken cancellationToken = default)
{
    var sql = @"INSERT INTO Vendor (...) VALUES (...); SELECT CAST(SCOPE_IDENTITY() AS INT);";
    var newId = await connection.ExecuteScalarAsync<int>(sql, vendor);
    return await GetByIdAsync(newId, cancellationToken)
        ?? throw new InvalidOperationException("Failed to retrieve created entity");
}
```

### 2. SearchRequest Property Naming

**Use `*Only` suffix for boolean search filters:**
- DTO Property: `IsActiveOnly`, `FuelSuppliersOnly`
- Database Column: `IsActive`, `IsFuelSupplier`

### 3. Complete Interface Implementation

**Ensure ALL interface methods are implemented in the repository.**

---

## UI Template Fixes (CORRECTED)

### 1. Namespace and Base Class (CORRECT)

```csharp
namespace BargeOpsAdmin.ViewModels
{
    public class VendorSearchModel : BargeOpsAdminBaseModel<VendorSearchModel>
    {
        // ...
    }
}
```

### 2. Dropdown Lists (CORRECT)

```csharp
using Microsoft.AspNetCore.Mvc.Rendering;

public List<SelectListItem> States { get; set; } = new List<SelectListItem>();
```

### 3. Search Model Pattern (CORRECT)

```csharp
public class VendorSearchModel : BargeOpsAdminBaseModel<VendorSearchModel>
{
    [Display(Name = "Name")]
    public string? Name { get; set; }

    [Display(Name = "Active Only")]
    public bool IsActiveOnly { get; set; } = true;

    // ListQuery pagination properties
    public int Offset { get; set; } = 0;
    public int Limit { get; set; } = 25;
    public string? OrderBy { get; set; }
    public string? Fields { get; set; }
}
```

### 4. UI Service Generation (CORRECTED - DO GENERATE!)

**DO generate UI services with ApiFetchResult:**

```csharp
public interface IVendorService
{
    Task<DataTableResponse<VendorListModel>> GetVendorsAsync(DataTableRequest request, VendorSearchCriteria criteria);
    Task<VendorEditViewModel> GetVendorAsync(int id);
    Task<ApiFetchResult> CreateVendorAsync(VendorEditViewModel model);
    Task<ApiFetchResult> UpdateVendorAsync(int id, VendorEditViewModel model);
    Task<ApiFetchResult> SetVendorActiveAsync(int id, bool isActive);
}
```

**Service implementation should:**
1. Inherit from `BargeOpsAdminBaseService`
2. Use HttpClient to call API
3. Return `ApiFetchResult` for CUD operations
4. Return `DataTableResponse<T>` for search operations
5. Include helper methods for DataTableRequest parsing

---

## Template Generation Summary

### DO Generate

**API Layer:**
- ✅ Repository interfaces and implementations with `Task<TDto>` return types
- ✅ Service layer that handles DTO returns
- ✅ SearchRequest with `*Only` suffix for filters
- ✅ SQL files for CRUD operations

**UI Layer:**
- ✅ Service interfaces with `ApiFetchResult` return types
- ✅ Service implementations inheriting from `BargeOpsAdminBaseService`
- ✅ ViewModels with correct namespace and base class
- ✅ DataTableRequest with full Columns/Order/Search structure
- ✅ Helper methods for parsing DataTableRequest
- ✅ Controllers (when service is complete)
- ✅ Views (when controller is complete)

### DO NOT Generate

- ❌ Duplicate/partial files (`_Part1.cs`, `_Part2.cs`)
- ❌ Files with incorrect namespaces or types
- ❌ Incomplete implementations that won't compile

---

## Action Items for Template Generation

1. **Move ApiFetchResult to Shared** - Extract from BoatLocationService to `BargeOps.Shared/Models/ApiFetchResult.cs`
2. **Update DataTableRequest in Shared** - Replace simple version with full structure (Columns, Order, Search)
3. **Update DataTableResponse** - Ensure it's in Shared and includes Error property
4. **Update Templates** - Ensure all new entity templates follow these patterns
5. **Add Helper Methods** - Include GetSortColumn and GetSortDirection in service templates

---

## Verification Checklist

### API Templates
- [ ] Repository Create methods return `Task<TDto>`
- [ ] Repository Update methods return `Task<TDto>`
- [ ] SearchRequest uses `*Only` suffix for filters
- [ ] All interface methods implemented

### UI Templates
- [ ] Namespace is `BargeOpsAdmin.ViewModels`
- [ ] Base class is `BargeOpsAdminBaseModel<T>`
- [ ] Dropdown lists use `List<SelectListItem>`
- [ ] Service interface uses `ApiFetchResult` for CUD operations
- [ ] Service interface uses `DataTableResponse<T>` for search
- [ ] Service implementation includes DataTableRequest helper methods
- [ ] DataTableRequest has full structure (Columns, Order, Search)
- [ ] Optional fields are nullable (`string?`)
