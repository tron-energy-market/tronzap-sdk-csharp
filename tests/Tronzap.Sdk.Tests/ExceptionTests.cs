using System;
using System.Linq;
using System.Net;
using System.Reflection;
using Tronzap.Sdk.Exceptions;
using Xunit;

namespace Tronzap.Sdk.Tests;

public sealed class ExceptionTests
{
    public static TheoryData<Type> ExceptionTypes => new(
        typeof(TronzapException).Assembly.GetExportedTypes().Where(t => typeof(Exception).IsAssignableFrom(t)));

    [Theory]
    [MemberData(nameof(ExceptionTypes))]
    public void HasStandardConstructors(Type type)
    {
        var inner = new InvalidOperationException("inner");

        var empty = (Exception)Activator.CreateInstance(type)!;
        var withMessage = (Exception)Activator.CreateInstance(type, "message")!;
        var withInner = (Exception)Activator.CreateInstance(type, "message", inner)!;

        Assert.False(string.IsNullOrEmpty(empty.Message));
        Assert.Equal("message", withMessage.Message);
        Assert.Same(inner, withInner.InnerException);
        Assert.True(typeof(TronzapException).IsAssignableFrom(type));
        PropertyInfo? body = type.GetProperty("ResponseBody");
        if (body is not null)
        {
            Assert.Equal("", body.GetValue(empty));
            Assert.Equal("", body.GetValue(withMessage));
            Assert.Equal("", body.GetValue(withInner));
        }
    }

    [Fact]
    public void ApiExceptionDefaultsToGenericCode()
    {
        Assert.Equal(1, new TronzapApiException().Code);
        Assert.Equal(1, new TronzapApiException("x").Code);
        Assert.Equal(1, new TronzapApiException("x", null).Code);
        Assert.Equal(TronzapErrorCode.AuthError, new TronzapApiException().ErrorCode);
        Assert.Equal(TronzapErrorCode.Unknown, new TronzapApiException("x", 0, null, null, HttpStatusCode.OK, null!).ErrorCode);
        Assert.Equal("", new TronzapApiException("x", 6, null, null, HttpStatusCode.OK, null!).ResponseBody);
    }

    [Fact]
    public void HttpExceptionsKeepStatusAndBody()
    {
        var unauthorized = new TronzapUnauthorizedException("u", HttpStatusCode.Forbidden, "body");
        var server = new TronzapServerException("s", HttpStatusCode.BadGateway, null!);
        var invalid = new TronzapInvalidResponseException("i", HttpStatusCode.OK, null!, null);

        Assert.Equal(HttpStatusCode.Forbidden, unauthorized.StatusCode);
        Assert.Equal("body", unauthorized.ResponseBody);
        Assert.Equal("", server.ResponseBody);
        Assert.Equal("", invalid.ResponseBody);
        Assert.Null(new TronzapRateLimitException().RetryAfter);
    }
}
