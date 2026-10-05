using System;
using System.Threading.Tasks;
using Tronzap.Sdk.Models;
using Tronzap.Sdk.Requests;
using Xunit;
using static Tronzap.Sdk.Tests.TestSupport;

namespace Tronzap.Sdk.Tests;

public sealed class ValidationTests
{
    public static TheoryData<string, Func<TronzapClient, Task>> InvalidCalls => new()
    {
        { "blank address info", c => c.GetAddressInfoAsync(" ", Ct) },
        { "null address info", c => c.GetAddressInfoAsync(null!, Ct) },
        { "estimate without from", c => c.EstimateEnergyAsync(new() { FromAddress = "", ToAddress = "TTo" }, Ct) },
        { "estimate without to", c => c.EstimateEnergyAsync(new() { FromAddress = "TFrom", ToAddress = null! }, Ct) },
        { "estimate blank contract", c => c.EstimateEnergyAsync(new() { FromAddress = "TFrom", ToAddress = "TTo", ContractAddress = "" }, Ct) },
        { "calculate zero energy", c => c.CalculateAsync(new() { Address = Address, Energy = 0 }, Ct) },
        { "calculate zero duration", c => c.CalculateAsync(new() { Address = Address, Energy = 65000, Duration = 0 }, Ct) },
        { "energy blank address", c => c.CreateEnergyTransactionAsync(new() { Address = "", Energy = 65000 }, Ct) },
        { "energy negative", c => c.CreateEnergyTransactionAsync(new() { Address = Address, Energy = -1 }, Ct) },
        { "energy blank external id", c => c.CreateEnergyTransactionAsync(new() { Address = Address, Energy = 65000, ExternalId = " " }, Ct) },
        { "bandwidth zero", c => c.CreateBandwidthTransactionAsync(new() { Address = Address, Bandwidth = 0 }, Ct) },
        { "bundle zero bandwidth", c => c.CreateResourceBundleTransactionAsync(new() { Address = Address, Energy = 65000, Bandwidth = 0 }, Ct) },
        { "bundle zero energy", c => c.CreateResourceBundleTransactionAsync(new() { Address = Address, Energy = 0, Bandwidth = 345 }, Ct) },
        { "activation blank", c => c.CreateAddressActivationTransactionAsync(new() { Address = "\t" }, Ct) },
        { "check without ids", c => c.CheckTransactionAsync(new CheckTransactionRequest(), Ct) },
        { "check blank id", c => c.CheckTransactionAsync(CheckTransactionRequest.ById(""), Ct) },
        { "check blank external id", c => c.CheckTransactionAsync(CheckTransactionRequest.ByExternalId(" "), Ct) },
        { "aml unknown type", c => c.CreateAmlCheckAsync(new() { Type = AmlCheckType.Unknown, Network = "TRX", Address = "T" }, Ct) },
        { "aml undefined type", c => c.CreateAmlCheckAsync(new() { Type = (AmlCheckType)42, Network = "TRX", Address = "T" }, Ct) },
        { "aml hash without hash", c => c.CreateAmlCheckAsync(new() { Type = AmlCheckType.Hash, Network = "BTC", Address = "bc1" }, Ct) },
        { "aml blank network", c => c.CreateAmlCheckAsync(AmlCheckRequest.ForAddress("", "T"), Ct) },
        { "aml unknown direction", c => c.CreateAmlCheckAsync(AmlCheckRequest.ForHash("BTC", "bc1", "h", AmlDirection.Unknown), Ct) },
        { "aml status blank", c => c.CheckAmlStatusAsync("", Ct) },
        { "history page zero", c => c.GetAmlHistoryAsync(new() { Page = 0 }, Ct) },
        { "history per page negative", c => c.GetAmlHistoryAsync(new() { PerPage = -5 }, Ct) },
        { "history unknown status", c => c.GetAmlHistoryAsync(new() { Status = AmlStatus.Unknown }, Ct) },
    };

    [Theory]
    [MemberData(nameof(InvalidCalls))]
    public async Task InvalidArgumentsAreRejectedBeforeSending(string name, Func<TronzapClient, Task> call)
    {
        await using var server = TestServer.Start(Reply.Ok("{}"));

        var e = await Assert.ThrowsAnyAsync<ArgumentException>(() => call(Client(server)));

        Assert.False(string.IsNullOrEmpty(e.Message), name);
        Assert.Empty(server.Requests);
    }

    [Fact]
    public async Task NullRequestsThrowArgumentNull()
    {
        await using var server = TestServer.Start(Reply.Ok("{}"));
        TronzapClient client = Client(server);

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.EstimateEnergyAsync(null!, Ct));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CalculateAsync(null!, Ct));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateEnergyTransactionAsync(null!, Ct));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateBandwidthTransactionAsync(null!, Ct));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateResourceBundleTransactionAsync(null!, Ct));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateAddressActivationTransactionAsync(null!, Ct));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CheckTransactionAsync(null!, Ct));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateAmlCheckAsync(null!, Ct));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetAmlHistoryAsync(null!, Ct));
        Assert.Empty(server.Requests);
    }

    [Fact]
    public void RequestDefaultsMatchTheApi()
    {
        Assert.Equal(1, new CalculateRequest { Address = Address, Energy = 65000 }.Duration);
        Assert.Equal(1, new EnergyTransactionRequest { Address = Address, Energy = 65000 }.Duration);
        Assert.Equal(1, new ResourceBundleTransactionRequest { Address = Address, Energy = 65000, Bandwidth = 345 }.Duration);
        Assert.False(new EnergyTransactionRequest { Address = Address, Energy = 65000 }.ActivateAddress);
        var history = new AmlHistoryRequest();
        Assert.Equal(1, history.Page);
        Assert.Equal(10, history.PerPage);
        Assert.Null(history.Status);
    }

    [Fact]
    public void RequestsHaveValueEquality()
    {
        var a = new EnergyTransactionRequest { Address = Address, Energy = 65000, ExternalId = "x" };

        Assert.Equal(a, a with { });
        Assert.NotEqual(a, a with { Energy = 65001 });
    }
}
