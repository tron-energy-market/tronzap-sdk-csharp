using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using static Tronzap.Sdk.Tests.TestSupport;

namespace Tronzap.Sdk.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public async Task ResolvesClientBackedByHttpClientFactory()
    {
        await using var server = TestServer.Start(Reply.Ok("""{"balance":"2.5","address":"T"}"""));
        var counter = new CountingHandler();
        var services = new ServiceCollection();
        services.AddTronzap(o =>
            {
                o.ApiToken = Token;
                o.ApiSecret = Secret;
                o.BaseUrl = server.BaseUrl;
            })
            .AddHttpMessageHandler(() => counter);

        await using ServiceProvider provider = services.BuildServiceProvider();
        ITronzapClient client = provider.GetRequiredService<ITronzapClient>();
        var balance = await client.GetBalanceAsync(Ct);

        Assert.Equal(2.5m, balance.Balance);
        Assert.Equal(1, counter.Calls);
        Assert.IsType<TronzapClient>(client);
        Assert.NotNull(provider.GetRequiredService<TronzapClient>());
        Assert.Equal(ExpectedSignature(server.SingleRequest.Body), server.SingleRequest.Header("X-Signature"));
    }

    [Fact]
    public async Task ReadsOptionsConfiguredSeparately()
    {
        await using var server = TestServer.Start(Reply.Ok("{}"));
        var services = new ServiceCollection();
        services.Configure<TronzapClientOptions>(o =>
        {
            o.ApiToken = "configured-token";
            o.ApiSecret = Secret;
            o.BaseUrl = server.BaseUrl;
        });
        services.AddTronzap();

        await using ServiceProvider provider = services.BuildServiceProvider();
        await provider.GetRequiredService<ITronzapClient>().GetBalanceAsync(Ct);

        Assert.Equal("Bearer configured-token", server.SingleRequest.Header("Authorization"));
    }

    [Fact]
    public async Task MissingCredentialsFailOnResolve()
    {
        var services = new ServiceCollection();
        services.AddTronzap();

        await using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Throws<ArgumentException>(() => provider.GetRequiredService<ITronzapClient>());
    }

    [Fact]
    public void RegistersNamedHttpClient()
    {
        var services = new ServiceCollection();
        IHttpClientBuilder builder = services.AddTronzap(o => o.ApiToken = Token);

        Assert.Equal(TronzapClient.HttpClientName, builder.Name);
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Equal(Token, provider.GetRequiredService<IOptions<TronzapClientOptions>>().Value.ApiToken);
    }

    [Fact]
    public void RejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddTronzap());
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddTronzap(null!));
    }

    private sealed class CountingHandler : DelegatingHandler
    {
        private int _calls;

        public int Calls => _calls;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            return base.SendAsync(request, cancellationToken);
        }
    }
}
