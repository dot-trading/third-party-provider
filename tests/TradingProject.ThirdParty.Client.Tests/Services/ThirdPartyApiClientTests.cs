using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using TradingProject.ThirdParty.Client.Models.Responses;
using TradingProject.ThirdParty.Client.Services;

namespace TradingProject.ThirdParty.Client.Tests.Services;

public class ThirdPartyApiClientTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly Mock<ILogger<ThirdPartyApiClient>> _loggerMock;
    private readonly HttpClient _httpClient;
    private readonly ThirdPartyApiClient _client;

    public ThirdPartyApiClientTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:5114/")
        };
        _loggerMock = new Mock<ILogger<ThirdPartyApiClient>>();
        _client = new ThirdPartyApiClient(_httpClient, _loggerMock.Object);
    }

    // =========================================================================
    //  GetBalancesAsync() — no symbol
    // =========================================================================

    [Fact]
    public async Task GetBalancesAsync_WhenApiReturnsValidResponse_ShouldReturnDeserializedResult()
    {
        // Arrange
        var expected = new ListBinanceBalanceResponse(
            Balances:
            [
                new BinanceBalanceDto("BTC", 1.5m, 0.5m),
                new BinanceBalanceDto("ETH", 10.0m, 1.0m),
                new BinanceBalanceDto("USDT", 5000.0m, 0.0m)
            ],
            MakerCommission: 10,
            TakerCommission: 10,
            Permissions: ["SPOT"]
        );

        var json = JsonSerializer.Serialize(expected);

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Get &&
                    r.RequestUri!.PathAndQuery == "/api/v1/Binance/balances"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });

        // Act
        var result = await _client.GetBalancesAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Balances.Should().HaveCount(3);
        result.Balances[0].Asset.Should().Be("BTC");
        result.Balances[0].Free.Should().Be(1.5m);
        result.Balances[0].Locked.Should().Be(0.5m);
        result.MakerCommission.Should().Be(10);
        result.TakerCommission.Should().Be(10);
        result.Permissions.Should().Contain("SPOT");

        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Get &&
                r.RequestUri!.PathAndQuery == "/api/v1/Binance/balances"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetBalancesAsync_WhenApiReturnsNull_ShouldReturnNull()
    {
        // Arrange
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("null")
            });

        // Act
        var result = await _client.GetBalancesAsync(CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBalancesAsync_WhenApiReturnsError_ShouldThrowHttpRequestException()
    {
        // Arrange
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Internal Server Error")
            });

        // Act
        var act = () => _client.GetBalancesAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetBalancesAsync_WhenCancelled_ShouldThrowTaskCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        // Act
        var act = () => _client.GetBalancesAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetBalancesAsync_ResponseMatchesV1ApiContract_ShouldDeserializeSuccessfully()
    {
        // Arrange — this JSON mirrors what the actual V1 API returns
        const string v1ApiJson = """
        {
            "balances": [
                { "asset": "BTC",  "free": 0.5, "locked": 0.0 },
                { "asset": "USDT", "free": 1200.0, "locked": 200.0 }
            ],
            "makerCommission": 10,
            "takerCommission": 10,
            "permissions": ["SPOT", "MARGIN"]
        }
        """;

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(v1ApiJson)
            });

        // Act
        var result = await _client.GetBalancesAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Balances.Should().HaveCount(2);
        result.Balances[0].Asset.Should().Be("BTC");
        result.Balances[0].Free.Should().Be(0.5m);
        result.Balances[1].Asset.Should().Be("USDT");
        result.Balances[1].Free.Should().Be(1200.0m);
        result.Balances[1].Locked.Should().Be(200.0m);
        result.MakerCommission.Should().Be(10);
        result.TakerCommission.Should().Be(10);
        result.Permissions.Should().Contain("SPOT").And.Contain("MARGIN");
    }

    // =========================================================================
    //  GetBalancesAsync(symbol) — with symbol filter
    // =========================================================================

    [Fact]
    public async Task GetBalancesBySymbolAsync_WhenApiReturnsValidResponse_ShouldReturnDeserializedResult()
    {
        // Arrange
        const string symbol = "BTCUSDT";
        var expected = new ListBinanceBalanceResponse(
            Balances:
            [
                new BinanceBalanceDto("BTC", 1.5m, 0.5m),
                new BinanceBalanceDto("USDT", 100.0m, 0.0m)
            ],
            MakerCommission: 10,
            TakerCommission: 10,
            Permissions: ["SPOT"]
        );

        var json = JsonSerializer.Serialize(expected);

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Get &&
                    r.RequestUri!.PathAndQuery == $"/api/v1/Binance/balances/{symbol}"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });

        // Act
        var result = await _client.GetBalancesAsync(symbol, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Balances.Should().HaveCount(2);
        result.Balances[0].Asset.Should().Be("BTC");
        result.Balances[0].Free.Should().Be(1.5m);
        result.Balances[0].Locked.Should().Be(0.5m);
        result.Balances[1].Asset.Should().Be("USDT");
        result.Balances[1].Free.Should().Be(100.0m);

        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Get &&
                r.RequestUri!.PathAndQuery == $"/api/v1/Binance/balances/{symbol}"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetBalancesBySymbolAsync_WhenSymbolIsNull_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _client.GetBalancesAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetBalancesBySymbolAsync_WhenApiReturnsNotFound_ShouldReturnNull()
    {
        // Arrange
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        // Act
        var result = await _client.GetBalancesAsync("UNKNOWN", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBalancesBySymbolAsync_WhenApiReturnsError_ShouldThrowHttpRequestException()
    {
        // Arrange
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        var act = () => _client.GetBalancesAsync("BTCUSDT", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetBalancesBySymbolAsync_WhenCancelled_ShouldThrowTaskCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        // Act
        var act = () => _client.GetBalancesAsync("BTCUSDT", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetBalancesBySymbolAsync_ResponseMatchesV1ApiContract_ShouldDeserializeSuccessfully()
    {
        // Arrange — this JSON mirrors what the actual V1 API returns
        const string v1ApiJson = """
        {
            "balances": [
                { "asset": "BTC",  "free": 1.5, "locked": 0.5 },
                { "asset": "USDT", "free": 100.0, "locked": 0.0 }
            ],
            "makerCommission": 10,
            "takerCommission": 10,
            "permissions": ["SPOT"]
        }
        """;

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Get &&
                    r.RequestUri!.PathAndQuery == "/api/v1/Binance/balances/BTCUSDT"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(v1ApiJson)
            });

        // Act
        var result = await _client.GetBalancesAsync("BTCUSDT", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Balances.Should().HaveCount(2);
        result.Balances[0].Asset.Should().Be("BTC");
        result.Balances[0].Free.Should().Be(1.5m);
        result.Balances[0].Locked.Should().Be(0.5m);
        result.Balances[1].Asset.Should().Be("USDT");
        result.Balances[1].Free.Should().Be(100.0m);
        result.MakerCommission.Should().Be(10);
        result.TakerCommission.Should().Be(10);
        result.Permissions.Should().Contain("SPOT");
    }

    // =========================================================================
    //  GetPriceAsync
    // =========================================================================

    [Fact]
    public async Task GetPriceAsync_WhenApiReturnsValidResponse_ShouldReturnDeserializedResult()
    {
        // Arrange
        const string symbol = "BTCUSDT";
        const decimal expectedPrice = 67890.12m;

        var json = $$"""
        {"price": {{expectedPrice}}}
        """;

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Get &&
                    r.RequestUri!.PathAndQuery == $"/api/v1/Binance/price/{symbol}"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });

        // Act
        var result = await _client.GetPriceAsync(symbol, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Price.Should().Be(expectedPrice);

        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Get &&
                r.RequestUri!.PathAndQuery == $"/api/v1/Binance/price/{symbol}"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetPriceAsync_WhenSymbolIsNull_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _client.GetPriceAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetPriceAsync_WhenApiReturnsNull_ShouldReturnNull()
    {
        // Arrange
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("null")
            });

        // Act
        var result = await _client.GetPriceAsync("BTCUSDT", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPriceAsync_WhenApiReturnsError_ShouldThrowHttpRequestException()
    {
        // Arrange
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        // Act
        var act = () => _client.GetPriceAsync("UNKNOWN", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetPriceAsync_WithHighPrecisionDecimal_ShouldPreserveFullPrecision()
    {
        // Arrange
        const string symbol = "BTCUSDT";
        // Uses 28 significant digits to verify decimal precision is preserved
        const string json = """
        {"price": 12345.678901234567890123456789}
        """;

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Get &&
                    r.RequestUri!.PathAndQuery == $"/api/v1/Binance/price/{symbol}"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });

        // Act
        var result = await _client.GetPriceAsync(symbol, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Price.Should().Be(12345.678901234567890123456789m);
    }

    [Fact]
    public async Task GetPriceAsync_WhenCancelled_ShouldThrowTaskCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        // Act
        var act = () => _client.GetPriceAsync("BTCUSDT", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetPriceAsync_ResponseMatchesV1ApiContract_ShouldDeserializeSuccessfully()
    {
        // Arrange — this JSON mirrors what the actual V1 API returns
        const string v1ApiJson = """
        {
            "price": 12345.67
        }
        """;

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Get &&
                    r.RequestUri!.PathAndQuery == "/api/v1/Binance/price/BTCUSDT"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(v1ApiJson)
            });

        // Act
        var result = await _client.GetPriceAsync("BTCUSDT", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Price.Should().Be(12345.67m);
    }

    // =========================================================================
    //  GetMinNotionalAsync
    // =========================================================================

    [Fact]
    public async Task GetMinNotionalAsync_WhenApiReturnsValidResponse_ShouldReturnDeserializedResult()
    {
        // Arrange
        const string symbol = "BTCUSDT";
        var expected = new BinanceNotionalResponse(
            FilterType: "MIN_NOTIONAL",
            StepSize: null,
            MinNotional: 10.0m,
            Notional: null
        );

        var json = JsonSerializer.Serialize(expected);

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Get &&
                    r.RequestUri!.PathAndQuery == $"/api/v1/Binance/notional/{symbol}"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });

        // Act
        var result = await _client.GetMinNotionalAsync(symbol, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.FilterType.Should().Be("MIN_NOTIONAL");
        result.MinNotional.Should().Be(10.0m);
        result.Notional.Should().BeNull();
        result.StepSize.Should().BeNull();

        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Get &&
                r.RequestUri!.PathAndQuery == $"/api/v1/Binance/notional/{symbol}"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetMinNotionalAsync_WhenSymbolIsNull_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _client.GetMinNotionalAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetMinNotionalAsync_WhenApiReturnsNull_ShouldReturnNull()
    {
        // Arrange
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("null")
            });

        // Act
        var result = await _client.GetMinNotionalAsync("BTCUSDT", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMinNotionalAsync_WhenApiReturnsError_ShouldThrowHttpRequestException()
    {
        // Arrange
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        // Act
        var act = () => _client.GetMinNotionalAsync("UNKNOWN", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetMinNotionalAsync_WhenAllDecimalFieldsAreNonNull_ShouldReturnAllValues()
    {
        // Arrange
        const string symbol = "BTCUSDT";
        var expected = new BinanceNotionalResponse(
            FilterType: "LOT_SIZE",
            StepSize: 0.001m,
            MinNotional: null,
            Notional: 15.5m
        );

        var json = JsonSerializer.Serialize(expected);

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Get &&
                    r.RequestUri!.PathAndQuery == $"/api/v1/Binance/notional/{symbol}"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });

        // Act
        var result = await _client.GetMinNotionalAsync(symbol, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.FilterType.Should().Be("LOT_SIZE");
        result.StepSize.Should().Be(0.001m);
        result.MinNotional.Should().BeNull();
        result.Notional.Should().Be(15.5m);
    }

    [Fact]
    public async Task GetMinNotionalAsync_WhenCancelled_ShouldThrowTaskCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        // Act
        var act = () => _client.GetMinNotionalAsync("BTCUSDT", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetMinNotionalAsync_ResponseMatchesV1ApiContract_ShouldDeserializeSuccessfully()
    {
        // Arrange — this JSON mirrors what the actual V1 API returns
        const string v1ApiJson = """
        {
            "filterType": "MIN_NOTIONAL",
            "stepSize": null,
            "minNotional": 10.0,
            "notional": null
        }
        """;

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Get &&
                    r.RequestUri!.PathAndQuery == "/api/v1/Binance/notional/BTCUSDT"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(v1ApiJson)
            });

        // Act
        var result = await _client.GetMinNotionalAsync("BTCUSDT", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.FilterType.Should().Be("MIN_NOTIONAL");
        result.MinNotional.Should().Be(10.0m);
        result.Notional.Should().BeNull();
        result.StepSize.Should().BeNull();
    }

    // =========================================================================
    //  GetKlinesAsync
    // =========================================================================

    [Fact]
    public async Task GetKlinesAsync_WhenApiReturnsValidResponse_ShouldReturnDeserializedResult()
    {
        // Arrange
        const string symbol = "BTCUSDT";
        const string interval = "1h";
        const int limit = 2;

        var expected = new BinanceKLineResponse[]
        {
            new(OpenTime: 1700000000000, Open: 50000.0m, High: 51000.0m, Low: 49000.0m, Close: 50500.0m, Volume: 100.5m),
            new(OpenTime: 1700003600000, Open: 50500.0m, High: 51500.0m, Low: 49500.0m, Close: 51000.0m, Volume: 200.0m)
        };

        var json = JsonSerializer.Serialize(expected);

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Get &&
                    r.RequestUri!.PathAndQuery == $"/api/v1/Binance/klines/{symbol}?interval={interval}&limit={limit}"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });

        // Act
        var result = await _client.GetKlinesAsync(symbol, interval, limit, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var klines = result!;
        klines.Should().HaveCount(2);
        klines[0].OpenTime.Should().Be(1700000000000);
        klines[0].Open.Should().Be(50000.0m);
        klines[0].High.Should().Be(51000.0m);
        klines[0].Low.Should().Be(49000.0m);
        klines[0].Close.Should().Be(50500.0m);
        klines[0].Volume.Should().Be(100.5m);
        klines[1].OpenTime.Should().Be(1700003600000);
        klines[1].Open.Should().Be(50500.0m);
        klines[1].Close.Should().Be(51000.0m);

        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Get &&
                r.RequestUri!.PathAndQuery == $"/api/v1/Binance/klines/{symbol}?interval={interval}&limit={limit}"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetKlinesAsync_WhenSymbolIsNull_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _client.GetKlinesAsync(null!, cancellationToken: CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetKlinesAsync_WhenIntervalIsNull_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _client.GetKlinesAsync("BTCUSDT", null!, cancellationToken: CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetKlinesAsync_WhenApiReturnsEmptyArray_ShouldReturnEmptyArray()
    {
        // Arrange
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]")
            });

        // Act
        var result = await _client.GetKlinesAsync("BTCUSDT", cancellationToken: CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetKlinesAsync_WhenApiReturnsError_ShouldThrowHttpRequestException()
    {
        // Arrange
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest
            });

        // Act
        var act = () => _client.GetKlinesAsync("UNKNOWN", cancellationToken: CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetKlinesAsync_WhenCancelled_ShouldThrowTaskCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        // Act
        var act = () => _client.GetKlinesAsync("BTCUSDT", cancellationToken: cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetKlinesAsync_ResponseMatchesV1ApiContract_ShouldDeserializeSuccessfully()
    {
        // Arrange — this JSON mirrors what the actual V1 API returns
        const string v1ApiJson = """
        [
            {
                "openTime": 1700000000000,
                "open": 50000.0,
                "high": 51000.0,
                "low": 49000.0,
                "close": 50500.0,
                "volume": 100.5
            },
            {
                "openTime": 1700003600000,
                "open": 50500.0,
                "high": 51500.0,
                "low": 49500.0,
                "close": 51000.0,
                "volume": 200.0
            }
        ]
        """;

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Get &&
                    r.RequestUri!.PathAndQuery == "/api/v1/Binance/klines/BTCUSDT?interval=1h&limit=24"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(v1ApiJson)
            });

        // Act
        var result = await _client.GetKlinesAsync("BTCUSDT", cancellationToken: CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var klines = result!;
        klines.Should().HaveCount(2);
        klines[0].OpenTime.Should().Be(1700000000000);
        klines[0].Open.Should().Be(50000.0m);
        klines[0].High.Should().Be(51000.0m);
        klines[0].Low.Should().Be(49000.0m);
        klines[0].Close.Should().Be(50500.0m);
        klines[0].Volume.Should().Be(100.5m);
        klines[1].OpenTime.Should().Be(1700003600000);
        klines[1].Open.Should().Be(50500.0m);
        klines[1].High.Should().Be(51500.0m);
        klines[1].Low.Should().Be(49500.0m);
        klines[1].Close.Should().Be(51000.0m);
        klines[1].Volume.Should().Be(200.0m);
    }

    // =========================================================================
    //  GetTicker24hAsync
    // =========================================================================

    [Fact]
    public async Task GetTicker24hAsync_WhenApiReturnsValidResponse_ShouldReturnDeserializedResult()
    {
        // Arrange
        const string symbol = "BTCUSDT";
        var expected = new BinanceTicker24HResponse(
            Symbol: symbol,
            Price: 50000.0m,
            PriceChangePercent: -2.5m,
            QuoteVolume: 1234567.89m,
            HighPrice: 51000.0m,
            LowPrice: 49000.0m
        );

        var json = JsonSerializer.Serialize(expected);

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Get &&
                    r.RequestUri!.PathAndQuery == $"/api/v1/Binance/ticker/{symbol}"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });

        // Act
        var result = await _client.GetTicker24hAsync(symbol, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Symbol.Should().Be(symbol);
        result.Price.Should().Be(50000.0m);
        result.PriceChangePercent.Should().Be(-2.5m);
        result.QuoteVolume.Should().Be(1234567.89m);
        result.HighPrice.Should().Be(51000.0m);
        result.LowPrice.Should().Be(49000.0m);

        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Get &&
                r.RequestUri!.PathAndQuery == $"/api/v1/Binance/ticker/{symbol}"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetTicker24hAsync_WhenSymbolIsNull_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _client.GetTicker24hAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetTicker24hAsync_WhenApiReturnsNotFound_ShouldReturnNull()
    {
        // Arrange
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        // Act
        var result = await _client.GetTicker24hAsync("UNKNOWN", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTicker24hAsync_WhenApiReturnsError_ShouldThrowHttpRequestException()
    {
        // Arrange
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        var act = () => _client.GetTicker24hAsync("BTCUSDT", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetTicker24hAsync_WithNegativePriceChangePercent_ShouldDeserializeCorrectly()
    {
        // Arrange
        const string json = """
        {
            "symbol": "BTCUSDT",
            "price": 49123.45,
            "priceChangePercent": -3.75,
            "quoteVolume": 987654.32,
            "highPrice": 52000.0,
            "lowPrice": 48500.0
        }
        """;

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Get &&
                    r.RequestUri!.PathAndQuery == "/api/v1/Binance/ticker/BTCUSDT"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });

        // Act
        var result = await _client.GetTicker24hAsync("BTCUSDT", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Symbol.Should().Be("BTCUSDT");
        result.Price.Should().Be(49123.45m);
        result.PriceChangePercent.Should().Be(-3.75m);
        result.QuoteVolume.Should().Be(987654.32m);
        result.HighPrice.Should().Be(52000.0m);
        result.LowPrice.Should().Be(48500.0m);
    }

    [Fact]
    public async Task GetTicker24hAsync_WhenCancelled_ShouldThrowTaskCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        // Act
        var act = () => _client.GetTicker24hAsync("BTCUSDT", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetTicker24hAsync_ResponseMatchesV1ApiContract_ShouldDeserializeSuccessfully()
    {
        // Arrange — this JSON mirrors what the actual V1 API returns
        const string v1ApiJson = """
        {
            "symbol": "BTCUSDT",
            "price": 50000.0,
            "priceChangePercent": -2.5,
            "quoteVolume": 1234567.89,
            "highPrice": 51000.0,
            "lowPrice": 49000.0
        }
        """;

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Get &&
                    r.RequestUri!.PathAndQuery == "/api/v1/Binance/ticker/BTCUSDT"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(v1ApiJson)
            });

        // Act
        var result = await _client.GetTicker24hAsync("BTCUSDT", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Symbol.Should().Be("BTCUSDT");
        result.Price.Should().Be(50000.0m);
        result.PriceChangePercent.Should().Be(-2.5m);
        result.QuoteVolume.Should().Be(1234567.89m);
        result.HighPrice.Should().Be(51000.0m);
        result.LowPrice.Should().Be(49000.0m);
    }

    // =========================================================================
    //  PlaceMarketBuyAsync
    // =========================================================================

    [Fact]
    public async Task PlaceMarketBuyAsync_WhenApiReturnsValidResponse_ShouldReturnDeserializedResult()
    {
        // Arrange
        var request = new PlaceMarketBuyRequest(Symbol: "BTCUSDT", QuoteOrderQty: 100.0m);
        var expected = new BinanceOrderResultResponse(
            OrderId: "12345",
            ExecutedQty: 0.0021m,
            CumulativeQuoteQty: 100.0m,
            Price: 47619.05m
        );

        var json = JsonSerializer.Serialize(expected);

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.RequestUri!.PathAndQuery == "/api/v1/Binance/order/buy"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });

        // Act
        var result = await _client.PlaceMarketBuyAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.OrderId.Should().Be("12345");
        result.ExecutedQty.Should().Be(0.0021m);
        result.CumulativeQuoteQty.Should().Be(100.0m);
        result.Price.Should().Be(47619.05m);

        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Post &&
                r.RequestUri!.PathAndQuery == "/api/v1/Binance/order/buy"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task PlaceMarketBuyAsync_WhenRequestIsNull_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _client.PlaceMarketBuyAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task PlaceMarketBuyAsync_WithLargeQuoteQty_ShouldPreservePrecision()
    {
        // Arrange
        var request = new PlaceMarketBuyRequest(Symbol: "BTCUSDT", QuoteOrderQty: 999999.123456789m);
        const string json = """
        {
            "orderId": "order-999",
            "executedQty": 21.123456789012345,
            "cumulativeQuoteQty": 999999.123456789,
            "price": 47356.78
        }
        """;

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.RequestUri!.PathAndQuery == "/api/v1/Binance/order/buy"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });

        // Act
        var result = await _client.PlaceMarketBuyAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.OrderId.Should().Be("order-999");
        result.ExecutedQty.Should().Be(21.123456789012345m);
        result.CumulativeQuoteQty.Should().Be(999999.123456789m);
        result.Price.Should().Be(47356.78m);
    }

    [Fact]
    public async Task PlaceMarketBuyAsync_WhenApiReturnsError_ShouldThrowHttpRequestException()
    {
        // Arrange
        var request = new PlaceMarketBuyRequest(Symbol: "BTCUSDT", QuoteOrderQty: 100.0m);

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\": \"insufficient balance\"}")
            });

        // Act
        var act = () => _client.PlaceMarketBuyAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task PlaceMarketBuyAsync_WhenCancelled_ShouldThrowTaskCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var request = new PlaceMarketBuyRequest(Symbol: "BTCUSDT", QuoteOrderQty: 100.0m);

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        // Act
        var act = () => _client.PlaceMarketBuyAsync(request, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task PlaceMarketBuyAsync_ResponseMatchesV1ApiContract_ShouldDeserializeSuccessfully()
    {
        // Arrange — this JSON mirrors what the actual V1 API returns
        const string v1ApiJson = """
        {
            "orderId": "binance-order-67890",
            "executedQty": 0.0021,
            "cumulativeQuoteQty": 100.0,
            "price": 47619.05
        }
        """;

        var request = new PlaceMarketBuyRequest(Symbol: "BTCUSDT", QuoteOrderQty: 100.0m);

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.RequestUri!.PathAndQuery == "/api/v1/Binance/order/buy"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(v1ApiJson)
            });

        // Act
        var result = await _client.PlaceMarketBuyAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.OrderId.Should().Be("binance-order-67890");
        result.ExecutedQty.Should().Be(0.0021m);
        result.CumulativeQuoteQty.Should().Be(100.0m);
        result.Price.Should().Be(47619.05m);
    }

    // =========================================================================
    //  PlaceMarketSellAsync
    // =========================================================================

    [Fact]
    public async Task PlaceMarketSellAsync_WhenApiReturnsValidResponse_ShouldReturnDeserializedResult()
    {
        // Arrange
        var request = new PlaceMarketSellRequest(Symbol: "BTCUSDT", Quantity: 0.1m);
        var expected = new BinanceOrderResultResponse(
            OrderId: "54321",
            ExecutedQty: 0.1m,
            CumulativeQuoteQty: 4750.0m,
            Price: 47500.0m
        );

        var json = JsonSerializer.Serialize(expected);

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.RequestUri!.PathAndQuery == "/api/v1/Binance/order/sell"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });

        // Act
        var result = await _client.PlaceMarketSellAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.OrderId.Should().Be("54321");
        result.ExecutedQty.Should().Be(0.1m);
        result.CumulativeQuoteQty.Should().Be(4750.0m);
        result.Price.Should().Be(47500.0m);

        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Post &&
                r.RequestUri!.PathAndQuery == "/api/v1/Binance/order/sell"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task PlaceMarketSellAsync_WhenRequestIsNull_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _client.PlaceMarketSellAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task PlaceMarketSellAsync_WithFractionalQuantity_ShouldPreservePrecision()
    {
        // Arrange
        var request = new PlaceMarketSellRequest(Symbol: "ETHUSDT", Quantity: 0.12345678m);
        const string json = """
        {
            "orderId": "sell-order-777",
            "executedQty": 0.12345678,
            "cumulativeQuoteQty": 301.23,
            "price": 2440.50
        }
        """;

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.RequestUri!.PathAndQuery == "/api/v1/Binance/order/sell"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });

        // Act
        var result = await _client.PlaceMarketSellAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.OrderId.Should().Be("sell-order-777");
        result.ExecutedQty.Should().Be(0.12345678m);
        result.CumulativeQuoteQty.Should().Be(301.23m);
        result.Price.Should().Be(2440.50m);
    }

    [Fact]
    public async Task PlaceMarketSellAsync_WhenApiReturnsError_ShouldThrowHttpRequestException()
    {
        // Arrange
        var request = new PlaceMarketSellRequest(Symbol: "BTCUSDT", Quantity: 0.1m);

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\": \"insufficient balance\"}")
            });

        // Act
        var act = () => _client.PlaceMarketSellAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task PlaceMarketSellAsync_WhenCancelled_ShouldThrowTaskCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var request = new PlaceMarketSellRequest(Symbol: "BTCUSDT", Quantity: 0.1m);

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        // Act
        var act = () => _client.PlaceMarketSellAsync(request, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task PlaceMarketSellAsync_ResponseMatchesV1ApiContract_ShouldDeserializeSuccessfully()
    {
        // Arrange — this JSON mirrors what the actual V1 API returns
        const string v1ApiJson = """
        {
            "orderId": "binance-sell-order-999",
            "executedQty": 0.1,
            "cumulativeQuoteQty": 4750.0,
            "price": 47500.0
        }
        """;

        var request = new PlaceMarketSellRequest(Symbol: "BTCUSDT", Quantity: 0.1m);

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.RequestUri!.PathAndQuery == "/api/v1/Binance/order/sell"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(v1ApiJson)
            });

        // Act
        var result = await _client.PlaceMarketSellAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.OrderId.Should().Be("binance-sell-order-999");
        result.ExecutedQty.Should().Be(0.1m);
        result.CumulativeQuoteQty.Should().Be(4750.0m);
        result.Price.Should().Be(47500.0m);
    }
}
