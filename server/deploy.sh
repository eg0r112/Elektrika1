#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

if [[ ! -f .env ]]; then
  cp .env.production.example .env
  echo "Created .env from .env.production.example"
  echo "Edit .env with real domain, passwords and Telegram token, then run this script again."
  exit 1
fi

# shellcheck disable=SC1091
set -a
source .env
set +a

API_IMAGE="${IMAGE_API:-${GHCR_IMAGE_API:-ghcr.io/eg0r112/elektrika1-api:latest}}"
ADMIN_IMAGE="${IMAGE_ADMIN:-${GHCR_IMAGE_ADMIN:-ghcr.io/eg0r112/elektrika1-admin:latest}}"

pull_image() {
  local image=$1
  local attempt
  for attempt in $(seq 1 10); do
    echo "Pulling ${image} (attempt ${attempt}/10)..."
    if docker pull "${image}"; then
      echo "OK: ${image}"
      return 0
    fi
    echo "Retry in 30s..."
    sleep 30
  done
  echo "FAILED: ${image}"
  return 1
}

echo "Pulling app images (built by GitHub Actions)..."
pull_image "${API_IMAGE}"
pull_image "${ADMIN_IMAGE}"

echo "Starting production stack..."
docker compose -f docker-compose.prod.yml up -d

echo ""
echo "Waiting for services..."
sleep 5
docker compose -f docker-compose.prod.yml ps

echo ""
echo "Site:  https://${DOMAIN:-your-domain}"
echo "Admin: https://${ADMIN_DOMAIN:-admin.your-domain}"
echo "Logs:  docker compose -f docker-compose.prod.yml logs -f api"
