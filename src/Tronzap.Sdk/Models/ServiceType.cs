namespace Tronzap.Sdk.Models;

/// <summary>A TronZap service a transaction is created for.</summary>
public enum ServiceType
{
    /// <summary>A value this SDK version does not recognise.</summary>
    Unknown = 0,

    /// <summary>Energy rental (<c>energy</c>).</summary>
    Energy,

    /// <summary>Bandwidth rental (<c>bandwidth</c>).</summary>
    Bandwidth,

    /// <summary>Energy and bandwidth in one purchase (<c>resource_bundle</c>).</summary>
    ResourceBundle,

    /// <summary>TRON address activation (<c>activate_address</c>).</summary>
    ActivateAddress,
}
