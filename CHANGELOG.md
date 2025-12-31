# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Next Up - Phase 3: Core Activity CRUD
- Backend Activity CRUD API
- Frontend Activity components and management
- Activity state management and backlog filtering

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

### Phase 2: Authentication ✅ COMPLETED
- [x] GitHub OAuth integration
  - [x] Backend: GitHubAuthService
  - [x] Backend: AuthController with OAuth endpoints
  - [x] Backend: Session middleware
  - [x] Frontend: AuthContext and useAuth hook
  - [x] Frontend: Login page and ProtectedRoute component
- [x] E2E auth testing with Playwright
- [x] Test-only auth endpoint for E2E testing
- [x] **Tag: v0.1.0 - Authentication Complete**

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

### [0.1.0] - 2025-12-31 - Authentication Complete

#### Added - Backend Authentication
- GitHub OAuth integration:
  - `IAuthService` interface defining authentication contract
  - `GitHubAuthService` with full OAuth flow implementation
  - Token exchange with GitHub API
  - User creation and retrieval from GitHub data
- `AuthController` with OAuth endpoints:
  - `GET /api/auth/github/login` - Initiates GitHub OAuth flow with CSRF protection
  - `GET /api/auth/github/callback` - Handles OAuth callback and creates session
  - `POST /api/auth/logout` - Clears user session
  - `GET /api/auth/me` - Returns current authenticated user
  - `POST /api/auth/test-login` - Test-only endpoint for E2E testing (Development/Test only)
- Session-based authentication:
  - HTTP-only secure cookies
  - 30-day session expiration
  - CSRF token validation with state parameter
  - Cookie policy configuration (SameSite.Lax, Secure)
- DTOs for auth operations:
  - `CurrentUserDto` - User information response
  - `GitHubTokenResponse` - GitHub token exchange response
  - `GitHubUserResponse` - GitHub user API response
  - `TestLoginDto` - Test login request
- Unit tests for `GitHubAuthService`:
  - Test-driven development (TDD) approach
  - URL encoding validation
  - Session creation and expiration
  - InMemory database for testing
- NuGet packages added:
  - Microsoft.Extensions.Http 10.0.0 (HttpClient factory)

#### Added - Frontend Authentication
- Authentication context and hooks:
  - `AuthContext` with user state management
  - `useAuth()` hook for consuming auth state
  - Automatic user loading on application mount
  - Login/logout functions
- Components:
  - `LoginPage` - Beautiful gradient login UI with GitHub button
  - `ProtectedRoute` - Route guard component for authenticated pages
  - `Dashboard` - Protected dashboard with user info and backlog cards
- Services:
  - `api.ts` - Generic fetch wrapper with credentials support
  - `authService.ts` - Auth API client (login, logout, getCurrentUser)
- Type definitions:
  - `User` interface matching backend DTOs
  - `AuthContextType` for context typing
- Styling:
  - Responsive login page with gradient background
  - GitHub icon SVG
  - User avatar display
  - Professional card-based layout

#### Added - E2E Testing Infrastructure
- Playwright setup:
  - playwright.config.ts with Chromium browser
  - Test directory structure (`e2e/`)
  - HTML reporter configuration
  - CI-aware webServer configuration
- E2E test suite (`e2e/auth.spec.ts`):
  - ✅ Full login flow with test user and dashboard access
  - ✅ Login page displays correctly when not authenticated
  - ✅ API returns user data when authenticated with cookie sharing
  - ✅ API returns 401 when not authenticated
- Test scripts in package.json:
  - `npm run test:e2e` - Run tests headless
  - `npm run test:e2e:ui` - Run tests with Playwright UI
  - `npm run test:e2e:headed` - Run tests in headed browser
- Test auth endpoint implementation:
  - Creates test users with `test_` prefix
  - Auto-generates avatars using DiceBear API
  - Bypasses real GitHub OAuth for reliable testing
  - Only available in Development/Test environments

#### Fixed - Docker Configuration
- Backend startup race condition:
  - Added retry logic for database connection
  - Automatic migration application on startup
  - dotnet-ef tool installation in SDK container
- Backend port configuration:
  - Updated launchSettings.json to listen on 0.0.0.0:5000
  - Fixed NGINX proxy connection issues
- NGINX configuration:
  - Updated frontend upstream to port 3000 (Vite dev server)
  - Rebuild process for configuration changes

#### Technical Details
- Authentication: GitHub OAuth 2.0 with cookie-based sessions
- Session Storage: PostgreSQL database
- Frontend State Management: React Context API
- E2E Testing: Playwright with Chromium
- Test Users: Isolated with `test_` ID prefix
- CSRF Protection: State token validation
- Cookie Security: HTTP-only, Secure, SameSite=Lax

#### Development Workflow Improvements
- Test-driven development for authentication services
- Playwright E2E tests run against live Docker containers
- Test auth endpoint enables reliable E2E testing without external dependencies
- Hot reload maintained for both frontend and backend

---

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

- **Current Version**: v0.1.0 - Authentication complete ✅
- **Next Milestone**: v0.2.0 - Core Activity CRUD and Backlog Views
- **Target for MVP**: v1.0.0 - Full CRUD, backlogs, testing, and production deployment
- **Last Updated**: 2025-12-31
