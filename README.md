# E-commerce Order & Payment Processing System

A self-contained, production-inspired **ASP.NET Core 9 Web API** demonstrating real-world payment processing patterns with **MongoDB**, **idempotent APIs**, **webhook signature verification**, and **asynchronous background workers**—all without external dependencies.

---

## 🎯 Overview

This project simulates a complete order-to-payment workflow found in production e-commerce systems. It implements critical backend patterns used by companies like Stripe, PayPal, and Shopify, providing a hands-on learning environment for understanding distributed payment architectures.

### Core Workflow
1. **Client creates order** → `POST /api/orders` (with idempotency key)
2. **Order persisted** → MongoDB with `Pending` status
3. **Background worker** → Simulates payment processing (5-second delay)
4. **Webhook callback** → Worker signs payload and POSTs to `/api/webhooks/payments`
5. **Signature verification** → HMAC SHA-256 validation with timestamp check
6. **Order updated** → Status changes to `Paid`
7. **Event stored** → Raw webhook payload persisted for audit trail

---

## 🏗️ Architecture & Design Patterns

### Implemented Patterns

#### 1. **Stripe-Style Idempotency**
- **Header-based**: Each request requires `Idempotency-Key` header
- **Response caching**: First response (status + body) stored in MongoDB
- **Exact replay**: Duplicate requests return identical cached response
- **Database constraints**: Unique index prevents concurrent duplicates
- **Race condition handling**: Graceful handling of simultaneous requests

```csharp
// First request with key "abc-123" → 201 Created + stores response
// Retry with same key → Returns cached 201 response (no duplicate order)
```

#### 2. **Producer-Consumer Pattern (Channel-based)**
- **Decoupled processing**: Order creation separate from payment simulation
- **Backpressure handling**: Unbounded channel for job queue
- **Type-safe jobs**: `PaymentSimulationJob` record for queue items
- **Async processing**: Non-blocking order submission

**Note**: Uses in-memory `Channel<T>` (jobs lost on restart). Production systems should use persistent outbox tables or message brokers like RabbitMQ/Azure Service Bus.

#### 3. **Webhook Signature Verification (HMAC-based)**
- **Shared secret**: HMAC SHA-256 with configurable secret
- **Timestamp inclusion**: `t={timestamp}.{body}` prevents replay attacks
- **Tolerance window**: Configurable time tolerance (default 300s)
- **Constant-time comparison**: `CryptographicOperations.FixedTimeEquals` prevents timing attacks
- **Detailed error reasons**: Signature mismatch, stale timestamp, malformed header

```
Signature Format: t={unix_timestamp},v1={hex_hmac_sha256}
Payload: t={timestamp}.{json_body}
```

#### 4. **Event Persistence & Audit Trail**
- **Raw event storage**: Complete webhook payloads stored in `payments` collection
- **Signature preservation**: Cryptographic proof of authenticity
- **Separation of concerns**: Event storage independent of order updates
- **Compliance ready**: Full audit trail for financial transactions

#### 5. **Background Worker (IHostedService)**
- **Long-running service**: Runs independently of HTTP requests
- **Graceful shutdown**: Cancellation token support
- **Dependency injection**: Full DI support in background services
- **Logging integration**: Structured logging for monitoring

---

## 🛠️ Technical Stack

| Component | Technology | Purpose |
|-----------|-----------|---------|
| **Framework** | ASP.NET Core 9 | Web API with controllers |
| **Database** | MongoDB 7.x | Document storage with indexes |
| **Async Queue** | System.Threading.Channels | In-memory job queue |
| **Cryptography** | HMAC SHA-256 | Webhook signature verification |
| **HTTP Client** | IHttpClientFactory | Managed HTTP connections |
| **Logging** | ILogger<T> | Structured logging |
| **API Docs** | Swagger/OpenAPI | Interactive API documentation |

---

## 📁 Project Structure

```
EcommerceLocal/
├── README.md                        # This file - complete documentation
├── QUICKSTART.md                    # 3-minute setup guide
├── PROJECT-STRUCTURE.md             # Detailed structure & workflow diagrams
├── ANALYSIS.md                      # Technical analysis & patterns
├── test-api.sh                      # Automated test suite
├── api-tests.http                   # HTTP test requests (VS Code)
└── Ecommerce.Api/
    ├── Controllers/                 # API endpoints
    ├── Services/                    # Business logic & workers
    ├── Data/                        # MongoDB access
    ├── Domain/                      # Entity models
    └── Program.cs                   # Application entry point
```

See [PROJECT-STRUCTURE.md](PROJECT-STRUCTURE.md) for detailed file-by-file breakdown.

---

## 🚀 Getting Started

### Prerequisites
- **.NET 9 SDK** ([Download](https://dotnet.microsoft.com/download))
- **MongoDB** (local or Docker)
- **curl** or **Postman** for testing

### MongoDB Setup (Docker)
```bash
docker run -d --name mongo -p 27017:27017 mongo:latest
```

### Run the Application
```bash
cd Ecommerce.Api
dotnet restore
dotnet build
dotnet run
```

Server starts on:
- **HTTP**: `http://localhost:5000`
- **HTTPS**: `https://localhost:5001`
- **Swagger**: `https://localhost:5001/swagger`

---

## 🧪 Testing the System

### 1. Create Order (First Time)
```bash
curl -X POST https://localhost:5001/api/Orders \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: unique-key-001" \
  -d '{
    "orderNumber": "ORD-12345",
    "amount": 99.99,
    "currency": "USD"
  }' \
  -k
```

**Expected**: `201 Created` with order details

### 2. Test Idempotency (Retry)
```bash
# Same request with same Idempotency-Key
curl -X POST https://localhost:5001/api/Orders \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: unique-key-001" \
  -d '{
    "orderNumber": "ORD-12345",
    "amount": 99.99,
    "currency": "USD"
  }' \
  -k
```

**Expected**: `201 Created` with **identical cached response** (no duplicate order created)

### 3. Missing Idempotency Key
```bash
curl -X POST https://localhost:5001/api/Orders \
  -H "Content-Type: application/json" \
  -d '{
    "orderNumber": "ORD-99999",
    "amount": 50.00,
    "currency": "USD"
  }' \
  -k
```

**Expected**: `400 Bad Request` with error message

### 4. Verify Payment Processing
Wait 5 seconds after order creation, then check MongoDB:

```bash
docker exec mongo mongosh ecommerce_local --eval 'db.orders.find().pretty()'
```

**Expected**: Order status changed from `0` (Pending) to `1` (Paid)

### 5. Check Webhook Events
```bash
docker exec mongo mongosh ecommerce_local --eval 'db.payments.find().pretty()'
```

**Expected**: PaymentEvent with raw body and HMAC signature

### 6. Automated Test Suite
Run the included test script:

```bash
chmod +x test-api.sh
./test-api.sh
```

---

## 🔐 Security Features

### 1. **HMAC Signature Verification**
- **Algorithm**: HMAC-SHA256
- **Payload**: `t={timestamp}.{json_body}`
- **Format**: `t={timestamp},v1={hex_signature}`
- **Secret**: Configurable in `appsettings.Development.json`

### 2. **Timing Attack Prevention**
```csharp
CryptographicOperations.FixedTimeEquals(expected, provided)
```
Prevents attackers from using response timing to guess signatures.

### 3. **Replay Attack Protection**
- Timestamps included in signature
- Configurable tolerance window (default 300 seconds)
- Rejects stale webhooks automatically

### 4. **Database-Level Idempotency**
- Unique index on `IdempotencyRecord.Key`
- Prevents duplicate processing even under concurrent load
- Graceful handling of race conditions

---

## ⚙️ Configuration

### `appsettings.Development.json`
```json
{
  "Mongo": {
    "ConnectionString": "mongodb://localhost:27017",
    "Database": "ecommerce_local"
  },
  "Webhook": {
    "SharedSecret": "super_secret_change_me",
    "SignatureHeader": "X-Signature",
    "ToleranceSeconds": 300
  },
  "Kestrel": {
    "Endpoints": {
      "Http": { "Url": "http://localhost:5000" },
      "Https": { "Url": "https://localhost:5001" }
    }
  }
}
```

---

## 📊 API Endpoints

### Orders

#### `POST /api/orders`
Create a new order (idempotent)

**Headers:**
- `Content-Type: application/json`
- `Idempotency-Key: {unique-key}` (required)

**Request Body:**
```json
{
  "orderNumber": "ORD-12345",
  "amount": 99.99,
  "currency": "USD"
}
```

**Response:** `201 Created`
```json
{
  "id": { "timestamp": 1762201059, "creationTime": "2025-11-03T20:17:39Z" },
  "orderNumber": "ORD-12345",
  "amount": 99.99,
  "currency": "USD",
  "status": 0,
  "createdAt": "2025-11-03T20:17:39.74123Z"
}
```

### Webhooks

#### `POST /api/webhooks/payments`
Receive payment confirmation (signature verified)

**Headers:**
- `Content-Type: application/json`
- `X-Signature: t={timestamp},v1={hmac_sha256}` (required)

**Request Body:**
```json
{
  "type": "payment.succeeded",
  "data": {
    "orderNumber": "ORD-12345",
    "amount": 99.99,
    "currency": "USD"
  }
}
```

**Response:** `200 OK` or `401 Unauthorized`

---

## 🎓 Learning Outcomes

By studying this project, you'll understand:

1. ✅ **Idempotent API Design** - Critical for payment systems and distributed architectures
2. ✅ **Webhook Security** - HMAC verification, timing attack prevention, replay protection
3. ✅ **Async Background Processing** - Decoupling HTTP requests from long-running tasks
4. ✅ **MongoDB Integration** - Document modeling, unique indexes, concurrent writes
5. ✅ **Dependency Injection** - Service lifetime management, testability patterns
6. ✅ **Event Persistence** - Audit trails, compliance, event sourcing concepts
7. ✅ **Cryptographic Best Practices** - Constant-time comparisons, secure signatures
8. ✅ **RESTful API Design** - Proper HTTP status codes, header usage, routing
9. ✅ **Concurrent Request Handling** - Race conditions, database constraints
10. ✅ **Configuration Management** - Environment-specific settings, secrets handling

---

## 🚧 Limitations & Production Considerations

### Current Limitations
- ⚠️ **In-memory job queue**: Jobs lost on application restart
- ⚠️ **No retry policies**: Failed webhook POSTs not retried
- ⚠️ **No health checks**: Missing `/health` endpoint for monitoring
- ⚠️ **No query endpoints**: Can't retrieve order status via API
- ⚠️ **No observability**: Missing metrics, distributed tracing
- ⚠️ **No graceful shutdown**: Worker may lose in-flight jobs

### Production Enhancements

#### 1. **Persistent Outbox Pattern**
Replace in-memory Channel with MongoDB-backed outbox:
```csharp
// Store jobs in database
await _db.Outbox.InsertOneAsync(new OutboxJob { ... });

// Worker polls database instead of Channel
var jobs = await _db.Outbox.Find(j => j.Status == "Pending").ToListAsync();
```

#### 2. **Retry Policies (Polly)**
```csharp
builder.Services.AddHttpClient("webhook")
    .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(
        3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
    ));
```

#### 3. **Health Checks**
```csharp
builder.Services.AddHealthChecks()
    .AddMongoDb(mongoConnectionString)
    .AddCheck<WorkerHealthCheck>("worker");

app.MapHealthChecks("/health");
```

#### 4. **Structured Logging (Serilog)**
```csharp
builder.Host.UseSerilog((ctx, config) => config
    .WriteTo.Console(new JsonFormatter())
    .Enrich.WithCorrelationId());
```

#### 5. **Order Query Endpoints**
```csharp
[HttpGet("{orderNumber}")]
public async Task<IActionResult> GetOrder(string orderNumber) { ... }
```

#### 6. **Webhook Idempotency**
Store webhook event IDs to prevent duplicate processing:
```csharp
var eventId = doc.RootElement.GetProperty("id").GetString();
if (await _db.ProcessedEvents.Find(e => e.EventId == eventId).AnyAsync())
    return Ok(new { ok = true, message = "Already processed" });
```

---

## 🐳 Docker Support

### Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Ecommerce.Api/Ecommerce.Api.csproj", "Ecommerce.Api/"]
RUN dotnet restore "Ecommerce.Api/Ecommerce.Api.csproj"
COPY . .
WORKDIR "/src/Ecommerce.Api"
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Ecommerce.Api.dll"]
```

### docker-compose.yml
```yaml
version: '3.8'
services:
  mongo:
    image: mongo:latest
    ports:
      - "27017:27017"
    volumes:
      - mongo-data:/data/db
  
  api:
    build: .
    ports:
      - "5000:8080"
    environment:
      - Mongo__ConnectionString=mongodb://mongo:27017
      - Mongo__Database=ecommerce_local
    depends_on:
      - mongo

volumes:
  mongo-data:
```

---

## 🧰 Development Tools

### VS Code Extensions
- **C# Dev Kit** - IntelliSense, debugging, project management
- **MongoDB for VS Code** - Database exploration
- **REST Client** - Test `.http` files directly in editor
- **Docker** - Container management

### Debugging
1. Open folder in VS Code
2. Accept prompt to add build/debug assets
3. Set breakpoints in controllers or worker
4. Press **F5** to start debugging

---

## 📚 References & Inspiration

- [Stripe API Idempotency](https://stripe.com/docs/api/idempotent_requests)
- [Webhook Security Best Practices](https://webhooks.fyi/security/hmac)
- [Outbox Pattern](https://microservices.io/patterns/data/transactional-outbox.html)
- [ASP.NET Core Background Services](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services)
- [MongoDB C# Driver](https://www.mongodb.com/docs/drivers/csharp/current/)

---

## 🤝 Contributing

This is an educational project. Suggestions for improvements:
1. Add integration tests (xUnit + Testcontainers)
2. Implement persistent outbox pattern
3. Add Polly retry policies
4. Create health check endpoints
5. Add order query/list endpoints
6. Implement webhook event deduplication
7. Add correlation IDs for distributed tracing

---

## 📄 License

MIT License - Free to use for learning and commercial projects.

---

## 🎯 Summary

This project demonstrates **production-grade payment processing patterns** in a self-contained, easy-to-understand format. While simplified for educational purposes, it accurately models the core workflows used by real payment platforms. The patterns learned here—idempotency, webhook verification, async processing, and event persistence—are directly applicable to building scalable, reliable distributed systems.

**Perfect for:** Backend developers learning payment systems, distributed architectures, or ASP.NET Core best practices.

**Next Steps:** Run the tests, explore the code, modify the worker delay, try breaking the signature verification, and experiment with concurrent requests to see idempotency in action!
