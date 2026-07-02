# Mechira Sinit - API Reference Guide

## 📚 Quick Navigation

- [API Gateway](#api-gateway)
- [AuthService API](#authservice-api)
- [CatalogService API](#catalogservice-api)
- [OrderService API](#orderservice-api)
- [LotteryService API](#lotteryservice-api)
- [Health Endpoints](#health-endpoints)
- [Error Codes](#error-codes)

---

## API Gateway

**Base URL:** `http://localhost:5000`

### Gateway Features
- Request routing to backend services
- Rate limiting
- JWT validation
- CORS handling

### Routing Rules

| Path | Service | Port |
|------|---------|------|
| `/api/auth/*` | AuthService | 5001 |
| `/api/catalog/*` | CatalogService | 5002 |
| `/api/orders/*` | OrderService | 5003 |
| `/api/lottery/*` | LotteryService | 5004 |

---

## AuthService API

**Base URL:** `http://localhost:5001` or via Gateway: `http://localhost:5000/api/auth`

### 1. Register User

```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "user@example.com",
  "password": "SecurePassword123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Response (201 Created):**
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "username": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "createdAt": "2026-07-02T12:00:00Z"
}
```

**Error Responses:**
- `400 Bad Request` - Invalid input
- `409 Conflict` - Username already exists

---

### 2. Login

```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "tokenType": "Bearer",
  "user": {
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "username": "user@example.com",
    "firstName": "John",
    "lastName": "Doe"
  }
}
```

**Error Responses:**
- `401 Unauthorized` - Invalid credentials
- `404 Not Found` - User not found

---

### 3. Refresh Token

```http
POST /api/auth/refresh
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "tokenType": "Bearer"
}
```

---

## CatalogService API

**Base URL:** `http://localhost:5002` or via Gateway: `http://localhost:5000/api/catalog`

### 1. Get All Products

```http
GET /api/products
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "data": [
    {
      "giftId": 1,
      "name": "Luxury Watch",
      "description": "Premium Swiss watch",
      "price": 299.99,
      "stock": 15,
      "category": "Electronics",
      "createdAt": "2026-07-01T10:00:00Z"
    },
    {
      "giftId": 2,
      "name": "Coffee Maker",
      "description": "Premium coffee maker",
      "price": 149.99,
      "stock": 0,
      "category": "Appliances",
      "createdAt": "2026-07-01T10:00:00Z"
    }
  ],
  "totalCount": 2,
  "pageNumber": 1,
  "pageSize": 50
}
```

**Query Parameters:**
- `pageNumber`: int (default: 1)
- `pageSize`: int (default: 50)
- `category`: string (optional filter)

---

### 2. Get Product by ID

```http
GET /api/products/{id}
Authorization: Bearer {token}
```

**Example:**
```http
GET /api/products/1
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (200 OK):**
```json
{
  "giftId": 1,
  "name": "Luxury Watch",
  "description": "Premium Swiss watch",
  "price": 299.99,
  "stock": 15,
  "category": "Electronics",
  "createdAt": "2026-07-01T10:00:00Z"
}
```

**Response (304 Not Modified):** If cached and not changed
**Response (404 Not Found):** If product doesn't exist

**Caching Behavior:**
- First request: Fetches from DB, stores in Redis (TTL: 10 min)
- Subsequent requests: Returns from Redis cache
- Cache key: `product:1`

---

### 3. Create Product

```http
POST /api/products
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Bluetooth Speaker",
  "description": "Portable Bluetooth speaker",
  "price": 79.99,
  "stock": 25,
  "category": "Electronics"
}
```

**Response (201 Created):**
```json
{
  "giftId": 3,
  "name": "Bluetooth Speaker",
  "description": "Portable Bluetooth speaker",
  "price": 79.99,
  "stock": 25,
  "category": "Electronics",
  "createdAt": "2026-07-02T12:00:00Z"
}
```

---

### 4. Update Product

```http
PUT /api/products/{id}
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Bluetooth Speaker Pro",
  "description": "Premium Bluetooth speaker",
  "price": 89.99,
  "stock": 20,
  "category": "Electronics"
}
```

**Response (200 OK):**
```json
{
  "giftId": 3,
  "name": "Bluetooth Speaker Pro",
  "description": "Premium Bluetooth speaker",
  "price": 89.99,
  "stock": 20,
  "category": "Electronics",
  "updatedAt": "2026-07-02T13:00:00Z"
}
```

**Side Effects:** Cache invalidated for this product

---

### 5. Delete Product

```http
DELETE /api/products/{id}
Authorization: Bearer {token}
```

**Response (204 No Content)**

**Side Effects:** Cache invalidated for this product

---

## OrderService API

**Base URL:** `http://localhost:5003` or via Gateway: `http://localhost:5000/api/orders`

### 1. Create Order (Initiates Order Saga)

```http
POST /api/orders
Authorization: Bearer {token}
Content-Type: application/json

{
  "giftId": 1,
  "quantity": 2,
  "totalPrice": 599.98,
  "items": [
    {
      "giftId": 1,
      "quantity": 2,
      "price": 299.99,
      "name": "Luxury Watch"
    }
  ]
}
```

**Response (201 Created):**
```json
{
  "orderId": "550e8400-e29b-41d4-a716-446655440000",
  "userId": "550e8400-e29b-41d4-a716-446655440001",
  "status": "Pending",
  "totalPrice": 599.98,
  "createdAt": "2026-07-02T12:00:00Z",
  "items": [
    {
      "giftId": 1,
      "quantity": 2,
      "price": 299.99,
      "name": "Luxury Watch"
    }
  ]
}
```

**Saga Flow:**
1. Order created with status "Pending"
2. `OrderPlaced` event published to RabbitMQ
3. CatalogService receives event, checks inventory
4. If in stock → `InventoryReserved` event → Order status → "Confirmed" → Email sent
5. If out of stock → `InventoryFailed` event → Order status → "Cancelled" → Cancel email sent

**Error Responses:**
- `400 Bad Request` - Invalid order data
- `401 Unauthorized` - Missing/invalid token
- `404 Not Found` - Gift not found

---

### 2. Get All Orders

```http
GET /api/orders
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "data": [
    {
      "orderId": "550e8400-e29b-41d4-a716-446655440000",
      "userId": "550e8400-e29b-41d4-a716-446655440001",
      "status": "Confirmed",
      "totalPrice": 599.98,
      "createdAt": "2026-07-02T12:00:00Z",
      "updatedAt": "2026-07-02T12:00:05Z"
    }
  ],
  "totalCount": 1,
  "pageNumber": 1,
  "pageSize": 50
}
```

---

### 3. Get Order by ID

```http
GET /api/orders/{orderId}
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "orderId": "550e8400-e29b-41d4-a716-446655440000",
  "userId": "550e8400-e29b-41d4-a716-446655440001",
  "status": "Confirmed",
  "totalPrice": 599.98,
  "createdAt": "2026-07-02T12:00:00Z",
  "updatedAt": "2026-07-02T12:00:05Z",
  "items": [
    {
      "giftId": 1,
      "quantity": 2,
      "price": 299.99,
      "name": "Luxury Watch"
    }
  ]
}
```

---

### 4. Update Order Status

```http
PUT /api/orders/{orderId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "status": "Shipped"
}
```

**Response (200 OK):**
```json
{
  "orderId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Shipped",
  "updatedAt": "2026-07-02T13:00:00Z"
}
```

---

## LotteryService API

**Base URL:** `http://localhost:5004` or via Gateway: `http://localhost:5000/api/lottery`

### 1. Get All Lotteries

```http
GET /api/lotteries
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "data": [
    {
      "lotteryId": 1,
      "name": "Summer Raffle 2026",
      "description": "Win amazing prizes",
      "drawDate": "2026-08-15T18:00:00Z",
      "maxParticipants": 1000,
      "ticketPrice": 10.00,
      "status": "Active"
    }
  ],
  "totalCount": 1
}
```

---

### 2. Create Lottery

```http
POST /api/lotteries
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Fall Raffle 2026",
  "description": "Win amazing fall prizes",
  "drawDate": "2026-09-15T18:00:00Z",
  "maxParticipants": 500,
  "ticketPrice": 15.00
}
```

**Response (201 Created):**
```json
{
  "lotteryId": 2,
  "name": "Fall Raffle 2026",
  "description": "Win amazing fall prizes",
  "drawDate": "2026-09-15T18:00:00Z",
  "maxParticipants": 500,
  "ticketPrice": 15.00,
  "status": "Active",
  "createdAt": "2026-07-02T12:00:00Z"
}
```

---

## Health Endpoints

### Liveness Probe (Service Running?)

```http
GET /api/health/live
```

**Response (200 OK) - Always:**
```json
{
  "status": "Live",
  "service": "OrderService",
  "timestamp": "2026-07-02T12:00:00Z"
}
```

---

### Readiness Probe (Ready to Accept Requests?)

```http
GET /api/health/ready
```

**Response (200 OK) - Ready:**
```json
{
  "status": "Ready",
  "service": "OrderService",
  "database": "Available"
}
```

**Response (503 Service Unavailable) - Not Ready:**
```json
{
  "status": "NotReady",
  "service": "OrderService",
  "database": "Unavailable"
}
```

---

### Overall Health

```http
GET /api/health
```

**Response (200 OK) - Healthy:**
```json
{
  "status": "Healthy",
  "service": "CatalogService",
  "database": "Connected",
  "cache": "Connected",
  "version": "1.0.0",
  "timestamp": "2026-07-02T12:00:00Z"
}
```

**Response (200 OK) - Degraded:**
```json
{
  "status": "Degraded",
  "service": "CatalogService",
  "database": "Connected",
  "cache": "Disconnected",
  "version": "1.0.0",
  "timestamp": "2026-07-02T12:00:00Z"
}
```

**Response (503 Service Unavailable) - Unhealthy:**
```json
{
  "status": "Unhealthy",
  "service": "OrderService",
  "database": "Disconnected",
  "error": "Cannot connect to database",
  "timestamp": "2026-07-02T12:00:00Z"
}
```

---

## Error Codes

### HTTP Status Codes

| Code | Meaning | Handling |
|------|---------|----------|
| `200` | OK | Request succeeded |
| `201` | Created | Resource created successfully |
| `204` | No Content | Resource deleted/updated successfully |
| `304` | Not Modified | Cached response is still valid |
| `400` | Bad Request | Invalid input/validation error |
| `401` | Unauthorized | Missing/invalid authentication |
| `403` | Forbidden | Authenticated but not authorized |
| `404` | Not Found | Resource not found |
| `409` | Conflict | Resource conflict (e.g., duplicate) |
| `500` | Internal Server Error | Server error, check logs |
| `503` | Service Unavailable | Service is down/unhealthy |

### Common Error Responses

**Invalid Credentials:**
```json
{
  "error": "Unauthorized",
  "message": "Invalid username or password",
  "statusCode": 401
}
```

**Validation Error:**
```json
{
  "error": "Bad Request",
  "message": "Validation failed",
  "errors": {
    "quantity": ["Quantity must be greater than 0"],
    "price": ["Price must be positive"]
  },
  "statusCode": 400
}
```

**Service Down:**
```json
{
  "error": "Service Unavailable",
  "message": "Database connection failed",
  "statusCode": 503,
  "retryAfter": 30
}
```

---

## Authentication

### JWT Token Format

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
```

### Headers Required

```http
Authorization: Bearer {token}
Content-Type: application/json
X-Correlation-Id: 550e8400-e29b-41d4-a716-446655440000 (optional)
```

---

## Rate Limiting

**API Gateway:** 100 requests per minute per IP

**Response Headers:**
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1656849600
```

**Rate Limit Exceeded:**
```
429 Too Many Requests
```

---

## Pagination

All list endpoints support pagination:

```http
GET /api/products?pageNumber=1&pageSize=50
```

**Response:**
```json
{
  "data": [...],
  "totalCount": 150,
  "pageNumber": 1,
  "pageSize": 50,
  "totalPages": 3,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

---

## Correlation ID

Every request should include a correlation ID for tracing:

```http
GET /api/products
X-Correlation-Id: 550e8400-e29b-41d4-a716-446655440000
```

**The correlation ID appears in:**
- Service logs
- Error responses
- Event messages
- Cross-service calls

---

**API Version:** 1.0  
**Last Updated:** 2026-07-02  
**Maintained by:** Development Team
