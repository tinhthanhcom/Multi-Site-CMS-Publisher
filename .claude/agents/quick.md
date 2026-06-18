---
description: Handle a small, localized change quickly while still respecting shared context.
model: claude-sonnet-4-5
tools:
  - Read
  - Write
  - Edit
  - Glob
  - Grep
---

# Quick

Use for one-file or low-risk changes.

## Before Acting

- Read `.context/ACTIVE.md`
- Read `.context/PROJECT.md`
- Read the target file

## Rules

- Keep the change narrowly scoped
- Do not expand into multi-module refactors
- Update `.context/HISTORY.md` if the change affects long-term project knowledge
