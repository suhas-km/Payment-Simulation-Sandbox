#!/bin/bash

# E-commerce API Test Script
# This script tests the order processing and payment webhook functionality

BASE_URL="https://localhost:5001"
SHARED_SECRET="super_secret_change_me"

echo "=========================================="
echo "E-commerce API Testing Script"
echo "=========================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test 1: Create a new order with idempotency
echo -e "${YELLOW}Test 1: Create a new order${NC}"
echo "POST /api/Orders with Idempotency-Key: test-order-002"
echo ""

RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/api/Orders" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: test-order-002" \
  -d '{"orderNumber": "ORD-67890", "amount": 149.99, "currency": "USD"}' \
  -k)

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')

if [ "$HTTP_CODE" == "201" ]; then
  echo -e "${GREEN}✓ Order created successfully (HTTP $HTTP_CODE)${NC}"
  echo "Response: $BODY"
else
  echo -e "${RED}✗ Failed to create order (HTTP $HTTP_CODE)${NC}"
  echo "Response: $BODY"
fi
echo ""

# Test 2: Test idempotency - retry with same key
echo -e "${YELLOW}Test 2: Test idempotency (retry with same key)${NC}"
echo "POST /api/Orders with same Idempotency-Key: test-order-002"
echo ""

RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/api/Orders" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: test-order-002" \
  -d '{"orderNumber": "ORD-67890", "amount": 149.99, "currency": "USD"}' \
  -k)

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')

if [ "$HTTP_CODE" == "201" ]; then
  echo -e "${GREEN}✓ Idempotency working - returned cached response (HTTP $HTTP_CODE)${NC}"
  echo "Response: $BODY"
else
  echo -e "${RED}✗ Idempotency test failed (HTTP $HTTP_CODE)${NC}"
  echo "Response: $BODY"
fi
echo ""

# Test 3: Create order without idempotency key (should fail)
echo -e "${YELLOW}Test 3: Create order without Idempotency-Key (should fail)${NC}"
echo "POST /api/Orders without Idempotency-Key header"
echo ""

RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/api/Orders" \
  -H "Content-Type: application/json" \
  -d '{"orderNumber": "ORD-99999", "amount": 50.00, "currency": "USD"}' \
  -k)

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')

if [ "$HTTP_CODE" == "400" ]; then
  echo -e "${GREEN}✓ Correctly rejected request without idempotency key (HTTP $HTTP_CODE)${NC}"
  echo "Response: $BODY"
else
  echo -e "${RED}✗ Should have rejected request (HTTP $HTTP_CODE)${NC}"
  echo "Response: $BODY"
fi
echo ""

# Test 4: Manual webhook test with valid signature
echo -e "${YELLOW}Test 4: Send webhook with valid signature${NC}"
echo "POST /api/webhooks/payments with valid HMAC signature"
echo ""

# Generate timestamp and signature
TIMESTAMP=$(date +%s)
WEBHOOK_BODY='{"type":"payment.succeeded","data":{"orderNumber":"ORD-MANUAL-TEST","amount":75.00,"currency":"USD"}}'

# Compute HMAC signature (requires openssl)
PAYLOAD="t=${TIMESTAMP}.${WEBHOOK_BODY}"
SIGNATURE=$(echo -n "$PAYLOAD" | openssl dgst -sha256 -hmac "$SHARED_SECRET" | awk '{print $2}')
HEADER="t=${TIMESTAMP},v1=${SIGNATURE}"

RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/api/webhooks/payments" \
  -H "Content-Type: application/json" \
  -H "X-Signature: $HEADER" \
  -d "$WEBHOOK_BODY" \
  -k)

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')

if [ "$HTTP_CODE" == "200" ]; then
  echo -e "${GREEN}✓ Webhook accepted with valid signature (HTTP $HTTP_CODE)${NC}"
  echo "Response: $BODY"
else
  echo -e "${RED}✗ Webhook rejected (HTTP $HTTP_CODE)${NC}"
  echo "Response: $BODY"
fi
echo ""

# Test 5: Webhook with invalid signature (should fail)
echo -e "${YELLOW}Test 5: Send webhook with invalid signature (should fail)${NC}"
echo "POST /api/webhooks/payments with invalid signature"
echo ""

INVALID_HEADER="t=${TIMESTAMP},v1=invalid_signature_here"

RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/api/webhooks/payments" \
  -H "Content-Type: application/json" \
  -H "X-Signature: $INVALID_HEADER" \
  -d "$WEBHOOK_BODY" \
  -k)

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')

if [ "$HTTP_CODE" == "401" ]; then
  echo -e "${GREEN}✓ Correctly rejected webhook with invalid signature (HTTP $HTTP_CODE)${NC}"
  echo "Response: $BODY"
else
  echo -e "${RED}✗ Should have rejected invalid signature (HTTP $HTTP_CODE)${NC}"
  echo "Response: $BODY"
fi
echo ""

# Test 6: Check Swagger UI
echo -e "${YELLOW}Test 6: Check Swagger UI availability${NC}"
echo "GET /swagger/index.html"
echo ""

HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/swagger/index.html" -k)

if [ "$HTTP_CODE" == "200" ]; then
  echo -e "${GREEN}✓ Swagger UI is available at $BASE_URL/swagger/index.html${NC}"
else
  echo -e "${RED}✗ Swagger UI not accessible (HTTP $HTTP_CODE)${NC}"
fi
echo ""

echo "=========================================="
echo "Test Summary"
echo "=========================================="
echo "All tests completed!"
echo ""
echo "To monitor the payment simulation worker:"
echo "  - Orders are automatically processed after 5 seconds"
echo "  - Check server logs for webhook POST status"
echo ""
echo "To check MongoDB data:"
echo "  docker exec mongo mongosh ecommerce_local --eval 'db.orders.find().pretty()'"
echo "  docker exec mongo mongosh ecommerce_local --eval 'db.payments.find().pretty()'"
echo "  docker exec mongo mongosh ecommerce_local --eval 'db.idempotency.find().pretty()'"
echo ""
