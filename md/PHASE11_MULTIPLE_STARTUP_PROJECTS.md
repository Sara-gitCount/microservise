# Phase 11: Multiple Startup Projects Configuration (Local Dev)

## Overview

Phase 11 enables starting all microservices simultaneously from Visual Studio with a single F5 press, making local development and testing seamless.

## Visual Studio Configuration (UI Method)

### Step-by-Step Instructions

#### Step 1: Open Solution Properties
1. Open `mecira-microservices.sln` in Visual Studio
2. Right-click on the **Solution** in Solution Explorer (not a project)
3. Select **"Properties"** or **"Set Startup Projects"**

#### Step 2: Select Multiple Startup Projects
1. In the Solution Property Pages dialog, find the **"Startup Project"** section
2. Select **"Multiple startup projects"** radio button

#### Step 3: Configure Projects
For each service, set the **Action** to **"Start"** in this order:

| Order | Project | Action | Port |
|-------|---------|--------|------|
| 1 | ApiGateway | Start | 5000 |
| 2 | AuthService | Start | 5001 |
| 3 | CatalogService | Start | 5002 |
| 4 | OrderService | Start | 5003 |
| 5 | LotteryService | Start | 5004 |

**Do NOT include SharedModels** - it's a library, not a runnable project.

#### Step 4: Apply and Close
1. Click **"OK"** or **"Apply"** to save configuration
2. Solution properties dialog closes

#### Step 5: Run All Services
```
Press F5 or Ctrl+F5
```

**Result**: All 5 services start in separate console windows with their respective ports.

## Important Configuration Points

### Visual Studio Version
- **Minimum**: Visual Studio 2022 (version 17.0+)
- **Recommended**: Latest version with .NET 9.0 support
- **Check**: Help → About Microsoft Visual Studio

### .NET Runtime
- **Required**: .NET 9.0 SDK installed locally
- **Check**: Open PowerShell and run `dotnet --list-sdks`
- **Missing?**: Install from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)

### IIS Express vs Kestrel
- **IIS Express**: ❌ Not recommended (doesn't support side-by-side instances well)
- **Kestrel**: ✅ Recommended (default ASP.NET Core server)

Verify each project uses Kestrel:
- Open each project's `Properties/launchSettings.json`
- Confirm `"serverName": "IIS Express"` is NOT set, OR
- Confirm `"applicationUrl"` uses localhost ports

### Port Configuration
Services automatically use configured ports from `launchSettings.json`:
- **AuthService**: Port 5001
- **CatalogService**: Port 5002
- **OrderService**: Port 5003
- **LotteryService**: Port 5004
- **ApiGateway**: Port 5000

If ports conflict with existing applications, modify `launchSettings.json` in each project.

## LaunchSettings.json Configuration

Each service should have a `Properties/launchSettings.json` file configured like this:

### AuthService Example
```json
{
  "profiles": {
    "AuthService": {
      "commandName": "Project",
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### ApiGateway Example
```json
{
  "profiles": {
    "ApiGateway": {
      "commandName": "Project",
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

**Key Points**:
- `"commandName": "Project"` - Uses Kestrel
- `"launchBrowser": false` - Don't open browser (avoid 5 browser windows opening!)
- `"applicationUrl"` - Correct port for each service
- `"ASPNETCORE_ENVIRONMENT": "Development"` - Use development config

## Startup Order Explanation

### Why Order Matters
1. **ApiGateway First**: Acts as entry point; doesn't harm if it starts first
2. **AuthService Second**: Other services may call it for token validation
3. **Other Services**: Order doesn't matter much; they start in parallel in practice

### Important Notes
- Services start asynchronously - don't wait for one to finish before starting next
- All 5 services should be running/ready within 10-30 seconds
- There's NO dependency enforcement in VS multiple startup projects

## Execution Guide

### Running All Services

#### Method 1: F5 (With Debugging)
```
Press F5
```
- Starts all projects in debug mode
- Allows setting breakpoints
- Can step through code
- Higher CPU/memory usage

#### Method 2: Ctrl+F5 (Without Debugging)
```
Press Ctrl+F5
```
- Starts all projects without debugger
- Faster startup
- Lower resource usage
- Cannot set breakpoints without attaching debugger

### Accessing Services

Once all services are running:

```
# Test API Gateway
curl http://localhost:5000/health

# Test Auth Service
curl http://localhost:5001/health

# Test Catalog Service
curl http://localhost:5002/health

# Test Order Service
curl http://localhost:5003/health

# Test Lottery Service
curl http://localhost:5004/health
```

### Monitoring Services

Each service runs in its own console window:
- Service name in window title
- Logs and startup messages displayed
- Check for errors or warnings
- Watch for "Listening on" messages

### Stopping All Services

#### Option 1: Stop Debugging
```
Press Shift+F5 or click Stop button
```
Stops all running services immediately.

#### Option 2: Close Console Windows
Manually close each service's console window.

## Troubleshooting

### Issue 1: "Port Already in Use" Error

**Error Message**:
```
System.IO.IOException: Failed to bind to address http://0.0.0.0:5001: 
address already in use
```

**Causes**:
- Another application using the port
- Previous instance still running
- Port assigned to another service

**Solutions**:
```powershell
# Check what's using port 5001
netstat -ano | findstr :5001

# Kill the process (replace PID)
taskkill /PID {PID} /F

# Or change port in launchSettings.json
"applicationUrl": "http://localhost:5011"
```

### Issue 2: Services Don't Start

**Symptoms**:
- Console windows appear then close
- No error messages visible

**Solutions**:
```powershell
# Build solution first
# Ctrl+Shift+B in Visual Studio

# Check for compilation errors
# View → Error List

# Run individual service to see specific error
# Right-click project → Debug → Start New Instance
```

### Issue 3: Debugger Attachment Issues

**Problem**: Breakpoints not working or debugger doesn't attach

**Solutions**:
1. Ensure project built in Debug configuration
2. Check Debug Info settings in project properties
3. Restart Visual Studio
4. Try attaching debugger manually:
   - Debug → Attach to Process
   - Select service process
   - Click Attach

### Issue 4: "Multiple startup projects" Option Missing

**Issue**: Cannot find multiple startup projects option

**Solution**:
- Ensure you're editing **Solution** properties, not Project
- Right-click on "Solution" in Solution Explorer (top level)
- Not on a project node
- Try: Solution → Properties menu from main menu bar

### Issue 5: Services Start But Can't Connect

**Symptoms**:
- Services running but `curl` returns "Connection refused"
- Console shows "Listening on" but no connection

**Causes**:
- Firewall blocking ports
- Port binding issue
- Service startup error (check logs)

**Solutions**:
```powershell
# Check firewall rules
netsh advfirewall show allprofiles

# Allow port through firewall
netsh advfirewall firewall add rule name="Allow 5000" dir=in action=allow protocol=tcp localport=5000

# Test connection
Test-NetConnection localhost -Port 5000
```

### Issue 6: High CPU Usage or Slow Startup

**Symptoms**:
- VS using 50%+ CPU
- Services take 60+ seconds to start

**Causes**:
- Debugger overhead (F5 vs Ctrl+F5)
- Large project compilation
- Disk I/O intensive operations

**Solutions**:
- Use Ctrl+F5 (without debugger) for non-debugging sessions
- Close unnecessary VS windows/extensions
- Increase RAM allocated to development
- Use SSD for faster disk I/O

### Issue 7: Some Services Start, Others Don't

**Symptom**:
- 3 services running, 2 missing
- Inconsistent startup

**Causes**:
- Project not configured in multiple startup projects
- Missing or incorrect port configuration
- Project build failing silently

**Solutions**:
```
1. Verify all 5 projects set to "Start" in solution properties
2. Build solution: Ctrl+Shift+B
3. Check Error List for build errors
4. Run each service individually to identify which fails
```

## Alternative: Start Services from Command Line

If Visual Studio configuration doesn't work, use PowerShell scripts:

### Using dotnet CLI
```powershell
cd d:\Documents\שרי\לימודים\microservise\server

# Terminal 1 - Auth Service
cd Services\AuthService
dotnet run

# Terminal 2 - Catalog Service
cd Services\CatalogService
dotnet run

# Terminal 3 - Order Service
cd Services\OrderService
dotnet run

# Terminal 4 - Lottery Service
cd Services\LotteryService
dotnet run

# Terminal 5 - API Gateway
cd Gateway\ApiGateway
dotnet run
```

### Using PowerShell Script
See `start-all-services.ps1` in the server directory for automated startup.

## Advanced Configurations

### Custom Environment Variables

Edit each project's `launchSettings.json` to add custom environment variables:

```json
{
  "profiles": {
    "AuthService": {
      "commandName": "Project",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "JWT_SECRET_KEY": "your-custom-secret-key",
        "LOG_LEVEL": "Debug"
      }
    }
  }
}
```

### Different Debugging Profiles

Create multiple launch profiles for different scenarios:

```json
{
  "profiles": {
    "AuthService": { /* Production-like */ },
    "AuthService Debug": { /* Extra logging */ }
  }
}
```

Then select profile from Debug dropdown in VS.

### Attach Debugger to Running Process

If services are already running via `dotnet run`:

1. Debug → Attach to Process
2. Find service executable (e.g., `AuthService.dll`)
3. Click Attach

## Best Practices

### Do's ✅
- [ ] Use Kestrel (not IIS Express)
- [ ] Set `launchBrowser: false`
- [ ] Use unique ports per service
- [ ] Monitor console output
- [ ] Stop all services before rebuilding
- [ ] Use Ctrl+F5 for faster iteration (when not debugging)

### Don'ts ❌
- [ ] Don't use IIS Express for multiple instances
- [ ] Don't set browser to open (5 windows!)
- [ ] Don't share ports between services
- [ ] Don't ignore build errors
- [ ] Don't rely on startup order guarantees
- [ ] Don't forget to update port config when you change it

## Performance Optimization

### Reduce Startup Time
```
1. Disable "Just My Code" in Debugger settings
2. Use Lightweight debugging for .NET
3. Disable unnecessary VS extensions
4. Use SSD for project storage
```

### Reduce Memory Usage
```
1. Close unnecessary VS windows
2. Don't attach debugger unless needed (use Ctrl+F5)
3. Disable IntelliSense for solution if slow
4. Clean NuGet cache periodically
```

## Verification Checklist

Before assuming everything works:

- [ ] All 5 services show console windows
- [ ] Each window shows service name in title
- [ ] No red error text in console windows
- [ ] Each service shows "Listening on" message
- [ ] Can curl each service's health endpoint (200 OK)
- [ ] API Gateway can route to other services
- [ ] Can set breakpoints in debugger
- [ ] Can restart all services with F5
- [ ] Services stop cleanly with Shift+F5

## Next Steps After Configuration

1. **Test Locally**:
   ```
   F5 → Wait 30 seconds → Test endpoints with curl
   ```

2. **Run Integration Tests**:
   ```
   dotnet test
   ```

3. **Debug Services**:
   - Set breakpoints
   - Call endpoints via curl or Postman
   - Watch request flow through gateway to services

4. **Monitor Logs**:
   - Keep console windows visible
   - Watch for warnings or errors
   - Use Serilog output for debugging

## Summary

Phase 11 enables efficient local development:
- ✅ Single F5 starts all services
- ✅ Independent debugging per service
- ✅ No Docker required for development
- ✅ Fast iteration cycle
- ✅ Easy stop/restart

**Configuration is a one-time setup** - once configured, F5 always starts all services!
