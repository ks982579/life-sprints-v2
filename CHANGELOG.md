# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Pre-Release - Project Setup

#### Added - Infrastructure
- Project monorepo structure with `src/backend`, `src/frontend`, `src/nginx`
- `.gitignore` for .NET, Node.js, and Docker
- `.env.example` template for environment variables
- `README.md` with comprehensive setup instructions

#### Added - Backend (.NET 10)
- .NET solution with 4 projects:
  - `LifeSprint.Api` - ASP.NET Core Web API
  - `LifeSprint.Core` - Domain models and interfaces
  - `LifeSprint.Infrastructure` - EF Core, repositories, services
  - `LifeSprint.Tests` - xUnit tests (unit + integration)
- Database models:
  - `Activity` - Task/project entity with hierarchy and backlog flags
  - `User` - GitHub OAuth user entity
  - `Session` - Authentication session entity
- Entity Framework Core setup:
  - `AppDbContext` with full entity configuration
  - Automatic timestamp management (CreatedAt, UpdatedAt)
  - Indexes for performance optimization
  - Initial database migration
- NuGet packages:
  - Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0
  - Microsoft.EntityFrameworkCore.Design 10.0.1
  - Moq 4.20.72 (testing)
  - FluentAssertions 8.8.0 (testing)
  - Microsoft.EntityFrameworkCore.InMemory 10.0.1 (testing)
- API configuration:
  - CORS setup for frontend communication
  - Controller support
  - Connection string configuration

#### Added - Frontend (React + TypeScript + SWC)
- Vite project with React and TypeScript
- SWC compiler for faster builds (Rust-based)

---

## Roadmap / TODO

### Phase 1: Docker & Local Development (Next Up)
- [ ] Create backend Dockerfile (multi-stage build)
- [ ] Create frontend Dockerfile (production build)
- [ ] Create NGINX configuration and Dockerfile
- [ ] Create `docker-compose.yml` for local development
- [ ] Create `docker-compose.prod.yml` for production
- [ ] Test full Docker environment
- [ ] **Tag: v0.0.1 - Initial Setup Complete**

### Phase 2: Authentication (Week 2)
- [ ] GitHub OAuth integration
  - [ ] Backend: GitHubAuthService
  - [ ] Backend: AuthController with OAuth endpoints
  - [ ] Backend: Session middleware
  - [ ] Frontend: AuthContext and useAuth hook
  - [ ] Frontend: Login page and ProtectedRoute component
- [ ] E2E auth testing with Playwright

### Phase 3: Core Activity CRUD (Week 2)
- [ ] Backend API:
  - [ ] ActivityRepository implementation
  - [ ] ActivityService with business logic
  - [ ] ActivitiesController with all CRUD endpoints
  - [ ] Backlog filtering endpoints (annual/monthly/weekly/daily)
- [ ] Frontend Components:
  - [ ] ActivityCard component
  - [ ] ActivityForm component
  - [ ] ActivityList component
  - [ ] useActivities custom hook
  - [ ] activityService API client
- [ ] Backend unit tests for ActivityService
- [ ] Backend integration tests for ActivityRepository
- [ ] Frontend component tests

### Phase 4: Backlog Views (Week 2)
- [ ] Frontend routing setup (React Router)
- [ ] AnnualBacklog component
- [ ] MonthlyBacklog component
- [ ] WeeklySprint component
- [ ] DailyChecklist component (optional)
- [ ] MainLayout with navigation
- [ ] Header with user info and logout
- [ ] Sidebar with backlog navigation

### Phase 5: Testing Infrastructure (Week 3)
- [ ] Playwright setup and configuration
- [ ] E2E tests for activity management
- [ ] E2E tests for backlog filtering
- [ ] E2E tests for activity state changes
- [ ] Frontend unit tests with Vitest
- [ ] Test coverage reporting

### Phase 6: CI/CD Pipeline (Week 4)
- [ ] GitHub Actions workflow:
  - [ ] Backend unit tests job
  - [ ] Backend integration tests job
  - [ ] Frontend tests job
  - [ ] E2E tests job
  - [ ] Build and push Docker images
  - [ ] Deploy to Digital Ocean job
- [ ] GitHub Container Registry setup
- [ ] Configure GitHub secrets

### Phase 7: Production Deployment (Week 4)
- [ ] Provision Digital Ocean droplet
- [ ] Configure DNS (life-sprint.sullivansoftware.dev)
- [ ] Set up SSL with Let's Encrypt
- [ ] Create production GitHub OAuth app
- [ ] Initial production deployment
- [ ] Verify auto-deployment on push to main

### Phase 8: Polish & Enhancements (Week 5)
- [ ] Error handling and user feedback
- [ ] Loading states and skeletons
- [ ] Form validation
- [ ] Toast notifications
- [ ] Responsive design improvements
- [ ] Drag & drop for moving activities between backlogs (optional)
- [ ] Activity search and filtering
- [ ] Performance optimization

### Future Enhancements (Post-MVP)
- [ ] Activity statistics and charts
- [ ] Sprint retrospectives
- [ ] Activity templates
- [ ] Bulk operations
- [ ] Export functionality (CSV, JSON)
- [ ] Mobile app (React Native)
- [ ] Team collaboration features
- [ ] Activity comments and attachments
- [ ] Notifications and reminders
- [ ] Dark mode theme

---

## Version History

### [0.0.1] - TBD - Initial Setup
- Initial project setup complete
- Backend structure with database models
- Frontend initialized with Vite + React + TypeScript
- Docker development environment ready

---

## Notes

- **Current Status**: Pre-release - Setting up development environment
- **Next Milestone**: v0.0.1 - Complete Docker setup and verify local dev environment
- **Target for MVP**: v1.0.0 - Full authentication, CRUD, backlogs, and production deployment
