# Microservices Business Logic Testing Guide

## Overview
This document outlines the business logic in each microservice that should be tested with unit tests. Focus is on non-trivial logic beyond simple CRUD operations.

---

## 1. AuthService (UsersService)

### Service: `IAuthService` / `UsersService`

#### Business Logic to Test:

##### A. **User Registration** (`RegisterAsync`)
**Complexity: HIGH** - Multi-step validation workflow

```csharp
public async Task<(bool Success, int UserId, string Message)> RegisterAsync(
    string firstName, string lastName, string email, string password, string phoneNumber)
```

**Business Rules to Test:**
1. ✅ Email validation using `ValidationHelpers.IsValidEmail()`
2. ✅ Password strength validation using `ValidationHelpers.IsValidPassword()`
3. ✅ Duplicate email detection - reject if email already exists
4. ✅ Password hashing with BCrypt (salt level 12)
5. ✅ User creation with default role "user"
6. ✅ CreatedAt timestamp set to UTC now
7. ✅ Error handling for all validation failures

**Test Scenarios:**
- Valid registration with all fields
- Registration with invalid email format
- Registration with weak password
- Registration with existing email (duplicate)
- Registration with null/empty email or password
- Verify password is hashed (not stored as plaintext)
- Verify correct user ID returned on success

---

##### B. **User Login** (`LoginAsync`)
**Complexity: HIGH** - Authentication workflow with token generation

```csharp
public async Task<(bool Success, string Token, int UserId, string Message)> LoginAsync(
    string email, string password)
```

**Business Rules to Test:**
1. ✅ Email/password validation (cannot be null/whitespace)
2. ✅ User lookup by email from repository
3. ✅ Non-existent user rejection (generic error message for security)
4. ✅ Password verification using BCrypt (compare against stored hash)
5. ✅ Invalid password rejection (generic error message for security)
6. ✅ JWT token generation on success (includes userId, email, firstName, lastName, role)
7. ✅ Return token and userId on successful login
8. ✅ Logging of failed/successful attempts

**Test Scenarios:**
- Successful login with correct credentials
- Login with non-existent email
- Login with correct email but wrong password
- Login with empty email/password
- Verify JWT token contains correct claims
- Verify JWT token can be used for subsequent requests
- Test security: verify generic error messages don't leak user existence

---

##### C. **Token Validation** (`ValidateTokenAsync`)
**Complexity: MEDIUM** - Token verification with DB cross-check

```csharp
public async Task<(bool Success, int UserId, string Message)> ValidateTokenAsync(string token)
```

**Business Rules to Test:**
1. ✅ Null/empty token rejection
2. ✅ Invalid token rejection (JWT validation fails)
3. ✅ Expired token rejection
4. ✅ User existence verification in database (prevents using tokens for deleted users)
5. ✅ Return userId on valid token

**Test Scenarios:**
- Valid token validation
- Invalid/malformed token
- Expired token
- Token for deleted user (in DB)
- Empty token
- Token with altered claims

---

##### D. **Change Password** (`ChangePasswordAsync`)
**Complexity: MEDIUM** - Password change with verification

```csharp
public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
```

**Business Rules to Test:**
1. ✅ Null/empty password validation
2. ✅ New password strength validation
3. ✅ Old password verification via BCrypt
4. ✅ Password update with new hash
5. ✅ UpdatedAt timestamp set

**Test Scenarios:**
- Successful password change
- Old password incorrect
- New password too weak
- New password same as old
- User not found

---

### Controllers: `UsersController`

**Endpoints to Test:**
- `POST /api/users/login` - Login endpoint
- `POST /api/users/register` - Registration endpoint
- Return appropriate HTTP status codes (200, 201, 400, 401, 500)
- Proper error message formatting in ApiResponse wrapper

---

## 2. OrderService (OrdersService)

### Service: `IOrdersService` / `OrdersService`

#### Business Logic to Test:

##### A. **Create Order** (`CreateOrderAsync`)
**Complexity: VERY HIGH** - Multi-service orchestration with inventory management

```csharp
public async Task<(bool Success, int OrderId, string Message)> CreateOrderAsync(
    int userId, DtoCreateOrderRequest request)
```

**Business Rules to Test:**
1. ✅ Quantity validation (must be > 0)
2. ✅ User existence verification via `AuthServiceClient.UserExistsAsync()`
3. ✅ Gift existence verification via `CatalogServiceClient.GiftExistsAndInStockAsync()`
4. ✅ Inventory check (sufficient stock for requested quantity)
5. ✅ Price retrieval from CatalogService
6. ✅ Total price calculation: `unitPrice × quantity`
7. ✅ Order creation with status "pending"
8. ✅ **CRITICAL**: Reduce gift quantity in CatalogService after order
9. ✅ Inventory reduction must happen AFTER order creation
10. ✅ Logging of all steps

**Test Scenarios:**
- ✅ Successful order creation with valid user and gift
- ✅ Order with quantity 0 or negative (rejection)
- ✅ Order for non-existent user (rejection)
- ✅ Order for non-existent gift (rejection)
- ✅ Order with insufficient stock (rejection)
- ✅ Verify total price calculation is correct
- ✅ Verify order status is "pending"
- ✅ Verify inventory is reduced (or test compensating transaction if reduction fails)
- ✅ Multiple orders for same gift (inventory must be tracked correctly)

**Test Data Scenarios:**
- Order with quantity 1, price $10.00 → Total should be $10.00
- Order with quantity 5, price $20.50 → Total should be $102.50
- Order quantity > available inventory → Rejection
- Order quantity = available inventory → Success, inventory becomes 0

---

##### B. **Get User Orders** (`GetUserOrdersAsync`)
**Complexity: MEDIUM** - With user validation

```csharp
public async Task<IEnumerable<DtoOrders>> GetUserOrdersAsync(int userId)
```

**Business Rules to Test:**
1. ✅ User existence verification via AuthService
2. ✅ Return empty list for non-existent user (or empty if exists but has no orders)
3. ✅ Orders ordered by CreatedAt descending (newest first)
4. ✅ Return mapped DTO objects

**Test Scenarios:**
- Get orders for user with multiple orders
- Get orders for user with no orders
- Get orders for non-existent user

---

##### C. **Cancel Order** (`CancelOrderAsync`)
**Complexity: HIGH** - State machine validation

```csharp
public async Task<(bool Success, string Message)> CancelOrderAsync(int orderId)
```

**Business Rules to Test:**
1. ✅ Order existence check
2. ✅ **CRITICAL**: Only allow cancellation if status is "pending"
3. ✅ Reject cancellation for completed orders (other statuses)
4. ✅ Update status to "cancelled"
5. ✅ Set UpdatedAt timestamp
6. ✅ **TODO/Issue**: Inventory restoration not yet implemented (noted in code)

**Test Scenarios:**
- ✅ Cancel pending order (success)
- ✅ Cancel non-existent order (failure)
- ✅ Cancel already cancelled order (failure - status not pending)
- ✅ Cancel completed order (failure - status not pending)
- ✅ Verify status becomes "cancelled"

---

##### D. **Get Orders Report** (`GetOrdersByGiftAsync`)
**Complexity: MEDIUM** - Aggregation and reporting

```csharp
public async Task<IEnumerable<DtoOrderReport>> GetOrdersByGiftAsync()
```

**Business Rules to Test:**
- Group orders by gift
- Calculate aggregates (count, total quantity, total revenue per gift)

---

### Controllers: `OrdersController`

**Endpoints to Test:**
- `POST /api/orders` - Create order
- `GET /api/orders/{id}` - Get order by ID
- `GET /api/orders/user/{userId}` - Get user's orders
- `POST /api/orders/{id}/cancel` - Cancel order
- Proper HTTP status codes (201 Created, 200 OK, 400 Bad Request, 404 Not Found)

---

## 3. CatalogService

### Service A: `IGiftsService` / `GiftsService`

#### Business Logic to Test:

##### A. **Create Gift** (`CreateGiftAsync`)
**Complexity: MEDIUM** - Validation and inventory setup

```csharp
public async Task<int> CreateGiftAsync(DtoCreateGiftRequest request)
```

**Business Rules to Test:**
1. ✅ Name validation (cannot be null/whitespace)
2. ✅ Quantity validation (must be ≥ 0)
3. ✅ Price validation (must be > 0)
4. ✅ Gift creation with provided data
5. ✅ Return new gift ID

**Test Scenarios:**
- Create gift with all valid fields
- Create gift with null name
- Create gift with negative quantity
- Create gift with zero or negative price
- Create gift with optional description

---

##### B. **Update Gift Quantity** (`UpdateGiftQuantityAsync`)
**Complexity: MEDIUM** - Inventory adjustment (negative and positive)

```csharp
public async Task<bool> UpdateGiftQuantityAsync(int giftId, int quantityChange)
```

**Business Rules to Test:**
1. ✅ Gift existence check
2. ✅ Quantity change (can be positive or negative)
3. ✅ New quantity calculation: `current + change`
4. ✅ Prevent negative inventory (or allow and handle appropriately)

**Test Scenarios:**
- Increase quantity by positive number
- Decrease quantity by negative number
- Reduce to zero quantity
- Attempt to reduce below zero (should fail or return false)
- Update non-existent gift (failure)

---

##### C. **Get Gifts by Price Range** (`GetGiftsByPriceRangeAsync`)
**Complexity: MEDIUM** - Range filtering with validation

```csharp
public async Task<IEnumerable<DtoGifts>> GetGiftsByPriceRangeAsync(
    decimal minPrice, decimal maxPrice)
```

**Business Rules to Test:**
1. ✅ Price range validation:
   - minPrice ≥ 0
   - maxPrice ≥ 0
   - minPrice ≤ maxPrice
2. ✅ Return empty list for invalid ranges
3. ✅ Return gifts within range (inclusive)
4. ✅ Exclude gifts outside range

**Test Scenarios:**
- Get gifts between $10 and $50
- Get gifts between $0 and $100
- Invalid range (min > max) → empty result
- Negative price range → empty result
- Gifts at boundary prices ($10, $50) should be included

---

##### D. **Get Filtered Gifts** (`GetFilteredGiftsAsync`)
**Complexity: HIGH** - Complex filtering with multiple criteria

```csharp
public async Task<IEnumerable<DtoGifts>> GetFilteredGiftsAsync(DtoGiftFilter filter)
```

**Business Rules to Test:**
1. ✅ Filter by CategoryId (if provided)
2. ✅ Filter by DonorId (if provided)
3. ✅ Filter by price range (MinPrice and MaxPrice)
4. ✅ **CRITICAL**: Search filter by name/description (case-insensitive)
5. ✅ Combine all filters (AND logic)
6. ✅ Return only gifts matching ALL criteria

**Test Scenarios:**
- Filter by category alone
- Filter by donor alone
- Filter by price range alone
- Filter by search term alone
- Combined filter: category + price range + search
- Case-insensitive search (e.g., "GIFT" matches "gift")
- Partial search (e.g., "gad" matches "gadget")
- Search in description field as well as name
- No filters provided → return all gifts

**Example Test Data:**
```
Gift 1: Name="Red Notebook", Description="Small red notebook", Category=1, Donor=2, Price=$10
Gift 2: Name="Blue Pen", Description="Blue ballpoint pen", Category=1, Donor=3, Price=$5
Gift 3: Name="Notebook Set", Description="Large notebook collection", Category=2, Donor=2, Price=$25

Filter: SearchTerm="notebook" → Returns Gift 1, Gift 3
Filter: Category=1 → Returns Gift 1, Gift 2
Filter: Price $10-$30 → Returns Gift 1, Gift 3
Filter: Category=1 AND Price $5-$15 → Returns Gift 1, Gift 2
```

---

### Service B: `IDonorsService` / `DonorsService`

**Complexity: LOW** - Mostly standard CRUD with input validation

**Business Rules:**
1. ✅ Name is required for donor creation
2. ✅ Optional fields: Email, PhoneNumber, Address, City, Country
3. ✅ Partial updates (only update provided fields)

---

### Service C: `ICategoriesService` / `CategoriesService`

**Complexity: LOW** - Standard CRUD with input validation

**Business Rules:**
1. ✅ Name is required for category creation
2. ✅ Optional description field

---

## 4. LotteryService (LotteryDrawService)

### Service: `ILotteryService` / `LotteryDrawService`

#### Business Logic to Test:

##### A. **Create Lottery Ticket** (`CreateLotteryTicketAsync`)
**Complexity: MEDIUM** - Multi-service validation

```csharp
public async Task<(bool Success, int TicketId, string Message)> CreateLotteryTicketAsync(
    int userId, int giftId)
```

**Business Rules to Test:**
1. ✅ User existence verification via AuthService
2. ✅ Gift existence verification via CatalogService
3. ✅ Lottery ticket creation with status "active"
4. ✅ Return ticket ID on success
5. ✅ Appropriate error messages for non-existent user/gift

**Test Scenarios:**
- Create ticket for valid user and gift
- Create ticket for non-existent user (rejection)
- Create ticket for non-existent gift (rejection)
- Multiple tickets for same user (allowed)
- Multiple tickets for same gift (allowed)

---

##### B. **Run Lottery Draw** (`RunLotteryAsync`)
**Complexity: VERY HIGH** - Complex random selection algorithm with state management

```csharp
public async Task<(bool Success, int WinningTicketCount, string Message)> RunLotteryAsync()
```

**Business Rules to Test (CRITICAL):**
1. ✅ Get all active lottery tickets
2. ✅ Return error if no active tickets
3. ✅ **GROUP tickets by GiftId**
4. ✅ **For EACH gift group, randomly select ONE winner**
5. ✅ Update winner status to "won"
6. ✅ Set WonAt timestamp on winning tickets
7. ✅ Mark non-winning tickets as "lost"
8. ✅ Return count of winners

**Test Scenarios (CRITICAL FOR BUSINESS LOGIC):**

```
Scenario 1: Single gift, multiple tickets
- Gift 1: User A, User B, User C tickets (3 total)
- Expected: 1 winner from Gift 1
- Verify: 2 losers, 1 winner

Scenario 2: Multiple gifts, multiple tickets each
- Gift 1: User A, User B tickets (2 total)
- Gift 2: User C, User D, User E tickets (3 total)
- Expected: 1 winner from Gift 1, 1 winner from Gift 2 = 2 winners total
- Verify: 3 losers from Gift 1+2

Scenario 3: No active tickets
- Expected: Failure, no winners

Scenario 4: Fairness testing
- Run lottery multiple times with same data
- Verify winners vary (random selection working)
- Verify probability distribution is approximately uniform
```

**IMPORTANT TEST CASES:**
- ✅ **Randomness test**: Run draw 100 times with 10 tickets for one gift → each ticket should win ~10% of time
- ✅ **Exclusivity test**: Only ONE winner per gift per draw
- ✅ **Status correctness**: Winners have "won" status, losers have "lost" status
- ✅ **Empty draw**: No active tickets → error return

---

##### C. **Get Winners** (`GetWinnersAsync`)
**Complexity: HIGH** - Cross-service data enrichment

```csharp
public async Task<IEnumerable<DtoLotteryWinner>> GetWinnersAsync()
```

**Business Rules to Test:**
1. ✅ Get all winners (status = "won")
2. ✅ For each winner, fetch:
   - User details from AuthService (email, firstName, lastName)
   - Gift details from CatalogService (name)
3. ✅ Combine data in DtoLotteryWinner object
4. ✅ Return enriched winner data

**Test Scenarios:**
- Get winners when no winners exist (empty list)
- Get winners with user/gift data properly enriched
- Verify user email and name are correct
- Verify gift name is correct
- Handle service failures gracefully

---

##### D. **Get Lottery Statistics** (`GetLotteryStatisticsAsync`)
**Complexity: HIGH** - Complex calculations and aggregation

```csharp
public async Task<DtoLotteryStatistics> GetLotteryStatisticsAsync()
```

**Business Rules to Test:**
1. ✅ Calculate **total tickets** (all statuses)
2. ✅ Calculate **winning tickets** (status = "won")
3. ✅ Calculate **losing tickets**: Total - Winning
4. ✅ Calculate **total gift value**: Sum of prices for all won gifts
5. ✅ Return statistics object

**Test Scenarios:**

```
Test Data:
- 5 active tickets (no winners yet)
- 3 won tickets: Gift A ($50), Gift B ($30), Gift C ($25)
- 2 lost tickets

Expected Results:
- TotalTickets = 10
- WinningTickets = 3
- LosingTickets = 7
- TotalGiftValue = $105

Verify Calculation:
- ✅ TotalGiftValue = 50 + 30 + 25 = $105
- ✅ LosingTickets = 10 - 3 = 7
```

**Edge Cases:**
- No tickets at all
- Only active tickets (no won tickets)
- All tickets won
- Large number of tickets (performance test)

---

### Controllers: `LotteryController`

**Endpoints to Test:**
- `POST /api/lottery/tickets` - Create lottery ticket
- `POST /api/lottery/draw` - Run lottery draw
- `GET /api/lottery/winners` - Get all winners
- `GET /api/lottery/user/{userId}/tickets` - Get user's tickets
- `GET /api/lottery/statistics` - Get lottery statistics
- Proper HTTP status codes and error messages

---

## Cross-Service Integration Testing

### Key Integration Points:

1. **OrderService ↔ AuthService**
   - User validation in order creation
   - User lookup for order queries

2. **OrderService ↔ CatalogService**
   - Gift existence check
   - Inventory validation
   - Price retrieval
   - Quantity reduction after order

3. **LotteryService ↔ AuthService**
   - User validation for ticket creation
   - User data enrichment for winner display

4. **LotteryService ↔ CatalogService**
   - Gift validation for ticket creation
   - Gift data enrichment for winner display
   - Price retrieval for statistics

### Integration Test Scenarios:

1. **Cascading Failures**
   - AuthService unreachable → OrderService should handle gracefully
   - CatalogService unreachable → LotteryService should retry/fail gracefully

2. **Data Consistency**
   - Order created → inventory reduced immediately
   - Winner selected → can still retrieve winner gift details
   - User deleted → can still view historical orders

---

## Validation Logic to Test

### Password Validation (`ValidationHelpers.IsValidPassword`)
- Minimum length (typically 8+ characters)
- Must contain uppercase letter
- Must contain lowercase letter
- Must contain number
- Must contain special character

### Email Validation (`ValidationHelpers.IsValidEmail`)
- Valid email format (user@domain.com)
- Reject invalid formats
- Case insensitive comparison

### Price Validation
- Must be > 0 for gifts
- Must be decimal/double
- Currency precision handling

---

## Error Handling & Logging to Test

### Expected Behaviors:
1. ✅ All exceptions caught and logged
2. ✅ Appropriate error messages returned to client
3. ✅ Sensitive data not exposed in error messages
4. ✅ Internal server errors return generic message
5. ✅ Validation errors return 400 Bad Request
6. ✅ Not found errors return 404
7. ✅ Unauthorized errors return 401

---

## Summary: High-Priority Business Logic for Testing

### ⭐ CRITICAL (Very Important):
1. **Order Creation** - Multi-step orchestration, inventory management
2. **Lottery Draw Algorithm** - Random selection fairness, status updates
3. **User Authentication** - Password verification, token generation
4. **Gift Filtering** - Complex multi-criteria filtering with search
5. **Lottery Statistics** - Complex calculations and aggregations

### ⭐ HIGH (Important):
1. User Registration validation (email, password strength)
2. Order Cancellation (status validation)
3. Inventory Reduction
4. Price Calculations
5. User/Gift Cross-Service Validation

### ⭐ MEDIUM (Standard):
1. Basic CRUD operations for gifts, donors, categories
2. Standard validation (name, email, phone)
3. Data mapping and DTO conversion
4. List filtering and sorting

### ⭐ LOW (Routine):
1. GET endpoints for simple data retrieval
2. Basic parameter validation
3. Proper HTTP status codes
4. Response formatting

