# Life Sprint — Frontend

React 19 + TypeScript + Vite frontend for the Life Sprint application.

## Overview

The frontend provides three backlog pages (Annual, Monthly, Weekly) that all share the same structure. A fourth (Daily) exists in the type system but is not yet surfaced in the UI.

Each page lets you:
- View todo items assigned to that backlog
- Create new items (automatically added to higher-level backlogs too)
- Check items complete (syncs across all backlogs)
- Add existing items to other backlogs via the "Add to Backlog" modal
- Start a new period (New Sprint / New Month / New Year) with optional rollover of incomplete items
- Navigate to historical containers via the date navigator

## Development

```bash
npm install
npm run dev        # http://localhost:3000 with HMR
npm run build      # Production build (also runs tsc)
npm test           # Vitest unit tests
npm run test:e2e   # Playwright E2E tests
```

The dev server proxies `/api` requests to the backend at `http://localhost:5000`. In production the NGINX reverse proxy handles this.

## Key Files

```
src/
├── components/
│   ├── Activities/
│   │   ├── ActivityEditor.tsx       # Create/edit form
│   │   ├── ActivityList.tsx         # Item list with checkbox, edit, move, delete
│   │   ├── ActivityDetailModal.tsx  # Read-only detail view
│   │   ├── MoveActivityModal.tsx    # Add item to another backlog (all types, grouped)
│   │   ├── NewContainerModal.tsx    # Start new period with rollover option
│   │   └── BacklogTabs.tsx          # Tab navigation component
│   ├── Navigation/
│   │   └── DateNavigator.tsx        # Navigate historical containers
│   └── Auth/
│       ├── LoginPage.tsx
│       └── ProtectedRoute.tsx
├── hooks/
│   └── useBacklog.ts                # Core state hook used by all backlog pages
├── pages/
│   ├── AnnualBacklog.tsx
│   ├── MonthlyBacklog.tsx
│   └── WeeklySprint.tsx
├── services/
│   ├── api.ts                       # Fetch wrapper with credentials
│   ├── activityService.ts           # Todo item CRUD + container operations
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

### Type Imports

Always use `type` keyword for pure type imports:

```typescript
import { ContainerType, type Activity, type Container } from '../types';
```

### Creating Items

Pass `defaultContainerType` when creating items so they land in the right backlog:

```typescript
await handleCreate({
  ...data,
  containerId: selectedContainerId,          // specific container if user navigated to one
  defaultContainerType: ContainerType.Weekly // fallback if containerId is undefined
});
```

The backend auto-propagates the item upward (Weekly → Monthly → Annual).

### All-Container Modal Data

Each backlog page fetches all containers (all types) separately for the `MoveActivityModal`:

```typescript
const [allContainers, setAllContainers] = useState<Container[]>([]);
useEffect(() => {
  containerService.getContainers().then(setAllContainers);
}, []);
```

This is separate from the `containers` list in `useBacklog`, which only holds the current page's type.
