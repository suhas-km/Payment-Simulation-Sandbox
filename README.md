# E-commerce Order & Payment Processing System

ASP.NET Core 9 Web API demonstrating payment mimicking patterns: **idempotent APIs**, **webhook signature verification**, **async background workers**, and **MongoDB** integration.

## 🎯 Overview

Simulates order-to-payment workflow with patterns used by Stripe, PayPal, and Shopify:

1. Client creates order → `POST /api/orders` (idempotency key required)
2. Order saved to MongoDB (`Pending` status)
3. Background worker simulates payment (5s delay)
4. Worker POSTs signed webhook → `/api/webhooks/payments`
5. Signature verified (HMAC SHA-256)
6. Order status → `Paid`, event stored for audit

## 🏗️ Key Patterns

**Idempotency (Stripe-style)**
- `Idempotency-Key` header required
- Response cached in MongoDB
- Duplicate requests return cached response
- Unique index prevents concurrent duplicates

**Webhook Security**
- HMAC SHA-256 signatures
- Timestamp-based replay protection
- Constant-time comparison (timing attack prevention)

**Async Processing**
- In-memory `Channel<T>` job queue (⚠️ not persistent)
- Background worker (`IHostedService`)
- Event persistence for audit trail

## 📁 Project Structure

```
Ecommerce.Api/
├── Controllers/          # OrdersController, WebhooksController
├── Services/             # OrderService, IdempotencyService, WebhookSigner, PaymentSimulationWorker
├── Data/                 # MongoContext, Collections
└── Domain/               # Order, PaymentEvent, IdempotencyRecord
```

## 🚀 Quick Start

**1. Start MongoDB**
```bash
docker run -d --name mongo -p 27017:27017 mongo:latest
```

**2. Run API**
```bash
cd Ecommerce.Api && dotnet run
```
Server: `https://localhost:5001` | Swagger: `https://localhost:5001/swagger`

**3. Test**
```bash
# Create order
curl -X POST https://localhost:5001/api/Orders \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: test-001" \
  -d '{"orderNumber": "ORD-001", "amount": 99.99, "currency": "USD"}' -k

# Run automated tests
chmod +x test-api.sh && ./test-api.sh

# Check MongoDB (wait 5s for payment processing)
docker exec mongo mongosh ecommerce_local --eval 'db.orders.find().pretty()'
```

## 📊 API Endpoints

**`POST /api/orders`** - Create order (requires `Idempotency-Key` header)
```json
{"orderNumber": "ORD-001", "amount": 99.99, "currency": "USD"}
```

**`POST /api/webhooks/payments`** - Webhook callback (requires `X-Signature` header)

## ⚙️ Configuration

Edit `appsettings.Development.json`:
```json
{
  "Mongo": {
    "ConnectionString": "mongodb://localhost:27017",
    "Database": "ecommerce_local"
  },
  "Webhook": {
    "SharedSecret": "super_secret_change_me",
    "ToleranceSeconds": 300
  }
}
```

## 🚧 Limitations

- ⚠️ In-memory job queue (jobs lost on restart)
- ⚠️ No retry policies or health checks
- ⚠️ No query endpoints for order status

**Production needs:** Persistent outbox (MongoDB-backed queue), Polly retry policies, health checks, observability.

## 📚 References

- [Stripe API Idempotency](https://stripe.com/docs/api/idempotent_requests)
- [Webhook Security](https://webhooks.fyi/security/hmac)
- [Outbox Pattern](https://microservices.io/patterns/data/transactional-outbox.html)

## 📄 License

MIT License
