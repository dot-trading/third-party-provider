using FluentAssertions;
using TradingProject.ThirdParty.Domain.Constants;

namespace TradingProject.ThirdParty.Application.Tests.Domain.Constants;

public class CacheKeysTests
{
    // ========================================================================
    // Binance
    // ========================================================================
    [Fact]
    public void Binance_ExchangeInfo_ShouldReturnFormattedKey()
    {
        var key = CacheKeys.Binance.ExchangeInfo("BTCUSDT");
        key.Should().Be("ThirdParty:Binance:ExchangeInfo:BTCUSDT");
    }

    [Fact]
    public void Binance_Price_ShouldReturnFormattedKey()
    {
        var key = CacheKeys.Binance.Price("BTCUSDT");
        key.Should().Be("ThirdParty:Binance:Price:BTCUSDT");
    }

    [Fact]
    public void Binance_Klines_ShouldReturnFormattedKey()
    {
        var key = CacheKeys.Binance.Klines("BTCUSDT", "1h", 24);
        key.Should().Be("ThirdParty:Binance:Klines:BTCUSDT:1h:24");
    }

    [Fact]
    public void Binance_Ticker24H_ShouldReturnFormattedKey()
    {
        var key = CacheKeys.Binance.Ticker24H("BTCUSDT");
        key.Should().Be("ThirdParty:Binance:Ticker24h:BTCUSDT");
    }

    [Fact]
    public void Binance_MinNotional_ShouldReturnFormattedKey()
    {
        var key = CacheKeys.Binance.MinNotional("BTCUSDT");
        key.Should().Be("ThirdParty:Binance:MinNotional:BTCUSDT");
    }

    [Fact]
    public void Binance_StaticKeys_ShouldHaveCorrectValues()
    {
        CacheKeys.Binance.BalancesKey.Should().Be("ThirdParty:Binance:Balances");
    }

    [Fact]
    public void Binance_Durations_ShouldBePositive()
    {
        CacheKeys.Binance.ExchangeInfoDuration.Should().BePositive();
        CacheKeys.Binance.PriceDuration.Should().BePositive();
        CacheKeys.Binance.KlinesDuration.Should().BePositive();
        CacheKeys.Binance.Ticker24HDuration.Should().BePositive();
        CacheKeys.Binance.BalancesDuration.Should().BePositive();
        CacheKeys.Binance.MinNotionalDuration.Should().BePositive();
    }

    // ========================================================================
    // CoinGecko
    // ========================================================================
    [Fact]
    public void CoinGecko_Price_ShouldReturnFormattedKey()
    {
        var key = CacheKeys.CoinGecko.Price("bitcoin", "usd");
        key.Should().Be("ThirdParty:CoinGecko:Price:bitcoin:usd");
    }

    [Fact]
    public void CoinGecko_Price_ShouldNormalizeCase()
    {
        var key = CacheKeys.CoinGecko.Price("BITCOIN", "USD");
        key.Should().Be("ThirdParty:CoinGecko:Price:bitcoin:usd");
    }

    [Fact]
    public void CoinGecko_Price_WithDefaultCurrency_ShouldUseUsd()
    {
        var key = CacheKeys.CoinGecko.Price("ethereum");
        key.Should().Be("ThirdParty:CoinGecko:Price:ethereum:usd");
    }

    [Fact]
    public void CoinGecko_StaticKeys_ShouldHaveCorrectValues()
    {
        CacheKeys.CoinGecko.GlobalKey.Should().Be("ThirdParty:CoinGecko:Global");
        CacheKeys.CoinGecko.TrendingKey.Should().Be("ThirdParty:CoinGecko:Trending");
    }

    [Fact]
    public void CoinGecko_Durations_ShouldBePositive()
    {
        CacheKeys.CoinGecko.PriceDuration.Should().BePositive();
        CacheKeys.CoinGecko.GlobalDuration.Should().BePositive();
        CacheKeys.CoinGecko.TrendingDuration.Should().BePositive();
    }

    // ========================================================================
    // News
    // ========================================================================
    [Fact]
    public void News_Key_WithCurrencies_ShouldReturnFormattedKey()
    {
        var key = CacheKeys.News.Key(["BTC", "ETH"], 10);
        key.Should().Be("ThirdParty:News:BTC,ETH:10");
    }

    [Fact]
    public void News_Key_ShouldSortCurrenciesAlphabetically()
    {
        var key = CacheKeys.News.Key(["ETH", "BTC", "SOL"], 5);
        key.Should().Be("ThirdParty:News:BTC,ETH,SOL:5");
    }

    [Fact]
    public void News_Key_ShouldNormalizeCurrenciesToUpper()
    {
        var key = CacheKeys.News.Key(["btc", "eth"], 10);
        key.Should().Be("ThirdParty:News:BTC,ETH:10");
    }

    [Fact]
    public void News_Key_WithEmptyCurrencies_ShouldReturnEmptyList()
    {
        var key = CacheKeys.News.Key([], 10);
        key.Should().Be("ThirdParty:News::10");
    }

    [Fact]
    public void News_Duration_ShouldBePositive()
    {
        CacheKeys.News.Duration.Should().BePositive();
    }

    // ========================================================================
    // Sentiment
    // ========================================================================
    [Fact]
    public void Sentiment_StaticKeys_ShouldHaveCorrectValues()
    {
        CacheKeys.Sentiment.FearAndGreedKey.Should().Be("ThirdParty:Sentiment:FearAndGreed");
    }

    [Fact]
    public void Sentiment_Durations_ShouldBePositive()
    {
        CacheKeys.Sentiment.FearAndGreedDuration.Should().BePositive();
    }
}
