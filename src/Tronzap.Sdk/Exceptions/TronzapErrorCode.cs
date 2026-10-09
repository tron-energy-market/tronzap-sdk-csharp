namespace Tronzap.Sdk.Exceptions;

/// <summary>An error code the TronZap API reports in <see cref="TronzapApiException.Code"/>.</summary>
public enum TronzapErrorCode
{
    /// <summary>A code this SDK version does not recognise. The number is in <see cref="TronzapApiException.Code"/>.</summary>
    Unknown = 0,

    /// <summary>Authentication error: check the API token and that the signature is calculated correctly.</summary>
    AuthError = 1,

    /// <summary>Invalid service or parameters: check the service name and the parameters.</summary>
    InvalidServiceOrParams = 2,

    /// <summary>Wallet not found: verify the wallet address, or contact support if you believe this is an error.</summary>
    WalletNotFound = 5,

    /// <summary>Insufficient funds: top up the account or request a smaller amount.</summary>
    InsufficientFunds = 6,

    /// <summary>
    /// Invalid TRON address, or the address already has an active subscription. A TRON address is 34 characters long.
    /// </summary>
    InvalidTronAddress = 10,

    /// <summary>Invalid energy amount.</summary>
    InvalidEnergyAmount = 11,

    /// <summary>Invalid duration.</summary>
    InvalidDuration = 12,

    /// <summary>
    /// Transaction or subscription not found: check the transaction ID or external ID. The API reports this code under
    /// the key <c>subscription_not_found</c>.
    /// </summary>
    TransactionNotFound = 20,

    /// <summary>Cannot stop subscription, for example because it has a transactions limit.</summary>
    CannotStopSubscription = 21,

    /// <summary>Address not activated: activate it first with an address activation transaction.</summary>
    AddressNotActivated = 24,

    /// <summary>Address already activated. No action is needed.</summary>
    AddressAlreadyActivated = 25,

    /// <summary>AML check not found: check the ID or run the AML check again.</summary>
    AmlCheckNotFound = 30,

    /// <summary>The service is temporarily unavailable.</summary>
    ServiceNotAvailable = 35,

    /// <summary>Invalid bandwidth amount.</summary>
    InvalidBandwidthAmount = 50,

    /// <summary>Internal server error. Contact support if it persists.</summary>
    InternalServerError = 500,
}
