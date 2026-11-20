# Changelog

All notable changes to this project are documented in this file. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.2.0] - 2026-11-20

### Added

- Bulk orders: `BulkCalculateAsync`, `BulkCreateAsync`, `BulkCheckAsync`, `GetBulkHistoryAsync` and
  `BulkReportAsync` for `/v1/bulk/calculate`, `/v1/bulk/create`, `/v1/bulk/check`, `/v1/bulk/history` and
  `/v1/bulk/report`, with the requests `BulkItem`, `BulkCalculateRequest`, `BulkCreateRequest`, `BulkOrderRequest`,
  `BulkReportRequest` and `BulkHistoryRequest`, the results `BulkCalculation`, `BulkOrder` and `BulkHistory`, and
  `BulkRejectedRow`, `BulkStatus` and `BulkRowStatus`. `BulkCalculateAsync` always orders energy; invalid rows are
  not checked by the SDK and come back in `Rejected` with their row number. `BulkReportAsync` returns the CSV report
  as a string.
- `TronzapClient.VerifyWebhookSignature` checks the `X-Signature` header of a bulk order webhook against the raw body.
- `TronzapApiException.Result` holds the `result` the API sends with an error, as a `JsonElement`. Until now it was
  dropped.
- `TronzapBulkPriceChangedException` and `TronzapBulkNoValidItemsException`, subclasses of `TronzapApiException` for
  codes 92 and 94, carry the new calculation and the rejected rows.
- `RetryAfter` on `TronzapApiException` and `TronzapHttpException` holds how long to wait before retrying, from the
  `Retry-After` header (seconds or an HTTP date) or the `retry_after` field, or is `null` when the server sent
  neither. `TronzapServerException` has a constructor that takes it.
- Error codes `BulkNotFound` (90), `BulkExpired` (91), `BulkPriceChanged` (92), `BulkTooManyItems` (93),
  `BulkNoValidItems` (94), `BulkResourceNotSupported` (95), `PayloadTooLarge` (96) and `RateLimited` (429).
- The example lists the bulk orders, calculates a bulk order for `TRONZAP_ADDRESS`, checks the order in
  `TRONZAP_BULK_ID` and prints its report, and with purchases allowed starts a one-address bulk order.
- This changelog.

### Changed

- `ITronzapClient` has the new bulk order methods, so custom implementations of the interface must add them.
- `RetryAfter` moved from `TronzapRateLimitException` to its base class `TronzapHttpException`; code that reads it
  is unchanged.
- The README and the `TronzapRateLimitException` docs say that a rate limit reported by the API, HTTP 429 with a JSON
  body, is a `TronzapApiException` with `RateLimited`, as it always has been; `TronzapRateLimitException` is only an
  HTTP 429 without an API body, such as one from a proxy.

## [1.1.0] - 2026-10-09

### Added

- Subscriptions: `GetSubscriptionsAsync`, `StartSubscriptionAsync`, `CheckSubscriptionAsync`, `StopSubscriptionAsync`
  and `GetSubscriptionHistoryAsync`, with their requests, the results `SubscriptionPlan`, `Subscription` and
  `SubscriptionHistory`, and the `SubscriptionStatus` enum.

### Changed

- A hash AML check without a direction sends `deposit` explicitly. Address checks are unchanged.
- `ITronzapClient` has the new subscription methods, so custom implementations of the interface must add them.

### Deprecated

- `EnergyRate.MinEnergy` and `MaxEnergy`, `Calculation.Energy` and `EnergyEstimate.Energy`. Use `MinAmount`,
  `MaxAmount` and `Amount`, which hold the same values.

### Fixed

- The docs of `EnergyRate.Price` and `DirectRechargeRate.Price`: they are the price of 1000 units, like
  `BandwidthRate.Price`, not of one unit. The values are unchanged.

## [1.0.0] - 2026-10-06

### Added

- The first release: `GetServicesAsync`, `GetBalanceAsync`, `GetAddressInfoAsync`, `EstimateEnergyAsync`,
  `CalculateAsync`, energy, bandwidth, resource bundle and address activation transactions, `CheckTransactionAsync`,
  `GetDirectRechargeInfoAsync` and AML checks, on .NET 8 and .NET 10.
- `TronzapClientOptions` with the API token and secret, base URL, timeout and `User-Agent`; a shared `HttpClient`,
  your own `HttpClient`, or `AddTronzap` with `IHttpClientFactory`.
- The exception hierarchy under `TronzapException`: `TronzapApiException` with `TronzapErrorCode`,
  `TronzapHttpException`, `TronzapInvalidResponseException` and `TronzapNetworkException`, with their subclasses.

[Unreleased]: https://github.com/tron-energy-market/tronzap-sdk-csharp/compare/v1.2.0...HEAD
[1.2.0]: https://github.com/tron-energy-market/tronzap-sdk-csharp/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/tron-energy-market/tronzap-sdk-csharp/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/tron-energy-market/tronzap-sdk-csharp/releases/tag/v1.0.0
