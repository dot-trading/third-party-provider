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
