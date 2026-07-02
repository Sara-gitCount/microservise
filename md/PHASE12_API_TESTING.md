# Phase 12: API Testing with curl

## 🎯 Objective

Test all API endpoints using curl to verify gateway routing, authentication, and service functionality.

## 📋 Prerequisites

- All 5 services running (F5)
- curl installed (Windows 10+)
- PowerShell or Command Prompt
- ~20 minutes

## 🚀 Quick Setup

### Save JWT Token to Variable

```powershell
# After login, save token:
$token = "YOUR_JWT_TOKEN_HERE"

# Use in headers:
curl -H "Authorization: Bearer $token" http://localhost:5000/auth/validate
```

## 🔑 Phase 1: Authentication Flow

### 1.1 Register New User

```powershell
# Register new user
curl -X POST http://localhost:5000/auth/register `
  -H "Content-Type: application/json" `
  -d '{
    "email": "test@example.com",
    "password": "Test123!@#",
    "fullName": "Test User"
  }'

# Expected response:
# {
#   "id": "user-guid",
#   "email": "test@example.com",
#   "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
#   "message": "User registered successfully"
# }

# Save token from response
$token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### 1.2 Login User

```powershell
# Login with credentials
curl -X POST http://localhost:5000/auth/login `
  -H "Content-Type: application/json" `
  -d '{
    "email": "test@example.com",
    "password": "Test123!@#"
  }'

# Expected response:
# {
#   "id": "user-guid",
#   "email": "test@example.com",
#   "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
#   "message": "Login successful"
# }

$token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### 1.3 Validate Token (Without Token)

```powershell
# Without token (should get 401)
curl http://localhost:5000/auth/validate

# Expected response:
# HTTP 401 Unauthorized
# {
#   "message": "Unauthorized",
#   "code": 401
# }
```

### 1.4 Validate Token (With Token)

```powershell
# With valid token (should get 200)
curl -H "Authorization: Bearer $token" `
  http://localhost:5000/auth/validate

# Expected response:
# HTTP 200 OK
# {
#   "id": "user-guid",
#   "email": "test@example.com",
#   "isValid": true
# }
```

### 1.5 Refresh Token (if implemented)

```powershell
# Refresh token
curl -X POST http://localhost:5000/auth/refresh `
  -H "Authorization: Bearer $token" `
  -H "Content-Type: application/json" `
  -d '{ "token": "'$token'" }'

# Expected: New JWT token with extended expiration
```

## 📦 Phase 2: Gateway Routing Tests

### 2.1 Test Gateway Health

```powershell
# Gateway health
curl http://localhost:5000/health

# Expected response:
# HTTP 200 OK
# {
#   "status": "healthy",
#   "timestamp": "2024-07-02T10:00:00Z"
# }
```

### 2.2 Test Auth Service via Gateway

```powershell
# Auth service through gateway
curl -H "Authorization: Bearer $token" `
  http://localhost:5000/auth/health

# Expected: 200 OK
```

### 2.3 Test Catalog Service via Gateway

```powershell
# Catalog service through gateway
curl http://localhost:5000/catalog/health

# Expected: 200 OK
```

### 2.4 Test Order Service via Gateway

```powershell
# Order service through gateway
curl -H "Authorization: Bearer $token" `
  http://localhost:5000/orders/health

# Expected: 200 OK
```

### 2.5 Test Lottery Service via Gateway

```powershell
# Lottery service through gateway
curl -H "Authorization: Bearer $token" `
  http://localhost:5000/lottery/health

# Expected: 200 OK
```

## 🏪 Phase 3: Catalog Service Tests

### 3.1 List All Gifts (No Auth)

```powershell
# Get all gifts (no token required)
curl http://localhost:5000/catalog/gifts

# Expected response:
# HTTP 200 OK
# [
#   {
#     "giftId": 1,
#     "name": "Gift 1",
#     "description": "Description",
#     "price": 29.99,
#     "stock": 100,
#     "category": "Electronics",
#     "imageUrl": "https://...",
#     "createdAt": "2024-07-01T00:00:00Z"
#   },
#   ...
# ]
```

### 3.2 Get Gift Details

```powershell
# Get specific gift
curl http://localhost:5000/catalog/gifts/1

# Expected response:
# HTTP 200 OK
# {
#   "giftId": 1,
#   "name": "Gift 1",
#   "description": "Description",
#   "price": 29.99,
#   "stock": 100,
#   "category": "Electronics"
# }
```

### 3.3 Search Gifts

```powershell
# Search gifts by name
curl http://localhost:5000/catalog/gifts/search?q=laptop

# Expected: Matching gifts
```

### 3.4 Filter by Category

```powershell
# Filter by category
curl http://localhost:5000/catalog/gifts/category/Electronics

# Expected: Gifts in that category
```

### 3.5 Check Gift Stock

```powershell
# Check if gift is in stock
curl http://localhost:5000/catalog/gifts/1/stock

# Expected response:
# HTTP 200 OK
# {
#   "giftId": 1,
#   "available": true,
#   "quantity": 100
# }
```

## 🛒 Phase 4: Order Service Tests

### 4.1 Create Order (Cross-Service)

```powershell
# Create new order (requires auth)
curl -X POST http://localhost:5000/orders `
  -H "Authorization: Bearer $token" `
  -H "Content-Type: application/json" `
  -d '{
    "giftId": 1,
    "quantity": 2,
    "totalPrice": 59.98
  }'

# Expected response:
# HTTP 201 Created
# {
#   "orderId": 1,
#   "userId": "user-guid",
#   "giftId": 1,
#   "quantity": 2,
#   "totalPrice": 59.98,
#   "status": "pending",
#   "createdAt": "2024-07-02T10:00:00Z"
# }

$orderId = 1
```

### 4.2 Get User Orders

```powershell
# Get all orders for current user
curl -H "Authorization: Bearer $token" `
  http://localhost:5000/orders/my-orders

# Expected response:
# HTTP 200 OK
# [
#   {
#     "orderId": 1,
#     "userId": "user-guid",
#     "giftId": 1,
#     "quantity": 2,
#     "totalPrice": 59.98,
#     "status": "pending",
#     "createdAt": "2024-07-02T10:00:00Z"
#   },
#   ...
# ]
```

### 4.3 Get Order Details

```powershell
# Get specific order
curl -H "Authorization: Bearer $token" `
  http://localhost:5000/orders/$orderId

# Expected response:
# HTTP 200 OK
# {
#   "orderId": 1,
#   "userId": "user-guid",
#   "giftId": 1,
#   "quantity": 2,
#   "totalPrice": 59.98,
#   "status": "pending"
# }
```

### 4.4 Update Order Status

```powershell
# Update order status (admin only)
curl -X PUT http://localhost:5000/orders/$orderId/status `
  -H "Authorization: Bearer $token" `
  -H "Content-Type: application/json" `
  -d '{ "status": "completed" }'

# Expected: 200 OK with updated order
```

### 4.5 Cancel Order

```powershell
# Cancel order
curl -X DELETE http://localhost:5000/orders/$orderId `
  -H "Authorization: Bearer $token"

# Expected response:
# HTTP 200 OK
# { "message": "Order cancelled successfully" }
```

### 4.6 Get Order Revenue (Admin)

```powershell
# Get total revenue (admin only)
curl -H "Authorization: Bearer $adminToken" `
  http://localhost:5000/orders/admin/revenue

# Expected response:
# HTTP 200 OK
# {
#   "totalRevenue": 1234.56,
#   "completedOrders": 42,
#   "pendingOrders": 5,
#   "cancelledOrders": 2
# }
```

## 🎰 Phase 5: Lottery Service Tests

### 5.1 List Draws

```powershell
# Get all lottery draws
curl http://localhost:5000/lottery/draws

# Expected response:
# HTTP 200 OK
# [
#   {
#     "drawId": 1,
#     "name": "Weekly Draw",
#     "drawDate": "2024-07-09T00:00:00Z",
#     "prize": 1000.00,
#     "status": "scheduled"
#   },
#   ...
# ]
```

### 5.2 Get Draw Details

```powershell
# Get specific draw
curl http://localhost:5000/lottery/draws/1

# Expected response:
# HTTP 200 OK
# {
#   "drawId": 1,
#   "name": "Weekly Draw",
#   "drawDate": "2024-07-09T00:00:00Z",
#   "prize": 1000.00,
#   "status": "scheduled",
#   "winnerId": null
# }
```

### 5.3 Enter Lottery (User)

```powershell
# Enter lottery draw
curl -X POST http://localhost:5000/lottery/enter `
  -H "Authorization: Bearer $token" `
  -H "Content-Type: application/json" `
  -d '{ "drawId": 1 }'

# Expected response:
# HTTP 201 Created
# {
#   "ticketId": 1,
#   "userId": "user-guid",
#   "drawId": 1,
#   "enteredAt": "2024-07-02T10:00:00Z"
# }
```

### 5.4 Conduct Draw (Admin)

```powershell
# Conduct draw and pick winner
curl -X POST http://localhost:5000/lottery/draws/1/conduct `
  -H "Authorization: Bearer $adminToken" `
  -H "Content-Type: application/json" `
  -d '{ }'

# Expected response:
# HTTP 200 OK
# {
#   "drawId": 1,
#   "status": "completed",
#   "winnerId": "winner-user-guid",
#   "winnerEmail": "winner@example.com"
# }
```

## 🔒 Phase 6: Security Tests

### 6.1 Test 401 Without Token

```powershell
# Try to access protected endpoint without token
curl http://localhost:5000/orders

# Expected: HTTP 401 Unauthorized
```

### 6.2 Test 401 With Invalid Token

```powershell
# Try with invalid token
curl -H "Authorization: Bearer invalid-token-xyz" `
  http://localhost:5000/orders

# Expected: HTTP 401 Unauthorized
```

### 6.3 Test Expired Token

```powershell
# Use expired token (after TTL)
curl -H "Authorization: Bearer $expiredToken" `
  http://localhost:5000/orders

# Expected: HTTP 401 Unauthorized
```

### 6.4 Test CORS Headers

```powershell
# Check CORS headers
curl -i http://localhost:5000/

# Expected headers:
# Access-Control-Allow-Origin: *
# Access-Control-Allow-Methods: GET, POST, PUT, DELETE
```

### 6.5 Test SQL Injection Prevention

```powershell
# Attempt SQL injection in gift name search
curl "http://localhost:5000/catalog/gifts/search?q='; DROP TABLE Gifts; --"

# Expected: No error, returns empty or safe result
```

## 📊 Phase 7: Load Testing (Optional)

### 7.1 Simple Load Test

```powershell
# Run 10 requests
for ($i=1; $i -le 10; $i++) {
  curl http://localhost:5000/catalog/gifts | Out-Null
  Write-Host "Request $i completed"
}
```

### 7.2 Stress Test Order Creation

```powershell
# Create multiple orders rapidly
for ($i=1; $i -le 5; $i++) {
  curl -X POST http://localhost:5000/orders `
    -H "Authorization: Bearer $token" `
    -H "Content-Type: application/json" `
    -d '{
      "giftId": 1,
      "quantity": 1,
      "totalPrice": 29.99
    }' | Out-Null
  Write-Host "Order $i created"
}
```

## 📋 Testing Checklist

### Authentication (Phase 1)
- [ ] Register new user
- [ ] Login existing user
- [ ] Get JWT token
- [ ] Validate token (with token)
- [ ] Validate token (without token) → 401

### Gateway Routing (Phase 2)
- [ ] Gateway health check
- [ ] Auth service via gateway
- [ ] Catalog service via gateway
- [ ] Order service via gateway
- [ ] Lottery service via gateway

### Catalog Service (Phase 3)
- [ ] List all gifts
- [ ] Get gift details
- [ ] Search gifts
- [ ] Filter by category
- [ ] Check stock availability

### Order Service (Phase 4)
- [ ] Create order
- [ ] Get user orders
- [ ] Get order details
- [ ] Update order status
- [ ] Cancel order
- [ ] Calculate total revenue

### Lottery Service (Phase 5)
- [ ] List draws
- [ ] Get draw details
- [ ] Enter lottery
- [ ] Conduct draw
- [ ] Verify winner

### Security (Phase 6)
- [ ] 401 without token
- [ ] 401 with invalid token
- [ ] 401 with expired token
- [ ] CORS headers present
- [ ] SQL injection prevention

## 🔍 Interpreting Responses

### Success Responses
```
HTTP 200 OK       - Request succeeded
HTTP 201 Created  - Resource created
HTTP 204 No Content - Success, no content
```

### Client Errors
```
HTTP 400 Bad Request       - Invalid input
HTTP 401 Unauthorized      - No/invalid token
HTTP 403 Forbidden         - No permission
HTTP 404 Not Found         - Resource not found
HTTP 409 Conflict          - Duplicate resource
```

### Server Errors
```
HTTP 500 Internal Server Error - Server error
HTTP 503 Service Unavailable   - Service down
```

## 🚨 Common Issues

### Issue: "Connection refused"
```
Cause: Service not running
Solution: Ensure all services started with F5
```

### Issue: "401 Unauthorized"
```
Cause: No/invalid token
Solution: Register/login first, copy token exactly
```

### Issue: "404 Not Found"
```
Cause: Wrong endpoint or service down
Solution: Check port number, verify service running
```

### Issue: "Response too large"
```
Cause: Many results returned
Solution: Use pagination or filtering
```

## 💾 Save Test Scripts

Create `test-apis.ps1`:

```powershell
# Test all APIs

# 1. Register
$user = curl -X POST http://localhost:5000/auth/register `
  -H "Content-Type: application/json" `
  -d '{
    "email": "test@example.com",
    "password": "Test123!",
    "fullName": "Test User"
  }' | ConvertFrom-Json

$token = $user.token

# 2. Test Catalog
curl http://localhost:5000/catalog/gifts | ConvertFrom-Json

# 3. Create Order
curl -X POST http://localhost:5000/orders `
  -H "Authorization: Bearer $token" `
  -H "Content-Type: application/json" `
  -d '{ "giftId": 1, "quantity": 1, "totalPrice": 29.99 }' | ConvertFrom-Json

Write-Host "All tests passed!"
```

## 📞 Next Steps

✅ APIs tested and working?
👉 Continue to [PHASE12_TESTING_SCENARIOS.md](PHASE12_TESTING_SCENARIOS.md)

❌ API issues?
👉 Check [PHASE12_TROUBLESHOOTING.md](PHASE12_TROUBLESHOOTING.md)

---

**Phase 12 API Testing ensures all endpoints work correctly!**
