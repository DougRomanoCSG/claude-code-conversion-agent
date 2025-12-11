# Watch and Compile Workflow

## Overview

The ClaudeOnshoreConversionAgent project now includes an automated watch and compile workflow, following the same pattern as `claude-workshop-live`. This allows you to automatically rebuild agents when source files change.

## Key Concepts

### When is `gen-assets.ts` run?

The `gen-assets.ts` script is **automatically run before every compilation** in both workflows:

1. **Watch Mode** (`bun run watch`)
   - Runs `gen-assets.ts` before initial build
   - Runs `gen-assets.ts` before every rebuild when files change
   - This ensures compiled binaries always include the latest prompts and settings

2. **Manual Compile** (`bun compile <file>`)
   - Runs `gen-assets.ts` before compiling the specific agent
   - Ensures the single binary includes all current assets

### Why This Matters

Because system prompts, inline prompts, and settings are **statically imported at compile time**, any changes to these files require:
1. Regenerating `lib/assets.gen.ts` (via `gen-assets.ts`)
2. Recompiling the agent binaries

The watch workflow handles both steps automatically.

## Available Commands

### Watch Mode (Recommended for Development)

```bash
bun run watch
```

**What it does:**
1. Generates `lib/assets.gen.ts` from all prompts and settings
2. Compiles all agents in `agents/` directory to `bin/`
3. Watches for changes in:
   - `agents/*.ts` - Agent source files
   - `system-prompts/*.md` - System prompts
   - `prompts/*.md` - Inline prompts
   - `settings/*.json` - Settings and MCP configs
4. On any change:
   - Regenerates `lib/assets.gen.ts`
   - Recompiles **all** agents (to ensure consistency)
5. Handles deleted agents by removing corresponding binaries

**When to use:**
- During active development
- When editing system prompts or settings
- When making changes to multiple agents

**Output:**
```
Watching agents, prompts, system-prompts, settings for changes...
Will rebuild ALL agents on any change in these directories
Performing initial build...
  Generating asset maps...
  ✓ Asset maps generated
Found 14 agent(s) to compile
  Compiling business-logic-extractor...
  ✓ Compiled business-logic-extractor
  ...
All agents rebuilt!

Watching for changes... Press Ctrl+C to stop.
```

### Manual Compile

```bash
bun compile agents/ui-component-mapper.ts
```

**What it does:**
1. Generates `lib/assets.gen.ts` (ensures latest assets)
2. Compiles the specific agent to a standalone binary in `bin/`

**When to use:**
- When you only need to compile a specific agent
- For production builds
- When testing a single agent

**Output:**
```
Generating asset maps...
✓ Generated lib/assets.gen.ts
  System Prompts: 15
  Prompts: 0
  Settings: 13
  MCP Configs: 10
Compiling ui-component-mapper...
✓ Compiled ui-component-mapper to ./bin/ui-component-mapper
```

### Manual Asset Generation

```bash
bun run gen-assets
```

**What it does:**
- Scans `system-prompts/`, `prompts/`, and `settings/` directories
- Generates `lib/assets.gen.ts` with static imports
- Does NOT compile agents

**When to use:**
- When you want to verify asset generation without compiling
- For troubleshooting import issues
- Rarely needed (watch and compile do this automatically)

## The Watch Workflow in Detail

### File Structure

```
ClaudeOnshoreConversionAgent/
├── agents/              # Source TypeScript files
│   ├── ui-component-mapper.ts
│   ├── form-structure-analyzer.ts
│   └── ...
├── bin/                 # Compiled binaries (auto-generated)
│   ├── ui-component-mapper
│   ├── form-structure-analyzer
│   └── ...
├── system-prompts/      # System prompts (watched)
│   ├── ui-component-mapper-prompt.md
│   └── ...
├── prompts/            # Inline prompts (watched)
├── settings/           # Settings and MCP configs (watched)
│   ├── ui-mapper.settings.json
│   ├── ui-mapper.mcp.json
│   └── ...
├── lib/
│   ├── assets.ts       # Asset resolution logic
│   └── assets.gen.ts   # Generated imports (auto-generated)
└── scripts/
    ├── watch-agents.ts  # Watch and rebuild script
    ├── compile.ts       # Single-file compile script
    └── gen-assets.ts    # Asset map generator
```

### Trigger Events

The watch script monitors these file changes:

| Directory | Files Watched | Triggers |
|-----------|---------------|----------|
| `agents/` | `*.ts` (except README) | Full rebuild |
| `system-prompts/` | `*.md` | Full rebuild |
| `prompts/` | `*.md` | Full rebuild |
| `settings/` | `*.json` | Full rebuild |

### Debouncing

Changes are debounced by 100ms to prevent multiple rapid rebuilds during batch edits.

### Why Rebuild All Agents?

When a system prompt or settings file changes, the watch script rebuilds **all agents** (not just the affected one) because:

1. **Shared assets**: Some prompts or settings might be shared across agents
2. **Consistency**: Ensures all binaries are compiled with the same asset versions
3. **Simplicity**: Easier to reason about than tracking dependencies

This is the same approach used by `claude-workshop-live`.

## Workflow Examples

### Example 1: Editing a System Prompt

```bash
# Terminal 1: Start watch mode
bun run watch

# Terminal 2: Edit the system prompt
vim system-prompts/ui-component-mapper-prompt.md
# Save changes

# Watch detects change and automatically:
# 1. Regenerates lib/assets.gen.ts
# 2. Recompiles all agents
# 3. Binaries in bin/ now include updated prompt
```

### Example 2: Creating a New Agent

```bash
# Terminal 1: Start watch mode
bun run watch

# Terminal 2: Create new agent files
echo '#!/usr/bin/env -S bun run' > agents/my-new-agent.ts
# ... write agent code ...

echo '# My Agent System Prompt' > system-prompts/my-new-agent-prompt.md
# ... write system prompt ...

echo '{}' > settings/my-new-agent.settings.json
# ... write settings ...

# Watch detects changes and automatically:
# 1. Regenerates lib/assets.gen.ts (includes new prompt/settings)
# 2. Compiles my-new-agent to bin/my-new-agent
# 3. Compiles all other agents (for consistency)
```

### Example 3: Deleting an Agent

```bash
# Terminal 1: Watch mode running
bun run watch

# Terminal 2: Delete agent
rm agents/old-agent.ts

# Watch detects deletion and automatically:
# 1. Removes bin/old-agent binary
# 2. Rebuilds remaining agents
```

## Production Builds

For production or distribution, use manual compile:

```bash
# Compile all agents individually (optional)
bun compile agents/ui-component-mapper.ts
bun compile agents/form-structure-analyzer.ts
# ... etc

# Or use watch once to build all
bun run watch
# Press Ctrl+C after initial build completes
```

## Comparison with claude-workshop-live

| Feature | claude-workshop-live | ClaudeOnshoreConversionAgent |
|---------|---------------------|------------------------------|
| Watch Command | `bun run watch` | `bun run watch` ✅ |
| Compile Command | `bun compile <file>` | `bun compile <file>` ✅ |
| Auto gen-assets | ✅ Before every build | ✅ Before every build |
| Watch Directories | agents, prompts, system-prompts, settings | agents, prompts, system-prompts, settings ✅ |
| Debouncing | 100ms | 100ms ✅ |
| Full Rebuild on Changes | ✅ Yes | ✅ Yes |
| Binary Output | `bin/` | `bin/` ✅ |
| Handles Deletions | ✅ Yes | ✅ Yes |

## Troubleshooting

### Watch not detecting changes
- Ensure you're in the project root directory
- Check that the files you're editing match the watched patterns (`.ts`, `.md`, `.json`)
- Some editors use atomic writes; try saving differently

### Compilation errors
- Check `lib/assets.gen.ts` was generated correctly
- Verify all imports in your agent are correct
- Run `bun run gen-assets` manually to see detailed output

### Binaries not updating
- Stop watch (Ctrl+C) and restart
- Delete `bin/` directory and rebuild
- Check for TypeScript errors in your agents

### Memory or performance issues
- Watch rebuilds all agents on every change
- For large projects, consider using manual compile for specific agents
- Close watch when not actively developing

## Best Practices

1. **Keep watch running during development**: It ensures you're always testing with the latest code and prompts

2. **Edit prompts and settings freely**: The watch automatically picks up changes and rebuilds

3. **Test compiled binaries**: Run `./bin/agent-name` to test the compiled binary directly

4. **Commit `lib/assets.gen.ts`**: While auto-generated, committing it helps with debugging and ensures consistent builds

5. **Use `.gitignore` for `bin/`**: Binaries are large and should not be committed to git

## Summary

The watch and compile workflow provides:
- ✅ **Automatic rebuilds** when any source file changes
- ✅ **Asset regeneration** before every build
- ✅ **Consistent binaries** by rebuilding all agents together
- ✅ **Developer convenience** with minimal manual steps
- ✅ **Pattern consistency** with claude-workshop-live

This ensures that your compiled agents always include the latest system prompts, inline prompts, and settings without manual intervention.
