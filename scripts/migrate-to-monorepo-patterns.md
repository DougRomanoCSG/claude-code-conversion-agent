# Migration Guide: Update Generated Code to MonoRepo Patterns

This guide helps you update existing generated code to match the correct MonoRepo architecture patterns.

## 🚨 Before You Start

1. **Backup your code** - Make a git commit or backup before running migrations
2. **Review patterns** - Read `ARCHITECTURE_PATTERNS_REFERENCE.md`
3. **Test after migration** - Ensure code compiles and tests pass

---

## Migration Checklist

Use this checklist for each entity you need to migrate:

### ✅ API Project Migrations

#### Step 1: Update API Controller Base Class

**Find and replace in**: `output/{Entity}/templates/api/Controllers/{Entity}Controller.cs`

**WRONG:**
```csharp
public class {Entity}Controller : ControllerBase
{
    private readonly I{Entity}Service _{entity}Service;
    private readonly ILogger<{Entity}Controller> _logger;

    public {Entity}Controller(I{Entity}Service {entity}Service, ILogger logger)
```

**CORRECT:**
```csharp
public class {Entity}Controller : ApiControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public {Entity}Controller(
        IListRequestValidator validator,
        IObjectMapper mapper,
        IUnitOfWork unitOfWork) : base(validator, mapper)
```

**Required Changes:**
1. Change base class: `ControllerBase` → `ApiControllerBase`
2. Change injection: `I{Entity}Service` → `IUnitOfWork`
3. Add constructor parameters: `IListRequestValidator validator, IObjectMapper mapper`
4. Call base constructor: `: base(validator, mapper)`
5. Update using statements:
   ```csharp
   using Admin.Api.Interfaces;
   using Admin.Infrastructure.Mapping;
   using Csg.ListQuery.AspNetCore;
   ```

#### Step 2: Update Controller Methods

**WRONG:**
```csharp
var result = await _{entity}Service.SearchAsync(request);
```

**CORRECT:**
```csharp
var list = await HandleListRequestAsync<{Entity}Dto, {Entity}Dto, {Entity}>(
    model, _unitOfWork.{Entity}Repository.Get{Entity}List);
```

**Required Changes:**
1. Use `HandleListRequestAsync<>()` helper for list operations
2. Access repository via `_unitOfWork.{Entity}Repository`
3. Use `_mapper.Create<>()` and `_mapper.Map<>()` for conversions
4. Use `HttpContext.GetForwardedUserOrIdentityName()` for audit fields

#### Step 3: Remove API Service Layer

**Delete these files if they exist:**
- `output/{Entity}/templates/api/Services/I{Entity}Service.cs`
- `output/{Entity}/templates/api/Services/{Entity}Service.cs`

**Reason:** Services belong ONLY in UI project, not API project.

#### Step 4: Update Repository to Use IDbHelper

**Find and replace in**: `output/{Entity}/templates/api/Repositories/{Entity}Repository.cs`

**WRONG:**
```csharp
private readonly IDbConnection _connection;

public {Entity}Repository(IDbConnection connection)
{
    _connection = connection;
}

var newID = await _connection.ExecuteScalarAsync<int>(
    "INSERT INTO...", entity);
```

**CORRECT:**
```csharp
private readonly IDbHelper _dbConnection;

public {Entity}Repository(IDbHelper dbConnection)
{
    _dbConnection = dbConnection;
}

var newID = await _dbConnection.ExecuteScalarAsync<int>(
    SqlText.Create{Entity}, entity);
```

**Required Changes:**
1. Change type: `IDbConnection` → `IDbHelper`
2. Change parameter name: `connection` → `dbConnection`
3. Use `SqlText.{PropertyName}` instead of hard-coded SQL

#### Step 5: Organize SQL Files

**Current structure (WRONG):**
```
output/{Entity}/templates/api/Sql/
  {Entity}_Insert.sql
  {Entity}_Update.sql
  {Entity}_GetById.sql
```

**Target structure (CORRECT):**
```
output/{Entity}/templates/api/Sql/{Entity}/
  {Entity}_Insert.sql
  {Entity}_Update.sql
  {Entity}_GetById.sql
  {Entity}_Search.sql
  {Entity}_SetActive.sql
```

**Migration steps:**
1. Create subfolder: `mkdir output/{Entity}/templates/api/Sql/{Entity}`
2. Move all SQL files: `mv output/{Entity}/templates/api/Sql/*.sql output/{Entity}/templates/api/Sql/{Entity}/`
3. Delete old Sql folder if empty

#### Step 6: Add SqlText Properties

**Create/Update**: `output/{Entity}/templates/api/DataAccess/SqlText.cs`

```csharp
namespace Admin.Infrastructure.DataAccess
{
    public static partial class SqlText
    {
        // {Entity} CRUD
        public static string Create{Entity} => Get("{Entity}.{Entity}_Insert");
        public static string Get{Entity}List => Get("{Entity}.{Entity}_Search");
        public static string Get{Entity}ById => Get("{Entity}.{Entity}_GetById");
        public static string Update{Entity} => Get("{Entity}.{Entity}_Update");
        public static string Set{Entity}Active => Get("{Entity}.{Entity}_SetActive");

        private static string Get(string fileName)
        {
            return Admin.Infrastructure.SqlText.Get(fileName);
        }
    }
}
```

---

### ✅ UI Project Migrations

#### Step 7: Update UI Controller Constructor

**Find and replace in**: `output/{Entity}/templates/ui/Controllers/{Entity}SearchController.cs`

**WRONG:**
```csharp
public {Entity}SearchController(
    I{Entity}Service {entity}Service,
    AppSession appSession) : base(appSession)
{
}
```

**CORRECT:**
```csharp
public {Entity}SearchController(
    I{Entity}Service {entity}Service,
    AppSession appSession,
    ICurrentUserService currentUser) : base(appSession, currentUser)
{
}
```

**Required Changes:**
1. Add parameter: `ICurrentUserService currentUser`
2. Pass to base: `: base(appSession, currentUser)`

#### Step 8: Update DataTables Pattern

**Find and replace in**: `output/{Entity}/templates/ui/Controllers/{Entity}SearchController.cs`

**WRONG:**
```csharp
[HttpPost("{Entity}Table")]
public async Task<IActionResult> {Entity}Table(
    DataTableRequest request,
    {Entity}SearchViewModel model)
{
    var searchRequest = model.ToSearchRequest();
    searchRequest.Start = request.Start;
}
```

**CORRECT:**
```csharp
[HttpPost("{Entity}Table")]
public async Task<IActionResult> {Entity}Table()
{
    var listRequest = GetListRequestFromFilter<{Entity}SearchModel>();

    var searchModel = new {Entity}SearchModel
    {
        PropertyName = Request.Form["propertyName"].FirstOrDefault(),
        IsActive = bool.TryParse(Request.Form["isActive"].FirstOrDefault(), out var active)
            ? active : (bool?)null
    };

    var searchValue = Request.Form["search[value]"].FirstOrDefault();
    var searchPropertySelector = SearchAllModelProperties<{Entity}SearchModel>();

    var model = await _{entity}Service.Search{Entity}s(
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
```

**Required Changes:**
1. Remove `DataTableRequest` parameter
2. Use `GetListRequestFromFilter<T>()` helper
3. Extract form values manually
4. Use `SearchAllModelProperties<T>()` for global search
5. Return DataTables format with draw, data, recordsFiltered, recordsTotal

#### Step 9: Add InitSessionVariables Call

**Add to every action method:**

```csharp
public async Task<IActionResult> Index()
{
    await InitSessionVariables(_appSession);  // ← Add this line

    // ... rest of method
}
```

---

## Automated Migration Script

### Quick Fix Script (PowerShell)

```powershell
# Run this script to automatically fix common issues
param(
    [Parameter(Mandatory=$true)]
    [string]$Entity
)

$outputPath = "output/$Entity/templates"

Write-Host "Migrating $Entity to MonoRepo patterns..." -ForegroundColor Cyan

# Step 1: Reorganize SQL files
$sqlPath = "$outputPath/api/Sql"
$entitySqlPath = "$outputPath/api/Sql/$Entity"

if (Test-Path $sqlPath) {
    if (-not (Test-Path $entitySqlPath)) {
        New-Item -ItemType Directory -Path $entitySqlPath -Force | Out-Null
    }

    Get-ChildItem "$sqlPath/*.sql" | ForEach-Object {
        Move-Item $_.FullName $entitySqlPath -Force
        Write-Host "  ✓ Moved: $($_.Name)" -ForegroundColor Green
    }
}

# Step 2: Update API Controller
$apiController = "$outputPath/api/Controllers/${Entity}Controller.cs"
if (Test-Path $apiController) {
    $content = Get-Content $apiController -Raw

    # Replace ControllerBase with ApiControllerBase
    $content = $content -replace 'public class (\w+)Controller : ControllerBase', 'public class $1Controller : ApiControllerBase'

    # Add required using statements
    if ($content -notmatch 'using Admin.Api.Interfaces;') {
        $content = $content -replace '(using Microsoft.AspNetCore.Mvc;)', '$1`nusing Admin.Api.Interfaces;`nusing Admin.Infrastructure.Mapping;`nusing Csg.ListQuery.AspNetCore;'
    }

    Set-Content $apiController $content
    Write-Host "  ✓ Updated: API Controller" -ForegroundColor Green
}

# Step 3: Update Repository
$repository = "$outputPath/api/Repositories/${Entity}Repository.cs"
if (Test-Path $repository) {
    $content = Get-Content $repository -Raw

    # Replace IDbConnection with IDbHelper
    $content = $content -replace 'IDbConnection _connection', 'IDbHelper _dbConnection'
    $content = $content -replace 'IDbConnection connection', 'IDbHelper dbConnection'
    $content = $content -replace '_connection\.', '_dbConnection.'

    Set-Content $repository $content
    Write-Host "  ✓ Updated: Repository" -ForegroundColor Green
}

# Step 4: Update UI Controller
$uiController = "$outputPath/ui/Controllers/${Entity}SearchController.cs"
if (Test-Path $uiController) {
    $content = Get-Content $uiController -Raw

    # Add ICurrentUserService parameter
    if ($content -match 'base\(appSession\)') {
        $content = $content -replace 'AppSession appSession\)', 'AppSession appSession,`n        ICurrentUserService currentUser)'
        $content = $content -replace 'base\(appSession\)', 'base(appSession, currentUser)'
    }

    Set-Content $uiController $content
    Write-Host "  ✓ Updated: UI Controller" -ForegroundColor Green
}

Write-Host "`n✅ Migration complete for $Entity" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "  1. Review changes in: $outputPath"
Write-Host "  2. Verify against ARCHITECTURE_PATTERNS_REFERENCE.md"
Write-Host "  3. Run deploy script: bun run scripts/deploy-templates.ts --entity $Entity"
```

**Usage:**
```powershell
.\scripts\migrate-to-monorepo-patterns.ps1 -Entity "Vendor"
```

---

## Manual Verification

After migration, verify each file:

### API Controller Checklist
- [ ] Inherits from `ApiControllerBase`
- [ ] Constructor: `(IListRequestValidator, IObjectMapper, IUnitOfWork)`
- [ ] Uses `HandleListRequestAsync<>()` for lists
- [ ] Uses `_unitOfWork.{Entity}Repository` pattern
- [ ] Uses `_mapper.Create<>()` for conversions
- [ ] Uses `HttpContext.GetForwardedUserOrIdentityName()`

### Repository Checklist
- [ ] Injects `IDbHelper` (not `IDbConnection`)
- [ ] Uses `SqlText.{PropertyName}` for SQL
- [ ] SQL files in `Sql/{Entity}/` folder
- [ ] Has validation helper methods

### UI Controller Checklist
- [ ] Constructor includes `ICurrentUserService`
- [ ] Calls `base(appSession, currentUser)`
- [ ] Uses `GetListRequestFromFilter<T>()`
- [ ] Uses `SearchAllModelProperties<T>()`
- [ ] Calls `InitSessionVariables()` in actions
- [ ] Returns correct DataTables format

---

## Testing After Migration

### 1. Compile Test
```bash
cd C:/Dev/BargeOps.Admin.Mono
dotnet build
```

### 2. Run Unit Tests
```bash
dotnet test
```

### 3. Manual Testing
- [ ] Search page loads
- [ ] DataTables filtering works
- [ ] Create/Edit forms work
- [ ] Validation triggers correctly
- [ ] Authorization enforced

---

## Rollback Plan

If migration fails:

```bash
# Restore from git
git checkout output/{Entity}/templates/

# Or restore from backup
cp -r output/{Entity}/templates.backup output/{Entity}/templates/
```

---

## Getting Help

If you encounter issues:

1. **Review**: `ARCHITECTURE_PATTERNS_REFERENCE.md`
2. **Compare**: Against working MonoRepo examples (Barge, Customer, etc.)
3. **Check**: Common mistakes section in architecture docs
4. **Verify**: All checklist items above

---

**Remember**: Every entity must follow these patterns EXACTLY. No exceptions.
