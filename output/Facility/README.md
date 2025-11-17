# Facility Conversion - Complete Package

## Overview

This package contains everything needed to convert the Facility management module from legacy VB.NET WinForms to modern ASP.NET Core architecture.

## Package Contents

### 📋 Planning Documents

1. **conversion-plan.md** - Master conversion plan
   - Executive summary and architecture
   - Complete data model and API design
   - UI design with layouts and component mappings
   - Security model and validation rules
   - Implementation steps (4 phases, 18 steps)
   - Database schema
   - Migration checklist

2. **TEMPLATE_GENERATION_SUMMARY.md** - Comprehensive implementation guide
   - Complete code patterns for all components
   - Repository, Service, Controller implementations
   - ViewModel and View templates
   - JavaScript implementations
   - Validation strategies
   - Testing checklist

3. **IMPLEMENTATION_STATUS.md** - Current implementation status
   - Completed items checklist
   - Pending items with code samples
   - Implementation priority
   - Testing checklist
   - Real-time progress tracker

### 📊 Analysis Files (output/Facility/)

4. **form-structure-search.json** - Search form analysis
   - Controls, dropdowns, grid columns
   - Event handlers and validation
   - Data access parameters

5. **form-structure-detail.json** - Detail form analysis
   - 4 tabs structure (Details, Status, Berths, NDC Data)
   - Controls by tab
   - Button definitions
   - Validation rules

6. **business-logic.json** - Business logic extraction
   - Stored procedures and parameters
   - Data formatting rules
   - Business rules and validation
   - Migration notes for Dapper

7. **data-access.json** - Data access patterns
   - All stored procedures documented
   - Parameter mappings
   - Result set mappings
   - Related procedures for child entities

8. **ui-mapping.json** - UI component mappings
   - Legacy to modern control mapping
   - DataTables configuration
   - Select2 dropdown setup
   - Bootstrap component replacements
   - Layout structures

9. **security.json** - Security model
   - Permissions (FacilityReadOnly, FacilityModify)
   - Button security mapping
   - Authorization attributes for API and MVC
   - Policy configuration

10. **tabs.json** - Tab structure analysis
    - Tab definitions and order
    - Controls per tab
    - Toolbars and grids
    - Shared controls

11. **validation.json** - Validation rules (21 rules)
    - Search form validation
    - Detail form validation
    - Business rules
    - Validation triggers
    - Modern validation strategies

12. **related-entities.json** - Entity relationships
    - FacilityBerth relationship
    - FacilityStatus relationship
    - CRUD methods per entity
    - Grid configurations

### 💻 Generated Code

#### API (BargeOps.Admin.API)

13. **Facility.cs** ✅ Created
    - Location: `C:\source\BargeOps\BargeOps.Admin.API\src\Admin.Domain\Models\`
    - Domain models: Facility, FacilityBerth, FacilityStatus
    - 40+ properties including Lock/Gauge and NDC fields

14. **FacilityDto.cs** ⚠️ Exists but needs expansion
    - Location: `C:\source\BargeOps\BargeOps.Admin.API\src\Admin.Domain\Dto\`
    - Current: Basic DTO with Csg.ListQuery annotations
    - Needed: Add FacilityListDto, FacilitySearchRequest, FacilityBerthDto, FacilityStatusDto

#### API Code Templates (in IMPLEMENTATION_STATUS.md)

15. **IFacilityRepository.cs** - Interface code provided
16. **FacilityRepository.cs** - Pattern and guidance provided
17. **IFacilityService.cs** - Interface code provided
18. **FacilityService.cs** - Implementation with business rules provided
19. **FacilityMappingProfile.cs** - Complete AutoMapper configuration provided
20. **FacilityController.cs** - Complete controller with all endpoints provided

#### UI Code Templates (in TEMPLATE_GENERATION_SUMMARY.md)

21. **ViewModels** - Complete code for 4 ViewModels
22. **UI Service** - Interface and implementation patterns
23. **MVC Controller** - Complete FacilitySearchController code
24. **Razor Views** - Complete Index.cshtml and Edit.cshtml templates
25. **JavaScript** - Complete facilitySearch.js and facilityDetail.js

## Quick Start Guide

### Step 1: Review Planning Documents (30 minutes)
1. Read `conversion-plan.md` for overall strategy
2. Review `IMPLEMENTATION_STATUS.md` for current state
3. Scan `TEMPLATE_GENERATION_SUMMARY.md` for code examples

### Step 2: Implement API Layer (6-8 hours)

Follow this exact order:

1. **Expand FacilityDto.cs**
   - Add FacilityListDto, FacilitySearchRequest, child DTOs
   - Add DataTableRequest and DataTableResponse classes

2. **Create IFacilityRepository.cs**
   - Copy interface from IMPLEMENTATION_STATUS.md
   - Place in: `Admin.Domain/Interfaces/`

3. **Create FacilityRepository.cs**
   - Follow pattern from BoatLocationRepository.cs
   - Use code guidance from IMPLEMENTATION_STATUS.md
   - Implement all 14+ methods
   - Place in: `Admin.Infrastructure/Repositories/`

4. **Create IFacilityService.cs**
   - Copy interface from IMPLEMENTATION_STATUS.md
   - Place in: `Admin.Domain/Interfaces/`

5. **Create FacilityService.cs**
   - Use business logic from IMPLEMENTATION_STATUS.md
   - Implement conditional Lock/Gauge field clearing
   - Place in: `Admin.Domain/Services/`

6. **Create FacilityMappingProfile.cs**
   - Copy complete code from IMPLEMENTATION_STATUS.md
   - Place in: `Admin.Infrastructure/Mappings/`

7. **Create FacilityController.cs**
   - Copy complete controller from IMPLEMENTATION_STATUS.md
   - Place in: `Admin.Api/Controllers/`

8. **Register Dependencies**
   - Add to Startup.cs or Program.cs
   - Register repository and service
   - Configure authorization policies

### Step 3: Implement UI Layer (8-10 hours)

1. **Create ViewModels** (Models/)
   - FacilitySearchViewModel.cs
   - FacilityEditViewModel.cs
   - FacilityBerthViewModel.cs
   - FacilityStatusViewModel.cs
   - Use code from TEMPLATE_GENERATION_SUMMARY.md

2. **Create UI Service** (Services/)
   - IFacilityService.cs
   - FacilityService.cs (HttpClient calls to API)

3. **Create MVC Controller** (Controllers/)
   - FacilitySearchController.cs
   - Copy from TEMPLATE_GENERATION_SUMMARY.md

4. **Create Razor Views** (Views/FacilitySearch/)
   - Index.cshtml (search page)
   - Edit.cshtml (detail with 4 tabs)
   - _StatusModal.cshtml
   - _BerthModal.cshtml
   - Use complete templates from TEMPLATE_GENERATION_SUMMARY.md

5. **Create JavaScript** (wwwroot/js/)
   - facilitySearch.js (DataTables, Select2)
   - facilityDetail.js (tabs, modals, validation)
   - Use complete code from TEMPLATE_GENERATION_SUMMARY.md

### Step 4: Testing (4-6 hours)

Use checklists in IMPLEMENTATION_STATUS.md:
- API endpoint testing (16 tests)
- UI functionality testing (15 tests)
- Security testing
- Validation testing

## Key Files Reference

### For API Development:
- **Primary Pattern**: `C:\source\BargeOps\BargeOps.Admin.API\src\Admin.Api\Controllers\BoatLocationController.cs`
- **Repository Pattern**: `C:\source\BargeOps\BargeOps.Admin.API\src\Admin.Infrastructure\Repositories\BoatLocationRepository.cs`
- **Business Rules**: `output/Facility/business-logic.json`
- **Validation Rules**: `output/Facility/validation.json`

### For UI Development:
- **Primary Pattern**: `C:\source\BargeOps\BargeOps.Admin.UI\Controllers\BoatLocationSearchController.cs`
- **DataTables Example**: `C:\source\BargeOps\Crewing.UI\wwwroot\js\crewingSearch.js`
- **UI Mapping**: `output/Facility/ui-mapping.json`
- **Tab Structure**: `output/Facility/tabs.json`

## Important Business Rules to Implement

1. **Conditional Lock/Gauge Fields**
   - Show/hide based on Facility Type selection
   - Clear values when type changes (client and server)

2. **Validation Rules**
   - River required when Mile specified
   - EndMile >= StartMile
   - Name required (max 100 chars)
   - 18 additional validation rules documented

3. **Parent-Child Relationships**
   - Must save Facility before adding Berths or Statuses
   - Disable tabs until parent saved

4. **Security**
   - FacilityReadOnly: View/search only
   - FacilityModify: Full CRUD access
   - Apply at API, Controller, and View levels

## Technology Stack

### API
- ASP.NET Core 6.0+
- Dapper (data access)
- AutoMapper (object mapping)
- Windows Authentication
- CSG Authorization Library

### UI
- ASP.NET Core MVC 6.0+
- Bootstrap 5.3+
- jQuery 3.6+
- DataTables 1.13+
- Select2 4.1+
- Moment.js 2.29+

## Stored Procedures Required

All these should already exist in the database:
- sp_FacilityLocationSearch
- sp_FacilityLocation_GetByID
- sp_FacilityLocation_Insert
- sp_FacilityLocation_Update
- sp_FacilityLocation_Delete
- sp_FacilityBerth_* (GetByFacilityID, Insert, Update, Delete)
- sp_FacilityStatus_* (GetByFacilityID, Insert, Update, Delete)
- sp_River_GetAll
- sp_BargeExLocationType_GetAll

## Success Criteria

### API Complete When:
- [x] Domain models created
- [ ] DTOs expanded with all required types
- [ ] Repository implemented with all 14+ methods
- [ ] Service implemented with business rules
- [ ] AutoMapper profile created
- [ ] Controller created with all endpoints
- [ ] Dependencies registered
- [ ] All API endpoints tested
- [ ] Authorization working correctly

### UI Complete When:
- [ ] All 4 ViewModels created
- [ ] UI service implemented
- [ ] MVC controller implemented
- [ ] All 4 views created
- [ ] JavaScript files created
- [ ] Search functionality working
- [ ] CRUD operations working
- [ ] Tabs working correctly
- [ ] Conditional Lock/Gauge panel working
- [ ] Berth/Status modals working
- [ ] Validation working (client and server)
- [ ] Security/permissions working

## Troubleshooting

### Common Issues:

**Issue**: Lock/Gauge fields not clearing
**Solution**: Check both JavaScript (toggleLockGaugePanel) and service layer (UpdateAsync)

**Issue**: DataTables not loading
**Solution**: Verify Search endpoint returns correct DataTableResponse format with Draw, RecordsTotal, RecordsFiltered, Data

**Issue**: River required validation not firing
**Solution**: Check custom jQuery validation rule is registered and FluentValidation rule implemented

**Issue**: Tabs not disabled for new facility
**Solution**: Check LocationId == 0 condition in Edit.cshtml and JavaScript

## Estimated Timeline

- **API Development**: 6-8 hours (1 day)
- **UI Development**: 8-10 hours (1-1.5 days)
- **Testing**: 4-6 hours (0.5-1 day)
- **Total**: 18-24 hours (2.5-3 days)

Assumes:
- Developer familiar with ASP.NET Core
- All stored procedures already exist
- Database permissions configured
- Development environment ready

## Support

### Documentation:
- All analysis files in `output/Facility/`
- conversion-plan.md (master plan)
- TEMPLATE_GENERATION_SUMMARY.md (code examples)
- IMPLEMENTATION_STATUS.md (progress tracker)

### Code Patterns:
- BoatLocation (primary reference)
- Crewing examples (DataTables/Select2)

### Questions?
- Review analysis JSON files for business rules
- Check validation.json for validation requirements
- Check security.json for permission requirements
- Reference existing BoatLocation implementation

---

## Getting Started NOW

**Right Now**: Open IMPLEMENTATION_STATUS.md and start with "Immediate Next Steps" section.

**Next**: Follow Step 2 (Implement API Layer) from Quick Start Guide above.

**Then**: Move to Step 3 (Implement UI Layer).

**Finally**: Complete Step 4 (Testing).

---

**Package Created**: 2025-11-11
**Status**: Ready for Implementation
**Estimated Completion**: 2.5-3 days for experienced developer

Good luck with the implementation! All the tools and documentation you need are in this package.
