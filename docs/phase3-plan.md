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

### Phase 3.1: Database Models & Migration ✅ / ⏳ / ❌
**Goal**: Create new EF Core models and migration

**Files to Create/Modify**:
- [ ] `src/backend/LifeSprint.Core/Models/ActivityTemplate.cs`
  - Master task definition
  - IsRecurring, RecurrenceType, ArchivedAt
  - Navigation: ContainerActivities collection

- [ ] `src/backend/LifeSprint.Core/Models/Container.cs`
  - Type enum (Annual/Monthly/Weekly/Daily)
  - Status enum (Active/Completed/Archived)
  - StartDate, EndDate, navigation properties

- [ ] `src/backend/LifeSprint.Core/Models/ContainerActivity.cs`
  - Junction table with composite key
  - CompletedAt, Order, IsRolledOver
  - Navigation: ActivityTemplate, Container

- [ ] `src/backend/LifeSprint.Core/Enums/ContainerType.cs`
  - Annual, Monthly, Weekly, Daily

- [ ] `src/backend/LifeSprint.Core/Enums/ContainerStatus.cs`
  - Active, Completed, Archived

- [ ] `src/backend/LifeSprint.Core/Enums/RecurrenceType.cs`
  - Annual, Monthly, Weekly, Daily, None

- [ ] `src/backend/LifeSprint.Infrastructure/Data/AppDbContext.cs`
  - Configure new entities
  - Set up composite keys for ContainerActivity
  - Add indexes for common queries
  - Create migration: `AddContainerArchitecture`

**Success Criteria**: Migration runs successfully, database schema created

---

### Phase 3.2: Activity Service Layer ✅ / ⏳ / ❌
**Goal**: Create service to handle Activity creation with container relationships

**Files to Create**:
- [ ] `src/backend/LifeSprint.Core/Interfaces/IActivityService.cs`
  - CreateActivityAsync(userId, createDto)
  - GetActivitiesForUserAsync(userId) - basic read for tests
  - Comments: Link to ActivityService implementation

- [ ] `src/backend/LifeSprint.Core/DTOs/CreateActivityDto.cs`
  - Title, Description, IsRecurring, RecurrenceType
  - ContainerId (optional - defaults to current Annual backlog)
  - Small, focused DTO with validation attributes

- [ ] `src/backend/LifeSprint.Core/DTOs/ActivityResponseDto.cs`
  - Return DTO with template info + container associations
  - Includes completion status per container

- [ ] `src/backend/LifeSprint.Infrastructure/Services/ActivityService.cs`
  - Implements IActivityService
  - **CreateActivityAsync logic**:
    1. Create ActivityTemplate record
    2. If ContainerId provided, add to ContainerActivities
    3. If no container, find or create current Annual container
    4. Return ActivityResponseDto
  - **GetActivitiesForUserAsync** (basic implementation):
    - Pull all ActivityTemplates for user with container associations
    - Transform to DTOs
  - Comments: Link to related files (DTOs, models, tests)

**Success Criteria**: Service compiles, basic logic in place

---

### Phase 3.3: Unit Tests ✅ / ⏳ / ❌
**Goal**: TDD-style unit tests for ActivityService

**Files to Create**:
- [ ] `src/backend/LifeSprint.Tests/Unit/ActivityServiceTests.cs`
  - Test: CreateActivity_WithoutContainer_AddsToAnnualBacklog
  - Test: CreateActivity_WithContainer_AddsToSpecifiedContainer
  - Test: CreateRecurringActivity_SetsRecurrenceCorrectly
  - Test: GetActivitiesForUser_ReturnsAllUserActivities
  - Test: GetActivitiesForUser_DoesNotReturnOtherUsersActivities
  - Mock dependencies (DbContext via InMemory)
  - Comments: Explain test scenarios and expected behavior

**Success Criteria**: All unit tests pass

---

### Phase 3.4: Integration Tests ✅ / ⏳ / ❌
**Goal**: Test actual database operations with Docker test DB

**Files to Create**:
- [ ] `src/backend/LifeSprint.Tests/Integration/ActivityServiceIntegrationTests.cs`
  - Test: CreateActivity_PersistsToDatabase
  - Test: CreateActivity_InAnnualContainer_CreatesCorrectAssociations
  - Test: CreateActivity_InMonthlyContainer_CreatesCorrectAssociations
  - Test: CreateActivity_InWeeklyContainer_CreatesCorrectAssociations
  - Test: CreateRecurringActivity_CanBeQueriedByType
  - Uses real database connection (test-db from docker-compose)
  - Setup/teardown with IAsyncLifetime
  - Comments: Explain integration test setup

**Success Criteria**: All integration tests pass against test database

---

### Phase 3.5: Container Helper Service ✅ / ⏳ / ❌
**Goal**: Service to manage containers (find current, create new)

**Files to Create**:
- [ ] `src/backend/LifeSprint.Core/Interfaces/IContainerService.cs`
  - GetOrCreateCurrentContainerAsync(userId, containerType)
  - GetContainerAsync(containerId)
  - Comments: Link to ContainerService

- [ ] `src/backend/LifeSprint.Infrastructure/Services/ContainerService.cs`
  - **GetOrCreateCurrentContainerAsync** logic:
    1. Find active container of type for user
    2. If not found, create new one with appropriate dates
    3. Return container
  - Date logic for each container type:
    - Annual: Jan 1 - Dec 31 of current year
    - Monthly: 1st - last day of current month
    - Weekly: Monday - Sunday of current week
    - Daily: Today's date
  - Comments: Explain date calculations and container lifecycle

- [ ] `src/backend/LifeSprint.Tests/Unit/ContainerServiceTests.cs`
  - Test: GetOrCreateAnnualContainer_CreatesIfNotExists
  - Test: GetOrCreateAnnualContainer_ReturnsExistingIfActive
  - Test: GetOrCreateMonthlyContainer_UsesCorrectDateRange
  - Test: GetOrCreateWeeklyContainer_UsesCorrectDateRange

**Success Criteria**: Container management working, tests pass

---

### Phase 3.6: API Controller ✅ / ⏳ / ❌
**Goal**: Expose Activity creation via REST API

**Files to Create**:
- [ ] `src/backend/LifeSprint.Api/Controllers/ActivitiesController.cs`
  - POST /api/activities - Create activity
  - GET /api/activities - Get all activities for current user
  - Depends on IActivityService
  - Authorization: Requires authenticated user
  - Comments: Link to service layer and DTOs

- [ ] Register services in `src/backend/LifeSprint.Api/Program.cs`
  - AddScoped<IActivityService, ActivityService>
  - AddScoped<IContainerService, ContainerService>

**Success Criteria**:
- Can create activity via API
- Can retrieve activities via API
- Returns 401 if not authenticated

---

### Phase 3.7: Manual Testing ✅ / ⏳ / ❌
**Goal**: Verify end-to-end functionality with curl/Postman

**Test Scenarios**:
- [ ] Create activity without container (should go to Annual)
- [ ] Create activity with specific container
- [ ] Create recurring activity
- [ ] Retrieve all activities for user
- [ ] Verify user isolation (can't see other users' activities)

**Success Criteria**: All scenarios work as expected

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

### Current Status: Planning Complete ✅

**Next Step**: Phase 3.1 - Database Models & Migration

**Estimated Effort**:
- Phase 3.1: 30 minutes (models + migration)
- Phase 3.2: 45 minutes (service layer)
- Phase 3.3: 30 minutes (unit tests)
- Phase 3.4: 30 minutes (integration tests)
- Phase 3.5: 30 minutes (container service)
- Phase 3.6: 20 minutes (API controller)
- Phase 3.7: 15 minutes (manual testing)

**Total**: ~3.5 hours

---

## Notes

- Keep files small (<200 lines ideally)
- Write tests first (TDD) where possible
- Add detailed comments for AI navigation
- Link related files in comments
- Focus on CREATE only - Read/Update/Delete later
- Basic read functionality just to support tests
