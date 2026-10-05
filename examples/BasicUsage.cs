#:project ../src/Tronzap.Sdk/Tronzap.Sdk.csproj

// Walks through the TronZap API operations. By default it only reads and spends nothing.
//
//   export TRONZAP_API_TOKEN=your_api_token
//   export TRONZAP_API_SECRET=your_api_secret
//   export TRONZAP_BASE_URL=api.tronzap.com      # optional, e.g. a dev host
//   export TRONZAP_ADDRESS=TRON_ADDRESS          # optional
//   export TRONZAP_FROM_ADDRESS=TRON_ADDRESS     # optional, with TO_ADDRESS
//   export TRONZAP_TO_ADDRESS=TRON_ADDRESS       # optional, with FROM_ADDRESS
//   export TRONZAP_TRANSACTION_ID=id             # optional
//   export TRONZAP_AML_CHECK_ID=id               # optional
//   dotnet run examples/BasicUsage.cs
//
// Setting TRONZAP_ALLOW_PURCHASES=1 additionally exercises the endpoints that create transactions and AML checks.
// Those DEBIT THE ACCOUNT BALANCE. It is meant for verifying an integration against a development environment, and
// it also needs TRONZAP_ADDRESS.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tronzap.Sdk;
using Tronzap.Sdk.Exceptions;
using Tronzap.Sdk.Models;
using Tronzap.Sdk.Requests;
using Tronzap.Sdk.Responses;

const long Energy = 65000;
const long Bandwidth = 345;

string? token = Env("TRONZAP_API_TOKEN");
string? secret = Env("TRONZAP_API_SECRET");
if (token is null || secret is null)
{
    Console.Error.WriteLine("set TRONZAP_API_TOKEN and TRONZAP_API_SECRET");
    return 2;
}

var options = new TronzapClientOptions
{
    ApiToken = token,
    ApiSecret = secret,
    Timeout = TimeSpan.FromSeconds(20),
    UserAgent = "tronzap-example/1.0",
};
if (Env("TRONZAP_BASE_URL") is { } baseUrl)
{
    options.BaseUrl = baseUrl;
}

Console.WriteLine($"Calling {options.BaseUrl}");
var client = new TronzapClient(options);
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};
CancellationToken ct = cts.Token;
var failed = new List<string>();

await Step("GetBalance", async () =>
{
    AccountBalance balance = await client.GetBalanceAsync(ct);
    Console.WriteLine($"  balance {Money(balance.Balance)}, deposit address {balance.Address}");
});
await Step("GetServices", async () =>
{
    ServiceRates services = await client.GetServicesAsync(ct);
    foreach (EnergyRate rate in services.Energy)
    {
        Console.WriteLine($"  energy {rate.Duration}h {rate.MinEnergy}..{rate.MaxEnergy} at {Money(rate.Price)} per unit (65k = {Money(rate.Price65K)})");
    }

    foreach (BandwidthRate rate in services.Bandwidth)
    {
        Console.WriteLine($"  bandwidth {rate.Duration}h {rate.MinAmount}..{rate.MaxAmount} at {Money(rate.Price)} per 1000 units");
    }

    if (services.ActivateAddress is { } activation)
    {
        Console.WriteLine($"  activation {Money(activation.Price)}");
    }
});
await Step("GetDirectRechargeInfo", async () =>
{
    DirectRechargeInfo info = await client.GetDirectRechargeInfoAsync(ct);
    Console.WriteLine($"  pay to {info.Address}, {info.Rates.Count} rate(s)");
});
await Step("GetAmlServices", async () =>
{
    foreach (AmlService service in await client.GetAmlServicesAsync(ct))
    {
        Console.WriteLine($"  {service.Id} {service.Type} at {Money(service.Price)}");
    }
});
await Step("GetAmlHistory", async () =>
{
    AmlHistory history = await client.GetAmlHistoryAsync(ct);
    Console.WriteLine($"  page {history.Page}, {history.Items.Count} of {history.Total} check(s)");
});

string? address = Env("TRONZAP_ADDRESS");
await OptionalStep("GetAddressInfo", address, async value =>
{
    AddressInfo info = await client.GetAddressInfoAsync(value, ct);
    Console.WriteLine($"  energy {info.Resources.Energy}, bandwidth {info.Resources.Bandwidth}, balances {string.Join(", ", info.Balances.Select(b => $"{b.Key} {Money(b.Value)}"))}");
});
await OptionalStep("Calculate", address, async value =>
{
    Calculation calculation = await client.CalculateAsync(new CalculateRequest { Address = value, Energy = Energy }, ct);
    Console.WriteLine($"  {calculation.Energy} energy for {calculation.Duration}h costs {Money(calculation.Total)}");
});

string? from = Env("TRONZAP_FROM_ADDRESS");
string? to = Env("TRONZAP_TO_ADDRESS");
await OptionalStep("EstimateEnergy", to is null ? null : from, async value =>
{
    EnergyEstimate estimate = await client.EstimateEnergyAsync(new EstimateEnergyRequest { FromAddress = value, ToAddress = to! }, ct);
    Console.WriteLine($"  {estimate.Energy} energy, total {Money(estimate.Total)}");
});
await OptionalStep("CheckTransaction", Env("TRONZAP_TRANSACTION_ID"),
    async value => Print(await client.CheckTransactionAsync(CheckTransactionRequest.ById(value), ct)));
await OptionalStep("CheckAmlStatus", Env("TRONZAP_AML_CHECK_ID"), async value =>
{
    AmlCheck check = await client.CheckAmlStatusAsync(value, ct);
    Console.WriteLine($"  {check.Status}, risk {(check.RiskScore is { } score ? Money(score) : "not scored yet")}");
});

if (Env("TRONZAP_ALLOW_PURCHASES") != "1")
{
    Console.WriteLine("\nSkipping purchases: set TRONZAP_ALLOW_PURCHASES=1 to create transactions (debits the balance)");
}
else if (address is null)
{
    Console.WriteLine("\nSkipping purchases: TRONZAP_ADDRESS is not set");
}
else
{
    string runId = $"csharp-example-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

    await Step("CreateAddressActivationTransaction", async () =>
    {
        try
        {
            Print(await client.CreateAddressActivationTransactionAsync(
                new AddressActivationRequest { Address = address, ExternalId = $"{runId}-activate" }, ct));
        }
        catch (TronzapApiException e) when (e.ErrorCode == TronzapErrorCode.AddressAlreadyActivated)
        {
            Console.WriteLine("  already activated");
        }
    });
    await Step("CreateEnergyTransaction", async () =>
    {
        Print(await client.CreateEnergyTransactionAsync(
            new EnergyTransactionRequest { Address = address, Energy = Energy, ExternalId = $"{runId}-energy" }, ct));
        Print(await client.CheckTransactionAsync(CheckTransactionRequest.ByExternalId($"{runId}-energy"), ct));
    });
    await Step("CreateBandwidthTransaction", async () => Print(await client.CreateBandwidthTransactionAsync(
        new BandwidthTransactionRequest { Address = address, Bandwidth = Bandwidth, ExternalId = $"{runId}-bandwidth" }, ct)));
    await Step("CreateResourceBundleTransaction", async () => Print(await client.CreateResourceBundleTransactionAsync(
        new ResourceBundleTransactionRequest { Address = address, Energy = Energy, Bandwidth = Bandwidth, ExternalId = $"{runId}-bundle" }, ct)));
    await Step("CreateAmlCheck", async () =>
    {
        AmlCheck check = await client.CreateAmlCheckAsync(AmlCheckRequest.ForAddress("TRX", address), ct);
        Console.WriteLine($"  AML check {check.Id} is {check.Status}");
    });
}

if (failed.Count > 0)
{
    Console.Error.WriteLine($"\nFailed: {string.Join(", ", failed)}");
    return 1;
}

Console.WriteLine("\nAll calls succeeded");
return 0;

async Task Step(string name, Func<Task> call)
{
    Console.WriteLine($"\n{name}");
    try
    {
        await call();
    }
    catch (TronzapException e)
    {
        Console.WriteLine($"  FAILED: {e.GetType().Name}: {e.Message}");
        failed.Add(name);
    }
}

async Task OptionalStep(string name, string? subject, Func<string, Task> call)
{
    if (subject is null)
    {
        Console.WriteLine($"\n{name}\n  skipped: its environment variable is not set");
        return;
    }

    await Step(name, () => call(subject));
}

static void Print(Transaction tx) =>
    Console.WriteLine($"  {tx.Id} {tx.Service} {tx.Status}, charged {Money(tx.Amount)}, created {Describe(tx.CreatedAt)}");

static string Describe(Timestamp? timestamp) => timestamp switch
{
    null => "unknown",
    { Value: { } value } => value.ToString("O", CultureInfo.InvariantCulture),
    _ => $"UNPARSED({timestamp.Raw})",
};

static string Money(decimal value) => value.ToString(CultureInfo.InvariantCulture);

static string? Env(string name) =>
    Environment.GetEnvironmentVariable(name) is { } value && !string.IsNullOrWhiteSpace(value) ? value : null;
