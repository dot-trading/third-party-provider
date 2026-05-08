# third-party-provider

ASP.NET Core service that acts as a unified gateway to external market data APIs.
All endpoints are cached in Redis to minimise upstream API calls and stay within free-tier rate limits.

## Architecture

```
Api (Controllers)
  └── Application (MediatR CQRS — Queries / Commands)
        ├── Abstractions (interfaces)
        └── Features/
              ├── Binance/         — price, klines, ticker, balances, orders
              ├── MarketData/      — CoinGecko price, global market data, trending
              ├── Sentiment/       — Fear & Greed Index (AlternativeMe)
              └── News/            — CryptoPanic news feed
Infrastructure (service implementations, HTTP clients, Redis cache)
```

## Endpoints

### Binance — `api/market-data`

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/market-data/price/{symbol}` | Current price from Binance |
| GET | `/api/market-data/notional/{symbol}` | Minimum notional value for a symbol |
| GET | `/api/market-data/klines/{symbol}?interval=1h&limit=24` | OHLCV klines |
| GET | `/api/market-data/ticker/{symbol}` | 24h ticker stats |

### Binance — `api/account`

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/account/balances` | Wallet balances |

### Binance — `api/trading`

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/trading/order` | Place a raw signed order |
| POST | `/api/trading/buy` | Place a market buy order |
| POST | `/api/trading/sell` | Place a market sell order |

### CoinGecko — `api/market-data`

| Method | Path | Cache | Description |
|--------|------|-------|-------------|
| GET | `/api/market-data/price/coingecko/{coinId}?vsCurrency=usd` | 5 min | Coin price |
| GET | `/api/market-data/global` | 5 min | BTC/ETH dominance, total market cap, 24h change |
| GET | `/api/market-data/trending` | 1 hour | Top 7 trending coins by search volume |

### Sentiment — `api/market-data`

| Method | Path | Cache | Description |
|--------|------|-------|-------------|
| GET | `/api/market-data/sentiment/fear-and-greed` | 1 hour | Fear & Greed Index with classification |

### News — `api/news`

| Method | Path | Cache | Description |
|--------|------|-------|-------------|
| GET | `/api/news?currencies=BTC,ETH&limit=10` | 15 min | Latest crypto news aggregated from public RSS feeds |

**News query parameters:**
- `currencies` — comma-separated symbols to filter by title match (e.g., `BTC,ETH`). Omit for all news.
- `limit` — max articles to return (default: `10`).

## Configuration

| Section | Key | Description | Default |
|---------|-----|-------------|---------|
| `Binance` | `BaseUrl` | Binance REST API base URL | — |
| `Binance` | `ApiKey` | Binance API key | — |
| `Binance` | `ApiSecret` | Binance API secret | — |
| `CoinGecko` | `ApiKey` | CoinGecko demo API key (optional, increases rate limit) | — |
| `RssNews` | `FeedUrls` | JSON array of RSS feed URLs to aggregate | CoinDesk + CoinTelegraph |
| `Redis` | `ConnectionString` | Redis connection string | — |

### RSS news feeds

News is aggregated from public RSS 2.0 feeds — no API key required.

**Two-layer aggregation strategy:**

1. **CryptoPanic per-coin feeds** (dynamic, symbol-driven) — when currencies are specified,
   the service automatically fetches `https://cryptopanic.com/news/{slug}/rss/` for each known
   symbol. Articles are pre-filtered at source, so no client-side title matching is needed.
   Supported symbols: BTC, ETH, BNB, SOL, XRP, ADA, AVAX, DOT, NEAR, MATIC, LINK, UNI,
   AAVE, CRV, OP, ARB, DOGE, SHIB, PEPE, FLOKI, WLD, LTC, BCH, TON, SUI, APT.

2. **Configured general feeds** (static, always fetched) — default: CoinDesk and CoinTelegraph.
   When currencies are specified, articles are filtered by symbol presence in the title.
   Override or extend via `RssNews:FeedUrls` in `appsettings.json`:

```json
"RssNews": {
  "FeedUrls": [
    "https://www.coindesk.com/arc/outboundfeeds/rss/",
    "https://cointelegraph.com/rss",
    "https://decrypt.co/feed"
  ]
}
```

Results from both layers are deduplicated by URL, sorted by date descending, then truncated to `limit`.
Individual feed failures are silenced — the remaining feeds still contribute results.

## External APIs

| Service | Endpoint | Rate limit | Docs |
|---------|----------|------------|------|
| Binance | `api.binance.com` | ~1200 req/min (weight-based) | [Binance API](https://binance-docs.github.io/apidocs/spot/en/) |
| CoinGecko | `api.coingecko.com/api/v3` | ~30 req/min (demo key) | [CoinGecko API](https://docs.coingecko.com/reference/introduction) |
| AlternativeMe | `api.alternative.me` | No documented limit | [Alternative.me](https://alternative.me/crypto/fear-and-greed-index/) |
| CoinDesk RSS | `coindesk.com/arc/outboundfeeds/rss/` | No limit | Public RSS |
| CoinTelegraph RSS | `cointelegraph.com/rss` | No limit | Public RSS |
