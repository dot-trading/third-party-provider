using System.Text.Json.Serialization;

namespace TradingProject.ThirdParty.Application.Common.Models;

public record CoinPriceDto(string Id, double Price);

// --- Global market data DTOs (/global) ---

public record CoinGeckoGlobalResponseDto(
    [property: JsonPropertyName("data")] CoinGeckoGlobalDataDto Data);

public record CoinGeckoGlobalDataDto(
    [property: JsonPropertyName("market_cap_percentage")] Dictionary<string, double> MarketCapPercentage,
    [property: JsonPropertyName("total_market_cap")] Dictionary<string, double> TotalMarketCap,
    [property: JsonPropertyName("total_volume")] Dictionary<string, double> TotalVolume,
    [property: JsonPropertyName("market_cap_change_percentage_24h_usd")] double MarketCapChangePercentage24hUsd,
    [property: JsonPropertyName("updated_at")] long UpdatedAt);

// --- Trending coins DTOs (/search/trending) ---

public record CoinGeckoTrendingResponseDto(
    [property: JsonPropertyName("coins")] List<CoinGeckoTrendingItemWrapperDto> Coins);

public record CoinGeckoTrendingItemWrapperDto(
    [property: JsonPropertyName("item")] CoinGeckoTrendingItemDto Item);

public record CoinGeckoTrendingItemDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("market_cap_rank")] int MarketCapRank,
    [property: JsonPropertyName("data")] CoinGeckoTrendingDataDto? Data);

public record CoinGeckoTrendingDataDto(
    [property: JsonPropertyName("price_change_percentage_24h")] Dictionary<string, double>? PriceChangePercentage24h);
