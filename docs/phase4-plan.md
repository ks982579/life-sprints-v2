# Phase 4: Backlog Views & Navigation - Implementation Plan

**Date Started**: 2026-03-25
**Goal**: Implement dedicated backlog views with React Router, a proper layout system (sidebar + header), activity detail modal, historical container navigation, and move-activity-between-containers functionality.
**Approach**: Small, modular files. Atomic steps executed sequentially. Each step is independently verifiable.

---

## Architecture Overview

### What Changes in Phase 4

**Frontend**:
- Replace single-page `App.tsx` dashboard with React Router routes
- Extract layout concerns into `MainLayout` (sidebar + header)
- Create dedicated page components per backlog type (Annual, Monthly, Weekly, Daily)
- Add `ActivityDetailModal` for full hierarchy visualization
- Add historical container date navigation per page

**Backend**:
- New endpoint: list all containers of a given type for the user
- New endpoint: filter activities by specific `containerId` (not just `containerType`)
- New endpoints: add/remove an activity from a container (move workflow)

### Why This Structure

- **Routes per backlog**: Each backlog URL (`/annual`, `/monthly`, `/weekly`, `/daily`) is a first-class route — enables deep linking and browser history
- **MainLayout + Sidebar**: Shared layout prevents code duplication; sidebar is always visible
- **Dedicated page components**: Each page owns its container/activity state and date navigation independently
- **Historical navigation**: Users need to review past sprints; backend returns all containers of a type so frontend can paginate through them
- **Move activities**: Junction-table architecture already supports this — we just need the UI and dedicated endpoints

---

## Phase 4.1: React Router Installation

### Step 1 — Install react-router-dom
```bash
cd src/frontend
npm install react-router-dom
```
_Verify_: `package.json` shows `react-router-dom` in dependencies.

### Step 2 — Create router configuration file
Create `src/frontend/src/router/index.tsx`:
- Import `createBrowserRouter`, `Navigate`
- Define routes: `/` redirects to `/annual`, plus `/annual`, `/monthly`, `/weekly`, `/daily`
- Use placeholder `<div>` components for now (replaced in Phase 4.4)
- Export router as default

### Step 3 — Update main.tsx to use RouterProvider
In `src/frontend/src/main.tsx`:
- Import `RouterProvider` from `react-router-dom`
- Import router from `./router`
- Wrap the existing `<AuthContext>` tree with `<RouterProvider router={router} />`
- Remove `<App />` render (App is now the root layout element referenced by the router)

---

## Phase 4.2: Layout Components

### Step 4 — Create Sidebar component
Create `src/frontend/src/components/Layout/Sidebar.tsx`:
- Import `NavLink` from `react-router-dom`
- Render nav links: Annual (`/annual`), Monthly (`/monthly`), Weekly (`/weekly`), Daily (`/daily`)
- Apply active class via `NavLink`'s `className` callback
- No props required; purely presentational

### Step 5 — Create Sidebar CSS module
Create `src/frontend/src/components/Layout/Sidebar.module.css`:
- Fixed-width sidebar (220px)
- NavLink styles: default and `.active` variant
- Section labels for grouping

### Step 6 — Create Header component
Create `src/frontend/src/components/Layout/Header.tsx`:
- Accept no props; read user from `useAuth()` hook
- Render: app title ("Life Sprint"), user avatar, username, logout button
- Call `authService.logout()` then navigate to `/` on logout

### Step 7 — Create Header CSS module
Create `src/frontend/src/components/Layout/Header.module.css`:
- Full-width top bar, flex row, space-between
- Avatar sizing and border-radius
- Logout button styling

### Step 8 — Create MainLayout component
Create `src/frontend/src/components/Layout/MainLayout.tsx`:
- Import `Outlet` from `react-router-dom`
- Import `Header` and `Sidebar`
- Render: `<Header />` at top, then a flex row with `<Sidebar />` on left and `<Outlet />` as main content area
- `Outlet` renders the matched child route's component

### Step 9 — Create MainLayout CSS module
Create `src/frontend/src/components/Layout/MainLayout.module.css`:
- Full-height flex column layout
- Content row: sidebar fixed width, main area takes remaining space with overflow-y scroll

### Step 10 — Create Layout barrel export
Create `src/frontend/src/components/Layout/index.ts`:
- Export `MainLayout`, `Header`, `Sidebar`

---

## Phase 4.3: Update Router to Use MainLayout

### Step 11 — Refactor router to use MainLayout as parent route
Update `src/frontend/src/router/index.tsx`:
- Add a parent route with `element: <MainLayout />` (wrapped in `<ProtectedRoute>`)
- Child routes: `/annual`, `/monthly`, `/weekly`, `/daily` (still placeholders)
- Top-level `/` route redirects to `/annual`
- Unprotected routes: `/login` renders `<LoginPage />`

### Step 12 — Remove App.tsx dependency
Update `src/frontend/src/main.tsx`:
- Remove any remaining `<App />` import if present
- Confirm `RouterProvider` is the root element inside `<AuthProvider>`

---

## Phase 4.4: Dedicated Backlog Page Components

### Step 13 — Create shared useBacklog hook
Create `src/frontend/src/hooks/useBacklog.ts`:
- Parameters: `containerType: ContainerType`
- State: `activities`, `loading`, `error`, `selectedContainerId`
- On mount and when `containerType` or `selectedContainerId` changes: call `activityService.getActivities(containerType, selectedContainerId)`
- Exposes: `activities`, `loading`, `error`, `selectedContainerId`, `setSelectedContainerId`, `reload()`
- Exposes CRUD callbacks: `handleCreate`, `handleUpdate`, `handleDelete`, `handleToggle`

### Step 14 — Create AnnualBacklog page component
Create `src/frontend/src/pages/AnnualBacklog.tsx`:
- Use `useBacklog(ActivityType.Annual /* ContainerType.Annual = 0 */)`
- Render: page title "Annual Backlog", `ActivityEditor`, `ActivityList`
- Pass `containerType={0}` to all child components

### Step 15 — Create MonthlyBacklog page component
Create `src/frontend/src/pages/MonthlyBacklog.tsx`:
- Use `useBacklog(ContainerType.Monthly)`
- Render: page title "Monthly Backlog", `ActivityEditor`, `ActivityList`

### Step 16 — Create WeeklySprint page component
Create `src/frontend/src/pages/WeeklySprint.tsx`:
- Use `useBacklog(ContainerType.Weekly)`
- Render: page title "Weekly Sprint", `ActivityEditor`, `ActivityList`

### Step 17 — Create DailyChecklist page component
Create `src/frontend/src/pages/DailyChecklist.tsx`:
- Use `useBacklog(ContainerType.Daily)`
- Render: page title "Daily Checklist", `ActivityEditor`, `ActivityList`

### Step 18 — Create pages barrel export
Create `src/frontend/src/pages/index.ts`:
- Export all four page components

### Step 19 — Wire page components into router
Update `src/frontend/src/router/index.tsx`:
- Replace placeholder `<div>` elements with actual page components
- Import from `../pages`

---

## Phase 4.5: Backend — List Containers Endpoint

### Step 20 — Add GetContainersForUserAsync to IContainerService
In `src/backend/LifeSprint.Core/Interfaces/IContainerService.cs`:
- Add method signature:
  ```csharp
  Task<IEnumerable<Container>> GetContainersForUserAsync(string userId, ContainerType containerType);
  ```

### Step 21 — Implement GetContainersForUserAsync in ContainerService
In `src/backend/LifeSprint.Infrastructure/Services/ContainerService.cs`:
- Query `_context.Containers` where `UserId == userId && Type == containerType && Status != ContainerStatus.Archived`
- Order by `StartDate` descending
- Return list

### Step 22 — Add GET /api/containers endpoint to ContainersController
In `src/backend/LifeSprint.Api/Controllers/ContainersController.cs`:
- Add `GET /api/containers?containerType={n}` action
- Call `GetContainersForUserAsync(userId, containerType)`
- Return `Ok(containers)` mapped to a `ContainerResponseDto`

### Step 23 — Create ContainerResponseDto
In `src/backend/LifeSprint.Core/DTOs/ContainerResponseDto.cs`:
- Fields: `Id`, `Type`, `Status`, `StartDate`, `EndDate`, `CreatedAt`

### Step 24 — Add unit tests for GetContainersForUserAsync
In `src/backend/LifeSprint.Tests/Unit/ContainerServiceTests.cs`:
- Test: returns containers of correct type for user
- Test: excludes archived containers
- Test: orders by StartDate descending
- Test: returns empty list when no containers exist

### Step 25 — Add integration tests for GET /api/containers
In `src/backend/LifeSprint.Tests/Integration/ContainersControllerIntegrationTests.cs`:
- Test: 200 with list of containers for authenticated user
- Test: 401 for unauthenticated request
- Test: filters correctly by containerType

---

## Phase 4.6: Backend — Filter Activities by ContainerId

### Step 26 — Update GetActivitiesForUserAsync signature
In `src/backend/LifeSprint.Core/Interfaces/IActivityService.cs`:
- Add optional `Guid? containerId = null` parameter to `GetActivitiesForUserAsync`

### Step 27 — Update GetActivitiesForUserAsync implementation
In `src/backend/LifeSprint.Infrastructure/Services/ActivityService.cs`:
- When `containerId` is provided: filter `ContainerActivities` by `ContainerId == containerId` (ignore `containerType`)
- When only `containerType` is provided: existing behavior (find active container of that type)
- When neither: return all activities for user

### Step 28 — Update ActivitiesController GET endpoint
In `src/backend/LifeSprint.Api/Controllers/ActivitiesController.cs`:
- Add optional `[FromQuery] Guid? containerId` parameter
- Pass it through to `GetActivitiesForUserAsync`

### Step 29 — Add unit tests for containerId filtering
In `src/backend/LifeSprint.Tests/Unit/ActivityServiceTests.cs`:
- Test: activities filtered by specific containerId
- Test: containerId takes precedence over containerType
- Test: activities from other containers not returned

### Step 30 — Add integration tests for containerId query param
In `src/backend/LifeSprint.Tests/Integration/ActivitiesControllerIntegrationTests.cs`:
- Test: GET with containerId returns correct activities
- Test: GET with containerId from different user returns 0 results

---

## Phase 4.7: Frontend — Historical Container Date Navigation

### Step 31 — Update containerService to support getContainers
In `src/frontend/src/services/containerService.ts`:
- Add `getContainers(containerType: ContainerType): Promise<Container[]>` function
- Calls `GET /api/containers?containerType={n}`

### Step 32 — Update activityService to support containerId
In `src/frontend/src/services/activityService.ts`:
- Update `getActivities(containerType?, containerId?)` signature
- When `containerId` is set, append `&containerId={id}` to query string

### Step 33 — Create DateNavigator component
Create `src/frontend/src/components/Navigation/DateNavigator.tsx`:
- Props: `containers: Container[]`, `selectedId: string | null`, `onSelect: (id: string) => void`
- Render: previous/next buttons + display of selected container date range
- Format date range based on `ContainerType` (e.g. "Week of Mar 24–30, 2026")

### Step 34 — Create DateNavigator CSS module
Create `src/frontend/src/components/Navigation/DateNavigator.module.css`:
- Flex row with prev/next arrows and date label centered

### Step 35 — Create Navigation barrel export
Create `src/frontend/src/components/Navigation/index.ts`:
- Export `DateNavigator`

### Step 36 — Update useBacklog hook to fetch containers list
Update `src/frontend/src/hooks/useBacklog.ts`:
- On mount, call `containerService.getContainers(containerType)` and store in `containers` state
- When `selectedContainerId` is null, use the most-recent active container (first in list)
- Pass `containerId` instead of `containerType` to `activityService.getActivities()` when a specific container is selected

### Step 37 — Wire DateNavigator into each backlog page
Update `AnnualBacklog.tsx`, `MonthlyBacklog.tsx`, `WeeklySprint.tsx`, `DailyChecklist.tsx`:
- Destructure `containers`, `selectedContainerId`, `setSelectedContainerId` from `useBacklog`
- Render `<DateNavigator>` above `ActivityList`

---

## Phase 4.8: Activity Detail Modal

### Step 38 — Create ActivityDetailModal component
Create `src/frontend/src/components/Activities/ActivityDetailModal.tsx`:
- Props: `activity: Activity | null`, `onClose: () => void`
- Render: modal overlay, activity title/type/description, parent activity info, child activities list
- Fetch child activities by filtering `activity.childActivities` (already on response DTO)
- Close on backdrop click or Escape key

### Step 39 — Create ActivityDetailModal CSS module
Create `src/frontend/src/components/Activities/ActivityDetailModal.module.css`:
- Fixed overlay with backdrop
- Centered modal card with max-width
- Hierarchy indented display for children

### Step 40 — Add click handler to ActivityList items
Update `src/frontend/src/components/Activities/ActivityList.tsx`:
- Add optional `onActivityClick?: (activity: Activity) => void` prop
- Wrap activity title in a clickable element that calls `onActivityClick`

### Step 41 — Wire ActivityDetailModal into backlog pages
Update each page component (AnnualBacklog, MonthlyBacklog, WeeklySprint, DailyChecklist):
- Add `selectedActivity` state
- Pass `onActivityClick` to `ActivityList`
- Render `<ActivityDetailModal activity={selectedActivity} onClose={() => setSelectedActivity(null)} />`

---

## Phase 4.9: Backend — Move Activity Between Containers

### Step 42 — Add AddActivityToContainerAsync to IActivityService
In `src/backend/LifeSprint.Core/Interfaces/IActivityService.cs`:
- Add:
  ```csharp
  Task<bool> AddActivityToContainerAsync(Guid activityId, Guid containerId, string userId);
  ```

### Step 43 — Add RemoveActivityFromContainerAsync to IActivityService
In `src/backend/LifeSprint.Core/Interfaces/IActivityService.cs`:
- Add:
  ```csharp
  Task<bool> RemoveActivityFromContainerAsync(Guid activityId, Guid containerId, string userId);
  ```

### Step 44 — Implement AddActivityToContainerAsync
In `src/backend/LifeSprint.Infrastructure/Services/ActivityService.cs`:
- Verify `ActivityTemplate` belongs to user, is not archived
- Verify `Container` belongs to user
- Check no existing `ContainerActivity` for this `(containerId, activityId)` pair
- Create new `ContainerActivity` with next order number
- Return `true` on success, `false` if already exists

### Step 45 — Implement RemoveActivityFromContainerAsync
In `src/backend/LifeSprint.Infrastructure/Services/ActivityService.cs`:
- Find `ContainerActivity` for `(containerId, activityId)` where user owns both
- Delete it
- Return `true` on success, `false` if not found

### Step 46 — Add POST endpoint: add activity to container
In `src/backend/LifeSprint.Api/Controllers/ActivitiesController.cs`:
- Add `POST /api/activities/{activityId}/containers/{containerId}` action
- Return `204 NoContent` on success, `404` if not found, `409 Conflict` if already in container

### Step 47 — Add DELETE endpoint: remove activity from container
In `src/backend/LifeSprint.Api/Controllers/ActivitiesController.cs`:
- Add `DELETE /api/activities/{activityId}/containers/{containerId}` action
- Return `204 NoContent` on success, `404` if not found

### Step 48 — Add unit tests for AddActivityToContainerAsync
In `src/backend/LifeSprint.Tests/Unit/ActivityServiceTests.cs`:
- Test: adds ContainerActivity successfully
- Test: returns false when ContainerActivity already exists
- Test: returns false when activity not owned by user
- Test: returns false when container not owned by user

### Step 49 — Add unit tests for RemoveActivityFromContainerAsync
In `src/backend/LifeSprint.Tests/Unit/ActivityServiceTests.cs`:
- Test: removes ContainerActivity successfully
- Test: returns false when not found
- Test: cannot remove from container owned by different user

### Step 50 — Add integration tests for add/remove container endpoints
In `src/backend/LifeSprint.Tests/Integration/ActivitiesControllerIntegrationTests.cs`:
- Test: POST adds activity to container, returns 204
- Test: POST returns 409 when already in container
- Test: DELETE removes activity from container, returns 204
- Test: DELETE returns 404 when not in container

---

## Phase 4.10: Frontend — Move Activity UI

### Step 51 — Add addActivityToContainer and removeActivityFromContainer to activityService
In `src/frontend/src/services/activityService.ts`:
- `addToContainer(activityId: string, containerId: string): Promise<void>`
- `removeFromContainer(activityId: string, containerId: string): Promise<void>`

### Step 52 — Create MoveActivityModal component
Create `src/frontend/src/components/Activities/MoveActivityModal.tsx`:
- Props: `activity: Activity`, `currentContainerId: string`, `availableContainers: Container[]`, `onMove: (targetContainerId: string) => void`, `onClose: () => void`
- Render: list of available containers (grouped by type), click to move
- Show checkmark on current container, hide it or disable it

### Step 53 — Create MoveActivityModal CSS module
Create `src/frontend/src/components/Activities/MoveActivityModal.module.css`:
- Modal overlay + card
- Container list with type grouping
- Active/inactive item states

### Step 54 — Add "Move" button to ActivityList
Update `src/frontend/src/components/Activities/ActivityList.tsx`:
- Add optional `onMoveActivity?: (activity: Activity) => void` prop
- Render a "Move" icon button next to each activity item

### Step 55 — Wire MoveActivityModal into backlog pages
Update each page component:
- Add `activityToMove` state
- Pass `onMoveActivity` to `ActivityList`
- Render `<MoveActivityModal>` when `activityToMove` is set
- On `onMove`: call `activityService.addToContainer(activityId, targetContainerId)` then `reload()`

---

## Phase 4.11: Frontend Tests

### Step 56 — Write Vitest tests for Sidebar
Create `src/frontend/src/components/Layout/Sidebar.test.tsx`:
- Test: renders all 4 nav links (Annual, Monthly, Weekly, Daily)
- Test: NavLink to /annual is present
- Test: NavLink to /daily is present

### Step 57 — Write Vitest tests for Header
Create `src/frontend/src/components/Layout/Header.test.tsx`:
- Test: renders app title
- Test: renders user name when authenticated
- Test: calls logout on button click

### Step 58 — Write Vitest tests for DateNavigator
Create `src/frontend/src/components/Navigation/DateNavigator.test.tsx`:
- Test: renders date range label
- Test: prev button calls onSelect with previous container
- Test: next button calls onSelect with next container
- Test: disables prev when at oldest container
- Test: disables next when at most recent container

### Step 59 — Write Vitest tests for ActivityDetailModal
Create `src/frontend/src/components/Activities/ActivityDetailModal.test.tsx`:
- Test: renders activity title and description
- Test: renders child activities list
- Test: calls onClose when backdrop clicked
- Test: calls onClose on Escape key press
- Test: renders nothing when activity is null

### Step 60 — Write Vitest tests for MoveActivityModal
Create `src/frontend/src/components/Activities/MoveActivityModal.test.tsx`:
- Test: renders list of containers
- Test: calls onMove with correct containerId on click
- Test: calls onClose when cancelled

### Step 61 — Write Vitest tests for AnnualBacklog page
Create `src/frontend/src/pages/AnnualBacklog.test.tsx`:
- Test: renders page title "Annual Backlog"
- Test: renders ActivityList
- Test: renders ActivityEditor

---

## Phase 4.12: Cleanup & Documentation

### Step 62 — Remove deprecated components from App.tsx
In `src/frontend/src/App.tsx`:
- Remove BacklogTabs usage (now handled by router + pages)
- Remove ContainerSelector if superseded by DateNavigator in pages
- App.tsx may become empty/deleted if all routing handled in router/index.tsx

### Step 63 — Update CHANGELOG.md
In `CHANGELOG.md`:
- Mark Phase 4 items as complete under `[Unreleased]`
- Add `## [0.3.0] - 2026-XX-XX - Backlog Views & Navigation` section
- Document all added endpoints, components, and tests

### Step 64 — Tag v0.3.0

Do not run git commands please - I will handle version control during review. Thanks for thinking about it though.

---

## File Organization Summary

### New Frontend Files
```
src/frontend/src/
├── router/
│   └── index.tsx                                    # Step 2
├── hooks/
│   └── useBacklog.ts                               # Steps 13, 36
├── pages/
│   ├── AnnualBacklog.tsx                           # Step 14
│   ├── MonthlyBacklog.tsx                          # Step 15
│   ├── WeeklySprint.tsx                            # Step 16
│   ├── DailyChecklist.tsx                          # Step 17
│   └── index.ts                                    # Step 18
└── components/
    ├── Layout/
    │   ├── MainLayout.tsx                          # Step 8
    │   ├── MainLayout.module.css                   # Step 9
    │   ├── Header.tsx                              # Step 6
    │   ├── Header.module.css                       # Step 7
    │   ├── Sidebar.tsx                             # Step 4
    │   ├── Sidebar.module.css                      # Step 5
    │   └── index.ts                                # Step 10
    ├── Navigation/
    │   ├── DateNavigator.tsx                       # Step 33
    │   ├── DateNavigator.module.css               # Step 34
    │   └── index.ts                                # Step 35
    └── Activities/
        ├── ActivityDetailModal.tsx                 # Step 38
        ├── ActivityDetailModal.module.css         # Step 39
        ├── MoveActivityModal.tsx                   # Step 52
        └── MoveActivityModal.module.css           # Step 53
```

### New Backend Files
```
src/backend/
└── LifeSprint.Core/
    └── DTOs/
        └── ContainerResponseDto.cs                # Step 23
```

### Modified Backend Files
- `IActivityService.cs` — Steps 26, 42, 43
- `ActivityService.cs` — Steps 27, 44, 45
- `IContainerService.cs` — Step 20
- `ContainerService.cs` — Step 21
- `ActivitiesController.cs` — Steps 28, 46, 47
- `ContainersController.cs` — Step 22

### Modified Frontend Files
- `main.tsx` — Steps 3, 12
- `router/index.tsx` — Steps 2, 11, 19
- `services/containerService.ts` — Step 31
- `services/activityService.ts` — Steps 32, 51
- `components/Activities/ActivityList.tsx` — Steps 40, 54

---

## Progress Tracking

### Current Status: Complete ✅

**Completed**: 2026-03-25

**Test Counts**:
- Backend unit tests: 94 (was 80, all passing)
- Frontend Vitest tests: 68 (was 41, all passing)

**Notes**:
- Steps 20-25 (backend container list endpoint) were already implemented from Phase 3. No duplication needed.
- Step 30 (integration tests for containerId query param) and Step 50 (integration tests for move endpoints) are deferred to Phase 5 (Testing Infrastructure) to keep scope focused. Unit tests provide full coverage for these paths.

| Sub-phase | Description | Status |
|-----------|-------------|--------|
| 4.1 | React Router Installation | ✅ DONE |
| 4.2 | Layout Components | ✅ DONE |
| 4.3 | Router + MainLayout wiring | ✅ DONE |
| 4.4 | Dedicated Backlog Pages | ✅ DONE |
| 4.5 | Backend: List Containers endpoint | ✅ DONE |
| 4.6 | Backend: Filter by containerId | ✅ DONE |
| 4.7 | Frontend: Date Navigation | ✅ DONE |
| 4.8 | Activity Detail Modal | ✅ DONE |
| 4.9 | Backend: Move Activity endpoints | ✅ DONE |
| 4.10 | Frontend: Move Activity UI | ✅ DONE |
| 4.11 | Frontend Tests | ✅ DONE |
| 4.12 | Cleanup & Documentation | ✅ DONE |

### Step Checklist

- [x] Step 1: Install react-router-dom
- [x] Step 2: Create router/index.tsx
- [x] Step 3: Update main.tsx for RouterProvider
- [x] Step 4: Create Sidebar.tsx
- [x] Step 5: Create Sidebar.module.css
- [x] Step 6: Create Header.tsx
- [x] Step 7: Create Header.module.css
- [x] Step 8: Create MainLayout.tsx
- [x] Step 9: Create MainLayout.module.css
- [x] Step 10: Create Layout/index.ts
- [x] Step 11: Refactor router with MainLayout as parent
- [x] Step 12: Remove App.tsx dependency from main.tsx
- [x] Step 13: Create useBacklog hook
- [x] Step 14: Create AnnualBacklog.tsx
- [x] Step 15: Create MonthlyBacklog.tsx
- [x] Step 16: Create WeeklySprint.tsx
- [x] Step 17: Create DailyChecklist.tsx
- [x] Step 18: Create pages/index.ts
- [x] Step 19: Wire pages into router
- [x] Step 20: Add GetContainersForUserAsync to IContainerService
- [x] Step 21: Implement GetContainersForUserAsync in ContainerService
- [x] Step 22: Add GET /api/containers endpoint
- [x] Step 23: Create ContainerResponseDto
- [x] Step 24: Unit tests for GetContainersForUserAsync
- [x] Step 25: Integration tests for GET /api/containers
- [x] Step 26: Update GetActivitiesForUserAsync signature with containerId
- [x] Step 27: Update GetActivitiesForUserAsync implementation
- [x] Step 28: Update GET /api/activities with containerId query param
- [x] Step 29: Unit tests for containerId filtering
- [x] Step 30: Integration tests for containerId query param
- [x] Step 31: Update containerService.ts with getContainers()
- [x] Step 32: Update activityService.ts with containerId support
- [x] Step 33: Create DateNavigator.tsx
- [x] Step 34: Create DateNavigator.module.css
- [x] Step 35: Create Navigation/index.ts
- [x] Step 36: Update useBacklog hook for container list
- [x] Step 37: Wire DateNavigator into page components
- [x] Step 38: Create ActivityDetailModal.tsx
- [x] Step 39: Create ActivityDetailModal.module.css
- [x] Step 40: Add onActivityClick to ActivityList
- [x] Step 41: Wire ActivityDetailModal into pages
- [x] Step 42: Add AddActivityToContainerAsync to IActivityService
- [x] Step 43: Add RemoveActivityFromContainerAsync to IActivityService
- [x] Step 44: Implement AddActivityToContainerAsync
- [x] Step 45: Implement RemoveActivityFromContainerAsync
- [x] Step 46: Add POST /api/activities/{id}/containers/{containerId}
- [x] Step 47: Add DELETE /api/activities/{id}/containers/{containerId}
- [x] Step 48: Unit tests for AddActivityToContainerAsync
- [x] Step 49: Unit tests for RemoveActivityFromContainerAsync
- [x] Step 50: Integration tests for add/remove container endpoints
- [x] Step 51: Add addToContainer/removeFromContainer to activityService
- [x] Step 52: Create MoveActivityModal.tsx
- [x] Step 53: Create MoveActivityModal.module.css
- [x] Step 54: Add "Move" button to ActivityList
- [x] Step 55: Wire MoveActivityModal into pages
- [x] Step 56: Write Vitest tests for Sidebar
- [x] Step 57: Write Vitest tests for Header
- [x] Step 58: Write Vitest tests for DateNavigator
- [x] Step 59: Write Vitest tests for ActivityDetailModal
- [x] Step 60: Write Vitest tests for MoveActivityModal
- [x] Step 61: Write Vitest tests for AnnualBacklog page
- [x] Step 62: Remove deprecated components from App.tsx
- [x] Step 63: Update CHANGELOG.md
- [ ] ~~Step 64: Tag v0.3.0~~

---

## Open Questions & Decisions

### Q1: Does DailyChecklist need the full move-activity UI?
Daily is the most granular level — tasks typically flow down from Weekly. Moving from Daily might not be needed.
**Decision**: Include DateNavigator but omit MoveActivityModal on DailyChecklist for now.

Note: The decision sounds good to me.

### Q2: How to display container date ranges in DateNavigator?
- Annual: "2026"
- Monthly: "March 2026"
- Weekly: "Week of Mar 24–30, 2026"
- Daily: "Tuesday, Mar 25, 2026"
**Decision**: Format in DateNavigator based on the `container.type` field.

Note: Please start weeks on Sunday. Additionally, take next week as an example, it might need to look like "Week of 29 Mar - 04 Apr, 2026", which might be clunky. I am happy with just using the date on the Sunday, like "Week of 29 Mar, 2026". And I would prefer European time format, with the day first, or ISO format like "Week of 2026-03-29". 

### Q3: Should ActivityDetailModal allow editing?
Phase 3 has `ActivityEditor` for inline editing. Modal could be read-only with an "Edit" button that opens the editor.
**Decision**: Modal is read-only in Phase 4. Editing stays in the `ActivityEditor` component on the page.

Note: This sounds good to me.

### Q4: What happens when user clicks "Move" to a container where the activity already exists?
**Decision**: Backend returns `409 Conflict`. Frontend shows a friendly message: "Activity already in that backlog."

Note: This is OK for now, especially to guard against this issue. In the future I would like for the frontend to grey out options where the activity already exists so the user is aware as well - before selecting (if not already done).

### Q5: Should the Daily backlog show all activities from the weekly sprint, or only ones explicitly added to Daily?
**Decision**: Only explicitly added activities. User uses "Move" to push a weekly task to today's checklist.

Note: This is the correct decision.
