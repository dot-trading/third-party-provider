using FluentAssertions;
using TradingProject.ThirdParty.Infrastructure.Services;

namespace TradingProject.ThirdParty.Application.Tests.Infrastructure.Services;

public class NullCacheServiceTests
{
    private readonly NullCacheService _sut = new();

    [Fact]
    public async Task GetAsync_ShouldAlwaysReturnNull()
    {
        var result = await _sut.GetAsync("some-key");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WithAnyKey_ShouldReturnNull()
    {
        var result = await _sut.GetAsync("Binance:Price:BTCUSDT");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WithCancellationToken_ShouldReturnNull()
    {
        using var cts = new CancellationTokenSource();
        var result = await _sut.GetAsync("any-key", cts.Token);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ShouldNotThrow()
    {
        var act = () => _sut.SetAsync("any-key", "any-value", TimeSpan.FromMinutes(5));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SetAsync_ShouldNotAffectSubsequentGetAsync()
    {
        await _sut.SetAsync("some-key", "some-value", TimeSpan.FromMinutes(5));
        var result = await _sut.GetAsync("some-key");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_WithZeroDuration_ShouldNotThrow()
    {
        var act = () => _sut.SetAsync("key", "value", TimeSpan.Zero);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetAsync_AfterMultipleSets_ShouldStillReturnNull()
    {
        await _sut.SetAsync("key1", "value1", TimeSpan.FromMinutes(1));
        await _sut.SetAsync("key2", "value2", TimeSpan.FromMinutes(5));
        await _sut.SetAsync("key3", "value3", TimeSpan.FromHours(1));

        var result1 = await _sut.GetAsync("key1");
        var result2 = await _sut.GetAsync("key2");
        var result3 = await _sut.GetAsync("key3");

        result1.Should().BeNull();
        result2.Should().BeNull();
        result3.Should().BeNull();
    }
}
