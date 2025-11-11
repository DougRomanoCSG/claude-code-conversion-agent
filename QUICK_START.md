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
bun run generate-template --entity "Facility"
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
├── conversion-plan.md          ← Main conversion plan
└── templates/                   ← Code templates
    ├── api/                     ← BargeOps.Admin.API templates
    └── ui/                      ← BargeOps.Admin.UI templates
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
bun run agents/conversion-template-generator.ts --entity "Facility"
```

## Agent Overview

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
| 10 | Template Generator | Generate code (INTERACTIVE) | `conversion-plan.md` |

## Examples

### Example 1: Converting Facility (Full Process)

```bash
# Step 1: Run orchestrator
bun run agents/orchestrator.ts --entity "Facility"

# Step 2: Review output
code output/Facility/conversion-plan.md

# Step 3: Implement using generated templates
# Templates are in: output/Facility/templates/
```

### Example 2: Re-running Just Template Generation

If you've already run agents 1-9 and want to regenerate templates:

```bash
bun run agents/conversion-template-generator.ts --entity "Facility"
```

This launches Claude Code interactively to regenerate templates.

### Example 3: Debugging a Specific Agent

Run an individual agent interactively to debug:

```bash
bun run agents/business-logic-extractor.ts --entity "Facility" --interactive
```

## Tips

1. **Always start with the orchestrator** - It runs all agents in the correct order
2. **Agent 10 is interactive** - You'll interact with Claude Code to refine templates
3. **Check examples/** - Review `examples/Crewing/` for reference implementations
4. **Output is entity-specific** - Each entity gets its own `output/{EntityName}/` folder

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

After conversion plan is generated:
1. Review `output/{Entity}/conversion-plan.md`
2. Examine code templates in `output/{Entity}/templates/`
3. Implement in BargeOps.Admin.API
4. Implement in BargeOps.Admin.UI
5. Test and iterate

## Reference

- **Examples**: `examples/Crewing/` - Complete examples
- **Settings**: `settings/` - Agent configurations
- **README**: Full documentation
