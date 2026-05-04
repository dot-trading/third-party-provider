using Moq;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TradingProject.ThirdParty.Application.Features.Sentiment.Queries.GetFearAndGreed;
using TradingProject.ThirdParty.Domain.Abstractions;
using TradingProject.ThirdParty.Domain.Models.Market;
using FluentAssertions;

namespace TradingProject.ThirdParty.Application.Tests.Features.Sentiment.Queries.GetFearAndGreed;

public class GetFearAndGreedQueryHandlerTests
{
    private readonly Mock<ISentimentService> _sentimentServiceMock;
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly Mock<ILogger<GetFearAndGreedQueryHandler>> _loggerMock;
    private readonly GetFearAndGreedQueryHandler _handler;

    public GetFearAndGreedQueryHandlerTests()
    {
        _sentimentServiceMock = new Mock<ISentimentService>();
        _redisMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();
        _loggerMock = new Mock<ILogger<GetFearAndGreedQueryHandler>>();

        _redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_databaseMock.Object);

        _handler = new GetFearAndGreedQueryHandler(
            _sentimentServiceMock.Object,
            _redisMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnCachedValue_WhenCacheExists()
    {
        // Arrange
        var cachedIndex = new FearAndGreedIndex(50, "Neutral", 123456789);
        var cachedJson = System.Text.Json.JsonSerializer.Serialize(cachedIndex);
        _databaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(cachedJson);

        // Act
        var result = await _handler.Handle(new GetFearAndGreedQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be(50);
        _sentimentServiceMock.Verify(x => x.GetFearAndGreedIndexAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldFetchFromService_WhenCacheDoesNotExist()
    {
        // Arrange
        var freshIndex = new FearAndGreedIndex(75, "Greed", 987654321);
        _databaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        _sentimentServiceMock.Setup(x => x.GetFearAndGreedIndexAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(freshIndex);

        // Act
        var result = await _handler.Handle(new GetFearAndGreedQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be(75);
        _sentimentServiceMock.Verify(x => x.GetFearAndGreedIndexAsync(It.IsAny<CancellationToken>()), Times.Once);
        _databaseMock.Verify(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Once);
    }
}
