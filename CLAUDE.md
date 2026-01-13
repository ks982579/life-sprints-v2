# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Life Sprint is a sprint planning and task management application for organizing personal life through hierarchical activities (Project → Epic → Story → Task) across multiple time containers (Annual/Monthly/Weekly/Daily backlogs).

**Tech Stack**: .NET 10, ASP.NET Core, EF Core 10, PostgreSQL 16, React 19, TypeScript, Vite, Docker

## Core Domain Architecture

### Container-Based System (NOT Boolean Flags)

The application uses a **Container architecture** with a many-to-many junction table pattern. This is critical to understand:

**ActivityTemplates**: Master task/goal definitions
- Can be recurring (weekly reviews, annual planning, etc.)
- Support hierarchical relationships via `ParentActivityId` (Story → Epic → Project)
- Soft delete with `ArchivedAt`

**Containers**: Unified table for all backlog types (Annual/Monthly/Weekly/Daily)
- Type discriminator determines timescale
- Date ranges (`StartDate`, `EndDate`) for historical tracking
- Status: Active/Completed/Archived

**ContainerActivities**: Junction table with composite key `(ContainerId, ActivityTemplateId)`
- Enables activities to exist in multiple containers simultaneously
- Tracks completion **per container** (same task can be incomplete in one sprint, done in another)
- `Order` field for user-defined ordering
- `IsRolledOver` tracks tasks carried forward from previous containers

**Why This Matters**: DO NOT add boolean flags like `InAnnualBacklog`, `InMonthlyBacklog` to ActivityTemplate. Use the junction table. This enables:
- Historical tracking (view previous sprints)
- Flexible relationships (task in both Annual backlog AND current weekly sprint)
- Consistent querying pattern across all container types
- Rollover logic for incomplete tasks

### Activity Type Hierarchy

Activities have a `Type` field with strict parent-child rules enforced in `ActivityService.ValidateHierarchy()`:

- **Project** (top-level): No parent allowed
- **Epic**: Parent must be Project
- **Story**: Parent must be Epic or Project
- **Task**: Parent must be Story or Epic

## Essential Commands

### Backend (.NET 10)

```bash
cd src/backend

# Build and restore
dotnet restore
dotnet build

# Run API locally
dotnet run --project LifeSprint.Api

# Testing
dotnet test                                    # All tests
dotnet test --filter "Category=Unit"          # Unit tests only (mocked DB)
dotnet test --filter "Category=Integration"   # Integration tests (requires test-db)

# Database migrations
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

# Development
npm install
npm run dev                # Dev server on port 3000

# Testing
npm test                   # Vitest unit tests
npm run test:e2e           # Playwright E2E tests
npm run test:e2e:ui        # Playwright UI mode

# Build
npm run build
npm run preview
```

### Docker Compose

```bash
# Start all services (postgres, backend, frontend, nginx, test-db)
docker compose up

# Start in detached mode
docker compose up -d

# View logs
docker compose logs -f backend
docker compose logs -f frontend

# Restart after code changes
docker compose restart backend

# Stop all services
docker compose down

# Delete database volume (fresh start)
docker compose down -v
```

**Service URLs**:
- Frontend: http://localhost:3000
- Backend API: http://localhost:5000
- NGINX reverse proxy: http://localhost
- PostgreSQL: localhost:5432
- Test DB: localhost:5433

## Project Structure

```
src/
├── backend/
│   ├── LifeSprint.Api/              # Controllers, Program.cs, middleware
│   ├── LifeSprint.Core/             # Domain models, interfaces, DTOs, enums
│   │   ├── Models/                  # ActivityTemplate, Container, ContainerActivity
│   │   ├── Interfaces/              # IActivityService, IContainerService
│   │   ├── DTOs/                    # Request/Response DTOs
│   │   └── Enums/                   # ActivityType, ContainerType, RecurrenceType
│   ├── LifeSprint.Infrastructure/   # EF Core, services implementation
│   │   ├── Data/                    # AppDbContext, migrations
│   │   └── Services/                # ActivityService, ContainerService
│   └── LifeSprint.Tests/
│       ├── Unit/                    # Mock-based tests
│       └── Integration/             # Real DB tests (use IntegrationTestBase)
│
└── frontend/
    └── src/
        ├── components/              # React components
        │   ├── Activities/          # BacklogTabs, ActivityList, ActivityEditor
        │   └── Auth/                # LoginPage, ProtectedRoute
        ├── services/                # API clients (activityService, authService)
        ├── types/                   # TypeScript types matching backend DTOs
        └── context/                 # React Context (AuthContext)
```

## Key Implementation Patterns

### Backend Service Pattern

Services are injected via DI in `Program.cs`:
```csharp
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IContainerService, ContainerService>();
```

**ActivityService** creates activities and manages container associations:
1. Validates hierarchy rules if `ParentActivityId` is provided
2. Creates `ActivityTemplate` record
3. Determines target container (provided or defaults to current Annual)
4. Creates `ContainerActivity` junction record with next order number

**ContainerService** manages container lifecycle:
- `GetOrCreateCurrentContainerAsync()` finds active container or creates new one
- Date ranges:
  - Annual: Jan 1 - Dec 31 of current year
  - Monthly: 1st - last day of current month
  - Weekly: Monday - Sunday (ISO 8601 week)
  - Daily: Single day

### Frontend TypeScript Patterns

Due to `verbatimModuleSyntax` and `erasableSyntaxOnly` in tsconfig, enums use this pattern:

```typescript
export type ActivityType = 0 | 1 | 2 | 3;
export const ActivityType = {
  Project: 0 as ActivityType,
  Epic: 1 as ActivityType,
  Story: 2 as ActivityType,
  Task: 3 as ActivityType,
} as const;
```

This provides type safety with zero runtime overhead.

### Testing Patterns

**Unit Tests**: Use in-memory EF Core database for isolation
```csharp
[Trait("Category", "Unit")]
public class ActivityServiceTests
{
    private readonly AppDbContext _context;
    // Mock dependencies, test business logic
}
```

**Integration Tests**: Extend `IntegrationTestBase` which:
- Connects to real PostgreSQL test-db (port 5433)
- Implements `IAsyncLifetime` for setup/teardown
- Cleans test data using raw SQL (avoids FK constraint issues)

```csharp
public class ActivityServiceIntegrationTests : IntegrationTestBase
{
    // Tests full stack: Service -> EF Core -> PostgreSQL
}
```

## Docker Development Workflow

The docker-compose.yml backend service:
1. Installs dotnet-ef tool globally
2. Restores NuGet packages
3. Builds projects
4. Waits for database health check
5. Applies EF Core migrations automatically
6. Starts API with `dotnet watch` (hot reload)

**Important**: The backend command explicitly builds before running migrations to avoid "Unable to retrieve project metadata" errors.

## Authentication Flow

GitHub OAuth with cookie-based sessions:
1. Frontend redirects to `/api/auth/github/login`
2. Backend redirects to GitHub OAuth
3. GitHub redirects back to `/api/auth/github/callback`
4. Backend creates session cookie
5. All API requests include cookie via `credentials: 'include'`

Controllers get current user via:
```csharp
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
```

## Common Pitfalls

1. **DO NOT** modify ActivityTemplate to add boolean flags for containers. Use ContainerActivities junction table.

2. **DO NOT** forget to include navigation properties when querying:
```csharp
// GOOD
await _context.ActivityTemplates
    .Include(at => at.ContainerActivities)
        .ThenInclude(ca => ca.Container)
    .Include(at => at.ParentActivity)
    .Include(at => at.ChildActivities)
    .ToListAsync();
```

3. **DO NOT** use `DateTime.Unspecified` - PostgreSQL requires UTC:
```csharp
// GOOD
var date = DateTime.SpecifyKind(new DateTime(2025, 1, 1), DateTimeKind.Utc);
```

4. **Integration test cleanup**: Use raw SQL to avoid FK constraint violations:
```csharp
await Context.Database.ExecuteSqlRawAsync(@"
    DELETE FROM ""ContainerActivities""
    WHERE ""ActivityTemplateId"" IN (
        SELECT ""Id"" FROM ""ActivityTemplates"" WHERE ""UserId"" = {0}
    )", TestUserId);
```

5. **Frontend imports**: Use `type` imports for types when `verbatimModuleSyntax` is enabled:
```typescript
import { ActivityType, type Activity } from './types';
```

## Migration Notes

The codebase previously used boolean flags (`InAnnualBacklog`, `InMonthlyBacklog`) on an `Activity` table. This has been **completely replaced** by the Container architecture. Do not revert to boolean flags.

The old `Activity` model may still exist in the codebase but is deprecated - all new code uses `ActivityTemplates` + `Containers` + `ContainerActivities`.
