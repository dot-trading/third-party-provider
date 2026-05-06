namespace TradingProject.ThirdParty.Application.Common.Models;

public record BinanceBalanceDto(string Asset, double Free, double Locked);

public record ListBinanceBalanceDto(
    BinanceBalanceDto[] Balances,
    int MakerCommission,
    int TakerCommission,
    string[] Permissions);

public record KLine(
    long OpenTime,
    double Open,
    double High,
    double Low,
    double Close,
    double Volume);

public record BinancePriceDto(double Price);

public record BinanceTicker24HDto(
    string Symbol,
    double LastPrice,
    double PriceChangePercent,
    double QuoteVolume,
    double HighPrice,
    double LowPrice);

/// <summary>
/// Response from GET /api/v3/order — query a single order.
/// Numeric string fields (price, qty, …) are deserialized automatically via
/// <see cref="System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString"/>.
/// </summary>
/// <param name="Symbol">Trading pair, e.g. "BTCUSDT".</param>
/// <param name="OrderId">Binance-assigned order ID (numeric in JSON).</param>
/// <param name="OrderListId">OCO group ID; −1 when the order is not part of an OCO.</param>
/// <param name="ClientOrderId">Client-supplied order ID.</param>
/// <param name="Price">Limit price; "0.00000000" for MARKET orders.</param>
/// <param name="OrigQty">Original order quantity in base asset.</param>
/// <param name="ExecutedQty">Quantity already filled in base asset.</param>
/// <param name="CummulativeQuoteQty">Cumulative quote-asset spent/received (Binance intentional double-m spelling).</param>
/// <param name="CumulativeQuoteQty">Correctly-spelled alias for <paramref name="CummulativeQuoteQty"/>, added in later API versions.</param>
/// <param name="Status">
/// Order lifecycle state.
/// Possible values: NEW, PARTIALLY_FILLED, FILLED, CANCELED,
/// PENDING_CANCEL, REJECTED, EXPIRED, EXPIRED_IN_MATCH.
/// </param>
/// <param name="TimeInForce">
/// Time-in-force policy.
/// Possible values: GTC (Good Till Cancel), IOC (Immediate Or Cancel), FOK (Fill Or Kill).
/// </param>
/// <param name="Type">
/// Order type.
/// Possible values: LIMIT, MARKET, STOP_LOSS, STOP_LOSS_LIMIT,
/// TAKE_PROFIT, TAKE_PROFIT_LIMIT, LIMIT_MAKER.
/// </param>
/// <param name="Side">Trade direction. Possible values: BUY, SELL.</param>
/// <param name="StopPrice">Trigger price for stop orders; 0 when not applicable.</param>
/// <param name="IcebergQty">Visible iceberg quantity; 0 when not an iceberg order.</param>
/// <param name="Time">Order creation time (Unix epoch, milliseconds).</param>
/// <param name="UpdateTime">Last status-change time (Unix epoch, milliseconds).</param>
/// <param name="WorkingTime">Time the order entered the order book (Unix epoch, milliseconds).</param>
/// <param name="IsWorking">Whether the order is currently on the order book.</param>
/// <param name="OrigQuoteOrderQty">Original quote-asset quantity (set when using quoteOrderQty); 0 otherwise.</param>
/// <param name="SelfTradePreventionMode">
/// Self-trade prevention rule.
/// Possible values: EXPIRE_TAKER, EXPIRE_MAKER, EXPIRE_BOTH, NONE.
/// </param>
public record BinanceOrderDto(
    string Symbol,
    long OrderId,
    long OrderListId,
    string ClientOrderId,
    double Price,
    double OrigQty,
    double ExecutedQty,
    double CummulativeQuoteQty,
    double CumulativeQuoteQty,
    string Status,
    string TimeInForce,
    string Type,
    string Side,
    double StopPrice,
    double IcebergQty,
    long Time,
    long UpdateTime,
    long WorkingTime,
    bool IsWorking,
    double OrigQuoteOrderQty,
    string SelfTradePreventionMode);

public class BinanceExchangeInfoDto
{
    public BinanceSymbolDto[] Symbols { get; set; } = [];

    public BinanceFilterDto? LotStepSize() => Symbols.FirstOrDefault()?.Filters
        .FirstOrDefault(f => f.FilterType is "LOT_SIZE");
    
    public BinanceFilterDto? MinNotional() => Symbols.FirstOrDefault()?.Filters
        .FirstOrDefault(f => f.FilterType is "NOTIONAL" or "MIN_NOTIONAL");
};

public record BinanceSymbolDto(List<BinanceFilterDto> Filters);

public record BinanceFilterDto(string FilterType, double? StepSize, double? MinNotional, double? Notional)
{
    public double NotionalValue => MinNotional ?? Notional ?? 0;
    public double StepSizeValue => StepSize ?? 0;

    public static implicit operator double?(BinanceFilterDto? dto) => dto?.FilterType switch
    {
        "LOT_SIZE" => dto.StepSizeValue,
        "NOTIONAL" or "MIN_NOTIONAL" => dto.NotionalValue,
        _ => 0
    };
};