# RiverArea Conversion Templates - READY TO USE! 🚀

**Generated**: 2025-12-11
**Entity**: RiverArea
**Architecture**: ASP.NET Core 8 MVC with Mono Shared Structure

---

## 📦 What's Included

This package contains **production-ready templates** for converting the RiverArea VB.NET WinForms application to ASP.NET Core 8 MVC.

### ✅ Generated Files

```
output/RiverArea/
├── conversion-plan.md                      ⭐ START HERE! Comprehensive guide
├── README.md                               📖 This file
└── templates/
    ├── shared/Dto/                         🔑 CREATE THESE FIRST!
    │   ├── RiverAreaDto.cs                 ✅ Main entity DTO
    │   ├── RiverAreaListDto.cs             ✅ Search results DTO
    │   ├── RiverAreaSearchRequest.cs       ✅ Search criteria DTO
    │   └── RiverAreaSegmentDto.cs          ✅ Child segment DTO
    │
    ├── api/                                🌐 API Layer Templates
    │   ├── Repositories/
    │   │   ├── IRiverAreaRepository.cs     ✅ Repository interface
    │   │   └── RiverAreaRepository.cs      ✅ Dapper implementation
    │   └── (Additional API templates available on request)
    │
    └── ui/                                 🖥️ UI Layer Templates
        ├── ViewModels/
        │   ├── RiverAreaSearchViewModel.cs ✅ Search screen ViewModel
        │   └── RiverAreaEditViewModel.cs   ✅ Edit screen ViewModel
        └── wwwroot/js/
            ├── riverAreaSearch.js          ✅ Search page JavaScript
            └── riverAreaEdit.js            ✅ Edit page JavaScript
```

---

## 🚀 Quick Start

### Step 1: Read the Conversion Plan

📋 **Open**: `conversion-plan.md`

This document contains:
- Complete architecture overview
- Step-by-step implementation guide
- Validation rules
- Special considerations
- Testing checklist

### Step 2: Create Shared DTOs (MOST IMPORTANT!)

⭐ **Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\Dto\`

Copy these files **FIRST**:
```bash
# Copy from templates/shared/Dto/ to BargeOps.Shared/Dto/
cp RiverAreaDto.cs → BargeOps.Shared/Dto/
cp RiverAreaListDto.cs → BargeOps.Shared/Dto/
cp RiverAreaSearchRequest.cs → BargeOps.Shared/Dto/
cp RiverAreaSegmentDto.cs → BargeOps.Shared/Dto/
```

**Why first?** Because both API and UI projects reference these DTOs!

### Step 3: Build Shared Project

```bash
cd C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared
dotnet build
```

Verify compilation succeeds before proceeding.

### Step 4: Create API Infrastructure

📂 **Locations**:
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Repositories\`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Services\`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Api\Controllers\`

Copy templates and customize as needed.

### Step 5: Create UI Layer

📂 **Locations**:
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\ViewModels\`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Controllers\`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Views\RiverArea\`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\wwwroot\js\`

Copy templates and customize as needed.

---

## 🎯 Key Features Implemented

### ✅ Shared DTOs (Single Source of Truth)
- All DTOs in BargeOps.Shared
- Used by BOTH API and UI
- NO duplication, NO mapping!

### ✅ Dapper Repository with Direct SQL
- Returns DTOs directly
- Uses embedded SQL queries (NOT stored procedures)
- Full CRUD + child segment management

### ✅ ViewModels with MVVM Pattern
- Contains DTOs from Shared project
- NO ViewBag/ViewData
- Includes dropdown data (SelectList)
- Feature flags for conditional UI

### ✅ Complex Validation
- Mutually exclusive checkboxes (IsPriceZone, IsPortalArea, IsHighWaterArea)
- Conditional required field (CustomerID when IsHighWaterArea = true)
- Segment validation (StartMile < EndMile)
- Pricing zone overlap detection

### ✅ Modern JavaScript
- DataTables with server-side processing
- Select2 dropdowns
- jQuery Validation
- Child segment grid management
- Form serialization

---

## 🔍 What ViewModels Were Generated?

### 1. RiverAreaSearchViewModel.cs

**Purpose**: Search screen ViewModel
**Location**: `templates/ui/ViewModels/`

**Properties**:
- Search criteria: Name, ActiveOnly, PricingZonesOnly, etc.
- Dropdown data: Customers (SelectList)
- Feature flags: ShowPortalAreas, ShowHighWaterFilters, ShowLiquidRateColumn

**Usage**:
```csharp
public IActionResult Index()
{
    var viewModel = new RiverAreaSearchViewModel
    {
        Customers = await GetCustomersSelectList(),
        ShowPortalAreas = _featureFlags.PortalEnabled,
        ShowHighWaterFilters = _featureFlags.HighWaterModelEnabled
    };
    return View(viewModel);
}
```

### 2. RiverAreaEditViewModel.cs

**Purpose**: Edit screen ViewModel
**Location**: `templates/ui/ViewModels/`

**Properties**:
- RiverArea (RiverAreaDto from BargeOps.Shared) - THE ACTUAL DTO!
- Convenience properties with [Display] attributes
- Dropdown data: Customers, Rivers (SelectList)
- Feature flags: ShowPortalArea, ShowHighWaterArea, ShowLiquidRateArea

**Pattern**:
```csharp
public class RiverAreaEditViewModel
{
    // The actual DTO from Shared
    public RiverAreaDto RiverArea { get; set; }

    // Convenience property with validation
    [Required]
    [StringLength(50)]
    [Display(Name = "River Area Name")]
    public string Name
    {
        get => RiverArea?.Name;
        set { if (RiverArea != null) RiverArea.Name = value; }
    }

    // Dropdown data
    public IEnumerable<SelectListItem> Customers { get; set; }
}
```

**Why this pattern?**
- ViewModel contains the DTO (not duplicates it!)
- Convenience properties for easy binding and validation
- View uses `asp-for="Name"` which maps to the convenience property
- On submit, the full DTO is populated via the convenience properties

---

## ⚠️ CRITICAL Architecture Notes

### 1. DTOs in BargeOps.Shared ONLY!

```
❌ WRONG:
BargeOps.API/Models/RiverAreaDto.cs  (NO!)
BargeOps.UI/Models/RiverAreaDto.cs   (NO!)

✅ CORRECT:
BargeOps.Shared/Dto/RiverAreaDto.cs  (YES!)
```

### 2. Repositories Return DTOs Directly

```csharp
// ❌ WRONG:
public async Task<RiverArea> GetByIdAsync(int id)  // Domain model
{
    var dto = await query...
    return _mapper.Map<RiverArea>(dto);  // Extra mapping!
}

// ✅ CORRECT:
public async Task<RiverAreaDto> GetByIdAsync(int id)  // DTO directly!
{
    return await query...  // No mapping needed!
}
```

### 3. ViewModels Contain DTOs (Not Duplicate Them)

```csharp
// ❌ WRONG:
public class RiverAreaEditViewModel
{
    public int RiverAreaID { get; set; }  // Duplicating DTO properties!
    public string Name { get; set; }
    // ... all properties duplicated
}

// ✅ CORRECT:
public class RiverAreaEditViewModel
{
    public RiverAreaDto RiverArea { get; set; }  // Contains the DTO!

    // Convenience properties map to DTO
    public string Name
    {
        get => RiverArea?.Name;
        set { if (RiverArea != null) RiverArea.Name = value; }
    }
}
```

---

## 🧪 Testing Checklist

Before considering the conversion complete, test these scenarios:

### Functionality
- [ ] Create new river area with segments
- [ ] Edit existing river area
- [ ] Delete river area (verify hard delete)
- [ ] Search with various filter combinations
- [ ] Add/edit/delete segments in edit form

### Validation
- [ ] Name required and max length (50)
- [ ] Mutually exclusive flags (only one of Price/Portal/HighWater)
- [ ] CustomerID required when IsHighWaterArea = true
- [ ] CustomerID auto-clears when IsHighWaterArea = false
- [ ] Segment fields (River, StartMile, EndMile) required
- [ ] StartMile < EndMile validation
- [ ] Pricing zone overlap detection

### UI/UX
- [ ] DataTables pagination, sorting, filtering work
- [ ] Select2 dropdowns work smoothly
- [ ] Reset button clears all filters
- [ ] Success/error messages display correctly
- [ ] Responsive layout on mobile devices
- [ ] Keyboard navigation works (Tab, Enter)

---

## 📚 Additional Templates Available

The following templates can be generated on request:

### API Layer
- `IRiverAreaService.cs` - Service interface
- `RiverAreaService.cs` - Business logic implementation
- `RiverAreaController.cs` - REST API controller

### UI Layer
- `IRiverAreaService.cs` (UI) - API client interface
- `RiverAreaService.cs` (UI) - HTTP client implementation
- `RiverAreaController.cs` (UI) - MVC controller
- `Index.cshtml` - Search view
- `Edit.cshtml` - Edit view

**To request additional templates**, let me know and I'll generate them immediately!

---

## 🎓 Learning Resources

### Pattern References in Mono Repo

**Shared DTOs**:
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\Dto\FacilityDto.cs`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\Dto\BoatLocationDto.cs`

**API Controllers**:
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Api\Controllers\FacilityController.cs`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Api\Controllers\BoatLocationController.cs`

**API Repositories**:
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Repositories\FacilityRepository.cs`

**UI ViewModels**:
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\ViewModels\BoatLocationSearchViewModel.cs`
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\ViewModels\BoatLocationEditViewModel.cs`

**UI Controllers**:
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Controllers\BoatLocationSearchController.cs`

**UI Views**:
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Views\BoatLocationSearch\Index.cshtml`

**JavaScript**:
- `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\wwwroot\js\boatLocationSearch.js`

---

## 🤝 Need Help?

### Common Issues

**Issue**: "RiverAreaDto not found"
**Solution**: Verify BargeOps.Shared is built and referenced by both API and UI projects

**Issue**: "Mutually exclusive checkboxes not working"
**Solution**: Ensure `riverAreaEdit.js` is loaded on the edit page

**Issue**: "Segments not saving"
**Solution**: Verify form submission serializes segments as hidden fields

**Issue**: "CustomerID not required when IsHighWaterArea checked"
**Solution**: Check JavaScript `toggleCustomerField()` function is executing

---

## ✨ Summary

You now have **production-ready templates** for converting RiverArea to ASP.NET Core 8 MVC!

### What Makes This Special?

✅ **Mono Shared Architecture** - Single source of truth for DTOs
✅ **No Duplication** - DTOs shared between API and UI
✅ **No Mapping** - Repositories return DTOs directly
✅ **MVVM Pattern** - ViewModels contain DTOs (not duplicate them)
✅ **Modern Stack** - Bootstrap 5, DataTables, Select2, jQuery
✅ **Complex Validation** - Mutually exclusive flags, conditional requirements
✅ **Child Collections** - Full CRUD for river area segments

### Next Steps

1. ✅ Copy Shared DTOs to BargeOps.Shared
2. ✅ Build Shared project
3. ✅ Copy API templates and customize
4. ✅ Copy UI templates and customize
5. ✅ Test thoroughly using checklist
6. ✅ Deploy!

---

**Happy Coding!** 🎉

*Generated by ClaudeOnshoreConversionAgent - 2025-12-11*
