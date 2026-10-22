@echo off
rem Starts the backend (API + PostgreSQL) and opens the API docs in your browser when it is ready.
rem Double-click this file, or run it from a terminal. Set API_PORT if you changed the port in .env.
setlocal
cd /d "%~dp0"
if "%API_PORT%"=="" set API_PORT=8080

docker compose up -d
if errorlevel 1 (
  echo Docker could not start the backend. Is Docker Desktop running?
  pause
  exit /b 1
)

echo Waiting for the API on port %API_PORT% (the first start takes about a minute)...
set /a tries=0
:wait
curl -sf -o nul "http://localhost:%API_PORT%/api/health/db" && goto ready
set /a tries+=1
if %tries% gtr 90 (
  echo The API did not start. See: docker compose logs api
  pause
  exit /b 1
)
ping -n 3 127.0.0.1 > nul
goto wait

:ready
echo Ready. API docs: http://localhost:%API_PORT%/docs
start "" "http://localhost:%API_PORT%/docs"
