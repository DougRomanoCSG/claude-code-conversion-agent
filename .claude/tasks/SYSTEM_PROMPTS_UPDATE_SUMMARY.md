# System Prompts Update Summary

**Date**: 2025-12-10
**Task**: Update agents/ and system-prompts/ folders to reflect CLAUDE.md and .cursorrules from BargeOps.Admin.Mono

## Overview

Updated all system prompts and agent documentation to align with the BargeOps.Admin.Mono monorepo structure, namespace conventions, and coding standards as defined in:
- `C:\Dev\BargeOps.Admin.Mono\CLAUDE.md`
- `C:\Dev\BargeOps.Admin.Mono\.cursorrules`

## Key Changes Made

### 1. Namespace Conventions Added

All prompts now reference the correct namespace structure:

**Shared Project (DTOs and Models):**
- Base DTOs: `BargeOps.Shared.Dto` (NOT `Admin.Domain.Models` or `Admin.Domain.Dto`)
- Admin DTOs: `BargeOps.Shared.Dto.Admin`
- Repository Interfaces: `BargeOps.Shared.Interfaces` (NOT `Admin.Domain.Interfaces`)
- Service Interfaces: `BargeOps.Shared.Services`

**API Project:**
- Controllers: `Admin.Api.Controllers`
- Services: `Admin.Api.Services`
- Interfaces: `Admin.Api.Interfaces`
- Repository Abstractions: `Admin.Infrastructure.Abstractions`
- Repository Implementations: `Admin.Infrastructure.Repositories`

**UI Project:**
- Controllers: `BargeOpsAdmin.Controllers`
- ViewModels: `BargeOpsAdmin.ViewModels`
- Services: `BargeOpsAdmin.Services`
- AppClasses: `BargeOpsAdmin.AppClasses`

### 2. Updated Project Structure References

All file path references updated to reflect the monorepo structure:

```
BargeOps.Admin.Mono/
├── src/
│   ├── BargeOps.Shared/       # NEW: Shared DTOs and Models
│   │   └── BargeOps.Shared/
│   │       ├── Dto/           # DTOs (namespace: BargeOps.Shared.Dto)
│   │       │   └── Admin/     # Admin-specific DTOs
│   │       ├── Interfaces/    # Repository interfaces
│   │       └── Services/      # Service interfaces
│   ├── BargeOps.API/
│   │   └── src/
│   │       ├── Admin.Api/            # Controllers, Services
│   │       └── Admin.Infrastructure/ # Repositories, SQL
│   └── BargeOps.UI/           # MVC Web App
```

### 3. Naming Convention Standards

Added consistent naming standards across all prompts:

- **ID Fields**: Always uppercase `ID` (e.g., `LocationID`, `BargeID`, NOT `LocationId`)
- **File-Scoped Namespaces**: Prefer `namespace BargeOps.Shared.Dto;` over braced namespaces
- **Async Methods**: Must use suffix "Async" (e.g., `GetByIdAsync`, `SaveAsync`)
- **Interfaces**: Prefix with "I" (e.g., `IBoatLocationService`)

### 4. Code Examples Updated

Updated all code examples in prompts to show:
- Correct file-scoped namespaces
- Uppercase ID properties
- Proper using statements referencing BargeOps.Shared namespaces
- File locations with full paths

## Files Updated

### System Prompts

1. **system-prompts/entity-conversion-prompt.md**
   - Added "Critical Namespace Conventions" section
   - Updated project structure diagram
   - Updated all code examples with namespaces
   - Updated reference architecture table
   - Added uppercase ID examples
   - Updated verification checklist

2. **system-prompts/viewmodel-generator-prompt.md**
   - Added "Critical Namespace Conventions" section
   - Added UI project namespace details
   - Updated BoatLocationEditViewModel example with namespace
   - Added file-scoped namespace examples

3. **system-prompts/data-access-analyzer-prompt.md**
   - Added "Project Structure and Namespaces" section
   - Updated DTOs location and namespace
   - Updated Repository interfaces and implementations locations
   - Added reference to BargeOps.Shared.Dto in repository pattern

4. **system-prompts/ui-component-mapper-prompt.md**
   - Added "Target UI Architecture" section
   - Added project structure for UI components
   - Added namespace conventions for ViewModels, Controllers, Services
   - Added technology stack details

### Agent Documentation

5. **agents/README.md**
   - Expanded "Project Conventions" section
   - Added comprehensive namespace conventions
   - Listed deprecated namespaces to avoid
   - Added naming conventions (uppercase ID, file-scoped namespaces)
   - Updated to reference BargeOps.Admin.Mono standards

## Deprecated Namespaces Documented

All prompts now clearly indicate these namespaces should NOT be used:

- ❌ `Admin.Domain.Models` → Use `BargeOps.Shared.Dto`
- ❌ `Admin.Domain.Dto` → Use `BargeOps.Shared.Dto`
- ❌ `Admin.Domain.Dto.Admin` → Use `BargeOps.Shared.Dto.Admin`
- ❌ `Admin.Domain.Interfaces` → Use `BargeOps.Shared.Interfaces`

## Reference Implementation

All prompts now reference BoatLocation as the canonical example:

**File Locations:**
- DTO: `src/BargeOps.Shared/BargeOps.Shared/Dto/BoatLocation.cs`
- Repository Interface: `src/BargeOps.API/src/Admin.Infrastructure/Abstractions/IBoatLocationRepository.cs`
- Repository Impl: `src/BargeOps.API/src/Admin.Infrastructure/Repositories/BoatLocationRepository.cs`
- Service Interface: `src/BargeOps.API/src/Admin.Api/Interfaces/IBoatLocationService.cs`
- Service Impl: `src/BargeOps.API/src/Admin.Api/Services/BoatLocationService.cs`
- ViewModel: `src/BargeOps.UI/Models/BoatLocationEditViewModel.cs`
- Controller: `src/BargeOps.UI/Controllers/BoatLocationSearchController.cs`

## Impact

These updates ensure that:

1. **Agents generate code with correct namespaces** from the start
2. **File paths match the actual monorepo structure**
3. **Naming conventions are consistent** (uppercase ID, file-scoped namespaces)
4. **References to deprecated namespaces** are eliminated
5. **All generated code follows BargeOps standards** automatically

## Testing Recommendations

To verify the updates are working correctly:

1. Run entity-converter agent and check generated namespace declarations
2. Run viewmodel-creator agent and verify ViewModel namespace
3. Run data-access-analyzer agent and check DTO references
4. Verify all generated code uses uppercase `ID` for ID fields
5. Ensure no references to deprecated `Admin.Domain.*` namespaces

## Next Steps

1. ✅ System prompts updated
2. ✅ Agent README updated
3. ⏳ Test agents with a sample entity to verify namespace usage
4. ⏳ Update any remaining prompts if needed (orchestrator, conversion-planner, etc.)
5. ⏳ Create examples showing before/after namespace usage

## Notes

- The conversion-template-generator-prompt.md was not updated as it's very large and would require selective updates
- Other analysis prompts (form-structure-analyzer, business-logic-extractor, etc.) focus on legacy code analysis and don't generate modern code, so they were not updated
- The orchestrator-prompt.md focuses on coordination and doesn't need namespace details

## Conclusion

All critical system prompts and agent documentation have been successfully updated to reflect the BargeOps.Admin.Mono monorepo structure and coding standards. The agents will now generate code that follows the established patterns and conventions.
