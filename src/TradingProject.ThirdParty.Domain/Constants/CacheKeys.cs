namespace TradingProject.ThirdParty.Domain.Constants;

/// <summary>
/// Centralized cache key builders and duration constants for third-party API responses.
///
/// Usage:
///   var key = CacheKeys.Binance.Price("BTCUSDT");
///   await cache.SetAsync(key, value, CacheKeys.Binance.PriceDuration);
/// </summary>
public static class CacheKeys
{
    
    public const string ServiceNamePrefix = "ThridParty:";
    
    // ========================================================================
    // Binance
    // ========================================================================
    public static class Binance
    {
        // -- Key prefixes / constants ---------------------------------------
        public const string ExchangeInfoPrefix = $"{ServiceNamePrefix}Binance:ExchangeInfo:";
        public const string PricePrefix        = $"{ServiceNamePrefix}Binance:Price:";
        public const string KlinesPrefix       = $"{ServiceNamePrefix}Binance:Klines:";
        public const string Ticker24HPrefix    = $"{ServiceNamePrefix}Binance:Ticker24h:";
        public const string BalancesKey        = $"{ServiceNamePrefix}Binance:Balances";
        public const string MinNotionalPrefix  = $"{ServiceNamePrefix}Binance:MinNotional:";

        // -- Durations ------------------------------------------------------
        /// <summary>Exchange info (filters, lot sizes) rarely changes — 3 min is safe.</summary>
        public static readonly TimeSpan ExchangeInfoDuration = TimeSpan.FromSeconds(180);
        /// <summary>Ticker price: 30 s keeps it fresh enough for near-real-time displays.</summary>
        public static readonly TimeSpan PriceDuration        = TimeSpan.FromSeconds(30);
        /// <summary>Klines (historical candles): 5 min is fine for 1h+ intervals.</summary>
        public static readonly TimeSpan KlinesDuration       = TimeSpan.FromMinutes(5);
        /// <summary>24 h ticker statistics: 1 min refresh is reasonable.</summary>
        public static readonly TimeSpan Ticker24HDuration    = TimeSpan.FromMinutes(1);
        /// <summary>Balances: 1 min is fine; balances only change on active trading.</summary>
        public static readonly TimeSpan BalancesDuration     = TimeSpan.FromMinutes(1);
        /// <summary>Min notional / LOT_SIZE: almost never changes — 24 h cache.</summary>
        public static readonly TimeSpan MinNotionalDuration  = TimeSpan.FromHours(24);

        // -- Key builders ---------------------------------------------------
        public static string ExchangeInfo(string symbol)         => $"{ExchangeInfoPrefix}{symbol}";
        public static string Price(string symbol)                => $"{PricePrefix}{symbol}";
        public static string Klines(string symbol, string interval, int limit)
            => $"{KlinesPrefix}{symbol}:{interval}:{limit}";
        public static string Ticker24H(string symbol)            => $"{Ticker24HPrefix}{symbol}";
        public static string MinNotional(string symbol)          => $"{MinNotionalPrefix}{symbol}";
    }

    // ========================================================================
    // CoinGecko
    // ========================================================================
    public static class CoinGecko
    {
        // -- Key constants --------------------------------------------------
        public const string PricePrefix   = $"{ServiceNamePrefix}CoinGecko:Price:";
        public const string GlobalKey     = $"{ServiceNamePrefix}CoinGecko:Global";
        public const string TrendingKey   = $"{ServiceNamePrefix}CoinGecko:Trending";

        // -- Durations ------------------------------------------------------
        /// <summary>CoinGecko free tier rate limits are tight; 5 min is a good balance.</summary>
        public static readonly TimeSpan PriceDuration    = TimeSpan.FromMinutes(5);
        /// <summary>Global market data (dominance, total cap) updates every few minutes.</summary>
        public static readonly TimeSpan GlobalDuration   = TimeSpan.FromMinutes(5);
        /// <summary>Trending coin list changes slowly — 1 h is sufficient.</summary>
        public static readonly TimeSpan TrendingDuration = TimeSpan.FromHours(1);

        // -- Key builders ---------------------------------------------------
        public static string Price(string coinId, string vsCurrency = "usd")
            => $"{PricePrefix}{coinId.ToLowerInvariant()}:{vsCurrency.ToLowerInvariant()}";
    }

    // ========================================================================
    // News (RSS)
    // ========================================================================
    public static class News
    {
        /// <summary>News articles don't appear every second — 15 min is fine.</summary>
        public static readonly TimeSpan Duration = TimeSpan.FromMinutes(15);

        public static string Key(IEnumerable<string> currencies, int limit)
        {
            var sorted = currencies
                .Select(c => c.ToUpperInvariant())
                .OrderBy(c => c)
                .ToArray();
            return $"{ServiceNamePrefix}News:{string.Join(",", sorted)}:{limit}";
        }
    }

    // ========================================================================
    // Sentiment
    // ========================================================================
    public static class Sentiment
    {
        /// <summary>Fear & Greed index is updated once a day by alternative.me.</summary>
        public const string FearAndGreedKey = $"{ServiceNamePrefix}Sentiment:FearAndGreed";
        /// <summary>1 h is more than enough for a daily-updated value.</summary>
        public static readonly TimeSpan FearAndGreedDuration = TimeSpan.FromHours(1);
    }
}
