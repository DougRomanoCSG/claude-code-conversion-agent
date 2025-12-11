# System Prompts

This directory contains system prompt templates for specialized agents used in the onshore conversion project. These prompts define the behavior, responsibilities, and best practices for different types of conversion work.

## Quick Reference

| System Prompt | Agent Script | Purpose |
|--------------|-------------|---------|
| **Conversion Agents** |
| `entity-conversion-prompt.md` | `entity-converter.ts` | Convert entities with Dapper repositories |
| `viewmodel-generator-prompt.md` | `viewmodel-creator.ts` | Create ViewModels following MVVM |
| `conversion-orchestrator-prompt.md` | `conversion-planner.ts` | Plan multi-component conversions |
| **Analysis Agents** |
| `form-structure-analyzer-prompt.md` | `form-structure-analyzer.ts` | Extract UI controls and layout |
| `business-logic-extractor-prompt.md` | `business-logic-extractor.ts` | Extract business rules and validation |
| `data-access-analyzer-prompt.md` | `data-access-analyzer.ts` | Extract database patterns |
| `security-extractor-prompt.md` | `security-extractor.ts` | Extract authorization requirements |
| `validation-extractor-prompt.md` | `validation-extractor.ts` | Extract validation rules |
| `related-entity-analyzer-prompt.md` | `related-entity-analyzer.ts` | Map entity relationships |
| `form-workflow-analyzer-prompt.md` | `form-workflow-analyzer.ts` | Extract user workflows |
| `detail-tab-analyzer-prompt.md` | `detail-tab-analyzer.ts` | Extract tab structures |
| `ui-component-mapper-prompt.md` | `ui-component-mapper.ts` | Map legacy to modern controls |
| `conversion-template-generator-prompt.md` | `conversion-template-generator.ts` | Generate conversion templates |
| `orchestrator-prompt.md` | `orchestrator.ts` | Coordinate all analysis agents |

## Available Prompts

### System Prompt-Based Agents (New)

#### entity-conversion-prompt.md
Specialized agent for converting and migrating entities in ASP.NET Core applications using Dapper. Use this prompt when:
- Creating or modifying entity models
- Setting up Dapper repositories and stored procedure mappings
- Implementing relationship loading through service layer
- Converting legacy entities to new structure

**Key Features:**
- Entity analysis and relationship mapping
- ViewModel integration guidance
- Implementation status documentation
- Safety checks for data integrity

**Agent**: `agents/entity-converter.ts`

#### viewmodel-generator-prompt.md
Specialized agent for creating ViewModels following the MVVM pattern. Use this prompt when:
- Creating new ViewModels for views
- Mapping entity properties to presentation layer
- Setting up validation attributes
- Designing UI-specific properties

**Key Features:**
- MVVM pattern enforcement
- Common ViewModel patterns (Create/Edit, List, Details)
- Validation best practices
- SelectListItem population patterns

**Agent**: `agents/viewmodel-creator.ts`

#### conversion-orchestrator-prompt.md
Orchestrator agent for managing complex multi-component conversions. Use this prompt when:
- Planning large-scale conversions
- Managing dependencies between components
- Coordinating entity, service, and presentation layer changes
- Creating comprehensive conversion plans

**Key Features:**
- Dependency mapping and sequencing
- Phase-based conversion approach
- Comprehensive documentation templates
- Quality assurance checklists

**Agent**: `agents/conversion-planner.ts`

### Analysis Agent System Prompts

#### form-structure-analyzer-prompt.md
Extracts complete UI structures from legacy VB.NET Windows Forms.

**Purpose**: Document all controls, grids, validation, and layout
**Output**: `form-structure-{formType}.json`
**Agent**: `agents/form-structure-analyzer.ts`

#### business-logic-extractor-prompt.md
Analyzes legacy business objects for rules, validation, and domain logic.

**Purpose**: Extract business rules, properties, methods, and relationships
**Output**: `business-logic.json`
**Agent**: `agents/business-logic-extractor.ts`

#### data-access-analyzer-prompt.md
Extracts database interaction patterns and stored procedure usage.

**Purpose**: Document queries, parameters, and data transformations
**Output**: `data-access.json`
**Agent**: `agents/data-access-analyzer.ts`

#### security-extractor-prompt.md
Analyzes authorization patterns and security requirements.

**Purpose**: Extract permissions, roles, and security controls
**Output**: `security.json`
**Agent**: `agents/security-extractor.ts`

#### validation-extractor-prompt.md
Extracts complete validation rules from forms and business objects.

**Purpose**: Document all validation logic and error messages
**Output**: `validation.json`
**Agent**: `agents/validation-extractor.ts`

#### related-entity-analyzer-prompt.md
Identifies and documents entity relationships.

**Purpose**: Map one-to-many, many-to-one relationships and cascading
**Output**: `related-entities.json`
**Agent**: `agents/related-entity-analyzer.ts`

#### form-workflow-analyzer-prompt.md
Extracts user workflows and interaction patterns.

**Purpose**: Document user flows, state transitions, and navigation
**Output**: `workflow.json`
**Agent**: `agents/form-workflow-analyzer.ts`

#### detail-tab-analyzer-prompt.md
Extracts tab structures from detail forms.

**Purpose**: Document tab organization and child entity grids
**Output**: `tabs.json`
**Agent**: `agents/detail-tab-analyzer.ts`

#### ui-component-mapper-prompt.md
Maps legacy controls to modern web UI components.

**Purpose**: Provide modern equivalents for legacy Windows Forms controls
**Output**: `ui-mapping.json`
**Agent**: `agents/ui-component-mapper.ts`

#### conversion-template-generator-prompt.md
Generates comprehensive conversion implementation plans.

**Purpose**: Create detailed step-by-step conversion templates
**Output**: `CONVERSION_TEMPLATE.md`
**Agent**: `agents/conversion-template-generator.ts`

#### orchestrator-prompt.md
Coordinates execution of multiple analysis agents in sequence.

**Purpose**: Run all analysis agents in correct order with proper data flow
**Output**: `ANALYSIS_SUMMARY.json`
**Agent**: `agents/orchestrator.ts`

## How to Use

### Creating Custom Agents
You can reference these prompts when launching specialized agents:

```bash
# Example: Use the entity conversion prompt for a specific task
claude --prompt @system-prompts/entity-conversion-prompt.md "Convert the Facility entity"
```

### Extending System Prompts
When creating new system prompts:

1. **Follow the Universal Best Practices** section pattern
2. **Define Core Responsibilities** clearly
3. **Provide concrete examples** and patterns
4. **Include checklists** for validation
5. **Reference project conventions** from CLAUDE.md

### Common Patterns

All system prompts in this project should:
- Emphasize IdentityConstants.ApplicationScheme (not "Cookies")
- Prefer ViewModels over ViewBag/ViewData
- Add comments sparingly (only for complex logic)
- Include precise file paths in references
- Follow existing project patterns

## Project-Specific Guidelines

### Documentation Location
Entity-specific implementation status files should be created in:
```
.claude/tasks/{EntityName}_IMPLEMENTATION_STATUS.md
```

### File Organization
- ViewModels: `ViewModels/{EntityName}{Purpose}ViewModel.cs`
- Entities: Follow existing entity location in main project
- Controllers: Follow existing controller patterns

### Validation Approach
- Use data annotations for basic validation
- Create custom validators only when necessary
- Test validation in unit tests

## Adding New Prompts

When adding new system prompts to this directory:

1. **Choose a descriptive name**: `{purpose}-prompt.md`
2. **Include standard sections**:
   - Title and overview
   - Universal Best Practices
   - Core Responsibilities
   - Approach/Process
   - Output Guidelines
   - Best Practices
   - Examples/Patterns

3. **Align with project standards**:
   - Reference CLAUDE.md conventions
   - Use existing patterns from other prompts
   - Include project-specific guidance

4. **Document the prompt**: Add it to this README

## Best Practices

### Prompt Design
- Be specific about expected outputs
- Include concrete examples
- Define success criteria clearly
- Provide safety checklists

### Usage
- Use the most specific prompt for the task
- Combine prompts when working across layers
- Reference project CLAUDE.md for standards
- Update prompts as patterns evolve

### Maintenance
- Keep prompts aligned with project conventions
- Update when new patterns emerge
- Remove outdated guidance
- Test prompts with real scenarios

## References

- Main project instructions: `../CLAUDE.md`
- Task tracking: `../.claude/tasks/`
- Example implementations: See completed entities in main project

## Future Prompts

Consider adding specialized prompts for:
- Authorization/security patterns
- Testing strategies
- Performance optimization
- API design
- Database migration planning
- Legacy code refactoring
