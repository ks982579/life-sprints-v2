# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Next Up - Phase 2: Authentication
- GitHub OAuth integration
- Backend authentication services and controllers
- Frontend auth context and protected routes

---

## Roadmap / TODO

### Phase 1: Docker & Local Development ✅ COMPLETED
- [x] Create backend Dockerfile (multi-stage build)
- [x] Create frontend Dockerfile (production build)
- [x] Create NGINX configuration and Dockerfile
- [x] Create `docker-compose.yml` for local development
- [x] Create `docker-compose.prod.yml` for production
- [x] Test full Docker environment
- [x] **Tag: v0.0.1 - Initial Setup Complete**

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

### [0.0.1] - 2025-12-30 - Initial Setup Complete

#### Added - Infrastructure
- Project monorepo structure with `src/backend`, `src/frontend`, `src/nginx`
- `.gitignore` for .NET, Node.js, and Docker
- `.env.example` template for environment variables
- `README.md` with comprehensive setup instructions
- `CHANGELOG.md` for version tracking

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
  - Initial database migration (`InitialCreate`)
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
  - PostgreSQL database integration

#### Added - Frontend (React + TypeScript + SWC)
- Vite project with React and TypeScript
- SWC compiler for faster builds (Rust-based)
- Development server configuration

#### Added - Docker & DevOps
- Backend Dockerfile (multi-stage: SDK → Runtime)
- Frontend Dockerfile (multi-stage: Node → NGINX)
- NGINX reverse proxy with:
  - API routing (`/api/*` → backend)
  - Frontend serving (`/` → frontend)
  - SSL/TLS configuration (ready for production)
  - Rate limiting and security headers
- `docker-compose.yml` for local development:
  - PostgreSQL 16 Alpine
  - Backend with SDK image and hot reload
  - Frontend with Vite dev server
  - NGINX reverse proxy
  - Test database (tmpfs for speed)
- `docker-compose.prod.yml` for production deployment:
  - Optimized builds
  - SSL/TLS support with certbot
  - Restart policies
- `.env` file for local configuration

#### Technical Details
- Framework: .NET 10 (SDK 10.0.101)
- Database: PostgreSQL 16
- ORM: Entity Framework Core 10.0.1
- Frontend: React 18 + TypeScript + SWC
- Build Tool: Vite
- Container Runtime: Docker
- Reverse Proxy: NGINX Alpine

#### Development Workflow
- Hot reload enabled for both backend and frontend
- Volume mounts for instant code changes
- Database migrations ready to apply
- Full local development environment with single `docker-compose up` command

---

## Notes

- **Current Version**: v0.0.1 - Initial setup complete ✅
- **Next Milestone**: v0.1.0 - GitHub OAuth authentication
- **Target for MVP**: v1.0.0 - Full authentication, CRUD, backlogs, and production deployment
- **Last Updated**: 2025-12-30
