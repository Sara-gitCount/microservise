# Phase 12: Quick Verification (5 Minutes)

## ⚡ TL;DR - Verify System is Working

### Step 1: Start All Services (1 minute)
```powershell
# Open Visual Studio
# Press F5

# Wait for all console windows to appear (5 services)
```

### Step 2: Health Check All Services (1 minute)
```powershell
# In PowerShell terminal:

curl http://localhost:5000/health   # Gateway
curl http://localhost:5001/health   # Auth Service
curl http://localhost:5002/health   # Catalog Service
curl http://localhost:5003/health   # Order Service
curl http://localhost:5004/health   # Lottery Service

# All should return HTTP 200 OK
```

### Step 3: Test Authentication (1 minute)
```powershell
# Register new user
curl -X POST http://localhost:5000/auth/register `
  -H "Content-Type: application/json" `
  -d '{
    "email": "test@example.com",
    "password": "Password123!",
    "fullName": "Test User"
  }'

# Copy the JWT token from response
# (or check console for token)
```

### Step 4: Test Gateway Routing (1 minute)
```powershell
# Without token (should get 401)
curl http://localhost:5000/auth/validate

# With token (should succeed)
curl -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" `
  http://localhost:5000/auth/validate
```

### Step 5: Test Catalog (1 minute)
```powershell
# Get gifts (no auth required)
curl http://localhost:5000/catalog/gifts

# Should return list of gifts
```

## ✅ What You're Verifying

| Check | Command | Expected |
|-------|---------|----------|
| **Gateway alive** | `curl http://localhost:5000/health` | 200 OK |
| **Auth Service alive** | `curl http://localhost:5001/health` | 200 OK |
| **Catalog Service alive** | `curl http://localhost:5002/health` | 200 OK |
| **Order Service alive** | `curl http://localhost:5003/health` | 200 OK |
| **Lottery Service alive** | `curl http://localhost:5004/health` | 200 OK |
| **Register user** | `POST /auth/register` | 201 Created with JWT |
| **Login** | `POST /auth/login` | 200 OK with JWT |
| **Gateway routes** | `GET /auth/validate` with token | 200 OK |
| **Catalog works** | `GET /catalog/gifts` | 200 OK with gift list |

## 🎯 Success Criteria

✅ All 5 services responding to health checks  
✅ Can register new user  
✅ Can login and get JWT token  
✅ Can validate token through gateway  
✅ Can fetch catalog without auth  

## 🚨 If Something Fails

| Problem | Solution |
|---------|----------|
| Services won't start | See [PHASE12_TROUBLESHOOTING.md](PHASE12_TROUBLESHOOTING.md) |
| Health check returns 500 | Check console for errors |
| Can't register user | Database might not be initialized |
| Token invalid | Make sure you copied the full token |
| Gateway returns 404 | Service might not be running |

## 📚 Next Steps

- ✅ Quick check complete?
- 👉 Read [PHASE12_COMPREHENSIVE_TESTING.md](PHASE12_COMPREHENSIVE_TESTING.md) for full testing
- 👉 Read [PHASE12_API_TESTING.md](PHASE12_API_TESTING.md) for all API endpoints
- 👉 Read [PHASE12_DATABASE_VERIFICATION.md](PHASE12_DATABASE_VERIFICATION.md) for database check

## ⏱️ Expected Time

- **Total**: 5 minutes
- **Per service**: 30 seconds
- **Full test**: 2 minutes

**Start now**: F5 in VS → Run curl commands above ✨
