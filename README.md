# 🌐 Third-Party Provider Gateway

![.NET](https://img.shields.io/badge/.NET-10.0-512bd4?style=for-the-badge&logo=dotnet)
![Redis](https://img.shields.io/badge/redis-%23DD0031.svg?style=for-the-badge&logo=redis&logoColor=white)
![Docker](https://img.shields.io/badge/docker-%230db7ed.svg?style=for-the-badge&logo=docker&logoColor=white)

The **Third-Party Provider** is a high-performance ASP.NET Core gateway designed to act as a unified bridge between your trading infrastructure and external market data providers. It simplifies integration by providing a consistent API while handling rate-limiting, caching, and resilience.

---

## 📖 Documentation

| Document | Description |
| :------- | :---------- |
| [🔄 Caching](docs/caching.md) | Caching architecture, providers, configuration, keys, and durations |

---

## ✨ Key Features

*   **Unified API**: Access Binance, CoinGecko, and Sentiment data through a single, clean interface.
*   **Smart Caching**: Integrated caching with **Redis** (default) or **in-memory** support — see [🔄 Caching](docs/caching.md).
*   **Resilient HTTP**: Built with **Polly** resilience policies (Retry, Circuit Breaker) to handle transient network failures.
*   **News Aggregator**: Advanced RSS engine that fetches and filters news from CryptoPanic and major news outlets.
*   **Strict Validation**: Fail-fast configuration validation to ensure system integrity on startup.

---

## 🏗 Architecture

The service follows **Clean Architecture** principles with a MediatR-based CQRS pattern:

```text
Api (Controllers)
  └── Application (CQRS — Queries & Commands)
        ├── Abstractions (Interfaces)
        └── Features/
              ├── Binance/     → Price, Klines, Ticker, Balances, Orders
              ├── MarketData/  → CoinGecko & Global Market Metrics
              ├── Sentiment/   → Fear & Greed Index
              └── News/        → Multi-source RSS Aggregation
Infrastructure (HTTP Clients, Resilience, Redis Cache)
```

---

## 🚀 Getting Started

### 1. Prerequisites
*   .NET 10 SDK
*   Redis Instance

### 2. Configuration
The service is configured via `appsettings.json` or Environment Variables.

| Section | Key | Description |
| :--- | :--- | :--- |
| **Binance** | `ApiKey` | Your Binance API Key |
| **Binance** | `ApiSecret` | Your Binance API Secret |
| **CoinGecko** | `ApiKey` | (Optional) Demo API Key to increase limits |
| **Redis** | `Host` | Redis server hostname |
| **RssNews** | `FeedUrls` | List of RSS feeds to monitor |

---

## 📡 API Reference

### 📊 Market Data (Binance)
| Endpoint | Description |
| :--- | :--- |
| `GET /api/market-data/price/{symbol}` | Get the current ticker price. |
| `GET /api/market-data/klines/{symbol}` | Fetch OHLCV candles (default 1h). |
| `GET /api/market-data/ticker/{symbol}` | 24h price change statistics. |

### 📈 Global Insights
| Endpoint | Source | Cache |
| :--- | :--- | :--- |
| `GET /api/market-data/global` | CoinGecko | 5m |
| `GET /api/market-data/sentiment/fear-and-greed` | AlternativeMe | 1h |
| `GET /api/news` | RSS Engine | 15m |

### 💰 Trading & Account
| Endpoint | Description |
| :--- | :--- |
| `GET /api/account/balances` | Retrieve current wallet balances. |
| `POST /api/trading/buy` | Execute a Market Buy order. |
| `POST /api/trading/sell` | Execute a Market Sell order. |

---

## 🤝 Contributing & Support

If this project helps you in your trading journey, feel free to contribute or give it a ⭐!

### 💎 Donations
Community support helps keep the development active and covers API subscription costs.

*   **BTC**: `1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa`
*   **ETH (ERC20)**: `0x742d35Cc6634C0532925a3b844Bc454e4438f44e`
*   **USDT (TRC20)**: `TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t`

---

## 📄 License
This project is licensed under the MIT License.
