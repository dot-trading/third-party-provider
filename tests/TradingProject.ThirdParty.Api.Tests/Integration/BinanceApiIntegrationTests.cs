using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Application.Common.Models;
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
}
