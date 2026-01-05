# BargeEvent Conversion Templates

## Overview

This directory contains complete conversion templates for migrating the **BargeEvent** entity from VB.NET WinForms to ASP.NET Core with Mono Shared architecture.

**Entity Complexity**: ⭐⭐⭐⭐⭐ Very High
**Priority**: High
**Estimated Effort**: 2-3 weeks

---

## What Has Been Generated

### ✅ Phase 1: SHARED DTOs (COMPLETE)

**Location**: `templates/shared/Dto/`

All DTOs created for use by **BOTH** API and UI projects:

1. **BargeEventDto.cs** - Complete entity DTO
   - 100+ properties from TicketEvent table
   - Full billing and freight fields
   - Audit trail fields
   - Computed/display fields
   - `[Sortable]` and `[Filterable]` attributes for ListQuery

2. **BargeEventSearchRequest.cs** - Search criteria DTO
   - 12+ search filter properties
   - ListQuery support (sorting, paging)
   - Validation for required Fleet ID
   - `HasAtLeastOneCriterion()` method

3. **BargeEventSearchDto.cs** - Search grid results DTO
   - Flattened structure with joined data
   - License-dependent fields (Freight)
   - Row formatting helpers
   - Display-friendly property names

4. **BargeEventBillingDto.cs** - Billing search results DTO
   - Financial and rate information
   - Ready-to-invoice indicators
   - Rate validation flags
   - GL account integration

---

## Architecture

### MONO SHARED Structure ⭐

```
C:\Dev\BargeOps.Admin.Mono\
├── src\
│   ├── BargeOps.Shared\           ⭐ SHARED PROJECT
│   │   └── Dto\                   ⭐ Copy DTOs here FIRST!
│   │       ├── BargeEventDto.cs
│   │       ├── BargeEventSearchRequest.cs
│   │       ├── BargeEventSearchDto.cs
│   │       └── BargeEventBillingDto.cs
│   │
│   ├── BargeOps.API\              (uses Shared DTOs)
│   │   ├── Controllers\
│   │   ├── Repositories\
│   │   └── Services\
│   │
│   └── BargeOps.UI\               (uses Shared DTOs)
│       ├── ViewModels\            (contain Shared DTOs)
│       ├── Services\              (return Shared DTOs)
│       └── Views\
```

**Key Principles**:
- ✅ DTOs in BargeOps.Shared are the SINGLE SOURCE OF TRUTH
- ✅ NO separate domain models in API
- ✅ NO AutoMapper - repositories return DTOs directly
- ✅ ViewModels in UI contain DTOs (not duplicate them)

---

## Next Steps

### Would you like me to generate ViewModels?

I can create the following ViewModels for the UI layer:

1. **BargeEventSearchViewModel** - For the search/list screen
   - Search criteria properties
   - SelectList properties for dropdowns
   - Permission flags (CanModify, CanViewBilling)
   - License flags (IsFreightActive, IsOnboardActive)

2. **BargeEventEditViewModel** - For the edit/create form
   - BargeEventDto property (from Shared)
   - Lookup lists (Customers, EventTypes, Boats, etc.)
   - Related data (Barges list, Billing audits)
   - Permission and license flags

**Do you want me to generate these ViewModels now?** (Yes/No)

---

## Implementation Checklist

### ✅ Completed
- [x] Reviewed all analysis files
- [x] Created comprehensive conversion plan
- [x] Generated Shared DTOs (BargeEventDto, SearchRequest, SearchDto, BillingDto)

### ⏳ Remaining Templates to Generate

**API Layer**:
- [ ] Repository interface (IBargeEventRepository.cs)
- [ ] Repository implementation (BargeEventRepository.cs)
- [ ] Service interface (IBargeEventService.cs)
- [ ] Service implementation (BargeEventService.cs)
- [ ] API Controller (BargeEventController.cs)
- [ ] SQL files (Search, GetById, Insert, Update, etc.)

**UI Layer**:
- [ ] ViewModels (Search, Edit) - **Ask user first!**
- [ ] Service interface (IBargeEventService.cs)
- [ ] Service implementation (BargeEventService.cs)
- [ ] UI Controllers (Search, Detail)
- [ ] Razor Views (Index.cshtml, Edit.cshtml)
- [ ] JavaScript files (search, detail)

---

## Key Features

### Multi-Tab Interface
- **Barge Details** - Main event information
- **Billing** - Billing rates and calculations
- **Freight Billing** - Freight-specific billing (license-dependent)
- **Demurrage Billing** - Demurrage calculations
- **Billing Audit** - Change history

### Complex Search Criteria
- Event type, customers, locations, boats
- Date ranges with time
- Barge number lists (comma-separated)
- Contract numbers, rate IDs
- Include voided events option

### License-Based Features
- **Freight License**: Freight billing, C/P time, contract numbers
- **Onboard License**: Create onboard orders

### Permission-Based Access
- **BargeEventView**: View search results
- **BargeEventModify**: Create/edit events
- **BargeEventBillingView**: View billing tabs
- **BargeEventBillingModify**: Edit billing information

---

## Critical Patterns

### DateTime Handling (24-hour format!)

**ViewModel**: Single DateTime property
```csharp
public DateTime? StartDateTime { get; set; }
```

**View**: Split into date + time (24-hour)
```html
<input type="date" id="dtStartDate" />
<input type="time" id="dtStartTime" />  <!-- 24-hour format -->
```

**JavaScript**: Split on load, combine on submit
```javascript
// Split
$('#dtStartDate').val(date.toISOString().split('T')[0]);
$('#dtStartTime').val(hours + ':' + minutes);

// Combine
var combined = date + 'T' + time + ':00';
```

### DataTables Server-Side Processing

```javascript
$('#bargeEventTable').DataTable({
    processing: true,
    serverSide: true,
    stateSave: true,
    ajax: {
        url: '/BargeEventSearch/EventTable',
        type: 'POST',
        data: function(d) {
            d.eventTypeId = $('#EventTypeId').val();
            d.startDate = combineDateTime('dtStartDate', 'dtStartTime');
        }
    }
});
```

### Permission-Based Rendering

```html
@if (Model.CanModify)
{
    <button type="submit" class="btn btn-primary">Submit</button>
}
@if (Model.CanViewBilling)
{
    <li class="nav-item">Billing Tab</li>
}
```

---

## Reference Files

### Existing Implementations

**Facility** (Complete reference):
- DTOs: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\Dto\FacilityDto.cs`
- API: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\`
- Repositories: `src\Admin.Infrastructure\Repositories\FacilityRepository.cs`

**BoatLocation** (UI reference):
- ViewModels: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\ViewModels\BoatLocationSearchViewModel.cs`
- Controllers: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Controllers\BoatLocationSearchController.cs`
- Views: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\Views\BoatLocationSearch\`
- JavaScript: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\wwwroot\js\boatLocationSearch.js`

**Crewing** (Additional patterns):
- API: `C:\source\BargeOps.Crewing.API\`
- UI: `C:\source\BargeOps.Crewing.UI\`

---

## Support

### Documentation
- See `conversion-plan.md` for detailed implementation guide
- Refer to `.claude/tasks/MONO_SHARED_STRUCTURE.md` for architecture details

### Questions?
Contact the development team or refer to existing Facility/BoatLocation implementations in the mono repo.

---

## Summary

All Shared DTOs are ready! These DTOs are used by BOTH the API and UI projects - no duplication needed.

**Next**: Would you like me to generate the UI ViewModels? They will contain these Shared DTOs and add UI-specific properties (SelectLists, permission flags, etc.).
