using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Application.Common.Models;
using TradingProject.ThirdParty.Domain.Constants;
using TradingProject.ThirdParty.Infrastructure.Services;
using TradingProject.ThirdParty.Infrastructure.Settings;

namespace TradingProject.ThirdParty.Application.Tests.Infrastructure.Services;

public class BinanceServiceTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string BaseUrl = "https://api.binance.com";
    private const string ApiKey = "test-api-key";
    private const string ApiSecret = "test-api-secret";
    private const long FixedTimestamp = 1_700_000_000_000;

    private readonly Mock<HttpMessageHandler> _handlerMock = new();
    private readonly Mock<ITimerService> _timerServiceMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly BinanceService _sut;
    private readonly HttpClient _httpClient;

    public BinanceServiceTests()
    {
        _timerServiceMock
            .Setup(t => t.BinanceNowDateTimeOffset())
            .Returns(FixedTimestamp);

        _httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri(BaseUrl)
        };

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(f => f.CreateClient(HttpClientNames.Binance))
            .Returns(_httpClient);

        var settings = Options.Create(new BinanceSettings
        {
            ApiKey = ApiKey,
            ApiSecret = ApiSecret,
            BaseUrl = BaseUrl
        });

        _sut = new BinanceService(
            httpClientFactoryMock.Object,
            _timerServiceMock.Object,
            settings,
            JsonOptions,
            _cacheServiceMock.Object);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    /// <summary>
    /// Sets up the mock HTTP handler to return an exchange-info response containing
    /// a LOT_SIZE filter (and optionally a MIN_NOTIONAL filter) for the given symbol,
    /// and optionally a price-ticker response.
    /// </summary>
    private void SetupExchangeInfoResponse(
        string symbol,
        string stepSize,
        string? minNotional = null)
    {
        var filters = new List<string>
        {
            $@"{{""filterType"":""LOT_SIZE"",""stepSize"":""{stepSize}""}}"
        };

        if (minNotional is not null)
        {
            filters.Add($@"{{""filterType"":""MIN_NOTIONAL"",""minNotional"":""{minNotional}""}}");
        }

        var exchangeInfoJson = $$"""
            {
                "symbols": [{
                    "filters": [{{string.Join(",", filters)}}]
                }]
            }
            """;

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Get &&
                    r.RequestUri!.PathAndQuery.Contains("/api/v3/exchangeInfo")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    exchangeInfoJson, Encoding.UTF8, "application/json")
            });
    }

    /// <summary>
    /// Sets up the mock HTTP handler to return a price-ticker response.
    /// </summary>
    private void SetupPriceResponse(string symbol, double price)
    {
        var priceJson = $$"""
            {
                "symbol": "{{symbol}}",
                "price": "{{price:F2}}"
            }
            """;

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Get &&
                    r.RequestUri!.PathAndQuery.Contains("/api/v3/ticker/price")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    priceJson, Encoding.UTF8, "application/json")
            });
    }

    /// <summary>
    /// Sets up the mock HTTP handler to return an order response.
    /// </summary>
    private void SetupOrderResponse(string json)
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.RequestUri!.PathAndQuery.Contains("/api/v3/order")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
    }

    /// <summary>
    /// Sets up the mock HTTP handler to return an order response, and captures the
    /// request so it can be inspected in the test.
    /// </summary>
    private void SetupOrderResponseWithCapture(
        string json,
        out TaskCompletionSource<HttpRequestMessage> requestSource)
    {
        var tcs = new TaskCompletionSource<HttpRequestMessage>();
        requestSource = tcs;

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.RequestUri!.PathAndQuery.Contains("/api/v3/order")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                tcs.TrySetResult(request);
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            });
    }

    private static string CreateOrderJson(
        double executedQty,
        double cummulativeQuoteQty,
        string status = "FILLED")
    {
        return $$"""
            {
                "symbol": "BTCUSDT",
                "orderId": 12345,
                "orderListId": -1,
                "clientOrderId": "test-order",
                "price": "0.00000000",
                "origQty": "{{executedQty:F8}}",
                "executedQty": "{{executedQty:F8}}",
                "cummulativeQuoteQty": "{{cummulativeQuoteQty:F8}}",
                "cumulativeQuoteQty": "{{cummulativeQuoteQty:F8}}",
                "status": "{{status}}",
                "timeInForce": "GTC",
                "type": "MARKET",
                "side": "SELL",
                "stopPrice": "0.00000000",
                "icebergQty": "0.00000000",
                "time": 123456789,
                "updateTime": 123456789,
                "workingTime": 123456789,
                "isWorking": true,
                "origQuoteOrderQty": "0.00000000",
                "selfTradePreventionMode": "NONE"
            }
            """;
    }

    // ========================================================================
    // PlaceMarketSellAsync – Happy path
    // ========================================================================

    [Fact]
    public async Task PlaceMarketSellAsync_WithValidInputs_ShouldPlaceOrderSuccessfully()
    {
        // Arrange
        const string symbol = "BTCUSDT";
        const double quantity = 1.234;
        const double stepSize = 0.001;
        const double price = 50_000.0;
        const double executedQty = 1.234;
        const double cummulativeQuoteQty = 61_700.0;

        SetupExchangeInfoResponse(symbol, stepSize.ToString("F3"), minNotional: "10.00000000");
        SetupPriceResponse(symbol, price);
        SetupOrderResponse(CreateOrderJson(executedQty, cummulativeQuoteQty));

        // Act
        var result = await _sut.PlaceMarketSellAsync(symbol, quantity);

        // Assert
        result.Should().NotBeNull();
        result.Symbol.Should().Be(symbol);
        result.Side.Should().Be("SELL");
        result.Type.Should().Be("MARKET");
        result.Status.Should().Be("FILLED");
        result.ExecutedQty.Should().Be(executedQty);
        result.Price.Should().BeApproximately(cummulativeQuoteQty / executedQty, 0.001);
    }

    // ========================================================================
    // PlaceMarketSellAsync – LOT_SIZE validation
    // ========================================================================

    [Fact]
    public async Task PlaceMarketSellAsync_WhenStepSizeIsNull_ShouldThrow()
    {
        // Arrange – exchange info with no LOT_SIZE filter
        const string symbol = "BTCUSDT";
        const string exchangeInfoJson = """
            {
                "symbols": [{
                    "filters": []
                }]
            }
            """;

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Get &&
                    r.RequestUri!.PathAndQuery.Contains("/api/v3/exchangeInfo")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(exchangeInfoJson, Encoding.UTF8, "application/json")
            });

        // Act
        var act = () => _sut.PlaceMarketSellAsync(symbol, 1.0);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*LotStepSize missing or invalid*");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.001)]
    public async Task PlaceMarketSellAsync_WhenStepSizeIsZeroOrNegative_ShouldThrow(double stepSize)
    {
        const string symbol = "BTCUSDT";
        var exchangeInfoJson = $$"""
            {
                "symbols": [{
                    "filters": [{
                        "filterType": "LOT_SIZE",
                        "stepSize": "{{stepSize:F8}}"
                    }]
                }]
            }
            """;

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Get &&
                    r.RequestUri!.PathAndQuery.Contains("/api/v3/exchangeInfo")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(exchangeInfoJson, Encoding.UTF8, "application/json")
            });

        var act = () => _sut.PlaceMarketSellAsync(symbol, 1.0);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*LotStepSize missing or invalid*");
    }

    // ========================================================================
    // PlaceMarketSellAsync – Zero-quantity guard
    // ========================================================================

    [Theory]
    [InlineData(0.0005, 0.001)]  // quantity < step size → alignedQty = 0
    [InlineData(0.0, 0.001)]
    [InlineData(-0.5, 0.001)]
    public async Task PlaceMarketSellAsync_WhenAlignedQtyIsZeroOrNegative_ShouldThrow(
        double quantity, double stepSize)
    {
        const string symbol = "BTCUSDT";

        SetupExchangeInfoResponse(symbol, stepSize.ToString("F3"), minNotional: "10.00000000");
        SetupPriceResponse(symbol, 50_000.0);
        // No order response needed – should throw before reaching PlaceOrderAsync

        var act = () => _sut.PlaceMarketSellAsync(symbol, quantity);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*zero or negative*")
            .WithMessage("*below the minimum step size*");
    }

    // ========================================================================
    // PlaceMarketSellAsync – MIN_NOTIONAL validation
    // ========================================================================

    [Fact]
    public async Task PlaceMarketSellAsync_WhenOrderValueIsBelowMinNotional_ShouldThrow()
    {
        // Arrange
        const string symbol = "BTCUSDT";
        const double quantity = 0.001;
        const double stepSize = 0.001;
        const double price = 100.0;       // order value = 0.001 * 100 = 0.10 USDT
        const string minNotional = "10.00000000"; // minimum is 10 USDT

        SetupExchangeInfoResponse(symbol, stepSize.ToString("F3"), minNotional);
        SetupPriceResponse(symbol, price);
        // No order response needed – should throw before reaching PlaceOrderAsync

        var act = () => _sut.PlaceMarketSellAsync(symbol, quantity);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*minimum notional*");
    }

    [Fact]
    public async Task PlaceMarketSellAsync_WhenNoMinNotionalFilter_ShouldSkipValidation()
    {
        // Arrange – exchange info without MIN_NOTIONAL filter
        const string symbol = "BTCUSDT";
        const double stepSize = 0.001;

        SetupExchangeInfoResponse(symbol, stepSize.ToString("F3"));  // no minNotional
        SetupPriceResponse(symbol, 100.0);
        SetupOrderResponse(CreateOrderJson(0.001, 0.10));

        // Act – should succeed even with a very small value because there's no MIN_NOTIONAL check
        var result = await _sut.PlaceMarketSellAsync(symbol, 0.001);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("FILLED");
    }

    // ========================================================================
    // PlaceMarketSellAsync – Quantity alignment & formatting
    // ========================================================================

    [Theory]
    [InlineData(1.234567, 0.001, "1.234")]
    [InlineData(1.234567, 0.01, "1.23")]
    [InlineData(1.234567, 0.1, "1.2")]
    [InlineData(1.234567, 1.0, "1")]
    [InlineData(15.6789, 0.01, "15.67")]
    [InlineData(100.0, 10.0, "100")]
    public async Task PlaceMarketSellAsync_ShouldAlignQuantityToStepSize_AndFormatCorrectly(
        double quantity, double stepSize, string expectedFormattedQty)
    {
        // Arrange
        const string symbol = "BTCUSDT";
        const double price = 50_000.0;

        SetupExchangeInfoResponse(symbol, stepSize.ToString("G"), minNotional: "10.00000000");
        SetupPriceResponse(symbol, price);

        SetupOrderResponseWithCapture(
            CreateOrderJson(
                double.Parse(expectedFormattedQty, System.Globalization.CultureInfo.InvariantCulture),
                double.Parse(expectedFormattedQty, System.Globalization.CultureInfo.InvariantCulture) * price),
            out var requestSource);

        // Act
        var result = await _sut.PlaceMarketSellAsync(symbol, quantity);

        // Assert
        result.Should().NotBeNull();

        var request = await requestSource.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var body = await request.Content!.ReadAsStringAsync();

        body.Should().Contain($"quantity={expectedFormattedQty}");
    }

    // ========================================================================
    // PlaceMarketSellAsync – Required query parameters
    // ========================================================================

    [Fact]
    public async Task PlaceMarketSellAsync_ShouldIncludeRequiredQueryParameters()
    {
        // Arrange
        const string symbol = "BTCUSDT";
        const double stepSize = 0.001;

        SetupExchangeInfoResponse(symbol, stepSize.ToString("F3"), minNotional: "10.00000000");
        SetupPriceResponse(symbol, 50_000.0);

        SetupOrderResponseWithCapture(CreateOrderJson(1.234, 61_700.0), out var requestSource);

        // Act
        await _sut.PlaceMarketSellAsync(symbol, 1.234);

        // Assert
        var request = await requestSource.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var body = await request.Content!.ReadAsStringAsync();

        body.Should().Contain("side=SELL");
        body.Should().Contain("type=MARKET");
        body.Should().Contain("recvWindow=5000");
        body.Should().Contain("newOrderRespType=RESULT");
        body.Should().Contain($"timestamp={FixedTimestamp}");

        // Verify the HMAC signature is present (should be 64 hex chars)
        var signatureParam = body.Split('&')
            .FirstOrDefault(p => p.StartsWith("signature="));
        signatureParam.Should().NotBeNull();
        signatureParam!.Split('=')[1].Should().MatchRegex("^[a-f0-9]{64}$");
    }

    [Fact]
    public async Task PlaceMarketSellAsync_ShouldUrlEncodeSymbol()
    {
        // Arrange
        const string symbol = "BTCUSDT";
        const double stepSize = 0.001;

        SetupExchangeInfoResponse(symbol, stepSize.ToString("F3"), minNotional: "10.00000000");
        SetupPriceResponse(symbol, 50_000.0);

        SetupOrderResponseWithCapture(CreateOrderJson(1.0, 50_000.0), out var requestSource);

        // Act
        await _sut.PlaceMarketSellAsync(symbol, 1.0);

        // Assert
        var request = await requestSource.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var body = await request.Content!.ReadAsStringAsync();

        // The symbol should appear correctly encoded in the query
        body.Should().MatchRegex(@"symbol=BTCUSDT&");
    }

    // ========================================================================
    // PlaceMarketSellAsync – Exchange info caching
    // ========================================================================

    [Fact]
    public async Task PlaceMarketSellAsync_WhenExchangeInfoIsCached_ShouldNotMakeHttpCallForExchangeInfo()
    {
        // Arrange
        const string symbol = "BTCUSDT";
        var cacheKey = CacheKeys.Binance.ExchangeInfo(symbol);

        var cachedExchangeInfoJson = $$"""
            {
                "symbols": [{
                    "filters": [
                        {"filterType": "LOT_SIZE", "stepSize": "0.001"},
                        {"filterType": "MIN_NOTIONAL", "minNotional": "10.00000000"}
                    ]
                }]
            }
            """;

        _cacheServiceMock
            .Setup(c => c.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedExchangeInfoJson);

        // Price and order endpoints still need HTTP
        SetupPriceResponse(symbol, 50_000.0);
        SetupOrderResponse(CreateOrderJson(1.234, 61_700.0));

        // Act
        var result = await _sut.PlaceMarketSellAsync(symbol, 1.234);

        // Assert
        result.Should().NotBeNull();
        result.ExecutedQty.Should().Be(1.234);

        // Verify exchange info was never requested via HTTP
        _handlerMock
            .Protected()
            .Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Get &&
                    r.RequestUri!.PathAndQuery.Contains("/api/v3/exchangeInfo")),
                ItExpr.IsAny<CancellationToken>());

        // Verify cache was checked
        _cacheServiceMock.Verify(
            c => c.GetAsync(cacheKey, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    // ========================================================================
    // PlaceOrderAsync – Error handling
    // ========================================================================

    [Fact]
    public async Task PlaceOrderAsync_WhenResponseBodyIsEmpty_ShouldThrowJsonException()
    {
        // Arrange — an empty response body causes System.Text.Json to throw a
        // JsonException during deserialization before the null check is reached.
        const string symbol = "BTCUSDT";

        SetupExchangeInfoResponse(symbol, "0.001", minNotional: "10.00000000");
        SetupPriceResponse(symbol, 50_000.0);

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.RequestUri!.PathAndQuery.Contains("/api/v3/order")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("", Encoding.UTF8, "application/json")
            });

        // Act
        var act = () => _sut.PlaceMarketSellAsync(symbol, 1.0);

        // Assert
        await act.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenResponseBodyIsNullJson_ShouldThrowInvalidOperationException()
    {
        // Arrange — a JSON "null" literal deserializes to null, hitting the null guard.
        const string symbol = "BTCUSDT";

        SetupExchangeInfoResponse(symbol, "0.001", minNotional: "10.00000000");
        SetupPriceResponse(symbol, 50_000.0);

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.RequestUri!.PathAndQuery.Contains("/api/v3/order")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("null", Encoding.UTF8, "application/json")
            });

        // Act
        var act = () => _sut.PlaceMarketSellAsync(symbol, 1.0);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to deserialize*");
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenHttpFails_ShouldThrowHttpRequestExceptionWithErrorBody()
    {
        // Arrange
        const string symbol = "BTCUSDT";

        SetupExchangeInfoResponse(symbol, "0.001", minNotional: "10.00000000");
        SetupPriceResponse(symbol, 50_000.0);

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.RequestUri!.PathAndQuery.Contains("/api/v3/order")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(
                    """{"code":-1013,"msg":"Filter failure: LOT_SIZE"}""",
                    Encoding.UTF8,
                    "application/json")
            });

        // Act
        var act = () => _sut.PlaceMarketSellAsync(symbol, 1.0);

        // Assert
        var ex = await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*Binance order failed*");
        ex.Which.Message.Should().Contain("-1013");
        ex.Which.Message.Should().Contain("LOT_SIZE");
    }

    // ========================================================================
    // PlaceOrderAsync – Average price calculation
    // ========================================================================

    [Fact]
    public async Task PlaceOrderAsync_ShouldCalculateAveragePriceFromCummulativeQuoteQty()
    {
        // Arrange
        const string symbol = "BTCUSDT";
        const double executedQty = 0.02;
        const double cummulativeQuoteQty = 999.87;
        const double expectedAvgPrice = cummulativeQuoteQty / executedQty; // ≈ 49_993.50

        SetupExchangeInfoResponse(symbol, "0.001", minNotional: "10.00000000");
        SetupPriceResponse(symbol, 50_000.0);
        SetupOrderResponse(CreateOrderJson(executedQty, cummulativeQuoteQty));

        // Act
        var result = await _sut.PlaceMarketSellAsync(symbol, 0.02);

        // Assert
        result.Should().NotBeNull();
        result.Price.Should().BeApproximately(expectedAvgPrice, 0.01);
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenNotFilled_ShouldReturnZeroAveragePrice()
    {
        // Arrange – no fills
        const string symbol = "BTCUSDT";
        const double executedQty = 0.0;
        const double cummulativeQuoteQty = 0.0;

        SetupExchangeInfoResponse(symbol, "0.001", minNotional: "10.00000000");
        SetupPriceResponse(symbol, 50_000.0);
        SetupOrderResponse(CreateOrderJson(executedQty, cummulativeQuoteQty, status: "NEW"));

        // Act
        var result = await _sut.PlaceMarketSellAsync(symbol, 1.0);

        // Assert
        result.Should().NotBeNull();
        result.Price.Should().Be(0.0);
    }

    // ========================================================================
    // PlaceMarketSellAsync – Timestamp freshness
    // ========================================================================

    [Fact]
    public async Task PlaceMarketSellAsync_ShouldUseFreshTimestamp_WhenPlacingOrder()
    {
        // Arrange
        const string symbol = "BTCUSDT";
        const double stepSize = 0.001;

        // The timer is called at the point the order query is built (after all async
        // pre-validation like exchange-info and price fetches), so it should reflect
        // a recent timestamp rather than one captured at the start of the method.
        var expectedTimestamp = FixedTimestamp;
        _timerServiceMock
            .Setup(t => t.BinanceNowDateTimeOffset())
            .Returns(expectedTimestamp);

        SetupExchangeInfoResponse(symbol, stepSize.ToString("F3"), minNotional: "10.00000000");
        SetupPriceResponse(symbol, 50_000.0);

        SetupOrderResponseWithCapture(CreateOrderJson(1.0, 50_000.0), out var requestSource);

        // Act
        await _sut.PlaceMarketSellAsync(symbol, 1.0);

        // Assert – the order query contains the fresh timestamp
        var request = await requestSource.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var body = await request.Content!.ReadAsStringAsync();
        body.Should().Contain($"timestamp={expectedTimestamp}");
    }

    // ========================================================================
    // PlaceOrderAsync – POST content type
    // ========================================================================

    [Fact]
    public async Task PlaceOrderAsync_ShouldSendAsFormUrlEncoded()
    {
        // Arrange
        const string symbol = "BTCUSDT";
        const double stepSize = 0.001;

        SetupExchangeInfoResponse(symbol, stepSize.ToString("F3"), minNotional: "10.00000000");
        SetupPriceResponse(symbol, 50_000.0);

        SetupOrderResponseWithCapture(CreateOrderJson(1.0, 50_000.0), out var requestSource);

        // Act
        await _sut.PlaceMarketSellAsync(symbol, 1.0);

        // Assert
        var request = await requestSource.Task.WaitAsync(TimeSpan.FromSeconds(2));
        request.Content.Should().NotBeNull();
        request.Content!.Headers.ContentType.Should().NotBeNull();
        request.Content.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");
    }
}
