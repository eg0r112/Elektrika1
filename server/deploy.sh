#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

if [[ ! -f .env ]]; then
  cp .env.production.example .env
  echo "Created .env from .env.production.example"
  echo "Edit .env with real domain, passwords and Telegram token, then run this script again."
  exit 1
fi

echo "Pulling app images from GHCR (built by GitHub Actions)..."
if ! docker compose -f docker-compose.prod.yml pull api admin; then
  echo ""
  echo "GHCR pull failed. For a private repo, login first:"
  echo "  docker login ghcr.io -u YOUR_GITHUB_USER -p YOUR_GITHUB_PAT"
  echo "Or make packages public: GitHub -> Packages -> elektrika1-api -> Package settings"
  exit 1
fi

echo "Starting production stack..."
docker compose -f docker-compose.prod.yml up -d

echo ""
echo "Waiting for services..."
sleep 5
docker compose -f docker-compose.prod.yml ps

echo ""
echo "Site:  https://${DOMAIN:-your-domain} (from .env)"
echo "Admin: https://${ADMIN_DOMAIN:-admin.your-domain} (from .env)"
echo "Logs:  docker compose -f docker-compose.prod.yml logs -f api"
