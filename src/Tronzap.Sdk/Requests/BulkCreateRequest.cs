using Tronzap.Sdk.Internal;
using Tronzap.Sdk.Responses;

namespace Tronzap.Sdk.Requests;

/// <summary>A bulk order to start from a calculation.</summary>
public sealed record BulkCreateRequest
{
    /// <summary>The <see cref="BulkCalculation.Id"/> of the calculation to start.</summary>
    public required string Id { get; init; }

    /// <summary>
    /// Your own ID for the order, unique per account. Starting an order again with the same external ID returns the
    /// order already started.
    /// </summary>
    public string? ExternalId { get; init; }

    /// <summary>
    /// Whether to buy even if the price rises while the order runs, within <see cref="MaxPriceIncrease"/>. Defaults
    /// to <see langword="false"/>: an address whose price rose is skipped.
    /// </summary>
    public bool Force { get; init; }

    /// <summary>
    /// The allowed price increase in percent, 0 to 100, used only with <see cref="Force"/>. The API default of 20
    /// applies when it is <see langword="null"/>.
    /// </summary>
    public int? MaxPriceIncrease { get; init; }

    /// <summary>
    /// A public <c>https</c> URL called once when the order becomes final. Check its <c>X-Signature</c> header with
    /// <see cref="TronzapClient.VerifyWebhookSignature(byte[], string?)"/>.
    /// </summary>
    public string? WebhookUrl { get; init; }

    internal void Validate()
    {
        Guard.Required(Id, nameof(Id));
        Guard.Optional(ExternalId, nameof(ExternalId));
        Guard.Optional(WebhookUrl, nameof(WebhookUrl));
    }
}
