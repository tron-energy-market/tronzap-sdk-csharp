using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Tronzap.Sdk.Exceptions;
using Tronzap.Sdk.Requests;
using Xunit;
using static Tronzap.Sdk.Tests.TestSupport;

namespace Tronzap.Sdk.Tests;

public sealed class ErrorHandlingTests
{
    private static async Task<TException> Fails<TException>(Reply reply, Func<TronzapClient, Task>? call = null)
        where TException : Exception
    {
        await using var server = TestServer.Start(reply);
        call ??= c => c.GetBalanceAsync(Ct);
        return await Assert.ThrowsAnyAsync<TException>(() => call(Client(server)));
    }

    [Theory]
    [InlineData(200)]
    [InlineData(400)]
    [InlineData(422)]
    [InlineData(500)]
    public async Task ApiErrorCodeTakesPrecedenceOverHttpStatus(int status)
    {
        Reply reply = Reply.Error(status, 10, "Invalid TRON address", "invalid_tron_address.from_address", "req-123");

        var e = await Fails<TronzapApiException>(reply);

        Assert.Equal(10, e.Code);
        Assert.Equal(TronzapErrorCode.InvalidTronAddress, e.ErrorCode);
        Assert.Equal("Invalid TRON address", e.Message);
        Assert.Equal("invalid_tron_address.from_address", e.ErrorKey);
        Assert.Equal("req-123", e.RequestId);
        Assert.Equal((HttpStatusCode)status, e.StatusCode);
        Assert.Contains("\"request_id\":\"req-123\"", e.ResponseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InsufficientFundsAt200IsNotSuccess()
    {
        var e = await Fails<TronzapApiException>(
            Reply.Error(200, 6, "Insufficient funds"),
            c => c.CreateEnergyTransactionAsync(new() { Address = Address, Energy = 65000 }, Ct));

        Assert.Equal(TronzapErrorCode.InsufficientFunds, e.ErrorCode);
    }

    [Fact]
    public async Task TransactionNotFoundKeepsServerKey()
    {
        var e = await Fails<TronzapApiException>(
            Reply.Error(200, 20, "Transaction not found", "subscription_not_found"),
            c => c.CheckTransactionAsync(CheckTransactionRequest.ById("missing"), Ct));

        Assert.Equal(TronzapErrorCode.TransactionNotFound, e.ErrorCode);
        Assert.Equal("subscription_not_found", e.ErrorKey);
    }

    [Theory]
    [InlineData(1, TronzapErrorCode.AuthError)]
    [InlineData(2, TronzapErrorCode.InvalidServiceOrParams)]
    [InlineData(5, TronzapErrorCode.WalletNotFound)]
    [InlineData(11, TronzapErrorCode.InvalidEnergyAmount)]
    [InlineData(12, TronzapErrorCode.InvalidDuration)]
    [InlineData(21, TronzapErrorCode.CannotStopSubscription)]
    [InlineData(24, TronzapErrorCode.AddressNotActivated)]
    [InlineData(25, TronzapErrorCode.AddressAlreadyActivated)]
    [InlineData(30, TronzapErrorCode.AmlCheckNotFound)]
    [InlineData(35, TronzapErrorCode.ServiceNotAvailable)]
    [InlineData(50, TronzapErrorCode.InvalidBandwidthAmount)]
    [InlineData(500, TronzapErrorCode.InternalServerError)]
    [InlineData(999, TronzapErrorCode.Unknown)]
    [InlineData(-3, TronzapErrorCode.Unknown)]
    public async Task MapsErrorCodes(int code, TronzapErrorCode expected)
    {
        var e = await Fails<TronzapApiException>(Reply.Error(200, code, "error"));

        Assert.Equal(code, e.Code);
        Assert.Equal(expected, e.ErrorCode);
    }

    [Fact]
    public async Task ReadsCodeSentAsString()
    {
        var e = await Fails<TronzapApiException>(Reply.Raw(200, """{"code":"6","error":"Insufficient funds"}"""));

        Assert.Equal(6, e.Code);
    }

    [Fact]
    public async Task CodeZeroAsStringIsSuccess()
    {
        await using var server = TestServer.Start(Reply.Raw(200, """{"code":"0","result":{"balance":1,"address":"T"}}"""));

        var balance = await Client(server).GetBalanceAsync(Ct);

        Assert.Equal(1m, balance.Balance);
    }

    [Theory]
    [InlineData("""{"result":{"balance":1}}""")]
    [InlineData("""{"code":null,"result":{}}""")]
    [InlineData("""{"code":"abc"}""")]
    [InlineData("{}")]
    public async Task MissingOrUnreadableCodeIsApiError(string body)
    {
        var e = await Fails<TronzapApiException>(Reply.Raw(200, body));

        Assert.Equal(1, e.Code);
        Assert.Equal("Unknown API error", e.Message);
        Assert.Equal(body, e.ResponseBody);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("[1,2]")]
    [InlineData("\"text\"")]
    [InlineData("42")]
    [InlineData("null")]
    [InlineData("true")]
    public async Task JsonThatIsNotAnObjectIsApiError(string body)
    {
        var e = await Fails<TronzapApiException>(Reply.Raw(200, body));

        Assert.Equal(1, e.Code);
        Assert.Equal(body, e.ResponseBody);
    }

    [Theory]
    [InlineData(401, typeof(TronzapUnauthorizedException))]
    [InlineData(403, typeof(TronzapUnauthorizedException))]
    [InlineData(429, typeof(TronzapRateLimitException))]
    [InlineData(500, typeof(TronzapServerException))]
    [InlineData(502, typeof(TronzapServerException))]
    [InlineData(503, typeof(TronzapServerException))]
    [InlineData(404, typeof(TronzapHttpException))]
    [InlineData(418, typeof(TronzapHttpException))]
    public async Task NonJsonErrorStatusMapsToHttpException(int status, Type expected)
    {
        var e = await Fails<TronzapHttpException>(Reply.Raw(status, "<html>gateway</html>", "text/html"));

        Assert.IsType(expected, e);
        Assert.Equal((HttpStatusCode)status, e.StatusCode);
        Assert.Equal("<html>gateway</html>", e.ResponseBody);
    }

    [Fact]
    public async Task CodeZeroWithErrorStatusIsHttpException()
    {
        var e = await Fails<TronzapServerException>(Reply.Raw(500, """{"code":0,"result":{}}"""));

        Assert.Equal(HttpStatusCode.InternalServerError, e.StatusCode);
    }

    [Fact]
    public async Task EmptyErrorBodyIsHttpException()
    {
        var e = await Fails<TronzapHttpException>(Reply.Raw(502, ""));

        Assert.IsType<TronzapServerException>(e);
        Assert.Equal("", e.ResponseBody);
    }

    [Theory]
    [InlineData("120", 120)]
    [InlineData("0", 0)]
    public async Task RateLimitReadsRetryAfterSeconds(string header, int seconds)
    {
        Reply reply = Reply.Raw(429, "Too Many Requests", "text/plain");
        reply.Headers["Retry-After"] = header;

        var e = await Fails<TronzapRateLimitException>(reply);

        Assert.Equal(TimeSpan.FromSeconds(seconds), e.RetryAfter);
    }

    [Fact]
    public async Task RateLimitReadsRetryAfterDate()
    {
        Reply reply = Reply.Raw(429, "Too Many Requests", "text/plain");
        reply.Headers["Retry-After"] = DateTimeOffset.UtcNow.AddMinutes(2).ToString("R", System.Globalization.CultureInfo.InvariantCulture);

        var e = await Fails<TronzapRateLimitException>(reply);

        Assert.InRange(e.RetryAfter!.Value, TimeSpan.FromSeconds(90), TimeSpan.FromSeconds(121));
    }

    [Fact]
    public async Task RateLimitWithoutRetryAfter()
    {
        var e = await Fails<TronzapRateLimitException>(Reply.Raw(429, ""));

        Assert.Null(e.RetryAfter);
    }

    [Fact]
    public async Task RateLimitWithApiPayloadIsApiError()
    {
        var e = await Fails<TronzapApiException>(Reply.Error(429, 2, "Slow down"));

        Assert.Equal(HttpStatusCode.TooManyRequests, e.StatusCode);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"code\":0,")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InvalidJsonAt2xxIsInvalidResponse(string body)
    {
        var e = await Fails<TronzapInvalidResponseException>(Reply.Raw(200, body));

        Assert.Equal(HttpStatusCode.OK, e.StatusCode);
        Assert.Equal(body, e.ResponseBody);
        Assert.Contains("Invalid JSON", e.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{"code":0}""")]
    [InlineData("""{"code":0,"result":null}""")]
    public async Task MissingResultIsInvalidResponse(string body)
    {
        var e = await Fails<TronzapInvalidResponseException>(Reply.Raw(200, body));

        Assert.Contains("Missing result", e.Message, StringComparison.Ordinal);
    }

    public static TheoryData<string, Func<TronzapClient, Task>> WrongShapes => new()
    {
        { "\"balance\"", c => c.GetBalanceAsync(Ct) },
        { "[1]", c => c.GetBalanceAsync(Ct) },
        { """{"balance":"lots"}""", c => c.GetBalanceAsync(Ct) },
        { """{"balance":{"x":1}}""", c => c.GetBalanceAsync(Ct) },
        { """{"address":["T"]}""", c => c.GetBalanceAsync(Ct) },
        { """{"energy":"none"}""", c => c.GetServicesAsync(Ct) },
        { """{"energy":[1]}""", c => c.GetServicesAsync(Ct) },
        { """{"amount":"1.5x"}""", c => c.CheckTransactionAsync(CheckTransactionRequest.ById("x"), Ct) },
        { """{"params":{"duration":1.5}}""", c => c.CheckTransactionAsync(CheckTransactionRequest.ById("x"), Ct) },
        { """{"params":{"duration":99999999999}}""", c => c.CheckTransactionAsync(CheckTransactionRequest.ById("x"), Ct) },
        { """{"created_at":{"date":"x"}}""", c => c.CheckTransactionAsync(CheckTransactionRequest.ById("x"), Ct) },
        { """{"blacklist":"maybe"}""", c => c.CheckAmlStatusAsync("x", Ct) },
        { "\"none\"", c => c.GetAmlServicesAsync(Ct) },
    };

    [Theory]
    [MemberData(nameof(WrongShapes))]
    public async Task ResultOfWrongShapeIsInvalidResponse(string result, Func<TronzapClient, Task> call)
    {
        var e = await Fails<TronzapInvalidResponseException>(Reply.Ok(result), call);

        Assert.Contains("Unexpected result", e.Message, StringComparison.Ordinal);
        Assert.Contains(result, e.ResponseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EmptyObjectAmlServicesIsEmptyList()
    {
        await using var server = TestServer.Start(Reply.Ok("{}"));

        Assert.Empty(await Client(server).GetAmlServicesAsync(Ct));
    }

    [Fact]
    public async Task OversizedResponseIsInvalidResponse()
    {
        string padding = new('x', TronzapClient.MaxResponseBytes);
        var e = await Fails<TronzapInvalidResponseException>(Reply.Ok($$"""{"pad":"{{padding}}"}"""));

        Assert.Contains("exceeds", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OversizedResponseWithoutContentLengthIsInvalidResponse()
    {
        string padding = new('x', TronzapClient.MaxResponseBytes);
        Reply reply = Reply.Ok($$"""{"pad":"{{padding}}"}""");

        var e = await Fails<TronzapInvalidResponseException>(new Reply { Body = reply.Body, OmitContentLength = true });

        Assert.Contains("exceeds", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResponseWithoutContentLengthIsRead()
    {
        Reply reply = Reply.Ok("""{"balance":"3","address":"T"}""");
        await using var server = TestServer.Start(new Reply { Body = reply.Body, OmitContentLength = true });

        var balance = await Client(server).GetBalanceAsync(Ct);

        Assert.Equal(3m, balance.Balance);
    }

    [Fact]
    public async Task NonStringErrorFieldsFallBack()
    {
        var e = await Fails<TronzapApiException>(Reply.Raw(200, """{"code":2,"error":{"text":"x"},"key":7,"request_id":[1]}"""));

        Assert.Equal("Unknown API error", e.Message);
        Assert.Equal("7", e.ErrorKey);
        Assert.Null(e.RequestId);
    }

    [Fact]
    public async Task AllFailuresShareTheBaseType()
    {
        var replies = new List<Reply>
        {
            Reply.Error(200, 6, "x"),
            Reply.Raw(500, "x"),
            Reply.Raw(200, "x"),
        };

        foreach (Reply reply in replies)
        {
            await Fails<TronzapException>(reply);
        }
    }
}
