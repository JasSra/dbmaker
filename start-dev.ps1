# DbMaker Development Server Startup Script
Write-Host "====================================" -ForegroundColor Cyan
Write-Host "DbMaker Development Server Startup" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# Function to kill processes on specific ports
function Stop-ProcessOnPort {
    param([int]$Port)
    
    try {
        $processes = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
        foreach ($process in $processes) {
            $pid = $process.OwningProcess
            Write-Host "Killing process on port $Port (PID: $pid)" -ForegroundColor Yellow
            Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
        }
    }
    catch {
        # Port not in use, which is fine
    }
}

Write-Host "[1/6] Killing existing processes on ports 4200 and 5021..." -ForegroundColor Green

# Kill processes on ports
Stop-ProcessOnPort -Port 4200
Stop-ProcessOnPort -Port 5021

# Kill all Node.js and dotnet processes
Write-Host "Killing all Node.js processes..." -ForegroundColor Yellow
Get-Process -Name "node" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host "Killing all .NET processes..." -ForegroundColor Yellow
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "[2/6] Cleaning Angular cache..." -ForegroundColor Green
$cacheDir = "src\frontend\dbmaker-frontend\.angular\cache"
if (Test-Path $cacheDir) {
    Remove-Item -Recurse -Force $cacheDir -ErrorAction SilentlyContinue
    Write-Host "Angular cache cleared" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "[3/6] Building backend..." -ForegroundColor Green
$buildResult = & dotnet build src\backend\DbMaker.sln -c Debug
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Backend build failed!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "[4/6] Starting Backend API on http://localhost:5021..." -ForegroundColor Green

# Start backend in new PowerShell window
$backendScript = @"
Set-Location '$PWD'
Write-Host 'Starting DbMaker Backend API...' -ForegroundColor Cyan
dotnet run --project src\backend\DbMaker.API\DbMaker.API.csproj
"@

Start-Process powershell -ArgumentList "-NoExit", "-Command", $backendScript

Write-Host "Waiting for backend to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 8

Write-Host ""
Write-Host "[5/6] Starting Frontend on http://localhost:4200..." -ForegroundColor Green

# Start frontend in new PowerShell window
$frontendScript = @"
Set-Location '$PWD\src\frontend\dbmaker-frontend'
Write-Host 'Starting DbMaker Frontend...' -ForegroundColor Cyan
npm start
"@

Start-Process powershell -ArgumentList "-NoExit", "-Command", $frontendScript

Write-Host ""
Write-Host "[6/6] Workers (optional - uncomment to enable)..." -ForegroundColor Green
# Uncomment the next lines if you want to run workers automatically
# $workersScript = @"
# Set-Location '$PWD'
# Write-Host 'Starting DbMaker Workers...' -ForegroundColor Cyan
# dotnet run --project src\backend\DbMaker.Workers\DbMaker.Workers.csproj
# "@
# Start-Process powershell -ArgumentList "-NoExit", "-Command", $workersScript

Write-Host ""
Write-Host "====================================" -ForegroundColor Cyan
Write-Host "All services are starting up!" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Backend API: " -NoNewline -ForegroundColor White
Write-Host "http://localhost:5021" -ForegroundColor Green
Write-Host "Frontend:    " -NoNewline -ForegroundColor White
Write-Host "http://localhost:4200" -ForegroundColor Green
Write-Host "Health:      " -NoNewline -ForegroundColor White
Write-Host "http://localhost:5021/health" -ForegroundColor Green
Write-Host ""

Write-Host "Waiting for services to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Test if backend is responding
try {
    $healthCheck = Invoke-WebRequest -Uri "http://localhost:5021/health" -TimeoutSec 10 -ErrorAction Stop
    Write-Host "✓ Backend is responding!" -ForegroundColor Green
}
catch {
    Write-Host "⚠ Backend may still be starting..." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Press any key to open the application in browser..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Open the application in default browser
Start-Process "http://localhost:4200"

Write-Host ""
Write-Host "Development servers are running!" -ForegroundColor Green
Write-Host "Close the PowerShell windows to stop the services." -ForegroundColor Yellow
Write-Host ""
