@echo off
REM DbMaker Deployment Script for Windows
REM Supports both development and production deployments

setlocal enabledelayedexpansion

set ENVIRONMENT=%1
if "%ENVIRONMENT%"=="" set ENVIRONMENT=dev

set COMPOSE_FILE=

if /i "%ENVIRONMENT%"=="dev" goto :dev
if /i "%ENVIRONMENT%"=="development" goto :dev
if /i "%ENVIRONMENT%"=="prod" goto :prod
if /i "%ENVIRONMENT%"=="production" goto :prod

echo ❌ Invalid environment. Use 'dev' or 'prod'
echo Usage: deploy.bat [dev^|prod]
exit /b 1

:dev
echo 🚀 Starting DbMaker in DEVELOPMENT mode...
set COMPOSE_FILE=docker-compose.dev.yml
goto :start

:prod
echo 🚀 Starting DbMaker in PRODUCTION mode...
set COMPOSE_FILE=docker-compose.prod.yml
goto :start

:start
echo 📋 Using compose file: %COMPOSE_FILE%

REM Check if Docker is running
docker info >nul 2>&1
if errorlevel 1 (
    echo ❌ Docker is not running. Please start Docker first.
    exit /b 1
)

REM Build and start services
echo 🔨 Building and starting services...
docker-compose -f %COMPOSE_FILE% up --build -d

echo ⏳ Waiting for services to be ready...
timeout /t 10 /nobreak >nul

REM Health checks
echo 🔍 Checking service health...

if /i "%ENVIRONMENT%"=="prod" goto :prod_health
if /i "%ENVIRONMENT%"=="production" goto :prod_health

REM Development health checks
echo Checking API health...
curl -f http://localhost:5000/health >nul 2>&1 || echo ⚠️  API health check failed

echo ℹ️  Frontend should be started manually with 'npm start' in development mode
goto :complete

:prod_health
REM Production health checks
echo Checking nginx health...
curl -f http://localhost:8080/health >nul 2>&1 || echo ⚠️  nginx health check failed

echo Checking API health...
curl -f http://localhost:5000/health >nul 2>&1 || echo ⚠️  API health check failed

echo Checking frontend...
curl -f http://localhost:4200 >nul 2>&1 || echo ⚠️  Frontend health check failed

:complete
echo ✅ DbMaker deployment complete!
echo.
echo 📊 Service Status:
docker-compose -f %COMPOSE_FILE% ps

echo.
echo 🌐 Access URLs:
if /i "%ENVIRONMENT%"=="prod" goto :prod_urls
if /i "%ENVIRONMENT%"=="production" goto :prod_urls

echo   API: http://localhost:5000
echo   Frontend: Start with 'cd src/frontend/dbmaker-frontend && npm start'
goto :next_steps

:prod_urls
echo   Frontend: http://console.mydomain.com (or http://localhost:4200)
echo   API: http://localhost:5000
echo   nginx Health: http://localhost:8080/health

:next_steps
echo.
echo 📱 Next Steps:
echo   1. Configure your MSAL settings in the frontend
echo   2. Set up your domain DNS (*.mydomain.com → your server)
echo   3. Create your first database container through the UI

echo.
echo 🔧 Management Commands:
echo   View logs: docker-compose -f %COMPOSE_FILE% logs -f [service]
echo   Stop: docker-compose -f %COMPOSE_FILE% down
echo   Restart: docker-compose -f %COMPOSE_FILE% restart [service]

endlocal
