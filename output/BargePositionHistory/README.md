# BargePositionHistory Conversion Templates

**Generated:** 2025-01-11
**Entity:** BargePositionHistory (FleetPositionHistory)
**Agent:** Template Generator

---

## 📋 Overview

This directory contains comprehensive conversion templates for migrating **BargePositionHistory** from VB.NET WinForms to ASP.NET Core with React-style patterns.

### Entity Description
Tracks historical positions of barges within fleet tier configurations, including when barges leave the fleet. Supports search by date, tier group, and barge number with tier coordinate (X,Y) tracking.

---

## 🎯 What's Included

### 1. Conversion Plan (`conversion-plan.md`)
Complete implementation guide including:
- Implementation order (Shared → API → UI)
- Data model with all properties
- Business rules and validation
- UI patterns and layouts
- Security & authorization
- Critical implementation notes
- Testing checklist

### 2. Shared DTOs ⭐ **CREATE FIRST!**
Location: `templates/shared/Dto/`
- `BargePositionHistoryDto.cs` - Complete entity DTO
- `BargePositionHistorySearchRequest.cs` - Search criteria DTO

### 3. API Layer Templates
Location: `templates/api/`

**Repositories:**
- `IBargePositionHistoryRepository.cs` - Repository interface
- `BargePositionHistoryRepository.cs` - Dapper implementation with direct SQL

**Services:**
- `IBargePositionHistoryService.cs` - Service interface
- `BargePositionHistoryService.cs` - Business logic implementation

**Controllers:**
- `BargePositionHistoryController.cs` - RESTful API endpoints

### 4. UI Layer Templates
Location: `templates/ui/`

**Services (API Clients):**
- `IBargePositionHistoryService.cs` - API client interface
- `BargePositionHistoryService.cs` - HTTP client implementation

**ViewModels:**
- `BargePositionHistorySearchViewModel.cs` - Search/list screen
- `BargePositionHistoryEditViewModel.cs` - Edit/create form

**Controllers:**
- `BargePositionHistoryController.cs` - MVC controller

**Views:**
- `Index.cshtml` - Search/list view
- `Edit.cshtml` - Edit/create form

**JavaScript:**
- `bargePositionHistory-search.js` - DataTables, search handlers
- `bargePositionHistory-detail.js` - DateTime split/combine, LeftFleet logic

---

## ⚠️ Critical Implementation Notes

### 1. DateTime Split/Combine Pattern
**CRITICAL:** All DateTime fields must use separate date and time inputs (24-hour format).

**On Load:**
```javascript
// Split PositionStartDateTime into date and time inputs
const date = new Date(dateTimeValue);
$('#dtPositionDate').val(date.toISOString().split('T')[0]);
const hours = ('0' + date.getHours()).slice(-2);
const minutes = ('0' + date.getMinutes()).slice(-2);
$('#dtPositionTime').val(hours + ':' + minutes);
```

**On Submit:**
```javascript
// Combine date and time into ISO 8601
const combined = dateValue + 'T' + timeValue + ':00';
$('#PositionStartDateTime').val(combined);
```

### 2. LeftFleet Conditional Logic
When `LeftFleet` checkbox is checked:
- Disable `TierID`, `TierX`, `TierY` fields
- Clear their values
- Server-side validation enforces this rule

```javascript
$('#LeftFleet').on('change', function() {
    const isChecked = $(this).is(':checked');
    $('#TierID, #TierX, #TierY').prop('disabled', isChecked);
    if (isChecked) {
        $('#TierID').val('').trigger('change');
        $('#TierX, #TierY').val('');
    }
});
```

### 3. MONO SHARED Architecture
⭐ **DTOs in BargeOps.Shared are the ONLY data models!**
- No separate Models folder
- DTOs used directly by both API and UI
- No AutoMapper needed between layers

---

## 📂 Target Deployment Locations

### Shared (BargeOps.Shared)
```
C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\BargeOps.Shared\Dto\
├── BargePositionHistoryDto.cs
└── BargePositionHistorySearchRequest.cs
```

### API (BargeOps.API)
```
C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\
├── src\Admin.Api\Controllers\BargePositionHistoryController.cs
├── src\Admin.Domain\Services\IBargePositionHistoryService.cs
├── src\Admin.Infrastructure\
│   ├── Repositories\
│   │   ├── IBargePositionHistoryRepository.cs
│   │   └── BargePositionHistoryRepository.cs
│   └── Services\BargePositionHistoryService.cs
```

### UI (BargeOps.UI)
```
C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\
├── Controllers\BargePositionHistoryController.cs
├── Services\
│   ├── IBargePositionHistoryService.cs
│   └── BargePositionHistoryService.cs
├── ViewModels\
│   ├── BargePositionHistorySearchViewModel.cs
│   └── BargePositionHistoryEditViewModel.cs
├── Views\BargePositionHistory\
│   ├── Index.cshtml
│   └── Edit.cshtml
└── wwwroot\js\
    ├── bargePositionHistory-search.js
    └── bargePositionHistory-detail.js
```

---

## 🔐 Security & Permissions

### Required Permissions
Add to `AuthPermissions` enum:
- `BargePositionHistoryView` - View records
- `BargePositionHistoryModify` - Create/update records
- `BargePositionHistoryDelete` - Delete records (hard delete)

### API Authentication
```csharp
[ApiKey] // API uses ApiKey authentication
[Authorize(Policy = "BargePositionHistoryView")]
```

### UI Authentication
```csharp
[Authorize] // UI uses OIDC/Cookie authentication
[RequirePermission<AuthPermissions>(AuthPermissions.BargePositionHistoryView, PermissionAccessType.ReadOnly)]
```

---

## ✅ Testing Checklist

- [ ] DateTime split/combine works correctly (24-hour format)
- [ ] LeftFleet checkbox disables/enables tier fields
- [ ] LeftFleet=true clears tier fields on save
- [ ] BargeNum lookup validation works
- [ ] Search requires Date and TierGroup
- [ ] Grid sorting, paging, filtering work
- [ ] Add/Modify/Delete operations work
- [ ] Optimistic concurrency with ModifyDateTime works
- [ ] Export functionality works
- [ ] Permissions properly restrict actions
- [ ] State save persists grid settings

---

## 📝 Business Rules

1. **PositionStartDateTime Required** - Must not be empty
2. **BargeNum Lookup** - Must match existing barge (via GetBargeIdByNumber)
3. **Tier Coordinates** - TierX/TierY must be Int16, validated against tier boundaries
4. **LeftFleet Logic** - When true, tier fields disabled/cleared
5. **Search Validation** - Date and TierGroup both required

---

## 🚀 Implementation Order

1. ✅ Create Shared DTOs (BargeOps.Shared)
2. ✅ Create API Infrastructure (Repositories, Services)
3. ✅ Create API Controller
4. ✅ Create UI Services (API clients)
5. ✅ Create UI ViewModels
6. ✅ Create UI Controller
7. ✅ Create Razor Views
8. ✅ Create JavaScript files
9. ⏳ Test thoroughly (DateTime and LeftFleet logic)
10. ⏳ Deploy and verify

---

## 📚 Reference Files

### Legacy (Onshore)
- `C:\Dev\BargeOps.Admin.Mono\OnShore\apps\Onshore\Forms\frmBargePositionHistory.vb`
- `C:\Dev\BargeOps.Admin.Mono\OnShore\apps\Onshore\Forms\frmBargePositionHistory.designer.vb`

### Modern Examples (Mono Repo)
- API: FacilityController.cs, BoatLocationController.cs
- UI: BoatLocationSearchController.cs
- DTOs: FacilityDto.cs, BoatLocationDto.cs
- JS: boatLocationSearch.js

---

## 🐛 Known Challenges

| Challenge | Solution |
|-----------|----------|
| DateTime Split/Combine | Use separate date + time inputs, combine on submit |
| LeftFleet Conditional | JavaScript toggles tier field state |
| BargeNum Validation | Remote API call for validation |
| Optimistic Concurrency | ModifyDateTime check in UPDATE query |
| State Persistence | DataTables stateSave with unique key |

---

## 📞 Support

For questions or issues:
1. Review `conversion-plan.md` for detailed guidance
2. Check reference implementations in mono repo
3. Review business logic in `business-logic.json`
4. Consult security mappings in `security.json`

---

*Generated by Claude Code Template Generator*
*Entity: BargePositionHistory*
*Date: 2025-01-11*
