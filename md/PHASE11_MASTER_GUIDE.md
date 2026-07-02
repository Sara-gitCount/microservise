# Phase 11: Master Guide - Local Development Setup

## 🎯 Phase 11 Objective

Enable all microservices to start simultaneously from Visual Studio using a single F5 press, enabling fast local development with full debugging capabilities.

## ⚡ 5-Minute Setup

### Step 1: Open Solution
```
File → Open → mecira-microservices.sln
```

### Step 2: Build
```
Ctrl+Shift+B (Build → Build Solution)
```
Verify no errors.

### Step 3: Configure Multiple Startup
```
Right-click "Solution" in Solution Explorer
↓
Properties
↓
"Multiple startup projects"
↓
Check: ApiGateway, AuthService, CatalogService, OrderService, LotteryService
↓
OK
```

### Step 4: Start Services
```
F5
```

### Step 5: Verify
```powershell
curl http://localhost:5000/health
curl http://localhost:5001/health
curl http://localhost:5002/health
curl http://localhost:5003/health
curl http://localhost:5004/health
```

All should return HTTP 200.

## 📊 Service Architecture

```
Your Microservices (Local Development)

F5 Press
  ↓
Visual Studio Starts All Services
  ├─ API Gateway (localhost:5000)
  │
  ├─ Auth Service (localhost:5001)
  ├─ Catalog Service (localhost:5002)
  ├─ Order Service (localhost:5003)
  ├─ Lottery Service (localhost:5004)
  │
  └─ Local SQL Server Connection
```

## 🎮 Development Commands

| Action | Command | Result |
|--------|---------|--------|
| **Start all** | F5 | All 5 services start in debug mode |
| **Start fast** | Ctrl+F5 | All 5 services start without debugger |
| **Stop all** | Shift+F5 | All services stop cleanly |
| **Restart one** | Right-click in console → Restart | Single service restarts |
| **Debug mode** | Set breakpoint → F5 → trigger | Execution stops at breakpoint |
| **Step over** | F10 | Execute next line |
| **Step into** | F11 | Enter function call |
| **Continue** | F5 (when paused) | Resume execution |

## 🔍 Debugging Workflow

### Setting a Breakpoint
```
1. Open service code file
2. Click line number where you want to pause
3. Red circle appears
4. F5 to start debugging
5. Trigger action (curl endpoint)
6. Breakpoint pauses execution
7. Inspect variables
8. Step through code
9. F5 to continue
```

### Testing Service-to-Service Communication
```powershell
# Start all services: F5

# Make request through gateway to auth service
curl -X POST http://localhost:5000/auth/users/login `
  -H "Content-Type: application/json" `
  -d '{"email":"test@test.com","password":"password"}'

# Set breakpoint in AuthService.Controllers.AuthController
# Request hits breakpoint
# Debug the flow
```

## 📋 Service Ports

| Service | Port | URL | Health Check |
|---------|------|-----|--------------|
| API Gateway | 5000 | http://localhost:5000 | curl http://localhost:5000/health |
| Auth Service | 5001 | http://localhost:5001 | curl http://localhost:5001/health |
| Catalog Service | 5002 | http://localhost:5002 | curl http://localhost:5002/health |
| Order Service | 5003 | http://localhost:5003 | curl http://localhost:5003/health |
| Lottery Service | 5004 | http://localhost:5004 | curl http://localhost:5004/health |

## ✅ Verification

After pressing F5, verify:

```powershell
# 1. All services started
docker ps
# or
# Look for 5 console windows

# 2. All services healthy
curl http://localhost:5000/health   # 200 OK
curl http://localhost:5001/health   # 200 OK
curl http://localhost:5002/health   # 200 OK
curl http://localhost:5003/health   # 200 OK
curl http://localhost:5004/health   # 200 OK

# 3. Gateway can route
curl http://localhost:5000/auth/health  # Routes to Auth Service

# 4. Debugger works
# Set breakpoint → call endpoint → should pause
```

## 🛠️ Common Tasks

### Task 1: Debug a Specific Endpoint
```
1. Find the endpoint in service controller
2. Set breakpoint on first line
3. F5 to start debugging
4. curl the endpoint
5. Execution stops at breakpoint
6. Inspect request/response
7. Step through business logic
```

### Task 2: Test Full Flow
```
1. Start with F5
2. Call API Gateway: POST /orders/create
3. Gateway routes to OrderService
4. OrderService calls CatalogService (to check inventory)
5. OrderService calls AuthService (to validate user)
6. Response returns through gateway
7. Check console logs for each service
8. Inspect database state
```

### Task 3: Fix a Bug
```
1. F5 to start all services
2. Reproduce the bug by calling endpoint
3. Set breakpoint where you think bug is
4. Call endpoint again
5. Inspect variables at breakpoint
6. Step through code to find issue
7. Fix code (Ctrl+Z to undo, edit file)
8. Shift+F5 to stop, F5 to restart with changes
9. Verify fix works
```

### Task 4: Add Database Migration
```
1. Make model changes
2. F5 to start services (optional, just for testing)
3. Add migration via NuGet Package Manager Console:
   Add-Migration MigrationName -Context AuthDbContext
4. Update database:
   Update-Database -Context AuthDbContext
5. Restart services to verify
```

## ⚠️ Troubleshooting

### "Port Already in Use"
```powershell
# Find what's using the port
netstat -ano | findstr :5001

# Kill the process
taskkill /PID {PID} /F

# Or restart your machine to free ports
```

### Services Don't Start
```powershell
# 1. Check for build errors
# Ctrl+Shift+B and look at Error List

# 2. Verify configuration
# Right-click Solution → Properties
# Check all 5 services are checked

# 3. Try individual service
# Right-click AuthService → Debug → Start New Instance
# If this works, configuration is OK
```

### Debugger Won't Attach
```powershell
# 1. Use Ctrl+F5 to start
# 2. Debug → Attach to Process
# 3. Select service (e.g., AuthService.dll)
# 4. Click Attach

# Or restart VS if still issues
```

### Can't Connect to Service
```powershell
# 1. Check service is running
# Should see 5 console windows open

# 2. Check firewall allows port
# netsh advfirewall firewall add rule name="Allow 5001" dir=in action=allow protocol=tcp localport=5001

# 3. Test port directly
# Test-NetConnection localhost -Port 5001
```

## 📚 Detailed Documentation

| Need | Document |
|------|----------|
| Quick setup | [PHASE11_QUICK_START.md](PHASE11_QUICK_START.md) |
| Comprehensive guide | [PHASE11_MULTIPLE_STARTUP_PROJECTS.md](PHASE11_MULTIPLE_STARTUP_PROJECTS.md) |
| Verify everything works | [PHASE11_VERIFICATION_CHECKLIST.md](PHASE11_VERIFICATION_CHECKLIST.md) |
| Port configuration | [PHASE11_LAUNCH_SETTINGS.md](PHASE11_LAUNCH_SETTINGS.md) |
| Local vs Docker | [PHASE11_LOCAL_VS_DOCKER.md](PHASE11_LOCAL_VS_DOCKER.md) |
| Documentation index | [PHASE11_DOCUMENTATION_INDEX.md](PHASE11_DOCUMENTATION_INDEX.md) |

## 🎯 Next Steps

### Immediately (Today)
- [ ] Follow 5-minute setup above
- [ ] Press F5
- [ ] Test health endpoints
- [ ] Try setting a breakpoint

### This Week
- [ ] Read [PHASE11_MULTIPLE_STARTUP_PROJECTS.md](PHASE11_MULTIPLE_STARTUP_PROJECTS.md)
- [ ] Practice debugging
- [ ] Test service-to-service communication
- [ ] Become comfortable with workflow

### This Month
- [ ] Compare with Docker approach
- [ ] Decide when to use each
- [ ] Optimize development workflow
- [ ] Help team members setup

## 🚀 Quick Decisions

### When to Use Local F5
✅ Active development  
✅ Debugging code  
✅ Testing new features  
✅ Setting breakpoints  
✅ Rapid iteration  

### When to Use Docker
✅ Final testing  
✅ Integration testing  
✅ Before deployment  
✅ Testing container behavior  
✅ CI/CD testing  

## 📈 Performance Tips

### Make Startup Faster
- Use Ctrl+F5 instead of F5 (no debugger overhead)
- Result: 50% faster startup

### Reduce Memory Use
- Close unnecessary VS extensions
- Use SSD for project storage
- Don't open multiple solutions

### Faster Debugging
- Use "Step Over" (F10) instead of "Step Into" when possible
- Use conditional breakpoints
- Use "Run to Cursor" (Ctrl+F10)

## 💡 Pro Tips

### Tip 1: Use Call Stack While Debugging
```
While paused at breakpoint:
Debug → Windows → Call Stack
See entire call chain
Can click to jump to previous call
```

### Tip 2: Use Immediate Window
```
While paused at breakpoint:
Debug → Windows → Immediate
Type variable names to inspect
Type expressions to evaluate
Type method calls to execute
```

### Tip 3: Use Watch Window
```
While paused at breakpoint:
Debug → Windows → Watch
Add variables to watch
See values update as you step
```

### Tip 4: Use Data Breakpoints
```
Debug → New Breakpoint → Function Breakpoint
Pause only when specific conditions met
Useful for debugging loops
```

### Tip 5: Conditional Breakpoints
```
Right-click on breakpoint
Select "Filter..."
Add condition: userId == 5
Only stops when condition true
```

## 📞 Support

**Quick help**: [PHASE11_QUICK_START.md](PHASE11_QUICK_START.md)

**Detailed help**: [PHASE11_MULTIPLE_STARTUP_PROJECTS.md](PHASE11_MULTIPLE_STARTUP_PROJECTS.md)

**Troubleshooting**: PHASE11_MULTIPLE_STARTUP_PROJECTS.md → "Troubleshooting" section

**Configuration issues**: [PHASE11_LAUNCH_SETTINGS.md](PHASE11_LAUNCH_SETTINGS.md)

## ✨ Phase 11 Complete!

You now have:
- ✅ All services start with F5
- ✅ Full debugging capabilities
- ✅ Fast iteration cycle
- ✅ Service-to-service debugging
- ✅ Comprehensive documentation
- ✅ Troubleshooting guides

**You're ready for efficient local microservices development!**

---

## Summary

**Configuration**: 5 minutes  
**Learning debugging**: 30 minutes  
**Full productivity**: 1-2 days of practice  

**Result**: Professional local development environment with all microservices running and debuggable.

**Start now**: Press F5 and enjoy developing! 🚀
