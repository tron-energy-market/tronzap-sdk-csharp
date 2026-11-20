using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Tronzap.Sdk.Exceptions;
using Tronzap.Sdk.Models;
using Tronzap.Sdk.Requests;
using Tronzap.Sdk.Responses;
using Xunit;
using static Tronzap.Sdk.Tests.TestSupport;

namespace Tronzap.Sdk.Tests;

public sealed class BulkTests
{
    private const string Calculation = """
        {"id":"01kb2m7a1bc2de3fg4hj5km6np","status":"draft","resource":"energy","duration":1,"total_amount":1240.1,
         "accepted":2,"rejected_count":1,"rejected":[{"row":3,"address":"abc","reason":"invalid_address"}],
         "expires_at":"2026-11-16T12:15:00+00:00"}
        """;

    private const string Order = """
        {"id":"01kb2m5x9qj4v8n3c7d6e5f4g3","external_id":"my-order-42","status":"partial","resource":"energy",
         "duration":1,"force":true,"max_price_increase":20,"webhook_url":"https://example.com/tronzap/bulk","total":4,
         "processed":4,"delivered":2,"failed":0,"in_review":0,"skipped":2,
         "skipped_by_reason":{"address_not_activated":1,"price_increased":1},"rejected_count":1,
         "total_amount":"1240.10","charged_amount":"620.05","created_at":"2026-11-16T12:05:00+00:00",
         "started_at":"2026-11-16T12:06:00+00:00","finalized_at":"2026-11-16T12:09:00+00:00"}
        """;

    private const string Csv = """
        row,address,amount,status,reason,charged,transaction_id
        1,TPY1Kb8cKAZQfm95gXQQs2Mh8Uygtos21D,65000,delivered,,3.25,01kb2m9c4de5fg6hj7km8np9qr
        2,TQn9Y2khEsLJW1ChVWFMSMeRDow5KcbLSE,131000,skipped,address_not_activated,,

        """;

    [Fact]
    public async Task CalculateMapsTheCalculation()
    {
        await using var server = TestServer.Start(Reply.Ok(Calculation));

        BulkCalculation calculation = await Client(server).BulkCalculateAsync(
            new() { Items = [new() { Address = Address, Amount = 65000 }] }, Ct);

        Assert.Equal("01kb2m7a1bc2de3fg4hj5km6np", calculation.Id);
        Assert.Equal(BulkStatus.Draft, calculation.Status);
        Assert.Equal("energy", calculation.Resource);
        Assert.Equal(1, calculation.Duration);
        Assert.Equal(1240.1m, calculation.TotalAmount);
        Assert.Equal(2, calculation.Accepted);
        Assert.Equal(1, calculation.RejectedCount);
        Assert.Equal(new BulkRejectedRow { Row = 3, Address = "abc", Reason = "invalid_address" }, Assert.Single(calculation.Rejected));
        Assert.Equal(new DateTimeOffset(2026, 11, 16, 12, 15, 0, TimeSpan.Zero), calculation.ExpiresAt!.Value);
    }

    [Fact]
    public async Task CalculateWithNoValidRowsReportsThem()
    {
        await using var server = TestServer.Start(Reply.Raw(200, """
            {"code":94,"error":"No valid items","key":"bulk_no_valid_items",
             "result":{"rejected":[{"row":1,"address":"abc","reason":"invalid_address"},{"row":2,"address":"abc","reason":"duplicate"}]}}
            """));

        var e = await Assert.ThrowsAsync<TronzapBulkNoValidItemsException>(() => Client(server).BulkCalculateAsync(
            new() { Items = [new() { Address = "abc", Amount = 1 }, new() { Address = "abc", Amount = 1 }] }, Ct));

        Assert.Equal(TronzapErrorCode.BulkNoValidItems, e.ErrorCode);
        Assert.Equal("bulk_no_valid_items", e.ErrorKey);
        Assert.Equal(2, e.Rejected.Count);
        Assert.Equal("duplicate", e.Rejected[1].Reason);
        Assert.Equal(2, e.Result!.Value.GetProperty("rejected").GetArrayLength());
    }

    [Fact]
    public async Task CreateMapsTheOrder()
    {
        await using var server = TestServer.Start(Reply.Ok(Order));

        BulkOrder order = await Client(server).BulkCreateAsync(new() { Id = "01kb2m7a1bc2de3fg4hj5km6np" }, Ct);

        Assert.Equal("01kb2m5x9qj4v8n3c7d6e5f4g3", order.Id);
        Assert.Equal("my-order-42", order.ExternalId);
        Assert.Equal(BulkStatus.Partial, order.Status);
        Assert.Equal("energy", order.Resource);
        Assert.Equal(1, order.Duration);
        Assert.True(order.Force);
        Assert.Equal(20, order.MaxPriceIncrease);
        Assert.Equal("https://example.com/tronzap/bulk", order.WebhookUrl);
        Assert.Equal(4, order.Total);
        Assert.Equal(4, order.Processed);
        Assert.Equal(2, order.Delivered);
        Assert.Equal(0, order.Failed);
        Assert.Equal(0, order.InReview);
        Assert.Equal(2, order.Skipped);
        Assert.Equal(1, order.SkippedByReason["address_not_activated"]);
        Assert.Equal(1, order.SkippedByReason["price_increased"]);
        Assert.Equal(1, order.RejectedCount);
        Assert.Equal(1240.10m, order.TotalAmount);
        Assert.Equal(620.05m, order.ChargedAmount);
        Assert.Equal("2026-11-16T12:05:00+00:00", order.CreatedAt!.Raw);
        Assert.NotNull(order.StartedAt);
        Assert.NotNull(order.FinalizedAt);
    }

    [Fact]
    public async Task CreateReturnsTheNewCalculationWhenThePriceChanged()
    {
        await using var server = TestServer.Start(Reply.Raw(200, $$"""
            {"code":92,"error":"Price changed","key":"bulk_price_changed","request_id":"req-9","result":{{Calculation}}}
            """));

        var e = await Assert.ThrowsAsync<TronzapBulkPriceChangedException>(
            () => Client(server).BulkCreateAsync(new() { Id = "old" }, Ct));

        Assert.Equal(TronzapErrorCode.BulkPriceChanged, e.ErrorCode);
        Assert.Equal("req-9", e.RequestId);
        Assert.Equal("01kb2m7a1bc2de3fg4hj5km6np", e.Calculation.Id);
        Assert.Equal(1240.1m, e.Calculation.TotalAmount);
        Assert.Equal("01kb2m7a1bc2de3fg4hj5km6np", e.Result!.Value.GetProperty("id").GetString());
    }

    [Theory]
    [InlineData(92, "[]")]
    [InlineData(92, "\"text\"")]
    [InlineData(92, """{"total_amount":"lots"}""")]
    [InlineData(94, """{"rejected":"none"}""")]
    [InlineData(94, "[1]")]
    public async Task ErrorDataOfAnUnexpectedShapeIsStillTheApiError(int code, string result)
    {
        await using var server = TestServer.Start(Reply.Raw(200, $$"""{"code":{{code}},"error":"x","result":{{result}}}"""));

        var e = await Assert.ThrowsAsync<TronzapApiException>(() => Client(server).BulkCreateAsync(new() { Id = "old" }, Ct));

        Assert.Equal(code, e.Code);
        Assert.NotNull(e.Result);
    }

    [Fact]
    public async Task ProcessingOrderHasNoFinalDateAndNoSkips()
    {
        await using var server = TestServer.Start(Reply.Ok("""
            {"id":"b","status":"processing","total":2,"processed":0,"skipped_by_reason":[],"finalized_at":null,
             "external_id":null,"webhook_url":null}
            """));

        BulkOrder order = await Client(server).BulkCheckAsync(BulkOrderRequest.ById("b"), Ct);

        Assert.Equal(BulkStatus.Processing, order.Status);
        Assert.Null(order.FinalizedAt);
        Assert.Null(order.ExternalId);
        Assert.Null(order.WebhookUrl);
        Assert.Empty(order.SkippedByReason);
    }

    [Fact]
    public async Task UnknownOrderIsAnApiError()
    {
        await using var server = TestServer.Start(Reply.Error(404, 90, "Bulk order not found", "bulk_not_found"));

        var e = await Assert.ThrowsAsync<TronzapApiException>(
            () => Client(server).BulkCheckAsync(BulkOrderRequest.ByExternalId("missing"), Ct));

        Assert.Equal(TronzapErrorCode.BulkNotFound, e.ErrorCode);
        Assert.Equal(HttpStatusCode.NotFound, e.StatusCode);
    }

    [Fact]
    public async Task HistoryMapsThePage()
    {
        await using var server = TestServer.Start(Reply.Ok($$"""{"page":2,"per_page":1,"total":3,"items":[{{Order}}]}"""));

        BulkHistory history = await Client(server).GetBulkHistoryAsync(new() { Page = 2, PerPage = 1 }, Ct);

        Assert.Equal(2, history.Page);
        Assert.Equal(1, history.PerPage);
        Assert.Equal(3, history.Total);
        Assert.Equal("my-order-42", Assert.Single(history.Items).ExternalId);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(BulkRowStatus.Delivered, "delivered")]
    [InlineData(BulkRowStatus.Failed, "failed")]
    [InlineData(BulkRowStatus.InReview, "in_review")]
    [InlineData(BulkRowStatus.Skipped, "skipped")]
    [InlineData(BulkRowStatus.Pending, "pending")]
    public async Task ReportReturnsTheCsv(BulkRowStatus? status, string? wire)
    {
        await using var server = TestServer.Start(Reply.Raw(200, Csv, "text/csv; charset=utf-8"));

        string report = await Client(server).BulkReportAsync(BulkReportRequest.ById("bulk-1") with { Status = status }, Ct);

        Assert.Equal(Csv, report);
        RecordedRequest request = server.SingleRequest;
        Assert.Equal("/v1/bulk/report", request.Path);
        AssertJsonEqual(wire is null ? """{"id":"bulk-1"}""" : $$"""{"id":"bulk-1","status":"{{wire}}"}""", request);
        Assert.Equal(ExpectedSignature(request.Body), request.Header("X-Signature"));
    }

    [Fact]
    public async Task ReportIsUtf8WithoutACharset()
    {
        await using var server = TestServer.Start(new Reply
        {
            Body = Encoding.UTF8.GetBytes("row,reason\n1,ünïcode\n"),
            Headers = new(StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = "text/csv" },
        });

        string report = await Client(server).BulkReportAsync(BulkReportRequest.ByExternalId("order-42"), Ct);

        Assert.Equal("row,reason\n1,ünïcode\n", report);
        AssertJsonEqual("""{"external_id":"order-42"}""", server.SingleRequest);
    }

    [Fact]
    public async Task ReportErrorsAreJson()
    {
        await using var server = TestServer.Start(Reply.Error(404, 90, "Bulk order not found"));

        var e = await Assert.ThrowsAsync<TronzapApiException>(
            () => Client(server).BulkReportAsync(BulkReportRequest.ById("missing"), Ct));

        Assert.Equal(TronzapErrorCode.BulkNotFound, e.ErrorCode);
    }

    [Fact]
    public async Task ReportErrorWithoutPayloadIsHttpError()
    {
        await using var server = TestServer.Start(Reply.Raw(502, "<html>bad gateway</html>", "text/html"));

        await Assert.ThrowsAsync<TronzapServerException>(() => Client(server).BulkReportAsync(BulkReportRequest.ById("b"), Ct));
    }

    [Theory]
    [InlineData("""{"code":0,"result":{}}""", "application/json")]
    [InlineData("row,address", "text/plain")]
    public async Task ReportThatIsNotCsvIsAnInvalidResponse(string body, string contentType)
    {
        await using var server = TestServer.Start(Reply.Raw(200, body, contentType));

        var e = await Assert.ThrowsAsync<TronzapInvalidResponseException>(
            () => Client(server).BulkReportAsync(BulkReportRequest.ById("b"), Ct));

        Assert.Equal(body, e.ResponseBody);
    }

    [Fact]
    public void WebhookSignature()
    {
        var client = new TronzapClient(new TronzapClientOptions { ApiToken = Token, ApiSecret = Secret });
        const string body = """{"id":"01kb2m5x9qj4v8n3c7d6e5f4g3","external_id":"my-order-42","status":"completed"}""";
        string signature = ExpectedSignature(Encoding.UTF8.GetBytes(body));

        Assert.True(client.VerifyWebhookSignature(body, signature));
        Assert.True(client.VerifyWebhookSignature(Encoding.UTF8.GetBytes(body), signature));
        Assert.True(client.VerifyWebhookSignature(body, $"  {signature.ToUpperInvariant()} "));
        Assert.False(client.VerifyWebhookSignature(body + " ", signature));
        Assert.False(client.VerifyWebhookSignature(body, signature[..^1]));
        Assert.False(client.VerifyWebhookSignature(body, ""));
        Assert.False(client.VerifyWebhookSignature(body, null));
        Assert.Throws<ArgumentNullException>(() => client.VerifyWebhookSignature((string)null!, signature));
        Assert.Throws<ArgumentNullException>(() => client.VerifyWebhookSignature((byte[])null!, signature));
    }

    [Fact]
    public async Task RequestsAreValidatedBeforeSending()
    {
        await using var server = TestServer.Start(Reply.Ok("{}"));
        TronzapClient client = Client(server);

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.BulkCalculateAsync(null!, Ct));
        await Assert.ThrowsAsync<ArgumentException>(() => client.BulkCalculateAsync(new() { Items = [] }, Ct));
        await Assert.ThrowsAsync<ArgumentException>(() => client.BulkCalculateAsync(new() { Items = null! }, Ct));
        await Assert.ThrowsAsync<ArgumentException>(() => client.BulkCalculateAsync(new() { Items = [null!] }, Ct));
        await Assert.ThrowsAsync<ArgumentException>(() => client.BulkCalculateAsync(new() { Items = [new() { Address = null!, Amount = 1 }] }, Ct));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => client.BulkCalculateAsync(new() { Items = [new() { Address = "T", Amount = 1 }], Duration = 0 }, Ct));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.BulkCreateAsync(null!, Ct));
        await Assert.ThrowsAsync<ArgumentException>(() => client.BulkCreateAsync(new() { Id = " " }, Ct));
        await Assert.ThrowsAsync<ArgumentException>(() => client.BulkCreateAsync(new() { Id = "c", ExternalId = "" }, Ct));
        await Assert.ThrowsAsync<ArgumentException>(() => client.BulkCreateAsync(new() { Id = "c", WebhookUrl = " " }, Ct));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.BulkCheckAsync(null!, Ct));
        await Assert.ThrowsAsync<ArgumentException>(() => client.BulkCheckAsync(new(), Ct));
        await Assert.ThrowsAsync<ArgumentException>(() => client.BulkCheckAsync(BulkOrderRequest.ById(""), Ct));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetBulkHistoryAsync(null!, Ct));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.GetBulkHistoryAsync(new() { Page = 0 }, Ct));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.GetBulkHistoryAsync(new() { PerPage = -1 }, Ct));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.BulkReportAsync(null!, Ct));
        await Assert.ThrowsAsync<ArgumentException>(() => client.BulkReportAsync(new(), Ct));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => client.BulkReportAsync(BulkReportRequest.ById("b") with { Status = BulkRowStatus.Unknown }, Ct));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => client.BulkReportAsync(BulkReportRequest.ById("b") with { Status = (BulkRowStatus)42 }, Ct));
        Assert.Empty(server.Requests);
    }

    [Fact]
    public void RequestDefaults()
    {
        var calculate = new BulkCalculateRequest { Items = [new() { Address = "T", Amount = 1 }] };
        var history = new BulkHistoryRequest();
        var create = new BulkCreateRequest { Id = "c" };

        Assert.Equal(1, calculate.Duration);
        Assert.Equal(1, history.Page);
        Assert.Equal(BulkHistoryRequest.DefaultPerPage, history.PerPage);
        Assert.False(create.Force);
        Assert.Null(create.MaxPriceIncrease);
        Assert.Null(create.ExternalId);
        Assert.Null(create.WebhookUrl);
    }

    [Theory]
    [InlineData("draft", BulkStatus.Draft)]
    [InlineData("processing", BulkStatus.Processing)]
    [InlineData("completed", BulkStatus.Completed)]
    [InlineData("partial", BulkStatus.Partial)]
    [InlineData("failed", BulkStatus.Failed)]
    [InlineData("paused", BulkStatus.Unknown)]
    public async Task ParsesBulkStatuses(string wire, BulkStatus expected)
    {
        await using var server = TestServer.Start(Reply.Ok($$"""{"id":"b","status":"{{wire}}"}"""));

        BulkOrder order = await Client(server).BulkCheckAsync(BulkOrderRequest.ById("b"), Ct);

        Assert.Equal(expected, order.Status);
    }
}
