using System;
using Tronzap.Sdk.Models;

namespace Tronzap.Sdk.Internal;

internal static class WireValues
{
    public static string ToWire(this ServiceType value) => value switch
    {
        ServiceType.Energy => "energy",
        ServiceType.Bandwidth => "bandwidth",
        ServiceType.ResourceBundle => "resource_bundle",
        ServiceType.ActivateAddress => "activate_address",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    public static string ToWire(this AmlCheckType value) => value switch
    {
        AmlCheckType.Address => "address",
        AmlCheckType.Hash => "hash",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    public static string ToWire(this AmlDirection value) => value switch
    {
        AmlDirection.Deposit => "deposit",
        AmlDirection.Withdrawal => "withdrawal",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    public static string ToWire(this AmlStatus value) => value switch
    {
        AmlStatus.Pending => "pending",
        AmlStatus.Processing => "processing",
        AmlStatus.Completed => "completed",
        AmlStatus.Failed => "failed",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    public static ServiceType ParseService(string value) => value switch
    {
        "energy" => ServiceType.Energy,
        "bandwidth" => ServiceType.Bandwidth,
        "resource_bundle" => ServiceType.ResourceBundle,
        "activate_address" => ServiceType.ActivateAddress,
        _ => ServiceType.Unknown,
    };

    public static TransactionStatus ParseTransactionStatus(string value) => value switch
    {
        "new" => TransactionStatus.New,
        "pending" => TransactionStatus.Pending,
        "success" => TransactionStatus.Success,
        "failed" => TransactionStatus.Failed,
        _ => TransactionStatus.Unknown,
    };

    public static AmlCheckType ParseAmlCheckType(string value) => value switch
    {
        "address" => AmlCheckType.Address,
        "hash" => AmlCheckType.Hash,
        _ => AmlCheckType.Unknown,
    };

    public static AmlDirection ParseAmlDirection(string value) => value switch
    {
        "deposit" => AmlDirection.Deposit,
        "withdrawal" => AmlDirection.Withdrawal,
        _ => AmlDirection.Unknown,
    };

    public static AmlStatus ParseAmlStatus(string value) => value switch
    {
        "pending" => AmlStatus.Pending,
        "processing" => AmlStatus.Processing,
        "completed" => AmlStatus.Completed,
        "failed" => AmlStatus.Failed,
        _ => AmlStatus.Unknown,
    };

    public static AmlRiskLevel ParseAmlRiskLevel(string value) => value switch
    {
        "low" => AmlRiskLevel.Low,
        "medium" => AmlRiskLevel.Medium,
        "high" => AmlRiskLevel.High,
        _ => AmlRiskLevel.Unknown,
    };
}
