# Quick Start Guide

## Installation

```bash
cd C:\source\agents\ClaudeOnshoreConversionAgent
bun install
```

## Convert an Entity (Recommended Method)

### For Facility Conversion:

```bash
# Option 1: With both entity and form name
bun run agents/orchestrator.ts --entity "Facility" --form-name "frmFacilitySearch"

# Option 2: With form name only (entity extracted automatically)
bun run agents/orchestrator.ts --form-name "frmFacilitySearch"

# Option 3: With entity only
bun run agents/orchestrator.ts --entity "Facility"

# Option 4: Interactive - select from available forms
bun run agents/orchestrator.ts
```

**What happens:**
1. Steps 1-10 extract analysis data automatically
2. Analysis files saved to `output/Facility/`

**Then run Step 11 separately to generate templates:**
```bash
bun run generate-template-api --entity "Facility"
bun run generate-template-ui --entity "Facility"
# or run both in sequence
bun run generate-templates --entity "Facility"
```

This launches Claude Code interactively for template generation. You can rerun this step multiple times without re-running the analysis.

### For Crewing Conversion:

```bash
bun run agents/orchestrator.ts --form-name "frmCrewingSearch"
# or
bun run agents/orchestrator.ts --entity "Crewing"
```

## Output Location

All analysis and templates are saved to:
```
output/{EntityName}/
├── form-structure-search.json
├── form-structure-detail.json
├── business-logic.json
├── data-access.json
├── security.json
├── ui-mapping.json
├── workflow.json
├── tabs.json
├── validation.json
├── related-entities.json
├── conversion-plan-api.md      ← API + Shared conversion plan
├── conversion-plan-ui.md       ← UI conversion plan (detail screens)
└── templates/                   ← Code templates
    ├── shared/                  ← BargeOps.Shared DTOs (create first!)
    ├── api/                     ← BargeOps.API templates
    └── ui/                      ← BargeOps.UI templates
```

## Common Commands

```bash
# Full conversion (all 11 steps)
bun run agents/orchestrator.ts --entity "Facility"

# Skip certain steps (if already done)
bun run agents/orchestrator.ts --entity "Facility" --skip-steps "1,2,3"

# Custom output directory
bun run agents/orchestrator.ts --entity "Facility" --output "./my-output"

# Run individual agent interactively
bun run agents/form-structure-analyzer.ts --entity "Facility" --form-type "Search" --interactive

# Generate templates only (assumes analysis already done)
bun run generate-template-api --entity "Facility"
bun run generate-template-ui --entity "Facility"
bun run generate-templates --entity "Facility"
```

## Agent Overview

### Analysis Agents (Run via Orchestrator)

| # | Agent Name | Purpose | Output File |
|---|------------|---------|-------------|
| 1 | Form Structure Analyzer | Extract UI controls | `form-structure-*.json` |
| 2 | Business Logic Extractor | Extract business rules | `business-logic.json` |
| 3 | Data Access Analyzer | Extract stored procedures | `data-access.json` |
| 4 | Security Extractor | Extract permissions | `security.json` |
| 5 | UI Component Mapper | Map legacy to modern | `ui-mapping.json` |
| 6 | Form Workflow Analyzer | Extract user flows | `workflow.json` |
| 7 | Detail Tab Analyzer | Extract tab structure | `tabs.json` |
| 8 | Validation Extractor | Extract validation | `validation.json` |
| 9 | Related Entity Analyzer | Extract relationships | `related-entities.json` |

### Interactive Implementation Agents (Run Separately)

| Agent | Purpose | Usage |
|-------|---------|-------|
| Template Generator | Generate conversion plan and code templates | `bun run generate-template --entity "Facility"` |
| Conversion Planner | Create comprehensive conversion plans | `bun run plan-conversion --entity "Facility"` |
| Entity Converter | Help implement entities with Dapper | `bun run entity-convert --entity "Facility"` |
| ViewModel Creator | Create ViewModels following MVVM | `bun run viewmodel-create --entity "Facility" --form-type Search` |

## Examples

### Example 1: Converting Facility (Full Process)

```bash
# Step 1: Run orchestrator
bun run agents/orchestrator.ts --entity "Facility"

# Step 2: Review output
code output/Facility/conversion-plan-api.md
code output/Facility/conversion-plan-ui.md

# Step 3: Implement using generated templates
# Templates are in: output/Facility/templates/
```

### Example 2: Re-running Just Template Generation

If you've already run agents 1-9 and want to regenerate templates:

```bash
bun run generate-template-api --entity "Facility"
bun run generate-template-ui --entity "Facility"
# or run both in sequence
bun run generate-templates --entity "Facility"
```

This launches Claude Code interactively to regenerate templates.

### Example 3: Using Interactive Implementation Agents

After running the orchestrator, use interactive agents for implementation help:

```bash
# Step 1: Plan the conversion
bun run plan-conversion --entity "Facility"
# Creates: .claude/tasks/Facility_IMPLEMENTATION_STATUS.md

# Step 2: Get help implementing the entity
bun run entity-convert --entity "Facility" "Implement the Facility DTO with all properties"
# Interactive session - helps create DTO in BargeOps.Shared

# Step 3: Create ViewModels for each form
bun run viewmodel-create --entity "Facility" --form-type Search
# Interactive session - helps create FacilitySearchViewModel

bun run viewmodel-create --entity "Facility" --form-type Detail
# Interactive session - helps create FacilityEditViewModel
```

### Example 4: Debugging a Specific Agent

Run an individual agent interactively to debug:

```bash
bun run agents/business-logic-extractor.ts --entity "Facility" --interactive
```

## Tips

1. **Always start with the orchestrator** - It runs all agents in the correct order
2. **Use interactive agents during implementation** - They provide targeted help for specific tasks
3. **Template generation is optional** - You can use the interactive agents directly after orchestrator
4. **Interactive agents can be rerun** - Iterate on entities and ViewModels as needed
5. **Check examples/** - Review `examples/Crewing/` for reference implementations
6. **Output is entity-specific** - Each entity gets its own `output/{EntityName}/` folder

## Troubleshooting

### Error: "entity parameter is required"
**Solution**: Always provide `--entity` flag:
```bash
bun run agents/orchestrator.ts --entity "Facility"
```

### Error: "File not found"
**Solution**: Ensure you're in the project root:
```bash
cd C:\source\agents\ClaudeOnshoreConversionAgent
```

### Agent fails during execution
**Solution**: Run the specific agent interactively to debug:
```bash
bun run agents/{agent-name}.ts --entity "Facility" --interactive
```

## Next Steps

After analysis completes, you have multiple options for implementation:

### Option 1: Generate Templates First (Recommended)
```bash
# Generate conversion plans and templates interactively
bun run generate-template-api --entity "Facility"
bun run generate-template-ui --entity "Facility"
# or run both in sequence
bun run generate-templates --entity "Facility"
```

**What it generates:**
- Conversion plan document
- Shared DTOs (create first!)
- API templates (Controllers, Services, Repositories)
- UI templates (Controllers, Views, JavaScript)
- **ViewModels** (Search, Edit, Details, ListItem) ⭐ NEW!

**Interactive prompts:**
- During the session, you'll be asked which ViewModels to generate
- Choose: Search, Edit, Details, and/or ListItem ViewModels
- Each ViewModel follows MVVM patterns with proper validation

Then review:
1. `output/Facility/conversion-plan-api.md` - API + Shared conversion strategy
2. `output/Facility/conversion-plan-ui.md` - UI conversion strategy (detail screens)
3. `output/Facility/templates/shared/` - Shared DTOs (create first!)
4. `output/Facility/templates/api/` - API code templates
5. `output/Facility/templates/ui/` - UI code templates (including ViewModels!)

### Option 2: Use Interactive Implementation Agents

These agents provide interactive help during implementation:

```bash
# Plan the conversion with dependency management
bun run plan-conversion --entity "Facility"

# Get help implementing entities with Dapper data access
bun run entity-convert --entity "Facility"
bun run entity-convert --entity "Facility" "Add auditing fields"

# Get help creating ViewModels following MVVM patterns
bun run viewmodel-create --entity "Facility" --form-type Search
bun run viewmodel-create --entity "Facility" --form-type Detail
```

### Implementation Order

1. **FIRST**: Implement Shared DTOs in `src/BargeOps.Shared/BargeOps.Shared/Dto/`
2. Implement API components in `src/BargeOps.API/` (Controllers, Services, Repositories)
3. Implement UI components in `src/BargeOps.UI/` (Controllers, ViewModels, Views)
4. Test and iterate

**Tip**: Use the interactive agents throughout implementation for specific help!

## Reference

- **Examples**: `examples/Crewing/` - Complete examples
- **Settings**: `settings/` - Agent configurations
- **README**: Full documentation
