using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tronzap.Sdk.Exceptions;
using Tronzap.Sdk.Internal;
using Tronzap.Sdk.Models;
using Tronzap.Sdk.Requests;
using Tronzap.Sdk.Responses;

namespace Tronzap.Sdk;

/// <summary>
/// Client for the <see href="https://docs.tronzap.com/">TronZap API</see>: buy TRON energy and bandwidth, activate
/// addresses and run AML checks.
/// </summary>
/// <remarks>
/// <para>
/// A client is immutable and safe for concurrent use. Create one per set of credentials and share it. It never
/// changes the <see cref="HttpClient"/> it is given: headers, base address and timeout are set per request.
/// </para>
/// <para>
/// A failure is reported as a <see cref="TronzapException"/>: <see cref="TronzapApiException"/> when the API rejected
/// the request, <see cref="TronzapHttpException"/> for a non-2xx response without an API error code,
/// <see cref="TronzapInvalidResponseException"/> for a response the SDK cannot read, and
/// <see cref="TronzapNetworkException"/> when no response arrived. Cancellation through the
/// <see cref="CancellationToken"/> raises <see cref="OperationCanceledException"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var client = new TronzapClient(new TronzapClientOptions
/// {
///     ApiToken = apiToken,
///     ApiSecret = apiSecret,
/// });
///
/// Transaction tx = await client.CreateEnergyTransactionAsync(
///     new EnergyTransactionRequest { Address = "TRecipientAddress", Energy = 65000 },
///     cancellationToken);
/// </code>
/// </example>
public sealed class TronzapClient : ITronzapClient
{
    /// <summary>The SDK version, reported in the default <c>User-Agent</c> header.</summary>
    public const string Version = "1.0.0";

    /// <summary>The name of the <see cref="HttpClient"/> registered by <c>AddTronzap</c> with <c>IHttpClientFactory</c>.</summary>
    public const string HttpClientName = "Tronzap.Sdk";

    internal const int MaxResponseBytes = 8 * 1024 * 1024;

    private const string DefaultUserAgent = "tronzap-sdk-csharp/" + Version;

    private static readonly Lazy<HttpClient> SharedHttpClient = new(CreateSharedHttpClient, LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly HttpClient _httpClient;
    private readonly string _apiToken;
    private readonly byte[] _apiSecret;
    private readonly string _baseUrl;
    private readonly TimeSpan _timeout;
    private readonly string _userAgent;

    /// <summary>
    /// Creates a client that sends requests through an <see cref="HttpClient"/> shared by every client created this
    /// way, so creating many clients does not exhaust sockets.
    /// </summary>
    /// <param name="options">The credentials and settings.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The options are invalid.</exception>
    public TronzapClient(TronzapClientOptions options)
        : this(SharedHttpClient.Value, options)
    {
    }

    /// <summary>
    /// Creates a client that sends requests through the given <see cref="HttpClient"/>, for example one from
    /// <c>IHttpClientFactory</c> or one configured with a proxy. The client is used as given and never disposed.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="options">The credentials and settings.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The options are invalid.</exception>
    public TronzapClient(HttpClient httpClient, TronzapClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ApiToken))
        {
            throw new ArgumentException("ApiToken is required.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.ApiSecret))
        {
            throw new ArgumentException("ApiSecret is required.", nameof(options));
        }

        if (options.Timeout != Timeout.InfiniteTimeSpan
            && (options.Timeout <= TimeSpan.Zero || options.Timeout.TotalMilliseconds > int.MaxValue))
        {
            throw new ArgumentException($"Timeout must be positive and at most {int.MaxValue} ms, got {options.Timeout}.", nameof(options));
        }

        string userAgent = options.UserAgent ?? DefaultUserAgent;
        if (string.IsNullOrWhiteSpace(userAgent) || userAgent.AsSpan().IndexOfAny('\r', '\n') >= 0)
        {
            throw new ArgumentException("UserAgent must be non-blank and on a single line.", nameof(options));
        }

        _httpClient = httpClient;
        _apiToken = options.ApiToken;
        _apiSecret = Encoding.UTF8.GetBytes(options.ApiSecret);
        _baseUrl = NormalizeBaseUrl(options.BaseUrl);
        _timeout = options.Timeout;
        _userAgent = userAgent;
    }

    /// <inheritdoc/>
    public Task<ServiceRates> GetServicesAsync(CancellationToken cancellationToken = default) =>
        CallAsync("/v1/services", EmptyBody(), ResultMapper.Services, cancellationToken);

    /// <inheritdoc/>
    public Task<AccountBalance> GetBalanceAsync(CancellationToken cancellationToken = default) =>
        CallAsync("/v1/balance", EmptyBody(), ResultMapper.Balance, cancellationToken);

    /// <inheritdoc/>
    public Task<AddressInfo> GetAddressInfoAsync(string address, CancellationToken cancellationToken = default)
    {
        Guard.Required(address, nameof(address));
        byte[] body = Json(w => w.WriteString("address", address));
        return CallAsync("/v1/address-info", body, ResultMapper.AddressInfo, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<EnergyEstimate> EstimateEnergyAsync(EstimateEnergyRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        byte[] body = Json(w =>
        {
            w.WriteString("from_address", request.FromAddress);
            w.WriteString("to_address", request.ToAddress);
            if (request.ContractAddress is not null)
            {
                w.WriteString("contract_address", request.ContractAddress);
            }
        });
        return CallAsync("/v1/estimate-energy", body, ResultMapper.EnergyEstimate, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Calculation> CalculateAsync(CalculateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        byte[] body = Json(w =>
        {
            w.WriteString("address", request.Address);
            w.WriteNumber("amount", request.Energy);
            w.WriteNumber("duration", request.Duration);
        });
        return CallAsync("/v1/calculate", body, ResultMapper.Calculation, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Transaction> CreateEnergyTransactionAsync(EnergyTransactionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        return CreateTransactionAsync(ServiceType.Energy, request.ExternalId, w =>
        {
            w.WriteString("address", request.Address);
            w.WriteStartObject("amounts");
            w.WriteNumber("energy", request.Energy);
            w.WriteEndObject();
            w.WriteNumber("duration", request.Duration);
            if (request.ActivateAddress)
            {
                w.WriteBoolean("activate_address", true);
            }
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Transaction> CreateBandwidthTransactionAsync(BandwidthTransactionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        return CreateTransactionAsync(ServiceType.Bandwidth, request.ExternalId, w =>
        {
            w.WriteString("address", request.Address);
            w.WriteStartObject("amounts");
            w.WriteNumber("bandwidth", request.Bandwidth);
            w.WriteEndObject();
            w.WriteNumber("duration", 1);
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Transaction> CreateResourceBundleTransactionAsync(ResourceBundleTransactionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        return CreateTransactionAsync(ServiceType.ResourceBundle, request.ExternalId, w =>
        {
            w.WriteString("address", request.Address);
            w.WriteStartObject("amounts");
            w.WriteNumber("energy", request.Energy);
            w.WriteNumber("bandwidth", request.Bandwidth);
            w.WriteEndObject();
            w.WriteNumber("duration", request.Duration);
            if (request.ActivateAddress)
            {
                w.WriteBoolean("activate_address", true);
            }
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Transaction> CreateAddressActivationTransactionAsync(AddressActivationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        return CreateTransactionAsync(
            ServiceType.ActivateAddress,
            request.ExternalId,
            w => w.WriteString("address", request.Address),
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Transaction> CheckTransactionAsync(CheckTransactionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        byte[] body = Json(w =>
        {
            if (request.Id is not null)
            {
                w.WriteString("id", request.Id);
            }

            if (request.ExternalId is not null)
            {
                w.WriteString("external_id", request.ExternalId);
            }
        });
        return CallAsync("/v1/transaction/check", body, ResultMapper.Transaction, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<DirectRechargeInfo> GetDirectRechargeInfoAsync(CancellationToken cancellationToken = default) =>
        CallAsync("/v1/direct-recharge-info", EmptyBody(), ResultMapper.DirectRechargeInfo, cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<AmlService>> GetAmlServicesAsync(CancellationToken cancellationToken = default) =>
        CallAsync("/v1/aml-checks", EmptyBody(), ResultMapper.AmlServices, cancellationToken);

    /// <inheritdoc/>
    public Task<AmlCheck> CreateAmlCheckAsync(AmlCheckRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        byte[] body = Json(w =>
        {
            w.WriteString("type", request.Type.ToWire());
            w.WriteString("network", request.Network);
            w.WriteString("address", request.Address);
            if (request.Hash is not null)
            {
                w.WriteString("hash", request.Hash);
            }

            if (request.Direction is { } direction)
            {
                w.WriteString("direction", direction.ToWire());
            }
            else if (request.Type == AmlCheckType.Hash)
            {
                w.WriteString("direction", AmlDirection.Deposit.ToWire());
            }
        });
        return CallAsync("/v1/aml-checks/new", body, ResultMapper.AmlCheck, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<AmlCheck> CheckAmlStatusAsync(string id, CancellationToken cancellationToken = default)
    {
        Guard.Required(id, nameof(id));
        byte[] body = Json(w => w.WriteString("id", id));
        return CallAsync("/v1/aml-checks/check", body, ResultMapper.AmlCheck, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<AmlHistory> GetAmlHistoryAsync(CancellationToken cancellationToken = default) =>
        GetAmlHistoryAsync(new AmlHistoryRequest(), cancellationToken);

    /// <inheritdoc/>
    public Task<AmlHistory> GetAmlHistoryAsync(AmlHistoryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        byte[] body = Json(w =>
        {
            w.WriteNumber("page", request.Page);
            w.WriteNumber("per_page", request.PerPage);
            if (request.Status is { } status)
            {
                w.WriteString("status", status.ToWire());
            }
        });
        return CallAsync("/v1/aml-checks/history", body, ResultMapper.AmlHistory, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SubscriptionPlan>> GetSubscriptionsAsync(CancellationToken cancellationToken = default) =>
        CallAsync("/v1/subscriptions", EmptyBody(), ResultMapper.SubscriptionPlans, cancellationToken);

    /// <inheritdoc/>
    public Task<Subscription> StartSubscriptionAsync(StartSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        byte[] body = Json(w =>
        {
            w.WriteString("subscription_id", request.SubscriptionId);
            if (request.ExternalId is not null)
            {
                w.WriteString("external_id", request.ExternalId);
            }

            w.WriteStartObject("params");
            w.WriteString("address", request.Address);
            w.WriteNumber("duration", request.DurationDays);
            w.WriteNumber("transactions_limit", request.TransactionsLimit);
            if (request.ActivateAddress)
            {
                w.WriteBoolean("activate_address", true);
            }

            w.WriteEndObject();
        });
        return CallAsync("/v1/subscription/start", body, ResultMapper.Subscription, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Subscription> CheckSubscriptionAsync(SubscriptionRequest request, CancellationToken cancellationToken = default) =>
        SubscriptionByIdAsync("/v1/subscription/check", request, cancellationToken);

    /// <inheritdoc/>
    public Task<Subscription> StopSubscriptionAsync(SubscriptionRequest request, CancellationToken cancellationToken = default) =>
        SubscriptionByIdAsync("/v1/subscription/stop", request, cancellationToken);

    /// <inheritdoc/>
    public Task<SubscriptionHistory> GetSubscriptionHistoryAsync(CancellationToken cancellationToken = default) =>
        GetSubscriptionHistoryAsync(new SubscriptionHistoryRequest(), cancellationToken);

    /// <inheritdoc/>
    public Task<SubscriptionHistory> GetSubscriptionHistoryAsync(SubscriptionHistoryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        byte[] body = Json(w =>
        {
            w.WriteNumber("page", request.Page);
            w.WriteNumber("per_page", request.PerPage);
            if (request.Status is { } status)
            {
                w.WriteString("status", status.ToWire());
            }
        });
        return CallAsync("/v1/subscriptions/history", body, ResultMapper.SubscriptionHistory, cancellationToken);
    }

    internal static string NormalizeBaseUrl(string? baseUrl)
    {
        string normalized = baseUrl?.Trim() ?? "";
        if (normalized.Length == 0)
        {
            throw new ArgumentException("BaseUrl must not be blank.", nameof(baseUrl));
        }

        if (!normalized.Contains("://", StringComparison.Ordinal))
        {
            normalized = "https://" + normalized.TrimStart('/');
        }

        normalized = normalized.TrimEnd('/');
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out Uri? uri))
        {
            throw new ArgumentException($"BaseUrl is not a valid URL: {baseUrl}", nameof(baseUrl));
        }

        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
        {
            throw new ArgumentException($"BaseUrl must use http or https: {baseUrl}", nameof(baseUrl));
        }

        if (uri.Host.Length == 0)
        {
            throw new ArgumentException($"BaseUrl has no host: {baseUrl}", nameof(baseUrl));
        }

        if (uri.Query.Length > 0 || uri.Fragment.Length > 0)
        {
            throw new ArgumentException($"BaseUrl must not have a query or fragment: {baseUrl}", nameof(baseUrl));
        }

        return normalized;
    }

    internal string Sign(byte[] body)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(body);
        hash.AppendData(_apiSecret);
        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }

    private Task<Transaction> CreateTransactionAsync(
        ServiceType service,
        string? externalId,
        Action<Utf8JsonWriter> writeParams,
        CancellationToken cancellationToken)
    {
        byte[] body = Json(w =>
        {
            w.WriteString("service", service.ToWire());
            w.WriteStartObject("params");
            writeParams(w);
            w.WriteEndObject();
            if (externalId is not null)
            {
                w.WriteString("external_id", externalId);
            }
        });
        return CallAsync("/v1/transaction/new", body, ResultMapper.Transaction, cancellationToken);
    }

    private Task<Subscription> SubscriptionByIdAsync(string endpoint, SubscriptionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        byte[] body = Json(w =>
        {
            if (request.Id is not null)
            {
                w.WriteString("id", request.Id);
            }

            if (request.ExternalId is not null)
            {
                w.WriteString("external_id", request.ExternalId);
            }
        });
        return CallAsync(endpoint, body, ResultMapper.Subscription, cancellationToken);
    }

    private async Task<T> CallAsync<T>(string endpoint, byte[] body, Func<JsonElement, T> map, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + endpoint);
        request.Content = new ByteArrayContent(body);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);
        request.Headers.TryAddWithoutValidation("X-Signature", Sign(body));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("User-Agent", _userAgent);

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (_timeout != Timeout.InfiniteTimeSpan)
        {
            deadline.CancelAfter(_timeout);
        }

        HttpStatusCode status;
        TimeSpan? retryAfter;
        byte[] responseBody;
        try
        {
            using HttpResponseMessage response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token)
                .ConfigureAwait(false);
            status = response.StatusCode;
            retryAfter = RetryAfter(response.Headers.RetryAfter);
            responseBody = await ReadBodyAsync(response, deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException e) when (cancellationToken.IsCancellationRequested)
        {
            throw new TaskCanceledException("The request was canceled.", e, cancellationToken);
        }
        catch (OperationCanceledException e)
        {
            string limit = e.InnerException is TimeoutException
                ? "the HttpClient timeout"
                : $"{_timeout.TotalMilliseconds:0} ms";
            throw new TronzapTimeoutException($"The request timed out after {limit}.", e);
        }
        catch (HttpRequestException e)
        {
            throw NetworkErrors.Classify(e);
        }
        catch (IOException e)
        {
            throw NetworkErrors.Classify(e);
        }

        return ResponseDecoder.Decode(status, responseBody, retryAfter, map);
    }

    private static async Task<byte[]> ReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength > MaxResponseBytes)
        {
            throw TooLarge(response.StatusCode);
        }

        Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            using var buffer = new MemoryStream();
            byte[] chunk = ArrayPool<byte>.Shared.Rent(16 * 1024);
            try
            {
                int read;
                while ((read = await stream.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    if (buffer.Length + read > MaxResponseBytes)
                    {
                        throw TooLarge(response.StatusCode);
                    }

                    buffer.Write(chunk, 0, read);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(chunk);
            }

            return buffer.ToArray();
        }
    }

    private static TronzapInvalidResponseException TooLarge(HttpStatusCode status) =>
        new($"Response body exceeds {MaxResponseBytes} bytes.", status, "", null);

    private static TimeSpan? RetryAfter(RetryConditionHeaderValue? header)
    {
        if (header?.Delta is { } delta)
        {
            return delta;
        }

        if (header?.Date is { } date)
        {
            TimeSpan wait = date - DateTimeOffset.UtcNow;
            return wait > TimeSpan.Zero ? wait : TimeSpan.Zero;
        }

        return null;
    }

    private static byte[] EmptyBody() => Json(_ => { });

    private static byte[] Json(Action<Utf8JsonWriter> writeProperties)
    {
        var buffer = new ArrayBufferWriter<byte>(256);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writeProperties(writer);
            writer.WriteEndObject();
        }

        return buffer.WrittenSpan.ToArray();
    }

    private static HttpClient CreateSharedHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectTimeout = TimeSpan.FromSeconds(10),
            AutomaticDecompression = DecompressionMethods.All,
        };
        return new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
    }
}
