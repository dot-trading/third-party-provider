using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TradingProject.ThirdParty.Api.Controllers.V1;
using TradingProject.ThirdParty.Application.Common.Models;
using TradingProject.ThirdParty.Application.Features.Binance.Queries.GetBalances;
using TradingProject.ThirdParty.Application.Features.Binance.Queries.GetPrice;
using TradingProject.ThirdParty.Application.Features.Binance.Queries.GetMinNotional;
using TradingProject.ThirdParty.Application.Features.Binance.Queries.GetKlines;
using TradingProject.ThirdParty.Application.Features.Binance.Queries.GetTicker24h;
using TradingProject.ThirdParty.Application.Features.Binance.Commands.PlaceMarketBuy;
using TradingProject.ThirdParty.Application.Features.Binance.Commands.PlaceMarketSell;
using TradingProject.ThirdParty.Domain.Models.Market;
using Xunit;

namespace TradingProject.ThirdParty.Api.Tests.Controllers.V1;

public class BinanceControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly BinanceController _controller;

    public BinanceControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new BinanceController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetBalances_ShouldReturnOkWithFullDto()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var expectedDto = new ListBinanceBalanceDto(
            new[]
            {
                new BinanceBalanceDto("BTC", 1.0, 0.0),
                new BinanceBalanceDto("ETH", 10.0, 1.0)
            },
            0,
            0,
            []
        );

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetBalancesQuery>(), cancellationToken))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.GetBalancesAsync(cancellationToken);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedDto);
        _mediatorMock.Verify(m => m.Send(It.IsAny<GetBalancesQuery>(), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetBalancesBySymbol_WhenFound_ShouldReturnOkWithFullDto()
    {
        // Arrange
        var symbol = "BTCUSDT";
        var cancellationToken = CancellationToken.None;
        var expectedDto = new ListBinanceBalanceDto(
            new[]
            {
                new BinanceBalanceDto("BTC", 1.0, 0.0),
                new BinanceBalanceDto("USDT", 50000.0, 0.0)
            },
            0,
            0,
            []
        );

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetBalancesBySymbolQuery>(), cancellationToken))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.GetBalancesAsync(symbol, cancellationToken);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedDto);
        _mediatorMock.Verify(m => m.Send(It.Is<GetBalancesBySymbolQuery>(q => q.Symbol == symbol), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetBalancesBySymbol_WhenNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var symbol = "UNKNOWN";
        var cancellationToken = CancellationToken.None;

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetBalancesBySymbolQuery>(), cancellationToken))
            .ReturnsAsync((ListBinanceBalanceDto?)null);

        // Act
        var result = await _controller.GetBalancesAsync(symbol, cancellationToken);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        _mediatorMock.Verify(m => m.Send(It.Is<GetBalancesBySymbolQuery>(q => q.Symbol == symbol), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetPrice_ShouldReturnOkWithFullDto()
    {
        // Arrange
        var symbol = "BTCUSDT";
        var cancellationToken = CancellationToken.None;
        var expectedDto = new BinancePriceDto(50000.0);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPriceQuery>(), cancellationToken))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.GetPriceAsync(symbol, cancellationToken);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedDto);
        _mediatorMock.Verify(m => m.Send(It.Is<GetPriceQuery>(q => q.Symbol == symbol), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetMinNotionalAsync_ShouldReturnOkWithFullDto()
    {
        // Arrange
        var symbol = "BTCUSDT";
        var cancellationToken = CancellationToken.None;
        var expectedDto = new BinanceFilterDto("MIN_NOTIONAL", null, 10.0, null);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetMinNotionalQuery>(), cancellationToken))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.GetMinNotionalAsync(symbol, cancellationToken);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedDto);
        _mediatorMock.Verify(m => m.Send(It.Is<GetMinNotionalQuery>(q => q.Symbol == symbol), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetKlines_ShouldReturnOkWithFullDto()
    {
        // Arrange
        var symbol = "BTCUSDT";
        var cancellationToken = CancellationToken.None;
        var expectedDto = new[]
        {
            new KLineDto(1715112000000, 50000.0, 51000.0, 49000.0, 50500.0, 100.0)
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetKlinesQuery>(), cancellationToken))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.GetKlinesAsync(symbol, "1h", 24, cancellationToken);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedDto);
        _mediatorMock.Verify(m => m.Send(It.IsAny<GetKlinesQuery>(), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetTicker24HAsync_ShouldReturnOkWithFullDto()
    {
        // Arrange
        var symbol = "BTCUSDT";
        var cancellationToken = CancellationToken.None;
        var expectedDto = new Ticker24h(symbol, 50000.0, 5.0, 1000.0, 51000.0, 49000.0);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTicker24hQuery>(), cancellationToken))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.GetTicker24HAsync(symbol, cancellationToken);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedDto);
        _mediatorMock.Verify(m => m.Send(It.Is<GetTicker24hQuery>(q => q.Symbol == symbol), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task PlaceMarketBuyAsync_ShouldReturnOkWithResultDto()
    {
        // Arrange
        var symbol = "BTCUSDT";
        var quoteOrderQty = 100.0m;
        var cancellationToken = CancellationToken.None;
        var command = new PlaceMarketBuyCommand(symbol, quoteOrderQty);
        var expectedDto = new BinanceOrderResultDto("12345", 0.002, 100.0, 50000.0);

        _mediatorMock.Setup(m => m.Send(command, cancellationToken))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.PlaceMarketBuyAsync(command, cancellationToken);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedDto);
        _mediatorMock.Verify(m => m.Send(command, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task PlaceMarketSellAsync_ShouldReturnOkWithResultDto()
    {
        // Arrange
        var symbol = "BTCUSDT";
        var quantity = 0.002m;
        var cancellationToken = CancellationToken.None;
        var command = new PlaceMarketSellCommand(symbol, quantity);
        var expectedDto = new BinanceOrderResultDto("67890", 0.002, 100.0, 50000.0);

        _mediatorMock.Setup(m => m.Send(command, cancellationToken))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.PlaceMarketSellAsync(command, cancellationToken);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedDto);
        _mediatorMock.Verify(m => m.Send(command, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task PlaceMarketBuyAsync_WhenBinanceFails_ShouldReturnBadRequest()
    {
        // Arrange
        var symbol = "BTCUSDT";
        var quoteOrderQty = 100.0m;
        var cancellationToken = CancellationToken.None;
        var command = new PlaceMarketBuyCommand(symbol, quoteOrderQty);
        const string errorMessage = "Binance order failed: insufficient balance";

        _mediatorMock.Setup(m => m.Send(command, cancellationToken))
            .ThrowsAsync(new HttpRequestException(errorMessage));

        // Act
        var result = await _controller.PlaceMarketBuyAsync(command, cancellationToken);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be(errorMessage);
        _mediatorMock.Verify(m => m.Send(command, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task PlaceMarketSellAsync_WhenBinanceFails_ShouldReturnBadRequest()
    {
        // Arrange
        var symbol = "BTCUSDT";
        var quantity = 0.002m;
        var cancellationToken = CancellationToken.None;
        var command = new PlaceMarketSellCommand(symbol, quantity);
        const string errorMessage = "Binance order failed: insufficient balance";

        _mediatorMock.Setup(m => m.Send(command, cancellationToken))
            .ThrowsAsync(new HttpRequestException(errorMessage));

        // Act
        var result = await _controller.PlaceMarketSellAsync(command, cancellationToken);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be(errorMessage);
        _mediatorMock.Verify(m => m.Send(command, cancellationToken), Times.Once);
    }
}
