# Покупка энергии Tron через API
## .NET SDK от TronZap.com

[English](README.md) | [Español](README.es.md) | [Português](README.pt-br.md) | **[Русский](README.ru.md)**

[![NuGet](https://img.shields.io/nuget/v/Tronzap.Sdk.svg)](https://www.nuget.org/packages/Tronzap.Sdk)
[![CI](https://github.com/tron-energy-market/tronzap-sdk-csharp/actions/workflows/ci.yml/badge.svg)](https://github.com/tron-energy-market/tronzap-sdk-csharp/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Официальный .NET SDK для API TronZap.
Этот SDK позволяет легко интегрировать сервисы TronZap для аренды энергии TRON.

TronZap.com позволяет [покупать энергию TRON](https://tronzap.com/), существенно снижая комиссии при переводах USDT (TRC20).

👉 [Зарегистрируйтесь для получения API ключа](https://tronzap.com), чтобы начать использовать TronZap API и интегрировать его через SDK.

- Сайт: https://tronzap.com/
- Справочник API: https://docs.tronzap.com/
- NuGet: https://www.nuget.org/packages/Tronzap.Sdk
- Исходный код: https://github.com/tron-energy-market/tronzap-sdk-csharp

## Установка

```bash
dotnet add package Tronzap.Sdk
```

## Требования

- .NET 8 или новее
- Одна зависимость, `Microsoft.Extensions.Http`, для интеграции с `IHttpClientFactory`. SDK не зависит от ASP.NET Core.

## Быстрый старт

```csharp
using Tronzap.Sdk;
using Tronzap.Sdk.Exceptions;
using Tronzap.Sdk.Requests;
using Tronzap.Sdk.Responses;

var client = new TronzapClient(new TronzapClientOptions
{
    ApiToken = "ваш_api_token",
    ApiSecret = "ваш_api_secret",
});

try
{
    AccountBalance balance = await client.GetBalanceAsync(cancellationToken);
    Console.WriteLine($"balance: {balance.Balance} (deposit to {balance.Address})");

    // Оцениваем, сколько энергии нужно для перевода USDT, и покупаем ровно столько.
    EnergyEstimate estimate = await client.EstimateEnergyAsync(
        new EstimateEnergyRequest { FromAddress = "TSenderAddress", ToAddress = "TRecipientAddress" },
        cancellationToken);

    Transaction tx = await client.CreateEnergyTransactionAsync(
        new EnergyTransactionRequest
        {
            Address = "TRecipientAddress",
            Energy = estimate.Energy,
            Duration = 1,
            ExternalId = "order-42",
            ActivateAddress = true,
        },
        cancellationToken);
    Console.WriteLine($"transaction {tx.Id} costs {tx.Amount} and is {tx.Status}");
}
catch (TronzapException e)
{
    Console.Error.WriteLine($"TronZap call failed: {e.Message}");
}
```

Запускаемый пример со всеми операциями находится в
[`examples/BasicUsage.cs`](examples/BasicUsage.cs). Для него нужен .NET 10 SDK:

```bash
export TRONZAP_API_TOKEN=ваш_api_token
export TRONZAP_API_SECRET=ваш_api_secret
export TRONZAP_BASE_URL=api.tronzap.com   # необязательно
dotnet run examples/BasicUsage.cs
```

По умолчанию пример только читает данные и ничего не тратит. С
`TRONZAP_ALLOW_PURCHASES=1` он также вызывает endpoints, которые создают транзакции
и AML-проверки и списывают средства с баланса. Остальные необязательные переменные
описаны в комментарии в начале файла.

## Настройка

`TronzapClientOptions` принимает два ключа из личного кабинета: API-токен
передаётся как bearer token, а API-секрет подписывает тело каждого запроса. Всё
остальное необязательно:

```csharp
var client = new TronzapClient(new TronzapClientOptions
{
    ApiToken = apiToken,
    ApiSecret = apiSecret,
    BaseUrl = "api.tronzap.com",          // по умолчанию TronzapClientOptions.DefaultBaseUrl
    Timeout = TimeSpan.FromSeconds(10),   // весь запрос; по умолчанию 30 секунд
    UserAgent = "my-app/1.0",
});
```

`BaseUrl` принимает домен или полный URL: если схема не указана, используется
`https`, а завершающий слэш удаляется, поэтому `"api.tronzap.com"`,
`"api.tronzap.com/"` и `"https://api.tronzap.com"` равнозначны. Чтобы этого
избежать, укажите схему явно, например `"http://localhost:8080"` для локального
мока.

`TronzapClient` неизменяем, безопасен для конкурентного использования и копирует
свои опции при создании, поэтому создайте один клиент на набор ключей и
используйте его совместно. `Timeout` ограничивает весь запрос, включая медленно
приходящее тело ответа.

### Свой HttpClient

`new TronzapClient(options)` отправляет запросы через один `HttpClient`, общий для
всех клиентов, созданных таким способом, поэтому множество клиентов не исчерпывает
сокеты. Чтобы использовать прокси, собственную проверку сертификатов или свои
handlers, передайте `HttpClient`. Он используется как есть и никогда не
освобождается (dispose) и не изменяется: заголовки задаются для каждого запроса.

```csharp
var handler = new SocketsHttpHandler
{
    Proxy = new WebProxy("http://proxy.internal:3128"),
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
};
var client = new TronzapClient(new HttpClient(handler), options);
```

Если заданы и `TronzapClientOptions.Timeout`, и `HttpClient.Timeout`, действует
меньший из них.

### Внедрение зависимостей

`AddTronzap` регистрирует `ITronzapClient` и `TronzapClient` поверх
`IHttpClientFactory`. Метод возвращает `IHttpClientBuilder`, поэтому handlers и
политики отказоустойчивости подключаются как обычно:

```csharp
builder.Services
    .AddTronzap(options =>
    {
        options.ApiToken = builder.Configuration["Tronzap:ApiToken"]!;
        options.ApiSecret = builder.Configuration["Tronzap:ApiSecret"]!;
    })
    .AddHttpMessageHandler<MyLoggingHandler>();

// или привяжите опции к конфигурации и вызовите AddTronzap() без аргументов:
builder.Services.Configure<TronzapClientOptions>(builder.Configuration.GetSection("Tronzap"));
builder.Services.AddTronzap();
```

Затем внедряйте `ITronzapClient` там, где он нужен. Зависьте от интерфейса, чтобы
в своих тестах подменять клиент на fake.

## Доступные методы

| Метод | Endpoint | Описание |
|---|---|---|
| `GetServicesAsync()` | `/v1/services` | Доступные сервисы и цены |
| `GetBalanceAsync()` | `/v1/balance` | Текущий баланс аккаунта |
| `GetAddressInfoAsync(address)` | `/v1/address-info` | Ресурсы (энергия, bandwidth) и балансы (TRX, USDT) адреса |
| `EstimateEnergyAsync(request)` | `/v1/estimate-energy` | Сколько энергии нужно для перевода и сколько она стоит |
| `CalculateAsync(request)` | `/v1/calculate` | Стоимость покупки без создания транзакции |
| `CreateEnergyTransactionAsync(request)` | `/v1/transaction/new` | Купить энергию |
| `CreateBandwidthTransactionAsync(request)` | `/v1/transaction/new` | Купить bandwidth |
| `CreateResourceBundleTransactionAsync(request)` | `/v1/transaction/new` | Купить энергию и bandwidth одной транзакцией |
| `CreateAddressActivationTransactionAsync(request)` | `/v1/transaction/new` | Активировать адрес TRON |
| `CheckTransactionAsync(request)` | `/v1/transaction/check` | Статус транзакции по id или внешнему id |
| `GetDirectRechargeInfoAsync()` | `/v1/direct-recharge-info` | Адрес и тарифы прямого пополнения |
| `GetAmlServicesAsync()` | `/v1/aml-checks` | AML-сервисы и цены |
| `CreateAmlCheckAsync(request)` | `/v1/aml-checks/new` | Запустить AML-проверку |
| `CheckAmlStatusAsync(id)` | `/v1/aml-checks/check` | Статус и результат AML-проверки |
| `GetAmlHistoryAsync()` / `GetAmlHistoryAsync(request)` | `/v1/aml-checks/history` | История AML-проверок с пагинацией |

Все методы асинхронные и последним параметром принимают необязательный
`CancellationToken`.

Параметры — неизменяемые records в `Tronzap.Sdk.Requests`, которые заполняются
через инициализаторы объектов; обязательные значения — члены `required`. Запрос
проверяется до отправки, поэтому невалидный запрос вызывает `ArgumentException` и
никогда не доходит до API. Значения по умолчанию совпадают с API: `Duration` —
1 час, история AML начинается со страницы 1 по 10 элементов.

Результаты — неизменяемые records в `Tronzap.Sdk.Responses`. Коллекции никогда не
бывают `null`, а значения, которые API может не прислать, — nullable.

### Покупка ресурсов

```csharp
// Энергия, при необходимости с активацией адреса в том же вызове.
Transaction tx = await client.CreateEnergyTransactionAsync(new EnergyTransactionRequest
{
    Address = "TRecipientAddress",
    Energy = 65000,
    Duration = 1,          // часы; доступные сроки смотрите в GetServicesAsync()
    ExternalId = "order-42",
    ActivateAddress = true,
});

// Bandwidth.
tx = await client.CreateBandwidthTransactionAsync(new BandwidthTransactionRequest
{
    Address = "TRecipientAddress",
    Bandwidth = 345,
    ExternalId = "bandwidth-1",
});

// Энергия и bandwidth вместе одной транзакцией.
tx = await client.CreateResourceBundleTransactionAsync(new ResourceBundleTransactionRequest
{
    Address = "TRecipientAddress",
    Energy = 65000,
    Bandwidth = 345,
    ExternalId = "bundle-1",
});

// Только активация.
tx = await client.CreateAddressActivationTransactionAsync(new AddressActivationRequest
{
    Address = "TRecipientAddress",
    ExternalId = "activation-1",
});
```

Цена энергии указана за единицу, цена bandwidth — за 1000 единиц: в
`GetServicesAsync()` `EnergyRate.Price` × 65000 — это стоимость 65000 энергии, а
345 bandwidth при `BandwidthRate.Price`, равном 1, стоят 0.345.

Сейчас API возвращает пакет ресурсов с `Service`, равным `ServiceType.Energy`, а не
`ServiceType.ResourceBundle`. Состав покупки смотрите в `Params.Amounts`.

### Отслеживание транзакции

Транзакция проходит путь `New` → `Pending` → `Success` или `Failed`:

```csharp
Transaction tx;
do
{
    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    tx = await client.CheckTransactionAsync(CheckTransactionRequest.ByExternalId("order-42"), cancellationToken);
}
while (tx.Status is TransactionStatus.New or TransactionStatus.Pending);

Console.WriteLine($"finished as {tx.Status}, hash {tx.Hash ?? "none"}");
```

### AML-проверка

```csharp
AmlCheck check = await client.CreateAmlCheckAsync(AmlCheckRequest.ForAddress("TRX", "TAddressToScreen"));
// или AmlCheckRequest.ForHash("BTC", "bc1RecipientAddress", "TX_HASH", AmlDirection.Withdrawal)

AmlCheck result = await client.CheckAmlStatusAsync(check.Id);
if (result.Status == AmlStatus.Completed)
{
    Console.WriteLine($"{result.RiskLevel} {result.RiskScore} {result.RiskFactors.Count} factor(s)");
}
```

`RiskScore` равен `null`, пока проверка не завершится. У завершённой проверки score
может быть равен 0, и это не то же самое, что отсутствие результата.

## Обработка ошибок

Любой сбой вызова API — исключение `TronzapException`. Ловите подкласс, чтобы
обработать конкретный вид сбоя:

```
TronzapException
├── TronzapApiException                — API ответил ненулевым code
├── TronzapHttpException               — ответ не 2xx без payload API
│   ├── TronzapRateLimitException      — HTTP 429
│   ├── TronzapUnauthorizedException   — HTTP 401 или 403
│   └── TronzapServerException         — HTTP 5xx
├── TronzapInvalidResponseException    — ответ 2xx, который SDK не смог прочитать
└── TronzapNetworkException            — ответ не пришёл
    ├── TronzapConnectionException     — ошибка DNS, соединение отклонено
    ├── TronzapTimeoutException        — запрос превысил timeout
    └── TronzapSslException            — сбой TLS-рукопожатия или сертификата
```

`TronzapApiException`, `TronzapHttpException` и `TronzapInvalidResponseException`
содержат HTTP-статус (`StatusCode`) и сырое тело ответа (`ResponseBody`).
`TronzapApiException` также содержит код ошибки API, ключ ошибки и ID запроса.
`TronzapRateLimitException.RetryAfter` хранит задержку из `Retry-After`, если API
её прислал.

Два вида сбоев не являются `TronzapException`: невалидные аргументы вызывают
`ArgumentException` ещё до отправки, а отмена `CancellationToken` вызывает
`OperationCanceledException`, как и везде в .NET. Таймаут всегда приходит как
`TronzapTimeoutException`, а не как `OperationCanceledException`.

```csharp
try
{
    await client.CreateEnergyTransactionAsync(
        new EnergyTransactionRequest { Address = "TRecipientAddress", Energy = 65000 },
        cancellationToken);
}
catch (TronzapApiException e)
{
    // Ошибка уровня приложения: код точно говорит, что пошло не так.
    switch (e.ErrorCode)
    {
        case TronzapErrorCode.InvalidTronAddress:
            // Ключ может уточнить, например "invalid_tron_address.from_address"
            Console.Error.WriteLine($"bad address: {e.ErrorKey}");
            break;
        case TronzapErrorCode.InsufficientFunds:
            Console.Error.WriteLine("top up the account");
            break;
        case TronzapErrorCode.AddressNotActivated:
            Console.Error.WriteLine("activate the address first");
            break;
        default:
            Console.Error.WriteLine($"api error {e.Code}: {e.Message} (request {e.RequestId ?? "-"})");
            break;
    }
}
catch (TronzapRateLimitException e)
{
    // Подождите и повторите, через e.RetryAfter, если API его прислал.
}
catch (TronzapUnauthorizedException)
{
    // Неверный токен или подпись.
}
catch (Exception e) when (e is TronzapTimeoutException or TronzapServerException)
{
    // Временный сбой; можно повторить.
}
catch (TronzapNetworkException)
{
    // Сервер недоступен.
}
```

`RequestId` — идентификатор, который API присваивает каждому запросу. Указывайте
его при обращении в поддержку.

Ошибка API важнее HTTP-статуса: часть сбоев API возвращает со статусом 2xx, а
часть — с 4xx или 5xx, поэтому читаемый payload с ненулевым кодом всегда
сообщается как `TronzapApiException`, а не как `TronzapHttpException`.

### Коды ошибок API

| Код | Константа | Описание |
|------|----------|-------------|
| 1 | `AuthError` | Ошибка аутентификации: неверный API-токен или подпись |
| 2 | `InvalidServiceOrParams` | Неверный сервис или параметры |
| 5 | `WalletNotFound` | Внутренний кошелёк не найден. Обратитесь в поддержку. |
| 6 | `InsufficientFunds` | Недостаточно средств |
| 10 | `InvalidTronAddress` | Неверный адрес TRON |
| 11 | `InvalidEnergyAmount` | Неверное количество энергии |
| 12 | `InvalidDuration` | Неверная длительность |
| 20 | `TransactionNotFound` | Транзакция/подписка не найдена |
| 21 | `CannotStopSubscription` | Невозможно остановить подписку |
| 24 | `AddressNotActivated` | Адрес не активирован |
| 25 | `AddressAlreadyActivated` | Адрес уже активирован |
| 30 | `AmlCheckNotFound` | AML-проверка не найдена |
| 35 | `ServiceNotAvailable` | Сервис недоступен |
| 50 | `InvalidBandwidthAmount` | Неверное количество bandwidth |
| 500 | `InternalServerError` | Внутренняя ошибка сервера: обратитесь в поддержку |

Константы — значения enum `TronzapErrorCode`. Код, который эта версия SDK не
знает, сообщается как `TronzapErrorCode.Unknown`, а число по-прежнему доступно
через `Code`.

## Числовые поля и даты

Суммы и цены — `decimal`, поэтому сохраняют ровно то значение, которое прислал
API. В одних ответах API кодирует деньги JSON-числом, в других — JSON-строкой; обе
формы читаются одинаково.

Даты — records `Timestamp`: `Value` — разобранный `DateTimeOffset`, `Raw` — текст
ровно в том виде, в каком его прислал API. Поддерживаются все форматы, которые
использует API, а время без смещения читается как UTC. Нераспознанная дата
оставляет `Value` равным `null` и не ломает весь ответ.

Значения, которые API может добавить в будущем, например новый статус транзакции,
сообщаются как член `Unknown` соответствующего enum и не приводят к ошибке.

## Тестирование

```bash
dotnet test
```

Запускает unit-тесты на .NET 8 и .NET 10 против локального HTTP-сервера: точное
тело запроса и подпись для каждого endpoint, ошибки API и HTTP, некорректный
JSON, таймауты, отмену, сетевые и TLS-сбои, а также конкурентное использование.

## Лицензия

Лицензия MIT (MIT). Подробнее в [файле лицензии](LICENSE).

## Поддержка

По вопросам поддержки пишите на [support@tronzap.com](mailto:support@tronzap.com).
