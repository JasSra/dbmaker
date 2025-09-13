@echo off
echo ====================================
echo Stopping DbMaker Development Servers
echo ====================================

echo.
echo Killing processes on ports 4200 and 5021...

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

REM Kill all Node.js processes
echo Killing all Node.js processes...
taskkill /f /im node.exe >nul 2>&1

REM Kill all dotnet processes
echo Killing all .NET processes...
taskkill /f /im dotnet.exe >nul 2>&1

echo.
echo All DbMaker development servers stopped!
echo.
pause
