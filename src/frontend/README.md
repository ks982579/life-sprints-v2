# Life Sprint — Frontend

React 19 + TypeScript + Vite frontend for the Life Sprint application.

## Overview

The frontend provides four backlog pages (Annual, Monthly, Weekly, Daily) and four recurring-item pages, all sharing common components. Each backlog page lets you:

- View todo items assigned to that backlog
- Create new items (automatically added to higher-level backlogs too)
- Reorder items within the current container using ▲/▼ arrows
- Add child items to Project/Epic/Story items via the "Add" button
- Check items complete (syncs across all backlogs)
- Add existing items to other backlogs via the "Add to Backlog" modal
- Start a new period (New Sprint / New Month / New Year) with optional rollover of incomplete items
- Navigate to historical containers via the date navigator

Recurring item pages let you define templates that auto-stamp into the correct container when a new period is started.

## Development

```bash
npm install
npm run dev              # http://localhost:3000 with HMR
npm run build            # Production build (also runs tsc)
npm run lint             # ESLint
npm test                 # Vitest unit tests (run once)
npm run test:watch       # Vitest in watch mode
npm run test:e2e         # Playwright E2E (requires docker compose up first)
npm run test:e2e:ui      # Playwright with interactive UI
npm run test:e2e:headed  # Playwright with visible browser
```

The dev server proxies `/api` requests to the backend at `http://localhost:5000`. In production the NGINX reverse proxy handles this.

## Key Files

```
src/
├── components/
│   ├── Activities/
│   │   ├── ActivityEditor.tsx       # Create/edit form; hideRecurring/fixedIsRecurring/fixedRecurrenceType props
│   │   ├── ActivityList.tsx         # Item list with reorder, checkbox, add-child, edit, move, delete
│   │   ├── AddChildModal.tsx        # Creates correct child type for Project/Epic/Story
│   │   ├── ActivityDetailModal.tsx  # Read-only detail view
│   │   ├── MoveActivityModal.tsx    # Add item to another backlog (all types, grouped)
│   │   ├── NewContainerModal.tsx    # Start new period with rollover option; onCreated receives Container
│   │   └── BacklogTabs.tsx          # Tab navigation component
│   ├── Navigation/
│   │   └── DateNavigator.tsx        # Navigate historical containers
│   └── Auth/
│       ├── LoginPage.tsx
│       └── ProtectedRoute.tsx
├── hooks/
│   ├── useBacklog.ts                # Core state hook used by all backlog pages; exposes reloadContainers
│   └── useRecurringItems.ts         # State hook for recurring pages; always passes skipContainerLink: true
├── pages/
│   ├── AnnualBacklog.tsx
│   ├── MonthlyBacklog.tsx
│   ├── WeeklySprint.tsx
│   ├── DailyChecklist.tsx
│   └── recurring/
│       ├── AnnualRecurring.tsx      # /recurring/annual
│       ├── MonthlyRecurring.tsx     # /recurring/monthly
│       ├── WeeklyRecurring.tsx      # /recurring/weekly
│       └── DailyRecurring.tsx       # /recurring/daily
├── services/
│   ├── api.ts                       # Fetch wrapper with credentials; handles 204 on PATCH/POST
│   ├── activityService.ts           # Todo item CRUD + reorder + container operations; isRecurring/recurrenceType filters
│   └── containerService.ts          # Backlog container CRUD + createNewContainer
├── types/
│   └── activity.ts                  # All TypeScript types (enums, DTOs, models)
└── context/
    └── AuthContext.tsx
```

## Important Patterns

### Enum Pattern

TypeScript enums are avoided due to `verbatimModuleSyntax`. Use const-object pattern:

```typescript
export type ContainerType = 0 | 1 | 2 | 3;
export const ContainerType = {
  Annual:  0 as ContainerType,
  Monthly: 1 as ContainerType,
  Weekly:  2 as ContainerType,
  Daily:   3 as ContainerType,
} as const;
```

`RecurrenceType` values: `Annual=0, Monthly=1, Weekly=2, Daily=3, None=99`. These must match the backend exactly — the DB stores enum names as strings, not numbers.

### Type Imports

Always use `type` keyword for pure type imports:

```typescript
import { ContainerType, type Activity, type Container } from '../types';
```

### Creating Items in Backlog Pages

Pass `defaultContainerType` when creating items so they land in the right backlog:

```typescript
await handleCreate({
  ...data,
  containerId: selectedContainerId,          // specific container if user navigated to one
  defaultContainerType: ContainerType.Weekly // fallback if containerId is undefined
});
```

The backend auto-propagates the item upward (Weekly → Monthly → Annual).

### Creating Recurring Templates

Always pass `skipContainerLink: true` — `useRecurringItems` handles this automatically:

```typescript
await handleCreate({ ...data, skipContainerLink: true });
```

Recurring templates have no container associations and are instantiated (with a stamped title like "Pay Bills | April 2026") when a new container of the matching type is created.

### New Container Reload Pattern

After a new container is created, pages must refresh both containers and activities without a page reload:

```typescript
const handleContainerCreated = async (newContainer: Container) => {
  setShowNewContainer(false);
  await reloadContainers();    // refresh DateNavigator
  await reload();              // refresh activity list
  setSelectedContainerId(newContainer.id);
  const updated = await containerService.getContainers();
  setAllContainers(updated);   // refresh MoveActivityModal data
};
```

`NewContainerModal.onCreated` receives the created `Container` object — this is required for `setSelectedContainerId`.

### ActivityList Without containerType

Recurring pages pass no `containerType` to `ActivityList`:

```tsx
<ActivityList activities={activities} onActivityClick={...} ... />
```

When `containerType` is `undefined`, the list skips container-based filtering and completion state lookups. Do not pass a `containerType` from recurring pages — it would filter out all items.

### All-Container Modal Data

Each backlog page fetches all containers (all types) separately for the `MoveActivityModal`:

```typescript
const [allContainers, setAllContainers] = useState<Container[]>([]);
useEffect(() => {
  containerService.getContainers().then(setAllContainers);
}, []);
```

This is separate from the `containers` list in `useBacklog`, which only holds the current page's type.
