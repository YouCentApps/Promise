# Promise Merchant & Payment System — Internal Reference
**Version:** 1.1  
**Date:** 2026-04-20  
**Audience:** Developers, Support, Internal Operations

---

## 1. Overview

The Merchant & Payment System extends the Promise platform to allow any registered Promise user to become a **merchant**. Merchants can:

- Create **one-time payment requests** (checkout sessions) that a buyer pays once.
- Create **subscription payment requests** that, once accepted by a buyer, can be charged repeatedly on a defined interval.
- **Refund** any charge transaction they have issued.
- Query their own payment requests, subscriptions, and transaction history.

Buyers (regular Promise users) can:

- View payment request details via a link/token (no login required to view).
- Pay a payment request (login required) — either as a one-time payment or by subscribing.
- View and cancel their own active subscriptions.

All money movement happens through the existing **PromiseTransactions** and **Balances** tables. Merchant records layer on top without changing core transfer logic.

---

## 2. Database Schema — New Tables

All new tables are described in `db.sql`. Below is a summary for quick reference.

### Lookup Tables (seed data, do not change at runtime)

| Table | Values |
|---|---|
| `MerchantPaymentRequestTypes` | 1 = OneTime, 2 = Subscription |
| `MerchantPaymentRequestStatuses` | 1 = Pending, 2 = Completed, 3 = Expired, 4 = Cancelled |
| `MerchantSubscriptionStatuses` | 1 = Active, 2 = Cancelled, 3 = Expired |
| `MerchantTransactionTypes` | 1 = Charge, 2 = Refund |

### `Merchants`
One row per merchant. `UserId` is a FK to `Users`. Contains `ApiKey` (plain, unique), `ApiSecretHash` + `Salt` (hashed like a password), `IsActive` flag.

### `MerchantPaymentRequests`
One row per checkout session created by a merchant. Key fields:
- `Token` — 64-char unique URL-safe string, used in payment links (`/pay/{token}`).
- `TypeId` — 1 = one-time, 2 = subscription.
- `StatusId` — starts at 1 (Pending), moves to 2 (Completed) once paid.
- `IntervalDays` — required for subscriptions; how many days between charges.
- `SubscriptionExpiresDate` — optional hard expiry for subscriptions.
- `ExpiresDate` — optional expiry for the payment request itself (before anyone pays).
- `CallbackUrl` — optional URL the merchant's system can poll/use after payment.

### `MerchantSubscriptions`
Created when a buyer accepts a subscription payment request. Key fields:
- `SubscriberId` — the buyer's `UserId`.
- `NextChargeDate` — updated after each successful charge.
- `StatusId` — Active (1) → Cancelled (2) by user, or Expired (3) by system/merchant.

### `MerchantTransactions`
Audit log. Every charge and refund gets one row. Always linked to a `PromiseTransaction` (which is the actual balance movement record).

---

## 3. How Merchant Authentication Works

Merchant API endpoints (create payment request, charge subscription, refund, queries) use **ApiKey + ApiSecret** authentication — separate from user JWT tokens.

- `ApiKey` is stored in plain text in `Merchants.ApiKey` and is used only to look up the record.
- `ApiSecret` is **never stored**. Only its hash (`ApiSecretHash`) is stored, computed as:  
  `SHA256(SHA256(apiSecret) + salt)`  
  (same algorithm as user passwords, via `Security.GetPasswordHash`).
- Both values are 64-character hex strings (two concatenated GUIDs, no dashes).
- Authentication helper: `MerchantAuth.Authenticate(db, auth)` — returns the `Merchant` entity or `null` if invalid/inactive.

**The ApiSecret is shown exactly once** — in the response to `POST /merchant/register`. It cannot be recovered; a new key pair would need to be generated if lost (not yet implemented — see Section 8).

---

## 4. How to Onboard a New Merchant (Current Manual Process)

There is no admin UI yet. There are two options:

### Option A — Via API (preferred)
The user must already have a Promise account. They (or support on their behalf) call `POST /merchant/register` while authenticated with their user JWT.

```
POST /merchant/register
Authorization: Bearer <user_jwt>
Content-Type: application/json

{
  "user": { "login": "their_username", "password": "their_password" },
  "merchantName": "Acme Store",
  "website": "https://acme.example.com"
}
```

The response contains `apiKey` and `apiSecret` — **save and deliver these to the merchant immediately**; the secret cannot be retrieved again.

### Option B — Directly in the Database (emergency/support only)
1. Confirm the user exists in `Users` and note their `Id`.
2. Generate a 64-char `ApiKey` (e.g., two `NEWID()` GUIDs concatenated, no dashes).
3. Generate a 64-char `ApiSecret` the same way — note it for delivery.
4. Compute salt and hash in a helper script or use the API approach instead.
5. Insert into `Merchants`:

```sql
DECLARE @userId     bigint      = <user_id>;
DECLARE @apiKey     nvarchar(64) = N'<generated_64_char_key>';
DECLARE @apiSecret  nvarchar(64) = N'<generated_64_char_secret>';
DECLARE @salt       nvarchar(32) = N'<random_base64_salt>';
-- ApiSecretHash must be computed by the same SHA256(SHA256(secret)+salt) algorithm.
-- Use Option A instead to avoid manual hashing.

INSERT INTO Merchants (UserId, Name, Website, ApiKey, ApiSecretHash, Salt, IsActive, CreatedDate)
VALUES (@userId, N'Merchant Name', N'https://...', @apiKey, N'<computed_hash>', @salt, 1, GETDATE());
```

> ⚠️ **Strongly prefer Option A.** Manual DB inserts risk hash mismatches that will cause all API calls to fail silently.

---

## 5. API Endpoints Reference

**Base URL (production):** `https://promiseapi.azurewebsites.net`  
**Base URL (dev):** `http://localhost:7800`

All request/response bodies are `application/json`. All amounts are in **YCP cents** (1 USD = 100 YCP = 10000 cents; 1 YCP = 1 cent in the system).

### Auth Types
- **User JWT** — `Authorization: Bearer <token>` header, obtained from `/signin`.
- **Merchant API Auth** — `ApiKey` + `ApiSecret` fields in the request body JSON.

---

### `POST /merchant/register`
Register an existing Promise user as a merchant.

**Auth:** User JWT  
**Request body:**
```json
{
  "user": { "login": "string", "password": "string" },
  "merchantName": "string",
  "website": "string | null"
}
```
**Success response `200`:**
```json
{
  "success": true,
  "userId": 123456,
  "name": "Acme Store",
  "website": "https://acme.example.com",
  "apiKey": "abc...64chars",
  "apiSecret": "xyz...64chars",
  "isActive": true
}
```
**Notes:** `apiSecret` is returned **only once**. `409` if user is already a merchant.

---

### `POST /merchant/payment-request`
Create a payment request (checkout session).

**Auth:** Merchant API Auth (in body)  
**Request body:**
```json
{
  "auth": { "apiKey": "...", "apiSecret": "..." },
  "amountCents": 1000,
  "description": "Order #42",
  "typeId": 1,
  "callbackUrl": "https://mysite.com/webhook",
  "expiresDate": "2025-08-01T00:00:00Z",
  "intervalDays": null,
  "subscriptionExpiresDate": null
}
```
`typeId`: 1 = OneTime, 2 = Subscription. For subscriptions, `intervalDays` is required (≥ 1).

**Success response `200`:**
```json
{
  "success": true,
  "id": 7,
  "token": "abc...64chars",
  "paymentUrl": "/pay/abc...64chars",
  "amountCents": 1000,
  "description": "Order #42",
  "typeId": 1,
  "statusId": 1,
  "createdDate": "2025-07-14T10:00:00Z",
  "expiresDate": null,
  "intervalDays": null,
  "subscriptionExpiresDate": null
}
```
**Notes:** Build the full payment URL as `<base_url>/pay/<token>`. Share this link with the buyer.

---

### `GET /pay/{token}`
Get public details of a payment request. No authentication required.

**Response `200`:**
```json
{
  "success": true,
  "merchantName": "Acme Store",
  "amountCents": 1000,
  "description": "Order #42",
  "typeId": 1,
  "statusId": 1,
  "intervalDays": null
}
```
Returns `success: false` with an error message if the request is not found, not pending, or expired. `statusId` is still returned even on failure so the UI can show an appropriate state.

---

### `POST /pay/{token}`
Execute a payment (one-time) or set up a subscription. The token in the URL is informational; the token in the body is the authoritative value used by the endpoint.

**Auth:** User JWT  
**Request body:**
```json
{
  "user": { "login": "string", "password": "string", "id": 123456 },
  "token": "abc...64chars"
}
```
**Success response `200`:**
```json
{
  "success": true,
  "subscriptionId": null,
  "promiseTransactionId": 99
}
```
For subscriptions, `subscriptionId` will be set. The `promiseTransactionId` is the ID in `PromiseTransactions` (the actual balance transfer record).

**Business rules enforced:**
- Buyer must have sufficient balance.
- Payment request must be `Pending` and not expired.
- Merchant cannot pay their own payment request.

---

### `POST /merchant/subscription/charge`
Charge the next interval on an active subscription. **The merchant must call this proactively** (cron/scheduler); the system does not auto-charge.

**Auth:** Merchant API Auth (in body)  
**Request body:**
```json
{
  "auth": { "apiKey": "...", "apiSecret": "..." },
  "subscriptionId": 5
}
```
**Success response `200`:**
```json
{
  "success": true,
  "subscriptionId": 5,
  "promiseTransactionId": 102
}
```
**Errors:**
- `400` — subscription not active, `NextChargeDate` has not arrived yet, subscription expired, or insufficient subscriber balance.
- When a subscription is found to be past its `ExpiresDate`, its status is automatically set to `Expired (3)` and the charge is rejected.

---

### `POST /merchant/refund`
Refund a previous charge transaction. Full refund only; partial refunds are not supported in v1.

**Auth:** Merchant API Auth (in body)  
**Request body:**
```json
{
  "auth": { "apiKey": "...", "apiSecret": "..." },
  "merchantTransactionId": 15
}
```
**Success response `200`:**
```json
{
  "success": true,
  "subscriptionId": null,
  "promiseTransactionId": 110
}
```
**Notes:**
- Only `Charge` (TypeId = 1) transactions can be refunded.
- Each charge can be refunded only once (duplicate check on `PaymentRequestId + PayerId + TypeId + AmountCents`).
- Merchant must have sufficient balance to issue the refund.

---

### `POST /merchant/payment-requests`
List the merchant's payment requests (most recent 100).

**Auth:** Merchant API Auth (in body)  
**Request body:** `{ "apiKey": "...", "apiSecret": "..." }`  
**Response:** `{ "success": true, "items": [ ...ApiResponsePaymentRequest... ] }`

---

### `POST /merchant/subscriptions`
List the merchant's subscriptions (most recent 100).

**Auth:** Merchant API Auth (in body)  
**Request body:** `{ "apiKey": "...", "apiSecret": "..." }`  
**Response:** `{ "success": true, "items": [ ...ApiResponseSubscription... ] }`

---

### `POST /merchant/transactions`
List the merchant's transactions (most recent 100).

**Auth:** Merchant API Auth (in body)  
**Request body:** `{ "apiKey": "...", "apiSecret": "..." }`  
**Response:** `{ "success": true, "items": [ ...ApiResponseMerchantTransaction... ] }`

---

### `POST /user/subscriptions`
List the authenticated user's subscriptions (as a subscriber).

**Auth:** User JWT  
**Request body:** `{ "login": "string", "password": "string" }`  
**Response:** `{ "success": true, "items": [ ...ApiResponseSubscription with merchantName... ] }`

---

### `DELETE /user/subscriptions/{subscriptionId}`
Cancel an active subscription (user-initiated).

**Auth:** User JWT  
**Request body:** `{ "login": "string", "password": "string" }`  
**Response:** `{ "success": true }`  
**Notes:** Sets `StatusId = 2 (Cancelled)` and records `CancelledDate`. Only the subscriber can cancel via this endpoint. The merchant can see the cancellation in their subscriptions list.

---

## 6. Client-Side Pages (Blazor + MAUI)

### `/pay/{token}` — Pay.razor (Promise.Comp)
Shared Blazor component used by both the web app and MAUI app.

**Flow:**
1. On load, calls `GET /pay/{token}` to fetch and display payment details (merchant name, amount, description, type).
2. If the user is not signed in, a "Sign In" button redirects to `/signin?returnUrl=/pay/{token}`.
3. If signed in, user enters their password and clicks "Confirm Payment", which calls `POST /pay/{token}`.
4. On success, a success message is shown along with the `subscriptionId` if a subscription was created.

### `/subscriptions` — Subscriptions.razor (Promise.Comp)
User-facing subscription manager.

**Flow:**
1. Calls `POST /user/subscriptions` to load all subscriptions.
2. Displays a table with merchant name, amount, interval, next charge date, and status.
3. Active subscriptions show a "Cancel" button that calls `DELETE /user/subscriptions/{id}`.

### Deep Links (Promise.Native — MAUI)
Payment links (`https://promiseapi.azurewebsites.net/pay/{token}`) will open the native app directly on Android and iOS:

- **Android:** Intent filter in `MainActivity.cs` with `AutoVerify = true`. Requires a Digital Asset Links file at `/.well-known/assetlinks.json` on the production domain to fully verify.
- **iOS:** Associated domains entitlement (`applinks:promiseapi.azurewebsites.net`) + `apple-app-site-association` file on the domain. URL scheme `youcent://` is also registered for custom URL scheme fallback.
- **App.xaml.cs:** `OnAppLinkRequestReceived(Uri uri)` parses the token from the path and stores it in `App.PendingPaymentToken`. The main Blazor page should check this property on load and navigate to `/pay/{token}` accordingly.

---

## 7. Key Business Workflows

### Workflow A: One-Time Payment
```
Merchant (backend) → POST /merchant/payment-request (TypeId=1)
                   ← token + paymentUrl

Merchant → shares paymentUrl with buyer (email, website redirect, QR, etc.)

Buyer → opens paymentUrl in browser/app
      → GET /pay/{token} — sees merchant name, amount, description
      → signs in if needed
      → POST /pay/{token} — confirms with password
      ← { success: true, promiseTransactionId: N }

Result:
  - Buyer balance decreases
  - Merchant balance increases
  - MerchantPaymentRequest.StatusId = Completed
  - PromiseTransaction created
  - MerchantTransaction (Charge) created
```

### Workflow B: Subscription Setup + Recurring Charges
```
Merchant (backend) → POST /merchant/payment-request (TypeId=2, intervalDays=30)
                   ← token + paymentUrl

Buyer → opens paymentUrl, signs in, confirms
      → POST /pay/{token}
      ← { success: true, subscriptionId: S, promiseTransactionId: N }

Result (first payment):
  - Balance moved (first interval charged immediately)
  - MerchantSubscription created (StatusId=Active, NextChargeDate = now + intervalDays)
  - MerchantPaymentRequest.StatusId = Completed

Merchant scheduler (every day or on a schedule) → POST /merchant/subscription/charge
  for each active subscription where NextChargeDate <= now:
  ← { success: true, promiseTransactionId: N }

  Result per charge:
    - Balance moved
    - NextChargeDate advanced by intervalDays
    - New MerchantTransaction (Charge) created

Buyer cancels → DELETE /user/subscriptions/{S}
  - MerchantSubscription.StatusId = Cancelled
  - No further charges possible
```

### Workflow C: Refund
```
Merchant (backend) → POST /merchant/refund (merchantTransactionId: T)
                   ← { success: true, promiseTransactionId: N }

Result:
  - Merchant balance decreases
  - Original payer balance increases
  - New PromiseTransaction (reverse) created
  - New MerchantTransaction (Refund) created
```

---

## 8. Known Limitations & Future Work (v1)

| Area | Current State | Future |
|---|---|---|
| Merchant self-signup | Manual via API or DB | Web UI / automated signup flow |
| API key rotation | Not implemented | Endpoint to regenerate key pair |
| Partial refunds | Not supported | Could be added with `refundAmountCents` field |
| Auto-charging subscriptions | Merchant must call charge endpoint manually | Server-side background scheduler / Azure Function |
| Callback/webhook delivery | `CallbackUrl` is stored but not actively called | Implement HTTP POST to `CallbackUrl` on payment events |
| Pagination | Query endpoints return max 100 records | Add `offset`/`limit` parameters |
| Admin UI for merchants | None | Future admin panel |
| Android App Links verification | Intent filter in place | Must host `/.well-known/assetlinks.json` on prod domain |
| iOS Universal Links | Entitlements in place | Must host `apple-app-site-association` on prod domain |

---

## 9. Error Response Format

All endpoints return a consistent JSON body on error:
```json
{ "success": false, "error": "Human-readable error message." }
```
HTTP status codes used: `400` Bad Request, `401` Unauthorized, `404` Not Found, `409` Conflict, `500` Server Error.

---

## 10. Useful Queries for Support

```sql
-- Find a merchant by username
SELECT m.*, u.Login
FROM Merchants m
JOIN Users u ON u.Id = m.UserId
WHERE u.Login = 'merchant_username';

-- List all active subscriptions for a merchant
SELECT ms.*, u.Login AS SubscriberLogin
FROM MerchantSubscriptions ms
JOIN Users u ON u.Id = ms.SubscriberId
WHERE ms.MerchantId = <merchant_user_id>
  AND ms.StatusId = 1  -- Active
ORDER BY ms.NextChargeDate;

-- Find all subscriptions due for charge today
SELECT ms.Id, ms.MerchantId, ms.SubscriberId, ms.AmountCents, ms.NextChargeDate
FROM MerchantSubscriptions ms
WHERE ms.StatusId = 1
  AND ms.NextChargeDate <= GETUTCDATE()
ORDER BY ms.NextChargeDate;

-- Transaction history for a merchant (charges and refunds)
SELECT mt.*, u.Login AS PayerLogin
FROM MerchantTransactions mt
JOIN Users u ON u.Id = mt.PayerId
WHERE mt.MerchantId = <merchant_user_id>
ORDER BY mt.Date DESC;

-- Check if a payment request has been paid
SELECT Id, Token, StatusId, CreatedDate, ExpiresDate
FROM MerchantPaymentRequests
WHERE Token = '<token>';
```
