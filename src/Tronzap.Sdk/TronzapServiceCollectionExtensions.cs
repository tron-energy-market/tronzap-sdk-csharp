using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Tronzap.Sdk;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers <see cref="TronzapClient"/> with a dependency injection container.</summary>
public static class TronzapServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ITronzapClient"/> and <see cref="TronzapClient"/>, backed by a named
    /// <see cref="HttpClient"/> from <c>IHttpClientFactory</c>, and configures <see cref="TronzapClientOptions"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Sets the credentials and settings.</param>
    /// <returns>The builder of the underlying HTTP client, to add handlers, a proxy or resilience policies.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static IHttpClientBuilder AddTronzap(this IServiceCollection services, Action<TronzapClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        services.Configure(configure);
        return services.AddTronzap();
    }

    /// <summary>
    /// Registers <see cref="ITronzapClient"/> and <see cref="TronzapClient"/>, backed by a named
    /// <see cref="HttpClient"/> from <c>IHttpClientFactory</c>. Configure <see cref="TronzapClientOptions"/>
    /// separately, for example with <c>services.Configure&lt;TronzapClientOptions&gt;(configuration.GetSection("Tronzap"))</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The builder of the underlying HTTP client, to add handlers, a proxy or resilience policies.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IHttpClientBuilder AddTronzap(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOptions<TronzapClientOptions>();
        IHttpClientBuilder builder = services.AddHttpClient(TronzapClient.HttpClientName);
        services.TryAddTransient(provider => new TronzapClient(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient(TronzapClient.HttpClientName),
            provider.GetRequiredService<IOptions<TronzapClientOptions>>().Value));
        services.TryAddTransient<ITronzapClient>(provider => provider.GetRequiredService<TronzapClient>());
        return builder;
    }
}
