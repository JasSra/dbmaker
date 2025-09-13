#!/usr/bin/env pwsh

# Simple API Test Script for DbMaker Container Creation
$apiUrl = "http://localhost:5021"

Write-Host "=== DbMaker API Container Creation Test ===" -ForegroundColor Green

# Test 1: Health Check
Write-Host "`n1. Testing API Health..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-RestMethod -Uri "$apiUrl/health" -Method GET -TimeoutSec 10
    Write-Host "✅ API Health: OK" -ForegroundColor Green
    Write-Host "   Response: $($healthResponse | ConvertTo-Json -Compress)" -ForegroundColor Gray
} catch {
    Write-Host "❌ API Health check failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 2: Docker Connectivity Test
Write-Host "`n2. Testing Docker connectivity..." -ForegroundColor Yellow
try {
    $dockerResponse = Invoke-RestMethod -Uri "$apiUrl/api/containers/docker-test" -Method GET -TimeoutSec 30
    Write-Host "✅ Docker connectivity: OK" -ForegroundColor Green
    Write-Host "   Response: $($dockerResponse | ConvertTo-Json -Compress)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Docker connectivity test failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Make sure Docker Desktop is running!" -ForegroundColor Red
}

# Test 3: Create Demo Container
Write-Host "`n3. Testing demo container creation..." -ForegroundColor Yellow
try {
    $createResponse = Invoke-RestMethod -Uri "$apiUrl/api/containers/create-demo" -Method POST -ContentType "application/json" -TimeoutSec 60
    Write-Host "✅ Demo container creation successful!" -ForegroundColor Green
    Write-Host "   Response: $($createResponse | ConvertTo-Json -Compress)" -ForegroundColor Cyan
} catch {
    Write-Host "❌ Demo container creation failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $errorBody = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorBody)
        $errorContent = $reader.ReadToEnd()
        Write-Host "   Error details: $errorContent" -ForegroundColor Red
    }
}

# Test 4: List Containers (Debug endpoint)
Write-Host "`n4. Listing containers..." -ForegroundColor Yellow
try {
    $containersResponse = Invoke-RestMethod -Uri "$apiUrl/api/containers/all-debug" -Method GET -TimeoutSec 10
    Write-Host "✅ Containers retrieved!" -ForegroundColor Green
    Write-Host "   Total containers: $($containersResponse.Count)" -ForegroundColor Cyan
    foreach ($container in $containersResponse) {
        Write-Host "   - $($container.name) ($($container.databaseType)) - Port: $($container.port), Status: $($container.status)" -ForegroundColor Gray
    }
} catch {
    Write-Host "❌ Failed to list containers: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Green
Write-Host "Check the backend logs for container creation details." -ForegroundColor Yellow
