# 📦 TradingProject.ThirdParty.Client — Client Library

The **TradingProject.ThirdParty.Client** NuGet package is a lightweight, typed client SDK for consuming the Third-Party Provider API (V1+). It provides ready-to-use DTOs, a service interface, and an `HttpClient`-backed implementation with built-in DI registration.

---

## 🎯 Supported .NET Versions

| Target Framework | Status |
| :--------------- | :----- |
| `net10.0`        | ✅     |
| `net9.0`         | ✅     |
| `net8.0`         | ✅     |

---

## 🚀 Getting Started

### 1. Install the Package

```shell
dotnet add package TradingProject.ThirdParty.Client
```

### 2. Register in DI

In your `Program.cs` (or composition root):

```csharp
using TradingProject.ThirdParty.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddThirdPartyApiClient(builder.Configuration);
```

### 3. Configure

Add the following section to your `appsettings.json`:

```json
{
  "ThirdPartyApi": {
    "BaseUrl": "http://localhost:5114",
    "ApiKey": "",
    "TimeoutSeconds": 30
  }
}
```

| Key               | Required | Description                                      |
| :---------------- | :------- | :----------------------------------------------- |
| `BaseUrl`         | ✅       | Base URL of the Third-Party Provider API         |
| `ApiKey`          | ❌       | Optional API key (sent as `X-Api-Key` header)   |
| `TimeoutSeconds`  | ❌       | Request timeout (default: 30, range: 1–300)      |

### 4. Inject & Use

```csharp
using TradingProject.ThirdParty.Client.Services;

public class MyService(IThirdPartyApiClient client)
{
    public async Task ShowBalances()
    {
        var balances = await client.GetBalancesAsync();
        foreach (var b in balances?.Balances ?? [])
        {
            Console.WriteLine($"{b.Asset}: Free={b.Free}, Locked={b.Locked}");
        }
    }
}
```

---

## 📡 API Endpoints Covered

### ✅ Currently Available

| Client Method               | API Endpoint                       | Version |
| :-------------------------- | :--------------------------------- | :------ |
| `GetBalancesAsync()`            | `GET /api/v1/Binance/balances`          | V1      |
| `GetPriceAsync(symbol)`         | `GET /api/v1/Binance/price/{symbol}`     | V1      |
| `GetMinNotionalAsync(symbol)`   | `GET /api/v1/Binance/notional/{symbol}`  | V1      |

### 🔜 Coming Soon (V1+)

| Client Method                  | API Endpoint                          |
| `GetKlinesAsync(symbol, ...)`  | `GET /api/v1/Binance/klines/{symbol}` |
| `GetTicker24hAsync(symbol)`    | `GET /api/v1/Binance/ticker/{symbol}` |
| `PlaceMarketBuyAsync(...)`     | `POST /api/v1/Binance/order/buy`      |
| `PlaceMarketSellAsync(...)`    | `POST /api/v1/Binance/order/sell`     |
| `GetMarketDataGlobalAsync()`   | `GET /api/v1/MarketData/global`       |
| `GetFearAndGreedAsync()`       | `GET /api/v1/MarketData/sentiment/fear-and-greed` |
| `GetNewsAsync(...)`            | `GET /api/v1/News`                    |

---

## 🧪 Testing

The library is designed for testability:

1. **Mock the interface** — inject `Mock<IThirdPartyApiClient>` in unit tests.
2. **Use the test project** — run `dotnet test` from the solution root to execute all client tests.
3. **Contract tests** — the test project includes a deserialization test that validates against the actual V1 API JSON contract.

### Example Mock

```csharp
var mock = new Mock<IThirdPartyApiClient>();
mock.Setup(c => c.GetBalancesAsync(It.IsAny<CancellationToken>()))
    .ReturnsAsync(new ListBinanceBalanceResponse(
        [new BinanceBalanceDto("BTC", 1.0, 0.0)],
        10, 10, ["SPOT"]
    ));
```

---

## 🏗 Project Structure

```
src/TradingProject.ThirdParty.Client/
├── Configuration/
│   └── ThirdPartyApiClientOptions.cs    # Strongly-typed options
├── Models/Responses/
│   └── BinanceBalanceResponse.cs        # V1 response DTOs
├── Services/
│   ├── IThirdPartyApiClient.cs          # Client interface
│   └── ThirdPartyApiClient.cs           # HttpClient implementation
└── DependencyInjection.cs               # DI extension method
```

---

## 🚢 NuGet Deployment

The package is built and published automatically by the CI pipeline when changes are merged to `main`. See the [CI workflow](../.github/workflows/ci.yml) for details.
