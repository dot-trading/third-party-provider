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
}
