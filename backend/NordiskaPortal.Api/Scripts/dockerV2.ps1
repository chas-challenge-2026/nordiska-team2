# Sets up / resets the local Docker environment for Nordiska Portal.
#
#   1. Checks prerequisites (Docker, .NET 8 SDK, dotnet-ef) and offers to install what's missing.
#   2. Deletes the database volume and rebuilds all containers:
#        - the bank API and database  (docker-compose.yml + docker-compose.override.yml)
#        - the AI assistant + Ollama  (docker-compose.assistant.yml, optional)
#        - the frontend               (docker-compose.frontend.yml, optional)
#        - real test BankID           (docker-compose.bankid.yml, only with -RealBankId)
#   3. Waits until everything answers, and re-applies the COMMITTED migrations.
#   4. With -RealBankId: links your test BankID's personal number to a customer again
#      (the database reset removes that link every time).
#
# Never generates or deletes migrations. The AI model is kept between resets.
#
# WARNING: deletes all local data in the nordiska database. Local dev only.
#
# Usage (from anywhere):
#   .\dockerV2.ps1                                 # everything
#   .\dockerV2.ps1 -SkipAssistant                  # no AI assistant (faster, no model download)
#   .\dockerV2.ps1 -SkipFrontend                   # no frontend container (use 'npm run dev' instead)
#   .\dockerV2.ps1 -SkipAssistant -SkipFrontend    # bank API and database only
#   .\dockerV2.ps1 -RealBankId                     # everything, with REAL test BankID instead of the mock
#
# Location: backend/NordiskaPortal.Api/Scripts/dockerV2.ps1
#
# ===========================================================================
# CHECKLIST BEFORE THE FIRST RUN
# [auto] = the script checks it and stops (or warns) if it's wrong.
# [you]  = the script can't check it; verify it yourself.
# ===========================================================================
#
# Tools
#   [auto] Docker Desktop installed and RUNNING (whale icon steady, not animating).
#   [auto] .NET 8 SDK installed (offers to install it with winget).
#   [auto] dotnet-ef 8.x installed (installed automatically if missing).
#   [you]  Docker Desktop has at least 8 GB memory (Settings > Resources).
#          Ollama alone is limited to 6 GB; with less, the AI model can fail to load.
#   [you]  These ports are free: 8080 (API), 5080 (assistant), 8081 (frontend), 5433 (Postgres).
#          A 'dotnet run' API uses 5063 and an 'npm run dev' frontend uses 5173; those are fine.
#
# infra/.env (never commit this file; values are per machine)
#   [auto] infra/.env exists.
#   [auto] DB_PASSWORD is set.
#   [auto] JWT_KEY is set (when the assistant is included).
#   [you]  JWT_KEY is EXACTLY the key the bank API signs tokens with. If the two
#          differ, every AI assistant call returns 401.
#   [you]  JWT_KEY is at least 32 characters (HMAC-SHA256 needs a 256-bit key).
#   [auto] CORS_ORIGIN is set (when the assistant is included).
#   [you]  CORS_ORIGIN has NO trailing slash: 'http://localhost:5173/' never matches.
#   [you]  Optional: ASSISTANT_MODEL (default qwen2.5:3b) and OLLAMA_CPUS (default 2).
#
# CORS: which frontend address is allowed
#   The API and the assistant only accept browser calls from the origins they're told about.
#     - Docker frontend:   http://localhost:8081
#     - 'npm run dev':     http://localhost:5173
#   [auto] Warns if the Docker frontend's origin (8081) isn't allowed anywhere.
#   [you]  To allow BOTH, keep CORS_ORIGIN=http://localhost:5173 and add a second origin
#          in the compose files, in the app service AND the assistant service:
#              Cors__AllowedOrigins__1: http://localhost:8081
#
# Docker files
#   [you]  infra/docker-compose.assistant.yml declares the model volume as external:
#              volumes:
#                ollama-data:
#                  external: true
#                  name: ollama-data
#          Otherwise every reset deletes and re-downloads the model (~2 GB).
#          (The script creates the volume if it's missing.)
#   [you]  The frontend Dockerfile declares 'ARG VITE_API_BASE_URL' and
#          'ARG VITE_ASSISTANT_BASE_URL' BEFORE 'npm run build'. VITE_ values are baked in
#          at build time; without the ARG lines the build args are silently ignored.
#
# Git
#   [you]  Pull the latest main first, so you reset to the team's migrations.
#   [you]  After the run, 'git status' shows NO changes in Migrations/.
#          This script never generates migrations; changes there came from elsewhere.
#
# BankID
#   Without -RealBankId, Docker runs the BankID MOCK. The Docker API runs as Production,
#   where the mock's "simulate the phone" step is switched off, so BankID login can't be
#   completed against Docker; use e-mail login there.
#
#   With -RealBankId (real BankID TEST environment), you also need:
#   [you]  BankID's test client certificate (.p12), downloaded OUTSIDE the repo
#          (bankid.com > Developers > Test > "Certificate for test").
#   [you]  A BankID app configured for test, with a test BankID in it
#          (PC: CavaServerSelector.txt with "kundtest"; see the backend doc, section 11).
#   [auto] infra/docker-compose.bankid.yml exists.
#   [auto] infra/.env has BANKID_CERT_PATH (and the file exists), BANKID_CERT_PASSWORD and
#          BANKID_TEST_PERSONAL_ID (your test BankID's personal number, YYYYMMDD-XXXX).
#          Optional: BANKID_TEST_CUSTOMER_EMAIL (default erik@example.com).
#   [auto] The public CA file is in backend/NordiskaPortal.Api/Certs/.
#   [auto] Links BANKID_TEST_PERSONAL_ID to that customer after every reset.
#   [you]  If the API doesn't start, the script prints its last log lines. A certificate
#          problem shows up there (wrong path, wrong password, missing CA).
# ===========================================================================

param(
    [switch]$SkipAssistant,
    [switch]$SkipFrontend,
    [switch]$RealBankId
)

$ErrorActionPreference = "Stop"

$repoRoot     = (Resolve-Path "$PSScriptRoot\..\..\..").Path
$apiDir       = Join-Path $repoRoot "backend\NordiskaPortal.Api"
$infraDir     = Join-Path $repoRoot "infra"
$envPath      = Join-Path $infraDir ".env"
$apiUrl       = "http://localhost:8080"
$assistantUrl = "http://localhost:5080"
$frontendUrl  = "http://localhost:8081"

# Ollama's model volume is EXTERNAL (see docker-compose.assistant.yml):
# 'docker compose down -v' never deletes it, so the model survives every database reset.
$ollamaVolume = "ollama-data"

# ---------------------------------------------------------------------------
# Compose files
# Passing -f switches OFF Docker Compose's automatic loading of docker-compose.override.yml,
# so it must be listed explicitly. The override holds the local port mappings (8080:8080).
# ---------------------------------------------------------------------------

$hasOverride = Test-Path (Join-Path $infraDir "docker-compose.override.yml")
$hasFrontend = Test-Path (Join-Path $infraDir "docker-compose.frontend.yml")
$hasBankId   = Test-Path (Join-Path $infraDir "docker-compose.bankid.yml")

$baseFiles = @("-f", "docker-compose.yml")
if ($hasOverride) { $baseFiles += @("-f", "docker-compose.override.yml") }
$assistantFiles = @("-f", "docker-compose.assistant.yml")
$frontendFiles  = @()
if ($hasFrontend) { $frontendFiles = @("-f", "docker-compose.frontend.yml") }

$includeAssistant = -not $SkipAssistant
$includeFrontend  = (-not $SkipFrontend) -and $hasFrontend

$composeArgs = $baseFiles
if ($includeAssistant) { $composeArgs += $assistantFiles }
if ($includeFrontend)  { $composeArgs += $frontendFiles }
if ($RealBankId)       { $composeArgs += @("-f", "docker-compose.bankid.yml") }

# 'down' includes every add-on that starts containers of its own, so containers from an
# earlier run with more add-ons are stopped too, whatever this run includes.
# The BankID file is left out on purpose: it adds no containers (it only changes 'app'),
# and it requires BANKID_* values that developers without a certificate don't have.
$downArgs = $baseFiles + $assistantFiles + $frontendFiles

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

# Runs a native command and stops the script if it fails.
function Invoke-Native([string]$description, [scriptblock]$command) {
    Write-Host $description
    $global:LASTEXITCODE = 0
    & $command
    if ($LASTEXITCODE -ne 0) { throw "Failed: $description (exit code $LASTEXITCODE)" }
}

# True if a native command succeeds, without printing its output.
function Test-Command([scriptblock]$command) {
    try {
        $global:LASTEXITCODE = 0
        & $command *> $null
        return $LASTEXITCODE -eq 0
    } catch { return $false }
}

# Asks before installing anything with winget.
function Install-WithWinget([string]$name, [string]$wingetId) {
    if (-not (Test-Command { winget --version })) {
        throw "$name is missing and winget isn't available. Install $name manually, then run this script again."
    }
    if ((Read-Host "$name is missing. Install it with winget now? (yes/no)") -ne "yes") {
        throw "$name is required. Install it, then run this script again."
    }
    Invoke-Native "Installing $name..." { winget install --id $wingetId --exact --accept-source-agreements --accept-package-agreements }
}

# Reads KEY=value from infra/.env (empty string if missing).
# If a key appears more than once, the last line wins and a warning is shown.
function Get-EnvValue([string[]]$lines, [string]$key) {
    $found = @($lines | Where-Object { $_ -match "^\s*$key\s*=" })
    if ($found.Count -eq 0) { return "" }
    if ($found.Count -gt 1) {
        Write-Host "WARNING: $key appears $($found.Count) times in infra/.env; using the last one. Remove the duplicates." -ForegroundColor Yellow
    }
    $value = ($found[-1] -replace "^\s*$key\s*=", "").Trim()
    return $value.Trim('"', "'")
}

# Polls a URL until it answers.
#   -RequireOk: only HTTP 200 counts (for /health and the frontend's start page).
#   otherwise:  any answer below 500 counts (the server is up, even if the path needs a token).
function Wait-ForUrl([string]$name, [string]$url, [int]$timeoutMinutes, [switch]$RequireOk) {
    Write-Host "Waiting for $name ($url)..."
    $deadline = (Get-Date).AddMinutes($timeoutMinutes)
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest $url -UseBasicParsing -TimeoutSec 3
            if (-not $RequireOk -or $response.StatusCode -eq 200) { return }
        } catch {
            $errorResponse = $_.Exception.Response
            if (-not $RequireOk -and $null -ne $errorResponse -and [int]$errorResponse.StatusCode -lt 500) { return }
        }
        Start-Sleep -Seconds 2
    }
    throw "$name did not respond within $timeoutMinutes minutes. Check (from infra/): docker compose $($composeArgs -join ' ') logs"
}

# ---------------------------------------------------------------------------
# 1. Prerequisites
# ---------------------------------------------------------------------------

Write-Host ""
Write-Host "Checking prerequisites..." -ForegroundColor Cyan

# Docker Desktop: installing it needs admin rights, WSL2 and usually a restart,
# so the script only points the way instead of installing it silently.
if (-not (Test-Command { docker --version })) {
    Write-Host "Docker Desktop is not installed." -ForegroundColor Red
    Write-Host "Install it (needs admin and a restart):  winget install --id Docker.DockerDesktop --exact"
    exit 1
}
if (-not (Test-Command { docker info })) {
    Write-Host "Docker is installed but not running. Start Docker Desktop, wait until it's ready, and run this again." -ForegroundColor Red
    exit 1
}

# .NET 8 SDK (needed for dotnet-ef and the migrations step)
$hasNet8 = (Test-Command { dotnet --version }) -and ((dotnet --list-sdks) -match '^8\.')
if (-not $hasNet8) {
    Install-WithWinget ".NET 8 SDK" "Microsoft.DotNet.SDK.8"
    Write-Host "Open a NEW PowerShell window so the new SDK is on the PATH, then run this script again." -ForegroundColor Yellow
    exit 0
}

# dotnet-ef, pinned to 8.x to match the project (a newer major version can refuse net8 projects)
if (-not (Test-Command { dotnet ef --version })) {
    Invoke-Native "Installing dotnet-ef 8.x (global tool)..." { dotnet tool install --global dotnet-ef --version "8.*" }
}

# infra/.env and the values this setup needs
if (-not (Test-Path $envPath)) { throw "Could not find .env at $envPath (copy infra/.env.example if there is one)." }
$envLines   = Get-Content $envPath
$dbPassword = Get-EnvValue $envLines "DB_PASSWORD"
if ([string]::IsNullOrWhiteSpace($dbPassword)) { throw "DB_PASSWORD not found in $envPath" }

if ($includeAssistant) {
    # JWT_KEY must be the SAME key the bank API signs tokens with, or the assistant rejects every call (401).
    foreach ($key in @("JWT_KEY", "CORS_ORIGIN")) {
        if ([string]::IsNullOrWhiteSpace((Get-EnvValue $envLines $key))) {
            throw "$key not found in $envPath (needed by the AI assistant). Or run with -SkipAssistant."
        }
    }
    $assistantModel = Get-EnvValue $envLines "ASSISTANT_MODEL"
    if ([string]::IsNullOrWhiteSpace($assistantModel)) { $assistantModel = "qwen2.5:3b" }
}

# Frontend: missing file is not an error (the frontend can run with 'npm run dev'), just a note.
if (-not $SkipFrontend -and -not $hasFrontend) {
    Write-Host "Note: infra/docker-compose.frontend.yml not found; starting without the frontend container." -ForegroundColor Yellow
}

# Frontend: warn if its origin isn't allowed by CORS anywhere. Not fatal; e-mail login and the
# API itself still work, but every browser call from http://localhost:8081 would be blocked.
if ($includeFrontend) {
    $corsOrigin = Get-EnvValue $envLines "CORS_ORIGIN"
    $composeText = ($composeArgs | Where-Object { $_ -like "*.yml" } |
        ForEach-Object { Get-Content (Join-Path $infraDir $_) -Raw }) -join "`n"
    $allowedInCompose = $composeText -match 'Cors__AllowedOrigins__\d+\s*:\s*"?http://localhost:8081'
    if ($corsOrigin -ne $frontendUrl -and -not $allowedInCompose) {
        Write-Host "WARNING: $frontendUrl is not an allowed CORS origin (CORS_ORIGIN=$corsOrigin)." -ForegroundColor Yellow
        Write-Host "         The Docker frontend's calls to the API and assistant will be blocked." -ForegroundColor Yellow
        Write-Host "         Fix: add 'Cors__AllowedOrigins__1: $frontendUrl' to the app and assistant services." -ForegroundColor Yellow
    }
}

if ($RealBankId) {
    if (-not $hasBankId) { throw "-RealBankId needs infra/docker-compose.bankid.yml, which wasn't found." }

    $bankIdCertPath = Get-EnvValue $envLines "BANKID_CERT_PATH"
    if ([string]::IsNullOrWhiteSpace($bankIdCertPath)) {
        throw "BANKID_CERT_PATH not found in $envPath (path to your BankID test .p12, with forward slashes)."
    }
    if (-not (Test-Path $bankIdCertPath)) {
        throw "BANKID_CERT_PATH points to '$bankIdCertPath', which doesn't exist."
    }
    if ([string]::IsNullOrWhiteSpace((Get-EnvValue $envLines "BANKID_CERT_PASSWORD"))) {
        throw "BANKID_CERT_PASSWORD not found in $envPath (from the certificate zip's info file)."
    }

    # The personal number and e-mail go into a SQL statement, so only accept the expected formats.
    $bankIdPersonalId = Get-EnvValue $envLines "BANKID_TEST_PERSONAL_ID"
    if ($bankIdPersonalId -notmatch '^\d{8}-?\d{4}$') {
        throw "BANKID_TEST_PERSONAL_ID in $envPath must be your test BankID's personal number (YYYYMMDD-XXXX)."
    }
    $bankIdCustomerEmail = Get-EnvValue $envLines "BANKID_TEST_CUSTOMER_EMAIL"
    if ([string]::IsNullOrWhiteSpace($bankIdCustomerEmail)) { $bankIdCustomerEmail = "erik@example.com" }
    if ($bankIdCustomerEmail -notmatch '^[^@\s''"]+@[^@\s''"]+$') {
        throw "BANKID_TEST_CUSTOMER_EMAIL in $envPath doesn't look like an e-mail address."
    }

    $caFile = Join-Path $apiDir "Certs\Test-BankID-SSL-Root-CA-v1-Test.pem"
    if (-not (Test-Path $caFile)) { throw "BankID's CA file is missing: $caFile" }
}

Write-Host "Prerequisites OK." -ForegroundColor Green

$localConnectionString = "Host=localhost;Port=5433;Database=nordiska;Username=nordiska;Password=$dbPassword"

# ---------------------------------------------------------------------------
# Confirmation
# ---------------------------------------------------------------------------

Write-Host ""
Write-Host "This will PERMANENTLY DELETE all local data in the nordiska database" -ForegroundColor Yellow
Write-Host "(database volume removed, containers rebuilt, committed migrations reapplied)." -ForegroundColor Yellow
if ($includeAssistant) { Write-Host "The downloaded AI model is kept." -ForegroundColor Yellow }
Write-Host ""
if ((Read-Host "Type 'yes' to continue, anything else to cancel") -ne "yes") {
    Write-Host "Cancelled. No changes made." -ForegroundColor Cyan
    exit
}

# ---------------------------------------------------------------------------
# 2. Reset and rebuild containers
# ---------------------------------------------------------------------------

if ($includeAssistant -and -not (Test-Command { docker volume inspect $ollamaVolume })) {
    Invoke-Native "Creating the volume for AI models ($ollamaVolume)..." { docker volume create $ollamaVolume }
}

Push-Location $infraDir
try {
    # The external model volume is not affected by -v.
    Invoke-Native "Stopping containers and removing the database volume..." { docker compose @downArgs down -v --remove-orphans }

    if ($includeAssistant) {
        Write-Host "The first run downloads the AI model ($assistantModel, ~2 GB); later runs only check it." -ForegroundColor Cyan
    }
    if ($includeFrontend) {
        Write-Host "The frontend is built with the API and assistant addresses baked in (VITE_ build args)." -ForegroundColor Cyan
    }
    # 'up' waits for ollama to be healthy and for ollama-pull to finish before starting the assistant,
    # and fails if the model download fails.
    Invoke-Native "Rebuilding images and starting containers..." { docker compose @composeArgs up -d --build }
}
finally { Pop-Location }

# ---------------------------------------------------------------------------
# 3. Health checks and migrations
# ---------------------------------------------------------------------------

# The API runs pending migrations itself at startup, so once /health answers, startup is done.
# If it never answers, show its last log lines: startup errors (e.g. a BankID certificate
# problem with -RealBankId) appear there.
try {
    Wait-ForUrl "the API" "$apiUrl/health" 3 -RequireOk
}
catch {
    Write-Host ""
    Write-Host "Last lines of the API's log:" -ForegroundColor Yellow
    Push-Location $infraDir
    try { docker compose @composeArgs logs app --tail 40 } finally { Pop-Location }
    throw
}

# Re-applies the committed migrations. A no-op if the API already applied them.
# Runs only after the API is healthy, so it never races with the API's own migration.
Push-Location $apiDir
try {
    Invoke-Native "Applying committed migrations..." { dotnet ef database update --connection $localConnectionString }
}
finally { Pop-Location }

# Real BankID: the reset gave every customer back their seeded personal number, so link
# your test BankID to the chosen customer again. The SQL goes in via stdin, so no quoting issues.
if ($RealBankId) {
    $sql = "UPDATE ""Customers"" SET ""PersonalId"" = '$bankIdPersonalId' WHERE ""Email"" = '$bankIdCustomerEmail';"
    Push-Location $infraDir
    try {
        Write-Host "Linking test BankID $bankIdPersonalId to $bankIdCustomerEmail..."
        $global:LASTEXITCODE = 0
        $result = $sql | docker compose @composeArgs exec -T db psql -U nordiska -d nordiska -v ON_ERROR_STOP=1
        if ($LASTEXITCODE -ne 0) { throw "Linking the test BankID failed (psql exit code $LASTEXITCODE)." }
        if (-not ($result -match '^UPDATE 1$')) {
            throw "No customer with e-mail $bankIdCustomerEmail; check BANKID_TEST_CUSTOMER_EMAIL in infra/.env."
        }
    }
    finally { Pop-Location }
}

if ($includeAssistant) {
    Wait-ForUrl "the AI assistant" $assistantUrl 2
}

if ($includeFrontend) {
    Wait-ForUrl "the frontend" $frontendUrl 2 -RequireOk
}

Write-Host ""
Write-Host "Done." -ForegroundColor Green
if ($includeFrontend)  { Write-Host "  Frontend:     $frontendUrl" }
Write-Host "  API:          $apiUrl/scalar/v1"
if ($includeAssistant) { Write-Host "  AI assistant: $assistantUrl  (model: $assistantModel)" }
if (-not $includeFrontend) {
    Write-Host "  Frontend:     not in Docker this run; use 'npm run dev' with VITE_API_BASE_URL=$apiUrl/api"
}
if ($RealBankId) {
    Write-Host "  BankID:       REAL test environment; log in as $bankIdCustomerEmail with your test BankID"
} else {
    Write-Host "  BankID:       mock (BankID login can't be completed in Docker; use e-mail login)"
}
Write-Host "Do not run 'dotnet run' as well; the API is already running in Docker." -ForegroundColor Green