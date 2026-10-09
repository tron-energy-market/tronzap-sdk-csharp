using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tronzap.Sdk.Models;
using Tronzap.Sdk.Requests;
using Xunit;
using static Tronzap.Sdk.Tests.TestSupport;

namespace Tronzap.Sdk.Tests;

public sealed class RequestWireTests
{
    private const string TransactionResult = """{"id":"tx-1","service":"energy","params":{"address":"T","amounts":{"energy":65000},"duration":1},"status":"new","amount":5.47}""";
    private const string AmlResult = """{"id":"aml-1","type":"address","address":"T","network":"TRX","status":"pending"}""";
    private const string SubscriptionResult = """{"id":"sub-1","subscription_id":"unlimited_energy","status":"active"}""";

    public static TheoryData<string, string, string, string> Calls => new()
    {
        { "GetServices", "/v1/services", "{}", """{"energy":[],"bandwidth":[]}""" },
        { "GetBalance", "/v1/balance", "{}", """{"balance":"1.5","address":"TDeposit"}""" },
        { "GetAddressInfo", "/v1/address-info", $$$"""{"address":"{{{Address}}}"}""", """{"resources":{},"balances":{}}""" },
        { "EstimateEnergy", "/v1/estimate-energy", """{"from_address":"TFrom","to_address":"TTo"}""", "{}" },
        { "EstimateEnergyContract", "/v1/estimate-energy", """{"from_address":"TFrom","to_address":"TTo","contract_address":"TContract"}""", "{}" },
        { "Calculate", "/v1/calculate", $$$"""{"address":"{{{Address}}}","amount":65000,"duration":1}""", "{}" },
        { "CalculateDay", "/v1/calculate", $$$"""{"address":"{{{Address}}}","amount":65000,"duration":24}""", "{}" },
        { "Energy", "/v1/transaction/new", $$$"""{"service":"energy","params":{"address":"{{{Address}}}","amounts":{"energy":65000},"duration":1}}""", TransactionResult },
        { "EnergyFull", "/v1/transaction/new", $$$"""{"service":"energy","params":{"address":"{{{Address}}}","amounts":{"energy":65000},"duration":24,"activate_address":true},"external_id":"order-42"}""", TransactionResult },
        { "Bandwidth", "/v1/transaction/new", $$$"""{"service":"bandwidth","params":{"address":"{{{Address}}}","amounts":{"bandwidth":345},"duration":1}}""", TransactionResult },
        { "BandwidthExternal", "/v1/transaction/new", $$$"""{"service":"bandwidth","params":{"address":"{{{Address}}}","amounts":{"bandwidth":345},"duration":1},"external_id":"bw-1"}""", TransactionResult },
        { "Bundle", "/v1/transaction/new", $$$"""{"service":"resource_bundle","params":{"address":"{{{Address}}}","amounts":{"energy":65000,"bandwidth":345},"duration":1}}""", TransactionResult },
        { "BundleFull", "/v1/transaction/new", $$$"""{"service":"resource_bundle","params":{"address":"{{{Address}}}","amounts":{"energy":65000,"bandwidth":345},"duration":1,"activate_address":true},"external_id":"bundle-1"}""", TransactionResult },
        { "Activation", "/v1/transaction/new", $$$"""{"service":"activate_address","params":{"address":"{{{Address}}}"}}""", TransactionResult },
        { "ActivationExternal", "/v1/transaction/new", $$$"""{"service":"activate_address","params":{"address":"{{{Address}}}"},"external_id":"act-1"}""", TransactionResult },
        { "CheckById", "/v1/transaction/check", """{"id":"tx-1"}""", TransactionResult },
        { "CheckByExternalId", "/v1/transaction/check", """{"external_id":"order-42"}""", TransactionResult },
        { "CheckByBoth", "/v1/transaction/check", """{"id":"tx-1","external_id":"order-42"}""", TransactionResult },
        { "DirectRecharge", "/v1/direct-recharge-info", "{}", """{"address":"T","rates":[]}""" },
        { "AmlServices", "/v1/aml-checks", "{}", "[]" },
        { "AmlAddress", "/v1/aml-checks/new", """{"type":"address","network":"TRX","address":"TScreen"}""", AmlResult },
        { "AmlHash", "/v1/aml-checks/new", """{"type":"hash","network":"BTC","address":"bc1x","hash":"h1","direction":"withdrawal"}""", AmlResult },
        { "AmlStatus", "/v1/aml-checks/check", """{"id":"aml-1"}""", AmlResult },
        { "AmlHistoryDefault", "/v1/aml-checks/history", """{"page":1,"per_page":10}""", """{"page":1,"per_page":10,"total":0,"items":[]}""" },
        { "AmlHistoryFiltered", "/v1/aml-checks/history", """{"page":3,"per_page":25,"status":"completed"}""", """{"page":3,"per_page":25,"total":0,"items":[]}""" },
        { "Subscriptions", "/v1/subscriptions", "{}", "{}" },
        { "StartSubscription", "/v1/subscription/start", """{"subscription_id":"unlimited_energy","external_id":"sub-1","params":{"address":"TAddress","duration":30,"transactions_limit":0,"activate_address":true}}""", SubscriptionResult },
        { "StartSubscriptionDefaults", "/v1/subscription/start", """{"subscription_id":"unlimited_energy","params":{"address":"TAddress","duration":0,"transactions_limit":0}}""", SubscriptionResult },
        { "StartSubscriptionLimited", "/v1/subscription/start", """{"subscription_id":"energy_pack_100","params":{"address":"TAddress","duration":5,"transactions_limit":10}}""", SubscriptionResult },
        { "CheckSubscriptionById", "/v1/subscription/check", """{"id":"sub-1"}""", SubscriptionResult },
        { "CheckSubscriptionByExternalId", "/v1/subscription/check", """{"external_id":"ext-1"}""", SubscriptionResult },
        { "StopSubscription", "/v1/subscription/stop", """{"id":"sub-1","external_id":"ext-1"}""", SubscriptionResult },
        { "StopSubscriptionByExternalId", "/v1/subscription/stop", """{"external_id":"ext-1"}""", SubscriptionResult },
        { "SubscriptionHistoryDefault", "/v1/subscriptions/history", """{"page":1,"per_page":10}""", """{"page":1,"per_page":10,"total":0,"items":[]}""" },
        { "SubscriptionHistoryFiltered", "/v1/subscriptions/history", """{"page":2,"per_page":50,"status":"active"}""", """{"page":2,"per_page":50,"total":0,"items":[]}""" },
    };

    private static readonly Dictionary<string, Func<TronzapClient, Task>> Invocations = new()
    {
        ["GetServices"] = c => c.GetServicesAsync(Ct),
        ["GetBalance"] = c => c.GetBalanceAsync(Ct),
        ["GetAddressInfo"] = c => c.GetAddressInfoAsync(Address, Ct),
        ["EstimateEnergy"] = c => c.EstimateEnergyAsync(new() { FromAddress = "TFrom", ToAddress = "TTo" }, Ct),
        ["EstimateEnergyContract"] = c => c.EstimateEnergyAsync(new() { FromAddress = "TFrom", ToAddress = "TTo", ContractAddress = "TContract" }, Ct),
        ["Calculate"] = c => c.CalculateAsync(new() { Address = Address, Energy = 65000 }, Ct),
        ["CalculateDay"] = c => c.CalculateAsync(new() { Address = Address, Energy = 65000, Duration = 24 }, Ct),
        ["Energy"] = c => c.CreateEnergyTransactionAsync(new() { Address = Address, Energy = 65000 }, Ct),
        ["EnergyFull"] = c => c.CreateEnergyTransactionAsync(
            new() { Address = Address, Energy = 65000, Duration = 24, ExternalId = "order-42", ActivateAddress = true }, Ct),
        ["Bandwidth"] = c => c.CreateBandwidthTransactionAsync(new() { Address = Address, Bandwidth = 345 }, Ct),
        ["BandwidthExternal"] = c => c.CreateBandwidthTransactionAsync(new() { Address = Address, Bandwidth = 345, ExternalId = "bw-1" }, Ct),
        ["Bundle"] = c => c.CreateResourceBundleTransactionAsync(new() { Address = Address, Energy = 65000, Bandwidth = 345 }, Ct),
        ["BundleFull"] = c => c.CreateResourceBundleTransactionAsync(
            new() { Address = Address, Energy = 65000, Bandwidth = 345, ExternalId = "bundle-1", ActivateAddress = true }, Ct),
        ["Activation"] = c => c.CreateAddressActivationTransactionAsync(new() { Address = Address }, Ct),
        ["ActivationExternal"] = c => c.CreateAddressActivationTransactionAsync(new() { Address = Address, ExternalId = "act-1" }, Ct),
        ["CheckById"] = c => c.CheckTransactionAsync(CheckTransactionRequest.ById("tx-1"), Ct),
        ["CheckByExternalId"] = c => c.CheckTransactionAsync(CheckTransactionRequest.ByExternalId("order-42"), Ct),
        ["CheckByBoth"] = c => c.CheckTransactionAsync(new() { Id = "tx-1", ExternalId = "order-42" }, Ct),
        ["DirectRecharge"] = c => c.GetDirectRechargeInfoAsync(Ct),
        ["AmlServices"] = c => c.GetAmlServicesAsync(Ct),
        ["AmlAddress"] = c => c.CreateAmlCheckAsync(AmlCheckRequest.ForAddress("TRX", "TScreen"), Ct),
        ["AmlHash"] = c => c.CreateAmlCheckAsync(AmlCheckRequest.ForHash("BTC", "bc1x", "h1", AmlDirection.Withdrawal), Ct),
        ["AmlStatus"] = c => c.CheckAmlStatusAsync("aml-1", Ct),
        ["AmlHistoryDefault"] = c => c.GetAmlHistoryAsync(Ct),
        ["AmlHistoryFiltered"] = c => c.GetAmlHistoryAsync(new() { Page = 3, PerPage = 25, Status = AmlStatus.Completed }, Ct),
        ["Subscriptions"] = c => c.GetSubscriptionsAsync(Ct),
        ["StartSubscription"] = c => c.StartSubscriptionAsync(
            new() { SubscriptionId = "unlimited_energy", Address = "TAddress", DurationDays = 30, ExternalId = "sub-1", ActivateAddress = true }, Ct),
        ["StartSubscriptionDefaults"] = c => c.StartSubscriptionAsync(new() { SubscriptionId = "unlimited_energy", Address = "TAddress" }, Ct),
        ["StartSubscriptionLimited"] = c => c.StartSubscriptionAsync(
            new() { SubscriptionId = "energy_pack_100", Address = "TAddress", DurationDays = 5, TransactionsLimit = 10 }, Ct),
        ["CheckSubscriptionById"] = c => c.CheckSubscriptionAsync(SubscriptionRequest.ById("sub-1"), Ct),
        ["CheckSubscriptionByExternalId"] = c => c.CheckSubscriptionAsync(SubscriptionRequest.ByExternalId("ext-1"), Ct),
        ["StopSubscription"] = c => c.StopSubscriptionAsync(new() { Id = "sub-1", ExternalId = "ext-1" }, Ct),
        ["StopSubscriptionByExternalId"] = c => c.StopSubscriptionAsync(SubscriptionRequest.ByExternalId("ext-1"), Ct),
        ["SubscriptionHistoryDefault"] = c => c.GetSubscriptionHistoryAsync(Ct),
        ["SubscriptionHistoryFiltered"] = c => c.GetSubscriptionHistoryAsync(new() { Page = 2, PerPage = 50, Status = SubscriptionStatus.Active }, Ct),
    };

    [Theory]
    [MemberData(nameof(Calls))]
    public async Task SendsExactBodyToEndpoint(string call, string path, string expectedBody, string result)
    {
        await using var server = TestServer.Start(Reply.Ok(result));

        await Invocations[call](Client(server));

        RecordedRequest request = server.SingleRequest;
        Assert.Equal("POST", request.Method);
        Assert.Equal(path, request.Path);
        AssertJsonEqual(expectedBody, request);
    }

    [Theory]
    [MemberData(nameof(Calls))]
    public async Task SignsTheBytesTheServerReceived(string call, string path, string expectedBody, string result)
    {
        _ = path;
        _ = expectedBody;
        await using var server = TestServer.Start(Reply.Ok(result));

        await Invocations[call](Client(server));

        RecordedRequest request = server.SingleRequest;
        Assert.Equal(ExpectedSignature(request.Body), request.Header("X-Signature"));
    }

    [Theory]
    [InlineData(AmlStatus.Pending, "pending")]
    [InlineData(AmlStatus.Processing, "processing")]
    [InlineData(AmlStatus.Completed, "completed")]
    [InlineData(AmlStatus.Failed, "failed")]
    public async Task SendsEveryAmlStatusFilter(AmlStatus status, string wire)
    {
        await using var server = TestServer.Start(Reply.Ok("{}"));

        await Client(server).GetAmlHistoryAsync(new() { Status = status }, Ct);

        Assert.Equal(wire, server.SingleRequest.Json!["status"]!.GetValue<string>());
    }

    [Theory]
    [InlineData(SubscriptionStatus.New, "new")]
    [InlineData(SubscriptionStatus.Pending, "pending")]
    [InlineData(SubscriptionStatus.Error, "error")]
    [InlineData(SubscriptionStatus.Active, "active")]
    [InlineData(SubscriptionStatus.Stopped, "stopped")]
    [InlineData(SubscriptionStatus.Expired, "expired")]
    public async Task SendsEverySubscriptionStatusFilter(SubscriptionStatus status, string wire)
    {
        await using var server = TestServer.Start(Reply.Ok("{}"));

        await Client(server).GetSubscriptionHistoryAsync(new() { Status = status }, Ct);

        Assert.Equal(wire, server.SingleRequest.Json!["status"]!.GetValue<string>());
    }

    [Fact]
    public async Task SendsDepositDirection()
    {
        await using var server = TestServer.Start(Reply.Ok("{}"));

        await Client(server).CreateAmlCheckAsync(AmlCheckRequest.ForHash("TRX", "T", "h", AmlDirection.Deposit), Ct);

        Assert.Equal("deposit", server.SingleRequest.Json!["direction"]!.GetValue<string>());
    }

    [Fact]
    public async Task SendsAuthenticationAndContentHeaders()
    {
        await using var server = TestServer.Start(Reply.Ok("{}"));

        await Client(server).GetBalanceAsync(Ct);

        RecordedRequest request = server.SingleRequest;
        Assert.Equal($"Bearer {Token}", request.Header("Authorization"));
        Assert.Equal("application/json", request.Header("Content-Type"));
        Assert.Equal("application/json", request.Header("Accept"));
        Assert.Equal($"tronzap-sdk-csharp/{TronzapClient.Version}", request.Header("User-Agent"));
        Assert.Matches("^[0-9a-f]{64}$", request.Header("X-Signature"));
    }

    [Fact]
    public async Task SendsCustomUserAgent()
    {
        await using var server = TestServer.Start(Reply.Ok("{}"));

        await Client(server, o => o.UserAgent = "my-app/2.0 (+https://example.com)").GetBalanceAsync(Ct);

        Assert.Equal("my-app/2.0 (+https://example.com)", server.SingleRequest.Header("User-Agent"));
    }

    [Fact]
    public async Task SignatureDependsOnSecret()
    {
        await using var server = TestServer.Start(Reply.Ok("{}"));

        await Client(server, o => o.ApiSecret = "another-secret").GetBalanceAsync(Ct);

        RecordedRequest request = server.SingleRequest;
        Assert.NotEqual(ExpectedSignature(request.Body), request.Header("X-Signature"));
    }

    [Fact]
    public async Task SignsKnownVector()
    {
        await using var server = TestServer.Start(Reply.Ok("{}"));

        await Client(server).GetBalanceAsync(Ct);

        Assert.Equal("{}", server.SingleRequest.BodyText);
        Assert.Equal(
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData("{}test-secret"u8)).ToLowerInvariant(),
            server.SingleRequest.Header("X-Signature"));
    }

    [Fact]
    public async Task KeepsNonAsciiTextSignedAsSent()
    {
        await using var server = TestServer.Start(Reply.Ok(TransactionResult));

        await Client(server).CreateEnergyTransactionAsync(
            new() { Address = Address, Energy = 65000, ExternalId = "pedido-año-订单-😀 \"q\"" }, Ct);

        RecordedRequest request = server.SingleRequest;
        Assert.Equal("pedido-año-订单-😀 \"q\"", request.Json!["external_id"]!.GetValue<string>());
        Assert.Equal(ExpectedSignature(request.Body), request.Header("X-Signature"));
    }

    [Theory]
    [InlineData("api.example.com", "https://api.example.com")]
    [InlineData("api.example.com/", "https://api.example.com")]
    [InlineData("  https://api.example.com//  ", "https://api.example.com")]
    [InlineData("//api.example.com", "https://api.example.com")]
    [InlineData("http://localhost:8080", "http://localhost:8080")]
    [InlineData("https://api.example.com/prefix/", "https://api.example.com/prefix")]
    public void NormalizesBaseUrl(string input, string expected) =>
        Assert.Equal(expected, TronzapClient.NormalizeBaseUrl(input));

    [Fact]
    public async Task AppendsEndpointToBaseUrlPath()
    {
        await using var server = TestServer.Start(Reply.Ok("{}"));

        await Client(server, o => o.BaseUrl = server.BaseUrl + "/prefix/").GetBalanceAsync(Ct);

        Assert.Equal("/prefix/v1/balance", server.SingleRequest.Path);
    }
}
