# Alquiler de Energía Tron vía API
## SDK .NET por TronZap.com

[English](README.md) | **[Español](README.es.md)** | [Português](README.pt-br.md) | [Русский](README.ru.md)

[![NuGet](https://img.shields.io/nuget/v/Tronzap.Sdk.svg)](https://www.nuget.org/packages/Tronzap.Sdk)
[![CI](https://github.com/tron-energy-market/tronzap-sdk-csharp/actions/workflows/ci.yml/badge.svg)](https://github.com/tron-energy-market/tronzap-sdk-csharp/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

SDK oficial en .NET para la API de TronZap.
Este SDK permite integrar fácilmente los servicios de TronZap para alquilar energía TRON.

TronZap.com permite [comprar energía TRON](https://tronzap.com/), reduciendo significativamente las comisiones en transferencias de USDT (TRC20).

👉 [Regístrate para obtener una clave API](https://tronzap.com) para comenzar a usar la API de TronZap e integrarla a través del SDK.

- Sitio web: https://tronzap.com/
- Referencia de la API: https://docs.tronzap.com/
- NuGet: https://www.nuget.org/packages/Tronzap.Sdk
- Código fuente: https://github.com/tron-energy-market/tronzap-sdk-csharp

## Instalación

```bash
dotnet add package Tronzap.Sdk
```

## Requisitos

- .NET 8 o superior
- Una sola dependencia, `Microsoft.Extensions.Http`, para la integración con `IHttpClientFactory`. El SDK no depende de ASP.NET Core.

## Inicio rápido

```csharp
using Tronzap.Sdk;
using Tronzap.Sdk.Exceptions;
using Tronzap.Sdk.Requests;
using Tronzap.Sdk.Responses;

var client = new TronzapClient(new TronzapClientOptions
{
    ApiToken = "su_api_token",
    ApiSecret = "su_api_secret",
});

try
{
    AccountBalance balance = await client.GetBalanceAsync(cancellationToken);
    Console.WriteLine($"balance: {balance.Balance} (deposit to {balance.Address})");

    // Estima cuánta energía necesita una transferencia de USDT y compra exactamente esa cantidad.
    EnergyEstimate estimate = await client.EstimateEnergyAsync(
        new EstimateEnergyRequest { FromAddress = "TSenderAddress", ToAddress = "TRecipientAddress" },
        cancellationToken);

    Transaction tx = await client.CreateEnergyTransactionAsync(
        new EnergyTransactionRequest
        {
            Address = "TRecipientAddress",
            Energy = estimate.Amount,
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

Un recorrido ejecutable por todas las operaciones está en
[`examples/BasicUsage.cs`](examples/BasicUsage.cs). Requiere el SDK de .NET 10:

```bash
export TRONZAP_API_TOKEN=su_api_token
export TRONZAP_API_SECRET=su_api_secret
export TRONZAP_BASE_URL=api.tronzap.com   # opcional
dotnet run examples/BasicUsage.cs
```

Por defecto solo lee y no gasta nada. Con `TRONZAP_ALLOW_PURCHASES=1` también
ejecuta los endpoints que crean transacciones y verificaciones AML, que debitan el
saldo de la cuenta. Consulta el comentario al inicio del archivo para ver las
demás variables opcionales.

## Configuración

`TronzapClientOptions` recibe las dos credenciales de tu panel: el token de API se
envía como bearer token y el secreto de API firma el cuerpo de cada solicitud.
Todo lo demás es opcional:

```csharp
var client = new TronzapClient(new TronzapClientOptions
{
    ApiToken = apiToken,
    ApiSecret = apiSecret,
    BaseUrl = "api.tronzap.com",          // por defecto TronzapClientOptions.DefaultBaseUrl
    Timeout = TimeSpan.FromSeconds(10),   // toda la solicitud; por defecto 30 segundos
    UserAgent = "my-app/1.0",
});
```

`BaseUrl` acepta un dominio o una URL completa: si falta el esquema se usa
`https` y se elimina la barra final, así que `"api.tronzap.com"`,
`"api.tronzap.com/"` y `"https://api.tronzap.com"` son equivalentes. Indica un
esquema explícito para evitarlo, por ejemplo `"http://localhost:8080"` con un mock
local.

Un `TronzapClient` es inmutable, seguro para uso concurrente y copia sus opciones
al crearse, así que crea uno por cada juego de credenciales y compártelo. `Timeout`
limita toda la solicitud, incluido un cuerpo de respuesta que llega lentamente.

### Tu propio HttpClient

`new TronzapClient(options)` envía las solicitudes a través de un único `HttpClient`
compartido por todos los clientes creados así, de modo que crear muchos clientes no
agota los sockets. Para usar un proxy, una validación de certificados personalizada
o tus propios handlers, pasa un `HttpClient`. Se usa tal cual y nunca se libera ni
se modifica: los encabezados se establecen en cada solicitud.

```csharp
var handler = new SocketsHttpHandler
{
    Proxy = new WebProxy("http://proxy.internal:3128"),
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
};
var client = new TronzapClient(new HttpClient(handler), options);
```

Cuando se configuran tanto `TronzapClientOptions.Timeout` como `HttpClient.Timeout`,
se aplica el más corto.

### Inyección de dependencias

`AddTronzap` registra `ITronzapClient` y `TronzapClient` sobre
`IHttpClientFactory`. Devuelve el `IHttpClientBuilder`, así que los handlers y las
políticas de resiliencia se añaden como de costumbre:

```csharp
builder.Services
    .AddTronzap(options =>
    {
        options.ApiToken = builder.Configuration["Tronzap:ApiToken"]!;
        options.ApiSecret = builder.Configuration["Tronzap:ApiSecret"]!;
    })
    .AddHttpMessageHandler<MyLoggingHandler>();

// o vincula las opciones desde la configuración y llama a AddTronzap() sin argumentos:
builder.Services.Configure<TronzapClientOptions>(builder.Configuration.GetSection("Tronzap"));
builder.Services.AddTronzap();
```

Después inyecta `ITronzapClient` donde lo necesites. Depende de la interfaz para
sustituir el cliente por uno falso en tus propias pruebas.

## Métodos disponibles

| Método | Endpoint | Descripción |
|---|---|---|
| `GetServicesAsync()` | `/v1/services` | Servicios disponibles y precios |
| `GetBalanceAsync()` | `/v1/balance` | Saldo actual de la cuenta |
| `GetAddressInfoAsync(address)` | `/v1/address-info` | Recursos (energía, ancho de banda) y saldos (TRX, USDT) de una dirección |
| `EstimateEnergyAsync(request)` | `/v1/estimate-energy` | Energía que necesita una transferencia y su coste |
| `CalculateAsync(request)` | `/v1/calculate` | Precio de una compra sin crear la transacción |
| `CreateEnergyTransactionAsync(request)` | `/v1/transaction/new` | Comprar energía |
| `CreateBandwidthTransactionAsync(request)` | `/v1/transaction/new` | Comprar ancho de banda |
| `CreateResourceBundleTransactionAsync(request)` | `/v1/transaction/new` | Comprar energía y ancho de banda en una sola transacción |
| `CreateAddressActivationTransactionAsync(request)` | `/v1/transaction/new` | Activar una dirección TRON |
| `CheckTransactionAsync(request)` | `/v1/transaction/check` | Estado de una transacción, por id o id externo |
| `GetDirectRechargeInfoAsync()` | `/v1/direct-recharge-info` | Dirección y tarifas de recarga directa |
| `GetAmlServicesAsync()` | `/v1/aml-checks` | Servicios AML y precios |
| `CreateAmlCheckAsync(request)` | `/v1/aml-checks/new` | Iniciar una verificación AML |
| `CheckAmlStatusAsync(id)` | `/v1/aml-checks/check` | Estado y resultado de una verificación AML |
| `GetAmlHistoryAsync()` / `GetAmlHistoryAsync(request)` | `/v1/aml-checks/history` | Historial paginado de verificaciones AML |
| `GetSubscriptionsAsync()` | `/v1/subscriptions` | Planes de suscripción y precios |
| `StartSubscriptionAsync(request)` | `/v1/subscription/start` | Suscribir una dirección a un plan |
| `CheckSubscriptionAsync(request)` | `/v1/subscription/check` | Estado de una suscripción, por id o id externo |
| `StopSubscriptionAsync(request)` | `/v1/subscription/stop` | Detener una suscripción |
| `GetSubscriptionHistoryAsync()` / `GetSubscriptionHistoryAsync(request)` | `/v1/subscriptions/history` | Historial paginado de suscripciones |

Todos los métodos son asíncronos y aceptan un `CancellationToken` opcional como
último parámetro.

Los parámetros son records inmutables en `Tronzap.Sdk.Requests`, que se rellenan
con inicializadores de objeto; los valores obligatorios son miembros `required`.
Una solicitud se valida antes de enviarse, así que una solicitud inválida lanza
`ArgumentException` y nunca llega a la API. Los valores por defecto coinciden con
la API: `Duration` es 1 hora y los historiales AML y de suscripciones empiezan
en la página 1 con 10 elementos. En `StartSubscriptionRequest`, un `DurationDays`
o `TransactionsLimit` igual a cero significa sin límite.

Los resultados son records inmutables en `Tronzap.Sdk.Responses`. Las colecciones
nunca son `null`, y los valores que la API puede omitir son nullable.

### Comprar recursos

```csharp
// Energía, con activación opcional de la dirección en la misma llamada.
Transaction tx = await client.CreateEnergyTransactionAsync(new EnergyTransactionRequest
{
    Address = "TRecipientAddress",
    Energy = 65000,
    Duration = 1,          // horas; consulte GetServicesAsync() para las duraciones disponibles
    ExternalId = "order-42",
    ActivateAddress = true,
});

// Ancho de banda.
tx = await client.CreateBandwidthTransactionAsync(new BandwidthTransactionRequest
{
    Address = "TRecipientAddress",
    Bandwidth = 345,
    ExternalId = "bandwidth-1",
});

// Energía y ancho de banda juntos en una sola transacción.
tx = await client.CreateResourceBundleTransactionAsync(new ResourceBundleTransactionRequest
{
    Address = "TRecipientAddress",
    Energy = 65000,
    Bandwidth = 345,
    ExternalId = "bundle-1",
});

// Solo la activación.
tx = await client.CreateAddressActivationTransactionAsync(new AddressActivationRequest
{
    Address = "TRecipientAddress",
    ExternalId = "activation-1",
});
```

Los precios de la energía y del ancho de banda son por 1000 unidades, así que el
coste es precio × cantidad / 1000: en `GetServicesAsync()`, 65000 de energía con un
`EnergyRate.Price` de 0.03 cuestan 1.95 (lo mismo que `EnergyRate.Price65K`), y 345
de ancho de banda con un `BandwidthRate.Price` de 1 cuestan 0.345.

Actualmente la API informa un paquete de recursos con `Service` igual a
`ServiceType.Energy`, no `ServiceType.ResourceBundle`. Consulta `Params.Amounts`
para saber qué recursos contiene una transacción.

### Seguir una transacción

Una transacción pasa por `New` → `Pending` → `Success` o `Failed`:

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

### Verificación AML

```csharp
AmlCheck check = await client.CreateAmlCheckAsync(AmlCheckRequest.ForAddress("TRX", "TAddressToScreen"));
// o AmlCheckRequest.ForHash("BTC", "bc1RecipientAddress", "TX_HASH", AmlDirection.Withdrawal)

AmlCheck result = await client.CheckAmlStatusAsync(check.Id);
if (result.Status == AmlStatus.Completed)
{
    Console.WriteLine($"{result.RiskLevel} {result.RiskScore} {result.RiskFactors.Count} factor(s)");
}
```

`RiskScore` es `null` hasta que termina la verificación. Una verificación
completada puede tener una puntuación de 0, que no es lo mismo que no tener
puntuación todavía.

### Suscripciones

Una suscripción mantiene una dirección abastecida de energía para cada transacción
hasta que se detiene o se agotan sus días o transacciones. Elija un plan de
`GetSubscriptionsAsync()` y pase su `SubscriptionId`, como `"unlimited_energy"`,
no su `Id` numérico:

```csharp
IReadOnlyList<SubscriptionPlan> plans = await client.GetSubscriptionsAsync();
foreach (SubscriptionPlan plan in plans)
{
    Console.WriteLine($"{plan.SubscriptionId} {plan.InitialPrice} {plan.Price}");
}

Subscription subscription = await client.StartSubscriptionAsync(new StartSubscriptionRequest
{
    SubscriptionId = "unlimited_energy",
    Address = "TRecipientAddress",
    DurationDays = 30,     // 0 para sin límite de tiempo
    TransactionsLimit = 0, // 0 para sin límite
    ExternalId = "subscription-42",
});

subscription = await client.CheckSubscriptionAsync(SubscriptionRequest.ByExternalId("subscription-42"));

subscription = await client.StopSubscriptionAsync(SubscriptionRequest.ById(subscription.Id));

SubscriptionHistory history = await client.GetSubscriptionHistoryAsync(
    new SubscriptionHistoryRequest { Status = SubscriptionStatus.Active });
```

Iniciar, consultar y detener devuelven la suscripción con sus `Params`; el
historial devuelve en su lugar los contadores de uso `TransactionsUsed`,
`EnergyUsed` y `TotalPrice`, con `Params` en `null`. Iniciar una suscripción cobra
el precio inicial del plan. Una suscripción con límite de transacciones no se
puede detener (`TronzapErrorCode.CannotStopSubscription`).

## Gestión de errores

Todo fallo de una llamada a la API es una `TronzapException`. Captura una subclase
para tratar un tipo concreto de fallo:

```
TronzapException
├── TronzapApiException                — la API respondió con un código distinto de cero
├── TronzapHttpException               — respuesta no 2xx sin payload de la API
│   ├── TronzapRateLimitException      — HTTP 429
│   ├── TronzapUnauthorizedException   — HTTP 401 o 403
│   └── TronzapServerException         — HTTP 5xx
├── TronzapInvalidResponseException    — respuesta 2xx que el SDK no pudo leer
└── TronzapNetworkException            — no llegó ninguna respuesta
    ├── TronzapConnectionException     — fallo de DNS, conexión rechazada
    ├── TronzapTimeoutException        — la solicitud superó su timeout
    └── TronzapSslException            — fallo del handshake TLS o del certificado
```

`TronzapApiException`, `TronzapHttpException` y `TronzapInvalidResponseException`
incluyen el estado HTTP (`StatusCode`) y el cuerpo de la respuesta sin procesar
(`ResponseBody`). `TronzapApiException` incluye además el código de error de la API,
la clave del error y el ID de la solicitud. `TronzapRateLimitException.RetryAfter`
contiene el retraso de `Retry-After` cuando la API lo envía.

Hay dos fallos que no son `TronzapException`: los argumentos inválidos lanzan
`ArgumentException` antes de enviar nada, y cancelar el `CancellationToken` lanza
`OperationCanceledException`, como en todo .NET. Un timeout siempre es
`TronzapTimeoutException`, nunca `OperationCanceledException`.

```csharp
try
{
    await client.CreateEnergyTransactionAsync(
        new EnergyTransactionRequest { Address = "TRecipientAddress", Energy = 65000 },
        cancellationToken);
}
catch (TronzapApiException e)
{
    // Fallo a nivel de aplicación: el código indica exactamente qué salió mal.
    switch (e.ErrorCode)
    {
        case TronzapErrorCode.InvalidTronAddress:
            // La clave puede precisarlo, p. ej. "invalid_tron_address.from_address"
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
    // Espera y reintenta, tras e.RetryAfter si la API lo envió.
}
catch (TronzapUnauthorizedException)
{
    // Token o firma incorrectos.
}
catch (Exception e) when (e is TronzapTimeoutException or TronzapServerException)
{
    // Transitorio; se puede reintentar.
}
catch (TronzapNetworkException)
{
    // Inalcanzable.
}
```

`RequestId` es el identificador que la API asigna a cada solicitud. Indícalo al
contactar con soporte.

Un error de la API tiene prioridad sobre el estado HTTP: la API informa de
algunos fallos con estado 2xx y de otros con 4xx o 5xx, así que un payload legible
con un código distinto de cero siempre se informa como `TronzapApiException`, nunca
como `TronzapHttpException`.

### Códigos de error de la API

| Código | Constante | Descripción |
|------|----------|-------------|
| 1 | `AuthError` | Error de autenticación: token de API o firma inválidos |
| 2 | `InvalidServiceOrParams` | Servicio o parámetros inválidos |
| 5 | `WalletNotFound` | Billetera interna no encontrada. Contacta con soporte. |
| 6 | `InsufficientFunds` | Fondos insuficientes |
| 10 | `InvalidTronAddress` | Dirección TRON inválida, o la dirección ya tiene una suscripción activa |
| 11 | `InvalidEnergyAmount` | Cantidad de energía inválida |
| 12 | `InvalidDuration` | Duración inválida |
| 20 | `TransactionNotFound` | Transacción/suscripción no encontrada |
| 21 | `CannotStopSubscription` | No se puede detener la suscripción, p. ej. tiene límite de transacciones |
| 24 | `AddressNotActivated` | Dirección no activada |
| 25 | `AddressAlreadyActivated` | Dirección ya activada |
| 30 | `AmlCheckNotFound` | Verificación AML no encontrada |
| 35 | `ServiceNotAvailable` | Servicio no disponible |
| 50 | `InvalidBandwidthAmount` | Cantidad de ancho de banda inválida |
| 500 | `InternalServerError` | Error interno del servidor: contacta con soporte |

Las constantes son valores del enum `TronzapErrorCode`. Un código que esta versión
del SDK no conoce se informa como `TronzapErrorCode.Unknown`, y el número sigue
disponible en `Code`.

## Campos decimales y de fecha

Los importes y precios son `decimal`, así que conservan el valor exacto que envió
la API. La API codifica el dinero como número JSON en algunas respuestas y como
cadena JSON en otras; ambas formas se leen igual.

Las fechas son records `Timestamp`: `Value` es el `DateTimeOffset` interpretado y
`Raw` es el texto tal como lo envió la API. Se aceptan los distintos formatos que
usa la API, y las horas sin desplazamiento se leen como UTC. Una fecha no
reconocida deja `Value` como `null` en lugar de hacer fallar toda la respuesta.

Los valores que la API pueda añadir en el futuro, como un nuevo estado de
transacción, se informan como el miembro `Unknown` del enum correspondiente en
lugar de fallar.

## Pruebas

```bash
dotnet test
```

Ejecuta las pruebas unitarias en .NET 8 y .NET 10 contra un servidor HTTP local: el
cuerpo exacto de la solicitud y la firma de cada endpoint, errores de la API y
HTTP, JSON malformado, timeouts, cancelación, fallos de red y de TLS, y uso
concurrente.

## Licencia

Licencia MIT (MIT). Consulta el [archivo de licencia](LICENSE) para más información.

## Soporte

Para soporte, contacta con [support@tronzap.com](mailto:support@tronzap.com).
