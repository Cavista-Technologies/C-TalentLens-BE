# C-TalentLens API
 
Backend for C-TalentLens, a recruiting/ATS platform: requisition tracking, referrals, alerts/notifications, analytics, and leadership reporting. Built with ASP.NET Core (.NET 10) using a clean architecture layout and SQLite.
 
## Architecture
 
```
src/
  C-TalentLens.Domain          Entities and domain logic (no framework dependencies)
  C-TalentLens.Application     DTOs, service interfaces, security/access-scope abstractions
  C-TalentLens.Infrastructure  EF Core DbContext, services, migrations, background jobs
  C-TalentLens.Api             Controllers, Program.cs, Swagger, auth wiring
tests/
  C-TalentLens.Tests           Integration tests against an in-memory/SQLite test host
```
 
## Prerequisites
 
- .NET 10 SDK
 
## Running locally
 
```bash
dotnet run --project src/C-TalentLens.Api --launch-profile http
```
 
The API listens on `http://localhost:5144` (see `src/C-TalentLens.Api/Properties/launchSettings.json`). Swagger UI is available at `/swagger` in Development.
 
On Windows, stop a running `dotnet run` process before rebuilding — the running process locks the output DLLs.
 
## Configuration
 
Key settings in `src/C-TalentLens.Api/appsettings.json` (override per-environment in `appsettings.Development.json`):
 
| Section | Purpose |
|---|---|
| `ConnectionStrings:TalentLens` | SQLite database file path |
| `Jwt` | Access/refresh token issuer, audience, and expiry |
| `Cors:AllowedOrigins` | Origins allowed to call the API (includes the local frontend dev server) |
| `DemoData:Enabled` | Seeds demo users, requisitions, and referrals on startup when `true` |
| `SmartRecruiters` | Mock jobs file and default recruiter/hiring manager for the SmartRecruiters import |
| `BackgroundJobs` | Hangfire job toggles and cron schedules (alert sync, SmartRecruiters sync) |
 
## Database
 
EF Core migrations live in `src/C-TalentLens.Infrastructure/Migrations`. To add a new migration:
 
```bash
dotnet ef migrations add <Name> --project src/C-TalentLens.Infrastructure --startup-project src/C-TalentLens.Api
```
 
Migrations apply automatically on startup.
 
## Seeded demo accounts
 
When `DemoData:Enabled` is `true`, these accounts are seeded (password `Password123` for all):
 
| Email | Role |
|---|---|
| `maya.chen@talentlens.local` | Recruiter |
| `noah.bello@talentlens.local` | Recruiter |
| `ta.manager@talentlens.local` | Talent Acquisition Manager |
| `ada.okafor@talentlens.local`, `james.wright@talentlens.local`, `priya.shah@talentlens.local`, `ife.daniels@talentlens.local` | Hiring Manager |
| `leadership@talentlens.local` | Leadership |
 
## Tests
 
```bash
dotnet test
```
ASP.NET Core, an open-source web development framework | .NET
Build web apps and services that run on Windows, Linux, and macOS using C#, HTML, CSS, and JavaScript. Get started for free on Windows, Linux, or macOS.
 
# C-TalentLens
 
Frontend for C-TalentLens, a recruiting/ATS platform: requisitions, referrals, alerts/notifications, analytics, and leadership reporting. Built with React, TypeScript, and Vite.
 
## Architecture
 
```
src/
  pages/        Route-level page components (Dashboard, Requisitions, Referrals, Analytics, ...)
  features/     API clients and types grouped by domain (auth, requisitions, referrals, alerts, analytics, ...)
  components/   Shared UI (layout shell, feedback/toasts, ...)
  routes/       React Router route definitions and protected-route guard
  lib/          Fetch wrapper (apiClient), auth token storage, shared utilities
  styles/       Per-page and shared CSS
  config/       Runtime env config (env.ts)
```
 
## Prerequisites
 
- Node.js 20+
 
## Setup
 
```bash
npm install
cp .env.example .env   # then edit VITE_API_BASE_URL if needed
npm run dev
```
 
The dev server runs at `http://localhost:5173` by default and expects the [backend API](../C-TalentLens) running at `http://localhost:5144`.
 
If `.env` is missing, the app falls back to a deployed remote backend instead of localhost — always create `.env` from `.env.example` for local development.
 
## Scripts
 
| Command | Purpose |
|---|---|
| `npm run dev` | Start the Vite dev server |
| `npm run build` | Type-check (`tsc -b`) and build for production |
| `npm run lint` | Run ESLint |
| `npm run preview` | Preview a production build locally |
 
## Configuration
 
Environment variables (see `.env.example`):
 
| Variable | Purpose |
|---|---|
| `VITE_API_BASE_URL` | Base URL of the backend API |
| `VITE_CLOUDINARY_CLOUD_NAME` / `VITE_CLOUDINARY_UPLOAD_PRESET` | Unsigned Cloudinary upload config for referral resume uploads |
 
## Auth
 
Login issues an access token and a refresh token (see `src/lib/authToken.ts`, `src/features/auth`). The API client (`src/lib/apiClient.ts`) retries once on a 401 by refreshing, and a proactive timer in `AuthProvider` refreshes shortly before expiry so active sessions aren't interrupted. A failed refresh clears tokens and redirects to `/login` via `ProtectedRoute`.
 
## Demo accounts
 
See the backend README for seeded accounts (password `Password123` for all).
 
