#!/usr/bin/env sh
# Starts the backend (API + PostgreSQL) and opens the API docs in your browser when it is ready.
# Usage: ./start.sh            (set API_PORT if you changed the port in .env)
set -e
cd "$(dirname "$0")"
PORT="${API_PORT:-8080}"

docker compose up -d
echo "Waiting for the API on port $PORT (the first start takes about a minute)..."
tries=0
until curl -sf "http://localhost:$PORT/api/health/db" > /dev/null; do
  tries=$((tries + 1))
  if [ "$tries" -gt 90 ]; then
    echo "The API did not start. See: docker compose logs api"
    exit 1
  fi
  sleep 2
done

URL="http://localhost:$PORT/docs"
echo "Ready. API docs: $URL"
if command -v open > /dev/null; then
  open "$URL"
elif command -v xdg-open > /dev/null; then
  xdg-open "$URL"
fi
