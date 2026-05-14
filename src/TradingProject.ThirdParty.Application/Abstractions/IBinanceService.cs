using TradingProject.ThirdParty.Application.Common.Models;

namespace TradingProject.ThirdParty.Application.Abstractions;

public interface IBinanceService
{
    Task<ListBinanceBalanceDto?> GetBalancesAsync(
        CancellationToken cancellationToken = default);
    
    Task<BinancePriceDto?> GetCurrentPriceAsync(
        string symbol,
        CancellationToken cancellationToken = default);
    
    Task<KLineDto[]> GetKLinesAsync(
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
        decimal quoteOrderQty,
        CancellationToken cancellationToken = default);
    
    Task<BinanceOrderDto> PlaceMarketSellAsync(
        string symbol,
        decimal quantity,
        CancellationToken cancellationToken = default);
    
    Task<BinanceFilterDto?> GetMinNotionalAsync(
        string symbol,
        CancellationToken cancellationToken = default);

    Task<BinanceExchangeInfoDto?> GetExchangeInfoAsync(
        string symbol,
        CancellationToken cancellationToken);

    Task<BinanceFilterDto?> GetLotStepSizeAsync(
        string symbol,
        CancellationToken cancellationToken = default);
}
