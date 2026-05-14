# External Data Sources

> **Project:** Third-Party Provider Gateway
> **Last updated:** 2025-07-15

This document describes every external data source integrated into the Third-Party Provider Gateway — including base URLs, authentication, rate limits, caching strategy, resilience configuration, real JSON response examples, and Kubernetes secrets.

---

## Summary

| #  | Source                 | Provider          | Base URL                                      | Auth              | Rate Limit (Free)       | Cache TTL    |
| -- | ---------------------- | ----------------- | ---------------------------------------------  | ----------------- | ----------------------- | ------------ |
| 1  | **Binance**            | Binance           | `https://api.binance.com`                     | API Key + Secret  | 1 200 req/min (weight)  | 30 s – 24 h  |
| 2  | **CoinGecko**          | CoinGecko         | `https://api.coingecko.com/api/v3/`           | Optional API Key  | 10–30 req/min           | 5 min – 1 h  |
| 3  | **Alternative.me**     | Alternative.me    | `https://api.alternative.me/`                 | None              | None documented         | 1 h          |
| 4  | **RSS News Feeds**     | CryptoPanic + others | See §4 below                                | None              | 5 req/min (CryptoPanic) | 15 min       |

---

## 1. Binance

### 1.1 Source & Provider

- **Source name:** Binance Spot API
- **Provider:** Binance (https://www.binance.com)

### 1.2 Base URL & API Version

| Environment | Base URL                     | API Version |
| ----------- | ---------------------------- | ----------- |
| Production  | `https://api.binance.com`    | `v3`        |
| Testnet     | `https://testnet.binance.vision` | `v3`    |

API endpoints are prefixed with `/api/v3/`.

### 1.3 Authentication

Binance uses **two-factor** authentication:

| Credential    | How it is used                                                              | Config key                  |
| ------------- | --------------------------------------------------------------------------- | --------------------------- |
| **API Key**   | Sent as the `X-MBX-APIKEY` HTTP header on every request                     | `Binance:ApiKey`            |
| **API Secret**| Used to HMAC-SHA256-sign the query string for **SIGNED** endpoints (`/account`, `/order`) | `Binance:ApiSecret` |

Unsigned endpoints (e.g. `/ticker/price`, `/klines`, `/exchangeInfo`) do **not** require any authentication.

> ⚠️ The service throws `InvalidOperationException` on startup if either `ApiKey` or `ApiSecret` is empty or whitespace.

### 1.4 Rate Limits

- **General limit:** 1 200 request-weight per minute per IP (most requests weigh 1–10).
- **Order rate limit:** 10 orders per second per symbol.
- **Weight consumption varies per endpoint** — the service does not parse Binance's rate-limit headers for back-off; instead, it relies on the Polly resilience pipeline to retry on `429 Too Many Requests`.

### 1.5 Endpoints Used

| Endpoint                    | HTTP Method | Signed | Purpose                                     |
| --------------------------- | ----------- | ------ | ------------------------------------------- |
| `/api/v3/ticker/price`      | GET         | No     | Current ticker price for a symbol           |
| `/api/v3/klines`            | GET         | No     | OHLCV candlestick data (default 1h)         |
| `/api/v3/ticker/24hr`       | GET         | No     | 24-hour rolling ticker statistics           |
| `/api/v3/account`           | GET         | Yes    | Account balances and commissions            |
| `/api/v3/order`             | POST        | Yes    | Place a new order (MARKET BUY / SELL)       |
| `/api/v3/exchangeInfo`      | GET         | No     | Trading rules, filters (LOT_SIZE, NOTIONAL) |

### 1.6 Caching Strategy

Caching is applied at the **query-handler level** (CQRS handlers in the Application layer), **not** inside the service class itself — except for `GetExchangeInfoAsync`, which is cached directly inside `BinanceService`.

| Data              | Cache Key Pattern                                                    | TTL     | Rationale                                    |
| ----------------- | -------------------------------------------------------------------- | ------- | -------------------------------------------- |
| Price             | `ThirdParty:Binance:Price:{symbol}`                                  | 30 s    | Near-real-time display                       |
| Klines            | `ThirdParty:Binance:Klines:{symbol}:{interval}:{limit}`              | 5 min   | Historical data, changes slowly              |
| 24h Ticker        | `ThirdParty:Binance:Ticker24h:{symbol}`                              | 1 min   | 24h stats change gradually                   |
| Balances          | `ThirdParty:Binance:Balances`                                        | 1 min   | Changes only during active trading           |
| ExchangeInfo      | `ThirdParty:Binance:ExchangeInfo:{symbol}`                           | 3 min   | Trading rules are stable                     |
| MinNotional       | `ThirdParty:Binance:MinNotional:{symbol}`                            | 24 h    | Exchange filters rarely change               |

The **Cache-Aside** pattern is used:

```
Query → Check ICacheService → hit? → return cached
                            → miss? → call BinanceService
                                   → store in ICacheService
                                   → return fresh
```

### 1.7 Resilience

All Binance HTTP traffic flows through an `HttpClient` registered with `.AddStandardResilienceHandler()` ([Microsoft.Extensions.Http.Resilience](https://www.nuget.org/packages/Microsoft.Extensions.Http.Resilience)), which applies a Polly-based pipeline:

| Policy           | Configuration (defaults)                                               |
| ---------------- | ---------------------------------------------------------------------- |
| **Retry**        | Up to 3 automatic retries with exponential back-off (0.5 s, 1 s, 2 s) |
| **Circuit Breaker** | Opens after 50 % failure rate over 10 s sampling window             |
| **Timeout**      | 30 s per attempt                                                       |
| **Total timeout**| 120 s for the entire request pipeline                                  |

Additionally, `PlaceMarketSellAsync` performs pre-flight validation:
- Checks `LOT_SIZE` step size and aligns quantity to a valid multiple.
- Validates order value against `MIN_NOTIONAL` filter before submitting.

### 1.8 Real Data Example

#### GET /api/v3/ticker/price?symbol=BTCUSDT

```json
{
  "symbol": "BTCUSDT",
  "price": "63452.10"
}
```

#### GET /api/v3/klines?symbol=BTCUSDT&interval=1h&limit=3

```json
[
  [
    1721016000000,
    "63200.00",
    "63850.00",
    "63100.00",
    "63720.00",
    "1245.67"
  ],
  [
    1721019600000,
    "63720.00",
    "64200.00",
    "63500.00",
    "64000.00",
    "1389.12"
  ],
  [
    1721023200000,
    "64000.00",
    "64300.00",
    "63650.00",
    "63452.10",
    "1102.45"
  ]
]
```

#### GET /api/v3/ticker/24hr?symbol=BTCUSDT

```json
{
  "symbol": "BTCUSDT",
  "lastPrice": "63452.10",
  "priceChangePercent": "2.45",
  "quoteVolume": "2854761234.56",
  "highPrice": "64500.00",
  "lowPrice": "61800.00"
}
```

#### GET /api/v3/account (SIGNED)

```json
{
  "makerCommission": 10,
  "takerCommission": 10,
  "canTrade": true,
  "canWithdraw": true,
  "canDeposit": true,
  "accountType": "SPOT",
  "balances": [
    { "asset": "BTC",  "free": "0.01500000",  "locked": "0.00500000" },
    { "asset": "ETH",  "free": "1.25000000",  "locked": "0.00000000" },
    { "asset": "USDT", "free": "12500.00",    "locked": "500.00"     },
    { "asset": "BNB",  "free": "2.00000000",  "locked": "0.00000000" }
  ],
  "permissions": ["SPOT"]
}
```

#### POST /api/v3/order (SIGNED) — Market Buy

```json
{
  "symbol": "BTCUSDT",
  "orderId": 285174352,
  "orderListId": -1,
  "clientOrderId": "x-R4A3UXEH29C481d6c7a8f8e6f4d",
  "price": "0.00000000",
  "origQty": "0.00000000",
  "executedQty": "0.00200000",
  "cummulativeQuoteQty": "126.90420",
  "cumulativeQuoteQty": "126.90420",
  "status": "FILLED",
  "timeInForce": "GTC",
  "type": "MARKET",
  "side": "BUY",
  "stopPrice": "0.00000000",
  "icebergQty": "0.00000000",
  "time": 1721025600000,
  "updateTime": 1721025600001,
  "workingTime": 1721025600000,
  "isWorking": true,
  "origQuoteOrderQty": "127.00000000",
  "selfTradePreventionMode": "NONE"
}
```

> **Note:** MARKET orders return `price: "0.00000000"` in the raw response. The service computes the actual average execution price as `cumulativeQuoteQty / executedQty`.

#### GET /api/v3/exchangeInfo?symbol=BTCUSDT

```json
{
  "timezone": "UTC",
  "serverTime": 1721025600000,
  "symbols": [
    {
      "symbol": "BTCUSDT",
      "status": "TRADING",
      "baseAsset": "BTC",
      "quoteAsset": "USDT",
      "filters": [
        {
          "filterType": "LOT_SIZE",
          "minQty": "0.00000100",
          "maxQty": "9000.00000000",
          "stepSize": "0.00100000"
        },
        {
          "filterType": "MIN_NOTIONAL",
          "minNotional": "10.00000000"
        },
        {
          "filterType": "PRICE_FILTER",
          "minPrice": "0.01000000",
          "maxPrice": "1000000.00000000",
          "tickSize": "0.01000000"
        }
      ]
    }
  ]
}
```

---

## 2. CoinGecko

### 2.1 Source & Provider

- **Source name:** CoinGecko API v3
- **Provider:** CoinGecko (https://www.coingecko.com)

### 2.2 Base URL & API Version

| Environment | Base URL                              | API Version |
| ----------- | ------------------------------------- | ----------- |
| Production  | `https://api.coingecko.com/api/v3/`   | `v3`        |

### 2.3 Authentication

CoinGecko supports an **optional** demo/pro API key:

| Tier       | Auth Header              | Config key           |
| ---------- | ------------------------ | -------------------- |
| Free       | None                     | —                    |
| Demo/Pro   | `x-cg-demo-api-key: {key}` | `CoinGecko:ApiKey`  |

If `CoinGecko:ApiKey` is empty or null, the header is **not** added and the free tier applies.

### 2.4 Rate Limits

| Tier     | Limit                                    |
| -------- | ---------------------------------------- |
| Free     | 10–30 calls per minute (varies)          |
| Demo     | 50 calls per minute                      |
| Pro      | 500+ calls per minute (plan-dependent)   |

Caching is essential on the free tier — most queries are served from cache to avoid hitting the limit.

### 2.5 Endpoints Used

| Endpoint               | Purpose                                          |
| ---------------------- | ------------------------------------------------ |
| `simple/price`         | Current price for one or more coins              |
| `global`               | Aggregated global market data (BTC/ETH dominance, total market cap, 24h volume, market cap change) |
| `search/trending`      | Top 7 trending coins by search volume (24h)      |

### 2.6 Caching Strategy

| Data              | Cache Key Pattern                                            | TTL     | Rationale                                |
| ----------------- | ------------------------------------------------------------ | ------- | ---------------------------------------- |
| Price             | `ThirdParty:CoinGecko:Price:{coinId}:{vsCurrency}`           | 5 min   | Free-tier rate limits are tight          |
| Global market     | `ThirdParty:CoinGecko:Global`                                | 5 min   | Global data updates every few minutes    |
| Trending coins    | `ThirdParty:CoinGecko:Trending`                              | 1 h     | Trending list changes slowly             |

### 2.7 Resilience

Same standard Polly pipeline as Binance (see §1.7):
- Up to 3 retries (exponential back-off)
- Circuit breaker (50 % failure / 10 s window)
- Per-attempt timeout: 30 s
- Total timeout: 120 s

### 2.8 Real Data Example

#### GET /simple/price?ids=bitcoin,ethereum&vs_currencies=usd

```json
{
  "bitcoin": {
    "usd": 63452
  },
  "ethereum": {
    "usd": 3456.78
  }
}
```

#### GET /global

```json
{
  "data": {
    "active_cryptocurrencies": 14256,
    "total_market_cap": {
      "usd": 2340000000000
    },
    "total_volume": {
      "usd": 78500000000
    },
    "market_cap_percentage": {
      "btc": 49.2,
      "eth": 16.8
    },
    "market_cap_change_percentage_24h_usd": -1.25,
    "updated_at": 1721025600
  }
}
```

#### GET /search/trending

```json
{
  "coins": [
    {
      "item": {
        "id": "bitcoin",
        "name": "Bitcoin",
        "symbol": "BTC",
        "market_cap_rank": 1,
        "data": {
          "price_change_percentage_24h": {
            "usd": 2.45
          }
        }
      }
    },
    {
      "item": {
        "id": "ethereum",
        "name": "Ethereum",
        "symbol": "ETH",
        "market_cap_rank": 2,
        "data": {
          "price_change_percentage_24h": {
            "usd": 1.82
          }
        }
      }
    },
    {
      "item": {
        "id": "solana",
        "name": "Solana",
        "symbol": "SOL",
        "market_cap_rank": 5,
        "data": {
          "price_change_percentage_24h": {
            "usd": 5.67
          }
        }
      }
    }
  ]
}
```

---

## 3. Alternative.me (Fear & Greed Index)

### 3.1 Source & Provider

- **Source name:** Alternative.me Fear & Greed Index API
- **Provider:** Alternative.me (https://alternative.me)

### 3.2 Base URL & API Version

| Environment | Base URL                        | Version |
| ----------- | ------------------------------- | ------- |
| Production  | `https://api.alternative.me/`   | `fng`   |

The Fear & Greed endpoint path is `/fng/`.

### 3.3 Authentication

**None.** The Alternative.me API is fully public and requires no API key.

### 3.4 Rate Limits

Alternative.me does **not** publish official rate limits. The service takes a conservative approach by caching the response for 1 hour, which reduces traffic to ~24 requests per day.

### 3.5 Endpoints Used

| Endpoint   | Purpose                                    |
| ---------- | ------------------------------------------ |
| `/fng/`    | Current Fear & Greed index value, classification, and timestamp |

### 3.6 Caching Strategy

| Data              | Cache Key Pattern                            | TTL  | Rationale                                     |
| ----------------- | -------------------------------------------- | ---- | --------------------------------------------- |
| Fear & Greed      | `ThirdParty:Sentiment:FearAndGreed`          | 1 h  | Index updates only once per day               |

### 3.7 Resilience

Same standard Polly pipeline as other sources (see §1.7).

### 3.8 Real Data Example

#### GET /fng/

```json
{
  "name": "Fear and Greed Index",
  "data": [
    {
      "value": "42",
      "value_classification": "Fear",
      "timestamp": "1721025600",
      "time_until_update": "43200"
    }
  ],
  "metadata": {
    "error": null
  }
}
```

---

## 4. RSS News Feeds

### 4.1 Source & Provider

The news aggregator pulls from **multiple RSS 2.0 sources** simultaneously:

| Source              | Provider             | Feed URL (per-coin) / General URL                       | Auth   |
| ------------------- | -------------------- | ------------------------------------------------------- | ------ |
| **CryptoPanic**     | CryptoPanic          | `https://cryptopanic.com/news/{slug}/rss/`              | None   |
| **CoinDesk**        | CoinDesk             | `https://www.coindesk.com/arc/outboundfeeds/rss/`       | None   |
| **CoinTelegraph**   | CoinTelegraph        | `https://cointelegraph.com/rss`                         | None   |

Additional feeds can be added via the `RssNews:FeedUrls` configuration list (see [Adding a New Data Source](#5-adding-a-new-data-source)).

### 4.2 Base URLs & Feed Format

All feeds use **RSS 2.0 XML** format with standard `<item>` elements containing `<title>`, `<link>`, `<pubDate>`, and `<source>`.

### 4.3 Authentication

**None.** All RSS feeds are publicly accessible without authentication.

### 4.4 Rate Limits

| Source              | Limit (free tier)    |
| ------------------- | -------------------- |
| CryptoPanic         | 5 requests per minute|
| CoinDesk            | None documented      |
| CoinTelegraph       | None documented      |

The 15-minute cache TTL keeps requests to CryptoPanic well within the free-tier limit.

### 4.5 Strategy

The RSS aggregation strategy works as follows:

1. **When currencies are specified** (e.g. `?currencies=BTC,ETH`):
   - Fetch per-coin CryptoPanic RSS feeds using the coin slug lookup table (e.g. `BTC` → `bitcoin`, `ETH` → `ethereum`).
   - Also fetch the configured general feeds (CoinDesk, CoinTelegraph).
   - Articles from general feeds are **included only if their title mentions** one of the requested symbols (case-insensitive).
2. **When no currencies are specified**:
   - Fetch only the configured general feeds — no per-coin filtering.
3. **Merging**:
   - Deduplicate by `Url`.
   - Sort descending by `PublishedAt`.
   - Take the top `limit` articles.

**Individual feed failures are silently swallowed** — if CryptoPanic is down, the service still returns articles from CoinDesk and CoinTelegraph.

### 4.6 Coin Slug Lookup Table

The service maps uppercase ticker symbols to CryptoPanic slugs:

| Symbol | Slug                  |
| ------ | --------------------- |
| BTC    | `bitcoin`             |
| ETH    | `ethereum`            |
| BNB    | `binancecoin`         |
| SOL    | `solana`              |
| XRP    | `xrp`                 |
| ADA    | `cardano`             |
| AVAX   | `avalanche`           |
| DOT    | `polkadot`            |
| NEAR   | `near-protocol`       |
| MATIC  | `polygon`             |
| LINK   | `chainlink`           |
| UNI    | `uniswap`             |
| AAVE   | `aave`                |
| CRV    | `curve-dao-token`     |
| OP     | `optimism`            |
| ARB    | `arbitrum`            |
| DOGE   | `dogecoin`            |
| SHIB   | `shiba-inu`           |
| PEPE   | `pepe`                |
| FLOKI  | `floki`               |
| WLD    | `worldcoin`           |
| LTC    | `litecoin`            |
| BCH    | `bitcoin-cash`        |
| TON    | `toncoin`             |
| SUI    | `sui`                 |
| APT    | `aptos`               |

### 4.7 Caching Strategy

| Data           | Cache Key Pattern                                | TTL    | Rationale                           |
| -------------- | ------------------------------------------------ | ------ | ----------------------------------- |
| News articles  | `ThirdParty:News:{sorted,comma-separated-symbols}:{limit}` | 15 min | Articles don't appear every second |

Cache key is built from:
- Sorted, uppercased, comma-separated currency symbols.
- The requested `limit` value.

### 4.8 Resilience

Same standard Polly pipeline as other sources (see §1.7). Additionally, individual feed fetch failures are caught and silently ignored at the `RssNewsService` level:

```
foreach feed URL:
    try → fetch & parse
    catch → skip feed, continue with remaining
```

### 4.9 Real Data Example

#### GET /api/news?currencies=BTC,ETH&limit=2

```json
[
  {
    "title": "Bitcoin Surges Past $63,000 as ETF Inflows Reach New High",
    "url": "https://cryptopanic.com/news/1928374/Bitcoin-Surges-Past-63000",
    "source": "CryptoPanic",
    "publishedAt": "2025-07-15T14:30:00Z",
    "currencies": ["BTC"],
    "bullishVotes": 342,
    "bearishVotes": 28
  },
  {
    "title": "Ethereum Layer-2 Transaction Volume Hits All-Time Record",
    "url": "https://cointelegraph.com/news/ethereum-layer2-record",
    "source": "CoinTelegraph",
    "publishedAt": "2025-07-15T14:15:00Z",
    "currencies": ["ETH"],
    "bullishVotes": 0,
    "bearishVotes": 0
  }
]
```

#### Raw RSS XML (CryptoPanic — BTC feed)

```xml
<?xml version="1.0" encoding="UTF-8"?>
<rss version="2.0">
  <channel>
    <title>CryptoPanic Bitcoin News</title>
    <link>https://cryptopanic.com</link>
    <item>
      <title>Bitcoin Surges Past $63,000 as ETF Inflows Reach New High</title>
      <link>https://cryptopanic.com/news/1928374/</link>
      <pubDate>Tue, 15 Jul 2025 14:30:00 +0000</pubDate>
      <source>CryptoPanic</source>
    </item>
    <item>
      <title>Bitcoin Mining Difficulty Adjusts Upward by 3.5%</title>
      <link>https://cryptopanic.com/news/1928375/</link>
      <pubDate>Tue, 15 Jul 2025 13:45:00 +0000</pubDate>
      <source>CryptoPanic</source>
    </item>
  </channel>
</rss>
```

---

## 5. Adding a New Data Source

This section describes the steps required to integrate a new external data provider into the gateway.

### 5.1 Overview

Every new data source requires changes across **all four layers** of the Clean Architecture:

```
Api (Controller)
  └── Application (CQRS Handler + Query/Command + Interface)
        └── Domain (Models + Cache Keys + HttpClient Name)
              └── Infrastructure (Settings + Service Implementation + DI Registration)
```

### 5.2 Step-by-Step Guide

#### 1. Add an HTTP client name constant

**File:** `TradingProject.ThirdParty.Domain/Constants/HttpClientNames.cs`

```csharp
public const string MyProvider = "MyProvider";
```

#### 2. Add domain models (if needed)

**File:** `TradingProject.ThirdParty.Domain/Models/Market/MyProviderData.cs`

```csharp
public record MyProviderData(string Field1, double Field2);
```

#### 3. Add cache keys (optional)

**File:** `TradingProject.ThirdParty.Domain/Constants/CacheKeys.cs`

```csharp
public static class MyProvider
{
    public const string DataPrefix = $"{ServiceNamePrefix}MyProvider:Data:";
    public static readonly TimeSpan DataDuration = TimeSpan.FromMinutes(5);
    public static string Data(string param) => $"{DataPrefix}{param}";
}
```

#### 4. Add settings class

**File:** `TradingProject.ThirdParty.Infrastructure/Settings/MyProviderSettings.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace TradingProject.ThirdParty.Infrastructure.Settings;

public class MyProviderSettings
{
    [Required, Url]
    public required string BaseUrl { get; set; }

    public string? ApiKey { get; set; }
}
```

#### 5. Create a service interface

**File:** `TradingProject.ThirdParty.Application/Abstractions/IMyProviderService.cs`

```csharp
using TradingProject.ThirdParty.Domain.Models.Market;

namespace TradingProject.ThirdParty.Application.Abstractions;

public interface IMyProviderService
{
    Task<MyProviderData?> GetDataAsync(CancellationToken cancellationToken = default);
}
```

#### 6. Implement the service

**File:** `TradingProject.ThirdParty.Infrastructure/Services/MyProviderService.cs`

```csharp
using System.Text.Json;
using Microsoft.Extensions.Options;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Constants;
using TradingProject.ThirdParty.Domain.Models.Market;
using TradingProject.ThirdParty.Infrastructure.Settings;

namespace TradingProject.ThirdParty.Infrastructure.Services;

public class MyProviderService(
    IHttpClientFactory httpClientFactory,
    JsonSerializerOptions jsonOptions) : IMyProviderService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(HttpClientNames.MyProvider);

    public async Task<MyProviderData?> GetDataAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("endpoint", cancellationToken);
        response.EnsureSuccessStatusCode();

        return await JsonSerializer.DeserializeAsync<MyProviderData>(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            jsonOptions,
            cancellationToken);
    }
}
```

#### 7. Register in DI

**File:** `TradingProject.ThirdParty.Infrastructure/DependencyInjection.cs`

```csharp
// 7a. Bind settings
services.AddOptions<MyProviderSettings>()
    .Bind(configuration.GetSection(HttpClientNames.MyProvider))
    .ValidateOnStart();

// 7b. Register HttpClient with resilience
services.AddHttpClient(HttpClientNames.MyProvider, (sp, client) =>
{
    var settings = sp.GetRequiredService<IOptions<MyProviderSettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
    client.WithDefaultRequestHeaders();

    if (!string.IsNullOrEmpty(settings.ApiKey))
        client.DefaultRequestHeaders.Add("X-API-Key", settings.ApiKey);
}).AddStandardResilienceHandler();

// 7c. Register service
services.AddTransient<IMyProviderService, MyProviderService>();
```

#### 8. Add CQRS handler

Create the query/command and handler in the Application layer:

```
Features/
  MyProvider/
    Queries/
      GetMyData/
        GetMyDataQuery.cs
        GetMyDataQueryHandler.cs
```

The handler should follow the **Cache-Aside** pattern:

```csharp
public class GetMyDataQueryHandler(
    IMyProviderService service,
    ICacheService cache,
    ILogger<GetMyDataQueryHandler> logger) : IRequestHandler<GetMyDataQuery, MyProviderData?>
{
    public async Task<MyProviderData?> Handle(GetMyDataQuery request, CancellationToken ct)
    {
        var key = CacheKeys.MyProvider.Data(request.Param);

        var cached = await cache.GetAsync(key, ct);
        if (cached is not null)
        {
            logger.LogInformation("Returning cached data for {Param}", request.Param);
            return JsonSerializer.Deserialize<MyProviderData>(cached);
        }

        var data = await service.GetDataAsync(ct);
        if (data is not null)
        {
            await cache.SetAsync(key, JsonSerializer.Serialize(data), CacheKeys.MyProvider.DataDuration, ct);
        }

        return data;
    }
}
```

#### 9. Add API endpoint

**File:** Controller in `TradingProject.ThirdParty.Api/Controllers/V1/`

```csharp
[HttpGet("my-data/{param}")]
public async Task<IActionResult> GetMyData(string param, CancellationToken ct)
{
    var result = await mediator.Send(new GetMyDataQuery(param), ct);
    if (result is null) return NotFound();
    return Ok(result);
}
```

#### 10. Add configuration & Kubernetes secrets

**File:** `appsettings.json`

```json
"MyProvider": {
    "BaseUrl": "https://api.myprovider.com/v1/",
    "ApiKey": ""
}
```

**File:** `k8s/deployment.yaml`

```yaml
env:
- name: MYPROVIDER__APIKEY
  valueFrom:
    secretKeyRef:
      name: trading-secrets
      key: myprovider-api-key
- name: MYPROVIDER__BASEURL
  value: "https://api.myprovider.com/v1/"
```

---

## 6. Kubernetes Secrets Reference

### 6.1 Secret Object

All secrets are stored in the `trading-secrets` Kubernetes secret in the `trading-ai` namespace.

### 6.2 Secret Key Mapping

| Secret Key               | Environment Variable        | Source      | Required | Description                     |
| ------------------------ | --------------------------- | ----------- | -------- | ------------------------------- |
| `binance-api-key`        | `BINANCE__APIKEY`           | Binance     | ✅       | Binance API key                 |
| `binance-api-secret`     | `BINANCE__APISECRET`        | Binance     | ✅       | Binance API secret              |
| `coingecko-api-key`      | `COINGECKO__APIKEY`         | CoinGecko   | ❌       | CoinGecko demo/pro API key      |

### 6.3 Environment Variables (Non-Secret)

These are set via `configMapRef: service-endpoints` or plain `env` values in the Deployment, **not** from `trading-secrets`:

| Environment Variable            | Source           | Value                                 |
| ------------------------------- | ---------------- | ------------------------------------- |
| `BINANCE__BASEURL`              | Binance          | `https://api.binance.com`             |
| `COINGECKO__BASEURL`            | CoinGecko        | `https://api.coingecko.com/api/v3/`   |
| `ALTERNATIVEME__BASEURL`        | Alternative.me   | `https://api.alternative.me/`         |
| `ASPNETCORE_ENVIRONMENT`        | —                | `Production`                          |

### 6.4 Creating / Updating the Secret

```shell
# Create the secret
kubectl create secret generic trading-secrets \
  --namespace trading-ai \
  --from-literal=binance-api-key='your-binance-api-key' \
  --from-literal=binance-api-secret='your-binance-api-secret' \
  --from-literal=coingecko-api-key='your-coingecko-api-key'

# Update an existing secret
kubectl patch secret trading-secrets \
  --namespace trading-ai \
  --patch='{"stringData":{"binance-api-key":"new-key"}}'
```

### 6.5 Full Deployment Env Configuration

Extracted from `k8s/deployment.yaml`:

```yaml
envFrom:
  - configMapRef:
      name: service-endpoints
env:
  - name: ASPNETCORE_ENVIRONMENT
    value: "Production"
  - name: BINANCE__APIKEY
    valueFrom:
      secretKeyRef:
        name: trading-secrets
        key: binance-api-key
  - name: BINANCE__APISECRET
    valueFrom:
      secretKeyRef:
        name: trading-secrets
        key: binance-api-secret
  - name: BINANCE__BASEURL
    value: "https://api.binance.com"
  - name: COINGECKO__APIKEY
    valueFrom:
      secretKeyRef:
        name: trading-secrets
        key: coingecko-api-key
  - name: COINGECKO__BASEURL
    value: "https://api.coingecko.com/api/v3/"
  - name: ALTERNATIVEME__BASEURL
    value: "https://api.alternative.me/"
```

---

## Appendix A — Resilience Pipeline Details

All HTTP clients registered through `AddInfrastructure` use `.AddStandardResilienceHandler()` from the `Microsoft.Extensions.Http.Resilience` package (NuGet).

The default pipeline applies the following **Polly-based policies** in order:

```
Incoming Request
    │
    ▼
┌───────────────────┐
│ Total Request     │  ← 120 s timeout for the entire pipeline
│ Timeout           │
└────────┬──────────┘
         │
         ▼
┌───────────────────┐
│ Retry             │  ← Up to 3 retries with exponential back-off
│ (exponential)     │     Initial delay: ~0.5 s
└────────┬──────────┘
         │
         ▼
┌───────────────────┐
│ Circuit Breaker   │  ← Opens when 50 %+ of requests fail
│ (50 % / 10 s)     │     Sampling duration: 10 s
└────────┬──────────┘
         │
         ▼
┌───────────────────┐
│ Attempt Timeout   │  ← 30 s per individual HTTP request
└────────┬──────────┘
         │
         ▼
   HttpClient.SendAsync
```

This configuration applies equally to **all** external sources (Binance, CoinGecko, Alternative.me, RSS Feeds).

---

## Appendix B — Namespace Reference

| Layer        | Namespace                                                  | Key Classes / Files                       |
| ------------ | ---------------------------------------------------------- | ----------------------------------------- |
| **Domain**   | `TradingProject.ThirdParty.Domain.Constants`               | `HttpClientNames`, `CacheKeys`            |
| **Domain**   | `TradingProject.ThirdParty.Domain.Models.Market`           | `FearAndGreedIndex`, `GlobalMarketData`, `TrendingCoin` |
| **Domain**   | `TradingProject.ThirdParty.Domain.Models.News`             | `NewsItem`                                |
| **App**      | `TradingProject.ThirdParty.Application.Abstractions`       | `IBinanceService`, `ICoinGeckoService`, `ISentimentService`, `INewsService`, `ICacheService` |
| **App**      | `TradingProject.ThirdParty.Application.Features.Binance`   | Query / Command handlers                  |
| **App**      | `TradingProject.ThirdParty.Application.Features.MarketData`| CoinGecko query handlers                  |
| **App**      | `TradingProject.ThirdParty.Application.Features.Sentiment` | Fear & Greed query handler                |
| **App**      | `TradingProject.ThirdParty.Application.Features.News`      | News query handler                        |
| **Infra**    | `TradingProject.ThirdParty.Infrastructure.Services`        | `BinanceService`, `CoinGeckoService`, `AlternativeMeService`, `RssNewsService`, `RedisCacheService`, `MemoryCacheService` |
| **Infra**    | `TradingProject.ThirdParty.Infrastructure.Settings`        | `BinanceSettings`, `CoinGeckoSettings`, `AlternativeMeSettings`, `RssNewsSettings`, `CacheSettings`, `RedisSettings` |
| **Api**      | `TradingProject.ThirdParty.Api.Controllers.V1`             | `BinanceController`, `MarketDataController`, `NewsController` |
