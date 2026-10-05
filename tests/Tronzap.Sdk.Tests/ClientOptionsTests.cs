using System;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tronzap.Sdk.Models;
using Xunit;
using static Tronzap.Sdk.Tests.TestSupport;

namespace Tronzap.Sdk.Tests;

public sealed class ClientOptionsTests
{
    private static TronzapClientOptions Valid() => new() { ApiToken = Token, ApiSecret = Secret };

    public static TheoryData<string, Action<TronzapClientOptions>> InvalidOptions => new()
    {
        { "missing token", o => o.ApiToken = "" },
        { "blank token", o => o.ApiToken = "  " },
        { "null token", o => o.ApiToken = null! },
        { "missing secret", o => o.ApiSecret = "" },
        { "blank base url", o => o.BaseUrl = " " },
        { "null base url", o => o.BaseUrl = null! },
        { "ftp base url", o => o.BaseUrl = "ftp://api.tronzap.com" },
        { "query in base url", o => o.BaseUrl = "https://api.tronzap.com?x=1" },
        { "fragment in base url", o => o.BaseUrl = "https://api.tronzap.com#x" },
        { "garbage base url", o => o.BaseUrl = "http://" },
        { "zero timeout", o => o.Timeout = TimeSpan.Zero },
        { "negative timeout", o => o.Timeout = TimeSpan.FromSeconds(-1) },
        { "huge timeout", o => o.Timeout = TimeSpan.FromDays(30) },
        { "blank user agent", o => o.UserAgent = " " },
        { "multi-line user agent", o => o.UserAgent = "app\r\nX-Evil: 1" },
    };

    [Theory]
    [MemberData(nameof(InvalidOptions))]
    public void RejectsInvalidOptions(string name, Action<TronzapClientOptions> configure)
    {
        TronzapClientOptions options = Valid();
        configure(options);

        var e = Assert.ThrowsAny<ArgumentException>(() => new TronzapClient(options));

        Assert.False(string.IsNullOrEmpty(e.Message), name);
    }

    [Fact]
    public void RejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new TronzapClient(null!));
        using var httpClient = new HttpClient();
        Assert.Throws<ArgumentNullException>(() => new TronzapClient(httpClient, null!));
        Assert.Throws<ArgumentNullException>(() => new TronzapClient(null!, Valid()));
    }

    [Fact]
    public void AcceptsInfiniteTimeout()
    {
        TronzapClientOptions options = Valid();
        options.Timeout = Timeout.InfiniteTimeSpan;

        _ = new TronzapClient(options);
    }

    [Fact]
    public void DefaultsMatchTheApi()
    {
        var options = new TronzapClientOptions();

        Assert.Equal("https://api.tronzap.com", options.BaseUrl);
        Assert.Equal(TimeSpan.FromSeconds(30), options.Timeout);
        Assert.Null(options.UserAgent);
    }

    [Fact]
    public async Task CopiesOptionsAtConstruction()
    {
        await using var server = TestServer.Start(Reply.Ok("{}"));
        var options = new TronzapClientOptions { ApiToken = Token, ApiSecret = Secret, BaseUrl = server.BaseUrl };
        var client = new TronzapClient(options);

        options.ApiToken = "changed";
        options.BaseUrl = "http://127.0.0.1:1";
        await client.GetBalanceAsync(Ct);

        Assert.Equal($"Bearer {Token}", server.SingleRequest.Header("Authorization"));
    }

    [Fact]
    public async Task DoesNotModifyTheGivenHttpClient()
    {
        await using var server = TestServer.Start(Reply.Ok("{}"));
        using var httpClient = new HttpClient();
        TimeSpan timeout = httpClient.Timeout;

        await Client(httpClient, server.BaseUrl).GetBalanceAsync(Ct);

        Assert.Null(httpClient.BaseAddress);
        Assert.Empty(httpClient.DefaultRequestHeaders);
        Assert.Equal(timeout, httpClient.Timeout);
    }

    [Fact]
    public async Task DoesNotDisposeTheGivenHttpClient()
    {
        await using var server = TestServer.Start(Reply.Ok("{}"));
        using var httpClient = new HttpClient();

        await Client(httpClient, server.BaseUrl).GetBalanceAsync(Ct);
        await Client(httpClient, server.BaseUrl).GetBalanceAsync(Ct);

        Assert.Equal(2, server.Requests.Count);
    }

    [Fact]
    public void VersionMatchesAssembly()
    {
        Assembly assembly = typeof(TronzapClient).Assembly;
        string? informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        Assert.Equal(new Version(TronzapClient.Version + ".0"), assembly.GetName().Version);
        Assert.StartsWith(TronzapClient.Version, informational, StringComparison.Ordinal);
    }

    [Fact]
    public void ModelsAreImmutable()
    {
        foreach (Type type in typeof(TronzapClient).Assembly.GetExportedTypes())
        {
            if (type.Namespace is not ("Tronzap.Sdk.Models" or "Tronzap.Sdk.Responses" or "Tronzap.Sdk.Requests") || type.IsEnum)
            {
                continue;
            }

            Assert.True(type.IsSealed, $"{type.Name} is not sealed");
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                MethodInfo? setter = property.SetMethod;
                bool initOnly = setter is null
                    || Array.IndexOf(setter.ReturnParameter.GetRequiredCustomModifiers(), typeof(System.Runtime.CompilerServices.IsExternalInit)) >= 0;
                Assert.True(initOnly, $"{type.Name}.{property.Name} has a public setter");
            }
        }
    }

    [Fact]
    public void ResponseCollectionsDefaultToEmpty()
    {
        Assert.Empty(new Responses.ServiceRates().Energy);
        Assert.Empty(new Responses.ServiceRates().Bandwidth);
        Assert.Empty(new Responses.AddressInfo().Balances);
        Assert.Empty(new Responses.DirectRechargeInfo().Rates);
        Assert.Empty(new Responses.AmlCheck().RiskFactors);
        Assert.Empty(new Responses.AmlHistory().Items);
        Assert.Equal(new ResourceAmounts(), new TransactionParams().Amounts);
    }
}
