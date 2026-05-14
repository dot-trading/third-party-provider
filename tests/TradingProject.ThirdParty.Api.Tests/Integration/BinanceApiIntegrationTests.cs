using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Application.Features.Binance.Commands.PlaceMarketBuy;
using TradingProject.ThirdParty.Application.Features.Binance.Commands.PlaceMarketSell;
using TradingProject.ThirdParty.Application.Common.Models;
using TradingProject.ThirdParty.Domain.Models.Market;
using Xunit;

namespace TradingProject.ThirdParty.Api.Tests.Integration;

public class BinanceApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IBinanceService> _binanceServiceMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    public BinanceApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing registrations
                var descriptor1 = services.SingleOrDefault(d => d.ServiceType == typeof(IBinanceService));
                if (descriptor1 != null) services.Remove(descriptor1);

                var descriptor2 = services.SingleOrDefault(d => d.ServiceType == typeof(ICacheService));
                if (descriptor2 != null) services.Remove(descriptor2);

                // Add mocks
                services.AddSingleton(_binanceServiceMock.Object);
                services.AddSingleton(_cacheServiceMock.Object);
            });
        });
    }

    [Fact]
    public async Task GetBalances_V0_ShouldReturnSimplifiedDictionary()
    {
        // Arrange
        var client = _factory.CreateClient();
        var balancesDto = new ListBinanceBalanceDto(
            new[]
            {
                new BinanceBalanceDto("BTC", 1.5, 0.5),
                new BinanceBalanceDto("USDT", 100.0, 0.0)
            },
            0,
            0,
            []
        );

        _binanceServiceMock.Setup(s => s.GetBalancesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(balancesDto);

        _cacheServiceMock.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var response = await client.GetAsync("/api/v0.0/Binance/balances");

        // Assert
        response.EnsureSuccessStatusCode();
        var balances = await response.Content.ReadFromJsonAsync<Dictionary<string, double>>();
        balances.Should().NotBeNull();
        balances.Should().HaveCount(2);
        balances!["BTC"].Should().Be(1.5);
        balances["USDT"].Should().Be(100.0);
    }

    [Fact]
    public async Task GetBalances_V1_ShouldReturnFullDto()
    {
        // Arrange
        var client = _factory.CreateClient();
        var balancesDto = new ListBinanceBalanceDto(
            new[]
            {
                new BinanceBalanceDto("BTC", 1.5, 0.5),
                new BinanceBalanceDto("USDT", 100.0, 0.0)
            },
            0,
            0,
            []
        );

        _binanceServiceMock.Setup(s => s.GetBalancesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(balancesDto);

        _cacheServiceMock.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var response = await client.GetAsync("/api/v1.0/Binance/balances");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ListBinanceBalanceDto>();
        result.Should().NotBeNull();
        result!.Balances.Should().HaveCount(2);
        result.Balances[0].Asset.Should().Be("BTC");
        result.Balances[0].Free.Should().Be(1.5);
        result.Balances[0].Locked.Should().Be(0.5);
    }

    [Fact]
    public async Task GetBalancesBySymbol_V1_WhenFound_ShouldReturnFilteredBalances()
    {
        // Arrange
        var client = _factory.CreateClient();
        var symbol = "BTCUSDT";
        var allBalancesDto = new ListBinanceBalanceDto(
            new[]
            {
                new BinanceBalanceDto("BTC", 1.5, 0.5),
                new BinanceBalanceDto("USDT", 100.0, 0.0),
                new BinanceBalanceDto("ETH", 10.0, 0.0)
            },
            0,
            0,
            []
        );
        var exchangeInfoDto = new BinanceExchangeInfoDto
        {
            Symbols =
            [
                new BinanceSymbolDto(
                    BaseAsset: "BTC",
                    QuoteAsset: "USDT",
                    Filters: []
                )
            ]
        };

        _binanceServiceMock.Setup(s => s.GetBalancesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allBalancesDto);

        _binanceServiceMock.Setup(s => s.GetExchangeInfoAsync(symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exchangeInfoDto);

        _cacheServiceMock.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var response = await client.GetAsync($"/api/v1.0/Binance/balances/{symbol}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ListBinanceBalanceDto>();
        result.Should().NotBeNull();
        result!.Balances.Should().HaveCount(2);
        result.Balances[0].Asset.Should().Be("BTC");
        result.Balances[0].Free.Should().Be(1.5);
        result.Balances[1].Asset.Should().Be("USDT");
        result.Balances[1].Free.Should().Be(100.0);
    }

    [Fact]
    public async Task GetBalancesBySymbol_V1_WhenSymbolNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        var symbol = "UNKNOWN";

        _binanceServiceMock.Setup(s => s.GetExchangeInfoAsync(symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BinanceExchangeInfoDto?)null);

        _cacheServiceMock.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var response = await client.GetAsync($"/api/v1.0/Binance/balances/{symbol}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPrice_V0_ShouldReturnDouble()
    {
        // Arrange
        var client = _factory.CreateClient();
        var symbol = "BTCUSDT";
        var priceDto = new BinancePriceDto(50000.0);

        _binanceServiceMock.Setup(s => s.GetCurrentPriceAsync(symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync(priceDto);

        _cacheServiceMock.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var response = await client.GetAsync($"/api/v0.0/Binance/price/{symbol}");

        // Assert
        response.EnsureSuccessStatusCode();
        var price = await response.Content.ReadFromJsonAsync<double>();
        price.Should().Be(50000.0);
    }

    [Fact]
    public async Task GetPrice_V1_ShouldReturnFullDto()
    {
        // Arrange
        var client = _factory.CreateClient();
        var symbol = "BTCUSDT";
        var priceDto = new BinancePriceDto(50000.0);

        _binanceServiceMock.Setup(s => s.GetCurrentPriceAsync(symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync(priceDto);

        _cacheServiceMock.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var response = await client.GetAsync($"/api/v1.0/Binance/price/{symbol}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BinancePriceDto>();
        result.Should().NotBeNull();
        result!.Price.Should().Be(50000.0);
    }

    [Fact]
    public async Task GetMinNotional_V0_ShouldReturnDouble()
    {
        // Arrange
        var client = _factory.CreateClient();
        var symbol = "BTCUSDT";
        var filterDto = new BinanceFilterDto("MIN_NOTIONAL", null, 10.0, null);

        _binanceServiceMock.Setup(s => s.GetMinNotionalAsync(symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync(filterDto);

        _cacheServiceMock.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var response = await client.GetAsync($"/api/v0.0/Binance/notional/{symbol}");

        // Assert
        response.EnsureSuccessStatusCode();
        var value = await response.Content.ReadFromJsonAsync<double>();
        value.Should().Be(10.0);
    }

    [Fact]
    public async Task GetMinNotional_V1_ShouldReturnFullDto()
    {
        // Arrange
        var client = _factory.CreateClient();
        var symbol = "BTCUSDT";
        var filterDto = new BinanceFilterDto("MIN_NOTIONAL", null, 10.0, null);

        _binanceServiceMock.Setup(s => s.GetMinNotionalAsync(symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync(filterDto);

        _cacheServiceMock.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var response = await client.GetAsync($"/api/v1.0/Binance/notional/{symbol}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BinanceFilterDto>();
        result.Should().NotBeNull();
        result!.MinNotional.Should().Be(10.0);
        result.FilterType.Should().Be("MIN_NOTIONAL");
    }

    [Fact]
    public async Task GetKlines_V0_ShouldReturnFullDto()
    {
        // Arrange
        var client = _factory.CreateClient();
        var symbol = "BTCUSDT";
        var klinesDto = new[]
        {
            new KLineDto(1715112000000, 50000.0, 51000.0, 49000.0, 50500.0, 100.0)
        };

        _binanceServiceMock.Setup(s => s.GetKLinesAsync(symbol, "1h", 24, It.IsAny<CancellationToken>()))
            .ReturnsAsync(klinesDto);

        _cacheServiceMock.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var response = await client.GetAsync($"/api/v0.0/Binance/klines/{symbol}?interval=1h&limit=24");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<KLineDto[]>();
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result![0].OpenTime.Should().Be(1715112000000);
        result[0].Close.Should().Be(50500.0);
    }

    [Fact]
    public async Task GetKlines_V1_ShouldReturnFullDto()
    {
        // Arrange
        var client = _factory.CreateClient();
        var symbol = "BTCUSDT";
        var klinesDto = new[]
        {
            new KLineDto(1715112000000, 50000.0, 51000.0, 49000.0, 50500.0, 100.0)
        };

        _binanceServiceMock.Setup(s => s.GetKLinesAsync(symbol, "1h", 24, It.IsAny<CancellationToken>()))
            .ReturnsAsync(klinesDto);

        _cacheServiceMock.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var response = await client.GetAsync($"/api/v1.0/Binance/klines/{symbol}?interval=1h&limit=24");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<KLineDto[]>();
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result![0].OpenTime.Should().Be(1715112000000);
        result[0].Close.Should().Be(50500.0);
    }

    [Fact]
    public async Task GetTicker24h_V0_ShouldReturnFullDto()
    {
        // Arrange
        var client = _factory.CreateClient();
        var symbol = "BTCUSDT";
        var tickerDto = new BinanceTicker24HDto(symbol, 50000.0, 5.0, 1000.0, 51000.0, 49000.0);

        _binanceServiceMock.Setup(s => s.GetTicker24HAsync(symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tickerDto);

        _cacheServiceMock.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var response = await client.GetAsync($"/api/v0.0/Binance/ticker/{symbol}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Ticker24h>();
        result.Should().NotBeNull();
        result!.Symbol.Should().Be(symbol);
        result.Price.Should().Be(50000.0);
    }

    [Fact]
    public async Task GetTicker24h_V1_ShouldReturnFullDto()
    {
        // Arrange
        var client = _factory.CreateClient();
        var symbol = "BTCUSDT";
        var tickerDto = new BinanceTicker24HDto(symbol, 50000.0, 5.0, 1000.0, 51000.0, 49000.0);

        _binanceServiceMock.Setup(s => s.GetTicker24HAsync(symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tickerDto);

        _cacheServiceMock.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var response = await client.GetAsync($"/api/v1.0/Binance/ticker/{symbol}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Ticker24h>();
        result.Should().NotBeNull();
        result!.Symbol.Should().Be(symbol);
        result.Price.Should().Be(50000.0);
    }

    [Fact]
    public async Task PlaceMarketBuy_V1_ShouldReturnSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();
        var command = new PlaceMarketBuyCommand("BTCUSDT", 100.0m);
        var orderDto = new BinanceOrderDto(
            "BTCUSDT", 12345, -1, "client_id", 50000.0, 0.002, 0.002, 100.0, 100.0,
            "FILLED", "GTC", "MARKET", "BUY", 0, 0, 1715112000000, 1715112000000, 1715112000000, true, 0, "NONE");

        _binanceServiceMock.Setup(s => s.PlaceMarketBuyAsync(command.Symbol, command.QuoteOrderQty, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orderDto);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1.0/Binance/order/buy", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BinanceOrderResultDto>();
        result.Should().NotBeNull();
        result!.OrderId.Should().Be("12345");
        result.ExecutedQty.Should().Be(0.002);
    }

    [Fact]
    public async Task PlaceMarketBuy_V1_InvalidCommand_ShouldReturnBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var command = new PlaceMarketBuyCommand("", -10.0m); // Invalid symbol and qty

        // Act
        var response = await client.PostAsJsonAsync("/api/v1.0/Binance/order/buy", command);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var error = await response.Content.ReadAsStringAsync();
        error.Should().Contain("Symbol is required");
        error.Should().Contain("Quote order quantity must be greater than zero");
    }

    [Fact]
    public async Task PlaceMarketSell_V1_ShouldReturnSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();
        var command = new PlaceMarketSellCommand("BTCUSDT", 0.002m);
        var orderDto = new BinanceOrderDto(
            "BTCUSDT", 67890, -1, "client_id", 50000.0, 0.002, 0.002, 100.0, 100.0,
            "FILLED", "GTC", "MARKET", "SELL", 0, 0, 1715112000000, 1715112000000, 1715112000000, true, 0, "NONE");

        _binanceServiceMock.Setup(s => s.PlaceMarketSellAsync(command.Symbol, command.Quantity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orderDto);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1.0/Binance/order/sell", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BinanceOrderResultDto>();
        result.Should().NotBeNull();
        result!.OrderId.Should().Be("67890");
        result.ExecutedQty.Should().Be(0.002);
    }

    [Fact]
    public async Task PlaceMarketSell_V1_InvalidCommand_ShouldReturnBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var command = new PlaceMarketSellCommand("", -1.0m); // Invalid symbol and quantity

        // Act
        var response = await client.PostAsJsonAsync("/api/v1.0/Binance/order/sell", command);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var error = await response.Content.ReadAsStringAsync();
        error.Should().Contain("Symbol is required");
        error.Should().Contain("Quantity must be greater than zero");
    }
}
