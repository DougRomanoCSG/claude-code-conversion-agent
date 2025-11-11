# Facility Conversion Example

This directory will contain example outputs from converting the Facility entity.

## Expected Files (After Running Conversion)

### Analysis Outputs (from Agents 1-9)

Located in `../../output/Facility/`:

- `form-structure-search.json` - frmFacilitySearch UI structure
- `form-structure-detail.json` - frmFacilityDetail UI structure
- `business-logic.json` - FacilityLocation business rules
- `data-access.json` - FacilityLocationSearch stored procedures
- `security.json` - Permission patterns
- `ui-mapping.json` - Legacy to modern UI component mappings
- `workflow.json` - User flow patterns
- `tabs.json` - Detail form tab structure (Details, Status, Berths, NDC Data)
- `validation.json` - All validation rules
- `related-entities.json` - FacilityStatus and FacilityBerth relationships

### Generated Templates (from Step 11)

Located in `../../output/Facility/templates/`:

#### BargeOps.Admin.API Templates
Located in `templates/api/`:

**Domain Models:**
- `Facility.cs` - Main domain model
- `FacilityStatus.cs` - Status tracking entity
- `FacilityBerth.cs` - Berth management entity

**DTOs:**
- `FacilityDto.cs` - Grid display DTO
- `FacilityDetailDto.cs` - Complete facility data
- `FacilitySearchCriteria.cs` - Search parameters
- `FacilitySearchRequest.cs` - DataTables request
- `FacilitySearchResponse.cs` - Paginated results
- `FacilityCreateDto.cs` - Create DTO
- `FacilityUpdateDto.cs` - Update DTO

**Repository:**
- `IFacilityRepository.cs` - Repository interface
- `FacilityRepository.cs` - Dapper implementation

**Service:**
- `IFacilityService.cs` - Service interface
- `FacilityService.cs` - Business logic implementation

**Controller:**
- `FacilityController.cs` - API endpoints

**Mappings:**
- `FacilityMappingProfile.cs` - AutoMapper profile

#### BargeOps.Admin.UI Templates
Located in `templates/ui/`:

**View Models:**
- `FacilitySearchViewModel.cs` - Search criteria
- `FacilityListModel.cs` - Grid row data
- `FacilityEditViewModel.cs` - Edit form model
- `FacilityBerthViewModel.cs` - Berth management
- `FacilityStatusViewModel.cs` - Status tracking

**Service:**
- `IFacilityService.cs` - UI service interface
- `FacilityService.cs` - HttpClient implementation

**Controller:**
- `FacilitySearchController.cs` - MVC controller

**Views:**
- `Index.cshtml` - Search page
- `_Search.cshtml` - Search panel partial
- `_SearchResults.cshtml` - Grid partial
- `Edit.cshtml` - Add/Edit form with tabs
- `Details.cshtml` - Read-only view

**JavaScript:**
- `facilitySearch.js` - DataTables and form handling

**CSS:**
- `facilitySearch.css` - Custom styling

## Key Features Extracted

### Search Form (frmFacilitySearch)

**Search Criteria:**
- Facility Name (txtName)
- Short Name (txtShortName)
- BargeEx Code (txtBargeExCode)
- River (dropdown)
- Facility Type (dropdown)
- Mile Range (StartMile - EndMile)
- Active Only (checkbox)

**Grid Columns:**
- LocationID (hidden)
- Name
- ShortName
- River
- Mile
- Bank
- Facility Type
- BargeEx Code
- IsActive

### Detail Form (frmFacilityDetail)

**Tab 1: Details**
- Basic Information (Name, ShortName, River, Mile, Bank)
- Facility Type (BargeExLocationType, BargeExCode)
- Lock/Gauge Specific Fields (conditional on type)

**Tab 2: Status**
- FacilityStatus grid with CRUD
- Status tracking over time

**Tab 3: Berths**
- FacilityBerth grid with CRUD
- Berth management

**Tab 4: NDC Data**
- NDC-specific fields (NdcName, NdcAddress, etc.)

### Business Rules

1. Lock/Gauge fields must be blank if Facility Type is not "Lock" or "Gauge Location"
2. River required if mile range specified
3. EndMile must be >= StartMile
4. Conditional validation based on Facility Type

### Security

- **ReadOnly**: FacilityReadOnly permission
- **Modify**: FacilityModify permission
- SubSystem: frmFacilitySearch

## Reference

For comparison, see:
- BargeOps.Admin.API: `BoatLocationController.cs`
- BargeOps.Admin.UI: `BoatLocationSearchController.cs`
- BargeOps.Crewing examples in `../Crewing/`

## Running the Conversion

```bash
cd C:\source\agents\ClaudeOnshoreConversionAgent
bun run agents/orchestrator.ts --entity "Facility"
```

This will populate all analysis files and generate templates.
