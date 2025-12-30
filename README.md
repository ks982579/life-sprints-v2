# Life Sprint

A sprint planning and task management application for organizing your life through annual backlogs, monthly plans, weekly sprints, and daily checklists.

## Tech Stack

- **Backend**: .NET 9, ASP.NET Core MVC, Entity Framework Core
- **Frontend**: TypeScript, React, Vite
- **Database**: PostgreSQL 16
- **Auth**: GitHub OAuth
- **Infrastructure**: Docker, NGINX, Digital Ocean
- **CI/CD**: GitHub Actions
- **Testing**: xUnit, Vitest, Playwright

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

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
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
- `GET /api/activities` - List all activities (with filters)
- `GET /api/activities/:id` - Get single activity
- `POST /api/activities` - Create activity
- `PUT /api/activities/:id` - Update activity
- `DELETE /api/activities/:id` - Delete activity
- `PATCH /api/activities/:id/state` - Update state
- `PATCH /api/activities/:id/backlog` - Update backlog flags

### Backlogs
- `GET /api/backlogs/annual` - Get annual backlog
- `GET /api/backlogs/monthly` - Get monthly backlog
- `GET /api/backlogs/weekly` - Get weekly sprint
- `GET /api/backlogs/daily` - Get daily checklist

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

---

Setting up project

```bash
dotnet new sln -n LifeSprint
dotnet new webapi -n LifeSprint.Api -o LifeSprint.Api --no-https
dotnet new classlib -n LifeSprint.Core -o LifeSprint.Core
dotnet new classlib -n LifeSprint.Infra -o LifeSprint.Infra
dotnet new xunit -n LifeSprint.Tests -o LifeSprint.Tests
dotnet sln add LifeSprint.Api/LifeSprint.Api.csproj LifeSprint.Core/LifeSprint.Core.csproj LifeSprint.Infra/LifeSprint.Infra.csproj LifeSprint.Tests/LifeSprint.Tests.csproj

# Projects reference each other
dotnet add LifeSprint.Api/LifeSprint.Api.csproj reference LifeSprint.Core/LifeSprint.Core.csproj LifeSprint.Infrastructure/LifeSprint.Infrastructure.csproj \ 
&& dotnet add LifeSprint.Infrastructure/LifeSprint.Infrastructure.csproj reference LifeSprint.Core/LifeSprint.Core.csproj \
&& dotnet add LifeSprint.Tests/LifeSprint.Tests.csproj reference LifeSprint.Api/LifeSprint.Api.csproj LifeSprint.Core/LifeSprint.Core.csproj LifeSprint.Infrastructure/LifeSprint.Infrastructure.csproj

# Get packages
dotnet add LifeSprint.Infrastructure/LifeSprint.Infrastructure.csproj package Npgsql.EntityFrameworkCore.PostgreSQL \
&& dotnet add LifeSprint.Infrastructure/LifeSprint.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design \
&& dotnet add LifeSprint.Api/LifeSprint.Api.csproj package Npgsql.EntityFrameworkCore.PostgreSQL \
&& dotnet add LifeSprint.Api/LifeSprint.Api.csproj package Microsoft.EntityFrameworkCore.Design

dotnet add LifeSprint.Tests/LifeSprint.Tests.csproj package Moq \
&& dotnet add LifeSprint.Tests/LifeSprint.Tests.csproj package FluentAssertions \
&& dotnet add LifeSprint.Tests/LifeSprint.Tests.csproj package Microsoft.EntityFrameworkCore.InMemory
```
