using System;
using System.Threading.Tasks;
using Tronzap.Sdk.Models;
using Tronzap.Sdk.Requests;
using Tronzap.Sdk.Responses;
using Xunit;
using static Tronzap.Sdk.Tests.TestSupport;

namespace Tronzap.Sdk.Tests;

public sealed class ResponseMappingTests
{
    private static async Task<T> Call<T>(string result, Func<TronzapClient, Task<T>> call)
    {
        await using var server = TestServer.Start(Reply.Ok(result));
        return await call(Client(server));
    }

    [Fact]
    public async Task MapsBalance()
    {
        AccountBalance balance = await Call("""{"balance":"12.345678","address":"TDeposit"}""", c => c.GetBalanceAsync(Ct));

        Assert.Equal(12.345678m, balance.Balance);
        Assert.Equal("TDeposit", balance.Address);
    }

    [Fact]
    public async Task MapsServices()
    {
        ServiceRates services = await Call(
            """
            {
              "energy":[{"duration":1,"min_amount":32000,"max_amount":5000000,"min_energy":32000,"max_energy":5000000,
                         "price":"0.0841","price_32k":2.69,"price_65k":"5.4665","price_131k":11.02}],
              "bandwidth":[{"duration":1,"min_amount":300,"max_amount":100000,"price":1}],
              "activate_address":{"price":"1.4"}
            }
            """,
            c => c.GetServicesAsync(Ct));

        EnergyRate energy = Assert.Single(services.Energy);
        Assert.Equal(new EnergyRate
        {
            Duration = 1,
            MinAmount = 32000,
            MaxAmount = 5000000,
#pragma warning disable CS0618
            MinEnergy = 32000,
            MaxEnergy = 5000000,
#pragma warning restore CS0618
            Price = 0.0841m,
            Price32K = 2.69m,
            Price65K = 5.4665m,
            Price131K = 11.02m,
        }, energy);
        Assert.Equal(new BandwidthRate { Duration = 1, MinAmount = 300, MaxAmount = 100000, Price = 1m }, Assert.Single(services.Bandwidth));
        Assert.Equal(1.4m, services.ActivateAddress!.Price);
    }

    [Theory]
    [InlineData("""{"energy":[{"duration":1,"min_amount":32000,"max_amount":5000000,"price":"0.0841"}]}""")]
    [InlineData("""{"energy":[{"duration":1,"min_amount":32000,"max_amount":5000000,"min_energy":1,"max_energy":2,"price":"0.0841"}]}""")]
    public async Task DeprecatedEnergyRangeComesFromAmounts(string result)
    {
        EnergyRate energy = Assert.Single((await Call(result, c => c.GetServicesAsync(Ct))).Energy);

#pragma warning disable CS0618
        Assert.Equal(32000, energy.MinEnergy);
        Assert.Equal(5000000, energy.MaxEnergy);
#pragma warning restore CS0618
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"energy":null,"bandwidth":[],"activate_address":null}""")]
    [InlineData("""{"energy":{},"bandwidth":{}}""")]
    public async Task MissingCollectionsAreEmptyNotNull(string result)
    {
        ServiceRates services = await Call(result, c => c.GetServicesAsync(Ct));

        Assert.Empty(services.Energy);
        Assert.Empty(services.Bandwidth);
        Assert.Null(services.ActivateAddress);
    }

    [Fact]
    public async Task MapsAddressInfo()
    {
        AddressInfo info = await Call(
            """{"resources":{"energy":65000,"bandwidth":"345"},"balances":{"TRX":"10.5","USDT":3}}""",
            c => c.GetAddressInfoAsync(Address, Ct));

        Assert.Equal(new AddressResources { Energy = 65000, Bandwidth = 345 }, info.Resources);
        Assert.Equal(10.5m, info.Balances["TRX"]);
        Assert.Equal(3m, info.Balances["USDT"]);
    }

    [Fact]
    public async Task ReadsPhpEmptyArrayAsEmptyObject()
    {
        AddressInfo info = await Call("""{"resources":[],"balances":[]}""", c => c.GetAddressInfoAsync(Address, Ct));

        Assert.Equal(0, info.Resources.Energy);
        Assert.Empty(info.Balances);
    }

    [Fact]
    public async Task MapsEnergyEstimate()
    {
        EnergyEstimate estimate = await Call(
            """
            {"amount":65000,"energy":65000,"duration":1,"price":"5.47","activation_fee":0,"total":"5.47",
             "from_address":"TFrom","to_address":"TTo","contract_address":"TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t"}
            """,
            c => c.EstimateEnergyAsync(new() { FromAddress = "TFrom", ToAddress = "TTo" }, Ct));

        Assert.Equal(new EnergyEstimate
        {
            Amount = 65000,
#pragma warning disable CS0618
            Energy = 65000,
#pragma warning restore CS0618
            Duration = 1,
            Price = 5.47m,
            ActivationFee = 0m,
            Total = 5.47m,
            FromAddress = "TFrom",
            ToAddress = "TTo",
            ContractAddress = EstimateEnergyRequest.UsdtContractAddress,
        }, estimate);
    }

    [Fact]
    public async Task MapsCalculation()
    {
        Calculation calculation = await Call(
            """{"address":"T","type":"energy","amount":65000,"energy":65000,"duration":24,"price":5.47,"activation_fee":"1.4","total":6.87}""",
            c => c.CalculateAsync(new() { Address = Address, Energy = 65000 }, Ct));

        Assert.Equal(ServiceType.Energy, calculation.Service);
        Assert.Equal(65000, calculation.Amount);
        Assert.Equal(24, calculation.Duration);
        Assert.Equal(1.4m, calculation.ActivationFee);
        Assert.Equal(6.87m, calculation.Total);
    }

    [Theory]
    [InlineData("""{"address":"T","type":"energy","amount":65000,"duration":1,"price":5.47,"total":5.47}""")]
    [InlineData("""{"address":"T","type":"energy","amount":65000,"energy":64285,"duration":1,"price":5.47,"total":5.47}""")]
    public async Task DeprecatedCalculationEnergyComesFromAmount(string result)
    {
        Calculation calculation = await Call(result, c => c.CalculateAsync(new() { Address = Address, Energy = 65000 }, Ct));

#pragma warning disable CS0618
        Assert.Equal(65000, calculation.Energy);
#pragma warning restore CS0618
    }

    [Theory]
    [InlineData("""{"amount":65000,"duration":1,"price":"5.47","total":"5.47"}""")]
    [InlineData("""{"amount":65000,"energy":64285,"duration":1,"price":"5.47","total":"5.47"}""")]
    public async Task DeprecatedEstimateEnergyComesFromAmount(string result)
    {
        EnergyEstimate estimate = await Call(result, c => c.EstimateEnergyAsync(new() { FromAddress = "TFrom", ToAddress = "TTo" }, Ct));

#pragma warning disable CS0618
        Assert.Equal(65000, estimate.Energy);
#pragma warning restore CS0618
    }

    [Theory]
    [InlineData("5.47")]
    [InlineData("\"5.47\"")]
    [InlineData("\" 5.47 \"")]
    public async Task ReadsAmountAsNumberOrString(string amount)
    {
        Transaction tx = await Call(
            $$"""{"id":"tx-1","service":"energy","params":{},"status":"success","amount":{{amount}}}""",
            c => c.CheckTransactionAsync(CheckTransactionRequest.ById("tx-1"), Ct));

        Assert.Equal(5.47m, tx.Amount);
    }

    [Fact]
    public async Task MapsTransaction()
    {
        Transaction tx = await Call(
            """
            {"id":"tx-1","external_id":"order-42","service":"energy",
             "params":{"address":"TAddr","amounts":{"energy":65000,"bandwidth":345},"duration":1,"activate_address":true},
             "status":"success","amount":"5.815","created_at":"2026-08-10T12:30:00+00:00","hash":"abc123"}
            """,
            c => c.CheckTransactionAsync(CheckTransactionRequest.ById("tx-1"), Ct));

        Assert.Equal("tx-1", tx.Id);
        Assert.Equal("order-42", tx.ExternalId);
        Assert.Equal(ServiceType.Energy, tx.Service);
        Assert.Equal(new TransactionParams
        {
            Address = "TAddr",
            Duration = 1,
            Amounts = new ResourceAmounts { Energy = 65000, Bandwidth = 345 },
            ActivateAddress = true,
        }, tx.Params);
        Assert.Equal(TransactionStatus.Success, tx.Status);
        Assert.Equal(5.815m, tx.Amount);
        Assert.Equal("2026-08-10T12:30:00+00:00", tx.CreatedAt!.Raw);
        Assert.Equal(new DateTimeOffset(2026, 8, 10, 12, 30, 0, TimeSpan.Zero), tx.CreatedAt.Value);
        Assert.Equal("abc123", tx.Hash);
    }

    [Fact]
    public async Task MissingOptionalTransactionFieldsAreNull()
    {
        Transaction tx = await Call(
            """{"id":"tx-1","external_id":"","service":"activate_address","params":{"address":"T"},"status":"new","amount":1.4,"hash":null}""",
            c => c.CheckTransactionAsync(CheckTransactionRequest.ById("tx-1"), Ct));

        Assert.Null(tx.ExternalId);
        Assert.Null(tx.Hash);
        Assert.Null(tx.CreatedAt);
        Assert.Equal(ServiceType.ActivateAddress, tx.Service);
        Assert.Equal(new ResourceAmounts(), tx.Params.Amounts);
    }

    [Theory]
    [InlineData("energy", """{"energy_amount":65000}""", 65000, 0)]
    [InlineData("energy", """{"amount":65000}""", 65000, 0)]
    [InlineData("bandwidth", """{"amount":345}""", 0, 345)]
    public async Task ReadsLegacyTransactionAmounts(string service, string paramsJson, long energy, long bandwidth)
    {
        Transaction tx = await Call(
            $$"""{"id":"tx-1","service":"{{service}}","params":{{paramsJson}},"status":"new","amount":1}""",
            c => c.CheckTransactionAsync(CheckTransactionRequest.ById("tx-1"), Ct));

        Assert.Equal(new ResourceAmounts { Energy = energy, Bandwidth = bandwidth }, tx.Params.Amounts);
    }

    [Theory]
    [InlineData("new", TransactionStatus.New)]
    [InlineData("pending", TransactionStatus.Pending)]
    [InlineData("success", TransactionStatus.Success)]
    [InlineData("failed", TransactionStatus.Failed)]
    [InlineData("refunded", TransactionStatus.Unknown)]
    [InlineData("", TransactionStatus.Unknown)]
    public async Task MapsTransactionStatus(string wire, TransactionStatus expected)
    {
        Transaction tx = await Call(
            $$"""{"id":"tx-1","service":"something_new","params":{},"status":"{{wire}}","amount":0}""",
            c => c.CheckTransactionAsync(CheckTransactionRequest.ById("tx-1"), Ct));

        Assert.Equal(expected, tx.Status);
        Assert.Equal(ServiceType.Unknown, tx.Service);
    }

    [Fact]
    public async Task MapsDirectRechargeInfo()
    {
        DirectRechargeInfo info = await Call(
            """{"address":"TPay","rates":[{"duration":1,"min_energy":32000,"max_energy":200000,"price":"0.08","price_32k":"2.56","price_65k":"5.2","price_131k":"10.48"}]}""",
            c => c.GetDirectRechargeInfoAsync(Ct));

        Assert.Equal("TPay", info.Address);
        DirectRechargeRate rate = Assert.Single(info.Rates);
        Assert.Equal(200000, rate.MaxEnergy);
        Assert.Equal(5.2m, rate.Price65K);
    }

    [Fact]
    public async Task MapsAmlServices()
    {
        var services = await Call(
            """[{"id":"aml-address","type":"address","price":"1.5"},{"id":"aml-hash","type":"hash","price":2}]""",
            c => c.GetAmlServicesAsync(Ct));

        Assert.Equal(2, services.Count);
        Assert.Equal(new AmlService { Id = "aml-address", Type = AmlCheckType.Address, Price = 1.5m }, services[0]);
        Assert.Equal(AmlCheckType.Hash, services[1].Type);
    }

    [Fact]
    public async Task MapsCompletedAmlCheck()
    {
        AmlCheck check = await Call(
            """
            {"id":"aml-1","type":"hash","address":"bc1x","hash":"h1","direction":"deposit","network":"BTC","status":"completed",
             "risk_score":"35.3","risk_level":"medium","blacklist":false,
             "risk_factors":[{"name":"exchange","label":"Exchange","group":"trusted","score":"20.1"}],
             "checked_at":"2026-08-10 12:30:00"}
            """,
            c => c.CheckAmlStatusAsync("aml-1", Ct));

        Assert.Equal(AmlCheckType.Hash, check.Type);
        Assert.Equal("h1", check.Hash);
        Assert.Equal(AmlDirection.Deposit, check.Direction);
        Assert.Equal(AmlStatus.Completed, check.Status);
        Assert.Equal(35.3m, check.RiskScore);
        Assert.Equal(AmlRiskLevel.Medium, check.RiskLevel);
        Assert.False(check.Blacklist);
        Assert.Equal(new AmlRiskFactor { Name = "exchange", Label = "Exchange", Group = "trusted", Score = 20.1m }, Assert.Single(check.RiskFactors));
        Assert.Equal(new DateTimeOffset(2026, 8, 10, 12, 30, 0, TimeSpan.Zero), check.CheckedAt!.Value);
    }

    [Fact]
    public async Task PendingAmlCheckHasNoScore()
    {
        AmlCheck check = await Call(
            """{"id":"aml-1","type":"address","address":"T","network":"TRX","status":"pending","risk_score":null,"risk_level":null,"risk_factors":null}""",
            c => c.CheckAmlStatusAsync("aml-1", Ct));

        Assert.Null(check.RiskScore);
        Assert.Null(check.RiskLevel);
        Assert.Null(check.Direction);
        Assert.Null(check.CheckedAt);
        Assert.Empty(check.RiskFactors);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("\"0\"")]
    [InlineData("\"0.0\"")]
    public async Task ZeroRiskScoreIsNotMissing(string score)
    {
        AmlCheck check = await Call(
            $$"""{"id":"aml-1","status":"completed","risk_score":{{score}}}""",
            c => c.CheckAmlStatusAsync("aml-1", Ct));

        Assert.Equal(0m, check.RiskScore);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("1", true)]
    [InlineData("\"1\"", true)]
    [InlineData("\"true\"", true)]
    [InlineData("false", false)]
    [InlineData("0", false)]
    [InlineData("\"\"", false)]
    [InlineData("null", false)]
    public async Task ReadsLenientBooleans(string value, bool expected)
    {
        AmlCheck check = await Call($$"""{"id":"aml-1","blacklist":{{value}}}""", c => c.CheckAmlStatusAsync("aml-1", Ct));

        Assert.Equal(expected, check.Blacklist);
    }

    [Fact]
    public async Task UnknownAmlValuesMapToUnknown()
    {
        AmlCheck check = await Call(
            """{"id":"aml-1","type":"contract","direction":"internal","status":"archived","risk_level":"severe"}""",
            c => c.CheckAmlStatusAsync("aml-1", Ct));

        Assert.Equal(AmlCheckType.Unknown, check.Type);
        Assert.Equal(AmlDirection.Unknown, check.Direction);
        Assert.Equal(AmlStatus.Unknown, check.Status);
        Assert.Equal(AmlRiskLevel.Unknown, check.RiskLevel);
    }

    [Fact]
    public async Task MapsAmlHistory()
    {
        AmlHistory history = await Call(
            """{"page":"2","per_page":10,"total":11,"items":[{"id":"aml-11","type":"address","status":"failed"}]}""",
            c => c.GetAmlHistoryAsync(new AmlHistoryRequest { Page = 2 }, Ct));

        Assert.Equal(2, history.Page);
        Assert.Equal(10, history.PerPage);
        Assert.Equal(11, history.Total);
        Assert.Equal(AmlStatus.Failed, Assert.Single(history.Items).Status);
    }

    [Fact]
    public async Task MapsSubscriptionPlansInApiOrder()
    {
        var plans = await Call(
            """
            {
              "unlimited_energy":{"id":8,"name":"Unlimited Energy","activation_fee":0,"initial_price":8,"price":2.8,"transactions_limit":0,"duration_days":0},
              "energy_pack_100":{"id":2,"name":"Energy Pack 100","activation_fee":"2.0","initial_price":10,"price":5,"transactions_limit":10,"duration_days":5}
            }
            """,
            c => c.GetSubscriptionsAsync(Ct));

        Assert.Equal(2, plans.Count);
        Assert.Equal(new SubscriptionPlan
        {
            SubscriptionId = "unlimited_energy",
            Id = 8,
            Name = "Unlimited Energy",
            ActivationFee = 0m,
            InitialPrice = 8m,
            Price = 2.8m,
            TransactionsLimit = 0,
            DurationDays = 0,
        }, plans[0]);
        Assert.Equal("energy_pack_100", plans[1].SubscriptionId);
        Assert.Equal(2, plans[1].Id);
        Assert.Equal(2m, plans[1].ActivationFee);
        Assert.Equal(10, plans[1].TransactionsLimit);
        Assert.Equal(5, plans[1].DurationDays);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    public async Task EmptySubscriptionPlans(string result)
    {
        var plans = await Call(result, c => c.GetSubscriptionsAsync(Ct));

        Assert.Empty(plans);
    }

    [Theory]
    [InlineData("\"unlimited_energy\"")]
    [InlineData("42")]
    [InlineData("[{\"id\":8}]")]
    public async Task UnexpectedSubscriptionPlansAreInvalidResponse(string result)
    {
        await using var server = TestServer.Start(Reply.Ok(result));

        await Assert.ThrowsAsync<Exceptions.TronzapInvalidResponseException>(() => Client(server).GetSubscriptionsAsync(Ct));
    }

    [Fact]
    public async Task MapsStartedSubscription()
    {
        Subscription subscription = await Call(
            """
            {"id":"01m4e1z3q0r7x225zc6p63m5ey","subscription_id":"unlimited_energy","created_at":"2026-10-08T15:26:32+00:00",
             "expire_at":"2026-11-07T15:26:32+00:00","address":"TAddress","status":"active","external_id":"sub-1",
             "params":{"address":"TAddress","duration":30,"transactions_limit":0,"activate_address":false}}
            """,
            c => c.StartSubscriptionAsync(new() { SubscriptionId = "unlimited_energy", Address = "TAddress", DurationDays = 30, ExternalId = "sub-1" }, Ct));

        Assert.Equal("01m4e1z3q0r7x225zc6p63m5ey", subscription.Id);
        Assert.Equal("unlimited_energy", subscription.SubscriptionId);
        Assert.Equal("sub-1", subscription.ExternalId);
        Assert.Equal("TAddress", subscription.Address);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal(new SubscriptionParams { Address = "TAddress", DurationDays = 30, TransactionsLimit = 0, ActivateAddress = false }, subscription.Params);
        Assert.Equal(new DateTimeOffset(2026, 10, 8, 15, 26, 32, TimeSpan.Zero), subscription.CreatedAt!.Value);
        Assert.Equal(new DateTimeOffset(2026, 11, 7, 15, 26, 32, TimeSpan.Zero), subscription.ExpireAt!.Value);
        Assert.Null(subscription.StartedAt);
        Assert.Null(subscription.StoppedAt);
        Assert.Equal(0m, subscription.TotalPrice);
    }

    [Fact]
    public async Task MapsStoppedSubscriptionWithoutAddressAndExpiry()
    {
        Subscription subscription = await Call(
            """
            {"id":"01m4e1z3q0r7x225zc6p63m5ey","subscription_id":"unlimited_energy","created_at":"2026-10-08T15:26:32+00:00",
             "stopped_at":"2026-10-08T15:28:44+00:00","status":"stopped","external_id":null,
             "params":{"address":"TAddress","duration":30,"transactions_limit":0,"activate_address":false,"future_field":1}}
            """,
            c => c.StopSubscriptionAsync(SubscriptionRequest.ById("01m4e1z3q0r7x225zc6p63m5ey"), Ct));

        Assert.Equal(SubscriptionStatus.Stopped, subscription.Status);
        Assert.Null(subscription.ExternalId);
        Assert.Equal("", subscription.Address);
        Assert.Null(subscription.ExpireAt);
        Assert.Equal(new DateTimeOffset(2026, 10, 8, 15, 28, 44, TimeSpan.Zero), subscription.StoppedAt!.Value);
        Assert.Equal("TAddress", subscription.Params!.Address);
    }

    [Fact]
    public async Task MapsSubscriptionHistory()
    {
        SubscriptionHistory history = await Call(
            """
            {"page":1,"per_page":10,"total":1,"items":[{
              "id":"01m4e1z3q0r7x225zc6p63m5ey","status":"active","subscription_id":"unlimited_energy","address":"TAddress",
              "transactions_limit":0,"transactions_used":4,"energy_used":262000,"total_price":13.6,
              "started_at":"2026-10-08T15:26:33+00:00","renewed_at":"2026-10-08T15:27:35+00:00","stopped_at":null,
              "expire_at":"2026-11-07T15:26:32+00:00","created_at":"2026-10-08T15:26:32+00:00"}]}
            """,
            c => c.GetSubscriptionHistoryAsync(Ct));

        Assert.Equal(1, history.Page);
        Assert.Equal(10, history.PerPage);
        Assert.Equal(1, history.Total);
        Subscription item = Assert.Single(history.Items);
        Assert.Equal(SubscriptionStatus.Active, item.Status);
        Assert.Equal(0, item.TransactionsLimit);
        Assert.Equal(4, item.TransactionsUsed);
        Assert.Equal(262000, item.EnergyUsed);
        Assert.Equal(13.6m, item.TotalPrice);
        Assert.Null(item.Params);
        Assert.Null(item.ExternalId);
        Assert.Equal(new DateTimeOffset(2026, 10, 8, 15, 26, 33, TimeSpan.Zero), item.StartedAt!.Value);
        Assert.Equal(new DateTimeOffset(2026, 10, 8, 15, 27, 35, TimeSpan.Zero), item.RenewedAt!.Value);
        Assert.Null(item.StoppedAt);
    }

    [Fact]
    public async Task ReadsSubscriptionTotalPriceAsString()
    {
        SubscriptionHistory history = await Call(
            """{"page":1,"per_page":10,"total":1,"items":[{"id":"sub-1","total_price":"8.00"}]}""",
            c => c.GetSubscriptionHistoryAsync(Ct));

        Assert.Equal(8m, Assert.Single(history.Items).TotalPrice);
    }

    [Theory]
    [InlineData("new", SubscriptionStatus.New)]
    [InlineData("pending", SubscriptionStatus.Pending)]
    [InlineData("error", SubscriptionStatus.Error)]
    [InlineData("active", SubscriptionStatus.Active)]
    [InlineData("stopped", SubscriptionStatus.Stopped)]
    [InlineData("expired", SubscriptionStatus.Expired)]
    [InlineData("paused", SubscriptionStatus.Unknown)]
    [InlineData("", SubscriptionStatus.Unknown)]
    public async Task MapsSubscriptionStatus(string wire, SubscriptionStatus expected)
    {
        Subscription subscription = await Call(
            $$"""{"id":"sub-1","status":"{{wire}}"}""",
            c => c.CheckSubscriptionAsync(SubscriptionRequest.ById("sub-1"), Ct));

        Assert.Equal(expected, subscription.Status);
    }

    [Fact]
    public async Task ReadsScalarsOfOtherTypesAsText()
    {
        Transaction tx = await Call(
            """{"id":12345,"external_id":true,"service":"energy","params":{"address":"T","amounts":{"energy":""},"duration":"1"},"status":"new","amount":"","created_at":1786364400}""",
            c => c.CheckTransactionAsync(CheckTransactionRequest.ById("tx-1"), Ct));

        Assert.Equal("12345", tx.Id);
        Assert.Equal("true", tx.ExternalId);
        Assert.Equal(0, tx.Params.Amounts.Energy);
        Assert.Equal(1, tx.Params.Duration);
        Assert.Equal(0m, tx.Amount);
        Assert.Equal("1786364400", tx.CreatedAt!.Raw);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1786364400), tx.CreatedAt.Value);
    }

    [Theory]
    [InlineData("1.5e2", 150)]
    [InlineData("\"1.5e2\"", 150)]
    [InlineData("65000.0", 65000)]
    [InlineData("\"65000.00\"", 65000)]
    public async Task ReadsNumbersInOtherNotations(string value, long expected)
    {
        AddressInfo info = await Call(
            $$$"""{"resources":{"energy":{{{value}}}},"balances":{"TRX":{{{value}}}}}""",
            c => c.GetAddressInfoAsync(Address, Ct));

        Assert.Equal(expected, info.Resources.Energy);
        Assert.Equal(expected, info.Balances["TRX"]);
    }

    [Fact]
    public async Task TooLargeDecimalIsInvalidResponse()
    {
        await using var server = TestServer.Start(Reply.Ok("""{"balance":1e40,"address":"T"}"""));

        await Assert.ThrowsAsync<Exceptions.TronzapInvalidResponseException>(() => Client(server).GetBalanceAsync(Ct));
    }

    [Fact]
    public async Task IgnoresUnknownFields()
    {
        AccountBalance balance = await Call(
            """{"balance":1,"address":"T","new_field":{"nested":[1,2,3]}}""",
            c => c.GetBalanceAsync(Ct));

        Assert.Equal(1m, balance.Balance);
    }

    [Fact]
    public async Task CollectionsAreReadOnly()
    {
        AmlHistory history = await Call(
            """{"page":1,"per_page":10,"total":1,"items":[{"id":"a"}]}""",
            c => c.GetAmlHistoryAsync(Ct));

        var list = Assert.IsAssignableFrom<System.Collections.Generic.IList<AmlCheck>>(history.Items);
        Assert.True(list.IsReadOnly);
    }
}
