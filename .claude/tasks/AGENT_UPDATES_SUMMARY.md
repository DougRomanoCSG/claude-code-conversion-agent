# Agent Updates Summary - Mono Shared Structure

## Overview
Updated all agents to reflect the new **BargeOps.Admin.Mono** shared project architecture where DTOs and Models are centralized in `BargeOps.Shared` instead of being duplicated in API and UI projects.

## Changes Made

### 1. Configuration Updates

#### `config.json`
**Changed:**
- Updated `targetProjects.adminApi` path from `C:\source\BargeOps\BargeOps.Admin.API` to `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API`
- Updated `targetProjects.adminUi` path from `C:\source\BargeOps\BargeOps.Admin.UI` to `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI`
- **Added** `targetProjects.shared` path: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared\BargeOps.Shared`
- Updated description to reflect "mono shared structure"

**Impact:** All agents now reference the correct mono repository paths.

---

### 2. Path Helper Updates

#### `lib/paths.ts`
**Added Functions:**
- `getSharedProjectPath()` - Returns path to BargeOps.Shared project
- `getSharedExamples()` - Returns paths to shared DTOs, Models, and Constants

**Modified Functions:**
- `getTargetProjectsForPrompt()` - Now includes BargeOps.Shared with note "(DTOs and Models)"
- `getAdminApiExamples()` - Removed `domainModels` and `dtos` (now in Shared), added `infrastructure`
- `getAdminUiExamples()` - Changed `viewModels` path from `Models` to `ViewModels`
- `getDetailedReferenceExamples()` - Completely rewritten to:
  - Lead with BargeOps.Shared section marked with ⭐
  - Add explicit warnings: "DTOs and Models are now in the SHARED project, not duplicated in API/UI!"
  - Include specific file examples from the Shared project
  - Update Admin.API and Admin.UI sections to note they reference Shared
  - Add concrete file examples for each section

**Impact:** All agents now have access to shared project paths and receive clear guidance about the shared architecture.

---

### 3. Agent Updates

#### `agents/conversion-template-generator.ts`
**Changes:**
- Added import for `getSharedProjectPath`
- Completely rewrote `systemPrompt` with:
  - **New Section:** "CRITICAL ARCHITECTURE NOTE" explaining mono shared structure
  - **New Section:** "FOR BargeOps.Shared" - Instructions to generate DTOs and Models FIRST
  - Updated file structure showing `templates/shared/` as the first folder
  - Added detailed breakdown of what goes in Shared vs API vs UI
  - Added "IMPORTANT IMPLEMENTATION ORDER" section
  - Referenced `.claude/tasks/MONO_SHARED_STRUCTURE.md` for detailed architecture

**Template Structure Changed:**
```
OLD:                          NEW:
templates/                    templates/
├── api/                      ├── shared/          ⭐ NEW FIRST!
│   ├── Facility.cs           │   └── Dto/         ⭐ DTOs are the ONLY data models
│   └── FacilityDto.cs        │       ├── {Entity}Dto.cs
└── ui/                       │       ├── {Entity}SearchRequest.cs
                              │       └── {Child}Dto.cs
                              ├── api/
                              │   ├── Controllers/
                              │   ├── Repositories/  (return DTOs directly)
                              │   └── Services/      (use DTOs, no AutoMapper)
                              └── ui/
                                  ├── Controllers/
                                  ├── Services/      (return DTOs)
                                  ├── ViewModels/    (contain DTOs)
                                  ├── Views/
                                  └── wwwroot/js/
```

**Note:** No Models/ folder - DTOs serve as the complete data model!

**Impact:** Generated templates now correctly place DTOs/Models in the Shared project first, then create API and UI files that reference them.

---

#### `agents/data-access-analyzer.ts`
**Changes:**
- Added import for `getSharedExamples`
- Added "ARCHITECTURE NOTE" section explaining shared structure
- Updated reference examples to include BargeOps.Shared DTOs
- Added note that "Repositories use Dapper with stored procedures" and "Results are mapped to DTOs from the shared project"
- Added "KEY PATTERNS TO IDENTIFY" section with stored procedure naming conventions

**Impact:** Data access analyzer now understands that query results should map to shared DTOs, not project-specific models.

---

#### `agents/form-structure-analyzer.ts`
**Changes:**
- Added import for `getSharedExamples`
- Added "ARCHITECTURE NOTE" section before reference examples
- Added "Shared DTOs (referenced by UI)" section in references
- Updated ViewModels path reference from `Models` to `ViewModels`

**Impact:** Form analyzer now knows that UI ViewModels should reference shared DTOs, not create duplicate models.

---

### 4. Documentation Created

#### `.claude/tasks/MONO_SHARED_STRUCTURE.md`
**New comprehensive documentation covering:**
- Overview of shared project architecture
- Complete project structure with annotations
- Key changes from old structure (with visual diagrams)
- Shared library details (location, dependencies, DTO patterns)
- API layer architecture (controller, service, repository patterns)
- UI layer architecture (service, viewmodel, controller patterns)
- Code generation structure
- Implementation checklist (order matters!)
- Benefits of shared structure
- Migration notes

**Impact:** Provides authoritative reference for all agents and developers about the mono shared architecture.

#### `.claude/tasks/AGENT_UPDATES_SUMMARY.md` (this file)
**Summary of all changes made to agents and configuration.**

---

## Key Architectural Principles

### Before (Duplicated)
```
API Project:
├── Models/Facility.cs        ❌ Duplicate
└── Dto/FacilityDto.cs        ❌ Duplicate

UI Project:
└── Models/FacilityDto.cs     ❌ Duplicate (copied from API)
```

### After (Shared - DTOs Only!)
```
Shared Project:
└── Dto/                      ⭐ DTOs are the ONLY data models
    ├── FacilityDto.cs        ✅ Single source of truth (used by both API and UI)
    ├── FacilityBerthDto.cs
    └── FacilitySearchRequest.cs

API Project:
└── References: BargeOps.Shared ✅
    Uses DTOs directly (no separate models, no AutoMapper)

UI Project:
└── References: BargeOps.Shared ✅
    Uses DTOs directly (no separate models)
```

---

## Implementation Order (Critical!)

When generating conversion templates, follow this order:

1. **Shared Project (FIRST)**
   - Create DTOs in `BargeOps.Shared/Dto/`
     - {Entity}Dto.cs - Complete entity with ALL fields
     - {Entity}SearchRequest.cs - Search criteria
     - {Child}Dto.cs - Child entities
   - Add `[Sortable]` and `[Filterable]` attributes
   - **NO Models/ folder** - DTOs are the data models!

2. **API Infrastructure**
   - Create repository (returns DTOs directly)
   - Create service (uses DTOs directly)
   - **NO AutoMapper needed** - repositories return DTOs!

3. **API Controller**
   - Create controller (accepts/returns DTOs)

4. **UI Services**
   - Create API client service (returns DTOs)

5. **UI Controllers & Views**
   - Create ViewModels (contain DTOs from Shared)
   - Create controller
   - Create views

---

## Benefits of This Architecture

1. **Type Safety**: Changes to DTOs propagate at compile-time to both API and UI
2. **Zero Duplication**: DTOs are the ONLY data models - no separate domain models!
3. **Simpler Code**: No AutoMapper, no mapping layers, no model conversions
4. **Consistency**: API and UI always use identical data structures
5. **Maintainability**: Update DTO once, affects all consumers instantly
6. **IntelliSense**: Better IDE support across projects
7. **Testability**: Mock DTOs work across both projects
8. **Less Code**: Fewer files, fewer abstractions, easier maintenance
9. **Performance**: No mapping overhead between layers

---

## What Wasn't Changed

The following agents don't need updates as they don't directly reference DTOs/Models:
- `orchestrator.ts` - Just coordinates other agents
- `business-logic-extractor.ts` - Extracts VB logic, doesn't generate C#
- `security-extractor.ts` - Extracts permissions, not models
- `ui-component-mapper.ts` - Maps controls, not data models
- `form-workflow-analyzer.ts` - Analyzes workflows, not models
- `detail-tab-analyzer.ts` - Analyzes tabs, not models
- `validation-extractor.ts` - Extracts validation, not models
- `related-entity-analyzer.ts` - Analyzes relationships, not models

These agents still work correctly with the new structure because they only extract information; they don't generate code that references DTOs/Models.

---

## Verification Steps

To verify the updates are working correctly:

1. **Run a full conversion:**
   ```bash
   bun run agents/orchestrator.ts --entity "TestEntity"
   ```

2. **Check output structure:**
   ```bash
   ls output/TestEntity/templates/
   # Should show: shared/, api/, ui/
   ```

3. **Generate templates:**
   ```bash
   bun run generate-template --entity "TestEntity"
   ```

4. **Verify template files:**
   ```bash
   ls output/TestEntity/templates/shared/Dto/
   # Should contain: TestEntityDto.cs, etc.
   ```

5. **Check conversion plan:**
   ```bash
   cat output/TestEntity/conversion-plan.md
   # Should mention BargeOps.Shared prominently
   ```

---

## Next Steps

1. **Test with a new entity** to ensure all agents work correctly
2. **Update existing entity conversions** to use shared structure
3. **Create migration script** to move existing DTOs to Shared project (if needed)
4. **Update CI/CD pipelines** to build Shared project first
5. **Document API versioning strategy** for shared DTOs

---

## Questions or Issues?

If you encounter any issues with the updated agents:

1. Check `.claude/tasks/MONO_SHARED_STRUCTURE.md` for architecture details
2. Verify `config.json` has correct paths
3. Ensure `C:\Dev\BargeOps.Admin.Mono` is the correct working directory
4. Review agent output for any references to old paths
5. Check that generated templates follow the new structure (shared/ first)

---

## Summary

✅ **Configuration updated** to use mono repo paths
✅ **Path helpers enhanced** with shared project support (DTOs only)
✅ **Template generator rewritten** to use DTOs as the ONLY data models
✅ **Data access analyzer updated** to return DTOs directly
✅ **Form analyzer updated** to use shared DTOs in ViewModels
✅ **Comprehensive documentation created** (.claude/tasks/MONO_SHARED_STRUCTURE.md)
✅ **Implementation order clarified** (DTOs first, no AutoMapper)
✅ **Architecture simplified** - no separate Models folder, no mapping layers

**Key Principle:** DTOs in BargeOps.Shared are the ONLY data models - used directly by both API and UI!

All agents are now aligned with the BargeOps.Admin.Mono shared project architecture! 🎉
