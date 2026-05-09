using System.Text.Json.Serialization;

namespace TradingProject.ThirdParty.Client.Models.Responses;

/// <summary>
/// Request body for <c>POST /api/v1/Binance/order/buy</c> (V1+).
/// </summary>
/// <param name="Symbol">Trading pair symbol (e.g. "BTCUSDT").</param>
/// <param name="QuoteOrderQty">Amount to spend in quote asset (e.g. USDT).</param>
public record PlaceMarketBuyRequest(
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("quoteOrderQty")] double QuoteOrderQty
);

/// <summary>
/// Request body for <c>POST /api/v1/Binance/order/sell</c> (V1+).
/// </summary>
/// <param name="Symbol">Trading pair symbol (e.g. "BTCUSDT").</param>
/// <param name="Quantity">Amount of the base asset to sell (e.g. BTC).</param>
public record PlaceMarketSellRequest(
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("quantity")] double Quantity
);

/// <summary>
/// Response returned by <c>POST /api/v1/Binance/order/buy</c> and <c>POST /api/v1/Binance/order/sell</c> (V1+).
/// Contains the result of a market buy order execution.
/// </summary>
/// <param name="OrderId">Binance-assigned order ID.</param>
/// <param name="ExecutedQty">Quantity of the base asset that was filled.</param>
/// <param name="CumulativeQuoteQty">Cumulative amount of quote asset spent.</param>
/// <param name="Price">Average fill price.</param>
public record BinanceOrderResultResponse(
    [property: JsonPropertyName("orderId")] string OrderId,
    [property: JsonPropertyName("executedQty")] double ExecutedQty,
    [property: JsonPropertyName("cumulativeQuoteQty")] double CumulativeQuoteQty,
    [property: JsonPropertyName("price")] double Price
);
