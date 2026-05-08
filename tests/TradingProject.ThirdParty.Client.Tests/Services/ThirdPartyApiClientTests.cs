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

    [Fact]
    public async Task GetBalancesAsync_WhenApiReturnsValidResponse_ShouldReturnDeserializedResult()
    {
        // Arrange
        var expected = new ListBinanceBalanceResponse(
            Balances:
            [
                new BinanceBalanceDto("BTC", 1.5, 0.5),
                new BinanceBalanceDto("ETH", 10.0, 1.0),
                new BinanceBalanceDto("USDT", 5000.0, 0.0)
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
        result.Balances[0].Free.Should().Be(1.5);
        result.Balances[0].Locked.Should().Be(0.5);
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
        result.Balances[0].Free.Should().Be(0.5);
        result.Balances[1].Asset.Should().Be("USDT");
        result.Balances[1].Free.Should().Be(1200.0);
        result.Balances[1].Locked.Should().Be(200.0);
        result.MakerCommission.Should().Be(10);
        result.TakerCommission.Should().Be(10);
        result.Permissions.Should().Contain("SPOT").And.Contain("MARGIN");
    }

    [Fact]
    public async Task GetPriceAsync_WhenApiReturnsValidResponse_ShouldReturnDeserializedResult()
    {
        // Arrange
        const string symbol = "BTCUSDT";
        const double expectedPrice = 67890.12;

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
        result!.Price.Should().Be(12345.67);
    }

    [Fact]
    public async Task GetMinNotionalAsync_WhenApiReturnsValidResponse_ShouldReturnDeserializedResult()
    {
        // Arrange
        const string symbol = "BTCUSDT";
        var expected = new BinanceNotionalResponse(
            FilterType: "MIN_NOTIONAL",
            StepSize: null,
            MinNotional: 10.0,
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
        result.MinNotional.Should().Be(10.0);
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
        result.MinNotional.Should().Be(10.0);
        result.Notional.Should().BeNull();
        result.StepSize.Should().BeNull();
    }

    [Fact]
    public async Task GetKlinesAsync_WhenApiReturnsValidResponse_ShouldReturnDeserializedResult()
    {
        // Arrange
        const string symbol = "BTCUSDT";
        const string interval = "1h";
        const int limit = 2;

        var expected = new BinanceKLineResponse[]
        {
            new(OpenTime: 1700000000000, Open: 50000.0, High: 51000.0, Low: 49000.0, Close: 50500.0, Volume: 100.5),
            new(OpenTime: 1700003600000, Open: 50500.0, High: 51500.0, Low: 49500.0, Close: 51000.0, Volume: 200.0)
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
        result!.Should().HaveCount(2);
        result[0].OpenTime.Should().Be(1700000000000);
        result[0].Open.Should().Be(50000.0);
        result[0].High.Should().Be(51000.0);
        result[0].Low.Should().Be(49000.0);
        result[0].Close.Should().Be(50500.0);
        result[0].Volume.Should().Be(100.5);
        result[1].OpenTime.Should().Be(1700003600000);
        result[1].Open.Should().Be(50500.0);
        result[1].Close.Should().Be(51000.0);

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
        result!.Should().HaveCount(2);
        result[0].OpenTime.Should().Be(1700000000000);
        result[0].Open.Should().Be(50000.0);
        result[0].High.Should().Be(51000.0);
        result[0].Low.Should().Be(49000.0);
        result[0].Close.Should().Be(50500.0);
        result[0].Volume.Should().Be(100.5);
        result[1].OpenTime.Should().Be(1700003600000);
        result[1].Open.Should().Be(50500.0);
        result[1].High.Should().Be(51500.0);
        result[1].Low.Should().Be(49500.0);
        result[1].Close.Should().Be(51000.0);
        result[1].Volume.Should().Be(200.0);
    }

    [Fact]
    public async Task GetTicker24hAsync_WhenApiReturnsValidResponse_ShouldReturnDeserializedResult()
    {
        // Arrange
        const string symbol = "BTCUSDT";
        var expected = new BinanceTicker24HResponse(
            Symbol: symbol,
            Price: 50000.0,
            PriceChangePercent: -2.5,
            QuoteVolume: 1234567.89,
            HighPrice: 51000.0,
            LowPrice: 49000.0
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
        result.Price.Should().Be(50000.0);
        result.PriceChangePercent.Should().Be(-2.5);
        result.QuoteVolume.Should().Be(1234567.89);
        result.HighPrice.Should().Be(51000.0);
        result.LowPrice.Should().Be(49000.0);

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
        result.Price.Should().Be(50000.0);
        result.PriceChangePercent.Should().Be(-2.5);
        result.QuoteVolume.Should().Be(1234567.89);
        result.HighPrice.Should().Be(51000.0);
        result.LowPrice.Should().Be(49000.0);
    }
}
