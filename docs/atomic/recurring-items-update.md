# Atomic Implementation Steps

Steps are grouped by the reason to do them. Each group has:
- **Why** — one sentence explaining the reason
- A markdown checklist where each item is the smallest possible indivisible change
- **Assumptions** — things taken as true while writing these steps
- **Questions** — things that block writing the steps or where a wrong assumption would cause a rewrite

---

## Phase 0 — Foundation: Fix RecurrenceType Enum Mismatch

> This is a pre-existing bug that must be fixed **before** any recurring item feature can work correctly. The backend and frontend have completely different integer values for the same enum names.
>
> Backend (`RecurrenceType.cs`): `Annual=0, Monthly=1, Weekly=2, Daily=3, None=99`
> Frontend (`types/activity.ts`): `None=0, Daily=1, Weekly=2, Monthly=3, Annual=4`
>
> The DB stores enum values as strings (via `.HasConversion<string>()`), so no data migration is needed. Only the frontend constants need to be updated to match the backend.

### Group 0.1 — Update frontend RecurrenceType constants

**Why:** The frontend int values don't match the backend enum, so any recurring-related API call sends and receives the wrong numeric codes.

- [ ] In `src/frontend/src/types/activity.ts`, change the `RecurrenceType` type union from `0 | 1 | 2 | 3 | 4` to `0 | 1 | 2 | 3 | 99`
- [ ] In `src/frontend/src/types/activity.ts`, change `RecurrenceType.None` value from `0` to `99`
- [ ] In `src/frontend/src/types/activity.ts`, change `RecurrenceType.Daily` value from `1` to `3`
- [ ] In `src/frontend/src/types/activity.ts`, change `RecurrenceType.Weekly` value from `2` to `2` *(no change — already correct)*
- [ ] In `src/frontend/src/types/activity.ts`, change `RecurrenceType.Monthly` value from `3` to `1`
- [ ] In `src/frontend/src/types/activity.ts`, change `RecurrenceType.Annual` value from `4` to `0`

**Assumptions:**
- A1. Fixing the frontend is the right side to change (backend enum ordering is the authoritative source).
- A2. Since the DB persists enum names as strings, no migration is required and no existing DB records will be corrupted.
- A3. All frontend code references enum members by name (e.g., `RecurrenceType.Monthly`), not by raw number, so changing the constant values fixes all usages automatically.

**Questions:**
- Q1. Are there any raw numeric recurrence type values hardcoded in the frontend (e.g., in tests or Playwright specs) that would also need updating?

---

## Phase 1 — Bug Fix: Daily Checklist Shows No Items

### Group 1.1 — Pass the correct defaultContainerType when creating items

**Why:** Without `defaultContainerType`, the backend falls back to Annual, so Daily items are silently created in the Annual backlog instead.

- [ ] In `src/frontend/src/pages/DailyChecklist.tsx`, in the `handleSave` function, locate the `handleCreate` call (the `else` branch, currently line ~43)
- [ ] Add `defaultContainerType: ContainerType.Daily` to the object passed to `handleCreate`, alongside the existing `containerId: selectedContainerId`

**Assumptions:**
- A1. `ContainerType` is already imported in `DailyChecklist.tsx` — confirmed by reading the file.
- A2. The auto-propagation behavior (Daily → Weekly → Monthly → Annual) is correct and desired; items created in Daily will also appear in the other backlogs.

**Questions:**
- None for this group.

### Group 1.2 — Integration test: Daily item creation uses correct container type

**Why:** Verify the fix at the API layer so it cannot regress silently.

- [ ] In `src/backend/LifeSprint.Tests/Integration/ActivityServiceIntegrationTests.cs`, add a new test method `CreateActivity_WithDefaultContainerTypeDaily_CreatesContainerActivityWithTypeDaily`
- [ ] In the test, call `CreateActivityAsync` with a `CreateActivityDto` where `DefaultContainerType = ContainerType.Daily` and `ContainerId = null`
- [ ] Assert that the returned `ActivityResponseDto.Containers` contains exactly one entry with `ContainerType == ContainerType.Daily`
- [ ] Assert that a `Container` of type `Daily` was created in the database for the test user

**Assumptions:**
- A1. The integration test infrastructure (`IntegrationTestBase`, `CleanupTestDataAsync`) already handles Daily container cleanup — confirmed: the raw SQL deletes all `ContainerActivities` and `ActivityTemplates` for the test user regardless of type.

**Questions:**
- None for this group.

### Group 1.3 — E2E test: Daily Checklist CRUD

**Why:** Confirm the full user journey works end-to-end in the browser.

- [ ] Create the file `src/frontend/e2e/daily-checklist.spec.ts`
- [ ] Add a `beforeEach` block that authenticates via `POST /api/auth/test-login` with username `e2e-daily-user`
- [ ] Add test: navigate to `/daily`, verify the "Daily Checklist" heading is visible
- [ ] Add test: click "New Item", fill in title "Buy groceries", submit, verify "Buy groceries" appears in the list without a page reload
- [ ] Add test: click the checkbox on "Buy groceries", verify the item gains a completed visual state (rely on the CSS class `completed` or the checkbox being checked)
- [ ] Add test: click "Edit" on "Buy groceries", change the title to "Buy groceries and milk", save, verify the updated title appears without a page reload
- [ ] Add test: click "Delete" on the item, confirm the browser confirm dialog, verify the item disappears from the list

**Assumptions:**
- A1. E2E tests do not clean up after themselves — consistent with the existing `auth.spec.ts` pattern.
- A2. The `test-login` endpoint creates a fresh session each time it is called.
- A3. Each test runs against a fresh browser context (Playwright default), so data from one test does not bleed into another within the same file.

**Questions:**
- Q1. Should E2E test users be unique per test file (e.g., `e2e-daily-user`) to avoid cross-file interference, or should a shared test user be used with explicit cleanup?

---

## Phase 2 — Bug Fix: New Container Creates Without Page Reload

### Group 2.1 — Expose `reloadContainers` from the `useBacklog` hook

**Why:** The hook holds the containers list internally but currently gives pages no way to refresh it after a new container is created.

- [ ] In `src/frontend/src/hooks/useBacklog.ts`, add `reloadContainers: () => Promise<void>` to the `UseBacklogResult` interface (after the `reload` field, line ~19)
- [ ] In `src/frontend/src/hooks/useBacklog.ts`, add `reloadContainers: loadContainers` to the return object at the bottom of the hook (after `reload`)

**Assumptions:**
- A1. `loadContainers` is already a stable `useCallback`, so exposing it as `reloadContainers` is safe and won't cause re-render loops.

**Questions:**
- None for this group.

### Group 2.2 — Update `NewContainerModal` to pass the created container to the callback

**Why:** The `onCreated` callback currently fires with no arguments, so the calling page has no reference to the newly created container and cannot auto-navigate to it.

- [ ] In `src/frontend/src/components/Activities/NewContainerModal.tsx`, add `type Container` to the import line from `'../../types'` (currently imports only `type ContainerType`)
- [ ] In `src/frontend/src/components/Activities/NewContainerModal.tsx`, in the `NewContainerModalProps` interface, change `onCreated: () => void` to `onCreated: (container: Container) => void`
- [ ] In `src/frontend/src/components/Activities/NewContainerModal.tsx`, in `handleCreate`, change `await containerService.createNewContainer(...)` so that the returned value is captured: `const created = await containerService.createNewContainer(...)`
- [ ] In `src/frontend/src/components/Activities/NewContainerModal.tsx`, change the `onCreated()` call to `onCreated(created)`

**Assumptions:**
- A1. `containerService.createNewContainer` already returns a `Container` object — confirmed by reading `containerService.ts`.

**Questions:**
- None for this group.

### Group 2.3 — Update `AnnualBacklog` to use `reloadContainers` and auto-navigate

**Why:** After creating a new container, the Annual page must refresh its containers list so the `DateNavigator` shows the new entry, and then navigate to it.

- [ ] In `src/frontend/src/pages/AnnualBacklog.tsx`, add `reloadContainers` to the destructure from `useBacklog(ContainerType.Annual)`
- [ ] In `src/frontend/src/pages/AnnualBacklog.tsx`, change the `handleContainerCreated` function signature from `async () =>` to `async (newContainer: Container) =>`
- [ ] In `src/frontend/src/pages/AnnualBacklog.tsx`, add `await reloadContainers()` as the first awaited statement inside `handleContainerCreated` (before the existing `await reload()`)
- [ ] In `src/frontend/src/pages/AnnualBacklog.tsx`, add `setSelectedContainerId(newContainer.id)` as a statement after `await reload()` inside `handleContainerCreated`

**Assumptions:**
- A1. `Container` is already imported in `AnnualBacklog.tsx` — confirmed by reading the file.

**Questions:**
- None for this group.

### Group 2.4 — Update `MonthlyBacklog` to use `reloadContainers` and auto-navigate

**Why:** Same reason as Group 2.3, for the Monthly page.

- [ ] In `src/frontend/src/pages/MonthlyBacklog.tsx`, add `reloadContainers` to the destructure from `useBacklog(ContainerType.Monthly)`
- [ ] In `src/frontend/src/pages/MonthlyBacklog.tsx`, change `handleContainerCreated` signature from `async () =>` to `async (newContainer: Container) =>`
- [ ] In `src/frontend/src/pages/MonthlyBacklog.tsx`, add `await reloadContainers()` as the first awaited statement in `handleContainerCreated`
- [ ] In `src/frontend/src/pages/MonthlyBacklog.tsx`, add `setSelectedContainerId(newContainer.id)` after `await reload()` in `handleContainerCreated`

**Assumptions:**
- A1. `MonthlyBacklog.tsx` has the same structure as `AnnualBacklog.tsx` and `WeeklySprint.tsx` — assumed based on the plan's description of identical page structure.

**Questions:**
- Q1. Please confirm whether `MonthlyBacklog.tsx` already imports `Container` from `'../types'` or if it needs to be added.

### Group 2.5 — Update `WeeklySprint` to use `reloadContainers` and auto-navigate

**Why:** Same reason as Group 2.3, for the Weekly page.

- [ ] In `src/frontend/src/pages/WeeklySprint.tsx`, add `reloadContainers` to the destructure from `useBacklog(ContainerType.Weekly)`
- [ ] In `src/frontend/src/pages/WeeklySprint.tsx`, change `handleContainerCreated` signature from `async () =>` to `async (newContainer: Container) =>`
- [ ] In `src/frontend/src/pages/WeeklySprint.tsx`, add `await reloadContainers()` as the first awaited statement in `handleContainerCreated`
- [ ] In `src/frontend/src/pages/WeeklySprint.tsx`, add `setSelectedContainerId(newContainer.id)` after `await reload()` in `handleContainerCreated`

**Assumptions:**
- A1. `Container` is already imported in `WeeklySprint.tsx` — confirmed by reading the file.

**Questions:**
- None for this group.

### Group 2.6 — Integration test: `POST /api/containers/new` returns the full container object

**Why:** Verify the backend response contains all fields the frontend needs to auto-navigate.

- [ ] In `src/backend/LifeSprint.Tests/Integration/ActivityServiceIntegrationTests.cs` (or a new `ContainerServiceIntegrationTests.cs`), add a test `CreateNewContainer_ReturnsContainerDtoWithCorrectFields`
- [ ] In the test, call `ContainerService.CreateNewContainerAsync` with `type = ContainerType.Monthly` and `rolloverIncomplete = false`
- [ ] Assert the returned `ContainerResponseDto` is not null
- [ ] Assert `dto.Id > 0`
- [ ] Assert `dto.Type == ContainerType.Monthly`
- [ ] Assert `dto.Status == ContainerStatus.Active`
- [ ] Assert `dto.StartDate` equals the first day of the current month (UTC)
- [ ] Add a second test `CreateNewContainer_WhenAlreadyExists_ReturnsNull` that creates the container once, then calls `CreateNewContainerAsync` again for the same type, and asserts the return value is `null`

**Assumptions:**
- A1. The test uses `IntegrationTestBase` and is decorated with `[Collection("IntegrationTests")]`.

**Questions:**
- None for this group.

### Group 2.7 — E2E test: New container appears without page reload

**Why:** Confirm the complete user journey from modal → container creation → DateNavigator update in the browser.

- [ ] Create the file `src/frontend/e2e/containers.spec.ts`
- [ ] Add a `beforeEach` block authenticating via `POST /api/auth/test-login` with username `e2e-container-user`
- [ ] Add test: navigate to `/monthly`, click "New Month", verify the modal appears with "Start New Month" heading
- [ ] Add test (continuing from modal): click "Create Month", verify the modal closes, verify the `DateNavigator` shows the current month label (e.g., "April 2026") — all without triggering `page.reload()`
- [ ] Add test: verify that after creation, the activity list is visible (empty or with items) and no full-page reload occurred (use Playwright's navigation event listener to confirm no reload)

**Assumptions:**
- A1. A `409 Conflict` may occur if the test user already has a container for the current period from a previous test run. The test must handle this gracefully (either skip or use a unique user per run).

**Questions:**
- Q1. How should E2E tests handle the 409 conflict when a container for the current period already exists? Options: (a) always use a fresh test user per run via a UUID suffix, (b) check for 409 and treat it as a pass (container already exists), (c) delete the container before the test runs.

---

## Phase 3 — Integration & E2E CRUD Tests Per Section

### Group 3.1 — Backend integration test: CRUD for each container type

**Why:** The existing integration tests cover Annual and the general case but not all four container types explicitly, leaving gaps in coverage.

- [ ] In `src/backend/LifeSprint.Tests/Integration/ActivityServiceIntegrationTests.cs`, add test `CreateActivity_InAnnualContainer_CreatesContainerActivity` (if not already present)
- [ ] In the same file, add test `CreateActivity_InMonthlyContainer_CreatesContainerActivity` that uses `DefaultContainerType = ContainerType.Monthly`
- [ ] In the same file, add test `CreateActivity_InWeeklyContainer_CreatesContainerActivity` that uses `DefaultContainerType = ContainerType.Weekly`
- [ ] In the same file, add test `CreateActivity_InDailyContainer_CreatesContainerActivity` that uses `DefaultContainerType = ContainerType.Daily`
- [ ] For each of the four tests above, assert that `ContainerActivity.Container.Type` matches the expected container type
- [ ] Add test `UpdateActivity_ChangesTitle_PersistsToDatabase` that creates an activity then calls `UpdateActivityAsync` with a new title and asserts the DB value changed
- [ ] Add test `ArchiveActivity_SetsArchivedAt_AndHidesFromGetActivities` that creates an activity, archives it, then calls `GetActivitiesForUserAsync` and asserts the archived item is absent
- [ ] Add test `ToggleCompletion_SetsCompletedAtOnAllContainerActivities` that creates an activity linked to multiple container types, toggles it complete, and asserts every `ContainerActivity` record has a non-null `CompletedAt`
- [ ] Add test `GetActivities_FilteredByContainerId_ReturnsOnlyItemsInThatContainer` that creates two activities in different containers and asserts `containerId` filter returns only the correct one

**Assumptions:**
- A1. All new tests inherit from `IntegrationTestBase` and are decorated with `[Trait("Category", "Integration")]` and `[Collection("IntegrationTests")]`.

**Questions:**
- None for this group.

### Group 3.2 — E2E test: Annual Backlog CRUD

**Why:** Provide browser-level coverage for the Annual Backlog create, read, update, delete, and toggle workflows.

- [ ] Create `src/frontend/e2e/annual-backlog.spec.ts`
- [ ] Add `beforeEach` authenticating via `test-login` with username `e2e-annual-user`
- [ ] Add test: navigate to `/annual`, verify "Annual Backlog" heading is visible
- [ ] Add test: create item "Annual Goal" via "New Item", verify it appears in the list
- [ ] Add test: click "Edit" on "Annual Goal", change title to "Annual Goal Updated", save, verify new title appears
- [ ] Add test: toggle completion checkbox on the item, verify the item shows a completed visual state
- [ ] Add test: toggle completion checkbox again, verify the item returns to incomplete state
- [ ] Add test: click "Delete" on the item, confirm the dialog, verify the item is gone from the list
- [ ] Add test: click "New Year", verify the modal appears, click "Create Year", verify the `DateNavigator` shows the current year without a page reload

**Assumptions:**
- A1. Same E2E test isolation assumptions as Phase 1, Group 1.3.

**Questions:**
- None for this group.

### Group 3.3 — E2E test: Monthly Backlog CRUD

**Why:** Same coverage as Group 3.2 for the Monthly page.

- [ ] Create `src/frontend/e2e/monthly-backlog.spec.ts`
- [ ] Add `beforeEach` authenticating with username `e2e-monthly-user`
- [ ] Add tests mirroring all tests in Group 3.2, substituting `/monthly`, "Monthly Backlog", "New Item", "New Month", and "Month" labels appropriately

**Questions:**
- Q1. Same as Phase 2 Group 2.7 Q1 — how to handle pre-existing containers for the current period.

### Group 3.4 — E2E test: Weekly Sprint CRUD

**Why:** Same coverage for the Weekly page.

- [ ] Create `src/frontend/e2e/weekly-sprint.spec.ts`
- [ ] Add `beforeEach` authenticating with username `e2e-weekly-user`
- [ ] Add tests mirroring Group 3.2, substituting `/weekly`, "Weekly Sprint", "New Sprint", and "Sprint" labels

**Questions:**
- None beyond Q1 from Group 3.3.

---

## Phase 4 — UI: Up / Down Arrow Reordering

### Group 4.1 — Backend DTO for reorder request

**Why:** The controller action needs a typed request body; a DTO keeps it clean and validated.

- [ ] Create `src/backend/LifeSprint.Core/DTOs/ReorderActivityDto.cs`
- [ ] In `ReorderActivityDto.cs`, add `public int ContainerId { get; set; }`
- [ ] In `ReorderActivityDto.cs`, add `public required string Direction { get; set; }` (valid values: `"up"` or `"down"`)

**Assumptions:**
- A1. `"up"` means the item moves toward the top of the list (lower `Order` value, i.e., swaps with the neighbor whose `Order` is the next lower value in the sorted list).
- A2. `"down"` means the item moves toward the bottom (higher `Order` value).
- A3. If the item is already at the top and `"up"` is requested, the service returns `false` (no-op).
- A4. The `Direction` field is a string rather than an enum to keep the DTO simple; validation happens in the service.

**Questions:**
- Q1. Should the endpoint be `PATCH /api/activities/{id}/reorder` (containerId in body) or `PATCH /api/activities/{id}/containers/{containerId}/reorder` (containerId in URL)? The plan lists both options. The body approach is simpler but less RESTful.

### Group 4.2 — Backend interface: add `ReorderActivityAsync`

**Why:** The interface must be updated before the service implementation and controller can reference it.

- [ ] In `src/backend/LifeSprint.Core/Interfaces/IActivityService.cs`, add the method signature:
  `Task<bool> ReorderActivityAsync(string userId, int activityId, int containerId, string direction);`

**Assumptions:**
- A1. Returns `true` on success, `false` when the activity/container association is not found or the item is already at the boundary.

**Questions:**
- None for this group.

### Group 4.3 — Backend service: implement `ReorderActivityAsync`

**Why:** The swap logic must find the adjacent item in the sorted order and exchange their `Order` values atomically.

- [ ] In `src/backend/LifeSprint.Infrastructure/Services/ActivityService.cs`, add method `public async Task<bool> ReorderActivityAsync(string userId, int activityId, int containerId, string direction)`
- [ ] Inside the method, query for the `ContainerActivity` where `ActivityTemplateId == activityId && ContainerId == containerId` and include the `Container` navigation property for auth check
- [ ] If the record is not found or `Container.UserId != userId`, return `false`
- [ ] Query all `ContainerActivity` records for the same `containerId`, ordered by `Order` ascending, into a list
- [ ] Find the index of the current item in that list
- [ ] If `direction == "up"` and `index == 0`, return `false` (already at top)
- [ ] If `direction == "down"` and `index == list.Count - 1`, return `false` (already at bottom)
- [ ] Determine the neighbor: index - 1 for `"up"`, index + 1 for `"down"`
- [ ] Swap the `Order` values of the current item and the neighbor item
- [ ] Call `await _context.SaveChangesAsync()`
- [ ] Return `true`

**Assumptions:**
- A1. The swap is done on in-memory objects already tracked by EF Core, so `SaveChangesAsync` will detect the changes automatically.

**Questions:**
- Q1. Should an invalid `direction` value (not `"up"` or `"down"`) throw an exception or return `false`?

### Group 4.4 — Backend controller: add `PATCH /{id}/reorder` action

**Why:** Expose the reorder service method via the REST API.

- [ ] In `src/backend/LifeSprint.Api/Controllers/ActivitiesController.cs`, add action `[HttpPatch("{id}/reorder")]`
- [ ] The action signature: `public async Task<IActionResult> ReorderActivity(int id, [FromBody] ReorderActivityDto dto)`
- [ ] Add `using LifeSprint.Core.DTOs;` if not already present (for `ReorderActivityDto`)
- [ ] Inside the action, call `GetCurrentUserId()` and assign to `userId`
- [ ] Call `await _activityService.ReorderActivityAsync(userId, id, dto.ContainerId, dto.Direction)`
- [ ] If result is `true`, return `NoContent()`
- [ ] If result is `false`, return `NotFound(new { message = "Activity or container not found, or item is already at the boundary" })`
- [ ] Wrap in a try/catch returning `StatusCode(500, ...)` for unexpected exceptions

**Assumptions:**
- A1. Following the existing controller pattern (see `RemoveFromContainer` action).

**Questions:**
- None for this group.

### Group 4.5 — Backend unit tests for `ReorderActivityAsync`

**Why:** The swap logic has edge cases (boundary conditions) that are best verified at the unit level.

- [ ] In `src/backend/LifeSprint.Tests/Unit/ActivityServiceTests.cs`, add test `ReorderActivity_MoveDown_SwapsOrderWithNextItem`
- [ ] Set up two `ContainerActivity` records in the same container with `Order = 1` and `Order = 2`
- [ ] Call `ReorderActivityAsync` with `direction = "down"` on the item with `Order = 1`
- [ ] Assert the first item now has `Order = 2` and the second has `Order = 1`
- [ ] Add test `ReorderActivity_MoveUp_SwapsOrderWithPreviousItem` — reverse of above
- [ ] Add test `ReorderActivity_MoveUpWhenFirst_ReturnsFalse` — item with the lowest order, direction "up", asserts return is `false` and no `Order` values changed
- [ ] Add test `ReorderActivity_MoveDownWhenLast_ReturnsFalse` — item with the highest order, direction "down", asserts return is `false`
- [ ] Add test `ReorderActivity_WrongUser_ReturnsFalse` — container belongs to a different user, asserts return is `false`

**Assumptions:**
- A1. Unit tests use the in-memory EF Core database (existing test pattern).

**Questions:**
- None for this group.

### Group 4.6 — Frontend service: add `reorderActivity`

**Why:** The frontend needs a service method to call the new backend endpoint.

- [ ] In `src/frontend/src/services/activityService.ts`, add method `reorderActivity: async (activityId: number, containerId: number, direction: 'up' | 'down'): Promise<void> => { ... }`
- [ ] Inside the method body, call `api.patch<void>(\`/activities/${activityId}/reorder\`, { containerId, direction })`

**Assumptions:**
- A1. `api.patch` exists and works the same way as `api.post` — confirmed by reading `api.ts` pattern in `activityService.ts`.

**Questions:**
- None for this group.

### Group 4.7 — Frontend: add `onReorder` prop and arrow buttons to `ActivityList`

**Why:** The list component must render the up/down controls and call the handler when clicked.

- [ ] In `src/frontend/src/components/Activities/ActivityList.tsx`, add `onReorder?: (activityId: number, containerId: number, direction: 'up' | 'down') => void` to `ActivityListProps`
- [ ] In `ActivityList.tsx`, inside the `sortedActivities.map()`, add a `handleReorderClick` local function that retrieves the container association for the current `containerType`, then calls `onReorder?.(activity.id, container.containerId, direction)`
- [ ] In `ActivityList.tsx`, in the `activity-actions` div, add an `▲` button before the existing buttons, with `onClick` calling `handleReorderClick('up')`, disabled when the current item is the first in `sortedActivities`
- [ ] In `ActivityList.tsx`, in the `activity-actions` div, add a `▼` button after the `▲` button, with `onClick` calling `handleReorderClick('down')`, disabled when the current item is the last in `sortedActivities`
- [ ] In `ActivityList.tsx`, add `e.stopPropagation()` to both button `onClick` handlers to prevent triggering the row's `onClick`
- [ ] In `src/frontend/src/components/Activities/ActivityList.css`, add styles for `.reorder-up-button` and `.reorder-down-button` (small, minimal styling consistent with other action buttons)

**Assumptions:**
- A1. Both arrow buttons are hidden (not just disabled) when `onReorder` is undefined, consistent with how `onMoveActivity` buttons are handled.

**Questions:**
- Q1. Should the arrow buttons be hidden or disabled when the item is at the boundary? The plan says "hidden" for first/last. Should they be hidden (`display: none`) or rendered but disabled with reduced opacity?

### Group 4.8 — Frontend: add `handleReorder` to `AnnualBacklog`

**Why:** The page must wire up the `onReorder` callback so the list component can trigger reordering.

- [ ] In `src/frontend/src/pages/AnnualBacklog.tsx`, add an `import` for `activityService` if not already present
- [ ] In `AnnualBacklog.tsx`, add a `handleReorder` async function: `async (activityId: number, containerId: number, direction: 'up' | 'down') => { await activityService.reorderActivity(activityId, containerId, direction); await reload(); }`
- [ ] In `AnnualBacklog.tsx`, pass `onReorder={handleReorder}` to the `<ActivityList>` component

**Assumptions:**
- A1. Calling `reload()` (which reloads activities) is sufficient to re-render the list in the new order, since the list sorts by `Order` from the response data.

**Questions:**
- None for this group.

### Group 4.9 — Frontend: add `handleReorder` to `MonthlyBacklog`

**Why:** Same as Group 4.8 for the Monthly page.

- [ ] Apply the same three steps from Group 4.8 to `src/frontend/src/pages/MonthlyBacklog.tsx`

### Group 4.10 — Frontend: add `handleReorder` to `WeeklySprint`

**Why:** Same as Group 4.8 for the Weekly page.

- [ ] Apply the same three steps from Group 4.8 to `src/frontend/src/pages/WeeklySprint.tsx`

### Group 4.11 — Frontend: add `handleReorder` to `DailyChecklist`

**Why:** Same as Group 4.8 for the Daily page.

- [ ] Apply the same three steps from Group 4.8 to `src/frontend/src/pages/DailyChecklist.tsx`

### Group 4.12 — E2E test: reordering changes item position

**Why:** Verify that clicking the arrows produces the correct visual order change.

- [ ] In `src/frontend/e2e/annual-backlog.spec.ts`, add test `reorder_movesItemDownOnePosition`
- [ ] In the test, create two items in sequence: "First Item" then "Second Item"
- [ ] Verify "First Item" appears before "Second Item" in the DOM (using Playwright's `locator` ordering)
- [ ] Click the `▼` button on "First Item"
- [ ] Verify "Second Item" now appears before "First Item" in the DOM

**Assumptions:**
- A1. The two items have different `Order` values after creation (based on the `GetNextOrderInContainerAsync` logic that increments by 1).

**Questions:**
- None for this group.

---

## Phase 5 — UI: Add Child Button on Project / Epic / Story

### Group 5.1 — Create `AddChildModal` component

**Why:** A dedicated modal for child item creation gives the user a pre-configured form with the type and parent locked, reducing friction.

- [ ] Create `src/frontend/src/components/Activities/AddChildModal.tsx`
- [ ] Create `src/frontend/src/components/Activities/AddChildModal.module.css`
- [ ] In `AddChildModal.tsx`, define the interface:
  ```typescript
  interface AddChildModalProps {
    parent: Activity;
    onSave: (data: CreateActivityDto) => Promise<void>;
    onClose: () => void;
  }
  ```
- [ ] In `AddChildModal.tsx`, define `childTypeFor`: a constant mapping `Project → Epic`, `Epic → Story`, `Story → Task` using the `ActivityType` const object
- [ ] In `AddChildModal.tsx`, define `childTypeLabelFor`: a mapping from `ActivityType` value to its display name string
- [ ] In `AddChildModal.tsx`, add local state: `title` (string, empty), `description` (string, empty), `submitting` (boolean, false)
- [ ] In `AddChildModal.tsx`, add a `useEffect` that listens for the `Escape` key and calls `onClose`
- [ ] In `AddChildModal.tsx`, render an overlay `div` with a centered modal `div` (use CSS module for styling)
- [ ] Inside the modal, render a close (`×`) button that calls `onClose`
- [ ] Inside the modal, render a heading: `Add {childTypeLabelFor[childTypeFor[parent.type]]} to {parent.title}`
- [ ] Inside the modal, render a read-only display of "Type: {childTypeLabelFor[childTypeFor[parent.type]]}"
- [ ] Inside the modal, render a read-only display of "Parent: {parent.title}"
- [ ] Inside the modal, render a required text input for `title`
- [ ] Inside the modal, render an optional textarea for `description`
- [ ] Inside the modal, render Cancel and Save buttons; the Save button calls `onSave` with `{ title, description, type: childTypeFor[parent.type], parentActivityId: parent.id }`, then calls `onClose`
- [ ] In `AddChildModal.module.css`, add styles for `.overlay`, `.modal`, `.closeButton`, `.title`, `.field`, `.actions` — use the existing `NewContainerModal.module.css` as a visual reference for consistent look

**Assumptions:**
- A1. The `AddChildModal` does not include `isRecurring` or `recurrenceType` fields. A child item created via this button will default to non-recurring.
- A2. The `containerId` and `defaultContainerType` are NOT passed through `AddChildModal` directly; instead the calling page's `handleCreate` function injects the correct container context (see Group 5.3).

**Questions:**
- Q1. Should the child item be placed in the same container as the parent, or in the currently active container on the page? For example, if a user is viewing the Weekly Sprint and clicks "Add" on a Project, should the new Epic go into the Weekly container, or into whatever container the parent Project is in?
- Q2. Should `AddChildModal` include `isRecurring` and `recurrenceType` fields, or always create non-recurring children?

### Group 5.2 — Add `onAddChild` prop and "Add" button to `ActivityList`

**Why:** The list component must render the "Add" button for eligible item types and call the parent's handler.

- [ ] In `src/frontend/src/components/Activities/ActivityList.tsx`, add `onAddChild?: (parent: Activity) => void` to `ActivityListProps`
- [ ] In `ActivityList.tsx`, in the `activity-actions` div, add a conditional "Add" button that renders only when `onAddChild` is defined AND `activity.type !== ActivityType.Task`
- [ ] The "Add" button's `onClick` calls `e.stopPropagation()` then `onAddChild?.(activity)`
- [ ] In `ActivityList.css`, add a style for `.add-child-button` consistent with the existing `.edit-button` style

**Assumptions:**
- A1. Tasks cannot have children based on the hierarchy rules, so no "Add" button is shown on Task items.

**Questions:**
- None for this group.

### Group 5.3 — Add `AddChildModal` to `AnnualBacklog`

**Why:** The Annual page must manage the add-child flow: open the modal, pass the correct container context, and close on save.

- [ ] In `src/frontend/src/pages/AnnualBacklog.tsx`, add `import { AddChildModal } from '../components/Activities/AddChildModal'`
- [ ] In `AnnualBacklog.tsx`, add state: `const [addChildParent, setAddChildParent] = useState<Activity | null>(null)`
- [ ] In `AnnualBacklog.tsx`, add handler: `const handleAddChild = (parent: Activity) => setAddChildParent(parent)`
- [ ] In `AnnualBacklog.tsx`, pass `onAddChild={handleAddChild}` to the `<ActivityList>` component
- [ ] In `AnnualBacklog.tsx`, add a conditional render below the `<ActivityList>` block:
  ```tsx
  {addChildParent && (
    <AddChildModal
      parent={addChildParent}
      onSave={async (data) => {
        await handleCreate({ ...data, containerId: selectedContainerId });
        setAddChildParent(null);
      }}
      onClose={() => setAddChildParent(null)}
    />
  )}
  ```

**Assumptions:**
- A1. The child item is created in the currently selected container (`selectedContainerId`), falling back to Annual if none is selected. This mirrors the behavior of the existing "New Item" flow on the Annual page.

**Questions:**
- Q1. Same as Group 5.1 Q1 — which container should the child be placed in?

### Group 5.4 — Add `AddChildModal` to `MonthlyBacklog`

**Why:** Same as Group 5.3 for the Monthly page.

- [ ] Apply the same five steps from Group 5.3 to `src/frontend/src/pages/MonthlyBacklog.tsx`, substituting `defaultContainerType: ContainerType.Monthly` in the `handleCreate` call

### Group 5.5 — Add `AddChildModal` to `WeeklySprint`

**Why:** Same as Group 5.3 for the Weekly page.

- [ ] Apply the same five steps from Group 5.3 to `src/frontend/src/pages/WeeklySprint.tsx`, substituting `defaultContainerType: ContainerType.Weekly`

### Group 5.6 — Add `AddChildModal` to `DailyChecklist`

**Why:** Same as Group 5.3 for the Daily page.

- [ ] Apply the same five steps from Group 5.3 to `src/frontend/src/pages/DailyChecklist.tsx`, substituting `defaultContainerType: ContainerType.Daily`

### Group 5.7 — Unit test for `AddChildModal`

**Why:** Verify the modal locks the correct type and parent fields and calls `onSave` with the right data.

- [ ] Create `src/frontend/src/components/Activities/__tests__/AddChildModal.test.tsx`
- [ ] Add test: render `<AddChildModal>` with a `parent` of type `Project`, assert the child type displayed is "Epic"
- [ ] Add test: render with parent type `Epic`, assert child type displayed is "Story"
- [ ] Add test: render with parent type `Story`, assert child type displayed is "Task"
- [ ] Add test: fill in a title, click Save, assert `onSave` was called with `type === ActivityType.Epic` and `parentActivityId === parent.id`
- [ ] Add test: pressing Escape calls `onClose`

**Questions:**
- None for this group.

### Group 5.8 — E2E test: Add child via button

**Why:** Confirm the full browser flow: click "Add" on a Project, fill in the form, verify the Epic appears.

- [ ] In `src/frontend/e2e/annual-backlog.spec.ts`, add test `addChild_createsEpicUnderProject`
- [ ] Create a Project item "Big Initiative"
- [ ] Click the "Add" button on "Big Initiative"
- [ ] Verify the `AddChildModal` appears with "Add Epic to Big Initiative" in the heading
- [ ] Fill in title "Launch Campaign"
- [ ] Click Save
- [ ] Verify "Launch Campaign" appears in the list with type badge "Epic" and parent label "Big Initiative"

**Questions:**
- None for this group.

---

## Phase 6a — Recurring: Backend Template Storage Model

> Before writing any recurring feature steps, this design question must be answered:

**QUESTION (blocking):** When a user creates a recurring item from the Recurring Items section, should the recurring template `ActivityTemplate` record:
- **(A) Have no `ContainerActivity` links** — it exists purely as a template, and only its concrete stamped copies appear in the backlogs. The recurring section lists templates directly by filtering `IsRecurring = true`.
- **(B) Have a `ContainerActivity` link to the current period** — the template itself appears in the backlog like any other item, but is also shown in the recurring section.

**The steps below assume Option A**, because the user described recurring templates as "a Class" — templates that generate instances but don't themselves appear in the backlog. If Option B is intended, the steps in Groups 6a.1–6a.3 change significantly.

**QUESTION (blocking):** When a recurring template is first created, should a concrete instance for the *current* period be immediately created and placed in the current backlog? The user wrote "when we create a class, we add an implementation to the Backlog in as well." This implies yes — creating a monthly recurring template should also produce a "Template Name | April 2026" item in the current Monthly backlog. Please confirm.

**QUESTION (blocking):** If immediate instantiation is desired and no container yet exists for the current period, should the backend implicitly create one (via `GetOrCreateCurrentContainerAsync`)? This could surprise the user if they're just defining templates before starting the month.

### Group 6a.1 — Add `SkipContainerLink` field to `CreateActivityDto`

**Why:** The Recurring Items pages need to create templates without any container association; the existing `CreateActivityDto` always creates one.

- [ ] In `src/backend/LifeSprint.Core/DTOs/CreateActivityDto.cs`, add `public bool SkipContainerLink { get; set; } = false;`
- [ ] In `src/frontend/src/types/activity.ts`, add `skipContainerLink?: boolean` to the `CreateActivityDto` interface

**Assumptions:**
- A1. Option A from the blocking question above: recurring templates have no container links.
- A2. `SkipContainerLink` defaults to `false` so all existing call sites are unaffected.

**Questions:**
- Q1. Is `SkipContainerLink` the right name, or would `IsTemplate` or `CreateAsRecurringTemplate` be clearer?

### Group 6a.2 — Update `CreateActivityAsync` to skip container linking when `SkipContainerLink = true`

**Why:** The service must bypass the container association logic when creating a pure template.

- [ ] In `src/backend/LifeSprint.Infrastructure/Services/ActivityService.cs`, in `CreateActivityAsync`, after the `ActivityTemplate` entity is saved, add an `if (!dto.SkipContainerLink)` guard around the entire block that determines the container and creates `ContainerActivity` records
- [ ] Inside the `if` block, keep all existing container logic (the `dto.ContainerId.HasValue` branch, the `GetOrCreateCurrentContainerAsync` call, the `GetParentContainerTypes` propagation)
- [ ] After the guard block (when `SkipContainerLink = true`), skip directly to returning `GetActivityByIdAsync`

**Assumptions:**
- A1. When `SkipContainerLink = true`, the returned `ActivityResponseDto.Containers` will be an empty list. This is acceptable for the recurring section, which doesn't show container info.

**Questions:**
- None for this group.

### Group 6a.3 — Add `BuildStampedTitle` helper to `ContainerService`

**Why:** The title-stamping logic is needed by both the explicit `CreateNewContainerAsync` path and the implicit `GetOrCreateCurrentContainerAsync` path, and should live in one place.

- [ ] In `src/backend/LifeSprint.Infrastructure/Services/ContainerService.cs`, add a `private static string BuildStampedTitle(string baseTitle, Container container)` method
- [ ] Inside the method, implement the `switch` on `container.Type`:
  - `Annual`: return `$"{baseTitle} | {container.StartDate.Year}"`
  - `Monthly`: return `$"{baseTitle} | {container.StartDate:MMMM yyyy}"` *(uses `CultureInfo.InvariantCulture` or `en-US` — see Q1)*
  - `Weekly`: return `$"{baseTitle} | Week of {container.StartDate.AddDays(-1):yyyy-MM-dd}"` *(subtracts 1 day to get the Sunday label — see Assumption A1)*
  - `Daily`: return `$"{baseTitle} | {container.StartDate:yyyy-MM-dd}"`
  - default: return `baseTitle`

**Assumptions:**
- A1. Weekly label uses `StartDate.AddDays(-1)` because `StartDate` is Monday (ISO week start) and the user expects the "Week of \<Sunday\>" format — confirmed by comparing the user's example ("Week of 2026-04-26" = Sunday) with the `DateNavigator` display logic which also subtracts one day.

**Questions:**
- Q1. Should `MMMM yyyy` formatting (month name) use the server's culture or explicitly `en-US`? Currently no culture is specified anywhere in the backend. If the server runs in a non-English locale, "April 2026" could become "avril 2026". Should we explicitly set `CultureInfo.InvariantCulture` or `new CultureInfo("en-US")`?

### Group 6a.4 — Add `InstantiateRecurringItemsAsync` to `ContainerService`

**Why:** This private method does the actual work of finding recurring templates and creating concrete stamped copies when a new container is opened.

- [ ] In `ContainerService.cs`, add `private async Task InstantiateRecurringItemsAsync(string userId, Container newContainer)` method
- [ ] Inside the method, map `newContainer.Type` to the corresponding `RecurrenceType` value:
  - `ContainerType.Annual → RecurrenceType.Annual`
  - `ContainerType.Monthly → RecurrenceType.Monthly`
  - `ContainerType.Weekly → RecurrenceType.Weekly`
  - `ContainerType.Daily → RecurrenceType.Daily`
- [ ] Query `_context.ActivityTemplates` for records where `UserId == userId`, `IsRecurring == true`, `RecurrenceType == matchingRecurrenceType`, and `ArchivedAt == null`
- [ ] For each template found, compute `stampedTitle = BuildStampedTitle(template.Title, newContainer)`
- [ ] Query whether an `ActivityTemplate` already exists for `userId` with `Title == stampedTitle` AND has a `ContainerActivity` for `newContainer.Id`; if so, `continue`
- [ ] Create a new `ActivityTemplate` with: `UserId = userId`, `Title = stampedTitle`, `Description = template.Description`, `Type = template.Type`, `ParentActivityId = null` *(see Q1)*, `IsRecurring = false`, `RecurrenceType = RecurrenceType.None`, `CreatedAt = DateTime.UtcNow`
- [ ] Add the new `ActivityTemplate` to the context and call `await _context.SaveChangesAsync()` to get its `Id`
- [ ] Add a `ContainerActivity` linking the new template to `newContainer.Id`, with `Order = await GetNextOrderInContainerAsync(newContainer.Id)`
- [ ] After the loop, call `await _context.SaveChangesAsync()`

**Assumptions:**
- A1. The concrete instance has `ParentActivityId = null` (it inherits the parent concept from the template conceptually, but the parent may not exist in the new container context).

**Questions:**
- Q1. Should the concrete instantiation copy the template's `ParentActivityId`? The parent project might not exist in the new container, which could cause a foreign key constraint issue or a dangling reference. Should it always be set to `null`, or should we look up whether the parent exists in the new container?
- Q2. Should instantiation auto-propagate upward (add the concrete item to parent containers via `GetParentContainerTypes`)? For example, a Monthly recurring item instantiated when creating a new Monthly container — should it also be added to the Annual container automatically?

### Group 6a.5 — Call `InstantiateRecurringItemsAsync` from `CreateNewContainerAsync`

**Why:** When a container is explicitly created via the "New Month / Sprint / Year" button, recurring items must be instantiated immediately.

- [ ] In `ContainerService.cs`, in `CreateNewContainerAsync`, after the rollover logic (`if (rolloverIncomplete && ...)` block ends and `SaveChangesAsync` is called), add a call to `await InstantiateRecurringItemsAsync(userId, newContainer)`
- [ ] Note: `userId` is not currently a parameter of `CreateNewContainerAsync` — add `string userId` as a parameter to the method signature
- [ ] Update `IContainerService` interface to add `userId` parameter to `CreateNewContainerAsync`
- [ ] Update `ContainersController.cs` to pass the current user's ID to `CreateNewContainerAsync`

**Assumptions:**
- A1. The `userId` parameter was omitted from `CreateNewContainerAsync` originally because it wasn't needed; the rollover logic used `previousContainer` which was already scoped to the user. Adding `userId` is a safe interface change.

**Questions:**
- None for this group.

### Group 6a.6 — Call `InstantiateRecurringItemsAsync` from `GetOrCreateCurrentContainerAsync`

**Why:** When a container is implicitly created (e.g., user creates their first item of the month), recurring items should also be instantiated so the backlog starts populated.

- [ ] In `ContainerService.cs`, in `GetOrCreateCurrentContainerAsync`, after `await _context.SaveChangesAsync()` (after creating the new container), add a call to `await InstantiateRecurringItemsAsync(userId, newContainer)`
- [ ] Note: `userId` is already a parameter of `GetOrCreateCurrentContainerAsync` — no signature change needed

**Assumptions:**
- A1. `InstantiateRecurringItemsAsync` is idempotent: if called multiple times for the same container and user, it will find existing instantiations via the title-match check and skip them. This is safe even though `GetOrCreateCurrentContainerAsync` may be called multiple times during auto-propagation.

**Questions:**
- Q1. Is auto-instantiation on implicit container creation desired? If a user is just adding an Annual item and an implicit Annual container is created, they'll also get all Annual recurring items instantiated. Is this the correct behavior, or should auto-instantiation only happen on the explicit "New Month / Sprint / Year" action?

---

## Phase 6b — Recurring: Filter API for Recurring Templates

### Group 6b.1 — Update `IActivityService.GetActivitiesForUserAsync` signature

**Why:** The interface must include the new optional filter parameters before the service and controller can use them.

- [ ] In `src/backend/LifeSprint.Core/Interfaces/IActivityService.cs`, update the `GetActivitiesForUserAsync` method signature to add `bool? isRecurring = null` and `RecurrenceType? recurrenceType = null` as optional parameters after the existing `int? containerId = null`

### Group 6b.2 — Implement filters in `ActivityService.GetActivitiesForUserAsync`

**Why:** The service must apply the `isRecurring` and `recurrenceType` filters to the EF Core query.

- [ ] In `src/backend/LifeSprint.Infrastructure/Services/ActivityService.cs`, in `GetActivitiesForUserAsync`, after the existing `containerId` filter block, add:
  ```csharp
  if (isRecurring.HasValue)
      query = query.Where(at => at.IsRecurring == isRecurring.Value);
  if (recurrenceType.HasValue)
      query = query.Where(at => at.RecurrenceType == recurrenceType.Value);
  ```

**Assumptions:**
- A1. When both `isRecurring = true` and `recurrenceType` are provided, both filters apply (AND, not OR).
- A2. When `isRecurring = true` is set, the `containerType` filter is NOT applied (recurring templates have no `ContainerActivity` records). The existing filter logic checks `at.ContainerActivities.Any(...)`, which would return 0 results for templates. So `isRecurring = true` and `containerType` should be mutually exclusive in practice, even if both are technically accepted by the endpoint.

**Questions:**
- Q1. Should the endpoint reject a request that has both `isRecurring=true` and `containerType` set, or silently return an empty list (which is what would happen given the `ContainerActivity` filter)?

### Group 6b.3 — Update `ActivitiesController.GetActivities` to accept new query params

**Why:** The controller must pass the new parameters from the HTTP request to the service.

- [ ] In `src/backend/LifeSprint.Api/Controllers/ActivitiesController.cs`, in the `GetActivities` action signature, add `[FromQuery] bool? isRecurring = null` and `[FromQuery] RecurrenceType? recurrenceType = null` parameters
- [ ] In the action body, pass `isRecurring` and `recurrenceType` to `_activityService.GetActivitiesForUserAsync`

### Group 6b.4 — Update `activityService.getActivities` in the frontend

**Why:** The frontend service must support the new query parameters to fetch recurring templates.

- [ ] In `src/frontend/src/services/activityService.ts`, in `getActivities`, add `isRecurring?: boolean` and `recurrenceType?: RecurrenceType` as optional parameters to the function signature
- [ ] In the function body, add `if (isRecurring !== undefined) params.set('isRecurring', String(isRecurring))` after the existing `containerId` block
- [ ] In the function body, add `if (recurrenceType !== undefined) params.set('recurrenceType', String(recurrenceType))` after the `isRecurring` block

---

## Phase 6c — Recurring: Sidebar and Routes

### Group 6c.1 — Add four new routes for recurring pages

**Why:** The router must know about the `/recurring/*` paths before the sidebar can link to them.

- [ ] In `src/frontend/src/router/index.tsx`, inside the `ProtectedRoute` / `MainLayout` children array, add four new route entries:
  - `{ path: 'recurring/annual', element: <AnnualRecurring /> }`
  - `{ path: 'recurring/monthly', element: <MonthlyRecurring /> }`
  - `{ path: 'recurring/weekly', element: <WeeklyRecurring /> }`
  - `{ path: 'recurring/daily', element: <DailyRecurring /> }`
- [ ] In `src/frontend/src/router/index.tsx`, add the import for all four page components from `'../pages/recurring'`

**Assumptions:**
- A1. The recurring pages live under `src/frontend/src/pages/recurring/` and are exported from an `index.ts` barrel file in that directory.

**Questions:**
- None for this group.

### Group 6c.2 — Add "Recurring Items" section to `Sidebar`

**Why:** Users need navigation links to the recurring section.

- [ ] In `src/frontend/src/components/Layout/Sidebar.tsx`, below the closing `</div>` of the "Backlogs" section, add a new `<div className={styles.section}>` block
- [ ] Inside the new block, add `<span className={styles.sectionLabel}>Recurring Items</span>`
- [ ] Add four `<NavLink>` entries: `to="/recurring/annual"` (label "Annual"), `to="/recurring/monthly"` (label "Monthly"), `to="/recurring/weekly"` (label "Weekly Sprint"), `to="/recurring/daily"` (label "Daily Checklist")
- [ ] Apply the same `className` pattern as the existing Backlog nav links for active state highlighting

**Assumptions:**
- A1. The "Recurring Items" section is not collapsible — it is always expanded, consistent with the "Backlogs" section.

**Questions:**
- Q1. Should the "Recurring Items" sidebar section be collapsible (with a toggle arrow), or always expanded like "Backlogs"?

---

## Phase 6d — Recurring: Pages and Hook

### Group 6d.1 — Create `useRecurringItems` hook

**Why:** The four recurring pages share identical data-fetching logic; a custom hook avoids duplication.

- [ ] Create `src/frontend/src/hooks/useRecurringItems.ts`
- [ ] Define and export `interface UseRecurringItemsResult` with: `activities: Activity[]`, `loading: boolean`, `error: string | null`, `handleCreate: (data: CreateActivityDto) => Promise<void>`, `handleUpdate: (id: number, data: UpdateActivityDto) => Promise<void>`, `handleDelete: (id: number) => Promise<void>`, `reload: () => Promise<void>`
- [ ] Implement `export function useRecurringItems(recurrenceType: RecurrenceType): UseRecurringItemsResult`
- [ ] Inside the hook, add state: `activities`, `loading`, `error`
- [ ] Add a `loadActivities` `useCallback` that calls `activityService.getActivities({ isRecurring: true, recurrenceType })` and sets state
- [ ] Add a `useEffect` that calls `loadActivities()` when `recurrenceType` changes
- [ ] Implement `handleCreate`: calls `activityService.createActivity({ ...data, skipContainerLink: true })`, then calls `loadActivities()` *(see Assumption A1)*
- [ ] Implement `handleUpdate`: calls `activityService.updateActivity(id, data)`, then calls `loadActivities()`
- [ ] Implement `handleDelete`: calls `activityService.deleteActivity(id)`, then calls `loadActivities()`
- [ ] Return all values

**Assumptions:**
- A1. When creating from the recurring section, `skipContainerLink: true` is always passed. This prevents the template from being linked to a container.

**Questions:**
- Q1. Should creating a recurring template from the Recurring section also immediately instantiate a concrete copy in the current period's container? If yes, the backend's `CreateActivityAsync` must detect `IsRecurring = true && SkipContainerLink = true` and call `InstantiateRecurringItemsAsync` for the current period.

### Group 6d.2 — Update `ActivityEditor` to support locked fields

**Why:** The recurring pages need to open the editor with `isRecurring` and `recurrenceType` pre-set and prevented from changing.

- [ ] In `src/frontend/src/components/Activities/ActivityEditor.tsx`, add `fixedIsRecurring?: boolean` and `fixedRecurrenceType?: RecurrenceType` to `ActivityEditorProps`
- [ ] In `ActivityEditor.tsx`, initialize `isRecurring` state from `fixedIsRecurring ?? editingActivity?.isRecurring ?? false`
- [ ] In `ActivityEditor.tsx`, initialize `recurrenceType` state from `fixedRecurrenceType ?? editingActivity?.recurrenceType ?? RecurrenceType.None`
- [ ] In `ActivityEditor.tsx`, on the "Recurring Activity" checkbox, add `disabled={fixedIsRecurring !== undefined}` to the `<input>`
- [ ] In `ActivityEditor.tsx`, on the recurrence type `<select>`, add `disabled={fixedRecurrenceType !== undefined}`

**Assumptions:**
- A1. Disabled fields are visible (not hidden), so the user can see what is set but cannot change it.

**Questions:**
- Q1. Should disabled fields be visually distinct (e.g., greyed out, with a lock icon or tooltip explaining why), or just the default browser disabled style?

### Group 6d.3 — Create the four recurring page components

**Why:** Each recurring page needs its own component to mount with the correct `recurrenceType`.

- [ ] Create directory `src/frontend/src/pages/recurring/`
- [ ] Create `src/frontend/src/pages/recurring/AnnualRecurring.tsx`
- [ ] In `AnnualRecurring.tsx`, call `useRecurringItems(RecurrenceType.Annual)`
- [ ] Create `src/frontend/src/pages/recurring/MonthlyRecurring.tsx`
- [ ] In `MonthlyRecurring.tsx`, call `useRecurringItems(RecurrenceType.Monthly)`
- [ ] Create `src/frontend/src/pages/recurring/WeeklyRecurring.tsx`
- [ ] In `WeeklyRecurring.tsx`, call `useRecurringItems(RecurrenceType.Weekly)`
- [ ] Create `src/frontend/src/pages/recurring/DailyRecurring.tsx`
- [ ] In `DailyRecurring.tsx`, call `useRecurringItems(RecurrenceType.Daily)`
- [ ] Each page component shares this structure:
  - Page header with title (e.g., "Monthly Recurring Items") and a "New Recurring Item" button
  - An `ActivityEditor` shown when the button is clicked, with `fixedIsRecurring={true}` and `fixedRecurrenceType={RecurrenceType.Monthly}` (matching the page)
  - An `ActivityList` with `onToggleCompletion` **omitted** (no completion in the recurring section)
  - No `DateNavigator`, no `NewContainerModal`, no `MoveActivityModal`
- [ ] Create `src/frontend/src/pages/recurring/index.ts`
- [ ] In `index.ts`, export all four page components
- [ ] In `src/frontend/src/pages/index.ts`, add exports for the four recurring pages (or re-export from `./recurring`)

**Assumptions:**
- A1. Recurring pages reuse `BacklogPage.module.css` for consistent layout.
- A2. All four recurring page components have nearly identical structure; the only difference is the `recurrenceType` passed to `useRecurringItems` and `ActivityEditor`.

**Questions:**
- Q1. Should the recurring page's `ActivityEditor` also lock the `type` field (Project/Epic/Story/Task)? Or should the user be free to choose the activity type for a recurring template?

---

## Phase 6e — Recurring: Tests

### Group 6e.1 — Backend integration tests for recurring item instantiation

**Why:** The auto-instantiation logic has complex duplicate-prevention and name-stamping behavior that must be verified at the database level.

- [ ] Create `src/backend/LifeSprint.Tests/Integration/RecurringItemsIntegrationTests.cs`
- [ ] Decorate the class with `[Collection("IntegrationTests")]` and `[Trait("Category", "Integration")]`
- [ ] Inherit from `IntegrationTestBase`
- [ ] Add test `CreateRecurringTemplate_WithSkipContainerLink_HasNoContainerActivities`: create an `ActivityTemplate` with `IsRecurring = true`, `RecurrenceType = Monthly`, `SkipContainerLink = true`; assert no `ContainerActivity` records exist for that template
- [ ] Add test `CreateNewMonthlyContainer_InstantiatesMatchingRecurringTemplates`: seed a recurring Monthly template "Pay Bills"; call `ContainerService.CreateNewContainerAsync` for Monthly; assert an `ActivityTemplate` exists with title matching `"Pay Bills | {month} {year}"` and a `ContainerActivity` linking it to the new container
- [ ] Add test `CreateNewWeeklyContainer_UsesWeekOfSundayLabelInTitle`: seed a recurring Weekly template "Mow the Lawn"; call `CreateNewContainerAsync` for Weekly; assert the concrete title is `"Mow the Lawn | Week of {sunday-date}"` where `{sunday-date}` is `container.StartDate.AddDays(-1)` formatted as `yyyy-MM-dd`
- [ ] Add test `CreateNewContainer_WhenRecurringItemAlreadyInstantiated_DoesNotCreateDuplicate`: create a Monthly container once (instantiates the template), then verify calling `InstantiateRecurringItemsAsync` directly on the same container a second time does not create a second concrete instance
- [ ] Add test `GetActivities_WithIsRecurringTrue_ReturnsOnlyRecurringTemplates`: seed one recurring and one non-recurring template; call `GetActivitiesForUserAsync(isRecurring: true)`; assert only the recurring one is returned
- [ ] Add test `GetActivities_WithRecurrenceTypeMonthly_ReturnsOnlyMonthlyRecurring`: seed one Monthly and one Weekly recurring template; call `GetActivitiesForUserAsync(isRecurring: true, recurrenceType: RecurrenceType.Monthly)`; assert only the Monthly one is returned
- [ ] Add test `GetActivities_WithContainerTypeMonthly_ExcludesRawRecurringTemplates`: seed a recurring template (no container link) and a regular activity in a Monthly container; call `GetActivitiesForUserAsync(containerType: ContainerType.Monthly)`; assert the recurring template is NOT in the result (only the regular activity is)
- [ ] Override `CleanupTestDataAsync` to also delete any test data created by these tests *(or rely on the base class raw SQL which deletes all templates for the test user)*

**Questions:**
- None for this group, pending resolution of the blocking questions in Phase 6a.

### Group 6e.2 — E2E tests for recurring items

**Why:** Verify the complete user journey from creating a template in the Recurring section to seeing the stamped instance appear automatically in the backlog.

- [ ] Create `src/frontend/e2e/recurring-items.spec.ts`
- [ ] Add `beforeEach` authenticating with username `e2e-recurring-user`
- [ ] Add test: navigate to `/recurring/monthly`, verify "Monthly Recurring Items" heading is visible
- [ ] Add test: click "New Recurring Item" on `/recurring/monthly`, verify the `ActivityEditor` appears with the "Recurring Activity" checkbox pre-checked and disabled, and the recurrence type showing "Monthly" and disabled
- [ ] Add test: fill in title "Pay Bills", click Save, verify "Pay Bills" appears in the Monthly Recurring list
- [ ] Add test: navigate to `/monthly`, click "New Month", click "Create Month" (with rollover off), verify the activity list contains an item titled `"Pay Bills | {current-month-name} {year}"` (e.g., "Pay Bills | April 2026")
- [ ] Add test: navigate back to `/recurring/monthly`, verify "Pay Bills" still appears unchanged (the template was not modified)
- [ ] Add test: the "Pay Bills | April 2026" item in `/monthly` has no completion checkbox (it's a normal concrete item — it DOES have a checkbox; verify the checkbox is present and can be toggled)
- [ ] Add test: edit "Pay Bills" template title to "Pay Student Loan" via the Edit button in `/recurring/monthly`; verify the change is reflected; verify the already-created "Pay Bills | April 2026" item is NOT renamed (concrete instances are independent)

**Assumptions:**
- A1. The month label in the stamped title uses English month names ("April 2026"), consistent with the backend using either `InvariantCulture` or `en-US`.

**Questions:**
- Q1. Same as Phase 6a Q1 about the monthly format culture.

---

## Outstanding Questions Summary

The following questions must be answered before implementation can proceed on the indicated phases. All other phases can proceed independently.

| # | Phase | Question |
|---|---|---|
| 1 | Phase 6a (blocking) | Should recurring templates have no container link (Option A: pure templates) or a container link (Option B: appears in backlog)? |
| 2 | Phase 6a (blocking) | When a recurring template is created, should a concrete instance be immediately created for the current period? |
| 3 | Phase 6a (blocking) | If Q2 is yes: should the backend implicitly create the container if none exists yet, or skip instantiation? |
| 4 | Phase 6a | Should the concrete instance copy `ParentActivityId` from the template? |
| 5 | Phase 6a | Should concrete instances auto-propagate to parent containers (e.g., a Monthly item also added to Annual)? |
| 6 | Phase 6a | Should `InstantiateRecurringItemsAsync` also be triggered from `GetOrCreateCurrentContainerAsync` (implicit creation)? |
| 7 | Phase 6a | Should `MMMM yyyy` in `BuildStampedTitle` use `en-US` explicitly or rely on server culture? |
| 8 | Phase 6b | Should the endpoint reject requests with both `isRecurring=true` and `containerType` set, or silently return an empty list? |
| 9 | Phase 6c | Should the "Recurring Items" sidebar section be collapsible or always expanded? |
| 10 | Phase 6d | Should creating a recurring template from the Recurring section also immediately instantiate a concrete copy in the current period? *(same as Q2)* |
| 11 | Phase 6d | Should the `ActivityEditor` in recurring pages also lock the `type` field, or keep it free? |
| 12 | Phase 5 | When adding a child via the "Add" button, which container should the child be placed in — the page's current container, or the same container(s) as the parent? |
| 13 | Phase 4 | Should the reorder endpoint put `containerId` in the URL (`PATCH /{id}/containers/{containerId}/reorder`) or in the request body (`PATCH /{id}/reorder`)? |
| 14 | Phase 4 | Should boundary arrow buttons (first item's ▲, last item's ▼) be hidden or disabled? |
| 15 | Phase 0 | Are there any raw numeric `RecurrenceType` values hardcoded in existing frontend tests or E2E specs that need updating? |
| 16 | Phase 1/2/3 | Should E2E test users be unique per test file to prevent data cross-contamination, or should a shared user with explicit data cleanup be used? |
| 17 | Phase 2 | How should E2E container tests handle a 409 conflict when the current period's container already exists from a prior test run? |

### Answers

1. Yes... This is a hard question. Only container links should be to other recurring items. It does not make sense to have a recurring 'Story' in a singleton 'Epic'. But a recurring 'Story' can have recurring 'Tasks' - Their recurrence rate is the same though. Let's not do anything too crazy. 
2. Yes, when a recurring template is created please create an instance immediately for that period.
3. In the rare event a container does not exist, don't create one. When the user creates it, then the process should check if recurring events exist and add them at this time of instantiation. 
4. No. Given if we allow nested recurring events, If we have a recurring 'Story' then we want that 'Task' items to inherit the concrete 'Story' `ParentActivityId`.
5. Yes. Treat the concrete instances or recurring items like their singleton counterparts for now.
6. Yes, I think this is reasonable.
7. Use the server culture please.
8. Please use your judgement on this, whatever makes more sense.
9. Keep it always expanded for now please.
10. Yes - and maybe a point to clarify; I don't think we should allow for recurring events to be created in the 'Backlog' section, they should only be created in their new 'Recurring' pages. That is a good question, thanks for pointing that out.
11. Please use your judgement here - whatever is easier to implement now. If there are issues, we will address them later.
12. I see, assuming a 'Story' is as far as the 'Weekly', but we are adding a 'Task' in the monthly. This child should be added to the context it is created, not it's parent's container. The user would move it when it is ready.
13. Request body please.
14. Disabled please.
15. I am unsure, please perform code exploration to discover the answer. I would hope that any raw numeric types that represent anything would be encapsulated in an Enum to explain its existence though.
16. Please have unique users per test file to ensure no cross-contamination occurrs. I believe the test database is wiped or deleted after running tests - please double check to avoid sessions bleeding into each other.
17. The idea around my Test Database was to spin up a fresh database instance for new test runs, and deleting the Test database when a run is complete. This should allow infinite new users and also keep test runs from interacting with each other. 
