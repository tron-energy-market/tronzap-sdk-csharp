# TronZap SDK for .NET

Official .NET SDK for the [TronZap API](https://docs.tronzap.com/): rent TRON energy and bandwidth to make USDT
(TRC20) transfers cheaper, activate TRON addresses and run AML checks.

- Website: https://tronzap.com/
- API reference: https://docs.tronzap.com/
- Source, full documentation and examples: https://github.com/tron-energy-market/tronzap-sdk-csharp

## Installation

```bash
dotnet add package Tronzap.Sdk
```

Requires .NET 8 or newer.

## Quick start

```csharp
using Tronzap.Sdk;
using Tronzap.Sdk.Requests;
using Tronzap.Sdk.Responses;

var client = new TronzapClient(new TronzapClientOptions
{
    ApiToken = "your_api_token",
    ApiSecret = "your_api_secret",
});

AccountBalance balance = await client.GetBalanceAsync(cancellationToken);

Transaction tx = await client.CreateEnergyTransactionAsync(
    new EnergyTransactionRequest { Address = "TRecipientAddress", Energy = 65000 },
    cancellationToken);
```

With dependency injection and `IHttpClientFactory`:

```csharp
builder.Services.AddTronzap(options =>
{
    options.ApiToken = builder.Configuration["Tronzap:ApiToken"]!;
    options.ApiSecret = builder.Configuration["Tronzap:ApiSecret"]!;
});
// then inject ITronzapClient
```

## Features

- Every TronZap API operation: services and prices, balance, address info, energy estimate, price calculation,
  energy, bandwidth, resource bundle and address activation transactions, transaction status, direct recharge,
  AML checks and history.
- Typed, immutable request and response records; collections are never `null`.
- `async` methods that all take a `CancellationToken`.
- Typed exceptions: `TronzapApiException` carries the API error code, error key, request ID, HTTP status and
  response body.
- Works with a shared `HttpClient`, your own `HttpClient`, or `IHttpClientFactory`. No ASP.NET Core dependency.

See the [README on GitHub](https://github.com/tron-energy-market/tronzap-sdk-csharp#readme) for configuration,
error handling and every method.

## License

MIT. Support: [support@tronzap.com](mailto:support@tronzap.com).
