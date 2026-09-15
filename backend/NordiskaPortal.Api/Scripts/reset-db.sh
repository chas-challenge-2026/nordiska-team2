#!/usr/bin/env bash
set -e

# Resolve script directory to allow running from anywhere
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo ""
echo -e "\033[1;33mThis will PERMANENTLY DELETE all local data in the nordiska database\033[0m"
echo -e "\033[1;33m(Docker volume removed, then migrations reapplied from scratch).\033[0m"
echo ""
read -p "Type 'yes' to continue, anything else to cancel: " confirmation

if [ "$confirmation" != "yes" ]; then
    echo -e "\033[0;36mCancelled. No changes made.\033[0m"
    exit 0
fi

# --- Read DB_PASSWORD from infra/.env safely ---
ENV_PATH="$SCRIPT_DIR/../../../infra/.env"
if [ ! -f "$ENV_PATH" ]; then
    echo -e "\033[0;31mERROR: Could not find .env at $ENV_PATH\033[0m"
    exit 1
fi

# Safely load variables from .env handling quotes and formatting
set -a
source "$ENV_PATH"
set +a

if [ -z "$DB_PASSWORD" ]; then
    echo -e "\033[0;31mERROR: DB_PASSWORD not found or empty in $ENV_PATH\033[0m"
    exit 1
fi

LOCAL_CONNECTION_STRING="Host=localhost;Port=5433;Database=nordiska;Username=nordiska;Password=$DB_PASSWORD"

echo "Deleting existing migrations..."
cd "$SCRIPT_DIR/../../../backend/NordiskaPortal.Api"
rm -rf Migrations

echo "Generating a fresh InitialCreate migration..."
dotnet ef migrations add InitialCreate

echo "Stopping and removing the local database volume..."
cd "$SCRIPT_DIR/../../../infra"
docker compose down -v

echo "Rebuilding the local database volume..."
docker compose build --no-cache app

echo "Starting fresh containers..."
docker compose up -d

echo "Waiting for Postgres to be ready..."
sleep 5

echo "Applying migrations..."
cd "$SCRIPT_DIR/.."
export ConnectionStrings__DefaultConnection="$LOCAL_CONNECTION_STRING"
dotnet ef database update --connection "$LOCAL_CONNECTION_STRING"

echo ""
echo -e "\033[0;32mDone. The application is running via Docker at http://localhost:8080/scalar/v1\033[0m"
echo -e "\033[0;32mNo further steps needed - do not run 'dotnet run' separately.\033[0m"
