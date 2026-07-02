# Phase 11: Local Development Setup - Quick Start

## 🚀 Quickest Path (5 minutes)

### 1. Open Visual Studio Solution
```
Open: d:\Documents\שרי\לימודים\microservise\server\mecira-microservices.sln
```

### 2. Configure Multiple Startup Projects
```
Right-click on "Solution" (not a project) in Solution Explorer
↓
Select "Properties" or "Set Startup Projects"
↓
Choose "Multiple startup projects" radio button
↓
Set Action = "Start" for these (in any order):
  • ApiGateway ← Set first
  • AuthService
  • CatalogService
  • OrderService
  • LotteryService

Do NOT select SharedModels (it's just a library)
```

### 3. Start All Services
```
Press F5 (or Ctrl+F5 for no debugger)
```

**That's it!** All 5 services start in separate console windows.

## ✅ Verify It Works

```powershell
# Open PowerShell, run this:
curl http://localhost:5000/health
curl http://localhost:5001/health
curl http://localhost:5002/health
curl http://localhost:5003/health
curl http://localhost:5004/health

# All should return: {"status":"healthy"}
```

## 📋 Service Ports

| Service | Port | URL |
|---------|------|-----|
| API Gateway | 5000 | http://localhost:5000 |
| Auth Service | 5001 | http://localhost:5001 |
| Catalog Service | 5002 | http://localhost:5002 |
| Order Service | 5003 | http://localhost:5003 |
| Lottery Service | 5004 | http://localhost:5004 |

## 🛑 Stop All Services

```
Press Shift+F5 in Visual Studio
```

## ⚠️ Common Issues

### "Port Already in Use"
```powershell
netstat -ano | findstr :5001
taskkill /PID {replace-with-PID} /F
```

### Services Don't Start
```
Build solution: Ctrl+Shift+B
Check Error List for compile errors
```

### "Multiple startup projects" Missing
```
Right-click on "Solution" node (not a project)
Make sure you're editing Solution properties, not Project
```

## 📚 Full Documentation

See [PHASE11_MULTIPLE_STARTUP_PROJECTS.md](PHASE11_MULTIPLE_STARTUP_PROJECTS.md) for:
- Step-by-step detailed instructions
- Troubleshooting guide
- Advanced configurations
- Performance optimization
- Best practices

## 🎯 Next: Test Your Setup

1. Press F5
2. Wait 30 seconds for all services to start
3. Open PowerShell and run health checks
4. Try accessing http://localhost:5000 in browser (Gateway)
5. Set a breakpoint in code and trigger it

**All working?** You can now:
- ✅ Develop locally without Docker
- ✅ Debug services independently
- ✅ Test end-to-end flows
- ✅ Iterate quickly with F5

---

**More help?** See [PHASE11_MULTIPLE_STARTUP_PROJECTS.md](PHASE11_MULTIPLE_STARTUP_PROJECTS.md)
