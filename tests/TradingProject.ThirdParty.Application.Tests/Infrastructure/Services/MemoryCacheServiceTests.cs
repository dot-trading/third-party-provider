using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using TradingProject.ThirdParty.Infrastructure.Services;

namespace TradingProject.ThirdParty.Application.Tests.Infrastructure.Services;

public class MemoryCacheServiceTests : IDisposable
{
    private readonly IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());
    private readonly MemoryCacheService _sut;

    public MemoryCacheServiceTests()
    {
        _sut = new MemoryCacheService(_memoryCache);
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
    }

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ShouldReturnNull()
    {
        var result = await _sut.GetAsync("non-existent-key");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAndGetAsync_ShouldReturnStoredValue()
    {
        await _sut.SetAsync("key", "value", TimeSpan.FromMinutes(5));

        var result = await _sut.GetAsync("key");

        result.Should().Be("value");
    }

    [Fact]
    public async Task GetAsync_AfterExpiration_ShouldReturnNull()
    {
        await _sut.SetAsync("quick-key", "quick-value", TimeSpan.FromMilliseconds(10));

        // Wait for the entry to expire
        await Task.Delay(50);

        var result = await _sut.GetAsync("quick-key");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_WithDifferentKeys_ShouldNotOverwrite()
    {
        await _sut.SetAsync("key1", "value1", TimeSpan.FromMinutes(5));
        await _sut.SetAsync("key2", "value2", TimeSpan.FromMinutes(5));

        var result1 = await _sut.GetAsync("key1");
        var result2 = await _sut.GetAsync("key2");

        result1.Should().Be("value1");
        result2.Should().Be("value2");
    }

    [Fact]
    public async Task SetAsync_OverwritesExistingKey()
    {
        await _sut.SetAsync("key", "original", TimeSpan.FromMinutes(5));
        await _sut.SetAsync("key", "updated", TimeSpan.FromMinutes(5));

        var result = await _sut.GetAsync("key");

        result.Should().Be("updated");
    }

    [Fact]
    public async Task GetAsync_ShouldReturnStringValue_ForNumericStrings()
    {
        await _sut.SetAsync("price-key", "12345.67", TimeSpan.FromMinutes(1));

        var result = await _sut.GetAsync("price-key");

        result.Should().Be("12345.67");
    }

    [Fact]
    public async Task SetAsync_WithDifferentDurations_ShouldAllWork()
    {
        await _sut.SetAsync("short", "v1", TimeSpan.FromSeconds(1));
        await _sut.SetAsync("medium", "v2", TimeSpan.FromMinutes(5));
        await _sut.SetAsync("long", "v3", TimeSpan.FromHours(24));

        var shortVal = await _sut.GetAsync("short");
        var mediumVal = await _sut.GetAsync("medium");
        var longVal = await _sut.GetAsync("long");

        shortVal.Should().Be("v1");
        mediumVal.Should().Be("v2");
        longVal.Should().Be("v3");
    }
}
