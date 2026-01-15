# Vendor Entity Conversion Templates

## Overview

This directory contains the complete set of conversion templates for migrating the Vendor entity from the legacy VB.NET/WinForms application to the modern ASP.NET Core MVC architecture using the **MONO SHARED structure**.

**Generated**: 2025-12-15
**Entity**: Vendor
**Status**: Ready for implementation

---

## Directory Structure

```
output/Vendor/
├── conversion-plan.md           ← Comprehensive conversion plan (START HERE!)
├── README.md                     ← This file
├── GAP_ANALYSIS.md               ← Optional: gaps vs templates
├── templates/
│   ├── shared/                   ⭐ IMPLEMENT THESE FIRST!
│   │   └── Dto/
│   │       ├── VendorDto.cs                    ← Complete entity DTO with attributes
│   │       ├── VendorSearchRequest.cs          ← Search criteria DTO
│   │       ├── VendorContactDto.cs             ← Contact child entity DTO
│   │       └── VendorBusinessUnitDto.cs        ← Business unit child entity DTO
│   ├── api/                      API Project Templates
│   │   ├── Controllers/
│   │   │   └── VendorController.cs             ← API controller with [ApiKey]
│   │   ├── Repositories/
│   │   │   ├── IVendorRepository.cs            ← Repository interface
│   │   │   └── VendorRepository.cs             ← Dapper implementation with SQL
│   │   └── Services/
│   │       ├── IVendorService.cs               ← Service interface
│   │       └── VendorService.cs                ← Service implementation
│   └── ui/                       UI Project Templates
│       ├── ViewModels/
│       │   ├── VendorSearchViewModel.cs        ← Search screen ViewModel
│       │   └── VendorEditViewModel.cs          ← Edit screen ViewModel
│       ├── Controllers/                        (Templates to be generated)
│       ├── Services/                           (Templates to be generated)
│       ├── Views/                              (Templates to be generated)
│       └── wwwroot/                            (Templates to be generated)
└── *.json                       ← Analysis outputs (in this folder)
    ├── business-logic.json
    ├── data-access.json
    ├── form-structure-search.json
    ├── form-structure-detail.json
    ├── security.json
    ├── ui-mapping.json
    ├── workflow.json
    ├── tabs.json
    ├── validation.json
    └── related-entities.json
```

**Note:** You may also see `Vendor_*.json` files. Those are task-sync copies; prefer the non-prefixed `*.json` files listed above.

---

## Implementation Order

### ⭐ Phase 1: Shared DTOs (HIGHEST PRIORITY)
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\Dto\`

Create these DTOs FIRST before any other code:

1. **VendorDto.cs** - Complete entity with all properties
   - Primary key: VendorID
   - Required fields: Name, LongName
   - Address/contact info
   - Feature flags (Portal, BargeEx, etc.)
   - Child collections (Contacts, BusinessUnits)
   - [Sortable] and [Filterable] attributes for DataTables

2. **VendorSearchRequest.cs** - Search criteria
   - Name, AccountingCode filters
   - 7 boolean filters (ActiveOnly, FuelSuppliersOnly, etc.)

3. **VendorContactDto.cs** - Contact child entity
   - Contact information (Name, Phone, Email)
   - IsPrimary flag (only one can be primary)
   - Portal integration (PortalUserID)

4. **VendorBusinessUnitDto.cs** - Business unit child entity
   - Location information (River, Mile, Bank)
   - Fuel supplier settings
   - IsDefaultFuelSupplier (only one can be default)

### Phase 2: API Project
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\`

1. **Repository Layer**
   - `IVendorRepository.cs` → `src/Admin.Domain/Repositories/`
   - `VendorRepository.cs` → `src/Admin.Infrastructure/Repositories/`
   - Uses Dapper with **DIRECT SQL QUERIES** (NOT stored procedures)
   - Returns DTOs directly - NO mapping needed!

2. **Service Layer**
   - `IVendorService.cs` → `src/Admin.Domain/Services/`
   - `VendorService.cs` → `src/Admin.Infrastructure/Services/`
   - Business logic: Clear BargeEx fields when disabled
   - Validation: BargeEx conditional validation

3. **API Controller**
   - `VendorController.cs` → `src/Admin.Api/Controllers/`
   - **Authentication**: `[ApiKey]` attribute (NOT Windows Auth)
   - RESTful endpoints for CRUD + child entities

### Phase 3: UI Project
**Location**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\`

1. **ViewModels** ✅ GENERATED
   - `VendorSearchViewModel.cs` → `ViewModels/`
   - `VendorEditViewModel.cs` → `ViewModels/`

2. **API Client Services** (To be generated)
   - `IVendorService.cs` → `Services/`
   - `VendorService.cs` → `Services/`

3. **UI Controllers** (To be generated)
   - `VendorSearchController.cs` → `Controllers/`

4. **Razor Views** (To be generated)
   - `Index.cshtml` → `Views/VendorSearch/`
   - `Edit.cshtml` → `Views/VendorSearch/`

5. **JavaScript** (To be generated)
   - `vendor-search.js` → `wwwroot/js/`
   - `vendor-edit.js` → `wwwroot/js/`

---

## Key Features

### Shared DTOs
- **Single Source of Truth**: DTOs are used by BOTH API and UI
- **NO Separate Models**: DTOs ARE the domain models
- **NO AutoMapper**: Repositories return DTOs directly
- **DataTables Attributes**: [Sortable] and [Filterable] for server-side processing

### API Layer
- **Dapper SQL Queries**: Direct parameterized SQL, NOT stored procedures
- **API Key Auth**: Uses [ApiKey] attribute, NOT Windows Auth
- **RESTful Design**: Standard HTTP verbs (GET, POST, PUT, PATCH, DELETE)
- **Child Entity Endpoints**: Nested routes for contacts and business units

### UI Layer
- **MVVM Pattern**: ViewModels contain DTOs + lookup lists
- **NO ViewBag/ViewData**: All data on ViewModel
- **OIDC Auth**: Production uses Azure AD, development uses auto-sign-in
- **DataTables**: Server-side processing for search results
- **Conditional UI**: License/feature flags control visibility

---

## Special Considerations

### License-Based Features

#### Portal Features
- **License**: Portal
- **UI Elements**: Portal tab, EnablePortal checkbox
- **Check**: `PortalLicenseActive` on ViewModel

#### UnitTow Features
- **License**: UnitTow
- **UI Elements**: IsLiquidBroker, IsTankerman checkboxes
- **Check**: `UnitTowLicenseActive` on ViewModel

#### BargeEx Features
- **Global Setting**: EnableBargeExBargeLineSupport
- **UI Elements**: BargeEx tab, conditional fields
- **Check**: `BargeExGlobalSettingEnabled` on ViewModel

### Conditional Validation

**BargeEx Fields** (BargeExTradingPartnerNum, BargeExConfigID):
- Required ONLY when `IsBargeExEnabled = true`
- Automatically cleared when `IsBargeExEnabled = false`
- Implement in both server-side (Service) and client-side (JavaScript)

**Fuel Supplier Fields** (MinDiscountQty, MinDiscountFrequency):
- Automatically cleared when `IsFuelSupplier = false`
- `IsDefaultFuelSupplier` can only be true for ONE business unit per vendor

**Primary Contact**:
- Only ONE contact can have `IsPrimary = true` per vendor
- Automatically clear IsPrimary on other contacts when setting a new primary

---

## Validation Strategy

### Server-Side (API)
- FluentValidation for complex rules
- Data Annotations for simple rules
- Business logic in Service layer

### Client-Side (UI)
- jQuery Validate with unobtrusive validation
- Custom conditional rules for BargeEx
- Phone number masking (jQuery Mask Plugin)
- ZIP code format validation

---

## Security

### API
```csharp
[ApiKey]
[Route("api/[controller]")]
public class VendorController : ControllerBase { }
```

### UI
```csharp
[Authorize]
public class VendorSearchController : AppController
{
    [Authorize(Policy = "VendorModify")]
    public IActionResult Create() { }
}
```

### Permissions
- **VendorView**: Search, list, view details
- **VendorModify**: Create, update, activate/deactivate

---

## Reference Implementations

### Primary References (MONO SHARED)
- `FacilityDto.cs`: Complete DTO example
- `FacilityRepository.cs`: Dapper patterns
- `FacilityController.cs`: API controller patterns
- `BoatLocationSearchController.cs`: UI controller patterns
- `boatLocationSearch.js`: DataTables patterns

### Secondary References (Crewing)
- `CrewingDto.cs`: DTO examples
- `crewingSearch.js`: JavaScript patterns

---

## Next Steps

1. **Review** the conversion-plan.md document thoroughly
2. **Generate** remaining UI templates (Controllers, Services, Views, JS)
3. **Implement** Shared DTOs first (highest priority!)
4. **Test** each phase before proceeding to next
5. **Deploy** following the migration checklist

---

## Template Status

| Component | Status | Location |
|-----------|--------|----------|
| **Shared DTOs** | ✅ Complete | `templates/shared/Dto/` |
| **API Repository** | ✅ Complete | `templates/api/Repositories/` |
| **API Service** | ✅ Complete | `templates/api/Services/` |
| **API Controller** | ✅ Complete | `templates/api/Controllers/` |
| **UI ViewModels** | ✅ Complete | `templates/ui/ViewModels/` |
| **UI Controllers** | ⏳ To Generate | `templates/ui/Controllers/` |
| **UI Services** | ⏳ To Generate | `templates/ui/Services/` |
| **UI Views** | ⏳ To Generate | `templates/ui/Views/` |
| **JavaScript** | ⏳ To Generate | `templates/ui/wwwroot/js/` |

---

## Questions or Issues?

Refer to:
1. **conversion-plan.md** - Comprehensive implementation guide
2. **Analysis files** - Original extracted data
3. **Reference implementations** - Working examples in mono repo

---

**Happy Coding!** 🚀
