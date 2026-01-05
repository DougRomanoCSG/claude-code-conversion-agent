# River ViewModels - Usage Guide

## Overview

This directory contains **4 ViewModels** for the River entity, each designed for specific UI scenarios following the **MVVM pattern**.

## Available ViewModels

### 1. RiverSearchViewModel
**File:** `RiverSearchViewModel.cs`
**Purpose:** Search/List screen
**Used By:** `Views/River/Index.cshtml`

**Properties:**
- `Code` (string?) - Filter by river code
- `Name` (string?) - Filter by river/waterway name
- `ActiveOnly` (bool) - Filter by active status (default: true)

**When to Use:**
- Main search/list page
- Grid filtering criteria
- All search criteria are optional

**Example:**
```csharp
public async Task<IActionResult> Index()
{
    var model = new RiverSearchViewModel
    {
        ActiveOnly = true
    };
    return View(model);
}
```

---

### 2. RiverEditViewModel
**File:** `RiverEditViewModel.cs`
**Purpose:** Edit/Create form
**Used By:** `Views/River/Edit.cshtml`

**Properties:**
- `River` (RiverDto) - Entity DTO from BargeOps.Shared
- `IsNew` (bool) - Computed property indicating create mode

**When to Use:**
- Create new river
- Edit existing river
- Form submission and validation

**Key Features:**
- Contains DTO directly from Shared project (no mapping!)
- Single property for both create and edit modes
- Validation handled by DTO attributes

**Example:**
```csharp
// Create
public IActionResult Create()
{
    var model = new RiverEditViewModel
    {
        River = new RiverDto { IsActive = true }
    };
    return View("Edit", model);
}

// Edit
public async Task<IActionResult> Edit(int id)
{
    var river = await _riverService.GetByIdAsync(id);
    var model = new RiverEditViewModel { River = river };
    return View(model);
}
```

---

### 3. RiverDetailsViewModel
**File:** `RiverDetailsViewModel.cs`
**Purpose:** Read-only details view
**Used By:** `Views/River/Details.cshtml`

**Properties:**
- `River` (RiverDto) - Entity DTO from BargeOps.Shared
- `StartMileFormatted` (string) - Formatted start mile with 2 decimals
- `EndMileFormatted` (string) - Formatted end mile with 2 decimals
- `MileRange` (string) - Computed mile range display
- `DirectionDisplay` (string) - Formatted direction text
- `StatusDisplay` (string) - Formatted status text
- `StatusBadgeClass` (string) - Bootstrap CSS class for status badge

**When to Use:**
- Read-only details page
- View river information without editing
- Display formatted/computed values

**Key Features:**
- Computed properties for better display formatting
- No form inputs - pure display
- Bootstrap badge classes for status

**Example:**
```csharp
public async Task<IActionResult> Details(int id)
{
    var river = await _riverService.GetByIdAsync(id);
    var model = new RiverDetailsViewModel { River = river };
    return View(model);
}
```

---

### 4. RiverListItemViewModel
**File:** `RiverListItemViewModel.cs`
**Purpose:** Grid row data (alternative to using DTOs directly)
**Used By:** DataTables grid (optional)

**Properties:**
- All river properties as individual fields
- `MileRange` (string) - Computed mile range
- `StatusText` (string) - Status display text
- `StatusBadgeClass` (string) - CSS class for badges
- `DirectionText` (string) - Direction display text

**When to Use:**
- If you need computed properties for grid display
- If you want to separate grid data from DTOs
- If you need different formatting than DTOs provide

**Note:**
- **Current implementation uses DTOs directly in DataTables**
- This ViewModel is optional and provided for future enhancement
- Use this if you need additional computed fields not in DTO

**Example:**
```csharp
// Map DTO to ListItemViewModel
public RiverListItemViewModel MapToListItem(RiverDto dto)
{
    return new RiverListItemViewModel
    {
        RiverID = dto.RiverID,
        Code = dto.Code,
        Name = dto.Name,
        StartMile = dto.StartMile?.ToString("0.00") ?? "",
        EndMile = dto.EndMile?.ToString("0.00") ?? "",
        IsLowToHighDirection = dto.IsLowToHighDirection,
        IsActive = dto.IsActive,
        UpLabel = dto.UpLabel,
        DownLabel = dto.DownLabel,
        BargeExCode = dto.BargeExCode
    };
}
```

---

## ViewModel Selection Guide

| Scenario | ViewModel | View | Notes |
|----------|-----------|------|-------|
| **Search/List Page** | RiverSearchViewModel | Index.cshtml | Simple search criteria |
| **Create New River** | RiverEditViewModel | Edit.cshtml | IsNew = true |
| **Edit Existing River** | RiverEditViewModel | Edit.cshtml | IsNew = false |
| **View Details (Read-Only)** | RiverDetailsViewModel | Details.cshtml | Formatted display |
| **Grid Rows** | Use DTO directly | Index.cshtml | Or use RiverListItemViewModel if needed |

---

## MVVM Pattern Rules

All ViewModels follow these rules:

1. **NO ViewBag/ViewData** - All data must be on the ViewModel
2. **File-scoped namespace** - `namespace BargeOpsAdmin.ViewModels;`
3. **Display attributes** - `[Display(Name = "...")]` for all user-facing properties
4. **ID fields uppercase** - `RiverID` not `RiverId`
5. **DateTime as single property** - Not split into date/time (unless required by UI)
6. **Dropdowns as SelectListItem** - `IEnumerable<SelectListItem>` properties
7. **Validation on ViewModel** - Data annotations where appropriate
8. **Contains DTOs from Shared** - Not duplicate properties (use DTO directly!)

---

## Code Organization

```
ViewModels/
├── RiverSearchViewModel.cs      ⭐ Search criteria
├── RiverEditViewModel.cs         ⭐ Edit/Create form (contains DTO)
├── RiverDetailsViewModel.cs      ⭐ Read-only details (contains DTO)
├── RiverListItemViewModel.cs     Optional grid row model
└── README_ViewModels.md          This guide
```

---

## Best Practices

### ✅ DO
- Use DTOs from BargeOps.Shared directly in ViewModels
- Add computed properties for formatting (like `MileRange`)
- Use `[Display]` attributes for all user-facing properties
- Keep ViewModels in `ViewModels/` folder
- Follow uppercase ID convention (`RiverID`)

### ❌ DON'T
- Duplicate DTO properties in ViewModel (use DTO directly!)
- Use ViewBag or ViewData (violates MVVM)
- Create separate date and time properties (unless UI requires it)
- Mix business logic in ViewModels (keep it in services)
- Use lowercase ID (`RiverId` is wrong, `RiverID` is correct)

---

## Example Usage in Controller

```csharp
[Authorize]
public class RiverController : AppController
{
    private readonly IRiverService _riverService;

    // Search/List
    public IActionResult Index()
    {
        return View(new RiverSearchViewModel { ActiveOnly = true });
    }

    // Create
    public IActionResult Create()
    {
        var model = new RiverEditViewModel
        {
            River = new RiverDto
            {
                IsActive = true,
                IsLowToHighDirection = true
            }
        };
        return View("Edit", model);
    }

    // Edit
    public async Task<IActionResult> Edit(int id)
    {
        var river = await _riverService.GetByIdAsync(id);
        return View(new RiverEditViewModel { River = river });
    }

    // Details
    public async Task<IActionResult> Details(int id)
    {
        var river = await _riverService.GetByIdAsync(id);
        return View(new RiverDetailsViewModel { River = river });
    }

    // Save (handles both create and update)
    [HttpPost]
    public async Task<IActionResult> Edit(RiverEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (model.IsNew)
            await _riverService.CreateRiverAsync(model.River);
        else
            await _riverService.UpdateRiverAsync(model.River.RiverID, model.River);

        return RedirectToAction(nameof(Index));
    }
}
```

---

**Generated:** 2025-12-11
**Status:** Ready for Use
**Architecture:** MONO SHARED - DTOs from BargeOps.Shared
