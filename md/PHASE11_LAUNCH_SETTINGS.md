# Phase 11: Launch Settings Configuration Verification

## LaunchSettings.json Files

Each service project must have a properly configured `Properties/launchSettings.json` file. Use this guide to verify and fix them.

## Service Configuration Templates

### AuthService - Properties/launchSettings.json

```json
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "AuthService": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### CatalogService - Properties/launchSettings.json

```json
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "CatalogService": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5002",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### OrderService - Properties/launchSettings.json

```json
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "OrderService": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5003",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### LotteryService - Properties/launchSettings.json

```json
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "LotteryService": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5004",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### ApiGateway - Properties/launchSettings.json

```json
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "ApiGateway": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

## Configuration Checklist

For each service, verify:

### ✓ File Location
- [ ] File exists at: `[ServiceName]/Properties/launchSettings.json`
- [ ] Not nested in subfolder

### ✓ JSON Structure
- [ ] Valid JSON format (use https://jsonlint.com/ if unsure)
- [ ] Contains `$schema` entry
- [ ] Contains `profiles` object

### ✓ Profile Configuration
- [ ] Profile name matches project name (e.g., "AuthService")
- [ ] `"commandName"`: should be `"Project"` (NOT `"IISExpress"`)
- [ ] `"launchBrowser"`: should be `false`
- [ ] `"dotnetRunMessages"`: should be `true`

### ✓ Port Configuration
- [ ] `"applicationUrl"` has correct port:
  - AuthService: `http://localhost:5001`
  - CatalogService: `http://localhost:5002`
  - OrderService: `http://localhost:5003`
  - LotteryService: `http://localhost:5004`
  - ApiGateway: `http://localhost:5000`

### ✓ Environment Variables
- [ ] `"ASPNETCORE_ENVIRONMENT"`: set to `"Development"`
- [ ] No hardcoded secrets
- [ ] Connection strings use docker/local DNS as per appsettings.json

## Fixing Incorrect Settings

### Issue: IIS Express Configuration

**Current (Wrong)**:
```json
{
  "iisSettings": {
    "windowsAuthentication": false,
    "anonymousAuthentication": true,
    "iisExpress": {
      "applicationUrl": "http://localhost:12345"
    }
  },
  "profiles": {
    "IIS Express": {
      "commandName": "IISExpress"
    }
  }
}
```

**Corrected (Right)**:
```json
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "AuthService": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### Issue: Browser Launching

**Current (Wrong)**:
```json
"launchBrowser": true
```

**Why It's Wrong**: Starting 5 services would open 5 browser windows!

**Corrected (Right)**:
```json
"launchBrowser": false
```

### Issue: Port Conflicts

**Current (Wrong)**:
```json
"applicationUrl": "http://localhost:5000"  // Wrong! This is Gateway port
```

**Corrected (Right)**:
```json
"applicationUrl": "http://localhost:5001"  // Correct for AuthService
```

## Environment Variables

### Development Environment

For local development with database on same machine:

```json
{
  "environmentVariables": {
    "ASPNETCORE_ENVIRONMENT": "Development",
    "ASPNETCORE_URLS": "http://localhost:5001"
  }
}
```

### With Custom Logging

```json
{
  "environmentVariables": {
    "ASPNETCORE_ENVIRONMENT": "Development",
    "ASPNETCORE_URLS": "http://localhost:5001",
    "Serilog__MinimumLevel": "Debug",
    "Serilog__WriteTo__0__Args__outputTemplate": "[{Timestamp:HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}"
  }
}
```

### With Database Connection Override

```json
{
  "environmentVariables": {
    "ASPNETCORE_ENVIRONMENT": "Development",
    "ASPNETCORE_URLS": "http://localhost:5001",
    "ConnectionStrings__DefaultConnection": "Server=(local)\\MSSQLSERVER01;Database=Mechira-sinit-microservices;Integrated Security=true;TrustServerCertificate=true;Encrypt=false;"
  }
}
```

## Verification Script

Run this PowerShell script to verify all launchSettings.json files:

```powershell
# Save as: verify-launch-settings.ps1

$services = @(
    @{ name = "AuthService"; port = 5001; path = "Services\AuthService\Properties\launchSettings.json" },
    @{ name = "CatalogService"; port = 5002; path = "Services\CatalogService\Properties\launchSettings.json" },
    @{ name = "OrderService"; port = 5003; path = "Services\OrderService\Properties\launchSettings.json" },
    @{ name = "LotteryService"; port = 5004; path = "Services\LotteryService\Properties\launchSettings.json" },
    @{ name = "ApiGateway"; port = 5000; path = "Gateway\ApiGateway\Properties\launchSettings.json" }
)

Write-Host "Verifying launchSettings.json files..." -ForegroundColor Cyan
Write-Host ""

foreach ($service in $services) {
    $fullPath = Join-Path -Path (Get-Location) -ChildPath $service.path
    
    if (-not (Test-Path $fullPath)) {
        Write-Host "❌ $($service.name): File not found at $fullPath" -ForegroundColor Red
        continue
    }
    
    try {
        $content = Get-Content $fullPath | ConvertFrom-Json
        
        # Check commandName
        $commandName = $content.profiles.$($service.name).commandName
        if ($commandName -ne "Project") {
            Write-Host "⚠️  $($service.name): commandName is '$commandName', should be 'Project'" -ForegroundColor Yellow
        }
        
        # Check launchBrowser
        $launchBrowser = $content.profiles.$($service.name).launchBrowser
        if ($launchBrowser -eq $true) {
            Write-Host "⚠️  $($service.name): launchBrowser is true, should be false" -ForegroundColor Yellow
        }
        
        # Check applicationUrl
        $appUrl = $content.profiles.$($service.name).applicationUrl
        $expectedUrl = "http://localhost:$($service.port)"
        if ($appUrl -ne $expectedUrl) {
            Write-Host "⚠️  $($service.name): applicationUrl is '$appUrl', should be '$expectedUrl'" -ForegroundColor Yellow
        }
        
        # Check environment
        $aspEnv = $content.profiles.$($service.name).environmentVariables.ASPNETCORE_ENVIRONMENT
        if ($aspEnv -ne "Development") {
            Write-Host "⚠️  $($service.name): ASPNETCORE_ENVIRONMENT is '$aspEnv', should be 'Development'" -ForegroundColor Yellow
        }
        
        if ($commandName -eq "Project" -and $launchBrowser -ne $true -and $appUrl -eq $expectedUrl -and $aspEnv -eq "Development") {
            Write-Host "✅ $($service.name): All settings correct" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "❌ $($service.name): Invalid JSON - $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Verification complete!" -ForegroundColor Cyan
```

## Testing Configuration

### Test 1: Can You Set launchSettings?

1. Open Project Properties (right-click project → Properties)
2. Go to Debug tab
3. Should see "Project" profile with correct port
4. If seeing "IIS Express", configuration needs fixing

### Test 2: Run Individual Service

```powershell
# From Visual Studio, right-click project
# Select: Debug → Start New Instance

# Should see:
# - Service starts successfully
# - No build errors
# - Service listens on correct port
# - Can see startup logs
```

### Test 3: Run All Services

1. F5 to start all services
2. Check each console window
3. Verify port numbers match expected ports
4. Test health endpoint for each

## Troubleshooting

### Profile Not Appearing in Debug Dropdown

**Cause**: Profile name doesn't match project name

**Solution**:
```json
// Wrong - profile name doesn't match
"profiles": {
  "AuthServiceApp": { ... }  // ❌ Profile name wrong
}

// Right - profile name matches project
"profiles": {
  "AuthService": { ... }  // ✅ Correct
}
```

### Still Starting IIS Express

**Cause**: launchSettings.json still configured for IIS

**Solution**:
- Delete IIS Express profile completely
- Set `commandName` to `"Project"`

```json
"profiles": {
  "AuthService": {  // ← Must match project name
    "commandName": "Project",  // ← NOT "IISExpress"
    ...
  }
}
```

### Ports Not Accessible

**Cause**: firewall, port already in use, or URL binding issue

**Solution**:
```powershell
# Check if port is already in use
netstat -ano | findstr :5001

# Kill the process if needed
taskkill /PID {PID} /F

# Test port accessibility
Test-NetConnection localhost -Port 5001
```

## Summary

✅ Each service has `Properties/launchSettings.json`
✅ `commandName` is `"Project"` (Kestrel, not IIS Express)
✅ `launchBrowser` is `false`
✅ Correct port in `applicationUrl`
✅ `ASPNETCORE_ENVIRONMENT` is `"Development"`

Once verified, F5 in Visual Studio will start all services correctly!
