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
also exercises the endpoints that create transactions and AML checks, which debit
the account balance. See the comment at the top of the file for the other optional
variables.

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

Every method is asynchronous and takes an optional `CancellationToken` as its last
parameter.

Parameters live in immutable request records in `Tronzap.Sdk.Requests`, set with
object initializers; required values are `required` members. A request is
validated before it is sent, so an invalid one raises `ArgumentException` and never
reaches the API. Defaults match the API: `Duration` is 1 hour, and AML history
starts at page 1 with 10 items.

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

`RiskScore` is `null` until screening finishes. A completed check can have a score
of 0, which is not the same as having no score yet.

## Error handling

Every failure of an API call is a `TronzapException`. Catch a subclass to handle
one kind of failure:

```
TronzapException
├── TronzapApiException                — the API answered with a non-zero code
├── TronzapHttpException               — non-2xx response without an API payload
│   ├── TronzapRateLimitException      — HTTP 429
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
request ID. `TronzapRateLimitException.RetryAfter` holds the `Retry-After` delay
when the API sends one.

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
    // Back off and retry, after e.RetryAfter if the API sent it.
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

### API error codes

| Code | Constant | Description |
|------|----------|-------------|
| 1 | `AuthError` | Authentication error – invalid API token or signature |
| 2 | `InvalidServiceOrParams` | Invalid service or parameters |
| 5 | `WalletNotFound` | Internal wallet not found. Contact support. |
| 6 | `InsufficientFunds` | Insufficient funds |
| 10 | `InvalidTronAddress` | Invalid TRON address |
| 11 | `InvalidEnergyAmount` | Invalid energy amount |
| 12 | `InvalidDuration` | Invalid duration |
| 20 | `TransactionNotFound` | Transaction/subscription not found |
| 21 | `CannotStopSubscription` | Cannot stop subscription |
| 24 | `AddressNotActivated` | Address not activated |
| 25 | `AddressAlreadyActivated` | Address already activated |
| 30 | `AmlCheckNotFound` | AML check not found |
| 35 | `ServiceNotAvailable` | Service not available |
| 50 | `InvalidBandwidthAmount` | Invalid bandwidth amount |
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
