# Phase 11: Configuration Verification Checklist

## Pre-Configuration

### System Requirements
- [ ] Visual Studio 2022 (version 17.0+) installed
- [ ] .NET 9.0 SDK installed
- [ ] Local SQL Server instance running (or configured)
- [ ] Solution file: `mecira-microservices.sln`

### Verification Commands
```powershell
# Check VS version
Write-Host $VSVERSION

# Check .NET SDK
dotnet --list-sdks

# Check SQL Server (if local)
sqlcmd -S (local)\MSSQLSERVER01 -Q "SELECT @@VERSION"
```

## Visual Studio Configuration

### Step 1: Open Solution
- [ ] File → Open → Browse to `mecira-microservices.sln`
- [ ] Solution loads without errors
- [ ] Error List is empty (View → Error List)
- [ ] All projects visible in Solution Explorer

### Step 2: Build Solution
- [ ] Press Ctrl+Shift+B (Build → Build Solution)
- [ ] Build completes successfully
- [ ] Output shows "Build succeeded"
- [ ] No compilation errors
- [ ] No warnings (if possible)

### Step 3: Access Solution Properties
- [ ] Right-click on **"Solution"** in Solution Explorer (not a project!)
- [ ] Select **"Properties"** option
- [ ] **NOT** "Project Properties" - that's for individual projects

### Step 4: Verify Multiple Startup Projects Option

In Solution Property Pages:
- [ ] Find **"Startup Project"** section
- [ ] Radio button for **"Multiple startup projects"** is available
- [ ] Can select this option

**If "Multiple startup projects" NOT available**:
```
❌ You might be in Project Properties instead of Solution Properties
✅ Make sure you right-clicked the "Solution" node, not a project
✅ Try: Solution → Properties from menu bar
```

### Step 5: Select Multiple Startup Projects
- [ ] Click **"Multiple startup projects"** radio button
- [ ] **DO** Check: ApiGateway
- [ ] **DO** Check: AuthService
- [ ] **DO** Check: CatalogService
- [ ] **DO** Check: OrderService
- [ ] **DO** Check: LotteryService
- [ ] **DON'T** Check: SharedModels (library only)

For each checked project:
- [ ] Action set to **"Start"** (or "Start without debugging")

### Step 6: Verify Startup Order
Recommended order:
- [ ] ApiGateway (1st)
- [ ] AuthService (2nd)
- [ ] CatalogService (3rd)
- [ ] OrderService (4th)
- [ ] LotteryService (5th)

**Note**: Order is not strictly enforced (parallel startup), but this is logical order.

### Step 7: Apply Settings
- [ ] Click **"OK"** or **"Apply"**
- [ ] Dialog closes
- [ ] Settings are saved

## Verification Before Running

### Solution Properties Persisted
- [ ] Right-click Solution → Properties again
- [ ] **"Multiple startup projects"** still selected
- [ ] All 5 projects still checked
- [ ] OK/Cancel

### Project Configuration Check

For each project, verify launchSettings.json:

**AuthService**:
- [ ] File exists: `Services/AuthService/Properties/launchSettings.json`
- [ ] Contains profile: "AuthService"
- [ ] Port configured: 5001

**CatalogService**:
- [ ] File exists: `Services/CatalogService/Properties/launchSettings.json`
- [ ] Contains profile: "CatalogService"
- [ ] Port configured: 5002

**OrderService**:
- [ ] File exists: `Services/OrderService/Properties/launchSettings.json`
- [ ] Contains profile: "OrderService"
- [ ] Port configured: 5003

**LotteryService**:
- [ ] File exists: `Services/LotteryService/Properties/launchSettings.json`
- [ ] Contains profile: "LotteryService"
- [ ] Port configured: 5004

**ApiGateway**:
- [ ] File exists: `Gateway/ApiGateway/Properties/launchSettings.json`
- [ ] Contains profile: "ApiGateway"
- [ ] Port configured: 5000

### Verify Program.cs Configuration

Each project's Program.cs should:
- [ ] Have `builder.WebApplication.CreateBuilder(args)`
- [ ] Configure to listen on the correct port
- [ ] Setup Kestrel server (default for ASP.NET Core)

## First Run Test

### Starting All Services
- [ ] Press **F5** (or Ctrl+F5 for no debugger)
- [ ] Visual Studio shows "Starting" in status bar
- [ ] Wait 30-45 seconds for all services to start

### Verify Startup (What You Should See)

**Console Windows**:
- [ ] 5 new console windows appear
- [ ] Window titles show service names:
  - "AuthService"
  - "CatalogService"
  - "OrderService"
  - "LotteryService"
  - "ApiGateway"

**Console Output** (in each window):
- [ ] No red/orange error text
- [ ] Shows "Listening on http://localhost:PORT"
- [ ] Shows application startup messages
- [ ] No connection refused errors
- [ ] Database connection successful (if running locally)

### Test Health Endpoints

Open PowerShell and test each service:

```powershell
# Auth Service
curl http://localhost:5001/health
```
- [ ] Returns HTTP 200
- [ ] Response contains status information

```powershell
# Catalog Service
curl http://localhost:5002/health
```
- [ ] Returns HTTP 200

```powershell
# Order Service
curl http://localhost:5003/health
```
- [ ] Returns HTTP 200

```powershell
# Lottery Service
curl http://localhost:5004/health
```
- [ ] Returns HTTP 200

```powershell
# API Gateway
curl http://localhost:5000/health
```
- [ ] Returns HTTP 200
- [ ] Shows all services reachable

### Test Service-to-Service Communication

```powershell
# Gateway routing to Auth Service
curl -X GET http://localhost:5000/auth/health
```
- [ ] Returns HTTP 200 (gateway routes to auth service)

### Stopping All Services
- [ ] Press **Shift+F5** or click "Stop" button
- [ ] All console windows close cleanly
- [ ] VS returns to normal state
- [ ] No lingering processes

## Debugging Capability Test

### Setting a Breakpoint

1. **Open a Controller file** (e.g., AuthService/Controllers/AuthController.cs)
2. **Click line number** to set breakpoint
   - [ ] Red circle appears on line
3. **Press F5** to start debugging
4. **Make a request** to trigger the breakpoint:
   ```powershell
   curl http://localhost:5001/api/health
   ```
5. **Verify breakpoint hits**:
   - [ ] Execution pauses at breakpoint
   - [ ] Can see variables in Locals window
   - [ ] Can hover over variables to inspect
   - [ ] Can step through code (F10, F11)

6. **Resume execution**:
   - [ ] Press F5 to continue
   - [ ] Request completes

### Debugger Features Working
- [ ] Can set breakpoint (red circle visible)
- [ ] Breakpoint causes execution to pause
- [ ] Can view variable values
- [ ] Can step line-by-line (F10)
- [ ] Can step into functions (F11)
- [ ] Can step out (Shift+F11)
- [ ] Call stack visible
- [ ] Locals/Watch window working

## Database Connectivity

### Local SQL Server Connection

```powershell
# Connect to local SQL Server
sqlcmd -S (local)\MSSQLSERVER01 -U sa -P {your_password}
```
- [ ] Connection successful
- [ ] Prompt shows `1>`

```sql
-- Inside sqlcmd
SELECT name FROM sys.databases;
GO
```
- [ ] See database list
- [ ] "Mechira-sinit-microservices" should exist

### Services Can Connect

Check each service's console output for:
- [ ] "Database connected successfully" or similar
- [ ] No connection timeout errors
- [ ] No authentication errors

## Performance Verification

### Startup Time
- [ ] Services start within 30-45 seconds
- [ ] No significant delays
- [ ] Consistent startup time

### Console Output Readability
- [ ] Can read all startup messages
- [ ] Logs show relevant information
- [ ] No excessive logging

### CPU/Memory Usage
While running:
- [ ] Task Manager shows reasonable usage
- [ ] No service using >50% CPU at idle
- [ ] No memory leaks visible

## Advanced Verification

### Hot Reload (if supported)

1. Edit a .cs file
2. Save (Ctrl+S)
3. Check if change is applied without restart:
   - [ ] Some changes apply immediately
   - [ ] For others, might need F5 restart

### Attaching Debugger Mid-Run

1. Services already running (started with Ctrl+F5)
2. Debug → Attach to Process
3. Find service process
4. Click Attach
5. Set breakpoint
6. Trigger breakpoint
   - [ ] Breakpoint works even though started without debugger

## Cleanup and Reset

### If Something Goes Wrong

```powershell
# 1. Stop debugger
Shift+F5

# 2. Close all console windows manually (if needed)

# 3. Clean build
Clean Solution (Build → Clean Solution)

# 4. Kill any stuck processes
Get-Process | Where-Object {$_.Name -like "*AuthService*"} | Stop-Process -Force

# 5. Rebuild
Ctrl+Shift+B (Build → Build Solution)

# 6. Try again
F5
```

- [ ] Processes killed cleanly
- [ ] Solution rebuilds successfully
- [ ] F5 works again

## Team Verification (if sharing)

- [ ] Configuration saved to solution file
- [ ] No user-specific paths in configuration
- [ ] LaunchSettings.json files in git
- [ ] Other developers can F5 without modification

## Troubleshooting Verification

If any issue occurs, check:

| Issue | Check |
|-------|-------|
| Services don't start | See PHASE11_MULTIPLE_STARTUP_PROJECTS.md |
| Port in use | `netstat -ano \| findstr :5001` |
| Build errors | View → Error List |
| Database connection | Verify SQL Server running |
| Debugger not working | Try Ctrl+F5 then attach |
| Breakpoint not hit | Ensure Debug configuration |
| Services hang | Check console for errors |

## Final Sign-Off

### Everything Working?
- [ ] All 5 services start with F5
- [ ] All health endpoints return 200
- [ ] Can set breakpoints and debug
- [ ] Can stop cleanly with Shift+F5
- [ ] Can restart multiple times

### Ready for Development!
- [ ] Solution properly configured
- [ ] Multiple startup projects set
- [ ] Services start reliably
- [ ] Debugging works
- [ ] Team can use same setup

---

## Verification Completion

**Status**: ✅ Phase 11 Configuration Verified

**Completed Date**: [Add date]
**Completed By**: [Your name]
**Issues Found**: [List any issues found and how they were resolved]
**Notes**: [Any special configuration or notes for team]

---

**Next Steps**:
1. Start using F5 for local development
2. Practice setting breakpoints
3. Try debugging service-to-service calls
4. Compare with Docker approach (PHASE11_LOCAL_VS_DOCKER.md)
