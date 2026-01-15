# `.claude/tasks` index

This folder contains **agent session artifacts** (status, summaries, and runbooks). These are meant to help you resume work in Claude Code, not to be shipped as application output.

## Conventions

- **Entity-specific status files**: prefix with the entity name, for example:
  - `Facility_IMPLEMENTATION_STATUS.md`
  - `Vendor_IMPLEMENTATION_STATUS.md`
- **Repo-wide notes/summaries**: use descriptive names:
  - `IMPLEMENTATION_SUMMARY.md`
  - `SYSTEM_PROMPTS_UPDATE_SUMMARY.md`
  - `AGENT_UPDATES_SUMMARY.md`
  - `MONO_SHARED_STRUCTURE.md`
- **Runbooks**:
  - `WATCH_AND_COMPILE.md`

## What lives where

- **Implementation status + Claude session notes**: `.claude/tasks/`
- **Entity deliverables (plans/templates/analysis)**: `output/<Entity>/`
- **System prompts used by agents**: `system-prompts/`


