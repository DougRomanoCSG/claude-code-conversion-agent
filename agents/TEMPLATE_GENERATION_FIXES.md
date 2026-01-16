# Template Generation Fixes and Best Practices

## Overview
This document captures critical fixes identified during Vendor entity generation (January 2026). These fixes must be applied to template generation agents to prevent compilation errors.

## API Template Fixes

### 1. Repository Method Return Types

**PROBLEM**: Generated repository Create/Update methods returned `Task<int>` (ID only), but interface expected `Task<TDto>` (full entity).

**FIX**: All Create and Update methods must return the full DTO after the operation:

```csharp
// ❌ INCORRECT - Returns only ID
public async Task<int> CreateAsync(VendorDto vendor, CancellationToken cancellationToken = default)
{
    var sql = @"INSERT INTO Vendor (...) VALUES (...); SELECT CAST(SCOPE_IDENTITY() AS INT);";
    return await connection.ExecuteScalarAsync<int>(sql, vendor);
}

// ✅ CORRECT - Returns full DTO
public async Task<VendorDto> CreateAsync(VendorDto vendor, CancellationToken cancellationToken = default)
{
    var sql = @"INSERT INTO Vendor (...) VALUES (...); SELECT CAST(SCOPE_IDENTITY() AS INT);";
    var newId = await connection.ExecuteScalarAsync<int>(sql, vendor);
    return await GetByIdAsync(newId, cancellationToken)
        ?? throw new InvalidOperationException("Failed to retrieve created vendor");
}
```

**APPLIES TO**:
- `CreateAsync` - Return full DTO after insert
- `UpdateAsync` - Return full DTO after update
- All child entity Create/Update methods (Contacts, BusinessUnits, etc.)

**IMPLEMENTATION PATTERN**:
For entities without GetById, use inline SELECT:
```csharp
var newId = await connection.ExecuteScalarAsync<int>(insertSql, entity);

var selectSql = @"SELECT * FROM TableName WHERE ID = @ID";
return await connection.QuerySingleAsync<TDto>(selectSql, new { ID = newId });
```

### 2. SearchRequest Property Naming

**PROBLEM**: Repository code used property names that didn't match the SearchRequest DTO.

**CORRECT PATTERN**: Use `*Only` suffix for boolean search filters:
```csharp
// SearchRequest DTO
public class VendorSearchRequest : DataTableRequest
{
    public string? Name { get; set; }
    public string? AccountingCode { get; set; }
    public bool? IsActiveOnly { get; set; } = true;        // ✅ Note: *Only suffix
    public bool? FuelSuppliersOnly { get; set; }           // ✅ Note: *Only suffix
    public bool? InternalVendorOnly { get; set; }          // ✅ Note: *Only suffix
}

// Repository implementation
if (request.IsActiveOnly.HasValue)  // ✅ Use IsActiveOnly, not IsActive
{
    whereConditions.Add("IsActive = @IsActive");
    parameters.Add("IsActive", request.IsActiveOnly.Value);
}
```

**NAMING CONVENTION**:
- DTO Property: `IsActiveOnly`, `FuelSuppliersOnly`, `EnablePortalOnly`
- Database Column: `IsActive`, `IsFuelSupplier`, `EnablePortal`

### 3. DataTableRequest Properties

**PROBLEM**: Generated code tried to call methods like `request.GetSortColumn()` and `request.GetSortDirection()`.

**FIX**: These are properties, not methods:

```csharp
// ❌ INCORRECT
var orderByColumn = request.GetSortColumn() ?? "Name";
var orderByDirection = request.GetSortDirection();

// ✅ CORRECT
var orderByColumn = request.SortColumn ?? "Name";
var orderByDirection = request.SortDirection ?? "asc";
```

**DataTableRequest PROPERTIES**:
```csharp
public class DataTableRequest
{
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public string? SortColumn { get; set; }      // ✅ Property
    public string? SortDirection { get; set; }   // ✅ Property
}
```

### 4. Service Layer Changes

**PROBLEM**: Service methods expected to receive IDs from repository methods but now receive DTOs.

**FIX**: Update service methods to handle DTO returns:

```csharp
// ❌ INCORRECT - Old pattern
public async Task<VendorDto> CreateAsync(VendorDto vendor, CancellationToken cancellationToken = default)
{
    await _vendorValidator.ValidateAndThrowAsync(vendor, cancellationToken);
    ApplyBusinessRules(vendor);

    var vendorId = await _vendorRepository.CreateAsync(vendor, cancellationToken);
    vendor.VendorID = vendorId;
    return vendor;
}

// ✅ CORRECT - New pattern
public async Task<VendorDto> CreateAsync(VendorDto vendor, CancellationToken cancellationToken = default)
{
    await _vendorValidator.ValidateAndThrowAsync(vendor, cancellationToken);
    ApplyBusinessRules(vendor);

    var createdVendor = await _vendorRepository.CreateAsync(vendor, cancellationToken);
    return createdVendor;
}
```

### 5. Missing Interface Methods

**PROBLEM**: Repository implementation was missing methods declared in interface.

**FIX**: Ensure ALL interface methods are implemented. Example:
```csharp
// Interface declared this but implementation was missing:
Task<IEnumerable<VendorPortalGroupDto>> GetPortalGroupsAsync(int vendorId, CancellationToken cancellationToken = default);

// Must be implemented:
public async Task<IEnumerable<VendorPortalGroupDto>> GetPortalGroupsAsync(int vendorId, CancellationToken cancellationToken = default)
{
    using var connection = new SqlConnection(_connectionString);
    var sql = @"SELECT * FROM VendorPortalGroup WHERE VendorID = @VendorID ORDER BY PortalGroupID";
    return await connection.QueryAsync<VendorPortalGroupDto>(sql, new { VendorID = vendorId });
}
```

## UI Template Fixes

### 1. Namespace Corrections

**PROBLEM**: Generated code used incorrect namespaces.

**CORRECT NAMESPACE**:
```csharp
// ✅ CORRECT
namespace BargeOpsAdmin.ViewModels
{
    public class VendorSearchModel : BargeOpsAdminBaseModel<VendorSearchModel>
    {
        // ...
    }
}
```

**INCORRECT NAMESPACES**:
- ❌ `BargeOps.UI.Models`
- ❌ `BargeOpsAdmin.Models`
- ❌ `BargeOps.Shared.ViewModels`

### 2. Base Class Corrections

**PROBLEM**: Generated ViewModels inherited from non-existent `CrewingBaseModel<T>`.

**FIX**: Use `BargeOpsAdminBaseModel<T>`:

```csharp
// ❌ INCORRECT
public class VendorSearchModel : CrewingBaseModel<VendorSearchModel>

// ✅ CORRECT
public class VendorSearchModel : BargeOpsAdminBaseModel<VendorSearchModel>
```

### 3. Dropdown List Type

**PROBLEM**: Generated code used `List<LookupModel>` for dropdowns, which doesn't exist.

**FIX**: Use `List<SelectListItem>` from `Microsoft.AspNetCore.Mvc.Rendering`:

```csharp
// ❌ INCORRECT
public List<LookupModel> States { get; set; } = new List<LookupModel>();

// ✅ CORRECT
using Microsoft.AspNetCore.Mvc.Rendering;

public class VendorDetailViewModel : BargeOpsAdminBaseModel<VendorDetailViewModel>
{
    public List<SelectListItem> States { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> TermsCodes { get; set; } = new List<SelectListItem>();
}
```

### 4. Search Model Pattern

**PROBLEM**: Search models had unnecessary `GetFields()` override and result properties.

**CORRECT PATTERN** for Search Models:
```csharp
using System.ComponentModel.DataAnnotations;

namespace BargeOpsAdmin.ViewModels
{
    public class VendorSearchModel : BargeOpsAdminBaseModel<VendorSearchModel>
    {
        // Search criteria only
        [Display(Name = "Name")]
        [StringLength(100)]
        public string? Name { get; set; }

        [Display(Name = "Accounting Code")]
        [StringLength(50)]
        public string? AccountingCode { get; set; }

        [Display(Name = "Active Only")]
        public bool IsActiveOnly { get; set; } = true;

        // ListQuery pagination properties
        public int Offset { get; set; } = 0;
        public int Limit { get; set; } = 25;
        public string? OrderBy { get; set; }
        public string? Fields { get; set; }

        // ✅ NO GetFields() override needed for search models
        // ✅ NO result properties (those come from API DTOs)
    }
}
```

### 5. Optional Field Nullability

**FIX**: Make optional string fields nullable:

```csharp
// ❌ INCORRECT - Non-nullable optional fields
public string AccountingCode { get; set; }
public string Address { get; set; }

// ✅ CORRECT - Nullable optional fields
public string? AccountingCode { get; set; }
public string? Address { get; set; }
```

### 6. Detail ViewModel Pattern

**CORRECT PATTERN** for Detail ViewModels:
```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BargeOpsAdmin.ViewModels
{
    public class VendorDetailViewModel : BargeOpsAdminBaseModel<VendorDetailViewModel>
    {
        // Primary Key
        public int VendorID { get; set; }

        // Required Fields
        [Required(ErrorMessage = "Name is required")]
        [StringLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
        [Display(Name = "Name")]
        public string Name { get; set; }

        // Optional Fields (nullable)
        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        // Boolean Fields
        [Display(Name = "Internal Vendor")]
        public bool IsInternalVendor { get; set; }

        // UI State Flags
        public bool IsDisabled { get; set; }
        public bool IsViewDetail { get; set; }

        // Child Collections
        public List<VendorContactViewModel> Contacts { get; set; } = new List<VendorContactViewModel>();

        // Dropdown Lists (use SelectListItem)
        public List<SelectListItem> States { get; set; } = new List<SelectListItem>();

        // ✅ NO GetFields() override for detail models
        // ✅ NO ApiFetchResult property
    }
}
```

### 7. Don't Generate Incomplete Files

**DO NOT GENERATE**:
1. **Service interfaces** referencing non-existent types like `ApiFetchResult`
2. **Controllers** without working service implementations
3. **Views** without working controllers
4. **Duplicate/partial files** (e.g., `_Part1.cs`, `_Part2.cs`)

**REASON**: These create compilation errors and must be manually deleted.

## Agent System Prompt Updates

### API Generator Updates

Add to `conversion-template-generator-api-prompt.md`:

```markdown
## Repository Method Return Types

**CRITICAL**: All Create and Update methods must return the full DTO:

1. **CreateAsync**: Return `Task<TDto>`, fetch entity after insert using GetByIdAsync or inline SELECT
2. **UpdateAsync**: Return `Task<TDto>`, fetch entity after update using GetByIdAsync or inline SELECT
3. **Child Entity Methods**: Follow same pattern for all nested entities

**Implementation**:
```csharp
public async Task<VendorDto> CreateAsync(VendorDto vendor, CancellationToken cancellationToken = default)
{
    var sql = @"INSERT INTO Vendor (...) VALUES (...); SELECT CAST(SCOPE_IDENTITY() AS INT);";
    var newId = await connection.ExecuteScalarAsync<int>(sql, vendor);
    return await GetByIdAsync(newId, cancellationToken)
        ?? throw new InvalidOperationException("Failed to retrieve created vendor");
}
```

## SearchRequest Naming Convention

**CRITICAL**: Use `*Only` suffix for boolean search filters:
- Property: `IsActiveOnly`, `FuelSuppliersOnly` (in DTO)
- Column: `IsActive`, `IsFuelSupplier` (in database)

## DataTableRequest Properties

Access as properties, not methods:
- `request.SortColumn` (not `request.GetSortColumn()`)
- `request.SortDirection` (not `request.GetSortDirection()`)
```

### UI Generator Updates

Add to `conversion-template-generator-ui-prompt.md`:

```markdown
## UI Template Generation Standards (BargeOps Admin)

### Namespace and Base Classes

**CRITICAL**: Use correct namespace and base class:
```csharp
namespace BargeOpsAdmin.ViewModels  // ✅ CORRECT namespace
{
    public class EntityViewModel : BargeOpsAdminBaseModel<EntityViewModel>  // ✅ CORRECT base class
    {
        // ...
    }
}
```

### Dropdown Lists

**CRITICAL**: Use `SelectListItem` for dropdowns:
```csharp
using Microsoft.AspNetCore.Mvc.Rendering;

public List<SelectListItem> States { get; set; } = new List<SelectListItem>();
```

### Search Model Pattern

Search models should:
1. Inherit from `BargeOpsAdminBaseModel<T>`
2. Include search criteria properties only
3. Include ListQuery pagination properties (Offset, Limit, OrderBy, Fields)
4. **NOT** include `GetFields()` override
5. **NOT** include result properties

### Optional Fields

Make optional string fields nullable with `string?` type.

### DO NOT GENERATE

1. Service interfaces with non-existent types (e.g., `ApiFetchResult`)
2. Controllers without complete service implementations
3. Duplicate/partial files (`_Part1.cs`, etc.)
4. Views without controllers
```

## Verification Checklist

Before finalizing templates, verify:

### API Templates
- [ ] All Create methods return `Task<TDto>`, not `Task<int>`
- [ ] All Update methods return `Task<TDto>`, not `Task`
- [ ] SearchRequest properties use `*Only` suffix for filters
- [ ] DataTableRequest accessed via properties, not methods
- [ ] All interface methods are implemented
- [ ] Service methods handle DTO returns from repository

### UI Templates
- [ ] Namespace is `BargeOpsAdmin.ViewModels`
- [ ] Base class is `BargeOpsAdminBaseModel<T>`
- [ ] Dropdown lists use `List<SelectListItem>`
- [ ] Search models have ListQuery properties
- [ ] Search models do NOT have `GetFields()` override
- [ ] Optional fields are nullable (`string?`)
- [ ] No service interfaces with non-existent types
- [ ] No incomplete controllers/views generated

## Summary

These fixes prevent the following compilation errors:
1. **CS0738**: Return type mismatch (Create/Update methods)
2. **CS1061**: Property/method not found (GetSortColumn, IsActive vs IsActiveOnly)
3. **CS0246**: Type not found (LookupModel, ApiFetchResult, CrewingBaseModel)
4. **CS0535**: Interface member not implemented (GetPortalGroupsAsync)

Apply these patterns to ALL entity template generation to ensure clean compilation.
