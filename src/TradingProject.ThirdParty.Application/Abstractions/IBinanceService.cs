using TradingProject.ThirdParty.Application.Common.Models;

namespace TradingProject.ThirdParty.Application.Abstractions;

public interface IBinanceService
{
    Task<ListBinanceBalanceDto?> GetBalancesAsync(
        CancellationToken cancellationToken = default);
    
    Task<BinancePriceDto?> GetCurrentPriceAsync(
        string symbol,
        CancellationToken cancellationToken = default);
    
    Task<KLine[]> GetKLinesAsync(
        string symbol,
        string interval = "1h",
        int limit = 24,
        CancellationToken cancellationToken = default);
    
    Task<BinanceTicker24HDto?> GetTicker24HAsync(
        string symbol,
        CancellationToken cancellationToken = default);

    Task<BinanceOrderDto> PlaceOrderAsync(
        string query,
        CancellationToken cancellationToken = default);
    
    Task<BinanceOrderDto> PlaceMarketBuyAsync(
        string symbol,
        double quoteOrderQty,
        CancellationToken cancellationToken = default);
    
    Task<BinanceOrderDto> PlaceMarketSellAsync(
        string symbol,
        double quantity,
        CancellationToken cancellationToken = default);
    
    Task<double?> GetMinNotionalAsync(
        string symbol,
        CancellationToken cancellationToken = default);

    Task<BinanceExchangeInfoDto?> GetExchangeInfoAsync(
        string symbol,
        CancellationToken cancellationToken);

    Task<double?> GetLotStepSizeAsync(
        string symbol,
        CancellationToken cancellationToken = default);
}
