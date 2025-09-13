# PowerShell script to test container creation with port management
param(
    [Parameter(Mandatory=$true)]
    [string]$ContainerName,
    
    [Parameter(Mandatory=$true)]
    [ValidateSet("redis", "postgresql")]
    [string]$DatabaseType,
    
    [int]$StartPort = 10000
)

function Get-AvailablePort {
    param([int]$StartPort = 10000)
    
    for ($port = $StartPort; $port -lt ($StartPort + 1000); $port++) {
        $tcpConnection = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
        if (-not $tcpConnection) {
            # Double-check with netstat
            $netstatResult = netstat -an | Select-String ":$port "
            if (-not $netstatResult) {
                return $port
            }
        }
    }
    throw "No available ports found in range $StartPort to $($StartPort + 1000)"
}

function New-DatabaseContainer {
    param(
        [string]$ContainerName,
        [string]$DatabaseType,
        [int]$Port
    )
    
    $dockerContainerName = "dbmaker-test-$DatabaseType-$ContainerName"
    $subdomain = "test-$ContainerName-$DatabaseType".ToLower()
    
    Write-Host "Creating $DatabaseType container..."
    Write-Host "  Name: $dockerContainerName"
    Write-Host "  Port: $Port"
    Write-Host "  Subdomain: $subdomain"
    
    try {
        switch ($DatabaseType) {
            "redis" {
                $result = docker run -d `
                    --name $dockerContainerName `
                    --label "dbmaker.userId=test-user" `
                    --label "dbmaker.databaseType=$DatabaseType" `
                    --label "dbmaker.containerName=$ContainerName" `
                    --label "dbmaker.subdomain=$subdomain" `
                    -p "${Port}:6379" `
                    redis:7-alpine
                
                if ($LASTEXITCODE -eq 0) {
                    $connectionString = "redis://localhost:$Port"
                    Write-Host "✅ Redis container created successfully!"
                    Write-Host "Connection String: $connectionString"
                } else {
                    throw "Docker run failed with exit code $LASTEXITCODE"
                }
            }
            
            "postgresql" {
                $result = docker run -d `
                    --name $dockerContainerName `
                    --label "dbmaker.userId=test-user" `
                    --label "dbmaker.databaseType=$DatabaseType" `
                    --label "dbmaker.containerName=$ContainerName" `
                    --label "dbmaker.subdomain=$subdomain" `
                    -p "${Port}:5432" `
                    -e POSTGRES_DB=$ContainerName `
                    -e POSTGRES_USER=admin `
                    -e POSTGRES_PASSWORD=password123 `
                    postgres:16-alpine
                
                if ($LASTEXITCODE -eq 0) {
                    $connectionString = "postgresql://admin:password123@localhost:$Port/$ContainerName"
                    Write-Host "✅ PostgreSQL container created successfully!"
                    Write-Host "Connection String: $connectionString"
                } else {
                    throw "Docker run failed with exit code $LASTEXITCODE"
                }
            }
        }
        
        # Wait for container to be healthy
        Write-Host "Waiting for container to start..."
        Start-Sleep -Seconds 3
        
        # Check container status
        $containerStatus = docker ps --filter "name=$dockerContainerName" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
        Write-Host "Container Status:"
        Write-Host $containerStatus
        
        return @{
            ContainerName = $dockerContainerName
            Port = $Port
            Subdomain = $subdomain
            ConnectionString = $connectionString
            Status = "Running"
        }
        
    } catch {
        Write-Error "Failed to create container: $_"
        # Cleanup on failure
        docker rm -f $dockerContainerName 2>$null
        throw
    }
}

# Main execution
try {
    Write-Host "=== DbMaker Container Creation Test ===" -ForegroundColor Cyan
    Write-Host "Container Name: $ContainerName"
    Write-Host "Database Type: $DatabaseType"
    
    # Find available port
    Write-Host "`nFinding available port..." -ForegroundColor Yellow
    $availablePort = Get-AvailablePort -StartPort $StartPort
    Write-Host "Selected port: $availablePort" -ForegroundColor Green
    
    # Create container
    Write-Host "`nCreating container..." -ForegroundColor Yellow
    $container = New-DatabaseContainer -ContainerName $ContainerName -DatabaseType $DatabaseType -Port $availablePort
    
    Write-Host "`n=== Container Created Successfully ===" -ForegroundColor Green
    Write-Host "Container: $($container.ContainerName)"
    Write-Host "Port: $($container.Port)"
    Write-Host "Subdomain: $($container.Subdomain)"
    Write-Host "Connection: $($container.ConnectionString)"
    Write-Host "Status: $($container.Status)"
    
    # Test connection
    Write-Host "`nTesting connection..." -ForegroundColor Yellow
    switch ($DatabaseType) {
        "redis" {
            # Test Redis connection using Docker exec
            $testResult = docker exec $container.ContainerName redis-cli ping
            if ($testResult -eq "PONG") {
                Write-Host "✅ Redis connection test successful!" -ForegroundColor Green
            } else {
                Write-Host "❌ Redis connection test failed!" -ForegroundColor Red
            }
        }
        "postgresql" {
            # Test PostgreSQL connection
            $testResult = docker exec $container.ContainerName psql -U admin -d $ContainerName -c "SELECT 1;" 2>$null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✅ PostgreSQL connection test successful!" -ForegroundColor Green
            } else {
                Write-Host "❌ PostgreSQL connection test failed!" -ForegroundColor Red
            }
        }
    }
    
    Write-Host "`nContainer is ready for use! 🚀" -ForegroundColor Cyan
    Write-Host "To remove: docker rm -f $($container.ContainerName)"
    
} catch {
    Write-Error "Container creation failed: $_"
    exit 1
}
