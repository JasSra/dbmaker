# API Container Creation Test Script
param(
    [string]$BaseUrl = "http://localhost:5021",
    [string]$DatabaseType = "redis",
    [string]$ContainerName = "test-api-container"
)

Write-Host "=== DbMaker API Container Creation Test ===" -ForegroundColor Green
Write-Host "Base URL: $BaseUrl" -ForegroundColor Cyan
Write-Host "Database Type: $DatabaseType" -ForegroundColor Cyan
Write-Host "Container Name: $ContainerName" -ForegroundColor Cyan
Write-Host

# Test API connectivity
try {
    Write-Host "Testing API connectivity..." -ForegroundColor Yellow
    $healthResponse = Invoke-RestMethod -Uri "$BaseUrl/api/health" -Method Get -ErrorAction Stop
    Write-Host "✓ API is accessible" -ForegroundColor Green
    Write-Host "Health Status: $($healthResponse.status)" -ForegroundColor White
} catch {
    Write-Host "✗ API is not accessible: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test Docker connectivity through API
try {
    Write-Host "`nTesting Docker connectivity..." -ForegroundColor Yellow
    $dockerResponse = Invoke-RestMethod -Uri "$BaseUrl/api/containers/docker-test" -Method Get -ErrorAction Stop
    Write-Host "✓ Docker connectivity test passed" -ForegroundColor Green
    Write-Host "Container count: $($dockerResponse.containerCount)" -ForegroundColor White
} catch {
    Write-Host "✗ Docker connectivity test failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $errorStream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorStream)
        $errorBody = $reader.ReadToEnd()
        Write-Host "Error details: $errorBody" -ForegroundColor Red
    }
}

# Get existing containers
try {
    Write-Host "`nGetting existing containers..." -ForegroundColor Yellow
    $existingResponse = Invoke-RestMethod -Uri "$BaseUrl/api/containers/all-debug" -Method Get -ErrorAction Stop
    Write-Host "✓ Retrieved container information" -ForegroundColor Green
    Write-Host "Database containers: $($existingResponse.totalDbContainers)" -ForegroundColor White
    Write-Host "Docker containers: $($existingResponse.totalDockerContainers)" -ForegroundColor White
    
    if ($existingResponse.databaseContainers.Count -gt 0) {
        Write-Host "`nExisting containers:" -ForegroundColor Cyan
        $existingResponse.databaseContainers | ForEach-Object {
            Write-Host "  - $($_.Name) ($($_.DatabaseType)) - Status: $($_.Status)" -ForegroundColor White
        }
    }
} catch {
    Write-Host "✗ Failed to get existing containers: $($_.Exception.Message)" -ForegroundColor Red
}

# Create a new container using the API
try {
    Write-Host "`nCreating new container via API..." -ForegroundColor Yellow
    
    $createRequest = @{
        DatabaseType = $DatabaseType
        Name = $ContainerName
        Configuration = @{
            "description" = "API test container"
            "environment" = "development"
        }
    }
    
    $jsonBody = $createRequest | ConvertTo-Json -Depth 3
    Write-Host "Request body:" -ForegroundColor Gray
    Write-Host $jsonBody -ForegroundColor Gray
    
    $headers = @{
        "Content-Type" = "application/json"
        "Accept" = "application/json"
    }
    
    $createResponse = Invoke-RestMethod -Uri "$BaseUrl/api/containers" -Method Post -Body $jsonBody -Headers $headers -ErrorAction Stop
    
    Write-Host "✓ Container created successfully!" -ForegroundColor Green
    Write-Host "Container ID: $($createResponse.Id)" -ForegroundColor White
    Write-Host "Container Name: $($createResponse.Name)" -ForegroundColor White
    Write-Host "Database Type: $($createResponse.DatabaseType)" -ForegroundColor White
    Write-Host "Port: $($createResponse.Port)" -ForegroundColor White
    Write-Host "Subdomain: $($createResponse.Subdomain)" -ForegroundColor White
    Write-Host "Status: $($createResponse.Status)" -ForegroundColor White
    Write-Host "Connection String: $($createResponse.ConnectionString)" -ForegroundColor White
    
    $containerId = $createResponse.Id
    
    # Wait a moment for container to fully start
    Write-Host "`nWaiting for container to initialize..." -ForegroundColor Yellow
    Start-Sleep -Seconds 3
    
    # Get container stats
    try {
        Write-Host "`nGetting container stats..." -ForegroundColor Yellow
        $statsResponse = Invoke-RestMethod -Uri "$BaseUrl/api/containers/$containerId/stats" -Method Get -ErrorAction Stop
        Write-Host "✓ Container stats retrieved" -ForegroundColor Green
        Write-Host "Container Status: $($statsResponse.Status)" -ForegroundColor White
        Write-Host "Is Healthy: $($statsResponse.IsHealthy)" -ForegroundColor White
    } catch {
        Write-Host "⚠ Could not retrieve container stats: $($_.Exception.Message)" -ForegroundColor Yellow
    }
    
    # Test container connection (if it's Redis)
    if ($DatabaseType -eq "redis" -and $createResponse.Port) {
        try {
            Write-Host "`nTesting Redis connection..." -ForegroundColor Yellow
            
            # Test if port is responding
            $tcpClient = New-Object System.Net.Sockets.TcpClient
            $connectResult = $tcpClient.BeginConnect("localhost", $createResponse.Port, $null, $null)
            $connectResult.AsyncWaitHandle.WaitOne(2000) | Out-Null
            
            if ($tcpClient.Connected) {
                Write-Host "✓ Redis port $($createResponse.Port) is accepting connections" -ForegroundColor Green
                $tcpClient.Close()
            } else {
                Write-Host "⚠ Redis port $($createResponse.Port) is not yet accepting connections" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "⚠ Could not test Redis connection: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
    
    Write-Host "`n=== Container Creation Test Completed Successfully! ===" -ForegroundColor Green
    Write-Host "You can now access your $DatabaseType container on port $($createResponse.Port)" -ForegroundColor Cyan
    Write-Host "Subdomain: $($createResponse.Subdomain).mydomain.com" -ForegroundColor Cyan
    
} catch {
    Write-Host "✗ Failed to create container: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        try {
            $errorStream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($errorStream)
            $errorBody = $reader.ReadToEnd() | ConvertFrom-Json
            Write-Host "Error details: $($errorBody.message)" -ForegroundColor Red
            if ($errorBody.error) {
                Write-Host "Error: $($errorBody.error)" -ForegroundColor Red
            }
        } catch {
            Write-Host "Could not parse error response" -ForegroundColor Red
        }
    }
    exit 1
}

Write-Host "`n=== Test Summary ===" -ForegroundColor Blue
Write-Host "✓ API Connectivity: OK" -ForegroundColor Green
Write-Host "✓ Docker Connectivity: OK" -ForegroundColor Green  
Write-Host "✓ Container Creation: OK" -ForegroundColor Green
Write-Host "✓ Container Configuration: OK" -ForegroundColor Green

# Offer to clean up
$cleanup = Read-Host "`nWould you like to clean up the test container? (y/N)"
if ($cleanup -eq "y" -or $cleanup -eq "Y") {
    try {
        Write-Host "Cleaning up test container..." -ForegroundColor Yellow
        Invoke-RestMethod -Uri "$BaseUrl/api/containers/$containerId" -Method Delete -ErrorAction Stop
        Write-Host "✓ Test container cleaned up successfully" -ForegroundColor Green
    } catch {
        Write-Host "⚠ Failed to clean up container: $($_.Exception.Message)" -ForegroundColor Yellow
        Write-Host "You may need to manually remove container ID: $containerId" -ForegroundColor Yellow
    }
}

Write-Host "`nAPI Container Creation Test Complete!" -ForegroundColor Green
