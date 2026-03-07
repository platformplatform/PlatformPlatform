---
name: retrospective
description: Conduct a retrospective to evaluate implementation quality, workflow effectiveness, and surface missed edge cases. Use at the end of a feature.
allowed-tools: Read, Write, Bash, Glob, Grep
---

# Retrospective

Conduct a retrospective at the end of a [feature] to evaluate what was built, how it was built, and what could be improved. This covers both the feature implementation quality and the agentic workflow effectiveness.

Team leads: delegate this to all agents including the team lead. Each agent writes their own retrospective, then all agents cross-review.

## STEP 1: Gather Context

1. Read the [feature] description and all [tasks] from [PRODUCT_MANAGEMENT_TOOL]
2. Read all commits on the branch related to the [feature]: `git log --oneline`
3. Review the actual implementation files (code, tests, translations)
4. Read any architect recommendations or plans saved in `.workspace/{branch-name}/`

## STEP 2: Individual Retrospective

Each agent writes their own retrospective covering all focus areas below. Save to `.workspace/{branch-name}/retrospective-{agent-name}.md`.

### Feature Focus Areas

1. **Requirements Coverage**: Were all [task] requirements implemented? What was missed or partially done?
2. **User Experience**: What could easily be added to improve UX? Are empty states, error states, loading states handled well?
3. **Security**: Are there authentication, authorization, or input validation gaps?
4. **Edge Cases**: What boundary conditions, race conditions, or unusual inputs were not handled?
5. **Code Quality**: Are there patterns that could be simplified, duplicated logic, or unclear naming?
6. **Performance**: Are there N+1 queries, unnecessary re-renders, missing indexes, or large payload concerns?
7. **Test Coverage**: Are the critical paths covered by E2E tests? What scenarios are missing?

### Workflow Focus Areas

8. **Zero-Defect Mindset**: Did the team maintain zero tolerance for warnings and failures? Were andon cords pulled when needed?
9. **Review Rigor**: Were reviews thorough? Were validation tools run consistently? Did reviewers follow the three-phase process?
10. **Communication**: Was communication clear and timely? Were there deadlocks or miscommunications? Did interrupt vs message usage make sense?
11. **Parallel Execution**: Did the parallel workflow work smoothly? Were there coordination issues between backend, frontend, and QA?
12. **Tooling**: Did build, test, format, inspect, Aspire, and browser tools work reliably? Were there unnecessary redundant cycles?

### Template

```markdown
# Retrospective: [Agent Name]

## Summary

[2-3 sentences: what you did, your overall assessment]

## Tasks Completed

| Task | Role | Outcome |
|------|------|---------|
| [task id/name] | [what you did] | [result + any issues] |

## Findings

### Finding 1: [Short title]

- **Area:** [Area 1-12 from focus areas above]
- **Priority:** [Critical / High / Medium / Low]
- **What happened:** [Factual description]
- **Why it matters:** [Impact on quality, speed, or reliability]
- **Suggested fix:** [Concrete proposal]

## Top 3 Changes I Would Make

1. [Most impactful]
2. [Second]
3. [Third]
```

## STEP 3: Cross-Review

After all agents complete Step 2, each agent reads ALL retrospective files and writes a suggested-changes file. Save to `.workspace/{branch-name}/suggested-changes-{agent-name}.md`.

Look for:
- Patterns that multiple agents identified (high-confidence issues)
- Contradictions between agents (need resolution)
- Ideas from other agents you strongly agree or disagree with
- Blind spots in your own retrospective

### Template

```markdown
# Suggested Changes: [Agent Name]

## Cross-Review Observations

[Patterns noticed, where agents agreed or disagreed]

## Suggested Changes

### Change 1: [Short title]

- **Area:** [Which focus area]
- **Priority:** [Critical / High / Medium / Low]
- **Current behavior:** [What happens now]
- **Proposed behavior:** [What should happen]
- **Supporting evidence:** [Which retrospectives support this]

## Disagreements

[Changes from other agents you disagree with and why]

## Top 5 Changes (Ranked)

1. [Most impactful]
2-5. [...]
```

## STEP 4: Executive Summary

The team lead aggregates all cross-review files into a single executive summary at `.workspace/{branch-name}/executive-summary.md`. This document ranks findings by agent agreement count and produces the final prioritized list of recommended changes.
