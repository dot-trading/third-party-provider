using System.Text.Json.Serialization;

namespace TradingProject.ThirdParty.Client.Models.Responses;

/// <summary>
/// Represents a single asset balance returned from the V1 Binance balances endpoint.
/// </summary>
/// <param name="Asset">The asset symbol (e.g. "BTC", "ETH").</param>
/// <param name="Free">Available (free) balance.</param>
/// <param name="Locked">Locked/held balance.</param>
public record BinanceBalanceDto(
    [property: JsonPropertyName("asset")] string Asset,
    [property: JsonPropertyName("free")] double Free,
    [property: JsonPropertyName("locked")] double Locked
);

/// <summary>
/// Full response returned by <c>GET /api/v{version}/Binance/balances</c> (V1+).
/// </summary>
/// <param name="Balances">Array of individual asset balances.</param>
/// <param name="MakerCommission">Maker commission rate (bps).</param>
/// <param name="TakerCommission">Taker commission rate (bps).</param>
/// <param name="Permissions">Account permissions (e.g. "SPOT", "MARGIN").</param>
public record ListBinanceBalanceResponse(
    [property: JsonPropertyName("balances")] BinanceBalanceDto[] Balances,
    [property: JsonPropertyName("makerCommission")] int MakerCommission,
    [property: JsonPropertyName("takerCommission")] int TakerCommission,
    [property: JsonPropertyName("permissions")] string[] Permissions
);

/// <summary>
/// Response returned by <c>GET /api/v{version}/Binance/price/{symbol}</c> (V1+).
/// </summary>
/// <param name="Price">Current market price of the trading pair.</param>
public record BinancePriceResponse(
    [property: JsonPropertyName("price")] double Price
);

/// <summary>
/// Response returned by <c>GET /api/v{version}/Binance/notional/{symbol}</c> (V1+).
/// Contains the MIN_NOTIONAL filter information for a trading pair.
/// </summary>
/// <param name="FilterType">The type of the filter (e.g. "MIN_NOTIONAL").</param>
/// <param name="StepSize">Step size for LOT_SIZE filter (null for MIN_NOTIONAL).</param>
/// <param name="MinNotional">The minimum notional value (older field name).</param>
/// <param name="Notional">The minimum notional value (newer field name).</param>
public record BinanceNotionalResponse(
    [property: JsonPropertyName("filterType")] string FilterType,
    [property: JsonPropertyName("stepSize")] double? StepSize,
    [property: JsonPropertyName("minNotional")] double? MinNotional,
    [property: JsonPropertyName("notional")] double? Notional
);

/// <summary>
/// Represents a single K-Line (candlestick) data point returned from the V1 Binance klines endpoint.
/// Corresponds to <c>GET /api/v{version}/Binance/klines/{symbol}</c>.
/// </summary>
/// <param name="OpenTime">Opening time of the candlestick (Unix epoch in milliseconds).</param>
/// <param name="Open">Opening price.</param>
/// <param name="High">Highest price during the interval.</param>
/// <param name="Low">Lowest price during the interval.</param>
/// <param name="Close">Closing price.</param>
/// <param name="Volume">Trading volume in base asset.</param>
public record BinanceKLineResponse(
    [property: JsonPropertyName("openTime")] long OpenTime,
    [property: JsonPropertyName("open")] double Open,
    [property: JsonPropertyName("high")] double High,
    [property: JsonPropertyName("low")] double Low,
    [property: JsonPropertyName("close")] double Close,
    [property: JsonPropertyName("volume")] double Volume
);

/// <summary>
/// Response returned by <c>GET /api/v{version}/Binance/ticker/{symbol}</c> (V1+).
/// Contains 24-hour ticker statistics for a trading pair.
/// </summary>
/// <param name="Symbol">Trading pair symbol (e.g. "BTCUSDT").</param>
/// <param name="Price">Last traded price.</param>
/// <param name="PriceChangePercent">Price change percentage over the last 24 hours.</param>
/// <param name="QuoteVolume">Total trading volume in quote asset.</param>
/// <param name="HighPrice">Highest price in the last 24 hours.</param>
/// <param name="LowPrice">Lowest price in the last 24 hours.</param>
public record BinanceTicker24HResponse(
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("price")] double Price,
    [property: JsonPropertyName("priceChangePercent")] double PriceChangePercent,
    [property: JsonPropertyName("quoteVolume")] double QuoteVolume,
    [property: JsonPropertyName("highPrice")] double HighPrice,
    [property: JsonPropertyName("lowPrice")] double LowPrice
);
