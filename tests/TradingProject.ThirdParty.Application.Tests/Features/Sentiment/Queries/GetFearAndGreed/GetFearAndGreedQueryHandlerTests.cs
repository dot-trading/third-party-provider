using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using System.Text.Json.Serialization;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Application.Features.Sentiment.Queries.GetFearAndGreed;
using TradingProject.ThirdParty.Domain.Models.Market;

namespace TradingProject.ThirdParty.Application.Tests.Features.Sentiment.Queries.GetFearAndGreed;

public class GetFearAndGreedQueryHandlerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly Mock<ISentimentService> _sentimentServiceMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly Mock<ILogger<GetFearAndGreedQueryHandler>> _loggerMock = new();
    private readonly GetFearAndGreedQueryHandler _handler;

    public GetFearAndGreedQueryHandlerTests()
    {
        _handler = new GetFearAndGreedQueryHandler(
            _sentimentServiceMock.Object,
            _cacheMock.Object,
            JsonOptions,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnCachedValue_WhenCacheExists()
    {
        var cachedIndex = new FearAndGreedIndex(50, "Neutral", 123456789);
        _cacheMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(cachedIndex, JsonOptions));

        var result = await _handler.Handle(new GetFearAndGreedQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Value.Should().Be(50);
        result.Classification.Should().Be("Neutral");
        result.Timestamp.Should().Be(123456789);
        _sentimentServiceMock.Verify(s => s.GetFearAndGreedIndexAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnCachedValue_WithRealisticFearAndGreedData()
    {
        var cachedIndex = new FearAndGreedIndex(27, "Fear", 1_778_976_000);
        _cacheMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(cachedIndex, JsonOptions));

        var result = await _handler.Handle(new GetFearAndGreedQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Value.Should().Be(27);
        result.Classification.Should().Be("Fear");
        result.Timestamp.Should().Be(1_778_976_000);
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

        result.Should().NotBeNull();
        result!.Value.Should().Be(75);
        result.Classification.Should().Be("Greed");
        result.Timestamp.Should().Be(987654321);
        _sentimentServiceMock.Verify(s => s.GetFearAndGreedIndexAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
