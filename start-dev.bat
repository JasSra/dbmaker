@echo off
echo ====================================
echo DbMaker Development Server Startup
echo ====================================

echo.
echo [1/6] Killing existing processes on ports 4200 and 5021...

REM Kill processes using port 4200 (Angular dev server)
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :4200') do (
    echo Killing process on port 4200 (PID: %%a)
    taskkill /f /pid %%a >nul 2>&1
)

REM Kill processes using port 5021 (Backend API)
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :5021') do (
    echo Killing process on port 5021 (PID: %%a)
    taskkill /f /pid %%a >nul 2>&1
)

REM Kill all Node.js processes (for Angular)
echo Killing all Node.js processes...
taskkill /f /im node.exe >nul 2>&1

REM Kill all dotnet processes (for Backend)
echo Killing all .NET processes...
taskkill /f /im dotnet.exe >nul 2>&1

echo.
echo [2/6] Cleaning Angular cache...
if exist "src\frontend\dbmaker-frontend\.angular\cache" (
    rmdir /s /q "src\frontend\dbmaker-frontend\.angular\cache" >nul 2>&1
    echo Angular cache cleared
)

echo.
echo [3/6] Building backend...
dotnet build src\backend\DbMaker.sln -c Debug
if %ERRORLEVEL% neq 0 (
    echo ERROR: Backend build failed!
    pause
    exit /b 1
)

echo.
echo [4/6] Starting Backend API on http://localhost:5021...
start "DbMaker Backend API" cmd /k "cd /d %~dp0 && dotnet run --project src\backend\DbMaker.API\DbMaker.API.csproj"

echo Waiting for backend to initialize...
timeout /t 5 /nobreak >nul

echo.
echo [5/6] Starting Frontend on http://localhost:4200...
start "DbMaker Frontend" cmd /k "cd /d %~dp0\src\frontend\dbmaker-frontend && npm start"

echo.
echo [6/6] Starting Workers (optional)...
REM Uncomment the next line if you want to run workers automatically
REM start "DbMaker Workers" cmd /k "cd /d %~dp0 && dotnet run --project src\backend\DbMaker.Workers\DbMaker.Workers.csproj"

echo.
echo ====================================
echo All services are starting up!
echo ====================================
echo.
echo Backend API: http://localhost:5021
echo Frontend:    http://localhost:4200
echo Health:      http://localhost:5021/health
echo.
echo Press any key to open the application in browser...
pause >nul

REM Open the application in default browser
start http://localhost:4200

echo.
echo Development servers are running!
echo Press Ctrl+C in each terminal window to stop the services.
echo.
