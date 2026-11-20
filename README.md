# Tron Energy Rental via API
## .NET SDK by TronZap.com

**[English](README.md)** | [Español](README.es.md) | [Português](README.pt-br.md) | [Русский](README.ru.md)

[![NuGet](https://img.shields.io/nuget/v/Tronzap.Sdk.svg)](https://www.nuget.org/packages/Tronzap.Sdk)
[![CI](https://github.com/tron-energy-market/tronzap-sdk-csharp/actions/workflows/ci.yml/badge.svg)](https://github.com/tron-energy-market/tronzap-sdk-csharp/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Official .NET SDK for the TronZap API.
This SDK allows you to easily integrate with TronZap services for TRON energy rental.

TronZap.com allows you to [buy TRON energy](https://tronzap.com/), making USDT (TRC20) transfers cheaper by significantly reducing transaction fees.

👉 [Register for an API key](https://tronzap.com) to start using TronZap API and integrate it via the SDK.

- Website: https://tronzap.com/
- API reference: https://docs.tronzap.com/
- NuGet: https://www.nuget.org/packages/Tronzap.Sdk
- Source: https://github.com/tron-energy-market/tronzap-sdk-csharp

## Installation

```bash
dotnet add package Tronzap.Sdk
```

## Requirements

- .NET 8 or newer
- One dependency, `Microsoft.Extensions.Http`, for the `IHttpClientFactory` integration. The SDK does not depend on ASP.NET Core.

## Quick start

```csharp
using Tronzap.Sdk;
using Tronzap.Sdk.Exceptions;
using Tronzap.Sdk.Requests;
using Tronzap.Sdk.Responses;

var client = new TronzapClient(new TronzapClientOptions
{
    ApiToken = "your_api_token",
    ApiSecret = "your_api_secret",
});

try
{
    AccountBalance balance = await client.GetBalanceAsync(cancellationToken);
    Console.WriteLine($"balance: {balance.Balance} (deposit to {balance.Address})");

    // Estimate how much energy a USDT transfer needs, then buy exactly that much.
    EnergyEstimate estimate = await client.EstimateEnergyAsync(
        new EstimateEnergyRequest { FromAddress = "TSenderAddress", ToAddress = "TRecipientAddress" },
        cancellationToken);

    Transaction tx = await client.CreateEnergyTransactionAsync(
        new EnergyTransactionRequest
        {
            Address = "TRecipientAddress",
            Energy = estimate.Amount,
            Duration = 1,
            ExternalId = "order-42",
            ActivateAddress = true,
        },
        cancellationToken);
    Console.WriteLine($"transaction {tx.Id} costs {tx.Amount} and is {tx.Status}");
}
catch (TronzapException e)
{
    Console.Error.WriteLine($"TronZap call failed: {e.Message}");
}
```

A runnable walkthrough of every operation lives in
[`examples/BasicUsage.cs`](examples/BasicUsage.cs). It needs the .NET 10 SDK:

```bash
export TRONZAP_API_TOKEN=your_api_token
export TRONZAP_API_SECRET=your_api_secret
export TRONZAP_BASE_URL=api.tronzap.com   # optional
dotnet run examples/BasicUsage.cs
```

By default it only reads and spends nothing. Setting `TRONZAP_ALLOW_PURCHASES=1`
also exercises the endpoints that create transactions, AML checks and a one-address
bulk order, which debit the account balance. See the comment at the top of the file
for the other optional variables.

## Configuration

`TronzapClientOptions` takes the two credentials from your dashboard: the API
token is sent as a bearer token, and the API secret signs every request body.
Everything else is optional:

```csharp
var client = new TronzapClient(new TronzapClientOptions
{
    ApiToken = apiToken,
    ApiSecret = apiSecret,
    BaseUrl = "api.tronzap.com",          // defaults to TronzapClientOptions.DefaultBaseUrl
    Timeout = TimeSpan.FromSeconds(10),   // whole request; defaults to 30 seconds
    UserAgent = "my-app/1.0",
});
```

`BaseUrl` takes either a bare domain or a full URL: a missing scheme becomes
`https` and a trailing slash is trimmed, so `"api.tronzap.com"`,
`"api.tronzap.com/"` and `"https://api.tronzap.com"` are equivalent. Pass an
explicit scheme to opt out, for example `"http://localhost:8080"` against a local
mock.

A `TronzapClient` is immutable, safe for concurrent use and copies its options
when it is created, so create one per set of credentials and share it. `Timeout`
bounds the whole request, including a response body that arrives slowly.

### Your own HttpClient

`new TronzapClient(options)` sends requests through one `HttpClient` shared by all
clients created that way, so creating many clients does not exhaust sockets. To use
a proxy, custom certificate validation or your own handlers, pass an `HttpClient`.
It is used as given and never disposed or modified: headers are set per request.

```csharp
var handler = new SocketsHttpHandler
{
    Proxy = new WebProxy("http://proxy.internal:3128"),
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
};
var client = new TronzapClient(new HttpClient(handler), options);
```

When both `TronzapClientOptions.Timeout` and `HttpClient.Timeout` are set, the
shorter one wins.

### Dependency injection

`AddTronzap` registers `ITronzapClient` and `TronzapClient` on top of
`IHttpClientFactory`. It returns the `IHttpClientBuilder`, so handlers and
resilience policies plug in as usual:

```csharp
builder.Services
    .AddTronzap(options =>
    {
        options.ApiToken = builder.Configuration["Tronzap:ApiToken"]!;
        options.ApiSecret = builder.Configuration["Tronzap:ApiSecret"]!;
    })
    .AddHttpMessageHandler<MyLoggingHandler>();

// or bind the options from configuration and call AddTronzap() without arguments:
builder.Services.Configure<TronzapClientOptions>(builder.Configuration.GetSection("Tronzap"));
builder.Services.AddTronzap();
```

Then inject `ITronzapClient` where you need it. Depend on the interface to replace
the client with a fake in your own tests.

## Available methods

| Method | Endpoint | Description |
|---|---|---|
| `GetServicesAsync()` | `/v1/services` | Available services and prices |
| `GetBalanceAsync()` | `/v1/balance` | Current account balance |
| `GetAddressInfoAsync(address)` | `/v1/address-info` | Address resources (energy, bandwidth) and balances (TRX, USDT) |
| `EstimateEnergyAsync(request)` | `/v1/estimate-energy` | Energy a transfer needs, and its cost |
| `CalculateAsync(request)` | `/v1/calculate` | Price a purchase without creating a transaction |
| `CreateEnergyTransactionAsync(request)` | `/v1/transaction/new` | Buy energy |
| `CreateBandwidthTransactionAsync(request)` | `/v1/transaction/new` | Buy bandwidth |
| `CreateResourceBundleTransactionAsync(request)` | `/v1/transaction/new` | Buy energy and bandwidth in one transaction |
| `CreateAddressActivationTransactionAsync(request)` | `/v1/transaction/new` | Activate a TRON address |
| `CheckTransactionAsync(request)` | `/v1/transaction/check` | Status of a transaction, by id or external id |
| `GetDirectRechargeInfoAsync()` | `/v1/direct-recharge-info` | Direct recharge address and rates |
| `GetAmlServicesAsync()` | `/v1/aml-checks` | AML services and pricing |
| `CreateAmlCheckAsync(request)` | `/v1/aml-checks/new` | Start an AML screening |
| `CheckAmlStatusAsync(id)` | `/v1/aml-checks/check` | Status and result of an AML check |
| `GetAmlHistoryAsync()` / `GetAmlHistoryAsync(request)` | `/v1/aml-checks/history` | Paginated AML check history |
| `GetSubscriptionsAsync()` | `/v1/subscriptions` | Subscription plans and prices |
| `StartSubscriptionAsync(request)` | `/v1/subscription/start` | Subscribe an address to a plan |
| `CheckSubscriptionAsync(request)` | `/v1/subscription/check` | Status of a subscription, by id or external id |
| `StopSubscriptionAsync(request)` | `/v1/subscription/stop` | Stop a subscription |
| `GetSubscriptionHistoryAsync()` / `GetSubscriptionHistoryAsync(request)` | `/v1/subscriptions/history` | Paginated subscription history |
| `BulkCalculateAsync(request)` | `/v1/bulk/calculate` | Validate and price a bulk order of up to 10 000 addresses |
| `BulkCreateAsync(request)` | `/v1/bulk/create` | Start a bulk order from a calculation |
| `BulkCheckAsync(request)` | `/v1/bulk/check` | Status and progress of a bulk order, by id or external id |
| `GetBulkHistoryAsync()` / `GetBulkHistoryAsync(request)` | `/v1/bulk/history` | Paginated bulk order history |
| `BulkReportAsync(request)` | `/v1/bulk/report` | Per-address CSV report of a bulk order |
| `VerifyWebhookSignature(body, signature)` | — | Check the signature of a bulk order webhook |

Every method is asynchronous and takes an optional `CancellationToken` as its last
parameter.

Parameters live in immutable request records in `Tronzap.Sdk.Requests`, set with
object initializers; required values are `required` members. A request is
validated before it is sent, so an invalid one raises `ArgumentException` and never
reaches the API. Defaults match the API: `Duration` is 1 hour, and AML,
subscription and bulk order history start at page 1 with 10 items. In
`StartSubscriptionRequest`, a zero `DurationDays` or `TransactionsLimit` means no
limit.

Results are immutable records in `Tronzap.Sdk.Responses`. Collections are never
`null`, and values the API may omit are nullable.

### Buying resources

```csharp
// Energy, optionally activating the address in the same call.
Transaction tx = await client.CreateEnergyTransactionAsync(new EnergyTransactionRequest
{
    Address = "TRecipientAddress",
    Energy = 65000,
    Duration = 1,          // hours; see GetServicesAsync() for the durations on sale
    ExternalId = "order-42",
    ActivateAddress = true,
});

// Bandwidth.
tx = await client.CreateBandwidthTransactionAsync(new BandwidthTransactionRequest
{
    Address = "TRecipientAddress",
    Bandwidth = 345,
    ExternalId = "bandwidth-1",
});

// Energy and bandwidth together in one transaction.
tx = await client.CreateResourceBundleTransactionAsync(new ResourceBundleTransactionRequest
{
    Address = "TRecipientAddress",
    Energy = 65000,
    Bandwidth = 345,
    ExternalId = "bundle-1",
});

// Activation on its own.
tx = await client.CreateAddressActivationTransactionAsync(new AddressActivationRequest
{
    Address = "TRecipientAddress",
    ExternalId = "activation-1",
});
```

Energy and bandwidth prices are both per 1000 units, so the cost is
price × amount / 1000: in `GetServicesAsync()`, 65000 energy at an
`EnergyRate.Price` of 0.03 costs 1.95 (the same as `EnergyRate.Price65K`), and
345 bandwidth at a `BandwidthRate.Price` of 1 costs 0.345.

The API currently reports a resource bundle with `Service` equal to
`ServiceType.Energy`, not `ServiceType.ResourceBundle`. Read `Params.Amounts` to see
which resources a transaction contains.

### Following a transaction

A transaction moves through `New` → `Pending` → `Success` or `Failed`:

```csharp
Transaction tx;
do
{
    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    tx = await client.CheckTransactionAsync(CheckTransactionRequest.ByExternalId("order-42"), cancellationToken);
}
while (tx.Status is TransactionStatus.New or TransactionStatus.Pending);

Console.WriteLine($"finished as {tx.Status}, hash {tx.Hash ?? "none"}");
```

### AML screening

```csharp
AmlCheck check = await client.CreateAmlCheckAsync(AmlCheckRequest.ForAddress("TRX", "TAddressToScreen"));
// or AmlCheckRequest.ForHash("BTC", "bc1RecipientAddress", "TX_HASH", AmlDirection.Withdrawal)

AmlCheck result = await client.CheckAmlStatusAsync(check.Id);
if (result.Status == AmlStatus.Completed)
{
    Console.WriteLine($"{result.RiskLevel} {result.RiskScore} {result.RiskFactors.Count} factor(s)");
}
```

For a hash check, `Address` is the recipient address of the transaction, where
the funds were received, and the direction says which side you are on: `Deposit`
if the funds were sent to your address (`Address` is your address), `Withdrawal`
if you sent them (`Address` is the external recipient's address). The risk is
scored for the counterparty: the sender of a deposit, the recipient of a
withdrawal. If you omit the direction, the SDK sends `deposit`.

`RiskScore` is `null` until screening finishes. A completed check can have a score
of 0, which is not the same as having no score yet.

### Subscriptions

A subscription keeps an address supplied with energy for every transaction until
it is stopped or runs out of days or transactions. Pick a plan from
`GetSubscriptionsAsync()` and pass its `SubscriptionId`, such as
`"unlimited_energy"`, not its numeric `Id`:

```csharp
IReadOnlyList<SubscriptionPlan> plans = await client.GetSubscriptionsAsync();
foreach (SubscriptionPlan plan in plans)
{
    Console.WriteLine($"{plan.SubscriptionId} {plan.InitialPrice} {plan.Price}");
}

Subscription subscription = await client.StartSubscriptionAsync(new StartSubscriptionRequest
{
    SubscriptionId = "unlimited_energy",
    Address = "TRecipientAddress",
    DurationDays = 30,     // 0 for no time limit
    TransactionsLimit = 0, // 0 for no limit
    ExternalId = "subscription-42",
});

subscription = await client.CheckSubscriptionAsync(SubscriptionRequest.ByExternalId("subscription-42"));

subscription = await client.StopSubscriptionAsync(SubscriptionRequest.ById(subscription.Id));

SubscriptionHistory history = await client.GetSubscriptionHistoryAsync(
    new SubscriptionHistoryRequest { Status = SubscriptionStatus.Active });
```

Start, check and stop return the subscription with its `Params`; the history
returns the usage counters `TransactionsUsed`, `EnergyUsed` and `TotalPrice`
instead, with `Params` set to `null`. Starting a subscription charges the plan's
initial price. A subscription with a transactions limit cannot be stopped
(`TronzapErrorCode.CannotStopSubscription`).

### Bulk orders

Buy energy for up to 10 000 addresses in one order. `BulkCalculateAsync` validates
and prices the rows and returns a calculation valid for 10 minutes; nothing is
charged. Invalid rows do not fail the request: they come back in `Rejected` with
their row number. `BulkCreateAsync` starts the order; each address is charged when
it is processed, and addresses that are not activated are skipped without a charge.

If the price changed since the calculation, `BulkCreateAsync` does not start the
order and throws `TronzapBulkPriceChangedException` (code `BulkPriceChanged`). Its
`Calculation` is the new calculation: confirm the new total, then start it by its
`Id`.

```csharp
BulkCalculation calculation = await client.BulkCalculateAsync(new BulkCalculateRequest
{
    Items =
    [
        new BulkItem { Address = "TRecipientAddress1", Amount = 65000 },
        new BulkItem { Address = "TRecipientAddress2", Amount = 131000 },
    ],
});
foreach (BulkRejectedRow row in calculation.Rejected)
{
    Console.WriteLine($"row {row.Row}: {row.Reason}");
}

BulkOrder order;
try
{
    order = await client.BulkCreateAsync(new BulkCreateRequest
    {
        Id = calculation.Id,
        ExternalId = "order-42",
        WebhookUrl = "https://example.com/tronzap/bulk",
    });
}
catch (TronzapBulkPriceChangedException e)
{
    BulkCalculation newCalculation = e.Calculation; // confirm the new total, then start it
    order = await client.BulkCreateAsync(new BulkCreateRequest { Id = newCalculation.Id, ExternalId = "order-42" });
}

order = await client.BulkCheckAsync(BulkOrderRequest.ByExternalId("order-42")); // poll until Completed, Partial or Failed
string csv = await client.BulkReportAsync(BulkReportRequest.ById(order.Id));    // row,address,amount,status,reason,charged,transaction_id
BulkHistory history = await client.GetBulkHistoryAsync(new BulkHistoryRequest { Page = 1, PerPage = 10 });
```

With `WebhookUrl`, the API sends one `POST` to it when the order becomes final.
Check its `X-Signature` header against the raw body before trusting it, for
example in ASP.NET Core (`AddTronzap` also registers `TronzapClient`):

```csharp
app.MapPost("/tronzap/bulk", async (HttpRequest request, TronzapClient client) =>
{
    using var body = new MemoryStream();
    await request.Body.CopyToAsync(body);
    if (!client.VerifyWebhookSignature(body.ToArray(), request.Headers["X-Signature"]))
    {
        return Results.Unauthorized();
    }

    // {"id":"01kb2m5x9qj4v8n3c7d6e5f4g3","external_id":"order-42","status":"partial"}
    return Results.Ok();
});
```

## Error handling

Every failure of an API call is a `TronzapException`. Catch a subclass to handle
one kind of failure:

```
TronzapException
├── TronzapApiException                — the API answered with a non-zero code
│   ├── TronzapBulkPriceChangedException — code 92, with the new calculation
│   └── TronzapBulkNoValidItemsException — code 94, with the rejected rows
├── TronzapHttpException               — non-2xx response without an API payload
│   ├── TronzapRateLimitException      — HTTP 429 without an API payload
│   ├── TronzapUnauthorizedException   — HTTP 401 or 403
│   └── TronzapServerException         — HTTP 5xx
├── TronzapInvalidResponseException    — 2xx response the SDK could not read
└── TronzapNetworkException            — no response arrived
    ├── TronzapConnectionException     — DNS failure, connection refused
    ├── TronzapTimeoutException        — the request exceeded its timeout
    └── TronzapSslException            — TLS handshake or certificate failure
```

`TronzapApiException`, `TronzapHttpException` and `TronzapInvalidResponseException`
carry the HTTP status (`StatusCode`) and the raw response body (`ResponseBody`).
`TronzapApiException` also carries the API error code, the error key and the
request ID.

Two failures are not `TronzapException`: invalid arguments raise `ArgumentException`
before anything is sent, and cancelling the `CancellationToken` raises
`OperationCanceledException`, as everywhere in .NET. A timeout is always
`TronzapTimeoutException`, never `OperationCanceledException`.

```csharp
try
{
    await client.CreateEnergyTransactionAsync(
        new EnergyTransactionRequest { Address = "TRecipientAddress", Energy = 65000 },
        cancellationToken);
}
catch (TronzapApiException e)
{
    // Application-level failure: the code says exactly what went wrong.
    switch (e.ErrorCode)
    {
        case TronzapErrorCode.InvalidTronAddress:
            // The key may narrow it down, e.g. "invalid_tron_address.from_address"
            Console.Error.WriteLine($"bad address: {e.ErrorKey}");
            break;
        case TronzapErrorCode.InsufficientFunds:
            Console.Error.WriteLine("top up the account");
            break;
        case TronzapErrorCode.AddressNotActivated:
            Console.Error.WriteLine("activate the address first");
            break;
        default:
            Console.Error.WriteLine($"api error {e.Code}: {e.Message} (request {e.RequestId ?? "-"})");
            break;
    }
}
catch (TronzapRateLimitException e)
{
    // Back off and retry, after e.RetryAfter if the server sent it.
}
catch (TronzapUnauthorizedException)
{
    // Bad token or signature.
}
catch (Exception e) when (e is TronzapTimeoutException or TronzapServerException)
{
    // Transient; safe to retry.
}
catch (TronzapNetworkException)
{
    // Unreachable.
}
```

`RequestId` is the identifier the API assigns to each request. Quote it when
contacting support.

An API error takes precedence over the HTTP status: the API reports some failures
with a 2xx status and others with a 4xx or 5xx status, so a readable payload with
a non-zero code is always reported as `TronzapApiException`, never as
`TronzapHttpException`.

`TronzapApiException.Result` is the `result` the API sent along with the error, as
a `JsonElement`. The SDK reads the data of `BulkPriceChanged` and `BulkNoValidItems`
into `TronzapBulkPriceChangedException.Calculation` and
`TronzapBulkNoValidItemsException.Rejected`. A rate limit reported by the API is a
`TronzapApiException` with `RateLimited`; `TronzapRateLimitException` is only an
HTTP 429 without an API payload, such as one from a proxy. Both, like the other
`TronzapHttpException`s, carry `RetryAfter`, `null` when the server did not say how
long to wait:

```csharp
catch (TronzapApiException e) when (e.ErrorCode == TronzapErrorCode.RateLimited)
{
    await Task.Delay(e.RetryAfter ?? TimeSpan.FromMinutes(1), cancellationToken);
}
```

### API error codes

| Code | Constant | Description |
|------|----------|-------------|
| 1 | `AuthError` | Authentication error – invalid API token or signature |
| 2 | `InvalidServiceOrParams` | Invalid service or parameters |
| 5 | `WalletNotFound` | Internal wallet not found. Contact support. |
| 6 | `InsufficientFunds` | Insufficient funds |
| 10 | `InvalidTronAddress` | Invalid TRON address, or the address already has an active subscription |
| 11 | `InvalidEnergyAmount` | Invalid energy amount |
| 12 | `InvalidDuration` | Invalid duration |
| 20 | `TransactionNotFound` | Transaction/subscription not found |
| 21 | `CannotStopSubscription` | Cannot stop subscription, e.g. it has a transactions limit |
| 24 | `AddressNotActivated` | Address not activated |
| 25 | `AddressAlreadyActivated` | Address already activated |
| 30 | `AmlCheckNotFound` | AML check not found |
| 35 | `ServiceNotAvailable` | Service not available |
| 50 | `InvalidBandwidthAmount` | Invalid bandwidth amount |
| 90 | `BulkNotFound` | Bulk order not found |
| 91 | `BulkExpired` | The calculation expired, calculate again |
| 92 | `BulkPriceChanged` | The price changed; the new calculation is in `TronzapBulkPriceChangedException` |
| 93 | `BulkTooManyItems` | More than 10 000 rows |
| 94 | `BulkNoValidItems` | No valid rows; they are in `TronzapBulkNoValidItemsException` |
| 95 | `BulkResourceNotSupported` | Only energy can be ordered in bulk |
| 96 | `PayloadTooLarge` | Request body too large (HTTP 413) |
| 429 | `RateLimited` | Too many requests; wait `RetryAfter` if given |
| 500 | `InternalServerError` | Internal server error – contact support |

The constants are values of the `TronzapErrorCode` enum. A code this SDK version
does not know is reported as `TronzapErrorCode.Unknown`, with the number still
available from `Code`.

## Decimal and timestamp fields

Amounts and prices are `decimal`, so they keep the exact value the API sent. The
API encodes money as a JSON number in some responses and as a JSON string in
others; both forms are read the same way.

Timestamps are `Timestamp` records: `Value` is the parsed `DateTimeOffset` and
`Raw` is the text exactly as the API sent it. The several formats the API emits
are accepted, and times without an offset are read as UTC. An unrecognised
timestamp leaves `Value` as `null` instead of failing the whole response.

Values the API may add in the future, such as a new transaction status, are
reported as the `Unknown` member of the matching enum instead of failing.

## Testing

```bash
dotnet test
```

It runs the unit tests on .NET 8 and .NET 10 against a local HTTP server: the exact
request body and signature of every endpoint, API and HTTP errors, malformed JSON,
timeouts, cancellation, network and TLS failures, and concurrent use.

## License

The MIT License (MIT). Please see [License File](LICENSE) for more information.

## Support

For support, please contact [support@tronzap.com](mailto:support@tronzap.com).
