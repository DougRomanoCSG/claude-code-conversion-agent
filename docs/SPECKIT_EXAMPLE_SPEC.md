# Example SpecKit Spec for Vendor Entity

This is an example of what a SpecKit spec would look like for the Vendor entity conversion, generated from analysis data.

## File Location
`specs/Vendor/spec.md`

---

# Vendor Module Conversion Specification

## Overview

Convert legacy VB.NET Vendor search and detail forms (`frmVendorSearch`, `frmVendorDetail`) to modern ASP.NET Core MVC architecture following BargeOps.Admin.Mono patterns.

**Source Forms:**
- `frmVendorSearch.vb` - Search/list panel with DataTables grid
- `frmVendorDetail.vb` - Detail form with 4 tabs

**Target Architecture:**
- **BargeOps.Shared**: DTOs (create first!)
- **BargeOps.API**: Repository, Service, Controller
- **BargeOps.UI**: ViewModels, MVC Controller, Razor Views, JavaScript

## Objectives

1. Preserve all business logic from legacy VB.NET forms
2. Modernize UI using Bootstrap 5 and DataTables
3. Implement proper separation of concerns (Shared → API → UI)
4. Maintain security and authorization patterns
5. Support all existing functionality

## Functional Requirements

### FR1: Vendor Search
- **As an** admin user
- **I want to** search for vendors by name, code, or status
- **So that** I can quickly find vendors in the system

**Acceptance Criteria:**
- Search form displays DataTables grid
- Server-side pagination and sorting
- Filter by vendor name (partial match)
- Filter by vendor code (exact match)
- Filter by active/inactive status
- Results display: Vendor Code, Name, Status, Last Modified

### FR2: Vendor Detail View
- **As an** admin user
- **I want to** view complete vendor information
- **So that** I can see all vendor details in one place

**Acceptance Criteria:**
- Detail view opens from search results
- Displays 4 tabs: Details, Portal, Business Units, BargeEx Settings
- All tabs load data correctly
- Read-only view for non-editable fields

### FR3: Vendor Details Tab
- **As an** admin user
- **I want to** edit vendor master information and contacts
- **So that** I can maintain vendor data

**Acceptance Criteria:**
- Edit vendor name, code, address, phone, email
- Inline editing of vendor contacts
- Add/remove contacts
- Validation: vendor code must be unique
- Save changes to database

### FR4: Portal Tab
- **As an** admin user
- **I want to** manage vendor portal groups
- **So that** I can configure portal access

**Acceptance Criteria:**
- Display list of portal groups for vendor
- Link to portal group configuration (frmVendorPortalGroup)
- Add/remove portal groups

### FR5: Vendor Business Units Tab
- **As an** admin user
- **I want to** manage vendor business units
- **So that** I can track multiple business units per vendor

**Acceptance Criteria:**
- Display list of business units
- Inline editing of business units
- Add/remove business units
- Each unit has: Name, Code, Active status

### FR6: BargeEx Settings Tab
- **As an** admin user
- **I want to** configure EDI settings for vendor
- **So that** BargeEx integration works correctly

**Acceptance Criteria:**
- Display EDI configuration
- Edit EDI settings
- Save configuration

## Business Rules

[Extracted from `business-logic.json`]

### BR1: Vendor Code Uniqueness
- Vendor codes must be unique across all vendors
- Validation occurs on save
- Error message: "Vendor code already exists"

### BR2: Active Vendor Protection
- Active vendors cannot be deleted
- Must set status to inactive first
- Inactive vendors can be deleted

### BR3: Contact Requirements
- Vendor must have at least one contact
- Primary contact must have email address
- Contact email must be valid format

### BR4: Portal Group Assignment
- Vendor can have multiple portal groups
- Portal groups must exist before assignment
- Cannot remove portal group if vendor has active transactions

## UI Requirements

[Extracted from `form-structure-search.json` and `form-structure-detail.json`]

### UI1: Search Form Layout
- **Header**: Page title "Vendor Search"
- **Filters Panel**: 
  - Vendor Name (text input)
  - Vendor Code (text input)
  - Status (dropdown: All, Active, Inactive)
  - Search button
- **Results Grid**: DataTables with columns:
  - Vendor Code (sortable, filterable)
  - Vendor Name (sortable, filterable)
  - Status (sortable, filterable)
  - Last Modified (sortable)
  - Actions (View, Edit buttons)

### UI2: Detail Form Layout
- **Header**: Vendor name and code
- **Tabs**: Bootstrap nav tabs
  - Tab 1: Details
  - Tab 2: Portal
  - Tab 3: Business Units
  - Tab 4: BargeEx Settings
- **Footer**: Save, Cancel buttons

### UI3: Details Tab
- **Vendor Master Section**:
  - Vendor Code (read-only if existing)
  - Vendor Name (required)
  - Address fields
  - Phone, Email
- **Contacts Section**:
  - DataTables grid for contacts
  - Inline editing enabled
  - Add/Remove buttons

### UI4: Component Mappings
[From `ui-mapping.json`]
- `UltraGrid` → DataTables (server-side)
- `UltraCombo` → Select2 dropdown
- `UltraPanel` → Bootstrap Card
- `UltraTabControl` → Bootstrap Nav Tabs
- `TextBox` → Bootstrap Form Input

## Data Access Requirements

[Extracted from `data-access.json`]

### DA1: Stored Procedures
- `spVendorLocationSearch` - Search vendors with filters
- `spVendorLocationGet` - Get single vendor by ID
- `spVendorLocationSave` - Insert/update vendor
- `spVendorLocationDelete` - Delete vendor (if inactive)
- `spVendorContactGet` - Get contacts for vendor
- `spVendorContactSave` - Save contact
- `spVendorBusinessUnitGet` - Get business units
- `spVendorBusinessUnitSave` - Save business unit

### DA2: Repository Pattern
- Use Dapper for data access
- SQL files as embedded resources
- Return DTOs directly (no domain models)
- Repository interface in BargeOps.Shared.Interfaces
- Implementation in BargeOps.API

## Security Requirements

[Extracted from `security.json`]

### SEC1: Authorization
- SubSystem: "Vendor"
- Required permissions:
  - View: `VendorView`
  - Edit: `VendorEdit`
  - Delete: `VendorDelete`
- Button security based on permissions
- Hide/disable buttons based on user permissions

### SEC2: Button Types
- `ButtonType.Add` → Requires `VendorEdit`
- `ButtonType.Edit` → Requires `VendorEdit`
- `ButtonType.Delete` → Requires `VendorDelete`
- `ButtonType.View` → Requires `VendorView`

## Validation Requirements

[Extracted from `validation.json`]

### VAL1: Form Validation
- Vendor Name: Required, max 100 characters
- Vendor Code: Required, max 20 characters, alphanumeric
- Email: Valid email format (if provided)
- Phone: Valid phone format (if provided)

### VAL2: Business Rule Validation
- Check vendor code uniqueness on save
- Validate contact requirements
- Check active vendor deletion rules

## Related Entities

[Extracted from `related-entities.json`]

### RE1: Vendor Contacts
- One-to-many relationship
- Inline editing in Details tab
- CRUD operations: Create, Read, Update, Delete

### RE2: Vendor Business Units
- One-to-many relationship
- Inline editing in Business Units tab
- CRUD operations: Create, Read, Update, Delete

### RE3: Vendor Portal Groups
- Many-to-many relationship
- Managed through Portal tab
- Links to `frmVendorPortalGroup` form

## Workflow Requirements

[Extracted from `workflow.json`]

### WF1: Search Workflow
1. User opens Vendor Search page
2. Applies filters (optional)
3. Clicks Search button
4. DataTables loads results via AJAX
5. User clicks View/Edit on row
6. Detail form opens

### WF2: Detail Workflow
1. User opens detail form
2. Details tab loads by default
3. User can switch tabs
4. User edits data
5. User clicks Save
6. Validation runs
7. If valid, save to database
8. Success message displayed
9. Form refreshes with updated data

## Technical Constraints

1. **Target Framework**: ASP.NET Core 8
2. **Database**: SQL Server (existing stored procedures)
3. **UI Framework**: Bootstrap 5, DataTables, Select2
4. **Architecture**: Repository pattern with Dapper
5. **DTOs**: Must be in BargeOps.Shared (create first!)
6. **Namespaces**: 
   - DTOs: `BargeOps.Shared.Dto`
   - API: `BargeOps.API.Controllers`, `BargeOps.API.Services`
   - UI: `BargeOps.UI.Controllers`, `BargeOps.UI.ViewModels`

## Success Criteria

1. ✅ All business logic preserved
2. ✅ All UI functionality working
3. ✅ Security and authorization implemented
4. ✅ Validation rules enforced
5. ✅ Code follows BargeOps.Admin.Mono patterns
6. ✅ DTOs in BargeOps.Shared (created first)
7. ✅ Repository pattern with Dapper
8. ✅ Service layer for business logic
9. ✅ MVC controllers inherit AppController
10. ✅ ViewModels use MVVM (no ViewBag/ViewData)
11. ✅ Bootstrap 5 UI components
12. ✅ DataTables server-side pagination
13. ✅ All tests passing

## Out of Scope

- Portal group configuration (separate form: `frmVendorPortalGroup`)
- Historical data migration
- Performance optimization (can be done later)
- Advanced search features (can be added later)

## Dependencies

- BargeOps.Shared project (must exist)
- BargeOps.API project
- BargeOps.UI project
- Existing stored procedures in database
- Reference examples: BoatLocation, Facility

## References

- Analysis files: `output/Vendor/*.json`
- Reference API: `src/Admin.Api/Controllers/BoatLocationController.cs`
- Reference UI: `Controllers/BoatLocationSearchController.cs`
- Reference DTOs: `src/BargeOps.Shared/BargeOps.Shared/Dto/BoatLocationDto.cs`

---

**Next Steps:**
1. Review this spec
2. Use `/speckit.plan` to create technical plan
3. Use `/speckit.tasks` to break down implementation
4. Generate templates using existing generators
