# RiverArea API Layer - Setup Guide

**Generated**: 2025-12-11
**Layer**: API (BargeOps.Admin.API)

---

## 📋 What Was Generated

### ✅ API Service Layer
1. **`IRiverAreaService.cs`** - Service interface
   - Location: `templates/api/Services/`
   - Namespace: `Admin.Domain.Services`
   - Defines business logic methods

2. **`RiverAreaService.cs`** - Service implementation
   - Location: `templates/api/Services/`
   - Namespace: `Admin.Infrastructure.Services`
   - Implements all business rules and validation

### ✅ API Controller
3. **`RiverAreaController.cs`** - RESTful API controller
   - Location: `templates/api/Controllers/`
   - Namespace: `Admin.Api.Controllers`
   - Full CRUD + Search endpoints

---

## 🚀 Quick Start

### Step 1: Copy Files to Target Locations

```bash
# Service Interface
cp templates/api/Services/IRiverAreaService.cs
→ C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Domain\Services\

# Service Implementation
cp templates/api/Services/RiverAreaService.cs
→ C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Services\

# API Controller
cp templates/api/Controllers/RiverAreaController.cs
→ C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Api\Controllers\
```

### Step 2: Register Services in DI Container

**Location**: `Admin.Api/Program.cs` or `Startup.cs`

```csharp
using Admin.Domain.Services;
using Admin.Infrastructure.Services;
using Admin.Infrastructure.Abstractions;
using Admin.Infrastructure.Repositories;

// In ConfigureServices or builder.Services:

// Repository
builder.Services.AddScoped<IRiverAreaRepository, RiverAreaRepository>();

// Service
builder.Services.AddScoped<IRiverAreaService, RiverAreaService>();
```

### Step 3: Verify Database Connection

Ensure your `appsettings.json` has the connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=...;Trusted_Connection=True;"
  }
}
```

### Step 4: Build and Test

```bash
cd C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API
dotnet build
dotnet run
```

---

## 🔍 API Endpoints

### Base URL
```
https://localhost:5001/api/riverarea
```

### Endpoints Summary

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/riverarea/search` | Search with filtering |
| GET | `/api/riverarea/{id}` | Get by ID with segments |
| POST | `/api/riverarea` | Create new river area |
| PUT | `/api/riverarea/{id}` | Update river area |
| DELETE | `/api/riverarea/{id}` | Delete river area (hard) |
| GET | `/api/riverarea/{id}/segments` | Get segments |
| POST | `/api/riverarea/validate` | Validate business rules |

---

## 📝 Detailed Endpoint Documentation

### 1. Search River Areas

**Endpoint**: `POST /api/riverarea/search`

**Request Body**:
```json
{
  "name": "Mississippi",
  "activeOnly": true,
  "pricingZonesOnly": false,
  "portalAreasOnly": false,
  "customerID": null,
  "highWaterAreasOnly": false,
  "draw": 1,
  "start": 0,
  "length": 25,
  "orderColumn": "Name",
  "orderDirection": "ASC"
}
```

**Response**: DataTables format
```json
{
  "draw": 1,
  "recordsTotal": 150,
  "recordsFiltered": 15,
  "data": [
    {
      "riverAreaID": 1,
      "name": "Upper Mississippi",
      "isActive": true,
      "isPriceZone": true,
      "isPortalArea": false,
      "isHighWaterArea": false,
      "customerName": null,
      "isFuelTaxArea": false,
      "isLiquidRateArea": false
    }
  ]
}
```

### 2. Get River Area by ID

**Endpoint**: `GET /api/riverarea/{id}`

**Example**: `GET /api/riverarea/1`

**Response**:
```json
{
  "riverAreaID": 1,
  "name": "Upper Mississippi",
  "isActive": true,
  "isPriceZone": true,
  "isPortalArea": false,
  "isHighWaterArea": false,
  "customerID": null,
  "isFuelTaxArea": false,
  "isLiquidRateArea": false,
  "segments": [
    {
      "riverAreaSegmentID": 1,
      "riverAreaID": 1,
      "river": "MIS",
      "startMile": 100.0,
      "endMile": 200.5
    }
  ]
}
```

### 3. Create River Area

**Endpoint**: `POST /api/riverarea`

**Request Body**:
```json
{
  "name": "New River Area",
  "isActive": true,
  "isPriceZone": true,
  "isPortalArea": false,
  "isHighWaterArea": false,
  "customerID": null,
  "isFuelTaxArea": false,
  "isLiquidRateArea": false,
  "segments": [
    {
      "river": "MIS",
      "startMile": 300.0,
      "endMile": 350.5
    }
  ]
}
```

**Response**: `201 Created`
```json
{
  "riverAreaID": 123,
  "name": "New River Area",
  // ... full object
}
```

**Location Header**: `/api/riverarea/123`

### 4. Update River Area

**Endpoint**: `PUT /api/riverarea/{id}`

**Example**: `PUT /api/riverarea/123`

**Request Body**: Full RiverAreaDto object

**Response**: `204 No Content`

### 5. Delete River Area

**Endpoint**: `DELETE /api/riverarea/{id}`

**Example**: `DELETE /api/riverarea/123`

**Response**: `204 No Content`

**Note**: This is a HARD DELETE (legacy pattern)

### 6. Get Segments

**Endpoint**: `GET /api/riverarea/{id}/segments`

**Example**: `GET /api/riverarea/1/segments`

**Response**:
```json
[
  {
    "riverAreaSegmentID": 1,
    "riverAreaID": 1,
    "river": "MIS",
    "startMile": 100.0,
    "endMile": 200.5
  },
  {
    "riverAreaSegmentID": 2,
    "riverAreaID": 1,
    "river": "OHI",
    "startMile": 50.0,
    "endMile": 100.0
  }
]
```

### 7. Validate River Area

**Endpoint**: `POST /api/riverarea/validate`

**Request Body**: Full RiverAreaDto object

**Response**:
```json
[
  "Name is required",
  "Only one of these may be checked: Pricing zone, Portal area, or High water area"
]
```

**Empty array** = Valid

---

## 🔐 Authorization

All endpoints require authentication:

```csharp
[Authorize]
```

Ensure your API has authentication configured (JWT, Cookies, etc.)

---

## ✅ Business Rules Implemented

### 1. Name Validation
- **Required**: Name cannot be empty
- **Max Length**: 50 characters

### 2. Mutually Exclusive Area Types
Only ONE can be true:
- IsPriceZone
- IsPortalArea
- IsHighWaterArea

### 3. CustomerID Conditional Validation
- **Required** when `IsHighWaterArea = true`
- **Must be blank** when `IsHighWaterArea = false`

### 4. River Segment Validation
- **River**: Required, max 3 characters
- **StartMile**: Required, must be numeric
- **EndMile**: Required, must be numeric
- **Range**: StartMile < EndMile

### 5. Pricing Zone Overlap Detection
When `IsPriceZone = true`:
- Check all segments against other pricing zones
- Segments cannot overlap on same river
- Algorithm: `NOT (myEnd <= otherStart OR myStart >= otherEnd)`

---

## 🧪 Testing with Swagger/Postman

### Swagger UI

The API should have Swagger enabled. Access at:
```
https://localhost:5001/swagger
```

### Postman Collection

Example requests:

**1. Search (POST)**
```
POST https://localhost:5001/api/riverarea/search
Content-Type: application/json

{
  "activeOnly": true,
  "length": 10
}
```

**2. Get by ID (GET)**
```
GET https://localhost:5001/api/riverarea/1
```

**3. Create (POST)**
```
POST https://localhost:5001/api/riverarea
Content-Type: application/json

{
  "name": "Test Area",
  "isActive": true,
  "isPriceZone": false,
  "segments": []
}
```

---

## 📊 Error Handling

### HTTP Status Codes

| Code | Meaning | When |
|------|---------|------|
| 200 | OK | Successful GET/Search |
| 201 | Created | Successful POST |
| 204 | No Content | Successful PUT/DELETE |
| 400 | Bad Request | Validation error |
| 401 | Unauthorized | Not authenticated |
| 404 | Not Found | Resource doesn't exist |
| 500 | Server Error | Unexpected error |

### Error Response Format

**Validation Error** (400):
```json
"Validation failed: Name is required; Only one of these may be checked: Pricing zone, Portal area, or High water area"
```

**Not Found** (404):
```json
"River area with ID 999 not found"
```

**Server Error** (500):
```json
"An error occurred while creating the river area"
```

---

## 🔧 Customization Points

### 1. Add Authorization Policies

```csharp
[Authorize(Policy = "RiverAreaManagement")]
public class RiverAreaController : ControllerBase
{
    // ...
}
```

### 2. Add API Versioning

```csharp
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class RiverAreaController : ControllerBase
{
    // ...
}
```

### 3. Add Rate Limiting

```csharp
[EnableRateLimiting("fixed")]
public class RiverAreaController : ControllerBase
{
    // ...
}
```

### 4. Add Caching

```csharp
[ResponseCache(Duration = 60)]
[HttpGet("{id}")]
public async Task<ActionResult<RiverAreaDto>> GetById(int id)
{
    // ...
}
```

---

## 🐛 Troubleshooting

### Issue: "Service not registered"

**Error**: `Unable to resolve service for type 'IRiverAreaService'`

**Solution**: Ensure DI registration in Program.cs:
```csharp
builder.Services.AddScoped<IRiverAreaService, RiverAreaService>();
builder.Services.AddScoped<IRiverAreaRepository, RiverAreaRepository>();
```

### Issue: "Connection string not found"

**Error**: `ArgumentNullException: Value cannot be null. (Parameter 'connectionString')`

**Solution**: Add to appsettings.json:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "your-connection-string-here"
  }
}
```

### Issue: "Validation exception not caught"

**Error**: Custom `ValidationException` throws 500 instead of 400

**Solution**: Add exception filter or use built-in validation

### Issue: "Segment endpoints return 501"

**Note**: Segments are managed as part of parent RiverArea update. This is by design. Update the parent to add/remove/modify segments.

---

## 📚 Reference Implementations

### Similar Controllers in Mono Repo

**Facility Controller** (primary reference):
```
C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Api\Controllers\FacilityController.cs
```

**BoatLocation Controller** (secondary reference):
```
C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Api\Controllers\BoatLocationController.cs
```

### Similar Services

**Facility Service**:
```
C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Services\FacilityService.cs
```

---

## ✨ Next Steps

1. ✅ Copy files to target locations
2. ✅ Register services in DI
3. ✅ Build and verify compilation
4. ✅ Test endpoints with Swagger
5. ✅ Test validation rules
6. ✅ Test with Postman/curl
7. ✅ Deploy to development environment

---

## 🎓 Advanced Topics

### Implement Segment CRUD Endpoints

If you want direct segment manipulation (optional):

```csharp
[HttpPost("{id}/segments")]
public async Task<ActionResult<RiverAreaSegmentDto>> CreateSegment(int id, [FromBody] RiverAreaSegmentDto segment)
{
    segment.RiverAreaID = id;
    var segmentId = await _repository.CreateSegmentAsync(segment);
    segment.RiverAreaSegmentID = segmentId;
    return CreatedAtAction(nameof(GetSegments), new { id }, segment);
}

[HttpPut("segments/{segmentId}")]
public async Task<IActionResult> UpdateSegment(int segmentId, [FromBody] RiverAreaSegmentDto segment)
{
    await _repository.UpdateSegmentAsync(segment);
    return NoContent();
}

[HttpDelete("segments/{segmentId}")]
public async Task<IActionResult> DeleteSegment(int segmentId)
{
    await _repository.DeleteSegmentAsync(segmentId);
    return NoContent();
}
```

### Add Bulk Operations

```csharp
[HttpPost("bulk")]
public async Task<ActionResult<BulkOperationResult>> BulkCreate([FromBody] List<RiverAreaDto> riverAreas)
{
    var results = new BulkOperationResult();
    foreach (var riverArea in riverAreas)
    {
        try
        {
            var id = await _riverAreaService.CreateAsync(riverArea);
            results.SuccessCount++;
        }
        catch (ValidationException ex)
        {
            results.ErrorCount++;
            results.Errors.Add(new { riverArea.Name, Error = ex.Message });
        }
    }
    return Ok(results);
}
```

---

**API Layer Complete!** 🎉

*Generated by ClaudeOnshoreConversionAgent - 2025-12-11*
