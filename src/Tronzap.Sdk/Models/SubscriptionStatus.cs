namespace Tronzap.Sdk.Models;

/// <summary>The state of a subscription.</summary>
public enum SubscriptionStatus
{
    /// <summary>A value this SDK version does not recognise.</summary>
    Unknown = 0,

    /// <summary>The subscription was created but has not started yet.</summary>
    New,

    /// <summary>The subscription is being started.</summary>
    Pending,

    /// <summary>The subscription could not be started.</summary>
    Error,

    /// <summary>The subscription is supplying the address with energy.</summary>
    Active,

    /// <summary>The subscription was stopped.</summary>
    Stopped,

    /// <summary>The subscription ran out of days or transactions.</summary>
    Expired,
}
