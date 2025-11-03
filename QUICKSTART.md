# Quick Start Guide

Get the E-commerce Order & Payment Processing system running in 3 minutes.

## Prerequisites

- ✅ .NET 9 SDK installed
- ✅ Docker running (for MongoDB)

## 1. Start MongoDB

```bash
docker run -d --name mongo -p 27017:27017 mongo:latest
```

## 2. Run the API

```bash
cd Ecommerce.Api
dotnet run
```

Server starts at:
- **HTTPS**: https://localhost:5001
- **Swagger**: https://localhost:5001/swagger

## 3. Test the System

### Option A: Automated Tests
```bash
chmod +x test-api.sh
./test-api.sh
```

### Option B: Manual Test
```bash
# Create an order
curl -X POST https://localhost:5001/api/Orders \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: test-001" \
  -d '{
    "orderNumber": "ORD-12345",
    "amount": 99.99,
    "currency": "USD"
  }' \
  -k

# Wait 5 seconds, then check MongoDB
docker exec mongo mongosh ecommerce_local --eval 'db.orders.find().pretty()'
```

## 4. Explore

- **Swagger UI**: https://localhost:5001/swagger
- **Test Requests**: Open `api-tests.http` in VS Code with REST Client extension
- **MongoDB Data**: 
  ```bash
  docker exec -it mongo mongosh ecommerce_local
  ```

## What Happens?

1. Order created with `Pending` status
2. Background worker simulates payment (5s delay)
3. Worker sends signed webhook to `/api/webhooks/payments`
4. Webhook verified and order marked as `Paid`
5. All events stored in MongoDB

## Next Steps

- Read the full [README.md](README.md) for architecture details
- Review [ANALYSIS.md](ANALYSIS.md) for technical deep-dive
- Experiment with concurrent requests to see idempotency in action
- Try sending invalid signatures to test webhook security

## Troubleshooting

**Port 5000/5001 already in use?**
```bash
lsof -ti:5000 | xargs kill -9
lsof -ti:5001 | xargs kill -9
```

**MongoDB connection failed?**
```bash
docker ps  # Check if mongo container is running
docker start mongo  # Start if stopped
```

**Build errors?**
```bash
dotnet clean
dotnet restore
dotnet build
```
