using System;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Tronzap.Sdk.Exceptions;

namespace Tronzap.Sdk.Internal;

internal static class ResponseDecoder
{
    private const string UnknownApiError = "Unknown API error";

    // The API code takes precedence over the HTTP status: some API errors arrive with 2xx, others with 4xx/5xx.
    public static T Decode<T>(HttpStatusCode status, byte[] body, TimeSpan? retryAfter, Func<JsonElement, T> map)
    {
        JsonDocument? document = null;
        JsonException? parseError = null;
        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException e)
        {
            parseError = e;
        }

        using (document)
        {
            if (document is not null)
            {
                JsonElement root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    throw new TronzapApiException(UnknownApiError, 1, null, null, status, Text(body));
                }

                int? code = Code(root);
                if (code != 0)
                {
                    throw new TronzapApiException(
                        StringField(root, "error") ?? UnknownApiError,
                        code ?? 1,
                        StringField(root, "key"),
                        StringField(root, "request_id"),
                        status,
                        Text(body));
                }
            }

            int statusCode = (int)status;
            if (statusCode is < 200 or > 299)
            {
                throw HttpError(status, Text(body), retryAfter);
            }

            if (document is null)
            {
                throw new TronzapInvalidResponseException("Invalid JSON response.", status, Text(body), parseError);
            }

            JsonElement result = JsonValues.Field(document.RootElement, "result");
            if (JsonValues.IsAbsent(result))
            {
                throw new TronzapInvalidResponseException("Missing result in response.", status, Text(body), null);
            }

            try
            {
                return map(result);
            }
            catch (MappingException e)
            {
                throw new TronzapInvalidResponseException($"Unexpected result in response: {e.Message}.", status, Text(body), e);
            }
        }
    }

    private static TronzapHttpException HttpError(HttpStatusCode status, string body, TimeSpan? retryAfter)
    {
        int code = (int)status;
        return code switch
        {
            429 => new TronzapRateLimitException("Too many requests.", status, body, retryAfter),
            401 or 403 => new TronzapUnauthorizedException("Unauthorized.", status, body),
            >= 500 => new TronzapServerException($"Server error (HTTP {code}).", status, body),
            _ => new TronzapHttpException($"HTTP error {code}.", status, body),
        };
    }

    private static int? Code(JsonElement root)
    {
        JsonElement code = JsonValues.Field(root, "code");
        if (code.ValueKind == JsonValueKind.Number && code.TryGetInt32(out int number))
        {
            return number;
        }

        if (code.ValueKind == JsonValueKind.String
            && int.TryParse(code.GetString(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out number))
        {
            return number;
        }

        return null;
    }

    private static string? StringField(JsonElement root, string name)
    {
        JsonElement value = JsonValues.Field(root, name);
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null,
        };
    }

    private static string Text(byte[] body) => Encoding.UTF8.GetString(body);
}
