# Docker Health Check Script
# Validates that all services are running and healthy

param(
    [Parameter(Mandatory=$false)]
    [string]$Timeout = 60,
    
    [Parameter(Mandatory=$false)]
    [switch]$Wait
)

$services = @(
    @{ name = "API Gateway"; port = 5000; container = "mechira-api-gateway" },
    @{ name = "Auth Service"; port = 5001; container = "mechira-auth-service" },
    @{ name = "Catalog Service"; port = 5002; container = "mechira-catalog-service" },
    @{ name = "Order Service"; port = 5003; container = "mechira-order-service" },
    @{ name = "Lottery Service"; port = 5004; container = "mechira-lottery-service" },
    @{ name = "SQL Server"; port = 1433; container = "mechira-mssql-db"; noHttp = $true }
)

$allHealthy = $true
$startTime = Get-Date

Write-Host "`n=== Docker Services Health Check ===" -ForegroundColor Cyan
Write-Host "Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray

# First check if containers are running
Write-Host "`n[1/3] Checking container status..." -ForegroundColor Yellow

$result = docker-compose ps -q
$runningContainers = $result | Measure-Object -Line

if ($runningContainers.Lines -lt 6) {
    Write-Host "⚠️  Not all containers are running!" -ForegroundColor Yellow
    Write-Host "Expected 6 containers, found $($runningContainers.Lines)" -ForegroundColor Red
    Write-Host "Run: docker-compose up -d --build" -ForegroundColor Gray
    $allHealthy = $false
}
else {
    Write-Host "✅ All 6 containers are running" -ForegroundColor Green
}

# Check health of HTTP services
Write-Host "`n[2/3] Checking health endpoints..." -ForegroundColor Yellow

foreach ($service in $services) {
    if ($service.noHttp) {
        # Check SQL Server connectivity
        $result = docker exec $service.container /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourPasswordHere123!" -Q "SELECT 1" -h -1 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ $($service.name) (Port $($service.port))" -ForegroundColor Green
        }
        else {
            Write-Host "❌ $($service.name) (Port $($service.port)) - Connection failed" -ForegroundColor Red
            $allHealthy = $false
        }
    }
    else {
        try {
            $response = curl -s -o /dev/null -w "%{http_code}" "http://localhost:$($service.port)/health" -m 5
            if ($response -eq "200") {
                Write-Host "✅ $($service.name) (Port $($service.port)) - HTTP $response" -ForegroundColor Green
            }
            else {
                Write-Host "⚠️  $($service.name) (Port $($service.port)) - HTTP $response" -ForegroundColor Yellow
                if ($response -eq "000") {
                    $allHealthy = $false
                }
            }
        }
        catch {
            Write-Host "❌ $($service.name) (Port $($service.port)) - Connection failed" -ForegroundColor Red
            $allHealthy = $false
        }
    }
}

# Check Docker network connectivity
Write-Host "`n[3/3] Checking network connectivity..." -ForegroundColor Yellow

$dns = docker exec mechira-auth-service nslookup order-service 2>&1 | Select-String "Name:"
if ($dns) {
    Write-Host "✅ Docker network DNS resolution working" -ForegroundColor Green
}
else {
    Write-Host "⚠️  Docker network DNS resolution may have issues" -ForegroundColor Yellow
    $allHealthy = $false
}

# Summary
Write-Host "`n=== Summary ===" -ForegroundColor Cyan

if ($allHealthy) {
    Write-Host "✅ All services are healthy!" -ForegroundColor Green
    Write-Host "`nAccess points:" -ForegroundColor Gray
    Write-Host "  API Gateway: http://localhost:5000" -ForegroundColor Gray
    Write-Host "  Auth Service: http://localhost:5001" -ForegroundColor Gray
    Write-Host "  Catalog Service: http://localhost:5002" -ForegroundColor Gray
    Write-Host "  Order Service: http://localhost:5003" -ForegroundColor Gray
    Write-Host "  Lottery Service: http://localhost:5004" -ForegroundColor Gray
    Write-Host "  SQL Server: localhost:1433 (sa/YourPasswordHere123!)" -ForegroundColor Gray
}
else {
    Write-Host "❌ Some services are not healthy" -ForegroundColor Red
    Write-Host "`nTroubleshooting steps:" -ForegroundColor Yellow
    Write-Host "1. Run: docker-compose ps" -ForegroundColor Gray
    Write-Host "2. Check logs: docker-compose logs" -ForegroundColor Gray
    Write-Host "3. Restart services: docker-compose restart" -ForegroundColor Gray
    Write-Host "4. Rebuild from scratch: docker-compose down -v && docker-compose up --build" -ForegroundColor Gray
}

if ($Wait -and -not $allHealthy) {
    Write-Host "`nWaiting for services to become healthy (timeout: $Timeout seconds)..." -ForegroundColor Yellow
    
    while ((Get-Date) -lt $startTime.AddSeconds($Timeout)) {
        Start-Sleep -Seconds 5
        
        $healthy = $true
        foreach ($service in $services) {
            if ($service.noHttp) {
                $result = docker exec $service.container /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourPasswordHere123!" -Q "SELECT 1" -h -1 2>$null
                if ($LASTEXITCODE -ne 0) {
                    $healthy = $false
                    break
                }
            }
            else {
                $response = curl -s -o /dev/null -w "%{http_code}" "http://localhost:$($service.port)/health" -m 5
                if ($response -ne "200") {
                    $healthy = $false
                    break
                }
            }
        }
        
        if ($healthy) {
            Write-Host "✅ All services are now healthy!" -ForegroundColor Green
            break
        }
    }
    
    if (-not $healthy) {
        Write-Host "⏱️  Timeout reached. Services still not fully healthy." -ForegroundColor Yellow
    }
}

Write-Host "`n"
