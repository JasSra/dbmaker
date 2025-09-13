# DbMaker Development Server Stop Script
Write-Host "====================================" -ForegroundColor Red
Write-Host "Stopping DbMaker Development Servers" -ForegroundColor Red
Write-Host "====================================" -ForegroundColor Red
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

Write-Host "Killing processes on ports 4200 and 5021..." -ForegroundColor Yellow

# Kill processes on ports
Stop-ProcessOnPort -Port 4200
Stop-ProcessOnPort -Port 5021

# Kill all Node.js processes
Write-Host "Killing all Node.js processes..." -ForegroundColor Yellow
Get-Process -Name "node" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

# Kill all dotnet processes  
Write-Host "Killing all .NET processes..." -ForegroundColor Yellow
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "All DbMaker development servers stopped!" -ForegroundColor Green
Write-Host ""
Read-Host "Press Enter to exit"
