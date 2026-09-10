# Resets the local Postgres container and re-applies all migrations from
# scratch. Run this after pulling changes that touched Migrations/, or
# whenever your local database and the migration files seem out of sync
# (e.g. "relation already exists" errors on dotnet ef database update).
#
# WARNING: this deletes all local data in the nordiska database. Never
# run this against anything other than your own local dev environment.
#
# Located at backend/NordiskaPortal.Api/Scripts/reset-db.ps1

Write-Host ""
Write-Host "This will PERMANENTLY DELETE all local data in the nordiska database" -ForegroundColor Yellow
Write-Host "(Docker volume removed, then migrations reapplied from scratch)." -ForegroundColor Yellow
Write-Host ""
$confirmation = Read-Host "Type 'yes' to continue, anything else to cancel"

if ($confirmation -ne "yes") {
    Write-Host "Cancelled. No changes made." -ForegroundColor Cyan
    exit
}
 
Write-Host ""
Write-Host "This will PERMANENTLY DELETE all local data in the nordiska database" -ForegroundColor Yellow
Write-Host "(Docker volume removed, then migrations reapplied from scratch)." -ForegroundColor Yellow
Write-Host ""
$confirmation = Read-Host "Type 'yes' to continue, anything else to cancel"
 
if ($confirmation -ne "yes") {
    Write-Host "Cancelled. No changes made." -ForegroundColor Cyan
    exit
}
 
Write-Host "Deleting existing migrations..."
Set-Location "$PSScriptRoot\..\..\..\backend\NordiskaPortal.Api"
Remove-Item -Path "Migrations" -Recurse -Force

Write-Host "Generating a fresh InitialCreate migration..."
dotnet ef migrations add InitialCreate

Write-Host "Stopping and removing the local database volume..."
Set-Location "$PSScriptRoot\..\..\..\infra"
docker compose down -v

Write-Host "Rebuilding the local database volume..."
docker compose build --no-cache app

Write-Host "Starting a fresh database container..."
docker compose up -d

Write-Host "Waiting for Postgres to be ready..."
Start-Sleep -Seconds 5

Write-Host "Applying migrations..."
Set-Location "$PSScriptRoot\.."
dotnet ef database update

Write-Host "Done. Run 'dotnet run' to start the API." -ForegroundColor Green