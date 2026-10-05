using System;

namespace Tronzap.Sdk;

/// <summary>
/// Settings for a <see cref="TronzapClient"/>. The client copies them when it is created, so changing an options
/// instance afterwards does not affect clients that already exist.
/// </summary>
public sealed class TronzapClientOptions
{
    /// <summary>The production API endpoint.</summary>
    public const string DefaultBaseUrl = "https://api.tronzap.com";

    /// <summary>The API token from your TronZap dashboard, sent as a bearer token. Required.</summary>
    public string ApiToken { get; set; } = "";

    /// <summary>The API secret from your TronZap dashboard, used to sign every request body. It is never sent. Required.</summary>
    public string ApiSecret { get; set; } = "";

    /// <summary>
    /// The API endpoint. Defaults to <see cref="DefaultBaseUrl"/>. A bare host such as <c>api.tronzap.com</c> gets the
    /// <c>https</c> scheme and a trailing slash is removed. Give an explicit scheme to opt out, for example
    /// <c>http://localhost:8080</c> against a local server.
    /// </summary>
    public string BaseUrl { get; set; } = DefaultBaseUrl;

    /// <summary>
    /// The deadline for a whole request, from sending it to reading the last byte of the response. Defaults to 30
    /// seconds. It applies on top of <see cref="System.Net.Http.HttpClient.Timeout"/>; the shorter one wins.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>The <c>User-Agent</c> header, or <see langword="null"/> for <c>tronzap-sdk-csharp/{version}</c>.</summary>
    public string? UserAgent { get; set; }
}
