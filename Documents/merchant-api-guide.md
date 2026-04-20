# Promise Payments — Merchant Integration Guide
**Version:** 1.0  
**Date:** 2025-07-14

---

## Overview

The Promise Payment API lets you accept **YouCent Promise (YCP)** payments from Promise users directly in your application. You can create one-time checkout sessions or recurring subscriptions, and issue full refunds.

**1 YCP = 1 cent in the system (AmountCents).** Think of it as: $1.00 = 100 YCP = `amountCents: 100`.

**Base URL:** `https://promiseapi.azurewebsites.net`

---

## Prerequisites

Before you integrate, you need:
- A **Promise user account** (your merchant account).
- An **ApiKey** and **ApiSecret** provided to you by the Promise team.

> Keep your `ApiSecret` private and never expose it in client-side code. All requests using your credentials must come from your backend server.

---

## Authentication

Merchant API endpoints authenticate using your `ApiKey` and `ApiSecret` passed in the JSON request body. There are no headers to set for merchant auth — just include the `auth` object.

```json
{
  "auth": {
    "apiKey": "your_api_key_64_chars",
    "apiSecret": "your_api_secret_64_chars"
  },
  ...other fields...
}
```

---

## Step 1 — Create a Payment Request

Call this from your backend to generate a payment session. You get back a **token** and a **payment URL** to redirect or link your customer to.

### One-Time Payment

```
POST /merchant/payment-request
Content-Type: application/json
```

```json
{
  "auth": { "apiKey": "...", "apiSecret": "..." },
  "amountCents": 2500,
  "description": "Order #1042 — Premium Plan",
  "typeId": 1,
  "callbackUrl": "https://yoursite.com/payment-webhook",
  "expiresDate": "2025-07-21T23:59:59Z"
}
```

| Field | Type | Required | Notes |
|---|---|---|---|
| `amountCents` | integer | ✅ | Amount in YCP cents. 100 = 1 USD equivalent. |
| `description` | string | ❌ | Shown to the buyer on the payment page. |
| `typeId` | integer | ✅ | `1` = one-time payment |
| `callbackUrl` | string (URL) | ❌ | Your webhook URL (stored for future use). |
| `expiresDate` | datetime (UTC) | ❌ | If set, the payment link expires at this time. |

### Subscription Payment

Same endpoint, with `typeId: 2` and additional fields:

```json
{
  "auth": { "apiKey": "...", "apiSecret": "..." },
  "amountCents": 999,
  "description": "Monthly Newsletter Subscription",
  "typeId": 2,
  "intervalDays": 30,
  "subscriptionExpiresDate": "2026-07-14T00:00:00Z",
  "callbackUrl": "https://yoursite.com/payment-webhook"
}
```

| Field | Type | Required | Notes |
|---|---|---|---|
| `typeId` | integer | ✅ | `2` = subscription |
| `intervalDays` | integer | ✅ | Days between each recurring charge. Minimum 1. |
| `subscriptionExpiresDate` | datetime (UTC) | ❌ | Hard end date for the subscription. Leave null for indefinite. |

### Response

```json
{
  "success": true,
  "id": 42,
  "token": "a1b2c3...64chars",
  "paymentUrl": "/pay/a1b2c3...64chars",
  "amountCents": 2500,
  "description": "Order #1042 — Premium Plan",
  "typeId": 1,
  "statusId": 1,
  "createdDate": "2025-07-14T10:00:00Z",
  "expiresDate": "2025-07-21T23:59:59Z",
  "intervalDays": null,
  "subscriptionExpiresDate": null
}
```

Build the full payment link for your customer:

```
https://promiseapi.azurewebsites.net/pay/<token>
```

Or, to open the Promise native app directly on mobile (if installed):

```
https://promiseapi.azurewebsites.net/pay/<token>
```

The same URL handles both web and deep link — Android and iOS Promise apps are registered to intercept it.

---

## Step 2 — Customer Pays

You don't need to do anything for this step. After you redirect the customer to the payment URL:

1. The Promise platform shows them the payment details (your merchant name, amount, description).
2. They sign in to their Promise account if not already signed in.
3. They confirm the payment with their password.
4. For subscriptions, a `MerchantSubscription` is created and the first interval is charged immediately.

**Payment request statuses:**

| StatusId | Meaning |
|---|---|
| 1 | Pending — waiting for payment |
| 2 | Completed — paid |
| 3 | Expired — past `ExpiresDate` without payment |
| 4 | Cancelled — cancelled by the system |

---

## Step 3 — Check Payment Status (optional polling)

You can check whether a payment request has been completed:

```
GET /pay/{token}
```

No authentication required. Response includes `statusId` and `success: true/false`.

```json
{
  "success": true,
  "merchantName": "Acme Store",
  "amountCents": 2500,
  "description": "Order #1042 — Premium Plan",
  "typeId": 1,
  "statusId": 2,
  "intervalDays": null
}
```

When `statusId` is `2` (Completed), the payment went through.

---

## Step 4 — Charge Subscriptions (recurring billing)

> For subscriptions only.

Your backend must call this endpoint on a schedule (e.g., daily cron job) for each active subscription that is due for its next charge.

```
POST /merchant/subscription/charge
Content-Type: application/json
```

```json
{
  "auth": { "apiKey": "...", "apiSecret": "..." },
  "subscriptionId": 5
}
```

**Response (success):**
```json
{
  "success": true,
  "subscriptionId": 5,
  "promiseTransactionId": 102
}
```

**What to handle:**
- `400` with `"Next charge date has not arrived yet"` — your scheduler is running ahead of time; skip this one.
- `400` with `"Subscriber has insufficient balance"` — user doesn't have enough funds. Retry later or notify the user.
- `400` with `"Subscription has expired"` — the subscription's `subscriptionExpiresDate` was reached; stop scheduling it.
- `400` with `"Subscription is not active"` — subscriber cancelled; stop scheduling it.

**Recommended scheduler approach:**
1. Call `POST /merchant/subscriptions` (see below) to get all your subscriptions.
2. Filter for `statusId = 1` (Active) and `nextChargeDate <= now (UTC)`.
3. For each, call `POST /merchant/subscription/charge`.

---

## Step 5 — Issue a Refund

To refund a charge, you need the `id` from your `MerchantTransactions` records (the merchant transaction ID, not the promise transaction ID).

```
POST /merchant/refund
Content-Type: application/json
```

```json
{
  "auth": { "apiKey": "...", "apiSecret": "..." },
  "merchantTransactionId": 15
}
```

**Response:**
```json
{
  "success": true,
  "subscriptionId": null,
  "promiseTransactionId": 110
}
```

**Notes:**
- Full refund only (v1). The original charge amount is returned to the buyer.
- Each charge can be refunded once.
- You (the merchant) must have sufficient balance to issue the refund.
- Refunding a subscription charge does not automatically cancel the subscription — cancel separately if needed.

---

## Query Endpoints

Use these to retrieve your data for dashboards, reconciliation, or scheduling decisions.

### List Payment Requests (last 100)
```
POST /merchant/payment-requests
{ "apiKey": "...", "apiSecret": "..." }
```

### List Subscriptions (last 100)
```
POST /merchant/subscriptions
{ "apiKey": "...", "apiSecret": "..." }
```

**Subscription response item fields:**

| Field | Notes |
|---|---|
| `id` | Use this as `subscriptionId` for charging |
| `statusId` | 1 = Active, 2 = Cancelled, 3 = Expired |
| `amountCents` | Per-interval charge amount |
| `intervalDays` | Days between charges |
| `nextChargeDate` | UTC datetime — charge when this is ≤ now |
| `expiresDate` | Null or the subscription end date |
| `consentDate` | When the buyer accepted the subscription |

### List Transactions (last 100)
```
POST /merchant/transactions
{ "apiKey": "...", "apiSecret": "..." }
```

**Transaction TypeId:** `1` = Charge, `2` = Refund.

---

## Error Responses

All error responses have the same shape:

```json
{ "success": false, "error": "Descriptive error message." }
```

| HTTP Status | Meaning |
|---|---|
| `400` | Bad request — check the `error` field for details |
| `401` | Invalid or inactive merchant credentials |
| `404` | Resource not found (payment request, subscription, or transaction) |
| `500` | Server error — contact Promise support |

---

## Integration Checklist

- [ ] Store `ApiKey` and `ApiSecret` securely in environment variables / secrets manager.
- [ ] Create payment requests from your backend only — never from the browser/client.
- [ ] Build the customer-facing payment link as `https://promiseapi.azurewebsites.net/pay/<token>`.
- [ ] For subscriptions: implement a daily scheduled job that calls `/merchant/subscription/charge` for due subscriptions.
- [ ] Handle `"Subscriber has insufficient balance"` gracefully — notify the user or retry.
- [ ] Store the `promiseTransactionId` and your `MerchantTransaction id` returned from charge/payment calls — you'll need the transaction ID to issue refunds.
- [ ] Test the full flow in development using `http://localhost:7800` before going live.
