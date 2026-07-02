# Phase 12: Frontend Testing (Angular)

## 🎯 Objective

Test Angular frontend integration with the microservices backend through the API Gateway.

## 📋 Prerequisites

- Node.js installed
- Angular CLI installed (`npm install -g @angular/cli`)
- All backend services running (F5)
- ~15 minutes

## 🚀 Step 1: Start Angular Dev Server

### 1.1 Navigate to Frontend Directory

```powershell
cd client/mecira_sinit_Angular
```

### 1.2 Install Dependencies

```powershell
npm install

# Expected: Installs packages
# Time: 2-5 minutes (first time only)
```

### 1.3 Start Dev Server

```powershell
ng serve

# Expected output:
# ✔ Compiled successfully.
# ✔ application bundle 250.34 kB (3.02 s)
# ✔ 1 polyfills bundle 36.04 kB (0.83 s)
# ✔ styles bundle 0.72 kB (0.58 s)
# ✔ Angular application bundle created (4 bundles) in 6.00 seconds.
# ✔ Watch mode enabled. Watching file changes...
# 
# Application bundle generated successfully. (21 seconds)
# Initial Chunk Files | Names        | Raw Size
# main.3fba47ec.js    | main         | 250.34 kB |
# polyfills.e59390d1. | polyfills    | 36.04 kB |
# styles.05c2d6fbbd. | styles       | 0.72 kB  |
#
# | Initial Total | 287.10 kB
#
# Build at: 2024-07-02T10:00:00.000Z - Hash: abc123def456 - Time: 6000ms
#
# ⠋ Watching for file changes...
```

### 1.4 Open in Browser

```
http://localhost:4200
```

Expected: Angular app loads successfully

## 🧪 Step 2: Test Login Page

### 2.1 Verify Login Page Loads

```
Visual check:
- Login form visible
- Email input field present
- Password input field present
- Login button present
- "Register" link present (if applicable)
- No console errors
```

### 2.2 Monitor Network Requests

```
Open browser DevTools: F12
Go to Network tab
Clear network history
```

### 2.3 Invalid Login Attempt

```
Email: invalid@test.com
Password: wrongpassword
Click Login

Expected:
- Error message displayed: "Invalid credentials"
- Network tab shows POST to /auth/login
- Response status: 401 Unauthorized
```

### 2.4 Register New User

```
Click "Register" link (if available)

Enter:
- Email: frontend-test@example.com
- Password: TestPass123!
- Full Name: Frontend Test User
- Confirm Password: TestPass123!

Click Register

Expected:
- Registration succeeds
- Redirected to dashboard or home
- User logged in automatically
- JWT token stored in localStorage
- Network tab shows POST to /auth/register
- Response includes JWT token
```

### 2.5 Verify Network Request

```
DevTools → Network tab
Find /auth/register request

Headers:
✓ Content-Type: application/json
✓ Request payload includes email, password, fullName

Response:
✓ Status: 201 Created
✓ Response includes token
✓ Response includes user ID
```

### 2.6 Verify Token Storage

```
DevTools → Application tab
Go to Local Storage
Look for key like: "authToken" or "jwt"

Expected:
✓ Token stored in localStorage
✓ Token format: jwt (starts with "eyJ")
✓ Can decode token (use jwt.io if needed)
```

## 🏪 Step 3: Test Catalog Page

### 3.1 Navigate to Catalog

```
Click "Catalog" or "Browse Gifts" link
```

### 3.2 Verify Gifts Display

```
Expected:
- List of gifts displayed
- Each gift shows:
  ✓ Name
  ✓ Image (if implemented)
  ✓ Price
  ✓ Description
  ✓ Stock availability
  ✓ "Add to Cart" button
```

### 3.3 Monitor Network Request

```
DevTools → Network tab
Should see GET /catalog/gifts

Check request:
✓ Method: GET
✓ URL: http://localhost:5000/catalog/gifts
✓ No Authorization header (public endpoint)

Check response:
✓ Status: 200 OK
✓ Response is array of gifts
✓ Each gift has required fields
```

### 3.4 Test Gift Details

```
Click on a gift to view details

Expected:
- Full gift details displayed
- Description visible
- Price displayed
- Stock quantity shown
- Add to Cart button present

Network:
- GET /catalog/gifts/{id} request made
- Response includes all gift data
```

### 3.5 Test Search (if implemented)

```
Go to search box
Type: "laptop"

Expected:
- Results filtered
- Only matching gifts shown

Network:
- GET /catalog/gifts/search?q=laptop
- Response includes matching gifts
```

## 🛒 Step 4: Test Shopping Cart

### 4.1 Add Item to Cart

```
Click "Add to Cart" on any gift

Expected:
- Item added to cart
- Cart counter updated
- Notification shown: "Added to cart"
- Cart icon shows quantity
```

### 4.2 View Cart

```
Click cart icon

Expected:
- Cart items displayed
- Each item shows:
  ✓ Product name
  ✓ Quantity (with +/- buttons)
  ✓ Price
  ✓ Subtotal
- Total price calculated
- "Checkout" button present
- "Continue Shopping" button present
```

### 4.3 Modify Quantity

```
In cart, increase quantity to 2

Expected:
- Quantity updated
- Subtotal recalculated
- Total price updated

NO network call should be made yet (local cart)
```

### 4.4 Remove Item

```
Click "Remove" or delete icon

Expected:
- Item removed from cart
- Total recalculated
- Cart counter updated
- If cart empty, show "Cart is empty"
```

## 📦 Step 5: Test Order Creation

### 5.1 Proceed to Checkout

```
Click "Checkout" button

Expected:
- Checkout page loads
- Order summary shown
- Shipping address form (if implemented)
- Payment information form (if implemented)
- "Place Order" button present
```

### 5.2 Place Order

```
Fill in required fields (if any)
Click "Place Order"

Expected:
- POST /orders request made
- Request includes:
  ✓ Authorization header with JWT token
  ✓ Order items
  ✓ Total price
  ✓ Any shipping/payment info
- Response: 201 Created
- Order confirmation page shown
- Order ID displayed
```

### 5.3 Verify Authorization Header

```
DevTools → Network tab
Find POST /orders request

Headers tab:
✓ Authorization: Bearer {JWT_TOKEN}
✓ Content-Type: application/json

Expected:
- Token included automatically
- Token format correct
- No token → should get 401 Unauthorized
```

### 5.4 Verify Order in Backend

```powershell
# Get user's orders
curl -H "Authorization: Bearer $token" `
  http://localhost:5000/orders/my-orders | ConvertFrom-Json

# Should include the order just created
```

### 5.5 Verify Order in Database

```sql
USE OrderDb;
SELECT * FROM Orders 
ORDER BY CreatedAt DESC 
LIMIT 1;

-- Should show order just created
```

## 📜 Step 6: Test Order History

### 6.1 Navigate to Orders

```
Click "My Orders" or "Order History" link
```

### 6.2 Verify Orders Display

```
Expected:
- List of user's orders
- Each order shows:
  ✓ Order ID
  ✓ Date ordered
  ✓ Total amount
  ✓ Status (pending/completed/cancelled)
  ✓ View details link
```

### 6.3 Verify Network Request

```
DevTools → Network tab
Should see GET /orders/my-orders

Check request:
✓ Authorization header present
✓ Method: GET

Check response:
✓ Status: 200 OK
✓ Response is array of orders
✓ Each order has required fields
```

### 6.4 View Order Details

```
Click on an order

Expected:
- Order details displayed
- Items in order shown
- Total price displayed
- Order status shown
- Order date shown
- Possibly option to track/cancel
```

## 🎰 Step 7: Test Lottery (if implemented)

### 7.1 Navigate to Lottery

```
Click "Lottery" or "Try Your Luck" link
```

### 7.2 View Available Draws

```
Expected:
- List of lottery draws
- Each draw shows:
  ✓ Draw name
  ✓ Draw date
  ✓ Prize amount
  ✓ Status (upcoming/completed)
  ✓ "Enter" button (for upcoming draws)
```

### 7.3 Enter Lottery

```
Click "Enter" on a draw

Expected:
- Confirmation: "Entered draw"
- Authorization header sent with token
- Network tab shows POST /lottery/enter
- Response: 201 Created
- Ticket ID shown
```

### 7.4 View My Tickets

```
View lottery section / my tickets

Expected:
- List of entered draws
- Ticket details shown
- Draw results (if completed)
```

## 🔐 Step 8: Test Authentication Features

### 8.1 Test Logout

```
Click "Logout" button

Expected:
- User logged out
- Redirected to login page
- Token removed from localStorage
- User cannot access protected pages
```

### 8.2 Test Session Timeout

```
Wait for token expiration (or manually expire)
Try to make request to protected endpoint

Expected:
- Request fails with 401 Unauthorized
- User redirected to login
- Message: "Session expired, please login again"
```

### 8.3 Test Protected Route Access

```
While logged out:
- Try to access http://localhost:4200/orders
- Try to access http://localhost:4200/checkout

Expected:
- Redirected to login page
- Cannot access without token
```

### 8.4 Test Token Refresh (if implemented)

```
After long time:
- Make request with old token

Expected:
- If expired: 401 Unauthorized
- If refresh implemented: New token obtained
- Request retried with new token
```

## 📊 Step 9: Test Error Handling

### 9.1 Test API Errors

```
Scenario: Create order for non-existent gift
Cause: Backend returns 404

Expected Frontend Behavior:
- Error message displayed
- User can retry or go back
- Console shows no unhandled errors
```

### 9.2 Test Network Errors

```
Disconnect internet or stop backend
Try to make request

Expected:
- User sees: "Connection error"
- Retry option offered
- App doesn't crash
```

### 9.3 Test Form Validation

```
In login form:
- Leave email empty → Try login
- Leave password empty → Try login
- Enter invalid email → Try login

Expected:
- Form errors shown
- No request sent
- User guided to fix errors
```

## 🔍 Step 10: Test Browser Console

### 10.1 Check for Errors

```
DevTools → Console tab

Expected:
✓ No red error messages
✓ No unhandled promise rejections
✓ No uncaught exceptions
✓ Warnings are OK (yellow)
```

### 10.2 Check Network Tab

```
DevTools → Network tab

Expected:
✓ All requests successful (status 200, 201, etc.)
✓ No 404 Not Found errors
✓ No 500 Server errors
✓ Response times acceptable (<500ms)
✓ No failed requests
```

### 10.3 Monitor Performance

```
DevTools → Performance tab

Click Record
Perform user actions
Click Stop

Expected:
✓ Load time < 3 seconds
✓ First paint < 1 second
✓ Smooth animations (60fps)
```

## 📋 Frontend Testing Checklist

### Login & Registration
- [ ] Login page loads
- [ ] Invalid login shows error
- [ ] Registration form works
- [ ] User can register
- [ ] Token stored in localStorage
- [ ] Token in Authorization header

### Catalog
- [ ] Catalog page loads
- [ ] Gifts displayed
- [ ] Search works (if implemented)
- [ ] Filter works (if implemented)
- [ ] Gift details page works
- [ ] No auth required for catalog

### Shopping
- [ ] Can add items to cart
- [ ] Cart counter updates
- [ ] Can modify quantity
- [ ] Can remove items
- [ ] Cart total calculated correctly

### Orders
- [ ] Can create order
- [ ] Authorization header sent
- [ ] Order confirmation shown
- [ ] Can view order history
- [ ] Can view order details
- [ ] Orders persist

### Lottery
- [ ] Lottery page loads
- [ ] Can view draws
- [ ] Can enter lottery
- [ ] Can view tickets

### Security
- [ ] Can logout
- [ ] Cannot access protected pages without token
- [ ] Token expires as expected
- [ ] Error on invalid token

### Performance
- [ ] No console errors
- [ ] All network requests successful
- [ ] Response times acceptable
- [ ] Smooth animations

### Error Handling
- [ ] API errors handled
- [ ] Network errors handled
- [ ] Form validation works
- [ ] User feedback clear

## 🚨 Common Frontend Issues

### Issue: Login not working
```
Check:
1. Backend auth service running? (curl http://localhost:5001/health)
2. Gateway running? (curl http://localhost:5000/health)
3. Token saved to localStorage?
4. Console shows any errors?
```

### Issue: Cart not working
```
Check:
1. JavaScript errors in console?
2. localStorage enabled in browser?
3. Cart state management working?
```

### Issue: Orders not appearing
```
Check:
1. Authorization header sent?
2. Token valid?
3. Backend order service running?
4. Database has orders?
```

### Issue: Slow performance
```
Check:
1. Network tab - response times
2. Performance tab - slow operations
3. Console - any warnings
4. Network - any failed requests
```

## 💡 Tips

### Debug API Calls
```
DevTools → Network tab
Filter by XHR/Fetch
Click request to see:
- Request headers (including token)
- Request payload
- Response data
- Response timing
```

### Debug State
```
Install Redux DevTools extension (if using Redux)
Or Vue DevTools (if applicable)
Monitor state changes
```

### Debug Errors
```
console.log() in components
DevTools → Console tab
Set breakpoints (DevTools → Sources tab)
Step through code
```

## 📞 Next Steps

✅ Frontend testing complete?
👉 Continue to [PHASE12_DOCKER_TESTING.md](PHASE12_DOCKER_TESTING.md)

❌ Frontend issues?
👉 Check [PHASE12_TROUBLESHOOTING.md](PHASE12_TROUBLESHOOTING.md)

---

**Phase 12 Frontend Testing ensures your UI works with the backend!**
