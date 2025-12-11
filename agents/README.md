# Conversion Agents

This directory contains specialized agents for converting legacy VB.NET Windows Forms to ASP.NET Core MVC. The agents are built using the Claude Agent SDK and leverage system prompts for specialized behavior.

## Agent Types

### 🔧 Analysis Agents (Orchestrated)

These are the extraction agents that analyze legacy forms and generate templates:

- **orchestrator.ts** - Master agent that coordinates all analysis steps (1-9/10)
- **form-structure-analyzer.ts** - Step 1: Extracts UI components and controls from forms
- **business-logic-extractor.ts** - Step 2: Extracts business rules and validation logic
- **data-access-analyzer.ts** - Step 3: Analyzes queries and stored procedures
- **security-extractor.ts** - Step 4: Extracts authorization patterns and permissions
- **ui-component-mapper.ts** - Step 5: Maps legacy controls to modern equivalents
- **form-workflow-analyzer.ts** - Step 6: Extracts user flows and state management
- **detail-tab-analyzer.ts** - Step 7: Extracts tab structure and related entities
- **validation-extractor.ts** - Step 8: Extracts all validation logic
- **related-entity-analyzer.ts** - Step 9: Extracts entity relationships
- **conversion-template-generator.ts** - Step 10: Interactive template generation (run separately)

**Orchestrator Usage:**
```bash
# Interactive - prompts for form selection
bun run agents/orchestrator.ts

# With form name (entity auto-extracted)
bun run agents/orchestrator.ts --form-name "frmFacilitySearch"

# With entity and form name
bun run agents/orchestrator.ts --entity "Facility" --form-name "frmFacilitySearch"

# Single form (non-Search/Detail)
bun run agents/orchestrator.ts --form-name "frmFuelPrices"

# Skip specific steps
bun run agents/orchestrator.ts --entity "Facility" --skip-steps "3,5,7"
```

**Template Generator Usage:**
```bash
# Generate conversion templates interactively
bun run generate-template --entity "Facility"

# Or direct invocation
bun run agents/conversion-template-generator.ts --entity "Facility"
```

### 🚀 System Prompt Agents (New)

These agents use specialized system prompts for guided conversion work:

#### entity-converter.ts
Converts and migrates entities with proper EF Core relationships.

**Uses:** `system-prompts/entity-conversion-prompt.md`

**Usage:**
```bash
# Direct command
bun run agents/entity-converter.ts "Convert the Facility entity"

# With entity flag
bun run agents/entity-converter.ts --entity Facility

# Specific task
bun run agents/entity-converter.ts --entity Facility "Add navigation properties for FacilityBerth"

# Via npm script
bun run entity-convert --entity Facility
```

**Features:**
- Entity analysis and relationship mapping
- ViewModel integration guidance
- Implementation status documentation
- Safety checks for data integrity
- Follows project MVVM patterns

#### viewmodel-creator.ts
Creates ViewModels following MVVM patterns with validation and display attributes.

**Uses:** `system-prompts/viewmodel-generator-prompt.md`

**Usage:**
```bash
# Direct command
bun run agents/viewmodel-creator.ts "Create a ViewModel for Facility list view"

# With entity flag
bun run agents/viewmodel-creator.ts --entity Facility

# Specific form type
bun run agents/viewmodel-creator.ts --entity Facility --form-type Detail

# Via npm script
bun run viewmodel-create --entity Facility --form-type Search
```

**Features:**
- MVVM pattern enforcement
- Common ViewModel patterns (Create/Edit, List, Details)
- Validation best practices
- SelectListItem population patterns
- Display and validation attributes

#### conversion-planner.ts
Creates comprehensive conversion plans with dependency management.

**Uses:** `system-prompts/conversion-orchestrator-prompt.md`

**Usage:**
```bash
# Direct command
bun run agents/conversion-planner.ts "Plan the Facility conversion"

# With entity flag
bun run agents/conversion-planner.ts --entity Facility

# Complex planning
bun run agents/conversion-planner.ts "Create a conversion plan for frmFacilitySearch and frmFacilityDetail"

# Via npm script
bun run plan-conversion --entity Facility
```

**Features:**
- Dependency mapping and sequencing
- Phase-based conversion approach
- Comprehensive documentation templates
- Quality assurance checklists
- Risk assessment and mitigation

## NPM Scripts

### System Prompt Agents (Interactive)
```bash
# Entity conversion with EF Core relationships
bun run entity-convert [--entity <name>] [prompt]

# ViewModel creation following MVVM patterns
bun run viewmodel-create [--entity <name>] [--form-type <type>] [prompt]

# Conversion planning and coordination
bun run plan-conversion [--entity <name>] [prompt]
```

### Analysis & Template Generation
```bash
# Run orchestrator (Steps 1-9)
bun run convert [--entity <name>] [--form-name <name>]
bun run agents/orchestrator.ts --entity "Facility"

# Generate templates (Step 10) - INTERACTIVE
bun run generate-template --entity <name>
bun run agents/conversion-template-generator.ts --entity "Facility"
```

### Individual Analysis Agents (Usually run via orchestrator)
```bash
bun run analyze-form            # Step 1: Form structure
bun run analyze-business        # Step 2: Business logic
bun run analyze-data            # Step 3: Data access
bun run analyze-security        # Step 4: Security patterns
```

### Development & Build
```bash
bun run compile                 # Compile all agents to bin/
bun run scripts/compile-all.ts  # Direct compilation
```

## Typical Workflows

### Workflow 1: Full Analysis + Template Generation (Recommended)
When starting conversion of a legacy form:

```bash
# 1. Run orchestrator to extract all data from legacy forms (Steps 1-9)
bun run agents/orchestrator.ts --form-name "frmFacilitySearch"
# OR for single forms:
bun run agents/orchestrator.ts --form-name "frmFuelPrices"

# 2. Review analysis outputs in output/Facility/
# - form-structure-search.json, form-structure-detail.json
# - business-logic.json, data-access.json
# - security.json, ui-mapping.json, workflow.json, tabs.json
# - validation.json, related-entities.json
# - child-forms.json (if child forms detected)

# 3. Generate conversion templates interactively (Step 10)
bun run generate-template --entity "Facility"
# This launches interactive Claude session to create:
# - conversion-plan.md
# - Code templates in templates/shared/, templates/api/, templates/ui/

# 4. Review and implement generated templates
```

### Workflow 2: Plan-First Conversion (New Entity)
When creating a new entity from scratch:

```bash
# 1. Create a comprehensive plan
bun run plan-conversion --entity Facility

# 2. Convert the entity based on the plan
bun run entity-convert --entity Facility "Implement the conversion plan"

# 3. Create ViewModels for each form
bun run viewmodel-create --entity Facility --form-type Search
bun run viewmodel-create --entity Facility --form-type Detail
```

### Workflow 3: Iterative Enhancement
When improving existing conversions:

```bash
# Add specific functionality
bun run entity-convert --entity Facility "Add auditing fields CreatedBy and CreatedDate"

# Enhance ViewModels
bun run viewmodel-create --entity Facility "Add pagination support to the list ViewModel"

# Update documentation
bun run plan-conversion --entity Facility "Update implementation status"
```

## Common Flags

All system prompt agents support these Claude CLI flags:

- `--debug` - Enable debug mode
- `--verbose` - Show verbose output
- `--continue` - Continue most recent conversation
- `--model <name>` - Use specific model (sonnet, opus, haiku)
- `--settings <file>` - Load custom settings
- `--add-dir <path>` - Add additional working directory

## Output Locations

### Analysis Agents (Orchestrator Steps 1-9)
```
output/<Entity>/
├── form-structure-search.json    # Step 1a: Search form UI
├── form-structure-detail.json    # Step 1b: Detail form UI (or form-structure.json for single forms)
├── business-logic.json           # Step 2: Business rules
├── data-access.json              # Step 3: Queries/SPs
├── security.json                 # Step 4: Authorization
├── ui-mapping.json               # Step 5: Control mapping
├── workflow.json                 # Step 6: User flows
├── tabs.json                     # Step 7: Tab structure
├── validation.json               # Step 8: Validation rules
├── related-entities.json         # Step 9: Relationships
└── child-forms.json              # Detected child forms
```

### Template Generator (Step 10)
```
output/<Entity>/
├── conversion-plan.md            # Comprehensive conversion guide
└── templates/
    ├── shared/                   # BargeOps.Shared DTOs (create first!)
    │   └── Dto/
    │       ├── {Entity}Dto.cs
    │       ├── {Entity}SearchRequest.cs
    │       └── {Child}Dto.cs
    ├── api/                      # API components
    │   ├── Controllers/
    │   ├── Repositories/
    │   └── Services/
    └── ui/                       # UI components
        ├── Controllers/
        ├── Services/
        ├── ViewModels/
        ├── Views/
        └── wwwroot/js/
```

### System Prompt Agents
```
.claude/tasks/
└── {Entity}_IMPLEMENTATION_STATUS.md  # Implementation tracking
```

## Project Conventions

All agents follow these project standards from `CLAUDE.md`:

- ✅ Always use `IdentityConstants.ApplicationScheme` (not "Cookies")
- ✅ Use MVVM pattern: ViewModels over @ViewBag/@ViewData
- ✅ Add comments sparingly, only for complex issues
- ✅ Entity-specific files go in `.claude/tasks/{EntityName}_*.md`

## System Prompts

The system prompt agents use markdown files in `system-prompts/`:

- `entity-conversion-prompt.md` - Entity conversion guidelines
- `viewmodel-generator-prompt.md` - ViewModel creation patterns
- `conversion-orchestrator-prompt.md` - Conversion planning strategies

See `system-prompts/README.md` for details on creating custom prompts.

## Adding New Agents

To create a new system-prompt-based agent:

1. **Create system prompt** in `system-prompts/my-agent-prompt.md`
2. **Create agent script** in `agents/my-agent.ts`:
   ```typescript
   #!/usr/bin/env -S bun run
   import { spawn } from "bun";
   import { buildClaudeFlags, getPositionals, parsedArgs, resolvePath } from "../lib/flags";
   import myPrompt from "../system-prompts/my-agent-prompt.md" with { type: "text" };
   import mySettings from "../settings/my-agent.settings.json" with { type: "json" };

   const projectRoot = resolvePath("../", import.meta.url);

   async function main() {
       const prompt = getPositionals().join(" ").trim();
       const flags = buildClaudeFlags({
           "append-system-prompt": myPrompt,
           settings: JSON.stringify(mySettings),
       });

       const child = spawn(["claude", ...flags, prompt], {
           stdin: "inherit",
           stdout: "inherit",
           stderr: "inherit",
           env: { ...process.env, CLAUDE_PROJECT_DIR: projectRoot },
       });

       await child.exited;
       process.exit(child.exitCode ?? 0);
   }
   await main();
   ```

3. **Create settings file** in `settings/my-agent.settings.json`
4. **Add npm script** to `package.json`:
   ```json
   "my-agent": "bun run agents/my-agent.ts"
   ```
5. **Make executable**: `chmod +x agents/my-agent.ts`
6. **Document** in this README

## Troubleshooting

### Agent won't run
- Ensure Bun is installed: `bun --version`
- Check file is executable: `chmod +x agents/<agent>.ts` (Unix/Mac)
- Verify Claude CLI is installed: `claude --version`
- For Windows: Use `bun run agents/<agent>.ts` instead of direct execution

### "MCP config file not found" error
- Ensure the MCP config path is correctly resolved
- Check that `settings/<agent>.mcp.json` exists
- Verify path resolution uses `path.join()` for Windows compatibility
- Don't pass JSON content to `--mcp-config`, pass the file path

### "Input must be provided" error (Template Generator)
- The conversion-template-generator requires an initial prompt
- This is automatically provided by the script
- If you see this error, check that the `initialPrompt` variable is being passed to args

### System prompt not applied
- Check the import path in the agent file
- Verify system prompt file exists in `system-prompts/`
- Ensure `with { type: "text" }` is used in import
- Verify it's passed via `--append-system-prompt` flag, not as positional arg

### Settings not loading
- Verify settings file exists in `settings/`
- Check JSON syntax is valid
- Ensure `with { type: "json" }` is used in import
- Settings should be stringified: `JSON.stringify(settings)`

### Orchestrator fails mid-step
- Check output directory exists and is writable
- Review the specific agent's output for errors
- Use `--skip-steps` to skip problematic steps: `--skip-steps "3,5"`
- Each step outputs to `output/<Entity>/<file>.json`

### Child forms not detected
- Ensure the Forms directory path is correct
- Check that form files exist in the expected location
- Child forms are detected by scanning for `ShowDialog()` calls

## Resources

- [Claude Agent SDK Documentation](https://docs.anthropic.com/claude/docs)
- [System Prompts Guide](../system-prompts/README.md)
- [Project Conventions](../CLAUDE.md)
- [Conversion Task Documentation](../.claude/tasks/)

## Support

For issues or questions:
- Check existing documentation in `.claude/tasks/`
- Review system prompt guidelines
- Consult the main project CLAUDE.md
- Review conversion examples in the codebase
