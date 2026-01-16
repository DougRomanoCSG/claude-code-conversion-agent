# Interactive Template Merge Agent - Developer Guide

## 📚 Table of Contents
- [Architecture Overview](#architecture-overview)
- [Core Components](#core-components)
- [Data Structures](#data-structures)
- [Parsing Algorithms](#parsing-algorithms)
- [Merge Logic](#merge-logic)
- [Insertion Strategies](#insertion-strategies)
- [Error Handling](#error-handling)
- [Testing Strategy](#testing-strategy)
- [Extending the Agent](#extending-the-agent)
- [Performance Considerations](#performance-considerations)
- [Known Issues](#known-issues)

---

## Architecture Overview

### High-Level Flow

```
┌─────────────────────────────────────────────────────────────┐
│  1. Parse Command-Line Options                              │
│     - Entity name, mode (interactive/auto/dry-run)          │
│     - Conflict strategy, rollback flag                      │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│  2. Identify File Operations                                │
│     - Scan template directory                               │
│     - Check which files exist in target                     │
│     - Categorize: Merge vs. Copy                            │
└────────────────────┬────────────────────────────────────────┘
                     │
           ┌─────────┴─────────┐
           │                   │
           ▼                   ▼
┌──────────────────┐  ┌────────────────────┐
│  Files to Merge  │  │  Files to Copy     │
│  (exist in both) │  │  (new files)       │
└────────┬─────────┘  └─────────┬──────────┘
         │                       │
         ▼                       ▼
┌──────────────────┐  ┌────────────────────┐
│  Parse Both      │  │  Prompt to Copy    │
│  Files (C#)      │  │  (interactive)     │
└────────┬─────────┘  └─────────┬──────────┘
         │                       │
         ▼                       │
┌──────────────────┐             │
│  Analyze Diff    │             │
│  - New methods   │             │
│  - New props     │             │
│  - Conflicts     │             │
└────────┬─────────┘             │
         │                       │
         ▼                       │
┌──────────────────┐             │
│  Create Backup   │             │
└────────┬─────────┘             │
         │                       │
         ▼                       │
┌──────────────────┐             │
│  Interactive     │             │
│  Prompts         │             │
│  - Add method?   │             │
│  - Add property? │             │
│  - Resolve       │             │
│    conflict?     │             │
└────────┬─────────┘             │
         │                       │
         ▼                       │
┌──────────────────┐             │
│  Insert Items    │             │
│  - Methods       │             │
│  - Properties    │             │
│  - Using stmts   │             │
└────────┬─────────┘             │
         │                       │
         ▼                       ▼
┌─────────────────────────────────────────────────────────────┐
│  3. Write Modified Files / Copy New Files                   │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│  4. Display Summary                                          │
│     - Files merged/copied/skipped                           │
│     - Methods/properties added                              │
│     - Conflicts resolved                                    │
└─────────────────────────────────────────────────────────────┘
```

### Design Principles

1. **Conservative Merging**: Never delete existing code automatically
2. **User Control**: Prompt for every significant decision (in interactive mode)
3. **Safety First**: Create backups before any modifications
4. **Predictable Behavior**: Always insert at bottom (methods) or property-block end (properties)
5. **Idempotent**: Running multiple times produces same result

---

## Core Components

### 1. Command-Line Parser (`parseOptions()`)

**Location**: Lines 1152-1193

Parses command-line arguments and returns `MergeOptions` object.

**Arguments**:
- `--entity <name>` (required)
- `--dry-run` (optional)
- `--auto` (optional)
- `--rollback` (optional)
- `--conflict-strategy <strategy>` (optional)

**Error Handling**: Exits with usage message if `--entity` not provided.

### 2. C# File Parser

**Functions**:
- `parseCSharpFile()` - Main entry point (lines 106-144)
- `extractMethods()` - Extract method definitions (lines 150-218)
- `extractProperties()` - Extract property definitions (lines 223-272)
- `findMatchingBrace()` - Find closing brace for method body (lines 278-375)

**Parsing Strategy**: Regex-based (not full AST parser)

**Why Regex?**
- ✅ Simple implementation
- ✅ No external dependencies (.NET Roslyn not needed)
- ✅ Sufficient for standard C# patterns
- ❌ Limited accuracy for complex syntax

### 3. Merge Analyzer (`analyzeMerge()`)

**Location**: Lines 344-399

Compares generated and existing parsed classes to identify:
- **New methods**: In generated but not in existing
- **Changed methods**: Signature mismatch
- **Removed methods**: In existing but not in generated (custom code to preserve)
- **New properties**: In generated but not in existing
- **Changed properties**: Type mismatch

**Normalization**: Signatures are normalized (whitespace removed) for comparison.

### 4. Interactive Prompt System

**Functions**:
- `promptMethodMerge()` - Prompt for method decisions (lines 408-442)
- `promptPropertyMerge()` - Prompt for property decisions (lines 447-480)
- `promptConflictResolution()` - Prompt for conflict resolution (lines 485-508)
- `promptFileCopy()` - Prompt for file copy decisions (lines 860-879)
- `showDiff()` - Display side-by-side diff (lines 513-533)

**Library**: Uses `prompts` npm package for interactive CLI.

**Prompt Types**:
- Select menu (arrow keys)
- Recursive prompts (e.g., view code → re-prompt)

### 5. Smart Insertion Engine

**Functions**:
- `insertMethod()` - Insert method into existing file (lines 665-680)
- `insertProperty()` - Insert property into existing file (lines 739-757)
- `replaceMethod()` - Replace existing method (lines 762-789)
- `findInsertionPoint()` - Find where to insert method (lines 631-660)
- `findPropertyInsertionPoint()` - Find where to insert property (lines 686-734)

**Strategy**:
- Methods → Bottom of class (after last method)
- Properties → After last property, before methods

### 6. Using Statement Manager

**Functions**:
- `mergeUsingStatements()` - Deduplicate and sort usings (lines 798-801)
- `updateUsingStatements()` - Replace usings in file (lines 806-851)

**Logic**:
1. Extract all using statements from both files
2. Combine into Set (deduplicates)
3. Sort alphabetically
4. Remove old using statements from file
5. Insert new using block at top

### 7. Backup & Rollback System

**Functions**:
- `createBackup()` - Copy file to `.backup` (lines 542-552)
- `restoreFromBackup()` - Restore from `.backup` (lines 557-571)
- `performRollback()` - Batch rollback for entity (lines 576-621)

**Backup Format**: `{filename}.backup` (e.g., `CustomerController.cs.backup`)

**Rollback Strategy**: Searches for all possible entity files and restores found backups.

### 8. File Operations Manager

**Functions**:
- `mergeFile()` - Main merge logic for single file (lines 960-1146)
- `copyNewFile()` - Copy new file to target (lines 884-951)

**mergeFile() Flow**:
1. Parse both files
2. Analyze differences
3. Create backup
4. Prompt for each new method/property
5. Insert accepted items
6. Handle conflicts
7. Merge using statements
8. Write modified file
9. Clean up temp files

---

## Data Structures

### ParsedMethod
```typescript
interface ParsedMethod {
  name: string;              // Method name (e.g., "GetById")
  signature: string;         // Full signature for comparison
  startLine: number;         // Start line number (1-indexed)
  endLine: number;           // End line number (1-indexed)
  fullText: string;          // Complete method code including attributes
  attributes: string[];      // Attributes like "[HttpGet]"
  isPublic: boolean;         // Public modifier present
  isAsync: boolean;          // Async modifier present
  returnType: string;        // Return type (e.g., "Task<ActionResult>")
  parameters: string;        // Parameter list
}
```

### ParsedProperty
```typescript
interface ParsedProperty {
  name: string;              // Property name (e.g., "Name")
  type: string;              // Property type (e.g., "string")
  attributes: string[];      // Attributes like "[Required]"
  accessibility: string;     // public/private/protected/internal
  isReadOnly: boolean;       // Readonly modifier present
  hasGetter: boolean;        // Has get accessor
  hasSetter: boolean;        // Has set accessor
  fullText: string;          // Complete property code
  startLine: number;         // Start line number (1-indexed)
}
```

### ParsedClass
```typescript
interface ParsedClass {
  name: string;              // Class name
  methods: ParsedMethod[];   // All methods in class
  properties: ParsedProperty[]; // All properties in class
  usings: string[];          // Using statements (namespace only)
  namespace: string;         // Namespace declaration
  rawContent: string;        // Full file content
}
```

### MergeAnalysis
```typescript
interface MergeAnalysis {
  newMethods: ParsedMethod[];              // To add
  changedMethods: {                        // Conflicts
    generated: ParsedMethod;
    existing: ParsedMethod;
  }[];
  removedMethods: ParsedMethod[];          // Custom code to preserve
  unchangedMethods: ParsedMethod[];        // No action needed
  newProperties: ParsedProperty[];         // To add
  changedProperties: {                     // Conflicts
    generated: ParsedProperty;
    existing: ParsedProperty;
  }[];
  conflicts: string[];                     // Conflict messages
}
```

---

## Parsing Algorithms

### Method Extraction Regex

```regex
/^\s*(?:\[[^\]]+\]\s*)*(?:public|private|protected|internal)\s+(?:static\s+)?(?:async\s+)?(?:virtual\s+)?(?:override\s+)?([\w<>[\]?]+)\s+([A-Z]\w+)\s*\(([\s\S]*?)\)\s*{/gm
```

**Breakdown**:
- `^\s*` - Start of line with optional whitespace
- `(?:\[[^\]]+\]\s*)*` - Zero or more attributes (same line)
- `(?:public|private|protected|internal)\s+` - Accessibility modifier (required)
- `(?:static\s+)?` - Optional static
- `(?:async\s+)?` - Optional async
- `(?:virtual\s+)?` - Optional virtual
- `(?:override\s+)?` - Optional override
- `([\w<>[\]?]+)\s+` - Return type (capture group 1)
- `([A-Z]\w+)\s*` - Method name starting with capital (capture group 2)
- `\(([\s\S]*?)\)` - Parameters (capture group 3)
- `\s*{` - Opening brace

**False Positive Prevention** (lines 165-177):
- Skip if line before starts with `return ` (not a method declaration)
- Skip common ASP.NET return methods: `Ok`, `NotFound`, `BadRequest`, etc.

### Property Extraction Regex

```regex
/^\s*(?:\[[^\]]+\]\s*)*(?:public|private|protected|internal)\s+(?:static\s+)?(?:readonly\s+)?([\w<>[\]?]+)\s+([A-Z]\w+)\s*{\s*get[^}]*?;(?:\s*set[^}]*?;)?\s*}/gm
```

**Breakdown**:
- Similar to method regex
- `{\s*get[^}]*?;` - Get accessor with semicolon
- `(?:\s*set[^}]*?;)?` - Optional set accessor
- `\s*}` - Closing brace

**Supports**:
- Auto-properties: `{ get; set; }`
- Read-only properties: `{ get; }`
- Property initializers: `{ get; set; } = "default";` (initializer ignored)

**Does NOT Support**:
- Expression-bodied properties: `=> _field;`
- Full getter/setter bodies with logic

### Brace Matching Algorithm (`findMatchingBrace`)

**Location**: Lines 278-375

**Purpose**: Find the closing `}` for a method body, correctly handling strings/comments.

**Algorithm**:
```
1. Start with depth = 1 (opening brace)
2. Iterate through characters after opening brace
3. Track state:
   - inString (regular string)
   - inVerbatimString (@"...")
   - inInterpolatedString ($"...")
   - inChar ('...')
   - inComment (/* ... */)
   - inLineComment (// ...)
   - interpolatedStringBraceDepth (for $"...{...}")
4. Skip braces inside strings/comments
5. Increment depth for {, decrement for }
6. When depth reaches 0, return position
```

**Special Cases Handled**:
- Escaped quotes: `\"` in regular strings
- Doubled quotes: `""` in verbatim strings
- Interpolation braces: `{` in $"..." strings
- Nested interpolation: $"Count: {items.Count}"

### Attribute Extraction

**Location**: Lines 181-188 (methods), 238-245 (properties)

**Algorithm**:
```
1. Start from line ABOVE property/method
2. Walk backwards through lines
3. If line starts with '[' and contains ']':
   - Add to attributes list
4. If line is non-empty and not comment:
   - Stop (found non-attribute code)
5. Continue until start of file
```

**Handles**:
- Multi-line attributes
- Comment lines between attributes
- Blank lines

---

## Merge Logic

### analyzeMerge() Algorithm

**Location**: Lines 344-399

**Logic**:
```
1. Compare Methods:
   For each method in generated:
     Find matching method in existing by name
     If not found:
       → Add to newMethods
     If found:
       Normalize signatures (remove whitespace)
       If signatures differ:
         → Add to changedMethods
         → Add to conflicts
       Else:
         → Add to unchangedMethods

   For each method in existing:
     If not in generated:
       → Add to removedMethods (custom code to preserve)

2. Compare Properties:
   For each property in generated:
     Find matching property in existing by name
     If not found:
       → Add to newProperties
     If found and types differ:
       → Add to changedProperties
       → Add to conflicts
```

### mergeFile() Algorithm

**Location**: Lines 960-1146

**Flow**:
```
1. Parse both files
2. Analyze differences
3. Display summary to user
4. If dry-run: return without modifying
5. Create backup
6. For each new method:
     If interactive: prompt user
     If user accepts:
       insertMethod()
       Re-parse file (to update line positions)
     Else:
       Skip
7. For each new property:
     If interactive: prompt user
     If user accepts:
       insertProperty()
       Re-parse file
     Else:
       Skip
8. For each conflict:
     If interactive: prompt for resolution
     If replace:
       replaceMethod()
       Re-parse file
     Else:
       Keep existing
9. Merge using statements
10. Write modified content
11. Clean up temp files
```

**Re-parsing Strategy**: After each insertion, the file is re-parsed to maintain accurate line numbers. This is critical because inserting a method changes all subsequent line numbers.

---

## Insertion Strategies

### Method Insertion (Bottom-Insertion Strategy)

**Function**: `insertMethod()` (lines 665-680)

**Algorithm**:
```
1. Find insertion point (after last method)
2. Format method with proper newlines:
   - "\n\n" before method
   - Method code
   - "\n" after method
3. Split content at insertion point
4. Insert formatted method
5. Return concatenated content
```

**Finding Insertion Point** (`findInsertionPoint()`, lines 631-660):
```
1. If no existing methods:
     → Insert before class closing brace
2. Else:
     Get last method from existing methods
     Find line after its closing brace
     Skip any blank lines
     Convert line number to character position
     Return position
```

**Why Bottom Insertion?**
- Minimizes merge conflicts in Git
- Predictable location
- Easy to review
- Doesn't disrupt existing method organization

### Property Insertion

**Function**: `insertProperty()` (lines 739-757)

**Algorithm**:
```
1. Find insertion point (after last property, before methods)
2. Add indentation (4 spaces)
3. Format property:
   - "\n" before property
   - Indented property lines
   - "\n" after property
4. Split content at insertion point
5. Insert formatted property
6. Return concatenated content
```

**Finding Insertion Point** (`findPropertyInsertionPoint()`, lines 686-734):
```
1. If existing properties:
     Get last property
     Find its closing }
     Skip blank lines
     Return character position
2. Else if existing methods:
     Get first method
     Find start line (including attributes/comments)
     Return character position before method
3. Else:
     → Insert before class closing brace
```

### Method Replacement

**Function**: `replaceMethod()` (lines 762-789)

**Algorithm**:
```
1. Find start line of existing method
2. Walk backwards to include attributes/comments
3. Calculate character positions (start and end)
4. Split content:
   - Before: content[0:startPos]
   - After: content[endPos:]
5. Insert new method code between
6. Return concatenated content
```

---

## Error Handling

### File Not Found
- `parseCSharpFile()` returns `null` if file doesn't exist
- Calling code checks for `null` and returns error result

### Parsing Failures
- If `parseCSharpFile()` returns `null`, status = 'error'
- Error message included in result

### Backup Creation Failures
- `createBackup()` returns `false` if copy fails
- Merge aborts if backup fails (prevents data loss)

### Write Failures
- Wrapped in try-catch blocks
- Error result returned with error message

### Temp File Cleanup
- Wrapped in try-catch with ignored errors
- Prevents cleanup failures from affecting merge

### User Cancellation
- Prompt returns 'quit'
- Merge stops immediately
- Status = 'skipped'

---

## Testing Strategy

### Manual Testing Checklist

#### Basic Merge Test
```bash
1. Generate templates for entity with existing implementation
2. Run: bun run agents/interactive-template-merge.ts --entity "Customer" --dry-run
3. Verify output shows:
   ✓ Correct number of new methods
   ✓ Correct number of preserved methods
   ✓ No unexpected conflicts
4. Run without --dry-run
5. Accept all new methods
6. Verify:
   ✓ Backup created
   ✓ Methods inserted at bottom
   ✓ Existing methods unchanged
   ✓ File compiles
```

#### Property Merge Test
```bash
1. Add new property to generated DTO
2. Run merge
3. Accept new property
4. Verify:
   ✓ Property inserted after last property
   ✓ Attributes preserved
   ✓ Indentation correct
```

#### Conflict Resolution Test
```bash
1. Change method signature in generated template
2. Run merge
3. Verify conflict prompt appears
4. Test all options:
   - Keep existing
   - Replace with generated
   - Skip
5. Verify chosen action taken
```

#### Rollback Test
```bash
1. Run merge and modify files
2. Run: bun run agents/interactive-template-merge.ts --rollback --entity "Customer"
3. Verify all files restored
```

#### File Copy Test
```bash
1. Generate templates for new entity (not in target)
2. Run merge
3. Verify:
   ✓ New files detected
   ✓ Prompted to copy each file
   ✓ Directories created if needed
   ✓ Files copied correctly
```

### Unit Testing (Future Enhancement)

**Functions to Test**:
- `parseCSharpFile()`
- `extractMethods()`
- `extractProperties()`
- `findMatchingBrace()`
- `analyzeMerge()`
- `insertMethod()`
- `insertProperty()`
- `mergeUsingStatements()`

**Test Cases**:
- Various C# syntax patterns
- Edge cases (empty files, no methods, etc.)
- Malformed code
- Complex nested structures

---

## Extending the Agent

### Adding Support for New File Types

**Example**: Adding TypeScript support

1. **Create Parser**:
```typescript
function parseTypeScriptFile(filePath: string): ParsedClass | null {
  // Similar to parseCSharpFile but with TS regex patterns
}
```

2. **Update File Operations List**:
```typescript
const fileOperations: FileOperation[] = [
  // ... existing C# operations
  {
    generated: join(templateRoot, 'ts', 'services', `${options.entity}Service.ts`),
    existing: join(uiPath, 'services', `${options.entity}Service.ts`),
    type: 'ui',
    exists: false,
  },
];
```

3. **Dispatch Based on Extension**:
```typescript
function mergeFile(generatedPath: string, existingPath: string, options: MergeOptions) {
  const ext = path.extname(generatedPath);

  if (ext === '.cs') {
    return mergeCSharpFile(generatedPath, existingPath, options);
  } else if (ext === '.ts') {
    return mergeTypeScriptFile(generatedPath, existingPath, options);
  }
  // ...
}
```

### Adding New Prompt Options

**Example**: Adding "Edit before adding" option

1. **Update Prompt**:
```typescript
async function promptMethodMerge(method: ParsedMethod, context: string) {
  const response = await prompts({
    type: 'select',
    name: 'action',
    choices: [
      { title: 'Add this method', value: 'add' },
      { title: 'Edit before adding', value: 'edit' },  // ← NEW
      // ... other options
    ],
  });

  if (response.action === 'edit') {
    const edited = await editMethodInline(method);
    return promptMethodMerge(edited, context);  // Re-prompt with edited
  }
  // ...
}
```

2. **Implement Editor**:
```typescript
async function editMethodInline(method: ParsedMethod): Promise<ParsedMethod> {
  // Open in editor, get modified text, re-parse
  // Return modified ParsedMethod
}
```

### Adding New Merge Strategies

**Example**: Adding "Top-insertion" strategy

1. **Add Option**:
```typescript
interface MergeOptions {
  // ... existing fields
  insertionStrategy: 'bottom' | 'top' | 'alphabetical';
}
```

2. **Update findInsertionPoint**:
```typescript
function findInsertionPoint(content: string, method: ParsedMethod, existingMethods: ParsedMethod[], strategy: string) {
  if (strategy === 'bottom') {
    // ... existing logic
  } else if (strategy === 'top') {
    // Find first method, insert before it
  } else if (strategy === 'alphabetical') {
    // Find alphabetically correct position
  }
}
```

---

## Performance Considerations

### Current Performance

**Typical Merge (10 files, 50 methods each)**:
- Parse time: ~50ms per file
- Diff analysis: ~10ms per file
- User interaction: Depends on user (can be slow)
- Total: < 5 seconds excluding user interaction

### Bottlenecks

1. **Re-parsing after each insertion** (lines 1042-1047)
   - Necessary for accuracy
   - Could be optimized by caching parse trees and updating incrementally

2. **Temp file writes** (lines 1043-1044)
   - Writes temp file for every insertion to re-parse
   - Could keep in-memory instead

3. **Regex parsing** (lines 150-272)
   - Relatively fast for typical files
   - Could be slow for very large files (10,000+ lines)

### Optimization Opportunities

1. **Parse Once Strategy**:
```typescript
// Instead of:
for (const method of newMethods) {
  insertMethod();
  writeTemp();
  reparse();  // ← Expensive
}

// Do:
const allInsertions = calculateInsertionPoints(newMethods);
const modifiedContent = applyAllInsertions(content, allInsertions);
writeOnce(modifiedContent);
```

2. **Incremental Updates**:
```typescript
// Track line offset after each insertion
let lineOffset = 0;
for (const method of newMethods) {
  const adjustedLine = method.insertLine + lineOffset;
  insertAt(adjustedLine);
  lineOffset += methodLineCount;
}
```

3. **Parallel Parsing**:
```typescript
// Parse multiple files in parallel
const parsedFiles = await Promise.all(
  fileOperations.map(file => parseCSharpFile(file.path))
);
```

### Scalability

**Current Limits**:
- Files: Tested up to 50 files
- Methods per file: Tested up to 100 methods
- File size: Tested up to 10,000 lines

**Recommendations**:
- For > 100 methods: Use auto mode to avoid prompt fatigue
- For > 50 files: Run in batches
- For > 10,000 line files: Manual merge recommended

---

## Known Issues

### Issue 1: Expression-Bodied Syntax Not Supported

**Symptom**: Methods like `public int Foo() => _bar;` not detected.

**Cause**: Regex requires `{` opening brace.

**Workaround**: Manually add these methods, or convert to full body in templates.

**Fix** (future): Update regex to support `=>` syntax.

### Issue 2: Multi-Line Parameters Not Always Detected

**Symptom**: Methods with parameters split across many lines might not match.

**Cause**: Regex uses non-greedy `[\s\S]*?` for parameters.

**Workaround**: Keep parameters on one line or few lines.

**Fix** (future): More sophisticated parsing.

### Issue 3: Partial Classes Not Supported

**Symptom**: If class is split across multiple files, only one file is processed.

**Cause**: Agent assumes single-file classes.

**Workaround**: Merge partial classes manually.

**Fix** (future): Detect partial classes and merge across files.

### Issue 4: Recursive Prompts Could Stack Overflow

**Symptom**: If user views code 1000+ times, could overflow.

**Cause**: `promptMethodMerge()` recursively calls itself (line 438).

**Likelihood**: Extremely low (user would need to view 1000+ times).

**Workaround**: None needed (unlikely scenario).

**Fix** (future): Use loop instead of recursion.

### Issue 5: Temp Files Left if Process Crashes

**Symptom**: `.temp` files remain after crash.

**Cause**: Cleanup only happens at end (line 1122-1130).

**Workaround**: Manually delete `.temp` files.

**Fix** (future): Register cleanup handler for process exit.

---

## Contributing

### Code Style

- Use TypeScript strict mode
- Add comments for complex logic
- Use descriptive variable names
- Keep functions focused (single responsibility)

### Adding Features

1. Update spec: `.claude/tasks/AGENT_InteractiveTemplateMerge_SPEC.md`
2. Implement feature
3. Update this developer guide
4. Update user guide (README)
5. Add tests
6. Update implementation summary

### Submitting Changes

1. Create feature branch
2. Make changes
3. Test manually with multiple entities
4. Update documentation
5. Create pull request

---

## References

- **Spec**: `.claude/tasks/AGENT_InteractiveTemplateMerge_SPEC.md`
- **User Guide**: `agents/README_InteractiveTemplateMerge.md`
- **Implementation Summary**: `.claude/tasks/InteractiveTemplateMerge_IMPLEMENTATION_SUMMARY.md`
- **Source Code**: `agents/interactive-template-merge.ts`

---

**Version**: 1.0
**Last Updated**: 2026-01-16
**Maintainer**: ClaudeOnshoreConversionAgent Team
