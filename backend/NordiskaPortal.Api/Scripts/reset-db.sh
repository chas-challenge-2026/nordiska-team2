#!/usr/bin/env bash
#
# Resets the local Postgres container and re-applies all migrations from
# scratch. Run this after pulling changes that touched Migrations/, or
# whenever your local database and the migration files seem out of sync
# (e.g. "relation already exists" errors on dotnet ef database update).
#
# WARNING: this deletes all local data in the nordiska database AND
# deletes and regenerates the migration files themselves. Never run
# this against anything other than your own local dev environment.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GREEN='\033[0;32m'
NC='\033[0m'

echo ""
echo -e "${YELLOW}This will PERMANENTLY DELETE all local data in the nordiska database${NC}"
echo -e "${YELLOW}AND regenerate the Migrations folder from scratch.${NC}"
echo ""
read -p "Type 'yes' to continue, anything else to cancel: " confirmation

if [ "$confirmation" != "yes" ]; then
    echo -e "${CYAN}Cancelled. No changes made.${NC}"
    exit 0
fi

echo "Deleting existing migrations..."
cd "$SCRIPT_DIR/.."
rm -rf Migrations

echo "Generating a fresh InitialCreate migration..."
    dotnet ef migrations add InitialCreate

echo "Stopping and removing the local database volume..."
cd "$SCRIPT_DIR/../../../infra"
docker compose down -v

echo "Rebuilding the app image..."
docker compose build --no-cache app

echo "Starting a fresh database container..."
docker compose up -d

echo "Waiting for Postgres to be ready..."
sleep 5

echo "Applying migrations..."
cd "$SCRIPT_DIR/../../../backend/NordiskaPortal.Api"
dotnet ef database update

echo -e "${GREEN}Done. Run 'dotnet run' to start the API.${NC}"