# Life Sprint

A sprint planning and task management application for organizing your life through annual backlogs, monthly plans, weekly sprints, and daily checklists.

## Tech Stack

- **Backend**: .NET 10, ASP.NET Core, Entity Framework Core 10
- **Frontend**: React 19, TypeScript, Vite, SWC
- **Database**: PostgreSQL 16
- **Auth**: GitHub OAuth (cookie-based sessions)
- **Infrastructure**: Docker, NGINX, Digital Ocean
- **CI/CD**: GitHub Actions
- **Testing**: xUnit + FluentAssertions + Moq, Vitest + Testing Library, Playwright

## Features

- Hierarchical task organization (Project → Epic → Story → Task)
- Multiple backlog views:
  - Annual Backlog - Long-term goals and projects
  - Monthly Backlog - Monthly objectives
  - Weekly Sprint - Current week's focus
  - Daily Checklist - Today's tasks
- GitHub OAuth authentication
- State management (Backlog → Todo → In Progress → Done)
- Activities can exist in multiple backlogs simultaneously

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker](https://www.docker.com/get-started) and Docker Compose
- [Git](https://git-scm.com/)
- GitHub account (for OAuth)

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/life-sprints-v2.git
cd life-sprints-v2
```

### 2. Configure Environment Variables

```bash
cp .env.example .env
```

Edit `.env` and configure:
- Database credentials
- GitHub OAuth credentials (see setup below)

### 3. Set Up GitHub OAuth

1. Go to [GitHub Developer Settings](https://github.com/settings/developers)
2. Click "New OAuth App"
3. Configure:
   - **Application name**: Life Sprint (Development)
   - **Homepage URL**: `http://localhost:3000`
   - **Authorization callback URL**: `http://localhost:5000/api/auth/github/callback`
4. Copy the Client ID and Client Secret to your `.env` file

### 4. Start the Development Environment

```bash
# Start all services (backend, frontend, database)
docker-compose up -d

# View logs
docker-compose logs -f

# The application will be available at:
# - Frontend: http://localhost:3000
# - Backend API: http://localhost:5000
# - Database: localhost:5432
```

### 5. Run Database Migrations

```bash
# Apply migrations
docker-compose exec backend dotnet ef database update
```

## Development

### Backend Development

```bash
cd src/backend

# Restore dependencies
dotnet restore

# Run the API
dotnet run --project LifeSprint.Api

# Run tests
dotnet test

# Run only unit tests
dotnet test --filter "Category=Unit"

# Run only integration tests
dotnet test --filter "Category=Integration"

# Create a new migration
dotnet ef migrations add MigrationName --project LifeSprint.Infrastructure --startup-project LifeSprint.Api
```

### Frontend Development

```bash
cd src/frontend

# Install dependencies
npm install

# Run development server
npm run dev

# Run tests
npm test

# Run tests in watch mode
npm run test:watch

# Build for production
npm run build

# Preview production build
npm run preview
```

### E2E Testing

```bash
cd src/frontend

# Install Playwright browsers
npx playwright install

# Run E2E tests
npm run test:e2e

# Run E2E tests in UI mode
npm run test:e2e:ui
```

## Project Structure

```
life-sprints-v2/
├── .github/
│   └── workflows/
│       └── deploy.yml           # CI/CD pipeline
├── src/
│   ├── backend/
│   │   ├── LifeSprint.Api/      # API controllers, middleware
│   │   ├── LifeSprint.Core/     # Domain models, interfaces
│   │   ├── LifeSprint.Infrastructure/  # EF Core, repositories
│   │   └── LifeSprint.Tests/    # Unit & integration tests
│   ├── frontend/
│   │   ├── src/                 # React components, hooks
│   │   ├── e2e/                 # Playwright tests
│   │   └── tests/               # Unit tests
│   └── nginx/
│       └── nginx.conf           # Reverse proxy config
├── docker-compose.yml           # Local development
├── docker-compose.prod.yml      # Production deployment
└── README.md
```

## API Endpoints

### Authentication
- `GET /api/auth/github/login` - Initiate GitHub OAuth
- `GET /api/auth/github/callback` - OAuth callback
- `POST /api/auth/logout` - Logout
- `GET /api/auth/me` - Get current user

### Activities
- `GET /api/activities` - List activities for current user
- `GET /api/activities?containerType={0|1|2|3}` - Filter by container type (Annual/Monthly/Weekly/Daily)
- `GET /api/activities/{id}` - Get single activity (404 if archived)
- `POST /api/activities` - Create activity
- `PUT /api/activities/{id}` - Update activity
- `PATCH /api/activities/{id}/complete` - Toggle completion in a container
- `DELETE /api/activities/{id}` - Archive activity (soft delete)

### Containers
- `GET /api/containers/current?containerType={0|1|2|3}` - Get or create current container

## Deployment

### Production Setup

1. Provision a Digital Ocean droplet (Ubuntu 24.04, 2GB RAM minimum)
2. Configure DNS: `life-sprint.sullivansoftware.dev` → droplet IP
3. Set up GitHub Actions secrets (see `.github/workflows/deploy.yml`)
4. Push to `main` branch - auto-deployment via GitHub Actions

### Manual Deployment

```bash
# SSH into droplet
ssh root@your-droplet-ip

# Clone and setup
cd /opt
git clone https://github.com/yourusername/life-sprints-v2.git
cd life-sprints-v2

# Configure environment
cp .env.example .env
nano .env  # Edit with production values

# Install SSL certificate
certbot certonly --standalone -d life-sprint.sullivansoftware.dev

# Start services
docker-compose -f docker-compose.prod.yml up -d
```

## Testing

### Backend Tests
- **Unit Tests**: Test business logic without database dependencies
- **Integration Tests**: Test database operations with test PostgreSQL instance

### Frontend Tests
- **Component Tests**: Test React components with Vitest & Testing Library
- **E2E Tests**: End-to-end tests with Playwright

### Running All Tests

```bash
# Backend
cd src/backend && dotnet test

# Frontend
cd src/frontend && npm test && npm run test:e2e
```

## Contributing

1. Create a feature branch
2. Make your changes
3. Run tests
4. Create a pull request

## License

MIT

## Support

For issues and questions, please open a GitHub issue.

