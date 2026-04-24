using TradingProject.ThirdParty.Domain.Models;

namespace TradingProject.ThirdParty.Domain.Abstractions;

public interface IBinanceService
{
    Task<Dictionary<string, double>> GetBalancesAsync(CancellationToken cancellationToken = default);
    Task<double> GetCurrentPriceAsync(string symbol, CancellationToken cancellationToken = default);
    Task<List<Kline>> GetKlinesAsync(string symbol, string interval = "1h", int limit = 24, CancellationToken cancellationToken = default);
    Task<Ticker24h?> GetTicker24hAsync(string symbol, CancellationToken cancellationToken = default);
}
