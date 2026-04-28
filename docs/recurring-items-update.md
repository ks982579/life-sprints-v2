# Life Sprints v2 — Feature Plan

This document covers all features and bug fixes requested. Each section includes the problem statement, the proposed solution, files affected, and the required tests.

---

## 0. Prerequisites & Shared Infrastructure

Before implementing any feature, confirm these are working correctly:

- `docker compose up` brings all services up cleanly
- `npm test` (Vitest) and `dotnet test --filter "Category=Unit"` pass
- Integration test DB is reachable at `localhost:5433`
- E2E baseline (`e2e/auth.spec.ts`) passes

---

## 1. Bug Fix — New Container Creates Without Page Reload

### Problem

When a user clicks "New Month / New Sprint / New Year" and the container is created successfully, the `DateNavigator` does not update and activities for the new container are not visible until the page is hard-reloaded. This happens because:

- `useBacklog` exposes `reload()` which only reloads **activities**, not the containers list.
- `handleContainerCreated` in each page calls `reload()` plus a one-off `containerService.getContainers()` to update `allContainers` (for the move modal), but does **not** update the `containers` state inside `useBacklog`.
- The `DateNavigator` and selected container are driven by `useBacklog`'s internal `containers` state, so the new container never appears there.

### Solution

**Backend change (minor):** `POST /api/containers/new` already returns the newly created `ContainerResponseDto`. No backend change needed — the frontend just needs to use the returned object.

**Frontend changes:**

1. Add `reloadContainers` to `UseBacklogResult` in `useBacklog.ts`:
   ```typescript
   reloadContainers: () => Promise<void>;
   ```
   Expose the existing `loadContainers` callback through the hook's return value.

2. Update `handleContainerCreated` in `AnnualBacklog.tsx`, `MonthlyBacklog.tsx`, `WeeklySprint.tsx`, and `DailyChecklist.tsx` (if/when it gets a "New Day" button) to call `reloadContainers()`:
   ```typescript
   const handleContainerCreated = async (newContainer: Container) => {
     setShowNewContainer(false);
     await reloadContainers();           // updates DateNavigator
     await reload();                     // updates activity list
     setSelectedContainerId(newContainer.id); // auto-navigate to new container
     const updated = await containerService.getContainers();
     setAllContainers(updated);
   };
   ```

3. Update `NewContainerModal` to pass the created container back to `onCreated`:
   ```typescript
   interface NewContainerModalProps {
     onCreated: (container: Container) => void;
     // ...
   }
   ```
   The `createNewContainer` service call already returns a `Container`. Pass it through.

**Files to change:**
- `src/frontend/src/hooks/useBacklog.ts`
- `src/frontend/src/components/Activities/NewContainerModal.tsx`
- `src/frontend/src/pages/AnnualBacklog.tsx`
- `src/frontend/src/pages/MonthlyBacklog.tsx`
- `src/frontend/src/pages/WeeklySprint.tsx`

### Tests

**Integration test** (`LifeSprint.Tests/Integration/`): Verify `POST /api/containers/new` returns the full `ContainerResponseDto` with correct `id`, `type`, `startDate`, and `status`.

**E2E test** (`e2e/containers.spec.ts`):
- Authenticate via `test-login`
- Navigate to Monthly Backlog
- Click "New Month" — confirm modal appears
- Click "Create Month" — confirm the `DateNavigator` shows the new month **without a page reload**
- Confirm the activity list is empty (fresh start) or contains rolled-over items depending on selection

---

## 2. Bug Fix — Daily Checklist Shows No Items

### Problem

The `DailyChecklist` page renders correctly but items created there never appear. The likely causes:

1. When `handleCreate` is called with `containerId: selectedContainerId` and `selectedContainerId` is `undefined`, the backend falls back to `defaultContainerType`. But the Daily page does **not** pass `defaultContainerType` in its create call — it omits it, so the backend defaults to `Annual (0)`, creating the item in the Annual backlog instead of Daily.

2. The `ActivityList` then filters by `containerType === ContainerType.Daily`, but the item only has an Annual `ContainerActivity`, so it is invisible on the Daily page.

### Solution

**Frontend change:**

In `DailyChecklist.tsx`, pass `defaultContainerType: ContainerType.Daily` in the create call:
```typescript
await handleCreate({
  ...data,
  containerId: selectedContainerId,
  defaultContainerType: ContainerType.Daily,
});
```

This mirrors how `MonthlyBacklog.tsx` and `WeeklySprint.tsx` pass their own `defaultContainerType`.

**Files to change:**
- `src/frontend/src/pages/DailyChecklist.tsx`

### Tests

**Integration test:** `POST /api/activities` with `defaultContainerType: 3` (Daily) and no `containerId` — verify the response contains a `ContainerActivity` with `containerType === 3`.

**E2E test** (`e2e/daily-checklist.spec.ts`):
- Authenticate
- Navigate to Daily Checklist
- Create a new item titled "Daily task"
- Confirm the item appears in the Daily list immediately (no reload)
- Toggle completion — confirm the checkbox reflects the change

---

## 3. Integration & E2E Tests — CRUD Coverage Per Section

Each backlog section (Annual, Monthly, Weekly, Daily) needs integration and E2E tests covering the full CRUD lifecycle.

### Backend Integration Tests

Create `LifeSprint.Tests/Integration/ContainerCrudIntegrationTests.cs` (or extend existing test files) covering per-container-type:

- **Create item in container** — verify `ContainerActivity` is created for the correct type and auto-propagates upward
- **Read items for container** — verify `GET /api/activities?containerType=N` returns only items linked to that type
- **Update item** — verify title/description changes persist
- **Delete (archive) item** — verify item disappears from list queries but `ArchivedAt` is set in DB
- **Toggle completion** — verify `CompletedAt` is set on all `ContainerActivity` records for the item
- **New container creation** — verify `POST /api/containers/new` creates container with correct date range and returns 409 if one already exists

Extend `LifeSprint.Tests/Integration/ActivityServiceIntegrationTests.cs` and `ActivitiesControllerIntegrationTests.cs` to add Daily container type coverage (currently likely missing).

### Frontend E2E Tests

Create `e2e/annual-backlog.spec.ts`, `e2e/monthly-backlog.spec.ts`, `e2e/weekly-sprint.spec.ts`, `e2e/daily-checklist.spec.ts`. Each file should test:

1. Navigate to the page
2. Create an item — verify it appears
3. Edit the item title — verify update reflects without reload
4. Toggle completion — verify checkbox state persists
5. Delete item — verify it disappears
6. (For Annual/Monthly/Weekly) Create new container — verify `DateNavigator` updates

---

## 4. UI — "Add Child" Button on Project / Epic / Story Items

### Problem

Creating a child item requires the user to use the global "New Item" form at the top of the page and manually select the correct type and parent. A contextual "Add" button on each parent item would be more convenient.

### Solution

Add an "Add Child" button to `ActivityList.tsx` for items of type `Project`, `Epic`, and `Story`. When clicked, open a centered modal (similar to `ActivityEditor`) with:
- **Type** field: pre-set to the next level down, read-only/disabled (Project → Epic, Epic → Story, Story → Task)
- **Parent Activity** field: pre-set to the clicked item, read-only/disabled
- All other fields editable normally (title, description, recurring)

**Child type mapping:**

| Parent type | Fixed child type |
|---|---|
| Project | Epic |
| Epic | Story |
| Story | Task |

**New component:** `AddChildModal.tsx` in `src/frontend/src/components/Activities/`

Props:
```typescript
interface AddChildModalProps {
  parent: Activity;
  onSave: (data: CreateActivityDto) => Promise<void>;
  onClose: () => void;
}
```

Internally renders an `ActivityEditor`-like form with `type` and `parentActivityId` locked.

**`ActivityList.tsx` change:** Add an "Add" button next to "Edit" for Project, Epic, and Story items. Pass `onAddChild?: (parent: Activity) => void` to the component.

**Page changes:** Each backlog page manages `addChildParent: Activity | null` state, opens/closes `AddChildModal`, and calls `handleCreate` with the fixed `type` and `parentActivityId`.

**Files to change/create:**
- `src/frontend/src/components/Activities/AddChildModal.tsx` (new)
- `src/frontend/src/components/Activities/AddChildModal.module.css` (new)
- `src/frontend/src/components/Activities/ActivityList.tsx`
- `src/frontend/src/pages/AnnualBacklog.tsx`
- `src/frontend/src/pages/MonthlyBacklog.tsx`
- `src/frontend/src/pages/WeeklySprint.tsx`
- `src/frontend/src/pages/DailyChecklist.tsx`

### Tests

**Unit test** (`__tests__/AddChildModal.test.tsx`): Render with a Project parent, verify type selector shows "Epic" and is disabled, verify parent field shows parent title and is disabled, verify submit calls `onSave` with correct `type` and `parentActivityId`.

**E2E**: On the Annual Backlog, create a Project, click its "Add" button, submit an Epic title — verify the Epic appears with the correct parent label.

---

## 5. UI — Up / Down Arrow Reordering

### Problem

Items have an `Order` field on `ContainerActivity` but there is no UI to change it. Users cannot reorganize their list.

### Solution

**Backend:**

Add a new endpoint:
```
PATCH /api/activities/{id}/containers/{containerId}/order
Body: { "order": number }
```

Or, more practical: a swap endpoint:
```
PATCH /api/activities/{id}/containers/{containerId}/reorder
Body: { "direction": "up" | "down" }
```

The service computes the swap: find the adjacent item (next lower or higher `Order`), swap the two `Order` values, and save.

Alternatively, use a simple `order` integer PATCH and let the frontend compute the new value.

**Recommended approach:** `PATCH /api/activities/{activityId}/reorder` with body `{ containerId, direction }`. The service finds the neighbor in the same container, swaps their `Order` values.

**Backend files to change/create:**
- `src/backend/LifeSprint.Core/Interfaces/IActivityService.cs` — add `ReorderActivityAsync`
- `src/backend/LifeSprint.Infrastructure/Services/ActivityService.cs` — implement `ReorderActivityAsync`
- `src/backend/LifeSprint.Api/Controllers/ActivitiesController.cs` — add `PATCH /{id}/reorder` action

**Frontend:**

In `ActivityList.tsx`, add up (▲) and down (▼) arrow buttons at the far right of each item row. The up arrow is hidden on the first item; the down arrow is hidden on the last item.

Add `onReorder?: (activityId: number, containerId: number, direction: 'up' | 'down') => void` to `ActivityListProps`.

Add `reorderActivity` to `activityService.ts`.

Add `handleReorder` to each page (calls service, then `reload()`).

**Frontend files to change:**
- `src/frontend/src/components/Activities/ActivityList.tsx`
- `src/frontend/src/services/activityService.ts`
- `src/frontend/src/pages/AnnualBacklog.tsx`
- `src/frontend/src/pages/MonthlyBacklog.tsx`
- `src/frontend/src/pages/WeeklySprint.tsx`
- `src/frontend/src/pages/DailyChecklist.tsx`

### Tests

**Unit test (backend):** `ReorderActivityAsync` — verify the target item and its neighbor swap `Order` values; verify the first item cannot move up (no-op or error); verify the last item cannot move down.

**E2E:** Create two items in order. Click the down arrow on the first item. Verify items are now in reversed order in the list.

---

## 6. Recurring Items — Full Implementation

This is the largest feature. It introduces a distinct UI section for recurring item *templates* and a factory pattern: when a new container is created, recurring items of the matching type are automatically instantiated as concrete `ContainerActivity` records with a time-stamped title.

### Conceptual Model

Recurring items already exist as `ActivityTemplate` records with `IsRecurring = true` and a non-None `RecurrenceType`. What's missing is:

1. **A dedicated UI section** for viewing and managing recurring templates without completion state.
2. **Auto-instantiation** when a new container is created — the backend looks for matching recurring templates and creates new concrete activities.
3. **Duplicate prevention** — if a recurring item has already been instantiated for a given container (detected by name pattern or a new FK), skip it.

### 6a. Backend — Recurring Item Instantiation on Container Creation

**Name format:**
- Annual recurring → `"<title> | <YYYY>"` e.g. `"Annual Review | 2026"`
- Monthly recurring → `"<title> | <Month YYYY>"` e.g. `"Pay Student Loan | April 2026"`
- Weekly recurring → `"<title> | Week of <YYYY-MM-dd>"` (Monday's date) e.g. `"Mow the Lawn | Week of 2026-04-26"`
- Daily recurring → `"<title> | <YYYY-MM-dd>"` e.g. `"Morning Routine | 2026-04-28"`

**Duplicate prevention strategy:** Check whether any `ActivityTemplate` for the user already has a title matching the stamped name AND has a `ContainerActivity` linking it to the new container. If so, skip creation.

**Where to implement:** In `ContainerService.CreateNewContainerAsync`, after the rollover logic, call a new private method `InstantiateRecurringItemsAsync(userId, newContainer)`.

```csharp
private async Task InstantiateRecurringItemsAsync(string userId, Container newContainer)
{
    // Map container type to recurrence type
    var matchingRecurrenceType = newContainer.Type switch {
        ContainerType.Annual  => RecurrenceType.Annual,
        ContainerType.Monthly => RecurrenceType.Monthly,
        ContainerType.Weekly  => RecurrenceType.Weekly,
        ContainerType.Daily   => RecurrenceType.Daily,
        _ => throw new ArgumentException("Unknown type")
    };

    // Find all recurring templates for this user and recurrence type
    var recurringTemplates = await _context.ActivityTemplates
        .Where(at => at.UserId == userId
                  && at.IsRecurring
                  && at.RecurrenceType == matchingRecurrenceType
                  && at.ArchivedAt == null)
        .ToListAsync();

    foreach (var template in recurringTemplates)
    {
        var stampedTitle = BuildStampedTitle(template.Title, newContainer);

        // Check for existing instantiation (by title match)
        var alreadyExists = await _context.ActivityTemplates
            .AnyAsync(at => at.UserId == userId
                         && at.Title == stampedTitle
                         && at.ContainerActivities.Any(ca => ca.ContainerId == newContainer.Id));

        if (alreadyExists) continue;

        // Create a new concrete ActivityTemplate for this period
        var concrete = new ActivityTemplate {
            UserId = userId,
            Title = stampedTitle,
            Description = template.Description,
            Type = template.Type,
            ParentActivityId = template.ParentActivityId,
            IsRecurring = false,
            RecurrenceType = RecurrenceType.None,
            CreatedAt = DateTime.UtcNow
        };
        _context.ActivityTemplates.Add(concrete);
        await _context.SaveChangesAsync();

        // Link to the new container (and propagate upward)
        _context.ContainerActivities.Add(new ContainerActivity {
            ContainerId = newContainer.Id,
            ActivityTemplateId = concrete.Id,
            AddedAt = DateTime.UtcNow,
            Order = await GetNextOrderInContainerAsync(newContainer.Id),
            IsRolledOver = false
        });
    }

    await _context.SaveChangesAsync();
}
```

Also call `InstantiateRecurringItemsAsync` from `GetOrCreateCurrentContainerAsync` when a new container is first created implicitly (when a user creates an item and no container exists).

**`BuildStampedTitle` helper:**
```csharp
private static string BuildStampedTitle(string baseTitle, Container container)
{
    return container.Type switch {
        ContainerType.Annual  => $"{baseTitle} | {container.StartDate.Year}",
        ContainerType.Monthly => $"{baseTitle} | {container.StartDate:MMMM yyyy}",
        ContainerType.Weekly  => $"{baseTitle} | Week of {container.StartDate:yyyy-MM-dd}",
        ContainerType.Daily   => $"{baseTitle} | {container.StartDate:yyyy-MM-dd}",
        _ => baseTitle
    };
}
```

**Files to change:**
- `src/backend/LifeSprint.Infrastructure/Services/ContainerService.cs`
- (No new API endpoints needed — behavior is triggered by existing `POST /api/containers/new`)

### 6b. Backend — New API Endpoint: List Recurring Templates

Add a query filter to the existing `GET /api/activities` endpoint:

```
GET /api/activities?isRecurring=true&recurrenceType=N
```

Add optional `isRecurring` and `recurrenceType` query params to `ActivitiesController.GetActivities` and `ActivityService.GetActivitiesForUserAsync`.

**Files to change:**
- `src/backend/LifeSprint.Api/Controllers/ActivitiesController.cs`
- `src/backend/LifeSprint.Core/Interfaces/IActivityService.cs`
- `src/backend/LifeSprint.Infrastructure/Services/ActivityService.cs`

### 6c. Frontend — Sidebar "Recurring Items" Section

Update `Sidebar.tsx` to add a second collapsible section below "Backlogs":

```
Backlogs
  Annual
  Monthly
  Weekly Sprint
  Daily Checklist

Recurring Items
  Annual
  Monthly
  Weekly Sprint
  Daily Checklist
```

Add four new routes:
- `/recurring/annual`
- `/recurring/monthly`
- `/recurring/weekly`
- `/recurring/daily`

**Files to change:**
- `src/frontend/src/components/Layout/Sidebar.tsx`
- `src/frontend/src/router/index.tsx`

### 6d. Frontend — Recurring Items Pages

Create four new page components (or a single shared component with a `recurrenceType` prop, analogous to `useBacklog`):

**New hook:** `useRecurringItems(recurrenceType: RecurrenceType)` in `src/frontend/src/hooks/useRecurringItems.ts`

- Calls `GET /api/activities?isRecurring=true&recurrenceType=N`
- Exposes `handleCreate`, `handleUpdate`, `handleDelete` (no toggle — recurring templates are never marked complete here)
- No container state — recurring items exist independent of containers

**New pages:**
- `src/frontend/src/pages/recurring/AnnualRecurring.tsx`
- `src/frontend/src/pages/recurring/MonthlyRecurring.tsx`
- `src/frontend/src/pages/recurring/WeeklyRecurring.tsx`
- `src/frontend/src/pages/recurring/DailyRecurring.tsx`

Each page:
- Shows only the items with the matching `RecurrenceType`
- Renders `ActivityList` **without** the completion checkbox (`onToggleCompletion` is omitted)
- Shows a "New Recurring Item" button opening `ActivityEditor` with `isRecurring` pre-checked and `recurrenceType` pre-set (and locked) to match the page
- Does **not** show `DateNavigator` or `NewContainerModal`

**`ActivityEditor` change:** Accept optional `fixedRecurrenceType` and `fixedIsRecurring` props that disable those fields when provided.

**Files to create:**
- `src/frontend/src/hooks/useRecurringItems.ts`
- `src/frontend/src/pages/recurring/AnnualRecurring.tsx`
- `src/frontend/src/pages/recurring/MonthlyRecurring.tsx`
- `src/frontend/src/pages/recurring/WeeklyRecurring.tsx`
- `src/frontend/src/pages/recurring/DailyRecurring.tsx`
- `src/frontend/src/pages/recurring/index.ts`

**Files to change:**
- `src/frontend/src/components/Activities/ActivityEditor.tsx`
- `src/frontend/src/services/activityService.ts` (add `isRecurring` / `recurrenceType` query params to `getActivities`)
- `src/frontend/src/components/Layout/Sidebar.tsx`
- `src/frontend/src/router/index.tsx`
- `src/frontend/src/pages/index.ts`

### 6e. Tests for Recurring Items

**Backend integration tests** (`LifeSprint.Tests/Integration/RecurringItemsIntegrationTests.cs`):

1. **Template creation**: Create a recurring Monthly template. Verify `IsRecurring = true`, `RecurrenceType = Monthly`.
2. **Auto-instantiation on new container**: Create a Monthly recurring template "Pay Student Loan". Call `POST /api/containers/new` with `type = Monthly`. Verify a new `ActivityTemplate` named `"Pay Student Loan | <Month YYYY>"` exists and is linked to the new container.
3. **Duplicate prevention**: Call `POST /api/containers/new` twice (second returns 409). The recurring item should only be instantiated once.
4. **Recurrence type filter**: Call `GET /api/activities?isRecurring=true&recurrenceType=3` (Monthly). Verify only Monthly recurring templates are returned.
5. **Weekly name format**: Create Weekly recurring "Mow the Lawn". Create a Weekly container. Verify instantiated title is `"Mow the Lawn | Week of <YYYY-MM-dd>"` where the date is the Monday of the current week.
6. **Recurring templates not returned in normal backlog queries**: Verify `GET /api/activities?containerType=1` does not include the raw recurring templates (they have no ContainerActivity; only their concrete instantiations appear).

**E2E tests** (`e2e/recurring-items.spec.ts`):

1. Navigate to "Recurring Items → Monthly"
2. Create a recurring item "Pay Bills"
3. Verify it appears in the Monthly Recurring list
4. Navigate to Monthly Backlog
5. Click "New Month" and create a new monthly container
6. Verify "Pay Bills | \<current month and year\>" appears in the Monthly Backlog automatically
7. Verify the recurring template "Pay Bills" still appears unchanged in the Recurring section
8. Navigate back to Monthly Backlog and verify the stamped item has no completion state carried from the template

---

## 7. Implementation Order

The features have dependencies. Implement in this order to minimize rework:

| Phase | Feature | Depends On |
|---|---|---|
| 1 | Bug Fix — Daily Checklist (§2) | Nothing |
| 2 | Bug Fix — New Container No Reload (§1) | Nothing |
| 3 | Integration & E2E CRUD tests (§3) | §1, §2 fixes |
| 4 | Up/Down Reordering (§5) | Nothing |
| 5 | Add Child Modal (§4) | Nothing |
| 6 | Recurring backend instantiation (§6a, §6b) | §1 (uses container creation path) |
| 7 | Recurring frontend pages & sidebar (§6c, §6d) | §6b |
| 8 | Recurring integration & E2E tests (§6e) | §6a–§6d |

Phases 4 and 5 can be done in parallel with Phase 6.

---

## 8. Files Changed Summary

### Backend
| File | Change |
|---|---|
| `LifeSprint.Infrastructure/Services/ContainerService.cs` | Add `InstantiateRecurringItemsAsync`, `BuildStampedTitle`; call from `CreateNewContainerAsync` and `GetOrCreateCurrentContainerAsync` |
| `LifeSprint.Infrastructure/Services/ActivityService.cs` | Add `ReorderActivityAsync`; add `isRecurring`/`recurrenceType` filter to `GetActivitiesForUserAsync` |
| `LifeSprint.Core/Interfaces/IActivityService.cs` | Add `ReorderActivityAsync` signature; add filter params |
| `LifeSprint.Api/Controllers/ActivitiesController.cs` | Add `PATCH /{id}/reorder`; add `isRecurring`/`recurrenceType` query params to `GetActivities` |
| `LifeSprint.Tests/Integration/RecurringItemsIntegrationTests.cs` | New file |
| `LifeSprint.Tests/Integration/ContainerCrudIntegrationTests.cs` | New or extended file |

### Frontend
| File | Change |
|---|---|
| `hooks/useBacklog.ts` | Expose `reloadContainers` |
| `hooks/useRecurringItems.ts` | New hook |
| `components/Activities/NewContainerModal.tsx` | Pass created `Container` to `onCreated` |
| `components/Activities/ActivityList.tsx` | Add up/down arrows; add "Add Child" button; accept `onReorder`, `onAddChild` props |
| `components/Activities/ActivityEditor.tsx` | Accept `fixedIsRecurring`, `fixedRecurrenceType` props |
| `components/Activities/AddChildModal.tsx` | New component |
| `components/Activities/AddChildModal.module.css` | New styles |
| `components/Layout/Sidebar.tsx` | Add "Recurring Items" section with sub-links |
| `pages/AnnualBacklog.tsx` | Use `reloadContainers`, add reorder and add-child handlers |
| `pages/MonthlyBacklog.tsx` | Same |
| `pages/WeeklySprint.tsx` | Same |
| `pages/DailyChecklist.tsx` | Fix `defaultContainerType`; add reorder and add-child handlers |
| `pages/recurring/*.tsx` | Four new recurring pages |
| `router/index.tsx` | Add `/recurring/*` routes |
| `services/activityService.ts` | Add `reorderActivity`; add filters to `getActivities` |
| `e2e/containers.spec.ts` | New E2E |
| `e2e/daily-checklist.spec.ts` | New E2E |
| `e2e/annual-backlog.spec.ts` | New E2E |
| `e2e/monthly-backlog.spec.ts` | New E2E |
| `e2e/weekly-sprint.spec.ts` | New E2E |
| `e2e/recurring-items.spec.ts` | New E2E |
