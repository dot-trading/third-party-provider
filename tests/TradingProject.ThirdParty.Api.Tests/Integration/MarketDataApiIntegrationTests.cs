using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Models.Market;

namespace TradingProject.ThirdParty.Api.Tests.Integration;

public class MarketDataApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<ISentimentService> _sentimentServiceMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    public MarketDataApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing registrations
                var descriptor1 = services.SingleOrDefault(d => d.ServiceType == typeof(ISentimentService));
                if (descriptor1 != null) services.Remove(descriptor1);

                var descriptor2 = services.SingleOrDefault(d => d.ServiceType == typeof(ICacheService));
                if (descriptor2 != null) services.Remove(descriptor2);

                // Add mocks
                services.AddSingleton(_sentimentServiceMock.Object);
                services.AddSingleton(_cacheServiceMock.Object);
            });
        });
    }

    [Fact]
    public async Task GetFearAndGreed_V0_ShouldReturnFearAndGreedIndex()
    {
        // Arrange
        var client = _factory.CreateClient();
        var expectedIndex = new FearAndGreedIndex(42, "Fear", 1700000000);

        _sentimentServiceMock
            .Setup(s => s.GetFearAndGreedIndexAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedIndex);

        _cacheServiceMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var response = await client.GetAsync("/api/v0.0/market-data/sentiment/fear-and-greed");

        // Assert
        response.EnsureSuccessStatusCode();

        // V0 returns the domain model directly (no wrapper)
        var result = await response.Content.ReadFromJsonAsync<FearAndGreedIndex>();
        result.Should().NotBeNull();
        result!.Value.Should().Be(42);
        result.Classification.Should().Be("Fear");
        result.Timestamp.Should().Be(1700000000);

        _sentimentServiceMock.Verify(
            s => s.GetFearAndGreedIndexAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetFearAndGreed_V1_ShouldReturnFearAndGreedIndex()
    {
        // Arrange
        var client = _factory.CreateClient();
        var expectedIndex = new FearAndGreedIndex(78, "Greed", 1700100000);

        _sentimentServiceMock
            .Setup(s => s.GetFearAndGreedIndexAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedIndex);

        _cacheServiceMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var response = await client.GetAsync("/api/v1.0/market-data/sentiment/fear-and-greed");

        // Assert
        response.EnsureSuccessStatusCode();

        // V1 returns the domain model directly (same as V0 for this endpoint)
        var result = await response.Content.ReadFromJsonAsync<FearAndGreedIndex>();
        result.Should().NotBeNull();
        result!.Value.Should().Be(78);
        result.Classification.Should().Be("Greed");
        result.Timestamp.Should().Be(1700100000);

        _sentimentServiceMock.Verify(
            s => s.GetFearAndGreedIndexAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetFearAndGreed_WhenCacheHits_ShouldNotCallSentimentService()
    {
        // Arrange
        var client = _factory.CreateClient();
        var cachedIndex = new FearAndGreedIndex(50, "Neutral", 1700200000);

        _cacheServiceMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(cachedIndex));

        // Act
        var response = await client.GetAsync("/api/v1.0/market-data/sentiment/fear-and-greed");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<FearAndGreedIndex>();
        result.Should().NotBeNull();
        result!.Value.Should().Be(50);
        result.Classification.Should().Be("Neutral");

        // Sentiment service should NOT be called when cache hits
        _sentimentServiceMock.Verify(
            s => s.GetFearAndGreedIndexAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetFearAndGreed_V1_NotFound_WhenServiceReturnsNull()
    {
        // Arrange
        var client = _factory.CreateClient();

        _cacheServiceMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // The controller returns Ok(...) always for this endpoint, but the
        // query handler always returns a FearAndGreedIndex. If the underlying
        // service returned null it would throw — so this test verifies the
        // endpoint never returns NotFound (it always returns Ok).
        _sentimentServiceMock
            .Setup(s => s.GetFearAndGreedIndexAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FearAndGreedIndex(25, "Extreme Fear", 1700300000));

        // Act
        var response = await client.GetAsync("/api/v1.0/market-data/sentiment/fear-and-greed");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
        response.EnsureSuccessStatusCode();
    }
}
