# Interactive Template Merge Agent - User Guide

## 📚 Table of Contents
- [Overview](#overview)
- [Features](#features)
- [Requirements](#requirements)
- [Quick Start](#quick-start)
- [Command-Line Options](#command-line-options)
- [Usage Examples](#usage-examples)
- [Interactive Prompts Guide](#interactive-prompts-guide)
- [Merge Strategies](#merge-strategies)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)
- [Limitations](#limitations)
- [FAQ](#faq)

---

## Overview

The **Interactive Template Merge Agent** intelligently merges generated C# code templates with existing implementations, preserving custom business logic while incorporating new features and methods.

**Problem it solves**: When converting entities that already have partial implementations (like Customer, Vendor, etc.), you need to:
- Add new generated methods without overwriting existing custom logic
- Merge new properties while preserving validation attributes
- Maintain developer customizations (comments, business rules, etc.)

**Solution**: This agent performs intelligent, function-by-function merging with interactive user prompts, automatic backups, and rollback capability.

---

## Features

### Core Capabilities
- ✅ **Method-Level Merge**: Add new methods to existing controllers, services, and repositories
- ✅ **Property-Level Merge**: Add new DTO properties with attributes preserved
- ✅ **File Copy**: Automatically copy new files that don't exist in target
- ✅ **Custom Logic Preservation**: Never deletes custom methods or properties
- ✅ **Interactive Prompts**: User-friendly CLI prompts for every decision
- ✅ **Backup & Rollback**: Automatic `.backup` files with full rollback capability
- ✅ **Using Statement Management**: Deduplicates and merges using statements
- ✅ **Dry-Run Mode**: Preview changes without modifying files
- ✅ **Auto Mode**: Batch processing without prompts

### Supported File Types
- **API**: Controllers, Services, Repositories, Interfaces
- **UI**: Controllers, Services, ViewModels
- **Shared**: DTOs, SearchRequest objects

### Merge Strategy
- **Bottom-insertion strategy**: Always inserts new methods at the bottom of the class (after last method) to minimize merge conflicts
- **Property-block insertion**: Inserts new properties after last property, before methods
- **Conflict detection**: Identifies method signature changes and prompts for resolution
- **Re-parsing**: Maintains accurate line positions during multi-step merges

---

## Requirements

- **Bun runtime**: The agent runs with `bun run` command
- **Node.js dependencies**:
  - `prompts` - Interactive CLI prompts
  - `diff` - Side-by-side diff viewer
- **File structure**: Expects templates in `output/{Entity}/Templates/` directory

---

## Quick Start

### 1. Basic Interactive Merge
```bash
bun run agents/interactive-template-merge.ts --entity "Customer"
```

This will:
1. Scan templates for the Customer entity
2. Compare with existing implementation
3. Prompt you for each new method/property
4. Create backups before any changes
5. Merge selected items

### 2. Preview Changes (Dry-Run)
```bash
bun run agents/interactive-template-merge.ts --entity "Customer" --dry-run
```

Shows what would be merged without making changes.

### 3. Rollback Changes
```bash
bun run agents/interactive-template-merge.ts --rollback --entity "Customer"
```

Restores all files from `.backup` files.

---

## Command-Line Options

### Required
- `--entity <name>` - Entity name to merge (e.g., "Customer", "Vendor")

### Optional
- `--dry-run` - Preview merge without making changes
- `--auto` - Automatically merge all without prompts
- `--rollback` - Restore files from backups
- `--conflict-strategy <strategy>` - Conflict resolution strategy
  - `prompt` (default) - Ask user for each conflict
  - `keep-existing` - Always keep existing code
  - `use-generated` - Always use generated code

---

## Usage Examples

### Example 1: First-Time Entity Deployment

**Scenario**: You've generated templates for a new "Vendor" entity and want to deploy them.

```bash
bun run agents/interactive-template-merge.ts --entity "Vendor" --dry-run
```

**Output**:
```
╔════════════════════════════════════════════════════════════════════════════╗
║            INTERACTIVE TEMPLATE MERGE AGENT                                ║
║  Entity: Vendor                                                            ║
║  Mode: dry-run                                                             ║
╚════════════════════════════════════════════════════════════════════════════╝

📦 Found 1 file(s) to merge and 6 new file(s) to copy

═══════════════════════════════════════════════════════════════════════════
📄 Merging: C:/Dev/BargeOps.Admin.Mono/Controllers/VendorController.cs
═══════════════════════════════════════════════════════════════════════════
   ✨ 14 new method(s)
   🔷 0 new property(ies)
   📝 0 changed method(s)
   💾 5 custom method(s) to preserve
   ⚠️  0 conflict(s)

  [DRY RUN] Would merge 14 method(s) and 0 property(ies)

═══════════════════════════════════════════════════════════════════════════
📄 New File: VendorService.cs
═══════════════════════════════════════════════════════════════════════════
  [DRY RUN] Would copy new file

[... more files ...]

═══════════════════════════════════════════════════════════════════════════
📊 MERGE & COPY SUMMARY
═══════════════════════════════════════════════════════════════════════════
   ✅ Merged: 1 file(s)
   📄 Copied: 6 new file(s)
   ⏭️  Skipped: 0 file(s)
   ❌ Errors: 0 file(s)
   ✨ Methods added: 14
   🔷 Properties added: 0
   ⚠️  Conflicts: 0
═══════════════════════════════════════════════════════════════════════════
```

After reviewing, run without `--dry-run` to actually merge:
```bash
bun run agents/interactive-template-merge.ts --entity "Vendor"
```

### Example 2: Adding New Methods to Existing Implementation

**Scenario**: Customer entity already exists with custom logic. You've regenerated templates with new features.

```bash
bun run agents/interactive-template-merge.ts --entity "Customer"
```

**Interactive Prompt**:
```
────────────────────────────────────────────────────────────────────────────
✨ NEW METHOD FOUND: GetCustomersByRegion
────────────────────────────────────────────────────────────────────────────
Context: C:/Dev/BargeOps.Admin.Mono/Controllers/CustomerController.cs
Signature: Task<ActionResult<PagedResult<CustomerDto>>> GetCustomersByRegion(string region)
Attributes: [HttpGet("region/{region}")], [ProducesResponseType(typeof(PagedResult<CustomerDto>), 200)]
Public: true, Async: true

? What would you like to do? (Use arrow keys)
❯ Add this method
  Skip (don't add)
  View full method code
  View diff
  Quit merge process
```

Select "Add this method" to merge it.

### Example 3: Handling Conflicts

**Scenario**: A method signature changed between versions.

```bash
bun run agents/interactive-template-merge.ts --entity "Customer"
```

**Conflict Prompt**:
```
⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠
⚠️  CONFLICT: Method signature mismatch for "Search"
⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠
Generated: Task<ActionResult<PagedResult<CustomerDto>>> Search(CustomerSearchRequest request)
Existing:  Task<ActionResult<IEnumerable<CustomerDto>>> Search(string searchTerm)

? How should this conflict be resolved?
❯ Keep existing (recommended)
  Replace with generated
  Skip (leave unchanged)
```

**Recommendation**: Keep existing to preserve custom logic.

### Example 4: Batch Processing (Auto Mode)

**Scenario**: You trust the generated code and want to merge everything automatically.

```bash
bun run agents/interactive-template-merge.ts --entity "Vendor" --auto --conflict-strategy keep-existing
```

This will:
- Add all new methods/properties automatically
- Keep existing code for conflicts
- No prompts (runs to completion)

### Example 5: Rollback After Bad Merge

**Scenario**: You merged changes but realize there's an issue.

```bash
bun run agents/interactive-template-merge.ts --rollback --entity "Customer"
```

**Output**:
```
📦 Searching for backup files for entity: Customer

  ✅ Restored from backup: C:/Dev/.../CustomerController.cs
  ✅ Restored from backup: C:/Dev/.../CustomerService.cs

═══════════════════════════════════════════════════════════════════════════
🔄 ROLLBACK SUMMARY
═══════════════════════════════════════════════════════════════════════════
   ✅ Restored: 2 file(s)
   ⏭️  No backup found: 8 file(s)
═══════════════════════════════════════════════════════════════════════════

✅ Rollback complete!

📝 Files have been restored to their pre-merge state.
```

---

## Interactive Prompts Guide

### Method Merge Prompt

When a new method is found, you'll see:

```
✨ NEW METHOD FOUND: MethodName
────────────────────────────────────────────────────────────────────────────
Context: /path/to/file.cs
Signature: ReturnType MethodName(params)
Attributes: [HttpGet], [Authorize]
Public: true, Async: true

? What would you like to do?
```

**Options**:
- **Add this method** - Inserts the method at the bottom of the class
- **Skip (don't add)** - Ignores this method
- **View full method code** - Shows the complete method implementation
- **View diff** - Shows side-by-side comparison (if applicable)
- **Quit merge process** - Stops merging and exits

### Property Merge Prompt

When a new property is found:

```
✨ NEW PROPERTY FOUND: PropertyName
────────────────────────────────────────────────────────────────────────────
Context: /path/to/Dto.cs
Type: string
Attributes: [Required], [StringLength(50)]
Accessibility: public

? What would you like to do?
```

**Options**:
- **Add this property** - Inserts property after existing properties
- **Skip (don't add)** - Ignores this property
- **View full property code** - Shows property with all attributes
- **Quit merge process** - Stops merging and exits

### Conflict Resolution Prompt

When method signatures differ:

```
⚠️  CONFLICT: Method signature mismatch for "MethodName"
Generated: NewSignature
Existing:  OldSignature

? How should this conflict be resolved?
```

**Options**:
- **Keep existing (recommended)** - Preserves your custom implementation
- **Replace with generated** - Overwrites with generated code (⚠️ loses custom logic)
- **Skip (leave unchanged)** - No action taken

---

## Merge Strategies

### Bottom-Insertion Strategy (Default)

New methods are always inserted **at the bottom of the class** (after the last method, before the closing brace).

**Why?**
- Minimizes merge conflicts in version control
- Predictable location for new code
- Easy to review changes
- Doesn't disrupt existing method order

**Example**:
```csharp
public class CustomerController
{
    // Constructor
    public CustomerController(...) { }

    // Existing methods
    public async Task<ActionResult> GetById(int id) { ... }
    public async Task<ActionResult> Create(...) { ... }

    // ← New methods inserted here (at bottom)
    public async Task<ActionResult> GetCustomersByRegion(string region) { ... }
}
```

### Property-Block Insertion

New properties are inserted **after the last property, before methods**.

**Example**:
```csharp
public class CustomerDto
{
    // Existing properties
    public int CustomerId { get; set; }
    public string Name { get; set; }

    // ← New properties inserted here
    public string TaxIdNumber { get; set; }

    // Methods (if any)
    public void Validate() { ... }
}
```

### Using Statement Merging

Using statements from both files are:
1. Combined into a single set
2. Deduplicated
3. Sorted alphabetically
4. Placed at the top of the file

---

## Best Practices

### 1. Always Use Dry-Run First

```bash
# Preview changes before committing
bun run agents/interactive-template-merge.ts --entity "Customer" --dry-run

# Review output, then run for real
bun run agents/interactive-template-merge.ts --entity "Customer"
```

### 2. Review Merged Files Before Committing

After merging:
1. Open merged files in your IDE
2. Review inserted methods/properties
3. Check that custom logic is preserved
4. Run compiler and tests
5. Commit to version control

### 3. Use Backups for Safety

Backups are created automatically as `.backup` files:
```
CustomerController.cs
CustomerController.cs.backup  ← Created automatically
```

If something goes wrong:
```bash
bun run agents/interactive-template-merge.ts --rollback --entity "Customer"
```

### 4. Handle Conflicts Carefully

When you see a conflict:
- **View the diff** to understand what changed
- **Keep existing** if you have custom logic
- **Replace with generated** only if you're sure the new version is correct
- **Skip** if you're unsure and want to handle manually

### 5. Use Git Before Major Merges

```bash
# Commit current state before merging
git add .
git commit -m "Before template merge"

# Run merge
bun run agents/interactive-template-merge.ts --entity "Customer"

# Review changes
git diff

# If bad, revert
git reset --hard HEAD
```

### 6. Test After Merging

```bash
# Compile
dotnet build

# Run tests
dotnet test

# Manual testing in dev environment
```

### 7. Entity-by-Entity Approach

Don't merge multiple entities at once. Do one at a time:
```bash
bun run agents/interactive-template-merge.ts --entity "Customer"   # Review & test
bun run agents/interactive-template-merge.ts --entity "Vendor"     # Review & test
bun run agents/interactive-template-merge.ts --entity "Barge"      # Review & test
```

---

## Troubleshooting

### Problem: "No files found to merge or copy"

**Cause**: Templates don't exist or paths are wrong.

**Solution**:
```bash
# Check that templates exist
ls output/Customer/Templates/

# Verify entity name is correct
bun run agents/interactive-template-merge.ts --entity "Customer"  # ✅ Correct
bun run agents/interactive-template-merge.ts --entity "customer"  # ❌ Wrong case
```

### Problem: Merge Added Wrong Methods

**Cause**: Accidentally selected "Add" for unwanted methods.

**Solution**:
```bash
# Rollback
bun run agents/interactive-template-merge.ts --rollback --entity "Customer"

# Re-run and be more selective
bun run agents/interactive-template-merge.ts --entity "Customer"
```

### Problem: Backup Files Not Found

**Cause**: Backups are only created when files are modified.

**Solution**: If you run dry-run or skip everything, no backups are created. Only actual merges create backups.

### Problem: Compiler Errors After Merge

**Cause**:
- Missing using statements
- Namespace conflicts
- Method signature changes

**Solution**:
```bash
# Check compiler errors
dotnet build

# Common issues:
# 1. Add missing using statements manually
# 2. Fix namespace mismatches
# 3. Update method calls if signatures changed

# If unfixable, rollback:
bun run agents/interactive-template-merge.ts --rollback --entity "Customer"
```

### Problem: Git Merge Conflicts

**Cause**: Multiple people modified same files.

**Solution**:
```bash
# Resolve conflicts manually in Git
git mergetool

# Or rollback and re-merge after pulling latest
bun run agents/interactive-template-merge.ts --rollback --entity "Customer"
git pull
bun run agents/interactive-template-merge.ts --entity "Customer"
```

### Problem: Method Not Detected

**Cause**: Method uses syntax not supported by regex parser.

**Solution**:
- Expression-bodied methods (`public int Foo() => _bar;`) are not supported
- Add these methods manually

### Problem: Temp Files Left Behind

**Cause**: Agent crashed or was interrupted.

**Solution**:
```bash
# Clean up temp files
find . -name "*.temp" -delete

# Or on Windows
del /s *.temp
```

---

## Limitations

### Syntax Support
- ❌ **Expression-bodied methods**: `public int Foo() => _bar;`
- ❌ **Expression-bodied properties**: `public string Name => _name;`
- ❌ **Partial class merging**: Assumes single-file classes
- ❌ **Razor views**: `.cshtml` files (Phase 3 - future enhancement)

### Parsing Limitations
- Uses regex-based parsing (not full AST)
- May miss complex method signatures with multi-line parameters
- Cannot detect semantic differences (same signature, different logic)

### Conflict Detection
- Only detects method signature differences
- Cannot detect if method body logic changed
- Cannot merge methods with identical signatures but different implementations

### File Operations
- Cannot merge split across multiple partial class files
- Cannot handle complex namespace hierarchies
- Assumes standard project structure

### Recommendations
- Use standard C# syntax (no expression-bodied syntax)
- Keep methods in single files (no partial classes)
- Use clear, unique method names
- Manually merge Razor views for now

---

## FAQ

### Q: Will this overwrite my custom code?
**A**: No. The agent NEVER deletes custom methods or properties. It only adds new items or prompts for conflicts.

### Q: Can I undo a merge?
**A**: Yes, use `--rollback` flag to restore from backups:
```bash
bun run agents/interactive-template-merge.ts --rollback --entity "Customer"
```

### Q: What if I accidentally add the wrong method?
**A**: Use rollback, then re-run and skip that method.

### Q: Can I run this in CI/CD?
**A**: Yes, use `--auto --conflict-strategy keep-existing` for non-interactive mode.

### Q: Does this work with Razor views?
**A**: Not yet. Razor view merging is a future enhancement (Phase 3).

### Q: How do I know what changed?
**A**:
- Use `--dry-run` first to preview
- Check `.backup` files
- Use `git diff` after merging

### Q: Can I merge multiple entities at once?
**A**: No. Run the agent once per entity for better control and review.

### Q: What happens to comments in my code?
**A**: Comments in existing code are preserved. Comments in generated code are added with new methods.

### Q: Can I customize insertion points?
**A**: Currently no. The agent uses bottom-insertion strategy for consistency. Manual adjustment needed for custom organization.

### Q: Does it work with interfaces?
**A**: Yes, it works with any C# file (classes, interfaces, etc.).

### Q: What about unit tests?
**A**: The agent doesn't handle test files. Merge those manually.

---

## Next Steps

1. **Try it out**: Start with `--dry-run` mode
2. **Read the developer guide**: `DEVELOPER_GUIDE.md` (coming soon)
3. **Report issues**: Open GitHub issues for bugs or feature requests
4. **Provide feedback**: Let us know how it works for your workflow

---

**Need Help?**
- Check the [Troubleshooting](#troubleshooting) section
- Review [Usage Examples](#usage-examples)
- Read the spec: `.claude/tasks/AGENT_InteractiveTemplateMerge_SPEC.md`
- Ask the team in Slack

---

**Version**: 1.0
**Last Updated**: 2026-01-16
**Status**: Production Ready
