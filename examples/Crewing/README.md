# Crewing Conversion Example

This directory contains example code templates showing how the agents generate code targeted at **BargeOps.Admin.API** and **BargeOps.Admin.UI**, using patterns from **BargeOps.Crewing** for reference.

## Files in This Directory

### API Layer Examples (BargeOps.Admin.API)

**`CrewingController.cs`**
- Complete API controller example
- Shows RESTful endpoints for Crewing entity
- Demonstrates proper error handling and logging
- Includes lookup endpoints for dropdowns
- Based on BoatLocation patterns but includes Crewing-specific logic

### UI Layer Examples (BargeOps.Admin.UI)

**`CrewingSearchController.cs`**
- Complete MVC controller example
- Shows DataTables integration
- Demonstrates CRUD operations
- Includes proper authorization attributes
- Uses patterns from BargeOps.Crewing.UI

## Key Patterns Demonstrated

### API Controller Patterns

1. **Dependency Injection**
   ```csharp
   private readonly ICrewingService _crewingService;
   private readonly ILogger<CrewingController> _logger;
   ```

2. **Search Endpoint (POST)**
   ```csharp
   [HttpPost("search")]
   public async Task<IActionResult> Search([FromBody] CrewingSearchRequest request)
   ```

3. **CRUD Endpoints**
   - GET /api/crewing/{id} - Retrieve
   - POST /api/crewing - Create
   - PUT /api/crewing/{id} - Update
   - DELETE /api/crewing/{id} - Delete

4. **Lookup Endpoints**
   - GET /api/crewing/rivers
   - GET /api/crewing/location-types

### UI Controller Patterns

1. **Search Page**
   ```csharp
   [HttpGet("Index")]
   [RequirePermission<AuthPermissions>(AuthPermissions.CrewingReadOnly, PermissionAccessType.ReadOnly)]
   public async Task<IActionResult> Index()
   ```

2. **DataTables AJAX**
   ```csharp
   [HttpPost("CrewingTable")]
   public async Task<IActionResult> CrewingTable(DataTableRequest request, CrewingSearchViewModel model)
   ```

3. **Edit Form Pattern**
   - Create: GET /CrewingSearch/Create
   - Edit: GET /CrewingSearch/Edit/{id}
   - Save: POST /CrewingSearch/Save (handles both create and update)

4. **Authorization**
   ```csharp
   [RequirePermission<AuthPermissions>(AuthPermissions.CrewingModify, PermissionAccessType.Modify)]
   ```

## Comparison with BargeOps.Crewing

These examples are **targeted at BargeOps.Admin** but use **BargeOps.Crewing patterns** for clarity:

| Aspect | BargeOps.Admin (Target) | BargeOps.Crewing (Reference) |
|--------|------------------------|------------------------------|
| API Project | BargeOps.Admin.API | BargeOps.Crewing.API |
| UI Project | BargeOps.Admin.UI | BargeOps.Crewing.UI |
| Namespace | Admin.Api.Controllers | Crewing.Api.Controllers |
| Authorization | Admin permissions | Crewing permissions |
| Service Layer | Admin.Domain.Services | Crewing.Domain.Services |

## How Agents Use These Examples

When Agent 10 (Conversion Template Generator) runs, it:

1. **References BoatLocation** (BargeOps.Admin) as the primary pattern
2. **Uses Crewing examples** to clarify UI patterns and DataTables usage
3. **Generates code** specifically for BargeOps.Admin.API and BargeOps.Admin.UI
4. **Adapts patterns** from both Admin and Crewing for the target entity

## Target Output Structure

The agents will generate code for this structure:

```
BargeOps.Admin.API/
├── src/
│   ├── Admin.Api/
│   │   └── Controllers/
│   │       └── CrewingController.cs
│   ├── Admin.Domain/
│   │   ├── Models/
│   │   │   ├── Crewing.cs
│   │   │   ├── CrewingSchedule.cs
│   │   │   └── CrewingAssignment.cs
│   │   ├── Dto/
│   │   │   ├── CrewingDto.cs
│   │   │   ├── CrewingSearchRequest.cs
│   │   │   └── CrewingSearchResponse.cs
│   │   ├── Interfaces/
│   │   │   └── ICrewingRepository.cs
│   │   └── Services/
│   │       └── ICrewingService.cs
│   └── Admin.Infrastructure/
│       ├── Repositories/
│       │   └── CrewingRepository.cs
│       ├── Services/
│       │   └── CrewingService.cs
│       └── Mappings/
│           └── CrewingMappingProfile.cs

BargeOps.Admin.UI/
├── Controllers/
│   └── CrewingSearchController.cs
├── Models/
│   ├── CrewingSearchViewModel.cs
│   ├── CrewingEditViewModel.cs
│   └── CrewingListModel.cs
├── Services/
│   ├── ICrewingService.cs
│   └── CrewingService.cs
├── Views/
│   └── CrewingSearch/
│       ├── Index.cshtml
│       ├── Edit.cshtml
│       └── Details.cshtml
└── wwwroot/
    ├── js/
    │   └── crewingSearch.js
    └── css/
        └── crewingSearch.css
```

## Usage

These examples serve as:
1. **Reference** for understanding output structure
2. **Templates** for manual implementation
3. **Validation** that generated code follows correct patterns

## Notes

- These are **simplified examples** for demonstration
- Full generated code includes more detailed error handling
- Actual implementation requires additional supporting files (DTOs, ViewModels, etc.)
- Always refer to BoatLocation in BargeOps.Admin for the canonical pattern
