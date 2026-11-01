# Nordiska Sparbanken (v2) — Customer Portal

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![React 18](https://img.shields.io/badge/React-18.3-61DAFB?logo=react)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-6.0-3178C6?logo=typescript)](https://www.typescriptlang.org/)
[![Vite](https://img.shields.io/badge/Vite-8.2-646CFF?logo=vite)](https://vitejs.dev/)
[![Tailwind CSS](https://img.shields.io/badge/Tailwind-4.3-38B2AC?logo=tailwindcss)](https://tailwindcss.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-4169E1?logo=postgresql)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)](https://www.docker.com/)
[![NuGet](https://img.shields.io/nuget/v/Nordiska.PdfEngine.Native?label=Nordiska.PdfEngine.Native)](https://www.nuget.org/packages/Nordiska.PdfEngine.Native)

Team 2's full refactoring of the Nordiska Sparbanken self-service portal from the legacy v1 Razor Pages monolith to a secure, scalable v2 architecture (.NET 8 Web API, React 18 SPA, PostgreSQL 15, and a native C/C++ PDF engine).

---

## Table of Contents

1. [What's New in v2](#whats-new-in-v2)
2. [Tech Stack](#tech-stack)
3. [Backend Overview (.NET 8 / C#)](#backend-overview-net-8--c)
4. [Project Structure](#project-structure)
5. [Prerequisites & Environment Variables](#prerequisites--environment-variables)
6. [Quickstart](#quickstart)
7. [Scripts & Tooling](#scripts--tooling)
8. [Service URLs & Test Accounts](#service-urls--test-accounts)
9. [Testing & Status](#testing--status)
10. [Architecture Decision Log (ADR)](#architecture-decision-log-adr)
11. [Known Limitations & Scope](#known-limitations--scope)
12. [Live Demo Fallback](#live-demo-fallback)
13. [Troubleshooting](#troubleshooting)

---

## What's New in v2

- **Immutable Ledger Accounting:** Eliminates v1 race conditions. Balances are dynamically computed via `SUM(SignedAmount)` using `Serializable` isolation for withdrawals and transfers.
- **Modern Auth & Security:** Replaces MD5 hashes and 365-day cookies with `BCrypt`, short-lived in-memory JWTs, `HttpOnly` refresh tokens, and a mocked BankID flow.
- **Native C/C++ PDF Engine (`Nordiska.PdfEngine.Native`):** Replaces blocking `Thread.Sleep` calls with a high-speed C/C++ engine (`libharu` + `OpenSSL`) for batch generation and cryptographic signing (`SHA-256` + `PKCS#1`) of tax reports and bank statements.
- **React 18 SPA & i18n:** Responsive Vite + React + TypeScript portal with full Swedish and English support (`Accept-Language`).
- **Rule-Based FAQ:** Database-backed FAQ search with keyword normalization, relevance scoring, and category filtering.
- **Observability & Protection:** Atomic database audit logging (`AuditEntry`) with a safety-net middleware, endpoint rate limiting, structured `ProblemDetails` error handling, and `/health` checks.

---

## Tech Stack

| Layer | Technologies | Responsibility |
|---|---|---|
| **Frontend** | React 18, TypeScript, Vite, Tailwind CSS, TanStack Query, React Router v7, Zustand, Axios, i18next | SPA with BankID/email login, dashboard, transfers, bank statements, savings goals, tax reports, and FAQ. |
| **Backend API** | .NET 8 (C#), ASP.NET Core Web API, EF Core 8, FluentValidation, Scalar | Layered REST API (`Controllers` → `Services` → `BankContext`), JWT auth, rate limiting, and audit trail. |
| **Database** | PostgreSQL 15 | Relational database managed via EF Core Code-First migrations. |
| **Native Engine** | C11 / C++17, CMake, libharu, OpenSSL 3.x, cJSON | High-throughput PDF generation and signing packaged as a NuGet library. |
| **Infrastructure** | Docker, Docker Compose, GitHub Actions | Containerized local development, CI/CD, and deployment. |

---

## Backend Overview (.NET 8 / C#)

*(For frontend details see [`frontend/mappstruktur.md`](frontend/mappstruktur.md); for native C/C++ details see [`native/README.md`](native/README.md)).*

### 1. Request Pipeline & Layered Architecture
Incoming requests pass through the ASP.NET Core middleware pipeline (exception handling, localization, CORS, rate limiting, JWT auth, and audit verification) before reaching controllers and services.

<img src="docs/images/backend-pipeline.png" alt="Backend Request Pipeline and Architecture" width="500" />

### 2. Transaction, Ledger & Audit Flow (Unit of Work)
Deposits are pure inserts, while withdrawals and transfers execute inside a `Serializable` database transaction. Business state changes and `AuditEntry` records share the same scoped `BankContext` and commit atomically in a single `SaveChangesAsync()` call.

<img src="docs/images/backend-transaction-flow.png" alt="Backend Transaction and Ledger Flow" width="500" />

### 3. PDF Generation & Signing Flow (.NET ↔ Native C/C++)
`PdfGeneratorService` serializes report models to JSON, ensures an X.509 signing certificate (`.pfx`) exists, and invokes `Nordiska.PdfEngine.Native` via P/Invoke to generate and cryptographically sign PDFs.

<img src="docs/images/backend-pdf-flow.png" alt="Backend PDF Generation Flow" width="420" />

---

## Project Structure

```text
nordiska-team2/
├── backend/
│   ├── NordiskaPortal.Api/          # .NET 8 Web API (Controllers, Services, Data, Models, Middleware)
│   ├── NordiskaPortal.Api.Tests/    # xUnit unit & integration tests
│   └── NordiskaPortal/              # Legacy v1 Razor Pages monolith (reference)
├── frontend/                        # React 18 + TypeScript + Vite SPA
├── infra/                           # Docker Compose configuration & .env.example
├── native/                          # C/C++ PDF Engine source & NuGet packaging
└── docs/                            # Project specifications & architecture diagrams (docs/images/)
```

---

## Prerequisites & Environment Variables

### Prerequisites
- **Git** & **Docker Desktop** (WSL 2 enabled on Windows)
- **.NET 8 SDK** & `dotnet-ef` CLI (`dotnet tool install --global dotnet-ef`)
- **Node.js** (v18/v20+) & `npm`

### Environment Files (`.env`)
Real `.env` files are git-ignored. Copy the provided `.env.example` templates before starting:

1. **`infra/.env`** (copied from [`infra/.env.example`](infra/.env.example)):
   ```env
   DB_PASSWORD=nordiska123
   JWT_KEY=dev_jwt_secret_key_minimum_32_characters_for_local_testing_only_123456
   CORS_ORIGIN=http://localhost:5173
   ```
   - `DB_PASSWORD`: PostgreSQL container password and API connection string password.
   - `JWT_KEY`: Signing key for JWT access tokens (minimum 32 chars).
   - `CORS_ORIGIN`: Allowed frontend origin.

2. **`frontend/.env`** (copied from [`frontend/.env.example`](frontend/.env.example)):
   ```env
   # Use http://localhost:8080/api for Docker Compose, or http://localhost:5063/api for local 'dotnet run'
   VITE_API_BASE_URL=http://localhost:8080/api
   ```

> [!NOTE]
> **Secrets Management:** `.env.example` files contain safe defaults for local testing only. Production/staging secrets are injected directly by the hosting platform (see [`DRIFT.md`](DRIFT.md)). When running the API locally without Docker, use `.NET User Secrets`.

---

## Quickstart

### Option A: Docker Compose (Recommended)

1. **Create `.env` files:**
   ```powershell
   # PowerShell
   Copy-Item infra\.env.example infra\.env
   Copy-Item frontend\.env.example frontend\.env
   ```
   ```bash
   # Bash
   cp infra/.env.example infra/.env && cp frontend/.env.example frontend/.env
   ```

2. **Start Backend & PostgreSQL:**
   ```bash
   cd infra
   docker compose up --build
   ```

3. **Start Frontend (in a separate terminal):**
   ```bash
   cd frontend
   npm install
   npm run dev
   ```

4. Open **[http://localhost:5173](http://localhost:5173)**.

---

### Option B: Hybrid Local Run (`.NET` + `Vite` on Host, `Postgres` in Docker)

1. **Start database container:**
   ```bash
   cd infra && docker compose up -d db
   ```
2. **Configure .NET User Secrets & run API:**
   ```bash
   cd ../backend/NordiskaPortal.Api
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=nordiska;Username=nordiska;Password=nordiska123"
   dotnet user-secrets set "Jwt:Key" "b8be980bdac31df37da51321f18abae2bdf4aa954531ca2f1a05a3e41f3c6bea"
   dotnet user-secrets set "Cors:AllowedOrigins:0" "http://localhost:5173"
   dotnet ef database update
   dotnet run
   ```
3. **Run Frontend:** Set `VITE_API_BASE_URL=http://localhost:5063/api` in `frontend/.env`, then run `npm install && npm run dev` inside `frontend/`.

---

## Scripts & Tooling

| Script | Path / Command | What It Does & When to Use It |
|---|---|---|
| **`docker.ps1`** (Windows) | `.\backend\NordiskaPortal.Api\Scripts\docker.ps1` | **Full DB & Docker Reset:** Reads `DB_PASSWORD` from `infra/.env`, recreates EF migrations (`InitialCreate`), runs `docker compose down -v`, rebuilds `app`, starts containers, and applies migrations. Run when migrations conflict or you want fresh seed data. |
| **`reset-db.sh`** (macOS/Linux/WSL) | `./backend/NordiskaPortal.Api/Scripts/reset-db.sh` | Bash equivalent of `docker.ps1`. |
| **Frontend scripts** | `cd frontend && npm run <script>` | `dev` (start Vite), `build` (typecheck + bundle), `lint` (ESLint), `test` (Vitest). |
| **Native scripts** | `native/scripts/` | `generate_mock_json.py` (synthetic benchmark data), `gen_logo.py` (logo asset embedding). |

---

## Service URLs & Test Accounts

### Service URLs
| Service | URL | Notes |
|---|---|---|
| **Frontend Portal** | `http://localhost:5173` | React SPA |
| **Backend API** | `http://localhost:8080` | (`http://localhost:5063` when running via `dotnet run`) |
| **Scalar API Docs** | `http://localhost:8080/scalar/v1` | Interactive OpenAPI documentation |
| **Health Check** | `http://localhost:8080/health` | API + PostgreSQL health probe |
| **PostgreSQL** | `localhost:5433` | User: `nordiska` / DB: `nordiska` |

### Seeded Test Accounts
Log in using **Email & Password** or **BankID (Demo)** (enter Personal ID + any 4-digit PIN):

| Name | Email | Password | Personal ID (BankID) | Seeded Accounts |
|---|---|---|---|---|
| **Anna Lindqvist** | `anna@example.com` | `password123` | `19850505-1234` | `NKM-10001` (Savings, 3.5%)<br>`NKM-10002` (Savings, 3.5%) |
| **Erik Johansson** | `erik@example.com` | `password123` | `19991212-5678` | `NKM-20001` (Savings, 3.5%) |

---

## Testing & Status

All automated test suites across Backend, Frontend, and Native pass.

- **Backend (`.NET xUnit`):** Unit and integration tests covering ledger calculations, concurrent withdrawals/transfers, JWT/BankID auth, account lifecycle, native PDF P/Invoke & error codes, atomic audit logging, FAQ scoring, and localization (`sv`/`en`).
  ```bash
  dotnet test backend/NordiskaPortal.Api.Tests/NordiskaPortal.Api.Tests.csproj
  ```
- **Frontend (`Vitest`):** Component and user-flow tests focusing on high-risk money movement modals (invalid amounts, insufficient balance, same-account prevention), bank statement filtering, FAQ, and UI building blocks.
  ```bash
  cd frontend && npm test -- --run
  ```
- **Native (`CTest`, `Valgrind`, `ASan`):** Unit tests for JSON streaming and PDF Engine API. Zero memory leaks in project source code, and benchmarked at **10,000 PDFs in ~17 seconds** in GitHub Actions (target: < 5 minutes).

---

## Architecture Decision Log (ADR)

| Area | Decision | Rationale |
|---|---|---|
| **Ledger & Concurrency** | Immutable Ledger (`SUM(SignedAmount)`) + `Serializable` isolation | Structurally prevents balance corruption. Deposits are append-only inserts; withdrawals/transfers detect concurrent overdraft attempts via PostgreSQL serialization. |
| **Audit Trail** | Shared Scoped `BankContext` + `AuditSafetyNetMiddleware` | Staging `AuditEntry` in the caller's unit of work guarantees atomic commits with the business operation. The middleware catches any state-changing endpoint missing an explicit audit call. |
| **PDF Engine** | C/C++ (`libharu` + `OpenSSL`) packaged as NuGet (`Nordiska.PdfEngine.Native`) | Delivers high-speed batch generation and PKCS#1 cryptographic signing while allowing the .NET backend to consume native binaries cleanly via P/Invoke. |
| **Authentication** | In-memory JWT + `HttpOnly` Refresh Token + `BCrypt` | Mitigates XSS token theft (no JWT in `localStorage`) and eliminates v1's vulnerable MD5 hashes and 1-year sessions. |

---

## Known Limitations & Scope

### Known Limitations
- **Backend:**
  - Tax report download endpoints (`/api/tax-reports/...`) currently lack `[Authorize]` ownership enforcement.
  - Uses a mocked BankID service rather than the official BankID API.
  - No admin role/UI outside the codebase, and no background cleanup worker for expired `RefreshTokens` in the database.
- **Frontend:**
  - Keyboard navigation and screen-reader accessibility (a11y) are not fully complete across all views.
  - Mobile responsiveness has minor gaps (dashboard quick-actions could be collapsed into a menu, and the logout button is hidden on small screens).
  - Customers can only open one savings account type, and transaction history renders without pagination.
- **Native:**
  - While our C/C++ code is leak-free, the underlying third-party signing library leaks 66 bytes per signed PDF (deemed low impact and out of scope to patch upstream).

### Customer Feedforward & Scope Prioritization
- **Completed (High Priority — v2 Core & Customer Value):**
  - **Bank Statements (PDF):** Implemented end-to-end across backend, native engine, and frontend.
  - **Audit Log:** Implemented with atomic DB persistence, safety-net middleware, and user-facing history in Settings.
  - **Notifications:** Asynchronous notification staging and background worker with exponential backoff retry.
  - **Dual Interest Visualization (Feedforward):** Added backend calculation and UI views for both **interest earned year-to-date** and **projected year-end interest**.
- **Delimited (Lower Priority / Out of MVP Scope):**
  - Dynamic FAQ admin input UI, senior citizen usability testing ("let grandma test the app"), and AI chatbot were deprioritized to ensure all core v2 deliverables and tests reached production quality on time.

---

## Live Demo Fallback

If the staging/production environment or network is unavailable during the live presentation:
1. **Offline Local Stack:** The entire system runs locally via Docker Compose (`docker compose up`) + `npm run dev`.
2. **One-Command Reset:** Running `docker.ps1` / `reset-db.sh` resets the database to a clean, predictable state with seeded customers and multi-year transactions in under a minute.
3. **Pre-Generated Tax Reports:** `TaxReportBackfillService` automatically generates and stores signed PDF tax reports at API startup for immediate download.

---

## Troubleshooting

- **"WSL 2 installation is incomplete" (Windows):** Run `wsl --install` in an elevated PowerShell, reboot, and restart Docker Desktop.
- **"Port is already allocated":** Stop conflicting local Postgres/web services or change ports in `infra/docker-compose.override.yml`.
- **"Jwt:Key is not configured":** Ensure `infra/.env` exists when using Docker, or configure `dotnet user-secrets` when running via `dotnet run`.
- **Database out of sync / "relation already exists":** Run `.\backend\NordiskaPortal.Api\Scripts\docker.ps1` (or `reset-db.sh`) to wipe the local volume and re-apply migrations.
