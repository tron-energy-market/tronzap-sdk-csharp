using System.Collections.Generic;
using System.Text.Json;
using Tronzap.Sdk.Models;
using Tronzap.Sdk.Responses;
using static Tronzap.Sdk.Internal.JsonValues;

namespace Tronzap.Sdk.Internal;

internal static class ResultMapper
{
    public static AccountBalance Balance(JsonElement result)
    {
        JsonElement o = Object(result, "result");
        return new AccountBalance { Balance = Decimal(o, "balance"), Address = Text(o, "address") };
    }

    public static ServiceRates Services(JsonElement result)
    {
        JsonElement o = Object(result, "result");
        JsonElement activation = Field(o, "activate_address");
        return new ServiceRates
        {
            Energy = List(o, "energy", EnergyRate),
            Bandwidth = List(o, "bandwidth", BandwidthRate),
            ActivateAddress = IsAbsent(activation)
                ? null
                : new ActivateAddressRate { Price = Decimal(Object(activation, "activate_address"), "price") },
        };
    }

    public static AddressInfo AddressInfo(JsonElement result)
    {
        JsonElement o = Object(result, "result");
        JsonElement resources = Object(Field(o, "resources"), "resources");
        return new AddressInfo
        {
            Resources = new AddressResources { Energy = Int64(resources, "energy"), Bandwidth = Int64(resources, "bandwidth") },
            Balances = DecimalMap(o, "balances"),
        };
    }

    public static EnergyEstimate EnergyEstimate(JsonElement result)
    {
        JsonElement o = Object(result, "result");
        long amount = Int64(o, "amount");
#pragma warning disable CS0618
        return new EnergyEstimate
        {
            Amount = amount,
            Energy = amount,
            Duration = Int32(o, "duration"),
            Price = Decimal(o, "price"),
            ActivationFee = Decimal(o, "activation_fee"),
            Total = Decimal(o, "total"),
            FromAddress = Text(o, "from_address"),
            ToAddress = Text(o, "to_address"),
            ContractAddress = Text(o, "contract_address"),
        };
#pragma warning restore CS0618
    }

    public static Calculation Calculation(JsonElement result)
    {
        JsonElement o = Object(result, "result");
        long amount = Int64(o, "amount");
#pragma warning disable CS0618
        return new Calculation
        {
            Address = Text(o, "address"),
            Service = WireValues.ParseService(Text(o, "type")),
            Amount = amount,
            Energy = amount,
            Duration = Int32(o, "duration"),
            Price = Decimal(o, "price"),
            ActivationFee = Decimal(o, "activation_fee"),
            Total = Decimal(o, "total"),
        };
#pragma warning restore CS0618
    }

    public static Transaction Transaction(JsonElement result)
    {
        JsonElement o = Object(result, "result");
        ServiceType service = WireValues.ParseService(Text(o, "service"));
        return new Transaction
        {
            Id = Text(o, "id"),
            ExternalId = OptionalText(o, "external_id"),
            Service = service,
            Params = TransactionParams(Object(Field(o, "params"), "params"), service),
            Status = WireValues.ParseTransactionStatus(Text(o, "status")),
            Amount = Decimal(o, "amount"),
            CreatedAt = Timestamp(o, "created_at"),
            Hash = OptionalText(o, "hash"),
        };
    }

    public static DirectRechargeInfo DirectRechargeInfo(JsonElement result)
    {
        JsonElement o = Object(result, "result");
        return new DirectRechargeInfo { Address = Text(o, "address"), Rates = List(o, "rates", DirectRechargeRate) };
    }

    public static IReadOnlyList<AmlService> AmlServices(JsonElement result) => Elements(result, "result", AmlService);

    public static AmlCheck AmlCheck(JsonElement element)
    {
        JsonElement o = Object(element, "AML check");
        string? direction = OptionalText(o, "direction");
        string? riskLevel = OptionalText(o, "risk_level");
        return new AmlCheck
        {
            Id = Text(o, "id"),
            Type = WireValues.ParseAmlCheckType(Text(o, "type")),
            Address = Text(o, "address"),
            Hash = OptionalText(o, "hash"),
            Direction = direction is null ? null : WireValues.ParseAmlDirection(direction),
            Network = Text(o, "network"),
            Status = WireValues.ParseAmlStatus(Text(o, "status")),
            RiskScore = OptionalDecimal(o, "risk_score"),
            RiskLevel = riskLevel is null ? null : WireValues.ParseAmlRiskLevel(riskLevel),
            Blacklist = Bool(o, "blacklist"),
            RiskFactors = List(o, "risk_factors", AmlRiskFactor),
            CheckedAt = Timestamp(o, "checked_at"),
        };
    }

    public static AmlHistory AmlHistory(JsonElement result)
    {
        JsonElement o = Object(result, "result");
        return new AmlHistory
        {
            Page = Int32(o, "page"),
            PerPage = Int32(o, "per_page"),
            Total = Int32(o, "total"),
            Items = List(o, "items", AmlCheck),
        };
    }

    // Older transactions carry params.amount / params.energy_amount instead of params.amounts.
    private static TransactionParams TransactionParams(JsonElement o, ServiceType service)
    {
        JsonElement amounts = Object(Field(o, "amounts"), "amounts");
        long energy = Int64(amounts, "energy");
        long bandwidth = Int64(amounts, "bandwidth");
        if (energy == 0 && bandwidth == 0)
        {
            energy = Int64(o, "energy_amount");
            long amount = Int64(o, "amount");
            if (service == ServiceType.Bandwidth)
            {
                bandwidth = amount;
            }
            else if (energy == 0)
            {
                energy = amount;
            }
        }

        return new TransactionParams
        {
            Address = Text(o, "address"),
            Duration = Int32(o, "duration"),
            Amounts = new ResourceAmounts { Energy = energy, Bandwidth = bandwidth },
            ActivateAddress = Bool(o, "activate_address"),
        };
    }

    private static EnergyRate EnergyRate(JsonElement element)
    {
        JsonElement o = Object(element, "energy rate");
        long minAmount = Int64(o, "min_amount");
        long maxAmount = Int64(o, "max_amount");
#pragma warning disable CS0618
        return new EnergyRate
        {
            Duration = Int32(o, "duration"),
            MinAmount = minAmount,
            MaxAmount = maxAmount,
            MinEnergy = minAmount,
            MaxEnergy = maxAmount,
            Price = Decimal(o, "price"),
            Price32K = Decimal(o, "price_32k"),
            Price65K = Decimal(o, "price_65k"),
            Price131K = Decimal(o, "price_131k"),
        };
#pragma warning restore CS0618
    }

    private static BandwidthRate BandwidthRate(JsonElement element)
    {
        JsonElement o = Object(element, "bandwidth rate");
        return new BandwidthRate
        {
            Duration = Int32(o, "duration"),
            MinAmount = Int64(o, "min_amount"),
            MaxAmount = Int64(o, "max_amount"),
            Price = Decimal(o, "price"),
        };
    }

    private static DirectRechargeRate DirectRechargeRate(JsonElement element)
    {
        JsonElement o = Object(element, "direct recharge rate");
        return new DirectRechargeRate
        {
            Duration = Int32(o, "duration"),
            MinEnergy = Int64(o, "min_energy"),
            MaxEnergy = Int64(o, "max_energy"),
            Price = Decimal(o, "price"),
            Price32K = Decimal(o, "price_32k"),
            Price65K = Decimal(o, "price_65k"),
            Price131K = Decimal(o, "price_131k"),
        };
    }

    private static AmlService AmlService(JsonElement element)
    {
        JsonElement o = Object(element, "AML service");
        return new AmlService
        {
            Id = Text(o, "id"),
            Type = WireValues.ParseAmlCheckType(Text(o, "type")),
            Price = Decimal(o, "price"),
        };
    }

    private static AmlRiskFactor AmlRiskFactor(JsonElement element)
    {
        JsonElement o = Object(element, "risk factor");
        return new AmlRiskFactor
        {
            Name = Text(o, "name"),
            Label = Text(o, "label"),
            Group = Text(o, "group"),
            Score = Decimal(o, "score"),
        };
    }
}
