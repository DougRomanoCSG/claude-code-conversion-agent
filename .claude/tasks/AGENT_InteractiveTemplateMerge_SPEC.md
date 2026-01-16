# Interactive Template Merge Agent - Specification

**Agent Name**: `interactive-template-merge`
**Purpose**: Intelligently merge generated code templates with existing implementations, preserving custom logic while incorporating new features.
**Priority**: High (Customer module has existing implementation)
**Complexity**: High (requires code parsing, diff analysis, interactive prompts)

---

## Problem Statement

When converting entities like Customer that already have partial implementations, we need to:
1. **Merge generated API endpoints** with existing CustomerController.cs
2. **Add missing methods** without overwriting custom business logic
3. **Update DTOs** while preserving custom properties
4. **Merge ViewModels** and Views with existing implementations
5. **Preserve developer customizations** (comments, validation logic, etc.)

Current `deploy-templates.ts` only does **copy** or **skip**. We need **intelligent merge**.

---

## Use Cases

### Use Case 1: Merging API Controller Methods
**Scenario**: Generated CustomerController.cs has new endpoint `GetCustomersByRegion()` but existing CustomerController.cs already has custom methods.

**Current Behavior** (deploy-templates.ts with `--skip-existing`):
- Skips entire file → New endpoint is never added

**Desired Behavior** (interactive-merge agent):
1. Detect that CustomerController.cs exists
2. Parse both files (AST analysis)
3. Identify new methods: `GetCustomersByRegion()`
4. Prompt user: "Add 'GetCustomersByRegion()' to existing CustomerController.cs? (y/n/view diff)"
5. If yes → Insert method in appropriate location
6. If no → Skip
7. If view diff → Show side-by-side comparison

### Use Case 2: Merging DTO Properties
**Scenario**: Generated CustomerDto.cs has new property `TaxIdNumber` but existing DTO has custom validation attributes.

**Desired Behavior**:
1. Parse existing CustomerDto.cs
2. Identify new properties: `TaxIdNumber`
3. Show diff with validation attributes
4. Prompt: "Add 'TaxIdNumber' property? (y/n/edit)"
5. Allow inline editing before merge

### Use Case 3: Merging Views (Razor)
**Scenario**: Generated Edit.cshtml has new BargeEx tab but existing Edit.cshtml has custom JavaScript.

**Desired Behavior**:
1. Detect structural differences (new tab sections)
2. Preserve existing `@section Scripts { ... }`
3. Prompt for each structural change
4. Generate merged file with both old and new code

### Use Case 4: Preserving Business Logic
**Scenario**: Existing CustomerService.cs has custom method `ValidateFreightCode()` that's not in generated template.

**Desired Behavior**:
1. Detect custom methods in existing file
2. Preserve ALL custom methods
3. Add new generated methods
4. Warn if method signatures conflict

---

## Agent Architecture

### Input Parameters
```typescript
interface MergeOptions {
  entity: string;              // "Customer"
  templatePath: string;        // "output/Customer/Templates"
  targetPath: string;          // "C:/Dev/BargeOps.Admin.Mono"
  mode: 'interactive' | 'auto' | 'dry-run';
  conflictStrategy: 'prompt' | 'keep-existing' | 'use-generated';
  fileTypes: ('cs' | 'cshtml' | 'sql')[];
}
```

### Core Capabilities

#### 1. **File Analysis**
- Parse C# files using TypeScript AST parser or regex patterns
- Identify: classes, methods, properties, attributes, comments
- Parse Razor views for sections, partials, scripts
- Detect SQL file differences (queries, parameters)

#### 2. **Diff Detection**
- Method-level diff (new methods, changed methods, removed methods)
- Property-level diff (new properties, changed attributes)
- View section diff (new sections, changed markup)
- Comment preservation

#### 3. **Conflict Resolution**
- **New items** → Add to existing file
- **Changed items** → Prompt user for resolution
- **Removed items** → Keep existing (never auto-delete)
- **Conflicts** → Show side-by-side diff

#### 4. **Interactive Prompts**
```
┌─────────────────────────────────────────────────────────────┐
│  Merge Conflict: CustomerController.cs                      │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Generated template has new method:                         │
│    • GetCustomersByRegion(string region)                   │
│                                                              │
│  Options:                                                   │
│    [a] Add method to existing file                         │
│    [s] Skip (don't add)                                    │
│    [d] View diff                                            │
│    [e] Edit before adding                                   │
│    [q] Quit merge process                                   │
│                                                              │
│  Choice: _                                                  │
└─────────────────────────────────────────────────────────────┘
```

#### 5. **Smart Insertion**
- Insert new methods **at bottom of class** (after last method) to minimize merge conflicts
- Insert new properties **at end of property block** (after last property, before methods)
- Preserve **using statements** (merge and deduplicate)
- Preserve **namespace** structure
- Maintain **code style** (indentation, spacing)
- **Function-by-function merge** strategy for clean, conflict-free merges

#### 6. **Backup & Rollback**
- Create `.backup` files before merge
- Support rollback: `--rollback` flag
- Git integration: auto-commit before merge (optional)

---

## Implementation Plan

### Phase 1: Basic Merge (C# Controllers)
**Duration**: 8-10 hours

1. Create `agents/interactive-template-merge.ts`
2. Implement C# method parsing (regex-based)
3. Detect new methods in generated template
4. Interactive prompts (inquirer.js)
5. Smart insertion logic
6. Test with CustomerController.cs

**Deliverables**:
- Basic merge for API Controllers
- Interactive CLI prompts
- Backup/rollback capability

### Phase 2: DTO & ViewModel Merge
**Duration**: 6-8 hours

1. Extend parser for properties and attributes
2. Handle ValidationAttributes merge
3. Handle Display attributes
4. Test with CustomerDto.cs

**Deliverables**:
- Property-level merge
- Attribute preservation

### Phase 3: View Merge (Razor)
**Duration**: 8-10 hours

1. Razor parsing (sections, partials, scripts)
2. Structural diff detection
3. Section-level merge
4. Test with Edit.cshtml

**Deliverables**:
- Razor view merge
- Script section preservation

### Phase 4: Advanced Features
**Duration**: 6-8 hours

1. Side-by-side diff viewer
2. Inline editing mode
3. Batch mode (auto-resolve simple conflicts)
4. Git integration
5. Comprehensive testing

**Deliverables**:
- Full-featured merge agent
- Documentation
- Test suite

---

## Usage Examples

### Example 1: Interactive Merge (Default)
```bash
bun run agents/interactive-template-merge.ts --entity "Customer"

# Output:
# 📦 Analyzing templates: output/Customer/Templates
# 🔍 Comparing with: C:/Dev/BargeOps.Admin.Mono
#
# Found 5 files to merge:
#   1. CustomerController.cs (3 new methods)
#   2. CustomerDto.cs (2 new properties)
#   3. CustomerRepository.cs (1 changed method)
#   4. Edit.cshtml (1 new tab section)
#   5. CustomerService.cs (no conflicts)
#
# [1/5] CustomerController.cs
# ────────────────────────────────────────────────────────────
# ✨ New method: GetCustomersByRegion(string region)
#
# Add this method? (y/n/d/e/q): d
#
# [Diff View]
# ────────────────────────────────────────────────────────────
# Generated Template                 | Existing File
# ────────────────────────────────────────────────────────────
#   [HttpGet("region/{region}")]     |
#   public async Task<IActionResult> |
#   GetCustomersByRegion(string...)  | (method does not exist)
#   {                                 |
#     ...                             |
#   }                                 |
# ────────────────────────────────────────────────────────────
#
# Add this method? (y/n/e/q): y
# ✓ Added GetCustomersByRegion() to CustomerController.cs
```

### Example 2: Auto Mode (Non-interactive)
```bash
bun run agents/interactive-template-merge.ts --entity "Customer" --mode auto --conflict-strategy keep-existing

# Automatically merges all non-conflicting changes
# Keeps existing code for conflicts
```

### Example 3: Dry Run
```bash
bun run agents/interactive-template-merge.ts --entity "Customer" --dry-run

# Shows what would be merged without making changes
```

### Example 4: Rollback
```bash
bun run agents/interactive-template-merge.ts --rollback --entity "Customer"

# Restores all files from .backup
```

---

## Technical Decisions

### C# Parsing Approach
**Option A**: Full AST parser (Roslyn via .NET or ts-morph for TypeScript)
- ✅ Accurate parsing
- ❌ Complex setup, requires .NET runtime

**Option B**: Regex-based parsing
- ✅ Simple, no dependencies
- ✅ Good enough for method/property detection
- ❌ Limited accuracy for complex scenarios

**Decision**: Start with **Option B (regex)** for MVP, upgrade to Option A if needed.

### Diff Library
Use `diff` npm package for line-by-line comparison.

### Interactive Prompts
Use `inquirer` or `prompts` npm package.

---

## Success Criteria

### Must Have (MVP)
- ✅ Merge API Controller methods (add new, preserve existing)
- ✅ Interactive prompts for conflicts
- ✅ Backup before merge
- ✅ Rollback capability
- ✅ Works for Customer module

### Nice to Have
- ✅ DTO property merge
- ✅ View merge
- ✅ Side-by-side diff viewer
- ✅ Inline editing
- ✅ Git integration
- ✅ Batch mode

### Success Metrics
1. **Customer module merge**: Successfully merge 9 generated files with existing implementation
2. **Zero data loss**: All custom logic preserved
3. **Developer time saved**: 50% reduction in manual merge time
4. **User satisfaction**: Developers prefer agent over manual merge

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Parsing errors (complex C#) | High | Start simple, expand gradually |
| Overwriting custom logic | Critical | Always backup, never auto-delete |
| Merge conflicts undetected | High | Conservative strategy (prompt on ambiguity) |
| Developer learning curve | Medium | Clear prompts, good documentation |
| Performance (large files) | Low | Optimize parsing, cache results |

---

## Testing Strategy

### Unit Tests
- C# method parsing
- Property extraction
- Diff detection
- Smart insertion logic

### Integration Tests
- Full merge workflow
- Backup/rollback
- Git integration

### Manual Tests
- Customer module merge (real-world test)
- Error handling
- Edge cases (empty files, partial classes, etc.)

---

## Documentation

### Developer Guide
- How the agent works
- Merge strategies
- Conflict resolution guide
- Troubleshooting

### User Guide
- Usage examples
- Common scenarios
- Best practices
- FAQ

---

## Future Enhancements

1. **AI-powered merge suggestions**: Use Claude to suggest best merge approach
2. **Multi-file refactoring**: Track dependencies across files
3. **Code quality checks**: Run linters/formatters after merge
4. **Team collaboration**: Share merge decisions across team
5. **Visual diff tool**: GUI for side-by-side comparison

---

## Implementation Checklist

### Phase 1: Basic Merge ✅ COMPLETED
- [x] Create agent file: `agents/interactive-template-merge.ts`
- [x] Implement C# method parser (regex)
- [x] Implement diff detection
- [x] Implement interactive prompts (prompts library)
- [x] Implement smart insertion
- [x] Implement backup logic
- [x] Test with CustomerController.cs

### Phase 2: Property Merge ✅ COMPLETED
- [x] Extend parser for properties
- [x] Handle attributes
- [x] Implement property insertion logic
- [x] Property merge prompts
- [x] Test with dry-run mode

### Phase 3: View Merge ⏸️ DEFERRED
- [ ] Implement Razor parser
- [ ] Section-level diff
- [ ] Test with Edit.cshtml
*Note: Deferred to future phase - C# file merge is priority*

### Phase 4: Polish ✅ MOSTLY COMPLETED
- [x] Side-by-side diff viewer (diffLines from 'diff' package)
- [x] Rollback command (fully implemented)
- [x] Method replacement for conflicts
- [x] Using statement merging and deduplication
- [ ] Documentation (in-code documentation complete)
- [ ] Comprehensive unit tests (manual testing complete)

---

## Timeline

| Phase | Duration | Deliverables |
|-------|----------|--------------|
| Phase 1 | 8-10 hours | Basic merge (Controllers) |
| Phase 2 | 6-8 hours | Property merge (DTOs) |
| Phase 3 | 8-10 hours | View merge (Razor) |
| Phase 4 | 6-8 hours | Polish & docs |
| **Total** | **28-36 hours** | **Full-featured agent** |

---

## Approval Required

Before starting implementation:
1. **Review this spec** with team
2. **Prioritize phases** (MVP vs. nice-to-have)
3. **Allocate development time**
4. **Assign developer/agent**

---

**Status**: ✅ IMPLEMENTED (Phases 1, 2, 4 Complete)
**Author**: ClaudeOnshoreConversionAgent
**Date**: 2026-01-15
**Implementation Date**: 2026-01-15
**Next Steps**:
1. ✅ Phase 1 (Basic Merge) - Complete
2. ✅ Phase 2 (Property Merge) - Complete
3. ⏸️ Phase 3 (View Merge) - Deferred for future enhancement
4. ✅ Phase 4 (Polish) - Mostly complete (documentation and unit tests remaining)

**Ready for Use**: Yes - Agent is fully functional for C# file merging (Controllers, Services, DTOs, etc.)
