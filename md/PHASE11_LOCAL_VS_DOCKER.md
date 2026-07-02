# Phase 11: Local Development vs Docker Comparison

## When to Use Which Approach

### Use Local Development (Visual Studio F5) When:
- ✅ Active development and debugging
- ✅ Setting breakpoints and stepping through code
- ✅ Rapid iteration and testing
- ✅ Testing new features
- ✅ Performance profiling with debugger
- ✅ You have .NET 9.0 SDK installed
- ✅ Fast feedback loop needed

### Use Docker When:
- ✅ Final testing before production
- ✅ Integration testing full stack
- ✅ Testing container-specific issues
- ✅ CI/CD pipeline testing
- ✅ Production deployment simulation
- ✅ Environment parity with production
- ✅ Team collaboration with different machines
- ✅ Testing across different OS

## Quick Comparison

| Aspect | Local (F5) | Docker |
|--------|-----------|--------|
| **Startup Time** | 10-30 sec | 30-60 sec |
| **Resource Use** | 500MB-1GB | 2-4GB |
| **Debugging** | ✅ Full support | ⚠️ Limited |
| **Breakpoints** | ✅ Yes | ❌ No |
| **Code Changes** | ✅ Hot reload | ❌ Rebuild needed |
| **Database** | Local SQL Server | Docker SQL Server |
| **Modification** | Immediate | Rebuild required |
| **Production Match** | ⚠️ Different | ✅ Identical |
| **Port Access** | Direct | Mapped ports |
| **Setup Required** | 10 minutes | 15 minutes |

## Workflow Recommendation

### Daily Development Cycle
```
1. Morning: Start services with F5
2. Work: Edit code, test with F5 (no restart needed for some changes)
3. Debug: Set breakpoints, step through code
4. Before Commit: Test with Docker to match production
5. Push: Code goes to CI/CD which tests with Docker
```

### Development Workflow

#### Option A: Local Development Only
```powershell
# Start all services
F5

# Make code changes
# Save (Ctrl+S)

# Services automatically reload (hot reload enabled)
# Test changes immediately

# When done
Shift+F5  # Stop all
```

#### Option B: Docker for Final Testing
```powershell
# Local development and debugging
F5
# ... make changes and test ...
Shift+F5

# When ready to finalize
docker-compose up --build

# Smoke test in Docker to match production
curl http://localhost:5000/health

# Deploy with confidence
git commit && git push
```

#### Option C: Both in Parallel
```powershell
# Terminal 1: Local development
F5

# Terminal 2: Check Docker
docker-compose up --build

# Switch between them as needed
# - Use F5 for debugging
# - Use Docker for production simulation
```

## Switching Between Local and Docker

### From Local to Docker

1. **Stop Local Services**:
   ```
   Shift+F5 in Visual Studio
   ```

2. **Start Docker**:
   ```powershell
   docker-compose up --build
   ```

3. **Verify Services**:
   ```powershell
   ./test-docker-health.ps1
   ```

### From Docker to Local

1. **Stop Docker**:
   ```powershell
   docker-compose down
   ```

2. **Start Local Services**:
   ```
   F5 in Visual Studio
   ```

3. **Verify Services**:
   ```powershell
   curl http://localhost:5000/health
   ```

## Configuration for Both

### Local Development (appsettings.Development.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(local)\\MSSQLSERVER01;Database=Mechira-sinit-microservices;Integrated Security=true;..."
  },
  "ServiceUrls": {
    "AuthService": "http://localhost:5001",
    "CatalogService": "http://localhost:5002",
    "OrderService": "http://localhost:5003",
    "LotteryService": "http://localhost:5004"
  }
}
```

### Docker (docker-compose.yml)
```yaml
services:
  auth-service:
    environment:
      ConnectionStrings__DefaultConnection: "Server=mssql-db,1433;..."
      ServiceUrls__AuthService: "http://auth-service:5001"
```

### Key Difference
- **Local**: `localhost` with backslash-escaped server name
- **Docker**: Service name with colon, using Docker DNS

## Debugging Capabilities

### Local Debugging (F5)
```
✅ Full debugger support
✅ Set breakpoints
✅ Step into/over/out
✅ Watch variables
✅ Edit and continue (sometimes)
✅ Memory profiling
✅ Performance profiling
✅ IntelliTrace (Premium)
```

### Docker Debugging
```
⚠️ Limited debugger support
❌ No breakpoints without port forwarding
❌ No edit and continue
✅ Can attach debugger with configuration
✅ Can view logs
✅ Can exec into container
```

### Attaching Debugger to Docker Container
```powershell
# If needed, can attach remote debugger
# Requires VSDBG in container and configuration
# Not recommended for most scenarios
```

## Troubleshooting Across Both

### "Database Connection Failed"

**In Local**:
```powershell
# Check local SQL Server
sqlcmd -S (local)\MSSQLSERVER01 -U sa -P password
```

**In Docker**:
```powershell
# Check Docker SQL Server
docker exec mechira-mssql-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P password
```

### "Port Already in Use"

**In Local**:
```powershell
netstat -ano | findstr :5001
taskkill /PID {PID} /F
```

**In Docker**:
```powershell
docker-compose down -v
docker-compose up --build
```

### "Service Can't Connect to Other Service"

**In Local**:
- Check firewall
- Check port is correct
- Try `http://localhost:5001`

**In Docker**:
- Check network is correct
- Try `http://service-name:5001`
- Verify service is running: `docker-compose ps`

## Performance Comparison

### Startup Time

**Local**:
- Cold start: 20-30 seconds (all 5 services)
- Restart after code change: 5-10 seconds (single service)

**Docker**:
- Cold start: 45-60 seconds (first build)
- Rebuild: 30-45 seconds (if images cached)
- Just restart: 10-15 seconds

### Runtime Performance

**Local**:
- Faster (direct execution on OS)
- Closer to development machine native performance

**Docker**:
- Slightly slower (containerization overhead)
- More predictable (environment consistency)
- Better resource isolation

### Memory Usage

**Local**:
- Lower: 500MB-1GB for 5 services
- Direct OS memory access

**Docker**:
- Higher: 2-4GB due to container overhead
- Better resource limits possible

## Hybrid Workflow Example

### Monday: Active Feature Development
```
✅ Use Local (F5)
- Set breakpoints
- Debug service interactions
- Quick iteration
```

### Tuesday: Integration Testing
```
✅ Use Docker
- Test full stack
- Check database persistence
- Simulate production
```

### Wednesday: Bug Investigation
```
✅ Use Local for debugging
- Reproduce locally
- Set breakpoints
- Step through code
- Fix
```

### Thursday: Pre-Deployment Testing
```
✅ Use Docker
- Test in production-like environment
- Verify deployment scripts
- Confirm everything works
```

### Friday: Deployment
```
✅ Docker images pushed to registry
✅ Deployed to cloud
✅ Monitoring verified
```

## Recommendation Summary

| Phase | Use |
|-------|-----|
| **Development** | ✅ Local (F5) |
| **Debugging** | ✅ Local (F5) |
| **Testing Features** | ✅ Local or Docker |
| **Integration Test** | ✅ Docker |
| **Pre-Deploy Check** | ✅ Docker |
| **Production Deploy** | ✅ Docker Images |
| **Production Debug** | ✅ Logs + Monitoring |

**Best Practice**: Use both! Start with local for development, finish with Docker for testing before deployment.
