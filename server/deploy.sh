#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

if [[ ! -f .env ]]; then
  cp .env.production.example .env
  echo "Created .env from .env.production.example"
  echo "Edit .env with real domain, passwords and Telegram token, then run this script again."
  exit 1
fi

echo "Building and starting production stack..."
docker compose -f docker-compose.prod.yml up -d --build

echo ""
echo "Waiting for services..."
sleep 5
docker compose -f docker-compose.prod.yml ps

echo ""
echo "Site:  https://${DOMAIN:-your-domain} (from .env)"
echo "Admin: https://${ADMIN_DOMAIN:-admin.your-domain} (from .env)"
echo "Logs:  docker compose -f docker-compose.prod.yml logs -f api"
