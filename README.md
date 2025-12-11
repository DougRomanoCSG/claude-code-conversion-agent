# Claude Onshore Conversion Agent

A specialized suite of TypeScript agents designed to systematically extract, analyze, and generate conversion templates for migrating legacy VB.NET Windows Forms applications to modern ASP.NET Core MVC architecture.

## Overview

This project provides **10 specialized agents** that work together to automate the conversion process from legacy OnShore VB.NET Windows Forms to modern BargeOps.API and BargeOps.UI applications.

### Key Features

- **Automated Analysis**: 10 specialized agents extract form structure, business logic, data access patterns, security, validation, and more
- **Orchestrated Workflow**: Master orchestrator runs all agents in sequence
- **Interactive Template Generation**: Agent 10 always runs interactively in Claude Code for collaborative template creation
- **Entity-Specific Outputs**: Each conversion creates organized output in `output/{EntityName}/`
- **Code Examples**: Includes reference implementations from BargeOps.Crewing for clarity
- **Target-Specific**: Generates code specifically for BargeOps.Shared (DTOs), BargeOps.API, and BargeOps.UI

## Quick Start

### Prerequisites

- [Bun](https://bun.sh) >= 1.0.0
- [Claude Code CLI](https://claude.ai/code)
- Access to OnShore legacy codebase
- Access to BargeOps.Admin.Mono monorepo (BargeOps.Shared, BargeOps.API, BargeOps.UI)

### Installation

```bash
cd C:\source\agents\ClaudeOnshoreConversionAgent
bun install
```

### Configuration

The input directory for OnShore source files is configured in `config.json`:

```json
{
  "inputDirectory": "C:\\source\\OnShore\\apps\\Onshore",
  "paths": {
    "forms": "Forms",
    "businessObjects": "BusinessLogic/Business Objects",
    "businessObjectsBase": "BusinessLogic/Business Objects/Base Classes",
    "lists": "BusinessLogic/Lists"
  },
  "referenceProjects": {
    "crewingApi": "C:\\source\\BargeOps.Crewing.API",
    "crewingUi": "C:\\source\\BargeOps.Crewing.UI"
  },
  "targetProjects": {
    "monorepo": "C:\\Dev\\BargeOps.Admin.Mono",
    "adminShared": "C:\\Dev\\BargeOps.Admin.Mono\\src\\BargeOps.Shared",
    "adminApi": "C:\\Dev\\BargeOps.Admin.Mono\\src\\BargeOps.API",
    "adminUi": "C:\\Dev\\BargeOps.Admin.Mono\\src\\BargeOps.UI"
  }
}
```

**To change the input directory:**
1. Edit `config.json` and update the `inputDirectory` path
2. The `paths` object defines relative subdirectories within the input directory
3. All agents will automatically use the configured paths

**To change reference projects:**
1. Edit `config.json` and update the `referenceProjects` paths
2. These paths point to the BargeOps.Crewing projects used as examples for ASP.NET MVC patterns
3. Agents will reference these projects when generating code templates

**To change target projects:**
1. Edit `config.json` and update the `targetProjects` paths
2. These paths point to where the generated code will be placed
3. Generated code templates will be structured for these target projects

**Default Configuration:**
- **Input Directory**: `C:\source\OnShore\apps\Onshore`
- **Forms**: `{inputDirectory}/Forms/`
- **Business Objects**: `{inputDirectory}/BusinessLogic/Business Objects/`
- **Lists**: `{inputDirectory}/BusinessLogic/Lists/`
- **Reference Projects** (for examples):
  - **BargeOps.Crewing.API**: `C:\source\BargeOps.Crewing.API` (Example API patterns)
  - **BargeOps.Crewing.UI**: `C:\source\BargeOps.Crewing.UI` (Example UI patterns)
- **Target Projects** (where code will be generated):
  - **BargeOps.Admin.Mono**: `C:\Dev\BargeOps.Admin.Mono` (Monorepo root)
  - **BargeOps.Shared**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.Shared` (Shared DTOs - create first!)
  - **BargeOps.API**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API` (API project)
  - **BargeOps.UI**: `C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI` (UI project)

### Basic Usage

#### Option 1: Use the Orchestrator (Recommended)

Run all 11 steps automatically. You can provide either entity name, form name, or both:

```bash
# With both entity and form name
bun run agents/orchestrator.ts --entity "Facility" --form-name "frmFacilitySearch"

# With form name only (entity will be extracted automatically)
bun run agents/orchestrator.ts --form-name "frmFacilitySearch"

# With entity only (form names will be constructed as frm{Entity}Search and frm{Entity}Detail)
bun run agents/orchestrator.ts --entity "Facility"

# Interactive mode - will prompt you to select from available forms
bun run agents/orchestrator.ts
```

This will:
1. Run agents 1-10 automatically to extract analysis data
2. Save all analysis files to `output/{Entity}/`

**After analysis completes, run Step 11 separately:**
```bash
bun run generate-template --entity "Facility"
# or
bun run agents/conversion-template-generator.ts --entity "Facility"
```

This will launch the interactive template generator in Claude Code to generate the conversion plan and code templates.

#### Option 2: Run Individual Agents

```bash
# Analyze search form
bun run agents/form-structure-analyzer.ts --entity "Facility" --form-type "Search"

# Analyze detail form
bun run agents/form-structure-analyzer.ts --entity "Facility" --form-type "Detail"

# Extract business logic
bun run agents/business-logic-extractor.ts --entity "Facility"

# Extract data access patterns
bun run agents/data-access-analyzer.ts --entity "Facility"

# And so on...
```

#### Option 3: Interactive Mode for Debugging

```bash
bun run agents/form-structure-analyzer.ts --entity "Facility" --form-type "Search" --interactive
```

### NPM Scripts

```bash
# Run full orchestrator
bun run convert --entity "Facility"

# Run individual agents
bun run analyze-form --entity "Facility" --form-type "Search"
bun run analyze-business --entity "Facility"
bun run analyze-data --entity "Facility"

# Generate templates (always interactive)
bun run generate-template --entity "Facility"

# Code quality
bun run lint
bun run format
bun run check
```

## The 10 Specialized Agents

### Agent 1: Form Structure Analyzer
**Purpose**: Extract UI components, controls, and layouts from Windows Forms

**Extracts**:
- All controls (textboxes, dropdowns, grids, buttons, checkboxes)
- Grid column definitions from `FormatGridColumns`
- Event handler mappings
- Validation patterns from `AreFieldsValid`
- Dropdown population methods
- Layout structure (panels, tabs)

**Output**: `form-structure-search.json` or `form-structure-detail.json`

### Agent 2: Business Logic Extractor
**Purpose**: Extract business rules and validation from business objects

**Extracts**:
- All properties with types and access modifiers
- Business rules from `CheckBusinessRules` method
- `BrokenRules.Assert` calls
- Initialization logic
- Factory methods (`New*`, `Get*`)
- CRUD operation patterns

**Output**: `business-logic.json`

### Agent 3: Data Access Pattern Analyzer
**Purpose**: Extract stored procedures and query patterns

**Extracts**:
- Stored procedure names and parameters
- Search criteria from `AddFetchParameters`
- Result column mappings from `ReadRow`
- CRUD operations
- Data formatting logic

**Output**: `data-access.json`

### Agent 4: Security & Authorization Extractor
**Purpose**: Extract permissions and authorization patterns

**Extracts**:
- SubSystem identifiers from `InitializeBase`
- Button security from `SetButtonTypes`
- `ControlAuthorization.SetButtonType` calls
- Permission requirements
- Button type to permission attribute mappings

**Output**: `security.json`

### Agent 5: UI Component Mapper
**Purpose**: Map legacy controls to modern equivalents

**Mappings**:
- `UltraGrid` → DataTables
- `UltraCombo` → Select2
- `UltraPanel` → Bootstrap Card
- `UltraTabControl` → Bootstrap Nav Tabs
- TextBox → Bootstrap Form Input

**Output**: `ui-mapping.json`

### Agent 6: Form Workflow Analyzer
**Purpose**: Extract user flows and state management

**Extracts**:
- Event handler chains
- Form lifecycle methods
- State persistence patterns
- Modal dialog patterns
- Refresh/update triggers

**Output**: `workflow.json`

### Agent 7: Detail Form Tab Analyzer
**Purpose**: Extract tab structure and related entities

**Extracts**:
- Tab definitions from Designer file
- Controls per tab
- Related entity grids
- Toolbar button configurations
- Shared controls (submit/cancel)

**Output**: `tabs.json`

### Agent 8: Validation Rule Extractor
**Purpose**: Extract all validation logic

**Extracts**:
- Form validation from `AreFieldsValid`
- Business rules from `CheckBusinessRules`
- Field-level constraints
- Error messages
- Validation triggers

**Output**: `validation.json`

### Agent 9: Related Entity Analyzer
**Purpose**: Extract entity relationships

**Extracts**:
- Child collection properties
- CRUD methods for related entities
- Grid structures for related entities
- Parent-child key relationships

**Output**: `related-entities.json`

### Step 11: Conversion Template Generator (ALWAYS INTERACTIVE)
**Purpose**: Generate complete conversion plan, code templates, and ViewModels

**Usage**: Run separately after steps 1-10 complete:
```bash
bun run generate-template --entity "Facility"
```

**Generates**:
- Comprehensive conversion plan document
- **Shared DTOs** (BargeOps.Shared - create first!)
- Repository interface and implementation
- Service interface and implementation
- API Controller with endpoints
- **ViewModels** (Search, Edit, Details, ListItem) - Interactive prompts
- Razor views (Index, Edit, Details)
- JavaScript files (DataTables)
- Step-by-step implementation guide

**Interactive ViewModel Generation**:
- During the session, you'll be asked which ViewModels to generate
- Choose from: SearchViewModel, EditViewModel, DetailsViewModel, ListItemViewModel
- Each ViewModel follows MVVM patterns with proper validation and display attributes

**Output**:
- `conversion-plan.md` - Main conversion plan document
- `templates/shared/` - DTOs for BargeOps.Shared (create first!)
- `templates/api/` - Code templates for BargeOps.API
- `templates/ui/` - Code templates for BargeOps.UI

**NOTE**: 
- This step **ALWAYS** runs interactively in Claude Code
- Can be rerun multiple times without re-running analysis steps 1-10
- Reads analysis data from `output/{Entity}/` directory

## Project Structure

```
ClaudeOnshoreConversionAgent/
├── agents/                          # All 10 specialized agents + orchestrator
│   ├── orchestrator.ts             # Master orchestrator
│   ├── form-structure-analyzer.ts  # Agent 1
│   ├── business-logic-extractor.ts # Agent 2
│   ├── data-access-analyzer.ts     # Agent 3
│   ├── security-extractor.ts       # Agent 4
│   ├── ui-component-mapper.ts      # Agent 5
│   ├── form-workflow-analyzer.ts   # Agent 6
│   ├── detail-tab-analyzer.ts      # Agent 7
│   ├── validation-extractor.ts     # Agent 8
│   ├── related-entity-analyzer.ts  # Agent 9
│   └── conversion-template-generator.ts  # Agent 10 (interactive)
├── lib/                            # Shared utilities
│   ├── claude-flags.types.ts
│   ├── flags.ts
│   └── paths.ts
├── settings/                       # Agent configurations
│   ├── form-analyzer.settings.json
│   ├── business-logic.settings.json
│   ├── template-generator.settings.json
│   └── ...
├── prompts/                        # System prompts (optional)
├── output/                         # Generated analysis (entity-specific)
│   ├── Facility/
│   │   ├── form-structure-search.json
│   │   ├── business-logic.json
│   │   ├── conversion-plan.md
│   │   └── templates/
│   └── Crewing/
├── examples/                       # Reference examples
│   ├── Facility/
│   └── Crewing/
│       ├── CrewingController.cs    # Example API controller
│       └── CrewingSearchController.cs  # Example UI controller
├── scripts/                        # Build utilities
├── bin/                            # Compiled binaries (generated)
├── package.json
├── tsconfig.json
├── biome.json
└── README.md
```

## Output Structure

Each entity conversion creates organized output:

```
output/Facility/
├── form-structure-search.json      # Agent 1 output (search form)
├── form-structure-detail.json      # Agent 1 output (detail form)
├── business-logic.json             # Agent 2 output
├── data-access.json                # Agent 3 output
├── security.json                   # Agent 4 output
├── ui-mapping.json                 # Agent 5 output
├── workflow.json                   # Agent 6 output
├── tabs.json                       # Agent 7 output
├── validation.json                 # Agent 8 output
├── related-entities.json           # Agent 9 output
├── conversion-plan.md              # Step 11 output (primary)
└── templates/                      # Step 11 output (code templates)
    ├── shared/                     # BargeOps.Shared (create first!)
    │   └── Dto/
    │       ├── {Entity}Dto.cs
    │       ├── {Entity}SearchRequest.cs
    │       └── Admin/              # If admin-specific
    ├── api/                        # BargeOps.API templates
    │   ├── Controllers/
    │   ├── Services/
    │   ├── Repositories/
    │   └── DataAccess/Sql/
    └── ui/                         # BargeOps.UI templates
        ├── Models/                 # ViewModels
        ├── Controllers/
        ├── Views/
        ├── wwwroot/js/
        └── wwwroot/css/
```

## Workflow Example: Converting "Facility" Entity

### Step 1: Run the Orchestrator

```bash
bun run agents/orchestrator.ts --entity "Facility"
```

This will execute steps 1-10:

1. **Step 1 (Search Form)**: Extract `frmFacilitySearch.vb` structure
2. **Step 2 (Detail Form)**: Extract `frmFacilityDetail.vb` structure
3. **Step 3**: Extract `FacilityLocation.vb` business logic
4. **Step 4**: Extract `FacilityLocationSearch.vb` data access
5. **Step 5**: Extract security patterns
6. **Step 6**: Map UI components
7. **Step 7**: Analyze workflows
8. **Step 8**: Analyze detail form tabs
9. **Step 9**: Extract validation rules
10. **Step 10**: Analyze related entities

### Step 2: Run Template Generator (Step 11)

After analysis completes, run the template generator:

```bash
bun run generate-template --entity "Facility"
```

This launches Claude Code in interactive mode. You can:
- Review extracted data
- Ask questions about patterns
- Request specific code examples
- Iterate on generated templates
- Clarify implementation details
- **Rerun this step multiple times** without re-running analysis

### Step 3: Review Generated Output

Check the generated output:
- `output/Facility/conversion-plan.md` - Complete conversion strategy and implementation guide
- `output/Facility/templates/shared/` - Shared DTOs (create these first in BargeOps.Shared!)
- `output/Facility/templates/api/` - All API code templates (repositories, services, controllers, SQL)
- `output/Facility/templates/ui/` - All UI code templates (ViewModels, Razor views, JavaScript)

## Advanced Usage

### Skip Specific Steps

```bash
bun run agents/orchestrator.ts --entity "Facility" --skip-steps "1,2,5"
```

### Custom Output Directory

```bash
bun run agents/orchestrator.ts --entity "Facility" --output "./custom/path"
```

### Run Individual Agent Interactively

```bash
bun run agents/business-logic-extractor.ts --entity "Facility" --interactive
```

## Reference Patterns

This project references Admin screen examples from BargeOps.Crewing projects, configured in `config.json`:

### BargeOps.Crewing.API Examples
Located at: `C:\source\BargeOps.Crewing.API` (configured in `config.json`)

**Where to find Admin screen patterns:**
- **API Controllers**: `src/Crewing.Api/Controllers/`
  - Look for: `CrewingController.cs`, `BoatController.cs`, or similar Admin screen controllers
- **Domain Models**: `src/Crewing.Domain/Models/`
  - Look for: `Crewing.cs`, `Boat.cs`, or similar entity models
- **DTOs**: `src/Crewing.Domain/Dto/`
  - Look for: `CrewingDto.cs`, `CrewingSearchRequest.cs`, `CrewingSearchResponse.cs`
- **Repositories**: `src/Crewing.Infrastructure/Repositories/`
  - Look for: `ICrewingRepository.cs`, `CrewingRepository.cs`
- **Services**: `src/Crewing.Domain/Services/`
  - Look for: `ICrewingService.cs`, `CrewingService.cs`
- **AutoMapper**: `src/Crewing.Infrastructure/Mappings/`
  - Look for: `CrewingMappingProfile.cs`

### BargeOps.Crewing.UI Examples
Located at: `C:\source\BargeOps.Crewing.UI` (configured in `config.json`)

**Where to find Admin screen patterns:**
- **MVC Controllers**: `Controllers/`
  - Look for: `CrewingSearchController.cs`, `BoatSearchController.cs`, or similar Admin search controllers
- **View Models**: `Models/`
  - Look for: `CrewingSearchViewModel.cs`, `CrewingEditViewModel.cs`, `CrewingListModel.cs`
- **Razor Views**: `Views/`
  - Look for: `CrewingSearch/Index.cshtml`, `CrewingSearch/Edit.cshtml`, `CrewingSearch/Details.cshtml`
- **JavaScript**: `wwwroot/js/`
  - Look for: `crewingSearch.js`, `boatSearch.js` - DataTables initialization and form handling
- **CSS**: `wwwroot/css/`
  - Look for: `crewingSearch.css`, `boatSearch.css` - Custom styling
- **Services**: `Services/`
  - Look for: `ICrewingService.cs`, `CrewingService.cs` - UI service layer

### BargeOps.Admin.Mono Target Patterns
Located at: `C:\Dev\BargeOps.Admin.Mono` (configured in `config.json`)

**Primary reference (canonical Admin patterns):**

**Shared DTOs (create first!):**
- Location: `src/BargeOps.Shared/BargeOps.Shared/Dto/`
- Namespace: `BargeOps.Shared.Dto`
- Example: `BoatLocation.cs`, `BoatLocationDto.cs`

**API Project:**
- Location: `src/BargeOps.API/`
- **Controllers**: `src/Admin.Api/Controllers/BoatLocationController.cs`
- **Services**: `src/Admin.Api/Services/BoatLocationService.cs`
- **Repository Interfaces**: `src/Admin.Infrastructure/Abstractions/IBoatLocationRepository.cs`
- **Repository Implementations**: `src/Admin.Infrastructure/Repositories/BoatLocationRepository.cs`
- **SQL Files**: `src/Admin.Infrastructure/DataAccess/Sql/BoatLocation_*.sql`

**UI Project:**
- Location: `src/BargeOps.UI/`
- **MVC Controllers**: `Controllers/BoatLocationSearchController.cs`
- **ViewModels**: `Models/BoatLocationSearchViewModel.cs`, `BoatLocationEditViewModel.cs`
- **Razor Views**: `Views/BoatLocationSearch/Index.cshtml`, `Edit.cshtml`
- **JavaScript**: `wwwroot/js/boatLocationSearch.js`
- **Services**: `Services/IBargeService.cs` (inherits BargeOpsAdminBaseService)

**Note**: All paths are configurable in `config.json`. The agents automatically use these configured paths to provide specific example locations in their prompts.

## Target Architecture

**Shared Layer** (BargeOps.Shared) - **Create First!**:
- DTOs in `BargeOps.Shared.Dto` namespace
- Repository Interfaces in `BargeOps.Shared.Interfaces`
- Service Interfaces in `BargeOps.Shared.Services`

**API Layer** (BargeOps.API):
- Repository Pattern (Dapper with SQL files as embedded resources)
- Service Layer (business logic)
- API Controllers (inherit ApiControllerBase)
- SQL queries in `.sql` files

**UI Layer** (BargeOps.UI):
- MVC Controllers (inherit AppController)
- ViewModels (MVVM pattern - NO ViewBag/ViewData)
- Razor Views
- Bootstrap 5
- DataTables (server-side)
- Select2
- jQuery

## Development

### Code Quality

```bash
# Format code
bun run format

# Lint and fix
bun run lint

# Check without fixing
bun run check
```

### Compiling Agents

```bash
# Compile all agents to binaries
bun run compile
```

Binaries will be created in `bin/` directory.

## FAQ

### Q: Why is Step 11 run separately?

**A**: Step 11 (Conversion Template Generator) is run separately so you can:
- Rerun template generation multiple times without re-running analysis
- Iterate on templates with different approaches
- Regenerate templates after reviewing analysis data
- Save time by not re-running all 10 analysis steps

The template generator always runs interactively in Claude Code, allowing you to:
- Review extracted data before generation
- Ask clarifying questions
- Request specific examples
- Iterate on templates
- Ensure quality and accuracy

### Q: Can I run agents in parallel?

**A**: No, the orchestrator runs agents sequentially. Each agent builds on the output of previous agents. However, you can run individual agents manually if needed.

### Q: Where should I look for examples?

**A**: Check the `examples/Crewing/` directory for complete examples of:
- API Controllers (BargeOps.Admin.API)
- UI Controllers (BargeOps.Admin.UI)
- Reference patterns from BargeOps.Crewing

### Q: What if an agent fails?

**A**: The orchestrator will stop and report which step failed. You can:
1. Fix the issue
2. Re-run the orchestrator
3. Use `--skip-steps` to skip completed steps
4. Run the failed agent individually with `--interactive` for debugging

### Q: Can I customize agent behavior?

**A**: Yes, modify the settings files in `settings/` to adjust:
- Tool permissions
- MCP configurations
- Hooks
- Output formats

## Support

For issues or questions:
1. Check existing examples in `examples/`
2. Review agent output in `output/`
3. Run agents with `--interactive` for debugging
4. Consult BargeOps.Admin and BargeOps.Crewing reference implementations

## License

UNLICENSED - Internal BargeOps use only
