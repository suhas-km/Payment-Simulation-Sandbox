# Project Structure

```
EcommerceLocal/
├── README.md                          # Complete documentation
├── QUICKSTART.md                      # 3-minute setup guide
├── ANALYSIS.md                        # Technical deep-dive & pattern analysis
├── PROJECT-STRUCTURE.md               # This file
├── .gitignore                         # Git ignore rules
├── EcommerceLocal.sln                 # Solution file
├── test-api.sh                        # Automated test suite
├── api-tests.http                     # HTTP test requests (VS Code REST Client)
│
└── Ecommerce.Api/                     # Main API project
    ├── Program.cs                     # Application entry point & DI setup
    ├── Ecommerce.Api.csproj           # Project file
    ├── appsettings.json               # Base configuration
    ├── appsettings.Development.json   # Development settings (MongoDB, Webhook)
    │
    ├── Controllers/                   # API endpoints
    │   ├── OrdersController.cs        # POST /api/orders (idempotent)
    │   └── WebhooksController.cs      # POST /api/webhooks/payments (verified)
    │
    ├── Services/                      # Business logic & background workers
    │   ├── OrderService.cs            # Order CRUD operations
    │   ├── IdempotencyService.cs      # Idempotency key management
    │   ├── WebhookSigner.cs           # HMAC signature compute/verify
    │   ├── OutboxChannel.cs           # In-memory job queue (Channel<T>)
    │   └── PaymentSimulationWorker.cs # Background payment processor
    │
    ├── Data/                          # Database access layer
    │   ├── MongoContext.cs            # MongoDB client + index creation
    │   └── Collections.cs             # Typed collection accessors
    │
    ├── Domain/                        # Entity models
    │   ├── Order.cs                   # Order entity (Pending → Paid → Failed)
    │   ├── PaymentEvent.cs            # Webhook event storage (audit trail)
    │   └── IdempotencyRecord.cs       # Cached API responses
    │
    └── Properties/
        └── launchSettings.json        # Development launch profiles
```

## Key Files Explained

### Root Level

| File | Purpose |
|------|---------|
| `README.md` | Complete documentation with architecture, patterns, setup, API reference |
| `QUICKSTART.md` | Fast 3-minute setup guide for first-time users |
| `ANALYSIS.md` | Technical analysis of patterns, what's implemented, limitations |
| `test-api.sh` | Bash script to run all API tests automatically |
| `api-tests.http` | HTTP requests for VS Code REST Client extension |
| `.gitignore` | Excludes bin/, obj/, .vs/, etc. from version control |

### Controllers (API Layer)

| File | Endpoint | Purpose |
|------|----------|---------|
| `OrdersController.cs` | `POST /api/orders` | Create orders with idempotency |
| `WebhooksController.cs` | `POST /api/webhooks/payments` | Receive signed payment webhooks |

### Services (Business Logic)

| File | Responsibility |
|------|----------------|
| `OrderService.cs` | Order creation and status updates |
| `IdempotencyService.cs` | Check/save idempotency keys and cached responses |
| `WebhookSigner.cs` | HMAC SHA-256 signature generation and verification |
| `OutboxChannel.cs` | In-memory job queue using `Channel<T>` |
| `PaymentSimulationWorker.cs` | Background service that processes payments |

### Data (Persistence)

| File | Responsibility |
|------|----------------|
| `MongoContext.cs` | MongoDB connection, database access, index creation |
| `Collections.cs` | Typed accessors for Orders, Payments, Idempotency collections |

### Domain (Models)

| File | Entity | Collections |
|------|--------|-------------|
| `Order.cs` | Order with status lifecycle | `orders` |
| `PaymentEvent.cs` | Webhook event audit trail | `payments` |
| `IdempotencyRecord.cs` | Cached API responses | `idempotency` |

## MongoDB Collections

### `orders`
```javascript
{
  _id: ObjectId,
  OrderNumber: String,
  Amount: Decimal128,
  Currency: String,
  Status: Int32,  // 0=Pending, 1=Paid, 2=Failed
  CreatedAt: ISODate
}
```

### `payments`
```javascript
{
  _id: ObjectId,
  OrderNumber: String,
  Type: String,  // "payment.succeeded"
  Timestamp: ISODate,
  RawBody: String,  // Original JSON payload
  Signature: String  // HMAC signature
}
```

### `idempotency`
```javascript
{
  _id: ObjectId,
  key: String,  // Unique index
  StatusCode: Int32,
  ResponseBody: String,
  CreatedAt: ISODate
}
```

## Configuration Files

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

## Workflow Diagram

```
┌─────────────┐
│   Client    │
└──────┬──────┘
       │ POST /api/orders
       │ Idempotency-Key: abc-123
       ▼
┌─────────────────────────────────┐
│   OrdersController              │
│  1. Check idempotency key       │
│  2. Create order (Pending)      │
│  3. Enqueue job to Channel      │
│  4. Save response cache         │
│  5. Return 201 Created          │
└──────┬──────────────────────────┘
       │
       ▼
┌─────────────────────────────────┐
│   MongoDB: orders collection    │
│   Status: Pending (0)           │
└─────────────────────────────────┘
       │
       ▼
┌─────────────────────────────────┐
│   OutboxChannel (in-memory)     │
│   PaymentSimulationJob queued   │
└──────┬──────────────────────────┘
       │
       ▼
┌─────────────────────────────────┐
│   PaymentSimulationWorker       │
│  1. Read job from Channel       │
│  2. Wait 5 seconds (simulate)   │
│  3. Generate HMAC signature     │
│  4. POST to /api/webhooks/...   │
└──────┬──────────────────────────┘
       │
       ▼
┌─────────────────────────────────┐
│   WebhooksController            │
│  1. Verify HMAC signature       │
│  2. Check timestamp tolerance   │
│  3. Store PaymentEvent          │
│  4. Update order → Paid         │
│  5. Return 200 OK               │
└──────┬──────────────────────────┘
       │
       ▼
┌─────────────────────────────────┐
│   MongoDB: orders collection    │
│   Status: Paid (1)              │
└─────────────────────────────────┘
```

## Dependencies

From `Ecommerce.Api.csproj`:
- **MongoDB.Driver** (3.5.0) - MongoDB C# driver
- **Microsoft.Extensions.Http** (9.0.10) - HTTP client factory
- **Microsoft.AspNetCore.OpenApi** (9.0.3) - OpenAPI/Swagger support
- **Swashbuckle.AspNetCore** (6.5.0) - Swagger UI

## Development Workflow

1. **Start MongoDB**: `docker run -d --name mongo -p 27017:27017 mongo:latest`
2. **Run API**: `cd Ecommerce.Api && dotnet run`
3. **Test**: `./test-api.sh` or use `api-tests.http` in VS Code
4. **Debug**: Open in VS Code, press F5
5. **Explore**: https://localhost:5001/swagger

## Clean Architecture Layers

```
┌─────────────────────────────────────┐
│  Presentation (Controllers)         │  ← HTTP endpoints
├─────────────────────────────────────┤
│  Business Logic (Services)          │  ← Core logic
├─────────────────────────────────────┤
│  Data Access (Data + Collections)   │  ← MongoDB
├─────────────────────────────────────┤
│  Domain Models (Entities)           │  ← Pure data
└─────────────────────────────────────┘
```

## Testing Strategy

- **Unit Tests**: (Not included - add xUnit project)
- **Integration Tests**: `test-api.sh` - End-to-end API testing
- **Manual Tests**: `api-tests.http` - Interactive HTTP requests
- **Swagger UI**: Interactive API exploration

## Next Steps

- Add xUnit test project for unit/integration tests
- Implement persistent outbox pattern (MongoDB-backed queue)
- Add health check endpoints
- Implement retry policies with Polly
- Add order query endpoints (GET /api/orders/{id})
- Add correlation IDs for distributed tracing
