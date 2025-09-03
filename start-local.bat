@echo off
echo 🚀 DbMaker Local Development Startup
echo =====================================

REM Check if Docker is running
docker info >nul 2>&1
if errorlevel 1 (
    echo ❌ Docker is not running. Please start Docker Desktop first.
    pause
    exit /b 1
)

echo ✅ Docker is running

REM Navigate to backend directory
echo 📦 Setting up .NET Backend...
cd src\backend\DbMaker.API

REM Restore packages
echo 🔄 Restoring NuGet packages...
dotnet restore

REM Create/update database
echo 🗃️  Setting up database...
dotnet ef database update

REM Start the API in background
echo 🚀 Starting API server...
start "DbMaker API" cmd /k "dotnet run"

REM Wait a moment for API to start
timeout /t 5 /nobreak >nul

REM Navigate to frontend directory
echo 🌐 Setting up Angular Frontend...
cd ..\..\..\src\frontend\dbmaker-frontend

REM Install npm packages
echo 📦 Installing npm packages...
call npm install

REM Start the frontend
echo 🚀 Starting Angular development server...
start "DbMaker Frontend" cmd /k "npm start"

echo ✅ DbMaker is starting up!
echo.
echo 🌐 Access URLs:
echo   Frontend: http://localhost:4200
echo   API: http://localhost:5000
echo   API Health: http://localhost:5000/health
echo.
echo 🔧 To stop services:
echo   - Close the API terminal window
echo   - Close the Frontend terminal window
echo   - Or press Ctrl+C in each terminal
echo.
pause
