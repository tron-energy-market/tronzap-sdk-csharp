# Aluguel de Energia Tron via API
## SDK .NET por TronZap.com

[English](README.md) | [Español](README.es.md) | **[Português](README.pt-br.md)** | [Русский](README.ru.md)

[![NuGet](https://img.shields.io/nuget/v/Tronzap.Sdk.svg)](https://www.nuget.org/packages/Tronzap.Sdk)
[![CI](https://github.com/tron-energy-market/tronzap-sdk-csharp/actions/workflows/ci.yml/badge.svg)](https://github.com/tron-energy-market/tronzap-sdk-csharp/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

SDK oficial em .NET para a API do TronZap.
Este SDK permite integrar facilmente os serviços TronZap para aluguel de energia TRON.

TronZap.com permite [comprar energia TRON](https://tronzap.com/), reduzindo significativamente as taxas nas transferências de USDT (TRC20).

👉 [Registre-se para obter uma chave API](https://tronzap.com) para começar a usar a API TronZap e integrá-la através do SDK.

- Site: https://tronzap.com/
- Referência da API: https://docs.tronzap.com/
- NuGet: https://www.nuget.org/packages/Tronzap.Sdk
- Código-fonte: https://github.com/tron-energy-market/tronzap-sdk-csharp

## Instalação

```bash
dotnet add package Tronzap.Sdk
```

## Requisitos

- .NET 8 ou superior
- Uma única dependência, `Microsoft.Extensions.Http`, para a integração com `IHttpClientFactory`. O SDK não depende do ASP.NET Core.

## Início rápido

```csharp
using Tronzap.Sdk;
using Tronzap.Sdk.Exceptions;
using Tronzap.Sdk.Requests;
using Tronzap.Sdk.Responses;

var client = new TronzapClient(new TronzapClientOptions
{
    ApiToken = "seu_api_token",
    ApiSecret = "seu_api_secret",
});

try
{
    AccountBalance balance = await client.GetBalanceAsync(cancellationToken);
    Console.WriteLine($"balance: {balance.Balance} (deposit to {balance.Address})");

    // Estima quanta energia uma transferência de USDT precisa e compra exatamente essa quantidade.
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

Um passo a passo executável de todas as operações está em
[`examples/BasicUsage.cs`](examples/BasicUsage.cs). Ele requer o SDK do .NET 10:

```bash
export TRONZAP_API_TOKEN=seu_api_token
export TRONZAP_API_SECRET=seu_api_secret
export TRONZAP_BASE_URL=api.tronzap.com   # opcional
dotnet run examples/BasicUsage.cs
```

Por padrão ele apenas lê e não gasta nada. Com `TRONZAP_ALLOW_PURCHASES=1` ele
também executa os endpoints que criam transações e verificações AML, que debitam o
saldo da conta. Veja o comentário no início do arquivo para as demais variáveis
opcionais.

## Configuração

`TronzapClientOptions` recebe as duas credenciais do seu painel: o token da API é
enviado como bearer token e o segredo da API assina o corpo de cada requisição.
Todo o resto é opcional:

```csharp
var client = new TronzapClient(new TronzapClientOptions
{
    ApiToken = apiToken,
    ApiSecret = apiSecret,
    BaseUrl = "api.tronzap.com",          // padrão: TronzapClientOptions.DefaultBaseUrl
    Timeout = TimeSpan.FromSeconds(10),   // requisição inteira; padrão: 30 segundos
    UserAgent = "my-app/1.0",
});
```

`BaseUrl` aceita um domínio ou uma URL completa: sem esquema, usa-se `https`, e a
barra final é removida, então `"api.tronzap.com"`, `"api.tronzap.com/"` e
`"https://api.tronzap.com"` são equivalentes. Informe um esquema explícito para
evitar isso, por exemplo `"http://localhost:8080"` com um mock local.

Um `TronzapClient` é imutável, seguro para uso concorrente e copia suas opções ao
ser criado, então crie um por conjunto de credenciais e compartilhe-o. `Timeout`
limita a requisição inteira, incluindo um corpo de resposta que chega lentamente.

### Seu próprio HttpClient

`new TronzapClient(options)` envia as requisições por um único `HttpClient`
compartilhado por todos os clientes criados dessa forma, então criar muitos
clientes não esgota os sockets. Para usar um proxy, uma validação de certificados
personalizada ou seus próprios handlers, passe um `HttpClient`. Ele é usado como
está e nunca é descartado nem modificado: os cabeçalhos são definidos a cada
requisição.

```csharp
var handler = new SocketsHttpHandler
{
    Proxy = new WebProxy("http://proxy.internal:3128"),
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
};
var client = new TronzapClient(new HttpClient(handler), options);
```

Quando `TronzapClientOptions.Timeout` e `HttpClient.Timeout` estão ambos
definidos, vale o menor.

### Injeção de dependência

`AddTronzap` registra `ITronzapClient` e `TronzapClient` sobre
`IHttpClientFactory`. Ele retorna o `IHttpClientBuilder`, então handlers e
políticas de resiliência se encaixam como de costume:

```csharp
builder.Services
    .AddTronzap(options =>
    {
        options.ApiToken = builder.Configuration["Tronzap:ApiToken"]!;
        options.ApiSecret = builder.Configuration["Tronzap:ApiSecret"]!;
    })
    .AddHttpMessageHandler<MyLoggingHandler>();

// ou vincule as opções à configuração e chame AddTronzap() sem argumentos:
builder.Services.Configure<TronzapClientOptions>(builder.Configuration.GetSection("Tronzap"));
builder.Services.AddTronzap();
```

Depois injete `ITronzapClient` onde precisar. Dependa da interface para substituir
o cliente por um fake nos seus próprios testes.

## Métodos disponíveis

| Método | Endpoint | Descrição |
|---|---|---|
| `GetServicesAsync()` | `/v1/services` | Serviços disponíveis e preços |
| `GetBalanceAsync()` | `/v1/balance` | Saldo atual da conta |
| `GetAddressInfoAsync(address)` | `/v1/address-info` | Recursos (energia, largura de banda) e saldos (TRX, USDT) de um endereço |
| `EstimateEnergyAsync(request)` | `/v1/estimate-energy` | Energia necessária para uma transferência e seu custo |
| `CalculateAsync(request)` | `/v1/calculate` | Preço de uma compra sem criar a transação |
| `CreateEnergyTransactionAsync(request)` | `/v1/transaction/new` | Comprar energia |
| `CreateBandwidthTransactionAsync(request)` | `/v1/transaction/new` | Comprar largura de banda |
| `CreateResourceBundleTransactionAsync(request)` | `/v1/transaction/new` | Comprar energia e largura de banda em uma única transação |
| `CreateAddressActivationTransactionAsync(request)` | `/v1/transaction/new` | Ativar um endereço TRON |
| `CheckTransactionAsync(request)` | `/v1/transaction/check` | Status de uma transação, por id ou id externo |
| `GetDirectRechargeInfoAsync()` | `/v1/direct-recharge-info` | Endereço e tarifas de recarga direta |
| `GetAmlServicesAsync()` | `/v1/aml-checks` | Serviços AML e preços |
| `CreateAmlCheckAsync(request)` | `/v1/aml-checks/new` | Iniciar uma verificação AML |
| `CheckAmlStatusAsync(id)` | `/v1/aml-checks/check` | Status e resultado de uma verificação AML |
| `GetAmlHistoryAsync()` / `GetAmlHistoryAsync(request)` | `/v1/aml-checks/history` | Histórico paginado de verificações AML |
| `GetSubscriptionsAsync()` | `/v1/subscriptions` | Planos de assinatura e preços |
| `StartSubscriptionAsync(request)` | `/v1/subscription/start` | Assinar um plano para um endereço |
| `CheckSubscriptionAsync(request)` | `/v1/subscription/check` | Status de uma assinatura, por id ou id externo |
| `StopSubscriptionAsync(request)` | `/v1/subscription/stop` | Parar uma assinatura |
| `GetSubscriptionHistoryAsync()` / `GetSubscriptionHistoryAsync(request)` | `/v1/subscriptions/history` | Histórico paginado de assinaturas |

Todos os métodos são assíncronos e aceitam um `CancellationToken` opcional como
último parâmetro.

Os parâmetros são records imutáveis em `Tronzap.Sdk.Requests`, preenchidos com
inicializadores de objeto; os valores obrigatórios são membros `required`. Uma
requisição é validada antes de ser enviada, então uma requisição inválida lança
`ArgumentException` e nunca chega à API. Os padrões coincidem com a API:
`Duration` é 1 hora e os históricos AML e de assinaturas começam na página 1
com 10 itens. Em `StartSubscriptionRequest`, `DurationDays` ou `TransactionsLimit`
igual a zero significa sem limite.

Os resultados são records imutáveis em `Tronzap.Sdk.Responses`. As coleções nunca
são `null`, e os valores que a API pode omitir são nullable.

### Comprar recursos

```csharp
// Energia, com ativação opcional do endereço na mesma chamada.
Transaction tx = await client.CreateEnergyTransactionAsync(new EnergyTransactionRequest
{
    Address = "TRecipientAddress",
    Energy = 65000,
    Duration = 1,          // horas; veja GetServicesAsync() para as durações disponíveis
    ExternalId = "order-42",
    ActivateAddress = true,
});

// Largura de banda.
tx = await client.CreateBandwidthTransactionAsync(new BandwidthTransactionRequest
{
    Address = "TRecipientAddress",
    Bandwidth = 345,
    ExternalId = "bandwidth-1",
});

// Energia e largura de banda juntas em uma única transação.
tx = await client.CreateResourceBundleTransactionAsync(new ResourceBundleTransactionRequest
{
    Address = "TRecipientAddress",
    Energy = 65000,
    Bandwidth = 345,
    ExternalId = "bundle-1",
});

// Apenas a ativação.
tx = await client.CreateAddressActivationTransactionAsync(new AddressActivationRequest
{
    Address = "TRecipientAddress",
    ExternalId = "activation-1",
});
```

Os preços de energia e de largura de banda são por 1000 unidades, então o custo é
preço × quantidade / 1000: em `GetServicesAsync()`, 65000 de energia com um
`EnergyRate.Price` de 0.03 custam 1.95 (o mesmo que `EnergyRate.Price65K`), e 345
de largura de banda com um `BandwidthRate.Price` de 1 custam 0.345.

Atualmente a API informa um pacote de recursos com `Service` igual a
`ServiceType.Energy`, e não `ServiceType.ResourceBundle`. Consulte `Params.Amounts`
para saber quais recursos uma transação contém.

### Acompanhar uma transação

Uma transação passa por `New` → `Pending` → `Success` ou `Failed`:

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

### Verificação AML

```csharp
AmlCheck check = await client.CreateAmlCheckAsync(AmlCheckRequest.ForAddress("TRX", "TAddressToScreen"));
// ou AmlCheckRequest.ForHash("BTC", "bc1RecipientAddress", "TX_HASH", AmlDirection.Withdrawal)

AmlCheck result = await client.CheckAmlStatusAsync(check.Id);
if (result.Status == AmlStatus.Completed)
{
    Console.WriteLine($"{result.RiskLevel} {result.RiskScore} {result.RiskFactors.Count} factor(s)");
}
```

Em uma verificação por hash, `Address` é o endereço do destinatário da
transação, onde os fundos foram recebidos, e a direção indica de que lado você
está: `Deposit` se os fundos chegaram ao seu endereço (`Address` é o seu
endereço), `Withdrawal` se foi você quem enviou (`Address` é o endereço do
destinatário externo). O risco é calculado para a contraparte: o remetente em um
deposit, o destinatário em um withdrawal. Se você omitir a direção, o SDK envia
`deposit`.

`RiskScore` é `null` até a verificação terminar. Uma verificação concluída pode
ter pontuação 0, o que não é o mesmo que ainda não ter pontuação.

### Assinaturas

Uma assinatura mantém um endereço abastecido de energia para cada transação até
ser parada ou esgotar seus dias ou transações. Escolha um plano de
`GetSubscriptionsAsync()` e passe o seu `SubscriptionId`, como
`"unlimited_energy"`, não o `Id` numérico:

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
    DurationDays = 30,     // 0 para sem limite de tempo
    TransactionsLimit = 0, // 0 para sem limite
    ExternalId = "subscription-42",
});

subscription = await client.CheckSubscriptionAsync(SubscriptionRequest.ByExternalId("subscription-42"));

subscription = await client.StopSubscriptionAsync(SubscriptionRequest.ById(subscription.Id));

SubscriptionHistory history = await client.GetSubscriptionHistoryAsync(
    new SubscriptionHistoryRequest { Status = SubscriptionStatus.Active });
```

Iniciar, consultar e parar retornam a assinatura com seus `Params`; o histórico
retorna em vez disso os contadores de uso `TransactionsUsed`, `EnergyUsed` e
`TotalPrice`, com `Params` igual a `null`. Iniciar uma assinatura cobra o preço
inicial do plano. Uma assinatura com limite de transações não pode ser parada
(`TronzapErrorCode.CannotStopSubscription`).

## Tratamento de erros

Toda falha de uma chamada à API é uma `TronzapException`. Capture uma subclasse
para tratar um tipo específico de falha:

```
TronzapException
├── TronzapApiException                — a API respondeu com um código diferente de zero
├── TronzapHttpException               — resposta não 2xx sem payload da API
│   ├── TronzapRateLimitException      — HTTP 429
│   ├── TronzapUnauthorizedException   — HTTP 401 ou 403
│   └── TronzapServerException         — HTTP 5xx
├── TronzapInvalidResponseException    — resposta 2xx que o SDK não conseguiu ler
└── TronzapNetworkException            — nenhuma resposta chegou
    ├── TronzapConnectionException     — falha de DNS, conexão recusada
    ├── TronzapTimeoutException        — a requisição excedeu o timeout
    └── TronzapSslException            — falha no handshake TLS ou no certificado
```

`TronzapApiException`, `TronzapHttpException` e `TronzapInvalidResponseException`
trazem o status HTTP (`StatusCode`) e o corpo bruto da resposta (`ResponseBody`).
`TronzapApiException` também traz o código de erro da API, a chave do erro e o ID
da requisição. `TronzapRateLimitException.RetryAfter` contém o intervalo de
`Retry-After` quando a API o envia.

Duas falhas não são `TronzapException`: argumentos inválidos lançam
`ArgumentException` antes de qualquer envio, e cancelar o `CancellationToken`
lança `OperationCanceledException`, como em todo o .NET. Um timeout é sempre
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
    // Falha no nível da aplicação: o código diz exatamente o que deu errado.
    switch (e.ErrorCode)
    {
        case TronzapErrorCode.InvalidTronAddress:
            // A chave pode detalhar, p. ex. "invalid_tron_address.from_address"
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
    // Aguarde e tente novamente, após e.RetryAfter se a API o enviou.
}
catch (TronzapUnauthorizedException)
{
    // Token ou assinatura incorretos.
}
catch (Exception e) when (e is TronzapTimeoutException or TronzapServerException)
{
    // Transitório; pode tentar novamente.
}
catch (TronzapNetworkException)
{
    // Inacessível.
}
```

`RequestId` é o identificador que a API atribui a cada requisição. Informe-o ao
contatar o suporte.

Um erro da API tem prioridade sobre o status HTTP: a API informa algumas falhas
com status 2xx e outras com 4xx ou 5xx, então um payload legível com código
diferente de zero é sempre informado como `TronzapApiException`, nunca como
`TronzapHttpException`.

### Códigos de erro da API

| Código | Constante | Descrição |
|------|----------|-------------|
| 1 | `AuthError` | Erro de autenticação: token da API ou assinatura inválidos |
| 2 | `InvalidServiceOrParams` | Serviço ou parâmetros inválidos |
| 5 | `WalletNotFound` | Carteira interna não encontrada. Contate o suporte. |
| 6 | `InsufficientFunds` | Saldo insuficiente |
| 10 | `InvalidTronAddress` | Endereço TRON inválido, ou o endereço já tem uma assinatura ativa |
| 11 | `InvalidEnergyAmount` | Quantidade de energia inválida |
| 12 | `InvalidDuration` | Duração inválida |
| 20 | `TransactionNotFound` | Transação/assinatura não encontrada |
| 21 | `CannotStopSubscription` | Não é possível interromper a assinatura, p. ex. ela tem limite de transações |
| 24 | `AddressNotActivated` | Endereço não ativado |
| 25 | `AddressAlreadyActivated` | Endereço já ativado |
| 30 | `AmlCheckNotFound` | Verificação AML não encontrada |
| 35 | `ServiceNotAvailable` | Serviço indisponível |
| 50 | `InvalidBandwidthAmount` | Quantidade de largura de banda inválida |
| 500 | `InternalServerError` | Erro interno do servidor: contate o suporte |

As constantes são valores do enum `TronzapErrorCode`. Um código que esta versão do
SDK não conhece é informado como `TronzapErrorCode.Unknown`, e o número continua
disponível em `Code`.

## Campos decimais e de data

Valores e preços são `decimal`, então mantêm o valor exato enviado pela API. A API
codifica dinheiro como número JSON em algumas respostas e como string JSON em
outras; as duas formas são lidas da mesma maneira.

Datas são records `Timestamp`: `Value` é o `DateTimeOffset` interpretado e `Raw` é
o texto exatamente como a API enviou. Os vários formatos usados pela API são
aceitos, e horários sem fuso são lidos como UTC. Uma data não reconhecida deixa
`Value` como `null` em vez de fazer toda a resposta falhar.

Valores que a API venha a adicionar no futuro, como um novo status de transação,
são informados como o membro `Unknown` do enum correspondente em vez de falhar.

## Testes

```bash
dotnet test
```

Executa os testes unitários no .NET 8 e no .NET 10 contra um servidor HTTP local:
o corpo exato da requisição e a assinatura de cada endpoint, erros da API e HTTP,
JSON malformado, timeouts, cancelamento, falhas de rede e de TLS, e uso
concorrente.

## Licença

Licença MIT (MIT). Veja o [arquivo de licença](LICENSE) para mais informações.

## Suporte

Para suporte, entre em contato com [support@tronzap.com](mailto:support@tronzap.com).
