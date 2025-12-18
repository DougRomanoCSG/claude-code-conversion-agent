# Vendor Entity Conversion - Implementation Status

## Executive Summary

**Entity**: Vendor
**Status**: Planning Phase
**Complexity**: HIGH
**Estimated Effort**: 8-10 days
**Dependencies**: VendorContact, VendorBusinessUnit, VendorPortalGroup (+ 7 child entities)
**Risk Level**: Medium-High (complex relationships, portal integration, BargeEx integration)

## Conversion Scope

### Primary Entity
- **Vendor** - Main vendor entity with soft delete (IsActive)

### Direct Child Entities (Must Convert)
1. **VendorContact** - Eager loaded, inline editing
2. **VendorBusinessUnit** - Deferred loaded, inline editing, fuel supplier logic
3. **VendorPortalGroup** - Deferred loaded, modal editing

### Nested Child Entities (VendorPortalGroup children)
4. **VendorPortalGroupCustomer** - Portal customer access limits
5. **VendorPortalGroupCriteria** - Portal filter criteria
6. **VendorPortalGroupDestArea** - Portal destination area limits
7. **VendorPortalGroupDestFacility** - Portal destination facility limits
8. **VendorPortalGroupEvent** - Portal event type limits
9. **VendorPortalGroupUser** - Portal user mappings
10. **VendorPortalGroupReport** - Portal report access limits

### External Dependencies (Lookup Entities)
- **BargeExConfig** - BargeEx configuration lookup
- **ValidationList** - Payment terms, river bank
- **State** - US State lookup
- **River** - River location data
- **User** - Portal user accounts

## Entity Analysis

### Vendor Entity Structure

**Primary Properties:**
- VendorID (int, PK)
- Name (string, required, 20 chars)
- LongName (string, 50 chars)
- AccountingCode (string, 20 chars)
- IsActive (bool, soft delete)
- EnablePortal (bool)

**Address Information:**
- Address (string, 80 chars)
- City (string, 30 chars)
- State (string, 2 chars)
- Zip (string, 10 chars, ZipCodeOptionalExtension format)

**Contact Information:**
- PhoneNumber (string, 10 chars, Phone10 format)
- FaxNumber (string, 10 chars, Phone10 format)
- EmailAddress (string, 100 chars)

**BargeEx Integration:**
- IsBargeExEnabled (bool)
- BargeExTradingPartnerNum (string, 8 chars, padded)
- BargeExConfigID (int, nullable, FK)

**Vendor Type Flags:**
- IsLiquidBroker (bool)
- IsInternalVendor (bool)
- IsTankerman (bool)

**Billing:**
- TermsCode (string, 8 chars, FK to ValidationList)

**Child Collections:**
- VendorContacts (List, eager loaded)
- VendorBusinessUnits (List, deferred)
- VendorPortalGroups (List, deferred)

### Complex Business Rules

1. **BargeEx Conditional Validation**
   - When IsBargeExEnabled = true, both BargeExTradingPartnerNum and BargeExConfigID are required
   - BargeExTradingPartnerNum must be padded to 8 characters
   - Fields cleared when IsBargeExEnabled = false

2. **Primary Contact Constraint**
   - Only one VendorContact can have IsPrimary = true per Vendor
   - Setting a new primary automatically clears existing primary

3. **Default Fuel Supplier Constraint**
   - Only one VendorBusinessUnit can have IsDefaultFuelSupplier = true per Vendor
   - Setting a new default automatically clears existing default

4. **Fuel Supplier Conditional Logic**
   - When VendorBusinessUnit.IsFuelSupplier = false:
     - IsDefaultFuelSupplier cleared
     - MinDiscountQty cleared
     - MinDiscountFrequency cleared

5. **River/Mile Validation**
   - River and Mile must both be set or both be empty
   - Mile value must be within selected river's MinMile/MaxMile range

6. **Portal Account Management**
   - Deleting VendorContact with PortalUserID requires special confirmation
   - Warning about saved searches and preferences deletion

7. **Portal Group Prerequisites**
   - Vendor must be saved (not new) before adding portal groups
   - Unsaved Vendor changes must be saved before opening portal group modal

8. **Data Formatting**
   - Phone10: 10-digit phone formatting
   - ZipCodeOptionalExtension: ZIP + optional 4-digit extension
   - State: 2-character abbreviation

### Data Access Patterns

**Stored Procedures:**
- VendorSearch - Complex search with 9 filter parameters
- VendorSelect - Get by ID with VendorContacts as child result set
- VendorsSelect - Get all vendors
- VendorInsert - Create new, returns VendorID as output parameter
- VendorUpdate - Update existing
- VendorDelete - LEGACY, should NOT be used (use SetActive instead)

**Modern SQL Files Required:**
- Vendor_Search.sql - ListQuery with filterable/sortable fields
- Vendor_GetById.sql - Fetch single with contacts
- Vendor_GetAll.sql - Get all (may not be needed if Search handles)
- Vendor_Insert.sql - Create new, return VendorID
- Vendor_Update.sql - Update existing
- Vendor_SetActive.sql - Soft delete via IsActive flag

**Child Entity SQL Files Required:**

VendorContact:
- VendorContact_GetByVendorId.sql
- VendorContact_Insert.sql
- VendorContact_Update.sql
- VendorContact_Delete.sql (hard delete allowed)

VendorBusinessUnit:
- VendorBusinessUnit_GetByVendorId.sql
- VendorBusinessUnit_Insert.sql
- VendorBusinessUnit_Update.sql
- VendorBusinessUnit_Delete.sql (hard delete allowed)

VendorPortalGroup + children:
- VendorPortalGroup_GetByVendorId.sql (with all children)
- VendorPortalGroup_Insert.sql
- VendorPortalGroup_Update.sql
- VendorPortalGroup_Delete.sql
- VendorPortalGroup_Copy.sql (special copy operation)
- VendorPortalGroupCustomer_GetByGroupId.sql
- VendorPortalGroupCriteria_GetByGroupId.sql
- VendorPortalGroupDestArea_GetByGroupId.sql
- VendorPortalGroupDestFacility_GetByGroupId.sql
- VendorPortalGroupEvent_GetByGroupId.sql
- VendorPortalGroupUser_GetByGroupId.sql
- VendorPortalGroupReport_GetByGroupId.sql

### UI Requirements

**Search Form (frmVendorSearch):**
- Search filters: Name, AccountingCode, IsActive, BargeEx, Portal, FuelSupplier, LiquidBroker, InternalVendor, Tankerman
- Result grid with sortable columns
- Default filter: IsActiveOnly = true
- Operations: New, Edit, Delete, Export

**Detail Form (frmVendorDetail) - 4 Tabs:**

**Tab 1: Details**
- Vendor information fields (Name, LongName, AccountingCode, IsActive)
- Address fields (Address, City, State, Zip)
- Contact fields (Phone, Fax, Email)
- Vendor flags (IsLiquidBroker, IsTankerman, IsInternalVendor)
- Billing (TermsCode)
- Inline grid: VendorContacts with Add/Edit/Delete/Export toolbar
- Inline editing panel for contact details

**Tab 2: Portal**
- Visible only if Portal license active
- EnablePortal checkbox
- Grid: VendorPortalGroups with Add/Edit/Delete/Copy/Export toolbar
- Opens modal form (frmVendorPortalGroup) for editing
- Requires saved Vendor before adding groups

**Tab 3: Vendor Business Units**
- Grid: VendorBusinessUnits with Add/Edit/Delete/Export toolbar
- Inline editing panel with location (River, Mile, Bank)
- Fuel supplier fields (IsFuelSupplier, IsDefaultFuelSupplier, MinDiscountQty, MinDiscountFrequency)
- Boat assist flag

**Tab 4: BargeEx Settings**
- Visible only if GlobalSettingList.EnableBargeExBargeLineSupport = true
- IsBargeExEnabled checkbox
- BargeExTradingPartnerNum text field
- BargeExConfigID dropdown

**Workflow Patterns:**
- List-detail editing for VendorContacts and VendorBusinessUnits
- Modal dialog for VendorPortalGroup
- Tab disabling during inline editing
- Toolbar contextual enabling
- Cursor management (wait cursor during operations)
- Status bar validation display

### Modernization Requirements

**Repository Pattern:**
- Dapper with SqlText.GetSqlText() for embedded SQL resources
- All SQL in .sql files, no inline SQL
- Interface-based abstractions

**Service Layer:**
- Load relationships separately and compose DTOs
- NO navigation properties on entity classes
- VendorContacts loaded eagerly
- VendorBusinessUnits and VendorPortalGroups loaded on demand

**DTOs:**
- VendorDto - Full detail DTO with child collections as List<T>
- VendorListDto - Lightweight for search results
- VendorSearchRequest - Extends DataTableRequest with 9 filter properties
- VendorContactDto - Contact details
- VendorBusinessUnitDto - Business unit details
- VendorPortalGroupDto - Portal group with nested child collections

**ListQuery Pattern:**
- [Filterable] attributes on searchable properties
- [Sortable] attributes on sortable properties
- Default sort: Name ASC

**Soft Delete:**
- CRITICAL: Use IsActive field, NOT hard delete
- VendorDelete stored procedure exists but should NOT be used
- Implement SetActive pattern instead

**Audit Fields:**
- Entity supports field-level auditing via AuditFieldEvent
- All 19 properties are audited

## Dependency Analysis

### Conversion Sequence

This entity has complex dependencies and should be converted in this order:

**Phase 1: Foundation Entities (Week 1)**
1. **Vendor** - Parent entity, repository, basic CRUD
2. **VendorContact** - Simple child, eager loaded
3. **VendorBusinessUnit** - Complex child with fuel logic

**Phase 2: Portal Infrastructure (Week 2)**
4. **VendorPortalGroup** - Parent of 7 children
5. **VendorPortalGroupCustomer**
6. **VendorPortalGroupCriteria**
7. **VendorPortalGroupDestArea**
8. **VendorPortalGroupDestFacility**
9. **VendorPortalGroupEvent**
10. **VendorPortalGroupUser**
11. **VendorPortalGroupReport**

**Phase 3: Integration (Week 3)**
- Service layer composition
- Controllers and ViewModels
- Views and JavaScript
- Testing and validation

### External Entity Dependencies (Must Exist First)
- BargeExConfig (for BargeEx integration)
- ValidationList (for payment terms, river bank)
- State (for address)
- River (for business unit location)
- User (for portal user mapping)

### Reverse Dependencies (Entities that depend on Vendor)
These entities likely reference Vendor and may need updates:
- Purchase orders/invoices
- Fuel transactions
- Boat assist records
- Any vendor-related reporting

## Verification Plan

### Phase 1: Foundation (Days 1-3)

**Checkpoint 1.1: Vendor Repository**
- [ ] IVendorRepository interface created in Admin.Infrastructure.Abstractions
- [ ] VendorRepository implementation in Admin.Infrastructure.Repositories
- [ ] All 6 SQL files created as embedded resources
- [ ] Vendor_Search.sql implements ListQuery pattern
- [ ] Vendor_SetActive.sql created (NOT Vendor_Delete.sql)
- [ ] All repository methods tested with unit tests

**Checkpoint 1.2: VendorContact Repository**
- [ ] IVendorContactRepository interface created
- [ ] VendorContactRepository implementation
- [ ] 4 SQL files created (GetByVendorId, Insert, Update, Delete)
- [ ] Eager loading verified in Vendor_GetById.sql (multiple result sets)

**Checkpoint 1.3: VendorBusinessUnit Repository**
- [ ] IVendorBusinessUnitRepository interface created
- [ ] VendorBusinessUnitRepository implementation
- [ ] 4 SQL files created
- [ ] Deferred loading pattern verified

**Checkpoint 1.4: DTOs**
- [ ] VendorDto with all 19 properties + 3 child collections
- [ ] VendorListDto with 12 search result properties
- [ ] VendorSearchRequest with 9 filter properties + DataTableRequest
- [ ] VendorContactDto with all properties
- [ ] VendorBusinessUnitDto with all properties
- [ ] [Filterable] and [Sortable] attributes applied

### Phase 2: Portal Infrastructure (Days 4-6)

**Checkpoint 2.1: VendorPortalGroup Repository**
- [ ] IVendorPortalGroupRepository interface created
- [ ] VendorPortalGroupRepository implementation
- [ ] SQL files for CRUD + Copy operation
- [ ] All 7 child entity repositories created
- [ ] Nested loading verified (portal group + all children)

**Checkpoint 2.2: Portal Group Children**
- [ ] VendorPortalGroupCustomer repository + SQL
- [ ] VendorPortalGroupCriteria repository + SQL
- [ ] VendorPortalGroupDestArea repository + SQL
- [ ] VendorPortalGroupDestFacility repository + SQL
- [ ] VendorPortalGroupEvent repository + SQL
- [ ] VendorPortalGroupUser repository + SQL
- [ ] VendorPortalGroupReport repository + SQL
- [ ] All child DTOs created

### Phase 3: Service Layer (Days 7-8)

**Checkpoint 3.1: Service Implementation**
- [ ] IVendorService interface created
- [ ] VendorService implementation with composition logic
- [ ] GetByIdAsync loads Vendor + eager VendorContacts
- [ ] GetBusinessUnitsAsync for deferred loading
- [ ] GetPortalGroupsAsync for deferred loading
- [ ] SearchAsync implements ListQuery pattern
- [ ] CreateAsync with transaction support for children
- [ ] UpdateAsync with transaction support for children
- [ ] SetActiveAsync for soft delete
- [ ] All business rules implemented:
  - [ ] BargeEx conditional validation
  - [ ] Primary contact constraint
  - [ ] Default fuel supplier constraint
  - [ ] Fuel supplier conditional clearing
  - [ ] River/Mile validation
- [ ] Service registered in DI container

**Checkpoint 3.2: Business Logic Validation**
- [ ] Unit tests for BargeEx validation
- [ ] Unit tests for primary contact logic
- [ ] Unit tests for default fuel supplier logic
- [ ] Unit tests for conditional field clearing
- [ ] Unit tests for river/mile validation

### Phase 4: API Layer (Days 9-10)

**Checkpoint 4.1: API Controller**
- [ ] VendorController created in Admin.Api
- [ ] GET /api/vendors (search with ListQuery)
- [ ] GET /api/vendors/{id} (detail with contacts)
- [ ] GET /api/vendors/{id}/business-units (deferred load)
- [ ] GET /api/vendors/{id}/portal-groups (deferred load)
- [ ] POST /api/vendors (create)
- [ ] PUT /api/vendors/{id} (update)
- [ ] POST /api/vendors/{id}/contacts (add contact)
- [ ] PUT /api/vendors/{id}/contacts/{contactId} (update contact)
- [ ] DELETE /api/vendors/{id}/contacts/{contactId} (delete contact)
- [ ] POST /api/vendors/{id}/business-units (add business unit)
- [ ] PUT /api/vendors/{id}/business-units/{buId} (update business unit)
- [ ] DELETE /api/vendors/{id}/business-units/{buId} (delete business unit)
- [ ] PATCH /api/vendors/{id}/active (soft delete)
- [ ] Authorization attributes applied
- [ ] API documentation (Swagger)

**Checkpoint 4.2: Portal Group API**
- [ ] GET /api/vendors/{id}/portal-groups/{groupId} (detail with all children)
- [ ] POST /api/vendors/{id}/portal-groups (create)
- [ ] PUT /api/vendors/{id}/portal-groups/{groupId} (update)
- [ ] DELETE /api/vendors/{id}/portal-groups/{groupId} (delete)
- [ ] POST /api/vendors/{id}/portal-groups/{groupId}/copy (copy operation)

### Phase 5: UI Layer (Days 11-14)

**Checkpoint 5.1: ViewModels**
- [ ] VendorSearchViewModel with filter properties
- [ ] VendorDetailViewModel with all tabs
- [ ] VendorContactViewModel for inline editing
- [ ] VendorBusinessUnitViewModel for inline editing
- [ ] VendorPortalGroupViewModel for modal editing
- [ ] License-based tab visibility logic
- [ ] GlobalSetting-based tab visibility logic

**Checkpoint 5.2: Search Page**
- [ ] VendorController (UI) created
- [ ] Index action returns VendorSearchViewModel
- [ ] Index.cshtml with search form
- [ ] 9 filter controls (Name, AccountingCode, checkboxes)
- [ ] DataTables configuration with server-side processing
- [ ] Default filter: IsActiveOnly = true
- [ ] New/Edit/Delete buttons
- [ ] Export functionality

**Checkpoint 5.3: Detail Page**
- [ ] Detail action (GET) loads VendorDetailViewModel
- [ ] Create action (GET) initializes new VendorDetailViewModel
- [ ] Save action (POST) with model validation
- [ ] Delete confirmation with soft delete
- [ ] Detail.cshtml with 4 tabs
- [ ] Tab visibility logic (Portal license, BargeEx setting)

**Checkpoint 5.4: Details Tab**
- [ ] Vendor information section
- [ ] Address section
- [ ] Contact information section
- [ ] Vendor flags section
- [ ] Billing section
- [ ] VendorContacts grid with DataTables
- [ ] Inline editing panel for contacts
- [ ] Add/Edit/Delete/Export toolbar
- [ ] Set/Cancel buttons
- [ ] Primary contact logic (only one)
- [ ] Portal user deletion confirmation

**Checkpoint 5.5: Portal Tab**
- [ ] EnablePortal checkbox
- [ ] VendorPortalGroups grid
- [ ] Add/Edit/Delete/Copy/Export toolbar
- [ ] Modal integration for frmVendorPortalGroup
- [ ] Unsaved changes prompt before modal
- [ ] License visibility check
- [ ] Grid refresh after modal save

**Checkpoint 5.6: Business Units Tab**
- [ ] VendorBusinessUnits grid
- [ ] Inline editing panel
- [ ] Location fields (River dropdown, Mile, Bank dropdown)
- [ ] Fuel supplier checkbox with conditional enabling
- [ ] IsDefaultFuelSupplier logic (only one)
- [ ] MinDiscountQty and MinDiscountFrequency (conditional)
- [ ] IsBoatAssistSupplier checkbox
- [ ] River/Mile validation
- [ ] Add/Edit/Delete/Export toolbar
- [ ] Set/Cancel buttons

**Checkpoint 5.7: BargeEx Tab**
- [ ] IsBargeExEnabled checkbox
- [ ] BargeExTradingPartnerNum text field
- [ ] BargeExConfigID dropdown
- [ ] Conditional enabling when checkbox checked
- [ ] Conditional validation (required when enabled)
- [ ] Field clearing when disabled
- [ ] GlobalSetting visibility check
- [ ] 8-character padding validation

**Checkpoint 5.8: JavaScript**
- [ ] DataTables initialization for search grid
- [ ] DataTables for all child grids (contacts, business units, portal groups)
- [ ] Inline editing enable/disable logic
- [ ] Tab disabling during child editing
- [ ] Toolbar contextual enabling
- [ ] Primary contact auto-clear logic
- [ ] Default fuel supplier auto-clear logic
- [ ] Fuel supplier conditional field clearing
- [ ] BargeEx conditional field enabling/clearing
- [ ] River/Mile validation
- [ ] Unsaved changes detection
- [ ] Modal dialog integration
- [ ] AJAX operations for child entities
- [ ] Client-side validation integration

### Phase 6: Testing (Days 15-16)

**Checkpoint 6.1: Playwright E2E Tests (7 Test Types)**

**1. CRUD Operations**
- [ ] Create new vendor with all required fields
- [ ] Edit existing vendor details
- [ ] Soft delete vendor (SetActive)
- [ ] Verify vendor appears/disappears in search based on IsActive

**2. Search and Filter**
- [ ] Search by Name
- [ ] Search by AccountingCode
- [ ] Filter by IsActiveOnly (default checked)
- [ ] Filter by FuelSuppliersOnly
- [ ] Filter by IsBargeExEnabledOnly
- [ ] Filter by EnablePortalOnly
- [ ] Filter by LiquidBrokerOnly
- [ ] Filter by InternalVendorOnly
- [ ] Filter by TankermanOnly
- [ ] Combined filters
- [ ] Sort by Name, LongName, AccountingCode

**3. Form Validation**
- [ ] Name required validation
- [ ] Phone10 format validation (Phone, Fax)
- [ ] Email format validation
- [ ] ZipCodeOptionalExtension format validation
- [ ] BargeEx conditional validation (TradingPartnerNum + ConfigID required when enabled)
- [ ] BargeExTradingPartnerNum 8-character padding
- [ ] River/Mile both required or both empty
- [ ] Mile within river min/max range

**4. Child Entity Management**
- [ ] Add VendorContact inline
- [ ] Edit VendorContact inline
- [ ] Delete VendorContact
- [ ] Export VendorContacts
- [ ] Primary contact constraint (only one)
- [ ] Portal user deletion confirmation
- [ ] Add VendorBusinessUnit inline
- [ ] Edit VendorBusinessUnit inline
- [ ] Delete VendorBusinessUnit
- [ ] Export VendorBusinessUnits
- [ ] Default fuel supplier constraint (only one)
- [ ] Fuel supplier conditional field clearing
- [ ] Add VendorPortalGroup via modal
- [ ] Edit VendorPortalGroup via modal
- [ ] Delete VendorPortalGroup
- [ ] Copy VendorPortalGroup
- [ ] Export VendorPortalGroups
- [ ] Portal group requires saved vendor

**5. Relationships and Data Loading**
- [ ] VendorContacts eager loaded with parent
- [ ] VendorBusinessUnits deferred loaded on tab activation
- [ ] VendorPortalGroups deferred loaded on tab activation
- [ ] BargeExConfig dropdown populated
- [ ] ValidationList (TermsCode) dropdown populated
- [ ] ValidationList (RiverBank) dropdown populated
- [ ] State dropdown populated
- [ ] River dropdown populated
- [ ] Verify child data persists after save
- [ ] Verify relationship integrity after update

**6. Permissions and Authorization**
- [ ] Search page accessible with appropriate role
- [ ] New button respects ButtonType.Add permissions
- [ ] Edit button respects ButtonType.Open permissions
- [ ] Delete button respects ButtonType.Remove permissions
- [ ] Submit button respects ButtonType.Submit permissions
- [ ] Portal tab visible only with Portal license
- [ ] BargeEx tab visible only with global setting enabled

**7. State Management and Workflow**
- [ ] Tab disabling during inline editing
- [ ] Details tab disabled while editing VendorBusinessUnit
- [ ] VendorBusinessUnits tab disabled while editing VendorContact
- [ ] Portal/BargeEx tabs disabled while editing child entities
- [ ] Toolbar disabled during inline editing
- [ ] Submit button disabled during inline editing
- [ ] Set/Cancel buttons for inline editing
- [ ] Form dirty tracking (unsaved changes)
- [ ] Unsaved changes prompt before portal group modal
- [ ] Form refresh after portal group modal save
- [ ] Search grid refresh after save/delete
- [ ] Child grid refresh after add/edit/delete
- [ ] Wait cursor during operations
- [ ] Status bar validation messages

**Checkpoint 6.2: Integration Tests**
- [ ] Repository integration tests against test database
- [ ] Service layer integration tests
- [ ] API endpoint integration tests
- [ ] Transaction rollback on error
- [ ] Concurrency handling

**Checkpoint 6.3: Performance Tests**
- [ ] Search with 10,000+ vendors
- [ ] Detail page load time
- [ ] Child entity loading performance
- [ ] Grid rendering performance
- [ ] SQL query performance (execution plans)

### Phase 7: Documentation and Handoff (Day 17)

**Checkpoint 7.1: Documentation**
- [ ] API documentation complete (Swagger)
- [ ] Entity relationship diagram
- [ ] Business rules documentation
- [ ] SQL file documentation
- [ ] UI workflow documentation
- [ ] Deployment guide

**Checkpoint 7.2: Code Quality**
- [ ] Code review completed
- [ ] All TODOs resolved
- [ ] No compiler warnings
- [ ] Code coverage > 80%
- [ ] Security review completed (XSS, SQL injection, authorization)

**Checkpoint 7.3: Deployment**
- [ ] Database migration scripts
- [ ] DI registration verified
- [ ] Configuration settings documented
- [ ] Rollback plan documented
- [ ] Production deployment checklist

## Risk Assessment

### High Risks

**1. Complex Child Entity Management (HIGH)**
- 10 total child entities (3 direct + 7 nested)
- Portal groups have 7 children each
- Risk: Complex transaction management, data integrity
- Mitigation: Phase-based approach, extensive testing, transaction boundaries

**2. Portal Integration Complexity (HIGH)**
- Separate modal form for portal groups
- Nested child collections (7 levels deep)
- Copy operation complexity
- Risk: Data loss, incorrect portal access configuration
- Mitigation: Comprehensive testing, clear user warnings, transaction safety

**3. Business Logic Complexity (MEDIUM-HIGH)**
- Multiple constraint enforcement (primary contact, default fuel supplier)
- Conditional validation (BargeEx, fuel supplier)
- Conditional field clearing
- Risk: Logic bugs, inconsistent state
- Mitigation: Extensive unit tests, business rule documentation

**4. Dual Loading Strategies (MEDIUM)**
- VendorContacts eager loaded
- VendorBusinessUnits/PortalGroups deferred
- Risk: N+1 queries, performance issues
- Mitigation: Explicit loading strategy, performance testing

### Medium Risks

**5. Soft Delete Pattern (MEDIUM)**
- IsActive flag instead of hard delete
- Legacy VendorDelete stored procedure exists but should NOT be used
- Risk: Developer uses wrong delete method
- Mitigation: Clear documentation, code comments, code review

**6. License-Based Feature Visibility (MEDIUM)**
- Portal tab only visible with license
- BargeEx tab only visible with global setting
- Risk: Features hidden unexpectedly
- Mitigation: Clear UI messaging, admin documentation

**7. Data Format Validation (MEDIUM)**
- Phone10, ZipCodeOptionalExtension, Email formats
- BargeExTradingPartnerNum padding
- Risk: Invalid data accepted/rejected
- Mitigation: Client and server-side validation, unit tests

### Low Risks

**8. Search Complexity (LOW-MEDIUM)**
- 9 filter parameters
- ListQuery implementation
- Risk: Search not finding expected results
- Mitigation: Comprehensive search tests

**9. Inline vs Modal Editing (LOW)**
- Contacts/BusinessUnits inline, PortalGroups modal
- Risk: Inconsistent UX
- Mitigation: Clear UI patterns, user testing

## Implementation Notes

### Critical Patterns to Follow

1. **Soft Delete**: Always use IsActive, NEVER VendorDelete stored procedure
2. **SQL Resources**: All SQL in .sql files, loaded via SqlText.GetSqlText()
3. **No Navigation Properties**: Load relationships via separate repository methods
4. **ListQuery**: Use [Filterable] and [Sortable] attributes
5. **Transactions**: Wrap multi-entity operations in transactions
6. **Eager vs Deferred**: VendorContacts eager, others deferred
7. **Validation**: Both client-side and server-side
8. **Authorization**: Use IdentityConstants.ApplicationScheme (NOT "Cookies")
9. **ViewModels**: Use MVVM pattern, avoid @ViewBag/@ViewData
10. **Comments**: Sparingly, only for complex logic

### Common Pitfalls to Avoid

1. ❌ Using VendorDelete stored procedure (use SetActive instead)
2. ❌ Adding navigation properties to entity classes
3. ❌ Loading all children eagerly (respect loading strategy)
4. ❌ Forgetting BargeEx conditional validation
5. ❌ Not clearing conditional fields when disabled
6. ❌ Allowing multiple primary contacts
7. ❌ Allowing multiple default fuel suppliers
8. ❌ Not padding BargeExTradingPartnerNum to 8 characters
9. ❌ Skipping river/mile validation
10. ❌ Not confirming portal user deletion
11. ❌ Allowing portal group add for unsaved vendor
12. ❌ Not disabling tabs during inline editing
13. ❌ Missing license checks for portal tab
14. ❌ Missing global setting check for BargeEx tab
15. ❌ Inline SQL instead of embedded resources

### Testing Strategy

**Unit Tests (Day 7-8):**
- Repository methods
- Service layer business logic
- Business rule enforcement
- Validation logic
- Conditional field clearing

**Integration Tests (Day 9-10):**
- Repository against test database
- Service layer with repository
- API endpoints
- Transaction handling

**E2E Tests (Day 15-16):**
- Complete workflows (create, search, edit, delete)
- All 7 Playwright test types
- Child entity management
- Business rule enforcement
- Authorization checks
- State management

**Performance Tests (Day 16):**
- Search with large dataset
- Grid rendering
- Child entity loading
- SQL query execution plans

## Timeline Estimate

**Week 1: Foundation (Days 1-3)**
- Day 1: Vendor repository + SQL files
- Day 2: VendorContact repository + SQL files
- Day 3: VendorBusinessUnit repository + SQL files

**Week 2: Portal Infrastructure (Days 4-6)**
- Day 4: VendorPortalGroup repository + SQL
- Day 5: Portal child repositories (1-4)
- Day 6: Portal child repositories (5-7)

**Week 3: Services and API (Days 7-10)**
- Day 7: Service layer + business logic
- Day 8: Service unit tests
- Day 9: API controllers
- Day 10: API integration tests

**Week 4: UI Layer (Days 11-14)**
- Day 11: ViewModels + Search page
- Day 12: Detail page tabs 1-2 (Details, Portal)
- Day 13: Detail page tabs 3-4 (Business Units, BargeEx)
- Day 14: JavaScript + client-side validation

**Week 5: Testing and Documentation (Days 15-17)**
- Day 15: Playwright E2E tests (CRUD, Search, Validation)
- Day 16: Playwright E2E tests (Children, Relationships, Permissions, State)
- Day 17: Documentation + deployment prep

**Total: 17 days (3.5 weeks)**

## Success Criteria

### Functional Requirements
- ✅ All CRUD operations working
- ✅ Search with all 9 filters working
- ✅ All child entities (10 total) managed correctly
- ✅ Soft delete working (IsActive pattern)
- ✅ All business rules enforced
- ✅ All validations working (client and server)
- ✅ License-based visibility working
- ✅ GlobalSetting-based visibility working
- ✅ All workflows tested (7 primary workflows)

### Technical Requirements
- ✅ All SQL in embedded .sql files
- ✅ Repository pattern implemented
- ✅ Service layer with composition
- ✅ DTOs for all entities
- ✅ ListQuery pattern for search
- ✅ [Filterable] and [Sortable] attributes
- ✅ Transactions for multi-entity operations
- ✅ No navigation properties
- ✅ IdentityConstants.ApplicationScheme
- ✅ MVVM pattern (ViewModels)

### Quality Requirements
- ✅ Code coverage > 80%
- ✅ All Playwright tests passing (7 types)
- ✅ No security vulnerabilities
- ✅ Performance acceptable (< 2s page load)
- ✅ Documentation complete
- ✅ Code review approved

### Deployment Requirements
- ✅ Migration scripts tested
- ✅ DI registration verified
- ✅ Configuration documented
- ✅ Rollback plan ready
- ✅ Production checklist complete

## Next Steps

1. **Review this plan** with stakeholders for approval
2. **Identify available resources** (developers, testers, DBA)
3. **Confirm external dependencies** are already converted:
   - BargeExConfig
   - ValidationList
   - State
   - River
   - User
4. **Set up development environment** with test database
5. **Create feature branch** for Vendor conversion
6. **Begin Phase 1: Foundation** (Day 1)

## Approval Required

Before proceeding with implementation, please confirm:
- [ ] Scope is complete and accurate
- [ ] Timeline is acceptable (17 days)
- [ ] Resources are available
- [ ] External dependencies are ready
- [ ] Risk assessment is acceptable
- [ ] Testing approach is approved
- [ ] All non-negotiables are understood

---

**Document Version**: 1.0
**Created**: 2025-12-11
**Status**: Awaiting Approval
**Next Review**: After Phase 1 completion
