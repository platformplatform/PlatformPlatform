---
name: commit
description: Commit session changes to git. Identify files from session context, ask user pick message, run validation only if needed. Use when user want commit / save / land changes.
allowed-tools: *
---

# Commit Workflow

**Asked commit now != permission for future commits.** Each commit needs explicit user instruction. No amend, revert, push without explicit ask.

Speed critical. No slow commands unless changes warrant.

## Identify Files

- Prefer session context over re-running `git status` / `log` / `diff`.
- Clean session: all changed files. With context: only files you changed.

## Translations

Frontend changes touching user-facing strings: run `translate` to check `.po` files. Missing keys: flag user; don't commit until resolved.

## Validation (judgment)

Skip if `build` / `test` / `lint` / `format` / `aspire-restart` / `e2e` already ran clean this session on same files. No re-run, no offer.

When need:

Run `build` first:
- **Backend change:** `build` (no flags - covers backend AND frontend; backend can break frontend via API contracts)
- **Frontend-only:** `build --frontend`
- **CLI-only:** `build --cli`

After build, parallel (`--no-build` where applicable):

- `format --<target>`, `lint --<target>` - per target whose code changed
- `test --no-build` - backend change
- `aspire-restart` - service / Aspire wiring change (large only)
- `e2e` - large frontend or shared E2E-covered change; `--smoke` for low-impact (rare pre-commit)

Judge target by file (project / manifest / lock files affect owning build, even when extension non-obvious).

## Ask User

Simple, focused commit: propose one message, commit.

Complex (multiple unrelated changes, doesn't fit one line, validation choices needed, file set ambiguous): use AskUserQuestion - 2-4 message options + conditionals in one call. No back-and-forth. Follow up only on "Other" or unexpected input.

No conventional commit prefixes (`feat:`, `fix:`, `docs:`, `chore:`).

Conditional questions:
- Validation tools multi-select, only those flagged needed. Default skip.
- File selection if ambiguous.

## Stage and Commit

Stage explicit files. Never `git add -A` or `git add .`.

---

Asked commit now != permission for future commits.
