# Skill: Atomic Plan

Produce a two-document implementation specification from a feature request: a high-level **Plan** and a detailed **Atomic Steps** document. The goal is to resolve ambiguity and surface assumptions *before* any code is written.

---

## Definition of "Atomic"

A step is atomic when it describes **one indivisible change** — something that cannot be broken down further without losing meaning. You know a step is atomic when:

- It touches exactly one location in one file (one field, one method signature, one import line, one constant value)
- A developer could execute it in under a minute without making any decisions
- Completing it does not implicitly require completing anything else first

**Not atomic:**
- "Update `useBacklog.ts`"
- "Add reorder support to the backend"
- "Write tests for the new feature"

**Atomic:**
- "In `useBacklog.ts`, add `reloadContainers: () => Promise<void>` to the `UseBacklogResult` interface after the `reload` field"
- "In `ActivityService.cs`, add `if (!dto.SkipContainerLink)` around the container-creation block, starting at line 89"
- "In `activityService.ts`, add `reorderActivity: async (...) => api.patch(...)` after the `deleteActivity` method"

---

## When to Invoke This Skill

Use this skill when:
- A feature request is complex enough that writing code immediately would produce something wrong or incomplete
- Multiple files across multiple layers (backend, frontend, tests) are involved
- There are design decisions that could go several ways, and the wrong choice would require a rewrite
- The user says "plan first" or "before we write any code"

---

## Process

### Step 1 — Read the codebase before writing anything

Before producing either document:

1. Read every file mentioned in the feature request, plus their immediate collaborators (interfaces, DTOs, callers, test files)
2. Read the enum definitions for any enum referenced in the feature — values must be verified, not assumed
3. Look for inconsistencies between layers (backend vs. frontend type alignment, API endpoint names vs. service calls, display labels vs. stored values)
4. Note the existing test patterns (base classes, cleanup strategy, collection attributes) so test steps match what is already in use

Any inconsistency or pre-existing bug found during reading must be **surfaced immediately** — either as a Phase 0 prerequisite or as a blocking question — rather than silently worked around.

### Step 2 — Write the Plan document

Place this in `docs/<feature-name>.md`.

The plan is a **human-readable narrative**. For each feature or bug fix:

- State the **problem** precisely (what is wrong or missing and why)
- State the **solution** at a design level (what approach will be taken and why that approach over alternatives)
- List the **files that will change**, grouped by backend / frontend / tests
- Describe the **tests** that must exist: what they verify, not how to write them

The plan does not contain step-by-step instructions. It answers "what and why" so that the atomic steps document can answer "how, exactly."

End the plan with an **implementation order table** showing which phases depend on which, so parallel work is identified and prerequisite ordering is clear.

### Step 3 — Write the Atomic Steps document

Place this in `docs/atomic/<feature-name>.md`.

#### Top-level structure: Phases

Phases correspond to the major features or bug fixes from the plan, in the order they should be implemented (respecting dependencies). Number them: Phase 0, Phase 1, Phase 2, etc.

**Phase 0** is always reserved for prerequisites — foundational fixes or verifications that must happen before any feature work begins (e.g., fixing an enum mismatch, verifying a baseline test passes, correcting a wrong assumption in existing documentation).

#### Within each phase: Groups

Each phase contains one or more **groups**. A group is a set of steps that all exist for the same reason. Groups should be as small as possible — typically 2–8 steps.

Each group follows this exact format:

```markdown
### Group N.M — <Short descriptive title>

**Why:** One sentence explaining the reason these steps exist. This sentence answers "why are we doing this at all?" not "what does the code do?"

- [ ] <atomic step>
- [ ] <atomic step>
- [ ] <atomic step>

**Assumptions:**
- A1. <statement taken as true while writing these steps, with the reasoning for why it seems safe>
- A2. ...

**Questions:**
- Q1. <a specific, answerable question that must be resolved before this group can be implemented correctly>
- Q2. ...
```

If a group has no assumptions, omit the **Assumptions** section. If it has no questions, omit the **Questions** section. Never write "None" as a placeholder — just omit the heading.

#### Writing atomic steps

Each checklist item must:

- Name the **exact file** (full path relative to repo root, or at minimum the filename if it is unique)
- Name the **exact location** within the file (method name, interface name, line number if known, or position relative to a named neighbor)
- Describe **one change** (add a field, change a type, add a method call, add an import, change a constant value)
- Use code snippets inline when the exact syntax matters and would otherwise be ambiguous

Steps that write new files are exempt from the location rule but must still be one file per step.

Test steps are atomic when they name **one test method** and describe **one assertion pattern** (what input, what expected output). Do not write "add CRUD tests" — write each test method as its own step.

#### Blocking questions

Some questions prevent the group from being written at all, or would force a complete rewrite of the steps if answered differently. Mark these clearly:

```markdown
> **QUESTION (blocking):** ...
```

Place a blocking question *before* the group it blocks, not inside it. Write the steps using an explicit assumption and label the assumption as provisional.

#### Closing summary

At the end of the document, add an **Outstanding Questions Summary** table:

| # | Phase | Question |
|---|---|---|
| 1 | Phase 6a (blocking) | ... |
| 2 | Phase 3 | ... |

This table is the single place a reviewer can look to understand what needs to be decided before implementation can proceed, and which decisions are truly blocking vs. merely clarifying.

---

## Rules for Assumptions vs. Questions

**Prefer questions over assumptions** for anything where:
- A wrong assumption would require rewriting steps (not just adjusting one value)
- The correct answer depends on user intent rather than what can be inferred from the code
- Two or more reasonable answers exist and each leads to meaningfully different code

**Assumptions are acceptable** when:
- The answer is clearly implied by the existing codebase patterns (e.g., "follows the same pattern as the existing X")
- A wrong assumption would only affect one isolated step and is trivially corrected
- The assumption matches the user's own words closely enough that it would be surprising if it were wrong

Always write down an assumption even when it seems obvious. The value is not that it's surprising — it's that it makes the reasoning auditable and easy to correct.

---

## Calibrating Step Granularity

Use this test: imagine handing the step list to a developer who knows the language and framework but has never seen this codebase. Can they execute each step without reading any source file and without making any design decisions? If yes, the steps are atomic enough.

Signs that a step is too coarse:
- It contains "and" joining two distinct changes in two distinct locations
- It says "update X to support Y" without specifying exactly what to add or change
- It refers to a file without naming the specific location within the file

Signs that a step is too fine:
- It breaks a single logical change (like a method signature) across multiple steps that would be meaningless if executed out of order
- It describes internal implementation details that a competent developer would naturally choose (e.g., "use a local variable to store the result before returning it")

---

## Output Checklist

Before delivering either document, verify:

- [ ] Every enum value used in the steps has been verified by reading the actual source file, not assumed
- [ ] Every file path in the steps has been verified to exist (or is explicitly marked as a new file)
- [ ] Every interface or method signature referenced in the steps matches the actual current signature
- [ ] Pre-existing bugs or inconsistencies found during reading are surfaced as Phase 0 items or blocking questions
- [ ] Each group's **Why** statement could be read in isolation and would explain why those steps exist
- [ ] All blocking questions are listed both inline and in the summary table
- [ ] The implementation order in the plan is reflected in the phase numbering of the atomic steps
