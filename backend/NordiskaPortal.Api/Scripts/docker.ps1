# Resets the local Postgres container and re-applies all migrations from
# scratch. Run this after pulling changes that touched Migrations/, or
# whenever your local database and the migration files seem out of sync
# (e.g. "relation already exists" errors on dotnet ef database update).
#
# WARNING: this deletes all local data in the nordiska database. Never
# run this against anything other than your own local dev environment.
#
# Located at backend/NordiskaPortal.Api/Scripts/reset-db.ps1
#
# Requires: infra/.env with DB_PASSWORD set (same file Docker Compose uses).
# No User Secrets setup needed — this script reads .env directly and
# passes the connection string explicitly to dotnet ef.

Write-Host ""
Write-Host "This will PERMANENTLY DELETE all local data in the nordiska database" -ForegroundColor Yellow
Write-Host "(Docker volume removed, then migrations reapplied from scratch)." -ForegroundColor Yellow
Write-Host ""
$confirmation = Read-Host "Type 'yes' to continue, anything else to cancel"

if ($confirmation -ne "yes") {
    Write-Host "Cancelled. No changes made." -ForegroundColor Cyan
    exit
}

# --- Read DB_PASSWORD from infra/.env so this script needs no local secrets setup ---
$envPath = "$PSScriptRoot\..\..\..\infra\.env"
if (-not (Test-Path $envPath)) {
    Write-Host "ERROR: Could not find .env at $envPath" -ForegroundColor Red
    exit 1
}

$dbPassword = (Get-Content $envPath | Where-Object { $_ -match '^DB_PASSWORD=' }) -replace '^DB_PASSWORD=', ''

if ([string]::IsNullOrWhiteSpace($dbPassword)) {
    Write-Host "ERROR: DB_PASSWORD not found in $envPath" -ForegroundColor Red
    exit 1
}

$localConnectionString = "Host=localhost;Port=5433;Database=nordiska;Username=nordiska;Password=$dbPassword"

Write-Host "Deleting existing migrations..."
Set-Location "$PSScriptRoot\..\..\..\backend\NordiskaPortal.Api"
Remove-Item -Path "Migrations" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Generating a fresh InitialCreate migration..."
dotnet ef migrations add InitialCreate

Write-Host "Stopping and removing the local database volume..."
Set-Location "$PSScriptRoot\..\..\..\infra"
docker compose down -v

Write-Host "Rebuilding the local database volume..."
docker compose build --no-cache app

Write-Host "Starting fresh containers..."
docker compose up -d

Write-Host "Waiting for Postgres to be ready..."
Start-Sleep -Seconds 5

Write-Host "Applying migrations..."
Set-Location "$PSScriptRoot\.."
dotnet ef database update --connection "$localConnectionString"

Write-Host ""
Write-Host "Done. The application is running via Docker at http://localhost:8080/scalar/v1" -ForegroundColor Green
Write-Host "No further steps needed - do not run 'dotnet run' separately." -ForegroundColor Green