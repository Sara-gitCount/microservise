# Phase 12: End-to-End Testing Scenarios

## 🎯 Objective

Test complete user workflows that involve multiple services, databases, and cross-service communication.

## 📋 Prerequisites

- All services running (F5)
- curl installed
- SQL Server Management Studio (optional)
- ~15 minutes

## 📖 Scenario 1: User Registration & Authentication Flow

### Scenario Overview
New user registers → Gets JWT token → Uses token to access protected resources

### Step-by-Step

#### 1.1 User Registers
```powershell
$response = curl -X POST http://localhost:5000/auth/register `
  -H "Content-Type: application/json" `
  -d '{
    "email": "alice@example.com",
    "password": "SecurePassword123!",
    "fullName": "Alice Johnson"
  }' | ConvertFrom-Json

Write-Host "Register Response:"
Write-Host $response

# Expected:
# {
#   "id": "alice-guid",
#   "email": "alice@example.com",
#   "token": "eyJhbGc...",
#   "message": "User registered successfully"
# }

$token = $response.token
$userId = $response.id
```

#### 1.2 Verify User in Database
```sql
-- SQL Server Management Studio

USE AuthDb;
SELECT * FROM AspNetUsers 
WHERE Email = 'alice@example.com';

-- Expected: 1 row with user data
```

#### 1.3 User Logs Out (Token Invalidation)
```powershell
curl -X POST http://localhost:5000/auth/logout `
  -H "Authorization: Bearer $token"

# Expected: 200 OK, token now invalid
```

#### 1.4 User Logs Back In
```powershell
$login = curl -X POST http://localhost:5000/auth/login `
  -H "Content-Type: application/json" `
  -d '{
    "email": "alice@example.com",
    "password": "SecurePassword123!"
  }' | ConvertFrom-Json

$token = $login.token

# Expected: New token in response
```

#### 1.5 Validate Token Works
```powershell
curl -H "Authorization: Bearer $token" `
  http://localhost:5000/auth/validate

# Expected: 200 OK with user info
```

### ✅ Scenario 1 Success Criteria
- [x] User created in database
- [x] JWT token generated
- [x] Token validates correctly
- [x] Old token invalidated on logout
- [x] New token works on login

---

## 📖 Scenario 2: Browse Catalog & Create Order

### Scenario Overview
User browses gifts → Finds gift → Creates order → OrderService validates gift exists and user exists

### Step-by-Step

#### 2.1 List All Gifts (No Auth Required)
```powershell
$gifts = curl http://localhost:5000/catalog/gifts | ConvertFrom-Json

Write-Host "Found gifts:"
$gifts | ForEach-Object { Write-Host "  - $($_.name) ($($_.price))" }

# Expected: List of available gifts
# [
#   { "giftId": 1, "name": "Laptop", "price": 799.99, "stock": 10 },
#   { "giftId": 2, "name": "Phone", "price": 499.99, "stock": 25 },
#   ...
# ]

$laptopId = $gifts | Where-Object { $_.name -eq "Laptop" } | Select -ExpandProperty giftId
```

#### 2.2 Verify Catalog in Database
```sql
USE CatalogDb;
SELECT * FROM Gifts;

-- Expected: Multiple gifts with prices, stock, etc.
```

#### 2.3 Check Specific Gift Details
```powershell
$gift = curl http://localhost:5000/catalog/gifts/$laptopId | ConvertFrom-Json

Write-Host "Selected Gift:"
Write-Host "  Name: $($gift.name)"
Write-Host "  Price: $($gift.price)"
Write-Host "  Stock: $($gift.stock)"
Write-Host "  Description: $($gift.description)"
```

#### 2.4 Verify Gift is in Stock
```powershell
$stock = curl http://localhost:5000/catalog/gifts/$laptopId/stock | ConvertFrom-Json

if ($stock.available) {
    Write-Host "Gift is available: $($stock.quantity) in stock"
} else {
    Write-Host "Gift is out of stock"
}
```

#### 2.5 Create Order (Cross-Service Communication)
```powershell
# This call triggers:
# 1. OrderService receives request
# 2. OrderService calls AuthService to validate user
# 3. OrderService calls CatalogService to validate gift
# 4. OrderService decrements gift stock
# 5. OrderService creates order in Orders database

$order = curl -X POST http://localhost:5000/orders `
  -H "Authorization: Bearer $token" `
  -H "Content-Type: application/json" `
  -d '{
    "giftId": '$laptopId',
    "quantity": 1,
    "totalPrice": 799.99
  }' | ConvertFrom-Json

Write-Host "Order Created:"
Write-Host "  OrderId: $($order.orderId)"
Write-Host "  Status: $($order.status)"
Write-Host "  Total: $($order.totalPrice)"

$orderId = $order.orderId
```

#### 2.6 Verify Order in Database
```sql
USE OrderDb;
SELECT * FROM Orders 
WHERE OrderId = $orderId;

-- Expected: 1 row with order details
```

#### 2.7 Verify Gift Stock Decreased
```powershell
$updatedStock = curl http://localhost:5000/catalog/gifts/$laptopId/stock | ConvertFrom-Json

Write-Host "Updated Stock: $($updatedStock.quantity)"
# Expected: Previous stock - 1
```

#### 2.8 Retrieve User's Orders
```powershell
$userOrders = curl -H "Authorization: Bearer $token" `
  http://localhost:5000/orders/my-orders | ConvertFrom-Json

Write-Host "My Orders:"
$userOrders | ForEach-Object { 
    Write-Host "  - Order $($_.orderId): $($_.status) - \$$($_.totalPrice)" 
}

# Expected: Includes the order we just created
```

### ✅ Scenario 2 Success Criteria
- [x] Can browse catalog without auth
- [x] Can fetch individual gift details
- [x] Can check stock availability
- [x] Can create order with valid gift
- [x] Order Service validates user exists
- [x] Order Service validates gift exists
- [x] Gift stock decrements
- [x] Order appears in user's order list
- [x] Order persists in database

---

## 📖 Scenario 3: Invalid Order Attempt (Error Handling)

### Scenario Overview
Test that system properly rejects invalid orders and maintains data integrity

### Step-by-Step

#### 3.1 Attempt to Order Non-Existent Gift
```powershell
$badOrder = curl -X POST http://localhost:5000/orders `
  -H "Authorization: Bearer $token" `
  -H "Content-Type: application/json" `
  -d '{
    "giftId": 99999,
    "quantity": 1,
    "totalPrice": 0
  }' -ErrorAction SilentlyContinue

Write-Host "Response: $badOrder"
# Expected: HTTP 400 Bad Request or 404 Not Found
```

#### 3.2 Attempt to Order More Than Stock
```powershell
# First, check stock
$stock = curl http://localhost:5000/catalog/gifts/1/stock | ConvertFrom-Json
$stockAvailable = $stock.quantity

Write-Host "Available stock: $stockAvailable"

# Try to order more than available
$overOrder = curl -X POST http://localhost:5000/orders `
  -H "Authorization: Bearer $token" `
  -H "Content-Type: application/json" `
  -d '{
    "giftId": 1,
    "quantity": '$($stockAvailable + 100)',
    "totalPrice": 99999
  }' -ErrorAction SilentlyContinue

# Expected: HTTP 400 Bad Request - "Insufficient stock"
```

#### 3.3 Attempt to Access Without Token
```powershell
$noToken = curl -X POST http://localhost:5000/orders `
  -H "Content-Type: application/json" `
  -d '{"giftId": 1, "quantity": 1}' -ErrorAction SilentlyContinue

# Expected: HTTP 401 Unauthorized
```

### ✅ Scenario 3 Success Criteria
- [x] Invalid gift ID rejected
- [x] Insufficient stock rejected
- [x] Missing token rejected
- [x] Proper error messages returned
- [x] No data corruption on errors
- [x] Stock not decremented on failed order

---

## 📖 Scenario 4: Order Status Management

### Scenario Overview
Test order status lifecycle: pending → completed → archived

### Step-by-Step

#### 4.1 Create New Order (Pending Status)
```powershell
$newOrder = curl -X POST http://localhost:5000/orders `
  -H "Authorization: Bearer $token" `
  -H "Content-Type: application/json" `
  -d '{
    "giftId": 2,
    "quantity": 1,
    "totalPrice": 499.99
  }' | ConvertFrom-Json

Write-Host "Order Status: $($newOrder.status)"
# Expected: "pending"

$orderId = $newOrder.orderId
```

#### 4.2 Update to Completed (Admin Function)
```powershell
# Assuming we have admin token
$updated = curl -X PUT http://localhost:5000/orders/$orderId/status `
  -H "Authorization: Bearer $adminToken" `
  -H "Content-Type: application/json" `
  -d '{ "status": "completed" }' | ConvertFrom-Json

Write-Host "Updated Status: $($updated.status)"
# Expected: "completed"
```

#### 4.3 Query Orders by Status
```powershell
# Get all completed orders (admin)
$completed = curl -H "Authorization: Bearer $adminToken" `
  http://localhost:5000/orders/status/completed | ConvertFrom-Json

Write-Host "Completed Orders: $($completed.Count)"
```

#### 4.4 Verify in Database
```sql
USE OrderDb;
SELECT * FROM Orders WHERE OrderId = $orderId;

-- Expected: Status = 'completed', UpdatedAt = now
```

### ✅ Scenario 4 Success Criteria
- [x] Order starts in pending state
- [x] Can update order status
- [x] UpdatedAt timestamp changes
- [x] Can query by status
- [x] Status persists in database

---

## 📖 Scenario 5: Cross-Service Communication

### Scenario Overview
Verify that OrderService successfully calls AuthService and CatalogService during order creation

### Step-by-Step

#### 5.1 Enable Service Logging
```
In Visual Studio console for each service, verify you see logs for:
- Incoming request
- Service logic execution
- Database operations
- Outgoing service calls
```

#### 5.2 Create Order (Watch Console Logs)
```powershell
# Create order and watch console output

$order = curl -X POST http://localhost:5000/orders `
  -H "Authorization: Bearer $token" `
  -H "Content-Type: application/json" `
  -d '{
    "giftId": 1,
    "quantity": 1,
    "totalPrice": 799.99
  }' | ConvertFrom-Json
```

#### 5.3 Verify Service Call Logs

**OrderService Console Should Show:**
```
[INFO] OrderService: Received order creation request
[INFO] OrderService: Calling AuthService to validate user
[HTTP] OrderService: GET http://auth-service:5001/validate
[INFO] AuthService: Validating user (user-guid)
[INFO] AuthService: User validation successful
[HTTP] OrderService: AuthService response received (200 OK)
[INFO] OrderService: Calling CatalogService to validate gift
[HTTP] OrderService: GET http://catalog-service:5002/gifts/1
[INFO] CatalogService: Fetching gift (giftId: 1)
[HTTP] OrderService: CatalogService response received (200 OK)
[INFO] OrderService: Creating order in database
[INFO] OrderService: Order created (orderId: N)
[INFO] OrderService: Order creation successful
```

#### 5.4 Verify Service-to-Service URLs
```
Local Development (F5):
- AuthService: http://localhost:5001
- CatalogService: http://localhost:5002
- OrderService calls via localhost

Docker (docker-compose):
- AuthService: http://auth-service:5001
- CatalogService: http://catalog-service:5002
- OrderService calls via Docker DNS
```

### ✅ Scenario 5 Success Criteria
- [x] OrderService receives request
- [x] OrderService calls AuthService
- [x] AuthService validates user
- [x] OrderService calls CatalogService
- [x] CatalogService validates gift
- [x] Order created in database
- [x] All service calls logged
- [x] Response time acceptable (<1 second)

---

## 📖 Scenario 6: Complete User Journey

### Scenario Overview
One user from registration to order completion

### Complete Flow
```powershell
# 1. REGISTER
$user = curl -X POST http://localhost:5000/auth/register `
  -H "Content-Type: application/json" `
  -d '{ "email": "bob@example.com", "password": "Secure123!", "fullName": "Bob Smith" }' | ConvertFrom-Json
$token = $user.token
Write-Host "✓ Registered: $($user.email)"

# 2. BROWSE CATALOG
$gifts = curl http://localhost:5000/catalog/gifts | ConvertFrom-Json
Write-Host "✓ Browsed catalog: Found $($gifts.Count) gifts"

# 3. VIEW GIFT DETAILS
$gift = curl http://localhost:5000/catalog/gifts/1 | ConvertFrom-Json
Write-Host "✓ Selected: $($gift.name) - \$$($gift.price)"

# 4. CREATE ORDER
$order = curl -X POST http://localhost:5000/orders `
  -H "Authorization: Bearer $token" `
  -H "Content-Type: application/json" `
  -d '{ "giftId": 1, "quantity": 1, "totalPrice": '$($gift.price)' }' | ConvertFrom-Json
Write-Host "✓ Created order: $($order.orderId) - Status: $($order.status)"

# 5. VIEW MY ORDERS
$myOrders = curl -H "Authorization: Bearer $token" `
  http://localhost:5000/orders/my-orders | ConvertFrom-Json
Write-Host "✓ My orders: $($myOrders.Count) orders"

# 6. ENTER LOTTERY
$lottery = curl -X POST http://localhost:5000/lottery/enter `
  -H "Authorization: Bearer $token" `
  -H "Content-Type: application/json" `
  -d '{ "drawId": 1 }' | ConvertFrom-Json
Write-Host "✓ Entered lottery: Ticket $($lottery.ticketId)"

Write-Host ""
Write-Host "✅ Complete user journey successful!"
```

### ✅ Scenario 6 Success Criteria
- [x] User registration works
- [x] Can browse catalog
- [x] Can view gift details
- [x] Can create order
- [x] Can view orders
- [x] Can enter lottery
- [x] All data persists
- [x] Response times acceptable

---

## 📋 Testing Checklist

### Scenario 1: Registration & Auth
- [ ] User registration successful
- [ ] JWT token generated
- [ ] Token validates correctly
- [ ] Logout invalidates token
- [ ] Can login with password
- [ ] New token works

### Scenario 2: Browse & Order
- [ ] Can list all gifts
- [ ] Can view gift details
- [ ] Can check stock
- [ ] Can create order
- [ ] AuthService called for validation
- [ ] CatalogService called for validation
- [ ] Stock decremented
- [ ] Order in database

### Scenario 3: Error Handling
- [ ] Invalid gift rejected
- [ ] Insufficient stock rejected
- [ ] Missing token rejected
- [ ] Proper error messages
- [ ] No data corruption
- [ ] Stock not decremented

### Scenario 4: Order Status
- [ ] Order created as pending
- [ ] Can update status
- [ ] UpdatedAt timestamp changes
- [ ] Can query by status
- [ ] Status persists

### Scenario 5: Cross-Service
- [ ] OrderService calls AuthService
- [ ] OrderService calls CatalogService
- [ ] Service calls logged
- [ ] Response times good
- [ ] No timeout errors

### Scenario 6: Complete Journey
- [ ] All steps succeed
- [ ] Data persists
- [ ] No errors
- [ ] Performance acceptable

## 🚨 Troubleshooting

### Scenario fails with timeout
```
Check: All services started with F5
       No port conflicts
       Network connectivity
```

### Cross-service calls fail
```
Check: Service names are correct
       Firewall allows communication
       Services are running
```

### Data doesn't persist
```
Check: Database is running
       Migrations were applied
       Connection string correct
```

## 📞 Next Steps

✅ All scenarios passed?
👉 Continue to [PHASE12_FRONTEND_TESTING.md](PHASE12_FRONTEND_TESTING.md)

❌ Issues?
👉 Check [PHASE12_TROUBLESHOOTING.md](PHASE12_TROUBLESHOOTING.md)

---

**Phase 12 Scenarios ensure complete end-to-end functionality!**
