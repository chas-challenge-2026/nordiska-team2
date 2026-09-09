# Resets the local Postgres container and re-applies all migrations from
# scratch. Run this after pulling changes that touched Migrations/, or
# whenever your local database and the migration files seem out of sync
# (e.g. "relation already exists" errors on dotnet ef database update).
#
# WARNING: this deletes all local data in the nordiska database AND
# deletes and regenerates the migration files themselves. Never run
# this against anything other than your own local dev environment.

Write-Host ""
Write-Host "This will PERMANENTLY DELETE all local data in the nordiska database" -ForegroundColor Yellow
Write-Host "AND regenerate the Migrations folder from scratch." -ForegroundColor Yellow
Write-Host ""
$confirmation = Read-Host "Type 'yes' to continue, anything else to cancel"

if ($confirmation -ne "yes") {
    Write-Host "Cancelled. No changes made." -ForegroundColor Cyan
    exit
}

Write-Host "Deleting existing migrations..."
Set-Location "$PSScriptRoot\.."
Remove-Item -Path "Migrations" -Recurse -Force

Write-Host "Generating a fresh InitialCreate migration..."
dotnet ef migrations add InitialCreate

Write-Host "Stopping and removing the local database volume..."
Set-Location "$PSScriptRoot\..\..\..\infra"
docker compose down -v

Write-Host "Rebuilding the app image..."
docker compose build --no-cache app

Write-Host "Starting a fresh database container..."
docker compose up -d

Write-Host "Waiting for Postgres to be ready..."
Start-Sleep -Seconds 5

Write-Host "Applying migrations..."
Set-Location "$PSScriptRoot\..\..\..\backend\NordiskaPortal.Api"
dotnet ef database update

Write-Host "Done. Run 'dotnet run' to start the API." -ForegroundColor Green