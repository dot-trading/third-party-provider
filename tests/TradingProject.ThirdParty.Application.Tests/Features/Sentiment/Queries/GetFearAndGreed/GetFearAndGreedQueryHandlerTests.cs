using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Application.Features.Sentiment.Queries.GetFearAndGreed;
using TradingProject.ThirdParty.Domain.Models.Market;

namespace TradingProject.ThirdParty.Application.Tests.Features.Sentiment.Queries.GetFearAndGreed;

public class GetFearAndGreedQueryHandlerTests
{
    private readonly Mock<ISentimentService> _sentimentServiceMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly Mock<ILogger<GetFearAndGreedQueryHandler>> _loggerMock = new();
    private readonly GetFearAndGreedQueryHandler _handler;

    public GetFearAndGreedQueryHandlerTests()
    {
        _handler = new GetFearAndGreedQueryHandler(
            _sentimentServiceMock.Object,
            _cacheMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnCachedValue_WhenCacheExists()
    {
        var cachedIndex = new FearAndGreedIndex(50, "Neutral", 123456789);
        _cacheMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(cachedIndex));

        var result = await _handler.Handle(new GetFearAndGreedQuery(), CancellationToken.None);

        result.Value.Should().Be(50);
        result.Classification.Should().Be("Neutral");
        _sentimentServiceMock.Verify(s => s.GetFearAndGreedIndexAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldFetchFromService_WhenCacheIsEmpty()
    {
        var freshIndex = new FearAndGreedIndex(75, "Greed", 987654321);
        _cacheMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        _sentimentServiceMock
            .Setup(s => s.GetFearAndGreedIndexAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(freshIndex);

        var result = await _handler.Handle(new GetFearAndGreedQuery(), CancellationToken.None);

        result.Value.Should().Be(75);
        _sentimentServiceMock.Verify(s => s.GetFearAndGreedIndexAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
