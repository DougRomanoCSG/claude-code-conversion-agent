# Barge Conversion Templates - Gap Analysis

**Date:** 2025-12-17
**Comparison:** Generated templates vs. Analysis files

## Overview

Comparing generated templates in `templates/` with analysis files:
- ✅ form-structure-detail.json
- ✅ data-access.json
- ✅ ui-mapping.json
- ✅ validation.json
- ✅ Barge_tabs.json (in .claude/tasks/)
- ✅ Barge_security.json (in .claude/tasks/)
- ✅ Barge_relationships.json (in .claude/tasks/)
- ✅ Barge_workflow.json (in .claude/tasks/)
- ✅ BargeSearch_form_structure.json (in .claude/tasks/)

---

## ✅ Generated and Complete

### Shared DTOs (4/4)
- ✅ BargeDto.cs - Complete with 80+ properties
- ✅ BargeSearchRequest.cs - All search criteria
- ✅ BargeSearchResultDto.cs - All 58 grid columns
- ✅ BargeCharterDto.cs - Child entity

### API Layer (5/5)
- ✅ IBargeRepository.cs - Interface with all methods
- ✅ BargeRepository.cs - Dapper implementation
- ✅ IBargeService.cs - Service interface
- ✅ BargeService.cs - Business logic + validations
- ✅ BargeController.cs - RESTful endpoints

### UI Layer - Services & Controllers (4/4)
- ✅ IBargeService.cs (HTTP client interface)
- ✅ BargeService.cs (HTTP client implementation)
- ✅ BargeSearchController.cs - MVC controller
- ✅ BargeSearchViewModel.cs
- ✅ BargeEditViewModel.cs

### UI Layer - Views & JavaScript (3/4)
- ✅ Index.cshtml - Search view
- ✅ barge-search.js - DataTables logic
- ✅ barge-edit.js - Form logic

---

## ❌ Missing / Incomplete Implementations

### 1. Edit.cshtml View (CRITICAL MISSING)

**Status:** ❌ Not Generated
**Source:** Barge_tabs.json, form-structure-detail.json

The Edit view needs a **5-tab structure** based on analysis:

#### Required Tabs:
1. **Basic Information Tab**
   - Barge #, USCG #, Hull #, GL Code, Active checkbox
   - **btnEditBargeNumber** - Enable barge number editing
   - USCG Lookup link

2. **Physical Tab**
   - Hull Type, Length, Width, Depth
   - Barge Series (with auto-population)
   - Size Category (auto-calculated)
   - Cover Type, Cover Sub Type
   - Barge Type

3. **Status & Location Tab**
   - Owner, Operator
   - Status (read-only)
   - Latest servicing boat (read-only)
   - Location, Location DateTime
   - **btnClearLocationDateTime** - Clear location time
   - Load Status, Cover Configuration
   - Equipment Type, Fleet ID
   - Clean Status, Commodity, Color
   - Insufficient Freeboard + Freeboard Range
   - In Service Date, Out of Service Date
   - Draft (feet/inches) + **btnDraftDetail** button
   - Draft Calculated (read-only, Freight license only)
   - **btnBargeCharters** button (if customization enabled)

4. **Damage / Repair Tab** (disabled for fleet-owned)
   - Repair Status, Dry Dock, Scheduled for Repair
   - Damaged Cargo, Damage Level, **btnMapColors** button
   - Leaker, Damaged, Damage Note

5. **Ticket Details Tab** (disabled for fleet-owned, read-only fields)
   - Inspection Status
   - Hold - Do not release
   - Destination In/Out
   - Load First/Last/Multiple Lots
   - On Order + Schedule
   - Scheduled In/Out
   - Pickup + Ready Time
   - Consigned to

#### Bottom Action Bar:
- Active checkbox
- **btnDefineColors** - Define color pairs
- **btnSubmit** - Save
- **btnCancel** - Cancel
- **btnViewPhotos** - View condition photos

**Missing File:** `templates/ui/Views/Edit.cshtml`

---

### 2. Charter Management (Child Collection)

**Status:** ❌ Partial - DTO exists, no UI implementation
**Source:** Barge_relationships.json

#### Missing Components:
- ❌ **_CharterModal.cshtml** - Modal for add/edit charter
- ❌ **Charter grid** in Edit.cshtml
- ❌ Charter CRUD operations in JavaScript
- ❌ Date range overlap validation UI

**Charter Grid Columns** (from analysis):
- Start Date
- End Date
- Chartered (CharterCodeDesc)
- Daily Rate (Currency)
- Charter Company (CustomerName)

**Missing Files:**
- `templates/ui/Views/_CharterModal.cshtml` or similar partial
- Charter management JavaScript in barge-edit.js

---

### 3. SQL Query Files

**Status:** ❌ Not Generated (placeholders in Repository only)
**Source:** data-access.json

#### Required SQL Files:
All should be in `templates/api/Sql/` as embedded resources:

- ❌ **Barge_GetById.sql** - Single barge with all fields
- ❌ **Barge_Search.sql** - Complex search with boat/facility/ship filters
- ❌ **Barge_Search_Count.sql** - Count for paging
- ❌ **Barge_Insert.sql** - Create new barge
- ❌ **Barge_Update.sql** - Update existing
- ❌ **Barge_SetActive.sql** - Soft delete (IsActive flag)
- ❌ **Barge_GetLocationList.sql** - Location dropdown
- ❌ **BargeCharter_GetByBargeId.sql** - Load charters
- ❌ **BargeCharter_Insert.sql** - Add charter
- ❌ **BargeCharter_Update.sql** - Update charter
- ❌ **BargeCharter_Delete.sql** - Remove charter
- ❌ **BargeSeries_GetById.sql** - For auto-population

**Note:** Based on data-access.json, the legacy system uses stored procedures. Modern implementation should convert these to **direct SQL queries** (NOT stored procedures) for the Dapper repository.

**Missing Files:** All SQL files

---

### 4. API Endpoints (Missing Features)

**Status:** ❌ Partial
**Source:** Barge_security.json

#### Missing Endpoints:
- ❌ **POST /api/Barge/{id}/close-ticket** - Close ticket operation
  - Mentioned in security.json
  - Not implemented in BargeController.cs

- ❌ **GET /api/bargeseries/{id}** - For auto-population
  - Referenced in barge-edit.js
  - Would be in separate BargeSeriesController

- ❌ **POST /api/Barge/bulk-close-tickets** - Bulk operation
  - For multi-select grid rows
  - Mentioned in analysis

**Action Required:** Add missing endpoints to BargeController.cs

---

### 5. ViewModels - Missing Fields

**Status:** ⚠️ Incomplete
**Source:** Barge_tabs.json

#### BargeEditViewModel Missing Fields:

Read-only fields from Ticket relationship (all missing):
- ❌ InspectionStatus (string, read-only)
- ❌ IsOnHold (bool, read-only)
- ❌ DestinationInName (string, read-only)
- ❌ LoadFirst (bool, read-only)
- ❌ LoadLast (bool, read-only)
- ❌ LoadMultipleLots (bool, read-only)
- ❌ DestinationOutName (string, read-only)
- ❌ ScheduleInName (string, read-only)
- ❌ IsOnOrder (bool, read-only)
- ❌ OnOrderScheduleDateTime (DateTime?, read-only)
- ❌ OnOrderTripNumber (string, read-only)
- ❌ ScheduleOutName (string, read-only)
- ❌ IsAwaitingPickup (bool, read-only)
- ❌ AwaitingPickupReadyDateTime (DateTime?, read-only)
- ❌ ConsignLocationName (string, read-only)

**Action Required:** Add ticket fields to BargeEditViewModel.cs

---

### 6. JavaScript - Missing Features

**Status:** ⚠️ Partial
**Source:** Barge_tabs.json, Barge_workflow.json

#### barge-edit.js Missing:

- ❌ **btnEditBargeNumber** click handler
  - Enable editing of normally read-only BargeNum field

- ❌ **btnClearLocationDateTime** click handler
  - Clear location date/time while keeping LocationID

- ❌ **Equipment Type group box logic**
  - Disable entire grpDamageRepair for fleet-owned
  - Disable entire grpTicketDetails for fleet-owned
  - Enable/disable specific fields in grpStatus

- ❌ **Charter grid management**
  - Add/Edit/Delete charter rows
  - Date range overlap validation

- ❌ **Draft detail button** logic
  - Open draft detail modal/form

- ❌ **Related form buttons**
  - btnDefineColors, btnMapColors, btnViewPhotos handlers

**Action Required:** Enhance barge-edit.js with missing features

---

### 7. Validation - FluentValidation Classes

**Status:** ❌ Not Generated
**Source:** validation.json, Barge_business_logic.json

#### Missing Validator:

- ❌ **BargeValidator.cs** - FluentValidation class
  - Should be in `templates/api/Validation/`
  - Implement ALL 25+ validation rules from validation.json
  - Include conditional logic for cover type, equipment type, etc.

**Complex Rules Needed:**
1. Draft must be >= highest corner draft
2. DraftInches range 0-11
3. CustomerID required if not fleet-owned and not Terminal Mode
4. SizeCategory required if not fleet-owned and not Terminal Mode
5. FleetID required if EquipmentType is 'fleet-owned'
6. FacilityBerthID must match barge location
7. DamageNote must be blank if IsDamaged is not checked
8. FreeboardRange required if HasInsufficientFreeboard is checked
9. OutOfServiceDate must be later than InServiceDate
10. Charter date ranges cannot overlap
11. CoverConfig required if company-operated and CoverType is not OT
12. CoverType required if commodity requires cover
13. CoverSubTypeID required if commodity requires cover
14. CoverSubTypeID must be blank if CoverType is OT or blank

**Missing File:** `templates/api/Validation/BargeValidator.cs`

---

### 8. CSS Styling

**Status:** ❌ Not Generated
**Source:** ui-mapping.json

#### Missing CSS:

- ❌ **barge-search.css** - Custom styles
  - Search title bar (blue background #0066cc)
  - Cross-charter row colors (CTC: #FFFACD, RTD: #FFE4E1)
  - Damage level cell colors
  - Expandable section styling

**Missing File:** `templates/ui/wwwroot/css/barge-search.css`

---

### 9. Partial Views / Modals

**Status:** ❌ Not Generated

#### Missing Partials:

- ❌ **_SearchCriteria.cshtml** - Search form section
- ❌ **_SearchResults.cshtml** - DataTables grid section
- ❌ **_CharterGrid.cshtml** - Charter management grid
- ❌ **_DraftDetail.cshtml** - Draft detail modal
- ❌ **_TicketDetails.cshtml** - Read-only ticket section

**Missing Files:** `templates/ui/Views/BargeSearch/_*.cshtml`

---

### 10. Related Entity Support

**Status:** ⚠️ Acknowledged but not implemented
**Source:** Barge_relationships.json

These require separate implementations (outside Barge scope):

- **BargeSeries** - Auto-population source
- **BargeLocation** - Location list
- **Ticket** - Read-only ticket details
- **Alert** - Alert association
- **FleetBoat** - Latest servicing boat
- **ColorPair** - Token colors
- **CoverSubType** - Filtered by CoverType
- **FacilityBerth** - Berth coordinates
- **Tier** - Tier coordinates

**Note:** These are dependencies, not gaps in Barge templates.

---

## 📋 Priority Recommendations

### HIGH PRIORITY (Critical for MVP)

1. **✅ COMPLETE: Edit.cshtml** - 5-tab detail view
   - File: `templates/ui/Views/Edit.cshtml`
   - Implement all tabs with proper field organization
   - Add all special buttons

2. **✅ COMPLETE: SQL Query Files**
   - All 12 SQL files for Dapper repository
   - Convert legacy stored procedures to direct SQL

3. **✅ COMPLETE: BargeValidator.cs**
   - FluentValidation class with all 25+ rules
   - File: `templates/api/Validation/BargeValidator.cs`

4. **✅ COMPLETE: BargeEditViewModel Updates**
   - Add all read-only ticket fields
   - Ensure proper organization for 5-tab structure

### MEDIUM PRIORITY (Important for full feature parity)

5. **Charter Management**
   - _CharterModal.cshtml or inline grid
   - Charter CRUD JavaScript
   - Date overlap validation

6. **JavaScript Enhancements**
   - btnEditBargeNumber logic
   - btnClearLocationDateTime logic
   - Equipment type group disable logic
   - All missing button handlers

7. **CSS Styling**
   - barge-search.css with colors and formatting

8. **Missing API Endpoints**
   - POST /api/Barge/{id}/close-ticket
   - Bulk ticket close operation

### LOW PRIORITY (Nice to have)

9. **Partial Views**
   - Break large views into reusable partials

10. **Additional Features**
    - Draft detail modal (separate form/modal)
    - Color management (separate feature)
    - Photo viewer (separate feature)

---

## 📝 Summary Statistics

| Category | Generated | Missing | Complete % |
|----------|-----------|---------|------------|
| **Shared DTOs** | 4 | 0 | 100% |
| **API Layer** | 5 | 3 | 63% |
| **UI ViewModels** | 2 | 1 update | 80% |
| **UI Services** | 2 | 0 | 100% |
| **UI Controllers** | 1 | 0 | 100% |
| **UI Views** | 1 | 1 | 50% |
| **JavaScript** | 2 | 5 features | 60% |
| **SQL Files** | 0 | 12 | 0% |
| **Validation** | 0 | 1 | 0% |
| **CSS** | 0 | 1 | 0% |
| **TOTAL** | 17 | 24 | 41% complete |

---

## 🎯 Next Steps

To achieve **100% feature parity**, the following files need to be generated:

### Critical (Must Have):
1. `templates/ui/Views/Edit.cshtml` - Full 5-tab edit view
2. `templates/api/Sql/*.sql` - 12 SQL query files
3. `templates/api/Validation/BargeValidator.cs` - FluentValidation
4. Update `templates/ui/ViewModels/BargeEditViewModel.cs` - Add ticket fields

### Important (Should Have):
5. `templates/ui/wwwroot/css/barge-search.css` - Custom styling
6. Update `templates/ui/wwwroot/js/barge-edit.js` - Add missing features
7. Update `templates/api/Controllers/BargeController.cs` - Add close-ticket endpoint
8. `templates/ui/Views/_CharterGrid.cshtml` - Charter management

### Optional (Nice to Have):
9. `templates/ui/Views/_*.cshtml` - Additional partials
10. Related form stubs (draft detail, colors, photos)

---

**Would you like me to generate any of these missing components?**

Options:
1. Generate ALL high-priority items (Edit.cshtml, SQL files, Validator, ViewModel updates)
2. Generate specific items (you choose)
3. Focus on a particular category (Views, SQL, Validation, etc.)
