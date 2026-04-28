# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## Standing Instructions for Claude

**ALWAYS update this file and the frontend README after making code changes.** This includes:
- New features: describe the behavior, which files changed, and any new API endpoints
- Bug fixes: note what was wrong and what the correct behavior is now
- Architectural decisions: explain why, not just what
- New pitfalls discovered: add them to the Common Pitfalls section

If a code change makes any section of this file inaccurate, fix the inaccuracy in the same session.

---

## Project Overview

Life Sprint is a personal sprint planning and task management app. The core idea is Obsidian-style hierarchical todo tracking — but with linked data so the same item appears across multiple time horizons without manual duplication.

**User workflow:**
- Add goals and tasks to the **Annual Backlog** (the master list for the year)
- Pull relevant items into the **Monthly Backlog** when planning a month
- Pull items from Monthly into the **Weekly Sprint** when planning a week
- Pull items from Weekly into the **Daily Checklist** for today's focus
- Checking an item complete in any backlog marks it complete everywhere
- At the start of a new week/month/year, click "New Sprint / New Month / New Year" to open the new container, with the option to roll over incomplete items automatically

**Tech Stack**: .NET 10, ASP.NET Core, EF Core 10, PostgreSQL 16, React 19, TypeScript, Vite, Docker

---

## Terminology

Getting this right prevents major confusion. There are two layers: what users see and what the code uses.

### User-Facing Terms (UI labels)

| UI Label | What it is |
|---|---|
| Annual Backlog | A Container of type Annual scoped to a calendar year |
| Monthly Backlog | A Container of type Monthly scoped to a calendar month |
| Weekly Sprint | A Container of type Weekly scoped to a Mon–Sun week |
| Daily Checklist | A Container of type Daily scoped to a single day |
| Todo item (or just "item") | An ActivityTemplate — the master record for a task or goal |
| Add to backlog | Creating a ContainerActivity link between an item and a container |
| Roll over | Carrying incomplete items from a previous container into a new one |

### Developer Terms (code and database)

| Term | Meaning |
|---|---|
| `Container` | One time-scoped backlog instance (e.g., "Week of 2026-03-23") |
| `ContainerType` | Enum: Annual(0) / Monthly(1) / Weekly(2) / Daily(3) |
| `ContainerStatus` | Enum: Active(0) / Completed(1) / Archived(2) |
| `ActivityTemplate` | The master record for a todo item or goal. Independent of any backlog. |
| `ContainerActivity` | Junction record linking one `ActivityTemplate` to one `Container`. Tracks per-container state. |
| `IsRolledOver` | Flag on `ContainerActivity` — true if the item was carried forward from a previous container |
| `ArchivedAt` | Soft-delete timestamp on `ActivityTemplate`. Archived items are hidden from all queries. |
| `DefaultContainerType` | Field on `CreateActivityDto` — the fallback container type when no `ContainerId` is provided |
| `SkipContainerLink` | Field on `CreateActivityDto` — when `true`, no `ContainerActivity` is created; used for recurring templates that should not belong to any container |
| Recurring template | An `ActivityTemplate` with `IsRecurring=true` and a `RecurrenceType` set. Has no `ContainerActivity` records. Auto-instantiates as a concrete copy (with a stamped title) when a matching container is created. |
| Concrete instance | An `ActivityTemplate` created by instantiating a recurring template. Has a stamped title (e.g., "Pay Bills \| April 2026") and normal `ContainerActivity` links. |

---

## Core Domain Architecture

### The Container + Junction Table Pattern

This is the most important concept in the codebase. **Do not simplify it away.**

```
ActivityTemplate  (one per todo item)
       │
       │  many ContainerActivity records
       │
       ▼
ContainerActivity  ← junction: (ContainerId, ActivityTemplateId)
       │               also stores: CompletedAt, Order, IsRolledOver
       │
       ▼
Container  (one per time period, e.g. "March 2026 Monthly")
```

A single `ActivityTemplate` can have `ContainerActivity` records pointing to many containers simultaneously — Annual, Monthly, and Weekly all at once. This is what enables an item to appear in all three backlogs without being duplicated in the database.

**Why not boolean flags?** Do NOT add `InAnnualBacklog`, `InMonthlyBacklog`, etc. to `ActivityTemplate`. That approach:
- Cannot represent historical containers (which March did this appear in?)
- Cannot show the same item in both Annual and Weekly simultaneously
- Cannot track rollover history
- Makes completion tracking per-backlog impossible

### Auto-Propagation Up the Hierarchy

When an item is added to any container — either at creation time or via the "Add to Backlog" button — it is **automatically also added to all higher-level containers** for the current period:

- Adding to **Daily** → also adds to Weekly, Monthly, Annual
- Adding to **Weekly** → also adds to Monthly, Annual
- Adding to **Monthly** → also adds to Annual
- Adding to **Annual** → no further propagation

This is enforced in `ActivityService.CreateActivityAsync` and `ActivityService.AddActivityToContainerAsync` via `GetParentContainerTypes()`.

**Exception**: recurring templates pass `SkipContainerLink=true`, so no propagation occurs. Concrete instances created from recurring templates do auto-propagate normally.

### Shared Completion State

Toggling an item's completion checkbox in any backlog marks it complete (or incomplete) **in all backlogs simultaneously**. This is handled in `ActivityService.ToggleActivityCompletionAsync`, which updates every `ContainerActivity` record for that item owned by the user.

### Activity Type Hierarchy

`ActivityTemplate.Type` enforces a strict parent-child relationship:

| Type | Allowed Parents |
|---|---|
| Project | None (top-level) |
| Epic | Project |
| Story | Epic or Project |
| Task | Story or Epic |

Validated in `ActivityService.ValidateHierarchy()`. All four types must remain in the dictionary — `Project` with an empty allowed-parents list.

---

## Container Lifecycle

### How Containers Are Created

Containers are **not created automatically on login**. They are created in two ways:

1. **Implicitly**, when a user creates a new todo item and no container exists yet for the current period. `ActivityService.CreateActivityAsync` calls `ContainerService.GetOrCreateCurrentContainerAsync()` which creates the container if needed.

2. **Explicitly**, via the "New Sprint / New Month / New Year" button on each backlog page. This calls `POST /api/containers/new` with a `rolloverIncomplete` flag. The backend checks whether a container for the current period already exists:
   - If it **does** → returns `409 Conflict` (the UI shows a warning in the modal)
   - If it **doesn't** → creates the container, optionally rolling over incomplete items from the most recent previous container of the same type

After creating a container (either path), `ContainerService.InstantiateRecurringItemsAsync` fires automatically. It finds all recurring templates for the user whose `RecurrenceType` matches the new container's `ContainerType`, then creates concrete copies (with stamped titles) linked to the new container with auto-propagation upward. Parent templates are instantiated before children via topological sort.

### Container Date Ranges

Calculated by `ContainerService.GetDateRangeForType()`:

| Type | Range |
|---|---|
| Annual | Jan 1 – Dec 31 of the current year |
| Monthly | 1st – last day of the current month |
| Weekly | Monday – Sunday of the current ISO week |
| Daily | A single day (same start and end) |

All dates are stored as UTC (`DateTimeKind.Utc`). Never use `DateTime.Unspecified` — PostgreSQL will reject it.

---

## API Reference

### Activities

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/activities` | Get all items for the user. `?containerType=N` or `?containerId=N` to filter; `?isRecurring=true` or `?recurrenceType=N` for recurring template queries |
| GET | `/api/activities/{id}` | Get one item (404 if archived) |
| POST | `/api/activities` | Create a new item. Body: `CreateActivityDto` |
| PUT | `/api/activities/{id}` | Update item metadata |
| DELETE | `/api/activities/{id}` | Soft-delete (archive) an item |
| PATCH | `/api/activities/{id}/complete` | Toggle completion in a specific container (updates all containers) |
| PATCH | `/api/activities/{id}/reorder` | Move item up or down within a container. Body: `{ containerId, direction: "up"\|"down" }`. Returns 204. |
| POST | `/api/activities/{id}/containers/{containerId}` | Add item to an additional container (propagates upward) |
| DELETE | `/api/activities/{id}/containers/{containerId}` | Remove item from a specific container |

**`CreateActivityDto` fields:**
- `title` (required), `description`, `type` (required), `parentActivityId`
- `isRecurring`, `recurrenceType`
- `containerId` — specific container to add to (takes precedence)
- `defaultContainerType` — fallback container type when `containerId` is null; defaults to Annual if omitted
- `skipContainerLink` — when `true`, no `ContainerActivity` is created (used for recurring templates)

### Containers

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/containers` | Get all containers. `?type=N` to filter by type |
| GET | `/api/containers/{id}` | Get one container |
| POST | `/api/containers/new` | Create a new container for the current period |
| PATCH | `/api/containers/{id}/status` | Update container status |

**`POST /api/containers/new` body:**
- `type` (required) — ContainerType enum value
- `rolloverIncomplete` — if true, copies incomplete items from the previous container of the same type

Returns `409 Conflict` if a container already exists for the current period.

### Auth

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/auth/github/login` | Redirect to GitHub OAuth |
| GET | `/api/auth/github/callback` | GitHub callback, sets session cookie |
| GET | `/api/auth/me` | Get current user info |
| POST | `/api/auth/logout` | Clear session |
| POST | `/api/auth/test-login` | Dev/test only — create session without GitHub OAuth. Body: `{ username, email?, avatarUrl? }`. Used by Playwright E2E tests. |

---

## Frontend Architecture

### Pages and Their Backlog Types

| Page component | ContainerType | "New" button label | Notes |
|---|---|---|---|
| `AnnualBacklog.tsx` | Annual (0) | New Year | |
| `MonthlyBacklog.tsx` | Monthly (1) | New Month | |
| `WeeklySprint.tsx` | Weekly (2) | New Sprint | |
| `DailyChecklist.tsx` | Daily (3) | — | No `NewContainerModal` or `MoveActivityModal` — daily containers are created implicitly |

All four pages use the `useBacklog(containerType)` hook. The annual, monthly, and weekly pages fetch all containers for `MoveActivityModal`; the daily page omits this since items flow down into daily, not out. All four pass `hideRecurring` to `ActivityEditor` so recurring fields are hidden in backlog pages.

### Recurring Item Pages

| Page component | RecurrenceType | Route |
|---|---|---|
| `pages/recurring/AnnualRecurring.tsx` | Annual (0) | `/recurring/annual` |
| `pages/recurring/MonthlyRecurring.tsx` | Monthly (1) | `/recurring/monthly` |
| `pages/recurring/WeeklyRecurring.tsx` | Weekly (2) | `/recurring/weekly` |
| `pages/recurring/DailyRecurring.tsx` | Daily (3) | `/recurring/daily` |

Recurring pages use `useRecurringItems(recurrenceType)` instead of `useBacklog`. They render `ActivityList` without a `containerType` prop (because recurring templates have no container links) and `ActivityEditor` with `fixedIsRecurring={true}` and the fixed `recurrenceType` so users cannot change these fields. All creates pass `skipContainerLink: true`.

### Key Components

| Component | Purpose |
|---|---|
| `useBacklog(type)` | Custom hook: loads containers + activities, exposes CRUD callbacks + `reloadContainers` |
| `useRecurringItems(recurrenceType)` | Custom hook for recurring pages: fetches templates with `isRecurring: true`, always passes `skipContainerLink: true` on create |
| `DateNavigator` | Lets user navigate to historical containers of the same type |
| `ActivityEditor` | Form for creating/editing an item; `hideRecurring` hides recurring fields; `fixedIsRecurring`/`fixedRecurrenceType` lock those fields |
| `ActivityList` | Renders items with completion checkbox, reorder (▲/▼), edit/move/delete, and "Add" (child) actions. `containerType` is optional — omit for recurring pages |
| `AddChildModal` | Modal to add a child item to a Project/Epic/Story; maps parent type to correct child type (Project→Epic, Epic→Story, Story→Task) |
| `MoveActivityModal` | "Add to Backlog" — shows all non-archived containers grouped by type |
| `NewContainerModal` | "New Sprint / Month / Year" — rollover choice, calls `POST /containers/new`; `onCreated` callback now receives the created `Container` object |
| `ActivityDetailModal` | Read-only detail view |
| `ContainerSelector` | Dropdown to switch between containers of the same type (used within pages) |

### The `useBacklog` Hook

Manages state for a single backlog page:
- Loads all containers of the given type (for `DateNavigator`)
- Loads activities filtered by `selectedContainerId` (or by `containerType` when no container is selected)
- Exposes `handleCreate`, `handleUpdate`, `handleDelete`, `handleToggle`, `reload`, `reloadContainers`

`selectedContainerId` starts as `undefined`. Each page passes `defaultContainerType` in create calls so the backend resolves the right container even before the user has navigated to a specific container.

**Important**: `reload` only reloads activities; `reloadContainers` reloads the containers list. After `NewContainerModal` calls `onCreated(newContainer)`, pages must call `reloadContainers()` → `reload()` → `setSelectedContainerId(newContainer.id)` to update `DateNavigator` without a page reload.

### All-Containers for the Move Modal

Each page independently fetches all containers (no type filter) on mount:
```typescript
useEffect(() => {
  containerService.getContainers().then(setAllContainers);
}, []);
```
This is passed to `MoveActivityModal` so the user can add an item from any backlog to any other backlog. `MoveActivityModal` groups the list by type (Annual → Monthly → Weekly → Daily).

### TypeScript Enum Pattern

Due to `verbatimModuleSyntax` and `erasableSyntaxOnly` in tsconfig, enums use a const-object pattern:

```typescript
export type ContainerType = 0 | 1 | 2 | 3;
export const ContainerType = {
  Annual:  0 as ContainerType,
  Monthly: 1 as ContainerType,
  Weekly:  2 as ContainerType,
  Daily:   3 as ContainerType,
} as const;
```

Always import with `type` for pure type usage:
```typescript
import { ContainerType, type Activity } from '../types';
```

---

## Essential Commands

### Backend (.NET 10)

```bash
cd src/backend

dotnet restore
dotnet build

# Run API locally (not needed in Docker workflow)
dotnet run --project LifeSprint.Api

# Tests
dotnet test                                    # All tests
dotnet test --filter "Category=Unit"          # Unit only (in-memory DB)
dotnet test --filter "Category=Integration"   # Integration (requires test-db on port 5433)

# Migrations
dotnet ef migrations add MigrationName \
  --project LifeSprint.Infrastructure/LifeSprint.Infrastructure.csproj \
  --startup-project LifeSprint.Api/LifeSprint.Api.csproj

dotnet ef database update \
  --project LifeSprint.Infrastructure/LifeSprint.Infrastructure.csproj \
  --startup-project LifeSprint.Api/LifeSprint.Api.csproj
```

### Frontend (React 19 + Vite)

```bash
cd src/frontend

npm install
npm run dev              # Dev server on port 3000
npm run build            # Production build (also runs tsc)
npm run lint             # ESLint
npm test                 # Vitest unit tests (run once)
npm run test:watch       # Vitest in watch mode
npm run test:e2e         # Playwright E2E (requires docker compose up first)
npm run test:e2e:ui      # Playwright with interactive UI
npm run test:e2e:headed  # Playwright with visible browser
```

**E2E prerequisite**: Playwright tests hit `http://localhost` (the NGINX proxy). Run `docker compose up -d` from the repo root before running any E2E tests locally. In CI the webServer config in `playwright.config.ts` starts Docker automatically.

### Docker Compose

```bash
docker compose up           # Start everything
docker compose up -d        # Detached
docker compose logs -f backend
docker compose restart backend   # After backend code changes
docker compose down
docker compose down -v      # Also wipe database volume
```

**Service URLs:**
- Frontend: http://localhost:3000
- Backend API: http://localhost:5000
- NGINX (proxies both): http://localhost
- PostgreSQL: localhost:5432
- Test DB: localhost:5433

The Docker backend startup sequence:
1. Install dotnet-ef globally
2. `dotnet restore` + `dotnet build`
3. Wait for DB health check
4. Apply EF migrations
5. `dotnet watch` (hot reload)

The explicit build before migrations avoids "Unable to retrieve project metadata" errors.

---

## Project Structure

```
src/
├── backend/
│   ├── LifeSprint.Api/
│   │   └── Controllers/         # ActivitiesController, ContainersController, AuthController
│   ├── LifeSprint.Core/
│   │   ├── Models/              # ActivityTemplate, Container, ContainerActivity
│   │   ├── Interfaces/          # IActivityService, IContainerService
│   │   ├── DTOs/                # CreateActivityDto, ContainerResponseDto, CreateNewContainerDto, …
│   │   └── Enums/               # ActivityType, ContainerType, RecurrenceType, ContainerStatus
│   ├── LifeSprint.Infrastructure/
│   │   ├── Data/                # AppDbContext, EF migrations
│   │   └── Services/            # ActivityService, ContainerService
│   └── LifeSprint.Tests/
│       ├── Unit/                # In-memory EF Core tests
│       └── Integration/         # Real PostgreSQL tests (IntegrationTestBase)
│
└── frontend/
    └── src/
        ├── components/
        │   ├── Activities/      # ActivityList, ActivityEditor, ActivityDetailModal,
        │   │                    # MoveActivityModal, NewContainerModal, AddChildModal, BacklogTabs
        │   ├── Navigation/      # DateNavigator
        │   └── Auth/            # LoginPage, ProtectedRoute
        ├── hooks/               # useBacklog, useRecurringItems
        ├── pages/               # AnnualBacklog, MonthlyBacklog, WeeklySprint, DailyChecklist
        │   └── recurring/       # AnnualRecurring, MonthlyRecurring, WeeklyRecurring, DailyRecurring
        ├── services/            # activityService, containerService, api
        ├── types/               # activity.ts — all TypeScript types
        └── context/             # AuthContext
```

---

## Common Pitfalls

1. **DO NOT add boolean backlog flags to `ActivityTemplate`.** Use `ContainerActivities`. See the Terminology section for why.

2. **Always include navigation properties** when querying ActivityTemplates:
   ```csharp
   _context.ActivityTemplates
       .Include(at => at.ContainerActivities).ThenInclude(ca => ca.Container)
       .Include(at => at.ParentActivity)
       .Include(at => at.ChildActivities)
   ```

3. **All DateTimes must be UTC.** PostgreSQL rejects `DateTimeKind.Unspecified`:
   ```csharp
   DateTime.SpecifyKind(new DateTime(2026, 1, 1), DateTimeKind.Utc)
   ```

4. **Integration tests need `[Collection("IntegrationTests")]`** to prevent parallel execution from corrupting shared test data. The collection is defined in `Integration/IntegrationTestCollection.cs` with `DisableParallelization = true`.

5. **Integration test cleanup uses raw SQL** to avoid FK constraint violations:
   ```csharp
   await Context.Database.ExecuteSqlRawAsync(@"
       DELETE FROM ""ContainerActivities""
       WHERE ""ActivityTemplateId"" IN (
           SELECT ""Id"" FROM ""ActivityTemplates"" WHERE ""UserId"" = {0}
       )", TestUserId);
   ```

6. **`ValidateHierarchy` must include `Project`** with an empty allowed-parents list. Removing it would allow `Project` items to have parents, which is invalid.

7. **`GetActivityByIdAsync` filters archived items** (`&& at.ArchivedAt == null`). A soft-deleted item returns `null` → 404, consistent with list queries.

8. **Docker runs the backend as non-root** (`user: "${UID:-1000}:${GID:-1000}"`). Do not remove the NuGet/dotnet home volumes — running as root creates root-owned `obj/`/`bin/` in the bind-mounted source tree.

9. **`CreateActivityDto.DefaultContainerType`** is the camelCase field (`defaultContainerType` in JSON) used to resolve which container to create in when no `ContainerId` is given. If omitted, the backend defaults to Annual. Each page must pass the correct value:
   - Monthly page → `defaultContainerType: ContainerType.Monthly`
   - Weekly page → `defaultContainerType: ContainerType.Weekly`
   - Annual page → can omit (Annual is the default)

10. **`MoveActivityModal` receives all containers**, not just the current page's type. Each page fetches all containers with `containerService.getContainers()` (no type arg) and passes them to the modal. The modal groups them by type.

11. **Completion is shared across all containers.** `ToggleActivityCompletionAsync` updates every `ContainerActivity` record for the activity, not just the one passed in `containerId`. The `containerId` parameter is only used for authorization verification.

12. **The toggle endpoint is `/complete`, not `/toggle`.** The controller action is `PATCH /api/activities/{id}/complete`. The CLAUDE.md previously listed it incorrectly as `/toggle`. The frontend `activityService.toggleCompletion` correctly calls `/complete`.

13. **`useBacklog` containers list does not auto-refresh on its own.** After `NewContainerModal` creates a container, pages call `reloadContainers()` (exposed by the hook) then `reload()` then `setSelectedContainerId(newContainer.id)`. `NewContainerModal.onCreated` now receives the created `Container` object — don't change it back to `() => void`.

14. **`ActivityList.containerType` is optional for recurring pages.** Recurring templates have no `ContainerActivity` records, so passing a `containerType` filter to `ActivityList` would result in an empty list. When `containerType` is `undefined`, the component skips container-based filtering and completion lookups.

15. **`api.patch` returns 204 for the reorder endpoint.** The `PATCH /api/activities/{id}/reorder` action returns `NoContent()`. `api.patch` handles this by checking the status code and returning `undefined as T` — the same pattern as `api.post`. Do not remove that check.

16. **Recurring templates must never have `ContainerActivity` records.** Always pass `skipContainerLink: true` when creating them. `useRecurringItems` enforces this automatically. If a recurring template acquires a container link, it will appear in backlog pages unexpectedly and also be double-counted when a new container is created.

17. **`BuildStampedTitle` uses Sunday as the week label.** For weekly containers, the stamped title uses `container.StartDate.AddDays(-1)` (i.e., the Sunday before the Mon–Sun week) to match the "week of Sunday" convention. For example, a week starting 2026-04-27 (Monday) gets the label "Week of 2026-04-26".

18. **`RecurrenceType` backend values differ from what was once in the frontend.** The fixed mapping is: `Annual=0, Monthly=1, Weekly=2, Daily=3, None=99`. The DB stores enum names as strings (`HasConversion<string>()`), so no migration is needed if only the numeric values change — but never reorder the enum names.

---

## Authentication Flow

GitHub OAuth with cookie-based sessions:
1. Frontend redirects to `/api/auth/github/login`
2. Backend redirects to GitHub
3. GitHub redirects to `/api/auth/github/callback`
4. Backend sets session cookie
5. All API calls use `credentials: 'include'`

Controllers extract the user:
```csharp
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
```

---

## Migration Notes

The codebase previously used boolean flags (`InAnnualBacklog`, `InMonthlyBacklog`) on an `Activity` table. This has been **fully replaced** by the Container architecture. Do not revert. The old `Activity` model is deprecated; all new code uses `ActivityTemplates` + `Containers` + `ContainerActivities`.
