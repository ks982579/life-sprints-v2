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
| GET | `/api/activities` | Get all items for the user. `?containerType=N` or `?containerId=N` to filter |
| GET | `/api/activities/{id}` | Get one item (404 if archived) |
| POST | `/api/activities` | Create a new item. Body: `CreateActivityDto` |
| PUT | `/api/activities/{id}` | Update item metadata |
| DELETE | `/api/activities/{id}` | Soft-delete (archive) an item |
| PATCH | `/api/activities/{id}/toggle` | Toggle completion in a specific container (updates all containers) |
| POST | `/api/activities/{id}/containers/{containerId}` | Add item to an additional container (propagates upward) |
| DELETE | `/api/activities/{id}/containers/{containerId}` | Remove item from a specific container |

**`CreateActivityDto` fields:**
- `title` (required), `description`, `type` (required), `parentActivityId`
- `isRecurring`, `recurrenceType`
- `containerId` — specific container to add to (takes precedence)
- `defaultContainerType` — fallback container type when `containerId` is null; defaults to Annual if omitted

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

---

## Frontend Architecture

### Pages and Their Backlog Types

| Page component | ContainerType | "New" button label |
|---|---|---|
| `AnnualBacklog.tsx` | Annual (0) | New Year |
| `MonthlyBacklog.tsx` | Monthly (1) | New Month |
| `WeeklySprint.tsx` | Weekly (2) | New Sprint |

All three pages share an identical structure using the `useBacklog(containerType)` hook.

### Key Components

| Component | Purpose |
|---|---|
| `useBacklog(type)` | Custom hook: loads containers + activities, exposes CRUD callbacks |
| `DateNavigator` | Lets user navigate to historical containers of the same type |
| `ActivityEditor` | Form for creating/editing an item |
| `ActivityList` | Renders items with completion checkbox, edit/move/delete actions |
| `MoveActivityModal` | "Add to Backlog" — shows all non-archived containers grouped by type |
| `NewContainerModal` | "New Sprint / Month / Year" — rollover choice, calls `POST /containers/new` |
| `ActivityDetailModal` | Read-only detail view |

### The `useBacklog` Hook

Manages state for a single backlog page:
- Loads all containers of the given type (for `DateNavigator`)
- Loads activities filtered by `selectedContainerId` (or by `containerType` when no container is selected)
- Exposes `handleCreate`, `handleUpdate`, `handleDelete`, `handleToggle`, `reload`

`selectedContainerId` starts as `undefined`. Each page passes `defaultContainerType` in create calls so the backend resolves the right container even before the user has navigated to a specific container.

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
npm run dev        # Dev server on port 3000
npm run build      # Production build (also runs tsc)
npm test           # Vitest unit tests
npm run test:e2e   # Playwright E2E
```

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
        │   │                    # MoveActivityModal, NewContainerModal, BacklogTabs
        │   ├── Navigation/      # DateNavigator
        │   └── Auth/            # LoginPage, ProtectedRoute
        ├── hooks/               # useBacklog
        ├── pages/               # AnnualBacklog, MonthlyBacklog, WeeklySprint
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
