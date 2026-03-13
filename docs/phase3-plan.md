# Phase 3: Activity Creation - Implementation Plan

**Date Started**: 2026-01-01
**Goal**: Implement CREATE functionality for Activities using the new Container-based architecture
**Approach**: Small, modular files with clear comments for AI agent navigation

---

## Architecture Overview

### New Database Schema (Replacing Boolean Flags)

**ActivityTemplates** - Master task/goal definitions
- Can be recurring (annual planning, weekly reviews, etc.)
- Templates can be instantiated into multiple containers
- Soft delete with ArchivedAt

**Containers** - Unified backlogs/sprints (Annual/Monthly/Weekly/Daily)
- Type discriminator for different timescales
- Active/Completed/Archived status
- Date ranges for historical tracking

**ContainerActivities** - Junction table (many-to-many)
- Links templates to containers
- Tracks completion per-container (same task can be incomplete in one sprint, done in another)
- Supports rollover tracking and ordering

### Benefits Over Boolean Flags
- Historical tracking: View previous sprints/backlogs
- Flexible relationships: Task in multiple containers simultaneously
- Rollover logic: Transfer incomplete tasks to new sprint
- Consistent querying: Same pattern for all container types
- Recurring tasks: Template-based instantiation

---

## Implementation Phases

### Phase 3.1: Database Models & Migration ✅ COMPLETE
- `ActivityTemplate`, `Container`, `ContainerActivity` models created
- Enums: `ContainerType`, `ContainerStatus`, `RecurrenceType`, `ActivityType`
- `AppDbContext` configured with composite keys, indexes, cascade deletes
- Migration `AddContainerArchitecture` applied

### Phase 3.2: Activity Service Layer ✅ COMPLETE
- `IActivityService` with full CRUD + toggle + filter signatures
- `CreateActivityDto`, `UpdateActivityDto`, `ActivityResponseDto` DTOs
- `ActivityService` implementing all operations with hierarchy validation

### Phase 3.3: Unit Tests ✅ COMPLETE
- `ActivityServiceTests.cs`: 40+ unit tests covering create, read, update, archive, toggle, hierarchy
- `ActivitiesControllerTests.cs`: 30+ controller unit tests covering all endpoints
- **80 unit tests total, all passing**

### Phase 3.4: Integration Tests ✅ COMPLETE
- `ActivityServiceIntegrationTests.cs`: create, update, archive, toggle, filter scenarios
- `ActivitiesControllerIntegrationTests.cs`: full request/response coverage
- xUnit `[Collection("IntegrationTests")]` prevents parallel execution conflicts
- **33 integration tests total, all passing**

### Phase 3.5: Container Helper Service ✅ COMPLETE
- `IContainerService` / `ContainerService` with `GetOrCreateCurrentContainerAsync`
- Correct date ranges for Annual, Monthly (fixed off-by-one bug), Weekly (ISO 8601), Daily
- `ContainerServiceTests.cs`: unit tests for all container types

### Phase 3.6: API Controller ✅ COMPLETE
- `ActivitiesController`: GET (with filter), POST, PUT, PATCH /complete, DELETE
- `ContainersController`: GET /current
- Services registered in `Program.cs`

### Phase 3.7: Frontend ✅ COMPLETE
- `BacklogTabs`, `ActivityList`, `ActivityEditor` components
- `activityService` API client for all CRUD + toggle
- Tab switching triggers server-side filtered reload
- **41 Vitest unit tests, all passing**

---

## File Organization Principles

### Small & Modular
- One class per file
- DTOs in separate files
- Services focused on single responsibility
- Tests mirror source structure

### AI-Friendly Comments
```csharp
/// <summary>
/// Creates a new activity template and adds it to a container.
/// </summary>
/// <remarks>
/// Related files:
/// - Models: src/backend/LifeSprint.Core/Models/ActivityTemplate.cs
/// - DTOs: src/backend/LifeSprint.Core/DTOs/CreateActivityDto.cs
/// - Tests: src/backend/LifeSprint.Tests/Unit/ActivityServiceTests.cs
/// </remarks>
```

### Navigation Hints
- Link to related files in comments
- Explain "why" not just "what"
- Document business logic decisions

---

## Migration Path from Old Schema

**Current Schema** (to be deprecated):
- Activity table with boolean flags (InAnnualBacklog, InMonthlyBacklog, etc.)

**New Schema**:
- ActivityTemplates + Containers + ContainerActivities

**Migration Strategy** (for later):
1. Create new tables alongside old ones
2. Migrate data from Activity to ActivityTemplates
3. Create Annual/Monthly/Weekly containers from existing data
4. Populate ContainerActivities based on boolean flags
5. Update all queries to use new schema
6. Drop old Activity table and columns

**For Now**: Build new schema, old Activity table can coexist temporarily

---

## Open Questions & Decisions

### Q1: How to handle "Sprint Planning" recurring task?
**Option A**: ActivityTemplate with IsRecurring=true, RecurrenceType=Weekly
**Option B**: System-generated task when new sprint created
**Decision**: TBD - start with Option A (simpler)

### Q2: What happens when user creates first activity?
**Decision**: Auto-create current Annual container if it doesn't exist

### Q3: Can a task be in multiple containers of same type?
**Example**: Task in both "January Monthly" and "February Monthly"
**Decision**: Yes - junction table supports this. Completion tracked per-container.

### Q4: Should we deprecate old Activity table immediately?
**Decision**: No - keep it for now, focus on new schema. Migration later.

---

## Progress Tracking

### Current Status: Phase 3 Complete ✅

**Completed**: 2026-03-13

**Test Counts**:
- Backend unit tests: 80 (all passing)
- Backend integration tests: 33 (all passing)
- Frontend Vitest tests: 41 (all passing)

## Bugs Fixed During Implementation

1. **`ContainerService.GetMonthlyRange`**: `AddDays(-1).AddMonths(1)` → `AddMonths(1).AddDays(-1)` (wrong end date)
2. **`ActivityService.ValidateHierarchy`**: `Project` type was missing from dictionary, allowing Projects to silently gain parents
3. **`ActivityService.GetActivityByIdAsync`**: Missing `&& at.ArchivedAt == null` filter caused archived activities to return 200
4. **Integration test parallelism**: xUnit runs test classes in parallel by default; shared `TestUserId` caused `CleanupTestDataAsync` to delete live test data; fixed with `[Collection("IntegrationTests")]`
