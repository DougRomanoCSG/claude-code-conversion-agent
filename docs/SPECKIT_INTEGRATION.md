# GitHub SpecKit Integration Guide

## Overview

This guide explains how to integrate **GitHub SpecKit** into the ClaudeOnshoreConversionAgent project to enhance specification-driven development and improve the conversion workflow.

## What is SpecKit?

SpecKit is a toolkit from GitHub for **specification-driven development** that:
- Emphasizes writing specifications first (the *what* and *why*)
- Creates executable artifacts that AI agents can use
- Provides structured workflows: `/speckit.specify` → `/speckit.plan` → `/speckit.tasks` → `/speckit.implement`
- Organizes specs in a `.specify/` directory structure

## Why Use SpecKit for This Project?

### Current State
- ✅ Custom orchestrator runs 10 analysis agents
- ✅ Generates JSON analysis files
- ✅ Creates markdown conversion plans (`conversion-plan-api.md`, `conversion-plan-ui.md`)
- ✅ Generates code templates

### Benefits of SpecKit Integration
1. **Structured Specifications**: Formalize conversion requirements as executable specs
2. **Better AI Agent Integration**: SpecKit specs are designed to be consumed by AI agents
3. **Task Management**: Built-in task breakdown and tracking
4. **Consistency**: Standardized format across all entity conversions
5. **Traceability**: Link specs → plans → tasks → implementation
6. **Validation**: Built-in checklists and analysis commands

## Integration Strategy

### Option 1: Hybrid Approach (Recommended)

Keep existing orchestrator but enhance with SpecKit for specification management:

```
Current Flow:
  Analysis Agents (1-10) → JSON Files → Conversion Plans (MD) → Templates

Enhanced Flow:
  Analysis Agents (1-10) → JSON Files → SpecKit Specs → Conversion Plans → Tasks → Templates
```

### Option 2: Full SpecKit Workflow

Replace conversion plan generation with SpecKit commands:

```
SpecKit Flow:
  /speckit.constitution → /speckit.specify → /speckit.clarify → 
  /speckit.plan → /speckit.tasks → /speckit.implement
```

## Installation

### Step 1: Install SpecKit CLI

```bash
# Using uv (recommended)
uv tool install spec-kit

# Or using pip
pip install spec-kit

# Or using npm
npm install -g spec-kit
```

### Step 2: Initialize SpecKit in Project

```bash
cd C:\source\agents\ClaudeOnshoreConversionAgent

# Initialize with Claude as the AI agent
specify init . --ai claude --script ps
```

This creates:
- `.specify/` directory with configuration
- `specs/` directory for feature specs
- Local scripts for SpecKit commands

## Project Structure with SpecKit

```
ClaudeOnshoreConversionAgent/
├── .specify/                    # SpecKit configuration
│   ├── constitution.md          # Project principles
│   ├── templates/               # Custom prompt templates
│   └── memory/                  # Agent memory files
├── specs/                       # SpecKit feature specs
│   ├── Vendor/
│   │   ├── spec.md             # Functional specification
│   │   ├── plan.md             # Technical plan
│   │   ├── tasks.md            # Task breakdown
│   │   └── implementation.md   # Implementation notes
│   ├── Facility/
│   └── ...
├── output/                      # Existing analysis output
│   └── {Entity}/
│       ├── *.json              # Analysis files (unchanged)
│       └── templates/          # Generated templates
└── agents/                      # Existing agents (unchanged)
```

## Integration Patterns

### Pattern 1: SpecKit as Specification Layer

Use SpecKit to create formal specs from analysis data:

```typescript
// New agent: spec-generator.ts
// Reads analysis JSON files and generates SpecKit specs
```

**Workflow:**
1. Run orchestrator (agents 1-10) → generates JSON files
2. Run spec generator → creates `specs/{Entity}/spec.md`
3. Use `/speckit.plan` to create technical plan
4. Use `/speckit.tasks` to break down implementation
5. Generate templates using existing template generators

### Pattern 2: SpecKit as Planning Tool

Use SpecKit commands during interactive template generation:

**Enhanced Template Generator:**
- After analysis, use `/speckit.specify` to create formal spec
- Use `/speckit.plan` to define architecture
- Use `/speckit.tasks` to break down work
- Then generate templates

### Pattern 3: SpecKit as Documentation Standard

Use SpecKit format for all conversion plans:

**Convert existing plans to SpecKit format:**
- `conversion-plan-api.md` → `specs/{Entity}/plan-api.md`
- `conversion-plan-ui.md` → `specs/{Entity}/plan-ui.md`
- Add `spec.md` with functional requirements
- Add `tasks.md` with implementation tasks

## Implementation Steps

### Step 1: Create SpecKit Constitution

Create `.specify/constitution.md` with project principles:

```markdown
# ClaudeOnshoreConversionAgent Constitution

## Core Principles
1. **Legacy Preservation**: Maintain all business logic from VB.NET forms
2. **Modern Architecture**: Target ASP.NET Core MVC with proper separation
3. **Pattern Consistency**: Follow BargeOps.Admin.Mono patterns
4. **Incremental Conversion**: One entity at a time, fully tested

## Conversion Standards
- Shared DTOs created first (BargeOps.Shared)
- Repository pattern with Dapper
- Service layer for business logic
- MVC controllers inherit AppController
- ViewModels use MVVM pattern (no ViewBag/ViewData)
- Bootstrap 5 + DataTables for UI
```

### Step 2: Create Spec Template Generator

Create `agents/spec-generator.ts`:

```typescript
// Reads analysis JSON files
// Generates SpecKit-compliant spec.md files
// Uses existing analysis data to populate specs
```

### Step 3: Enhance Template Generators

Modify existing template generators to:
1. Read SpecKit specs if they exist
2. Use spec data to inform template generation
3. Generate SpecKit tasks.md alongside templates

### Step 4: Add SpecKit Commands to Package.json

```json
{
  "scripts": {
    "spec:init": "specify init . --ai claude --script ps",
    "spec:create": "bun run agents/spec-generator.ts",
    "spec:plan": "echo 'Use /speckit.plan in Claude Code'",
    "spec:tasks": "echo 'Use /speckit.tasks in Claude Code'"
  }
}
```

## Example: Vendor Entity with SpecKit

### 1. After Analysis (Existing Flow)

```bash
bun run agents/orchestrator.ts --entity "Vendor" --form-name "frmVendorSearch"
```

Generates JSON files in `output/Vendor/`

### 2. Generate SpecKit Spec (New)

```bash
bun run agents/spec-generator.ts --entity "Vendor"
```

Creates `specs/Vendor/spec.md`:

```markdown
# Vendor Module Specification

## Overview
Convert legacy VB.NET Vendor search and detail forms to ASP.NET Core MVC.

## Functional Requirements
- Search vendors by name, code, status
- View vendor details with 4 tabs
- Manage vendor contacts inline
- Manage vendor business units
- Configure portal groups
- BargeEx EDI settings

## User Stories
1. As an admin, I can search for vendors...
2. As an admin, I can edit vendor details...
...

## Business Rules
[Extracted from business-logic.json]
- Vendor codes must be unique
- Active vendors cannot be deleted
...

## UI Requirements
[Extracted from form-structure-*.json]
- Search form with DataTables grid
- Detail form with Bootstrap tabs
...
```

### 3. Create Technical Plan

In Claude Code, use:
```
/speckit.plan
```

This creates `specs/Vendor/plan.md` with:
- Technical stack decisions
- Architecture patterns
- API design
- UI component choices

### 4. Break Down Tasks

In Claude Code, use:
```
/speckit.tasks
```

This creates `specs/Vendor/tasks.md` with:
- Task 1: Create Shared DTOs
- Task 2: Implement Repository
- Task 3: Create API Service
- Task 4: Build API Controller
- Task 5: Create ViewModels
- Task 6: Build MVC Controller
- Task 7: Create Razor Views
- Task 8: Add JavaScript
- Task 9: Testing
- Task 10: Deployment

### 5. Generate Templates (Existing)

```bash
bun run generate-template-api --entity "Vendor"
bun run generate-template-ui --entity "Vendor"
```

Templates now reference SpecKit specs for context.

## SpecKit Commands Reference

### In Claude Code

| Command | Purpose | When to Use |
|---------|---------|-------------|
| `/speckit.constitution` | Define project principles | Once, at project setup |
| `/speckit.specify` | Create functional spec | After analysis, before planning |
| `/speckit.clarify` | Clarify requirements | If spec is unclear |
| `/speckit.plan` | Create technical plan | After spec, before tasks |
| `/speckit.tasks` | Break down tasks | After plan, before implementation |
| `/speckit.implement` | Execute implementation | After tasks are defined |
| `/speckit.analyze` | Analyze code quality | During/after implementation |
| `/speckit.checklist` | Validation checklist | Before completion |

### CLI Commands

```bash
# Initialize project
specify init . --ai claude

# Check prerequisites
specify check

# List available commands
specify --help
```

## Benefits for This Project

### 1. Better Documentation
- Formal specs instead of ad-hoc markdown
- Structured format that AI agents understand
- Traceability from requirements to implementation

### 2. Improved Template Generation
- Template generators can read SpecKit specs
- More context-aware code generation
- Better alignment with requirements

### 3. Task Management
- Built-in task breakdown
- Progress tracking
- Implementation checklists

### 4. Consistency
- Standard format across all entities
- Reusable templates
- Predictable structure

### 5. AI Agent Optimization
- SpecKit specs are designed for AI consumption
- Better prompt context
- More accurate code generation

## Migration Path

### Phase 1: Pilot (Recommended)
1. Install SpecKit
2. Create constitution
3. Manually create SpecKit spec for one entity (e.g., Vendor)
4. Test workflow
5. Gather feedback

### Phase 2: Automation
1. Create `spec-generator.ts` agent
2. Automate spec creation from analysis
3. Integrate with template generators
4. Update documentation

### Phase 3: Full Integration
1. Convert all existing plans to SpecKit format
2. Update all agents to use SpecKit
3. Standardize on SpecKit workflow
4. Train team on SpecKit commands

## Example Workflow

### Complete Vendor Conversion with SpecKit

```bash
# Step 1: Analysis (existing)
bun run agents/orchestrator.ts --entity "Vendor" --form-name "frmVendorSearch"

# Step 2: Generate SpecKit spec (new)
bun run agents/spec-generator.ts --entity "Vendor"

# Step 3: In Claude Code - Create plan
/speckit.plan

# Step 4: In Claude Code - Break down tasks
/speckit.tasks

# Step 5: Generate templates (existing, but enhanced)
bun run generate-template-api --entity "Vendor"
bun run generate-template-ui --entity "Vendor"

# Step 6: Implementation (existing)
# Copy templates to BargeOps.Admin.Mono
# Implement and test

# Step 7: Validation (new)
/speckit.checklist
/speckit.analyze
```

## Customization

### Custom Templates

Create `.specify/templates/` with custom prompts:
- `conversion-spec.md` - Template for conversion specs
- `conversion-plan.md` - Template for conversion plans
- `conversion-tasks.md` - Template for task breakdown

### Memory Files

Store project-specific knowledge in `.specify/memory/`:
- `bargeops-patterns.md` - BargeOps architecture patterns
- `namespace-conventions.md` - Naming conventions
- `reference-examples.md` - Links to reference code

## Troubleshooting

### Issue: SpecKit not found
**Solution**: Install via `uv tool install spec-kit` or add to PATH

### Issue: Commands not working in Claude Code
**Solution**: Ensure SpecKit is initialized and `.specify/` directory exists

### Issue: Specs not aligning with analysis
**Solution**: Update `spec-generator.ts` to better map JSON to SpecKit format

## Resources

- [SpecKit Documentation](https://speckit.org)
- [SpecKit GitHub](https://github.com/github/spec-kit)
- [SpecKit Quickstart](https://github.github.com/spec-kit/quickstart.html)

## Next Steps

1. **Install SpecKit**: `uv tool install spec-kit`
2. **Initialize**: `specify init . --ai claude --script ps`
3. **Create Constitution**: Edit `.specify/constitution.md`
4. **Pilot Test**: Create spec for Vendor entity manually
5. **Build Agent**: Create `spec-generator.ts` to automate
6. **Integrate**: Update template generators to use specs
7. **Document**: Update README with SpecKit workflow

## Questions?

- Check SpecKit docs: https://speckit.org
- Review existing conversion plans for patterns
- Test with one entity before full rollout
