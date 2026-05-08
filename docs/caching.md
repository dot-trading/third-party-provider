# 🔄 Caching

The gateway uses a pluggable caching layer to minimize upstream API calls, reduce latency, and stay within free-tier rate limits.

---

## 📋 Table of Contents

- [Overview](#overview)
- [Configuration](#configuration)
  - [Cache Settings](#cache-settings)
  - [Redis Settings](#redis-settings)
  - [Configuration Validation](#configuration-validation)
- [Cache Providers](#cache-providers)
  - [Redis (default)](#redis-default)
  - [Memory](#memory)
  - [Null / Disabled](#null--disabled)
- [Cache Keys](#cache-keys)
- [Per-API Cache Durations](#per-api-cache-durations)
  - [Binance](#binance)
  - [CoinGecko](#coingecko)
  - [News (RSS)](#news-rss)
  - [Sentiment](#sentiment)
- [Architecture](#architecture)

---

## Overview

Caching is implemented via the `ICacheService` interface. The concrete implementation is selected at startup based on the `Cache` configuration section:

- **Redis** — Distributed cache via a Redis instance (default, production-ready).
- **Memory** — In-process `IMemoryCache`, ideal for development or single-instance deployments.
- **Null / Disabled** — No-op cache that always returns `null`; upstream services are called on every request.

---

## Configuration

### Cache Settings

Configured in the `"Cache"` section of `appsettings.json`:

```json
{
  "Cache": {
    "Enabled": true,
    "Provider": "Redis"
  }
}
```

| Key        | Type      | Default  | Description                                                                 |
| :--------- | :-------- | :------- | :-------------------------------------------------------------------------- |
| `Enabled`  | `boolean` | `true`   | When `false`, all caching is disabled and every request calls the upstream. |
| `Provider` | `string`  | `"Redis"`| Cache backend. Must be either `"Redis"` or `"Memory"` (case-sensitive).     |

### Redis Settings

Required when `Provider` is `"Redis"` (or when omitted / default):

```json
{
  "Redis": {
    "Host": "localhost",
    "Port": 6379
  }
}
```

### Configuration Validation

The service performs **fail-fast validation** on startup:

- If `Provider` is missing, empty, or has an invalid value (`! "Redis"` and `!= "Memory"`), the application will throw an `OptionsValidationException` and fail to start.
- If `Enabled` is `true` and `Provider` is `"Redis"`, a Redis connection is established at startup. Invalid Redis connection strings will cause the application to fail.

---

## Cache Providers

### Redis (default)

Uses `StackExchange.Redis` to connect to an external Redis instance.

```csharp
// Registered as singleton ICacheService
new RedisCacheService(ConnectionMultiplexer)
```

**Best for:** Multi-instance deployments, shared cache state, production.

### Memory

Uses `Microsoft.Extensions.Caching.Memory.IMemoryCache` — an in-process, non-distributed cache.

```csharp
// Registered as singleton ICacheService
new MemoryCacheService(new MemoryCache(new MemoryCacheOptions()))
```

**Best for:** Development, testing, single-instance deployments, environments without Redis.

### Null / Disabled

A no-op implementation that discards all values. `GetAsync` always returns `null` and `SetAsync` does nothing.

```csharp
new NullCacheService()
```

**Best for:** Debugging, when you want to bypass the cache entirely without code changes.

---

## Cache Keys

All cache keys are built by the `CacheKeys` static class in `TradingProject.ThirdParty.Domain.Constants`. Keys follow a consistent naming convention:

```
{ServiceNamePrefix}{Provider}:{Resource}:{Parameters}
```

Where:
- `ServiceNamePrefix` = `"ThirdParty:"` (ensures all keys are namespaced)
- `Provider` = `"Binance"`, `"CoinGecko"`, `"News"`, `"Sentiment"`
- `Resource` = the specific data type (e.g. `"Price"`, `"Klines"`, `"Global"`)
- `Parameters` = query parameters (symbol, interval, limit, etc.)

**Examples:**

| Key | Purpose |
| :--- | :--- |
| `ThirdParty:Binance:Price:BTCUSDT` | Binance ticker price |
| `ThirdParty:Binance:Klines:BTCUSDT:1h:24` | OHLCV candles |
| `ThirdParty:Binance:ExchangeInfo:BTCUSDT` | Trading rules & filters |
| `ThirdParty:Binance:Ticker24h:BTCUSDT` | 24h statistics |
| `ThirdParty:Binance:Balances` | Wallet balances |
| `ThirdParty:Binance:MinNotional:BTCUSDT` | Min notional filter |
| `ThirdParty:CoinGecko:Price:bitcoin:usd` | CoinGecko price |
| `ThirdParty:CoinGecko:Global` | Global market data |
| `ThirdParty:CoinGecko:Trending` | Trending coins |
| `ThirdParty:News:BTC,ETH:10` | Aggregated news |
| `ThirdParty:Sentiment:FearAndGreed` | Fear & Greed index |

---

## Per-API Cache Durations

### Binance

| Data | Duration | Rationale |
| :--- | :------- | :-------- |
| Price | **30 s** | Near-real-time price display |
| Klines (candles) | **5 min** | Historical data, changes slowly |
| 24h Ticker | **1 min** | 24h stats change gradually |
| Balances | **1 min** | Balances change only during active trading |
| Min Notional / LOT_SIZE | **24 h** | Exchange filters rarely change |
| Exchange Info | **3 min** | Trading rules are stable |

### CoinGecko

| Data | Duration | Rationale |
| :--- | :------- | :-------- |
| Price (single/multi) | **5 min** | Free-tier rate limits are tight |
| Global market data | **5 min** | Updates every few minutes |
| Trending coins | **1 h** | Trending list changes slowly |

### News (RSS)

| Data | Duration | Rationale |
| :--- | :------- | :-------- |
| News articles | **15 min** | Articles don't appear every second |

### Sentiment

| Data | Duration | Rationale |
| :--- | :------- | :-------- |
| Fear & Greed Index | **1 h** | Index updates once a day |

---

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   Query Handler                          │
│  (reads cache first, misses → calls service → writes)   │
└────────────────────┬────────────────────────────────────┘
                     │ uses
                     ▼
┌─────────────────────────────────────────────────────────┐
│                   ICacheService                          │
│              (singleton, resolved at runtime)            │
├─────────────────────────────┬───────────────────────────┤
│  CacheSettings.Enabled=false │ CacheSettings.Provider    │
│  ────────────────────────── │ ────────────────────────  │
│  NullCacheService           │ "Redis" → RedisCacheService│
│                             │ "Memory"→ MemoryCacheService│
└─────────────────────────────┴───────────────────────────┘
```

The `ICacheService` is registered as a **singleton** in `DependencyInjection.cs`. The factory method reads `CacheSettings` at resolution time to decide which implementation to create:

```csharp
services.AddSingleton<ICacheService>(sp =>
{
    var cacheSettings = sp.GetRequiredService<IOptions<CacheSettings>>().Value;

    if (!cacheSettings.Enabled)
        return new NullCacheService();

    return cacheSettings.Provider switch
    {
        "Memory" => CreateMemoryCacheService(),
        _        => CreateRedisCacheService(sp),
    };
});
```

Each query handler follows the **Cache-Aside** pattern:
1. Check cache → if hit, return cached value
2. If miss, call the upstream API
3. Store the result in cache with the appropriate duration
4. Return the fresh value
