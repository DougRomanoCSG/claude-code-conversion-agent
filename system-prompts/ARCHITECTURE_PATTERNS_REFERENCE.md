# MonoRepo Architecture Patterns Reference
## For Use in All Code Generation Agents

**CRITICAL**: This document defines the ONLY correct patterns for generating code based on **Crewing.API** implementation.

**Last Updated**: 2026-01-15
**Source**: Based on actual C:\source\BargeOps\BargeOps.Crewing.API implementation

---

## 🚨 CRITICAL API PATTERNS

### ✅ CORRECT Architecture Layering

**RULE**: Controllers → Services → Repositories → IUnitOfWork/IDbHelper

```
┌─────────────┐
│ Controller  │  ← Handles HTTP, routing, authorization
└─────┬───────┘
      │ injects I{Entity}Service
      ▼
┌─────────────┐
│  Service    │  ← Business logic, validation, orchestration
└─────┬───────┘
      │ injects I{Entity}Repository
      ▼
┌─────────────┐
│ Repository  │  ← Data access, SQL queries
└─────┬───────┘
      │ injects IUnitOfWork or IDbHelper
      ▼
┌─────────────┐
│  Database   │
└─────────────┘
```

**❌ WRONG**: Controllers inject IUnitOfWork or repositories directly
**✅ CORRECT**: Controllers inject Services ONLY

### API Controller Pattern (CORRECT)

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Admin.Infrastructure.Services;  // Service namespace
using BargeOps.Shared.Dto;

namespace Admin.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class {EntityName}Controller : ControllerBase
    {
        private readonly I{EntityName}Service _{entityName}Service;
        private readonly ILogger<{EntityName}Controller> _logger;

        public {EntityName}Controller(
            I{EntityName}Service {entityName}Service,
            ILogger<{EntityName}Controller> logger)
        {
            _{entityName}Service = {entityName}Service;
            _logger = logger;
        }

        [ProducesResponseType(200)]
        [HttpPost("{entityName}Filter", Name = "List{EntityName}ByPost")]
        public async Task<ActionResult<ListResponse<{EntityName}Dto>>> ListPost(ListRequest model)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var list = await HandleListRequestAsync<{EntityName}Dto, {EntityName}Dto, {EntityName}>(
                model, _unitOfWork.{EntityName}Repository.Get{EntityName}List);

            return list;
        }

        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<{EntityName}Dto>> Post([FromBody] {EntityName}Dto model)
        {
            try
            {
                var result = await _{entityName}Service.Create{EntityName}(
                    model, HttpContext.GetForwardedUserOrIdentityName());
                return CreatedAtAction("Get", new { id = result.{EntityName}ID }, result);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<{EntityName}Dto>> Put(int id, [FromBody] {EntityName}Dto model)
        {
            try
            {
                var result = await _{entityName}Service.Update{EntityName}(
                    model, HttpContext.GetForwardedUserOrIdentityName());
                return Ok(result);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<{EntityName}Dto>> Get(int id)
        {
            try
            {
                var model = await _{entityName}Service.Get{EntityName}ById(id);
                if (model == null)
                    return NotFound();
                return Ok(model);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
```

**✅ DO:**
- Inherit from `ApiControllerBase` (defined in API project as abstract class inheriting from ControllerBase)
- Inject `IUnitOfWork`, `IListRequestValidator`, `IObjectMapper`, and `I{EntityName}Service`
- Use `_unitOfWork.{EntityName}Repository` for list operations
- Use `_{entityName}Service` for create/update/get operations
- Use `HandleListRequestAsync<>()` helper for list operations
- Use `HttpContext.GetForwardedUserOrIdentityName()` for audit fields
- **Services in API project ARE ALLOWED** for business logic

**❌ DO NOT:**
- Inherit from `ControllerBase` directly (inherit from ApiControllerBase)
- Skip service injection when business logic is needed
- Use custom logic instead of base class helpers for lists

---

## 🚨 CRITICAL REPOSITORY PATTERNS

### Repository Pattern (CORRECT - Based on Crewing.API)

**Key Change**: Use `DbHelper` concrete class (NOT `IDbHelper` interface)

```csharp
using Admin.Infrastructure.Abstractions;
using Admin.Infrastructure.Exceptions;
using Admin.Infrastructure.Models;
using Admin.Infrastructure.DataAccess;
using Csg.Data;
using Csg.ListQuery;
using Csg.ListQuery.Sql;

namespace Admin.Infrastructure.Repositories;

public class {EntityName}Repository : I{EntityName}Repository
{
    private readonly DbHelper _db;  // ← Concrete class, NOT interface

    public {EntityName}Repository(DbHelper dbConnection)
    {
        _db = dbConnection;
    }

    public async Task<{EntityName}> Create{EntityName}({EntityName} entity)
    {
        var newID = await _db.ExecuteScalarAsync<int>(
            SqlText.Create{EntityName}, entity);

        entity.{EntityName}ID = newID;
        return entity;
    }

    public async Task<ListQueryResult<{EntityName}>> Get{EntityName}ListAsync(
        ListQueryDefinition queryDefinition)
    {
        var sql = SqlText.Get{EntityName}List;
        var query = await _db.CreateQueryBuilderAsync(sql);

        var result = await query.ListQuery(queryDefinition)
            .ValidateWith<{EntityName}>()
            .GetResultAsync<{EntityName}>();

        return result;
    }

    public async Task<{EntityName}> Find{EntityName}Async(int {entityName}ID)
    {
        var entity = await _db.QuerySingleOrDefaultAsync<{EntityName}>(
            SqlText.Get{EntityName}ById,
            new { {EntityName}ID = {entityName}ID });

        return entity;
    }

    public async Task<{EntityName}> Update{EntityName}Async({EntityName} entity)
    {
        var rowCount = await _db.ExecuteAsync(
            SqlText.Update{EntityName}, entity);

        if (rowCount == 0)
            throw new RepositoryItemNotFoundException();

        return entity;
    }
}
```

**✅ DO:**
- Inject `DbHelper` concrete class (NOT `IDbHelper` interface)
- Use variable name `_db` (NOT `_dbConnection`)
- Use `SqlText.{PropertyName}` for SQL loading
- Throw `RepositoryItemNotFoundException` on not found
- Add "Async" suffix to method names (e.g., `Find{EntityName}Async`)
- ExecuteScalarAsync parameter is `object` (NOT `UnitTowBase`)

**❌ DO NOT:**
- Use `IDbHelper` interface (use concrete `DbHelper`)
- Use `IDbConnection` directly
- Hard-code SQL strings
- Forget "Async" suffix on async methods

---

## 🚨 CRITICAL SQL PATTERNS

### SqlText Implementation

**File: `Admin.Infrastructure/SqlText.cs`**
```csharp
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Admin.Infrastructure
{
    public static partial class SqlText
    {
        private static Assembly DataAssembly { get; set; }
        private static string[] ResourceNames { get; set; }

        static SqlText()
        {
            DataAssembly = typeof(SqlText).Assembly;
            ResourceNames = DataAssembly.GetManifestResourceNames();
        }

        public static string Get(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return string.Empty;

            var fullMatchingResourceNames = ResourceNames.Where(name =>
                name.EndsWith($".{fileName}.sql"));

            if (fullMatchingResourceNames.Count() == 0)
                throw new ArgumentException($"SQL resource not found: {fileName}");

            if (fullMatchingResourceNames.Count() > 1)
                throw new AmbiguousMatchException($"Multiple SQL resources match: {fileName}");

            var stream = DataAssembly.GetManifestResourceStream(fullMatchingResourceNames.First());
            if (stream == null)
                throw new InvalidOperationException($"Could not load embedded resource");

            using (stream)
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
```

**File: `Admin.Infrastructure/DataAccess/SqlText.cs`**
```csharp
namespace Admin.Infrastructure.DataAccess
{
    public static partial class SqlText
    {
        // {EntityName} CRUD
        public static string Create{EntityName} => Get("{EntityName}.{EntityName}_Insert");
        public static string Get{EntityName}List => Get("{EntityName}.{EntityName}_Search");
        public static string Get{EntityName}ById => Get("{EntityName}.{EntityName}_GetById");
        public static string Update{EntityName} => Get("{EntityName}.{EntityName}_Update");
        public static string SetBargeActive => Get("{EntityName}.{EntityName}_SetActive");

        // Child collections
        public static string Get{ChildEntity}sByParentId => Get("{EntityName}.{ChildEntity}_GetBy{EntityName}Id");

        private static string Get(string fileName)
        {
            return Admin.Infrastructure.SqlText.Get(fileName);
        }
    }
}
```

### SQL File Organization
```
Admin.Infrastructure/
  DataAccess/
    SqlText.cs                          ← Property definitions
    Sql/
      {EntityName}/                     ← One folder per entity
        {EntityName}_Insert.sql
        {EntityName}_Update.sql
        {EntityName}_GetById.sql
        {EntityName}_Search.sql
        {EntityName}_SetActive.sql
        {ChildEntity}_GetBy{EntityName}Id.sql
```

**✅ DO:**
- Organize SQL in entity subfolders: `Sql/{EntityName}/*.sql`
- Use naming pattern: `Get("{EntityName}.{FileName}")`
- Mark SQL files as embedded resources in .csproj

**❌ DO NOT:**
- Put all SQL files in root `Sql/` folder
- Hard-code SQL in C# strings
- Skip entity subfolder

---

## 🚨 CRITICAL UI PATTERNS

### UI Search Controller Pattern (CORRECT)

```csharp
using BargeOpsAdmin.ViewModels;
using BargeOpsAdmin.AppClasses;
using BargeOpsAdmin.Services;
using Microsoft.AspNetCore.Mvc;
using CsgAuthorization.AspNetCore.Handlers;
using CsgAuthorization.Core.Enums;
using BargeOpsAdmin.Enums;

namespace BargeOpsAdmin.Controllers
{
    [Route("[controller]")]
    public class {EntityName}SearchController : AppController
    {
        private readonly I{EntityName}Service _{entityName}Service;
        private readonly AppSession _appSession;

        public {EntityName}SearchController(
            I{EntityName}Service {entityName}Service,
            AppSession appSession,
            ICurrentUserService currentUser) : base(appSession, currentUser)
        {
            _{entityName}Service = {entityName}Service;
            _appSession = appSession;
        }

        [RequirePermission<AuthPermissions>(AuthPermissions.{EntityName}Management,
            PermissionAccessType.ReadOnly)]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(bool isActive = true)
        {
            if (!User.Identity?.IsAuthenticated == true)
                return View("Unauthorized");

            await InitSessionVariables(_appSession);

            var model = new {EntityName}SearchModel
            {
                IsActive = isActive
            };

            return View("Index", model);
        }

        [RequirePermission<AuthPermissions>(AuthPermissions.{EntityName}Management,
            PermissionAccessType.ReadOnly)]
        [HttpPost("{EntityName}Table")]
        public async Task<IActionResult> {EntityName}Table()
        {
            if (!User.Identity?.IsAuthenticated == true)
                return Unauthorized();

            var listRequest = GetListRequestFromFilter<{EntityName}SearchModel>();

            var searchModel = new {EntityName}SearchModel
            {
                // Extract form values
                PropertyName = Request.Form["propertyName"].FirstOrDefault(),
                IsActive = bool.TryParse(Request.Form["isActive"].FirstOrDefault(), out var active)
                    ? active : (bool?)null
            };

            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            var searchPropertySelector = SearchAllModelProperties<{EntityName}SearchModel>();

            var model = await _{entityName}Service.Search{EntityName}s(
                searchModel, listRequest, searchPropertySelector, searchValue);

            var total = model.Meta?.TotalCount ?? model.Results?.Count ?? 0;

            return Ok(new
            {
                draw = Request.Form["draw"].FirstOrDefault(),
                data = model.Results,
                recordsFiltered = total,
                recordsTotal = total
            });
        }
    }
}
```

**✅ DO:**
- Inherit from `AppController`
- Inject `IService`, `AppSession`, `ICurrentUserService`
- Call `base(appSession, currentUser)` in constructor
- Use `GetListRequestFromFilter<T>()` for DataTables
- Use `SearchAllModelProperties<T>()` for global search
- Call `await InitSessionVariables(_appSession)` in each action

**❌ DO NOT:**
- Forget `ICurrentUserService` parameter
- Skip `InitSessionVariables()` call
- Use custom `DataTableRequest` class
- Omit `currentUser` in base constructor call

---

## 🚨 CRITICAL PROJECT STRUCTURE

### Services in API Project (ALLOWED - Based on Crewing.API)

**Key Change**: Services ARE ALLOWED in API project for business logic

```
✅ CORRECT (Crewing.API Pattern):
BargeOps.API/
  src/
    Admin.Api/
      Services/              ← Services ARE ALLOWED
        I{EntityName}Service.cs
        {EntityName}Service.cs
      Interfaces/
        IUnitOfWork.cs

BargeOps.UI/
  Services/                  ← Services also in UI project for UI logic
    I{EntityName}Service.cs
    {EntityName}Service.cs
```

**Pattern Explanation**:
- API Services handle business logic and orchestrate repository calls
- UI Services handle UI-specific logic and call API services
- Controllers delegate business logic to services
- Services use repositories for data access

### Namespace Patterns

**API:**
- Controllers: `Admin.Api.Controllers`
- Repositories: `Admin.Infrastructure.Repositories`
- Repository Interfaces: `Admin.Infrastructure.Abstractions`
- Models: `Admin.Infrastructure.Models`
- SQL PropertyDefinitions: `Admin.Infrastructure.DataAccess`
- SQL Base Loader: `Admin.Infrastructure`

**UI:**
- Controllers: `BargeOpsAdmin.Controllers`
- ViewModels: `BargeOpsAdmin.ViewModels`
- Services: `BargeOpsAdmin.Services`

**Shared:**
- DTOs: `BargeOps.Shared.Dto.Admin`

---

## 📋 VERIFICATION CHECKLIST

### API Code Generation ✅ (Crewing.API Pattern)
- [ ] Controller inherits from `ApiControllerBase`
- [ ] Constructor: `(IListRequestValidator, IObjectMapper, IUnitOfWork, I{EntityName}Service)`
- [ ] Uses `_unitOfWork.{EntityName}Repository` for list operations
- [ ] Uses `_{entityName}Service` for create/update/get operations
- [ ] Uses `HandleListRequestAsync<>()` helper for lists
- [ ] Uses `HttpContext.GetForwardedUserOrIdentityName()` for audit
- [ ] Service layer in API project IS ALLOWED for business logic

### Repository Code Generation ✅ (Crewing.API Pattern)
- [ ] Injects `DbHelper` concrete class (NOT `IDbHelper` interface)
- [ ] Uses variable name `_db` (NOT `_dbConnection`)
- [ ] Uses `SqlText.{PropertyName}` for SQL loading
- [ ] SQL files organized in `DataAccess/{EntityName}/` or `Sql/{EntityName}/` folder
- [ ] SQL files marked as embedded resources
- [ ] Method names include "Async" suffix (e.g., `Find{EntityName}Async`)

### UI Code Generation ✅
- [ ] Controller inherits from `AppController`
- [ ] Constructor: `(IService, AppSession, ICurrentUserService)`
- [ ] Calls `base(appSession, currentUser)` in constructor
- [ ] Uses `GetListRequestFromFilter<T>()` for DataTables
- [ ] Uses `SearchAllModelProperties<T>()` for search
- [ ] Calls `await InitSessionVariables(_appSession)` in actions
- [ ] Returns correct DataTables format

### SQL Organization ✅
- [ ] SQL files in `Sql/{EntityName}/` subfolder
- [ ] SqlText properties use `Get("{EntityName}.{FileName}")`
- [ ] Files marked as `<EmbeddedResource>` in .csproj
- [ ] SqlText partial classes in correct namespaces

---

## 🔥 COMMON MISTAKES TO AVOID

| Mistake | Correct Pattern (Crewing.API) |
|---------|----------------|
| Inherit from `ControllerBase` directly | Inherit from `ApiControllerBase` |
| Only inject `IService` | Inject `IUnitOfWork` + `I{Entity}Service` |
| `IDbHelper` interface | `DbHelper` concrete class |
| Variable name `_dbConnection` | Variable name `_db` |
| Hard-coded SQL | `SqlText.{PropertyName}` |
| `Sql/*.sql` | `DataAccess/{Entity}/*.sql` or `Sql/{Entity}/*.sql` |
| Missing `ICurrentUserService` | Always include in UI controllers |
| Custom `DataTableRequest` | Use `GetListRequestFromFilter<T>()` |
| Avoid services in API | Services ARE ALLOWED for business logic |
| Skip `InitSessionVariables()` | Call in every UI action |
| Missing "Async" suffix | Add "Async" to async methods |
| ExecuteScalarAsync param: `UnitTowBase` | ExecuteScalarAsync param: `object` |

---

**This document MUST be referenced when generating ANY code for the MonoRepo.**
