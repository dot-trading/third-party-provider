using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TradingProject.ThirdParty.Api.Controllers.V1;
using TradingProject.ThirdParty.Application.Features.Sentiment.Queries.GetFearAndGreed;
using TradingProject.ThirdParty.Domain.Models.Market;

namespace TradingProject.ThirdParty.Api.Tests.Controllers.V1;

public class MarketDataControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly MarketDataController _controller;

    public MarketDataControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new MarketDataController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetFearAndGreedAsync_ShouldReturnOkWithFearAndGreedIndex()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var expectedIndex = new FearAndGreedIndex(42, "Fear", 1700000000);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetFearAndGreedQuery>(), cancellationToken))
            .ReturnsAsync(expectedIndex);

        // Act
        var result = await _controller.GetFearAndGreedAsync(cancellationToken);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedIndex);
        _mediatorMock.Verify(
            m => m.Send(It.IsAny<GetFearAndGreedQuery>(), cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task GetFearAndGreedAsync_WithExtremeGreed_ShouldReturnCorrectClassification()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var expectedIndex = new FearAndGreedIndex(92, "Extreme Greed", 1700100000);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetFearAndGreedQuery>(), cancellationToken))
            .ReturnsAsync(expectedIndex);

        // Act
        var result = await _controller.GetFearAndGreedAsync(cancellationToken);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var index = okResult.Value.Should().BeOfType<FearAndGreedIndex>().Subject;
        index.Value.Should().Be(92);
        index.Classification.Should().Be("Extreme Greed");
        index.Timestamp.Should().Be(1700100000);
    }

    [Fact]
    public async Task GetFearAndGreedAsync_WithExtremeFear_ShouldReturnCorrectClassification()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var expectedIndex = new FearAndGreedIndex(8, "Extreme Fear", 1700200000);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetFearAndGreedQuery>(), cancellationToken))
            .ReturnsAsync(expectedIndex);

        // Act
        var result = await _controller.GetFearAndGreedAsync(cancellationToken);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var index = okResult.Value.Should().BeOfType<FearAndGreedIndex>().Subject;
        index.Value.Should().Be(8);
        index.Classification.Should().Be("Extreme Fear");
        index.Timestamp.Should().Be(1700200000);
    }

    [Fact]
    public async Task GetFearAndGreedAsync_ShouldSendGetFearAndGreedQuery()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetFearAndGreedQuery>(), cancellationToken))
            .ReturnsAsync(new FearAndGreedIndex(50, "Neutral", 1700300000));

        // Act
        await _controller.GetFearAndGreedAsync(cancellationToken);

        // Assert
        _mediatorMock.Verify(
            m => m.Send(
                It.Is<GetFearAndGreedQuery>(q => q != null),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task GetFearAndGreedAsync_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetFearAndGreedQuery>(), cts.Token))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var act = () => _controller.GetFearAndGreedAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
