# Testing Guide - Mechira Sinit Microservices

## 📋 Table of Contents

1. [Unit Testing](#unit-testing)
2. [Integration Testing](#integration-testing)
3. [Event-Driven Testing](#event-driven-testing)
4. [API Testing](#api-testing)
5. [Load Testing](#load-testing)
6. [Debugging](#debugging)

---

## Unit Testing

### Test Project Structure

```
Services/
├── OrderService/
│   ├── OrderService.csproj
│   ├── OrderService.Tests/
│   │   ├── OrderService.Tests.csproj
│   │   ├── OrdersServiceTests.cs
│   │   ├── Consumers/
│   │   └── obj/
```

### Run Unit Tests

```bash
cd server

# Run all tests
dotnet test

# Run specific test project
dotnet test Services/OrderService/OrderService.Tests/OrderService.Tests.csproj

# Run with verbose output
dotnet test --verbosity normal

# Run tests matching pattern
dotnet test --filter "Category=Unit"

# Generate coverage report
dotnet test /p:CollectCoverageMetrics=true
```

### Example Unit Test - OrderService

**File:** `Services/OrderService/OrderService.Tests/OrdersServiceTests.cs`

```csharp
using Xunit;
using Moq;
using OrderService.Services;
using OrderService.Interfaces;

public class OrdersServiceTests
{
    private readonly Mock<IOrdersRepository> _mockOrdersRepository;
    private readonly IOrdersService _ordersService;

    public OrdersServiceTests()
    {
        _mockOrdersRepository = new Mock<IOrdersRepository>();
        _ordersService = new OrdersService(_mockOrdersRepository.Object);
    }

    [Fact]
    public async Task CreateOrder_ValidInput_ReturnsOrderWithPendingStatus()
    {
        // Arrange
        var createOrderDto = new CreateOrderDto
        {
            UserId = Guid.NewGuid(),
            GiftId = 1,
            Quantity = 2,
            TotalPrice = 599.98m
        };

        // Act
        var result = await _ordersService.CreateOrderAsync(createOrderDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Pending", result.Status);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetOrderById_ValidId_ReturnsOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order { OrderId = orderId, Status = "Confirmed" };
        _mockOrdersRepository.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var result = await _ordersService.GetOrderAsync(orderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(orderId, result.OrderId);
        Assert.Equal("Confirmed", result.Status);
    }

    [Fact]
    public async Task CreateOrder_InvalidQuantity_ThrowsException()
    {
        // Arrange
        var createOrderDto = new CreateOrderDto
        {
            UserId = Guid.NewGuid(),
            GiftId = 1,
            Quantity = 0, // Invalid
            TotalPrice = 0m
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _ordersService.CreateOrderAsync(createOrderDto)
        );
    }
}
```

---

## Integration Testing

### Database Integration Tests

```csharp
using Xunit;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;

public class OrderServiceIntegrationTests : IAsyncLifetime
{
    private readonly OrderDbContext _dbContext;

    public OrderServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;
        _dbContext = new OrderDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task SaveOrder_ValidOrder_PersistsToDatabase()
    {
        // Arrange
        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = "Pending",
            TotalPrice = 599.98m,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        // Assert
        var savedOrder = await _dbContext.Orders.FindAsync(order.OrderId);
        Assert.NotNull(savedOrder);
        Assert.Equal(order.OrderId, savedOrder.OrderId);
        Assert.Equal("Pending", savedOrder.Status);
    }
}
```

### Run Integration Tests

```bash
# Run integration tests only
dotnet test --filter "Category=Integration"

# Run with trace logging
dotnet test --verbosity diagnostic
```

---

## Event-Driven Testing

### Consumer Testing

```csharp
using Xunit;
using MassTransit;
using MassTransit.Testing;
using OrderService.Consumers;
using SharedModels.Events;

public class OrderPlacedConsumerTests
{
    [Fact]
    public async Task OrderPlacedConsumer_ReceivesEvent_UpdatesInventory()
    {
        // Arrange
        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer<OrderPlacedConsumer>();

        await harness.Start();

        var orderId = Guid.NewGuid();
        var orderPlacedEvent = new OrderPlaced
        {
            OrderId = orderId,
            UserId = Guid.NewGuid(),
            GiftId = 1,
            Quantity = 2,
            TotalPrice = 599.98m
        };

        // Act
        await harness.Bus.Publish(orderPlacedEvent);

        // Assert
        Assert.True(await consumerHarness.Consumed.Any<OrderPlaced>());
        var consumed = consumerHarness.Consumed.Select<OrderPlaced>().FirstOrDefault();
        Assert.NotNull(consumed);
        Assert.Equal(orderId, consumed.Context.Message.OrderId);

        await harness.Stop();
    }
}
```

### Test Order Saga Flow

```bash
# Test happy path: Create order with in-stock item
# 1. Verify OrderPlaced event published
# 2. Verify CatalogService receives event
# 3. Verify InventoryReserved event published
# 4. Verify OrderService updates order to "Confirmed"
# 5. Verify OrderConfirmed event published
# 6. Verify NotificationService sends email

# Run saga test
dotnet test --filter "Category=Saga"
```

---

## API Testing

### Manual Testing with cURL

```bash
### 1. Register User
curl -X POST http://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser@example.com",
    "password": "TestPassword123!",
    "firstName": "Test",
    "lastName": "User"
  }'

### 2. Login (Get JWT Token)
curl -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser@example.com",
    "password": "TestPassword123!"
  }'

# Response includes token:
# {
#   "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
#   "expiresIn": 3600
# }

### 3. Create Product (CatalogService)
curl -X POST http://localhost:5002/api/products \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "name": "Luxury Watch",
    "description": "Premium Swiss watch",
    "price": 299.99,
    "stock": 15,
    "category": "Electronics"
  }'

### 4. Get Products (Test Caching)
# First call - cache miss
curl http://localhost:5002/api/products/1 \
  -H "Authorization: Bearer {token}"

# Second call - cache hit
curl http://localhost:5002/api/products/1 \
  -H "Authorization: Bearer {token}"

### 5. Create Order (Initiate Saga)
curl -X POST http://localhost:5003/api/orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
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
  }'

### 6. Check Order Status
curl http://localhost:5003/api/orders/{orderId} \
  -H "Authorization: Bearer {token}"

### 7. Health Checks
# Liveness probe
curl http://localhost:5003/api/health/live

# Readiness probe
curl http://localhost:5003/api/health/ready

# Overall health
curl http://localhost:5003/api/health
```

### Testing with Postman

1. **Create Environment:**
   - Variable: `baseUrl` = `http://localhost:5000`
   - Variable: `token` = (will be set after login)

2. **Create Requests:**
   - POST `/api/auth/login` → Extract token to environment
   - POST `/api/catalog/products` → Requires token
   - GET `/api/catalog/products/1` → Check cache headers
   - POST `/api/orders` → Create order, verify response

3. **Run Collection:** 
   - Postman → Collections → Run Collection
   - Set iterations to test saga flow

---

## Load Testing

### Load Testing with Apache JMeter

**Install JMeter:**
```bash
# Windows (Chocolatey)
choco install jmeter

# macOS (Homebrew)
brew install jmeter

# Linux
wget https://archive.apache.org/dist/jmeter/binaries/apache-jmeter-5.5.zip
unzip apache-jmeter-5.5.zip
```

**Create Test Plan:**

1. Add Thread Group
   - Number of Threads: 100
   - Ramp-Up Period: 10 seconds
   - Loop Count: 10

2. Add HTTP Request
   - Protocol: HTTP
   - Server: localhost
   - Port: 5000
   - Path: /api/products

3. Add Listener (Results)

**Run Test:**
```bash
jmeter -n -t test-plan.jmx -l results.jtl -j jmeter.log
```

### Load Testing with k6

**Install k6:**
```bash
# Windows (Chocolatey)
choco install k6

# macOS (Homebrew)
brew install k6

# Linux
sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3232A
echo "deb https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6
```

**Create Test Script:**

```javascript
// load-test.js
import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
  stages: [
    { duration: '10s', target: 100 },   // Ramp-up
    { duration: '30s', target: 100 },   // Stay
    { duration: '10s', target: 0 },     // Ramp-down
  ],
};

export default function () {
  // Test GET /api/health
  let response = http.get('http://localhost:5003/api/health');
  check(response, {
    'status is 200': (r) => r.status === 200,
    'response time < 100ms': (r) => r.timings.duration < 100,
  });

  // Test GET /api/products
  response = http.get('http://localhost:5002/api/products', {
    headers: {
      'Authorization': `Bearer ${__ENV.TOKEN}`,
    },
  });
  check(response, {
    'product list status is 200': (r) => r.status === 200,
  });

  sleep(1);
}
```

**Run Test:**
```bash
k6 run load-test.js
```

### Expected Load Test Results

| Metric | Target | Acceptable |
|--------|--------|------------|
| Response Time P95 | 100ms | < 200ms |
| Response Time P99 | 200ms | < 500ms |
| Error Rate | 0% | < 1% |
| Throughput | 1000 req/s | > 500 req/s |

---

## Debugging

### Enable Debug Logging

**appsettings.Development.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Debug",
      "MassTransit": "Debug"
    }
  }
}
```

### Visual Studio Debugger

```csharp
// Set breakpoint and run with F5 (Debug)
// In Visual Studio:
// 1. Set breakpoint on line
// 2. Press F5 to start debugging
// 3. Execute request
// 4. Debugger pauses at breakpoint
// 5. Inspect variables, step through code

public async Task<Order> CreateOrderAsync(CreateOrderDto dto)
{
    // Debugger will stop here if breakpoint set
    var order = new Order
    {
        OrderId = Guid.NewGuid(),
        UserId = dto.UserId,
        Status = "Pending"
    };

    _dbContext.Orders.Add(order);
    await _dbContext.SaveChangesAsync();

    return order;
}
```

### View Service Logs

```bash
# Docker Compose
docker-compose logs -f order-service

# Filter errors
docker-compose logs order-service | grep -i error

# Last 100 lines
docker-compose logs --tail 100 order-service
```

### Check RabbitMQ Management UI

```
http://localhost:15672
Username: guest
Password: guest
```

**Verify:**
- Exchanges created
- Queues created
- Messages flowing through queues
- Dead letter queues for failures

### Redis CLI Debugging

```bash
# Connect to Redis
docker-compose exec redis-cache redis-cli

# Inside redis-cli:
PING                    # Test connection
KEYS *                  # List all keys
GET product:1           # Get cached product
TTL product:1           # Check expiration time
FLUSHDB                 # Clear cache
MONITOR                 # Watch all commands
```

### Database Query Debugging

```bash
# Connect to SQL Server
docker-compose exec mssql-db sqlcmd -S localhost -U sa -P YourPasswordHere123!

# Inside sqlcmd:
SELECT * FROM [Orders].[Orders]
SELECT * FROM [Catalog].[Products]
SELECT * FROM [Auth].[Users]
GO
```

---

## Testing Checklist

- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Saga happy path tested
- [ ] Saga compensation path tested
- [ ] Cache invalidation tested
- [ ] Health endpoints return correct status
- [ ] Error handling tested (DB down, RabbitMQ down, etc.)
- [ ] Load testing shows acceptable performance
- [ ] Security testing completed
- [ ] API documentation tested

---

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Run Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    services:
      mssql:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
          SA_PASSWORD: YourPasswordHere123!
          ACCEPT_EULA: Y
    steps:
      - uses: actions/checkout@v2
      - uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '8.0.x'
      - run: dotnet restore
      - run: dotnet build -c Release
      - run: dotnet test --verbosity normal
```

---

**Version:** 1.0  
**Last Updated:** 2026-07-02  
**Maintained by:** QA Team
