namespace TradingProject.ThirdParty.Domain.Abstractions;

public interface IBinanceService
{
    Task<Dictionary<string, double>> GetBalancesAsync(CancellationToken cancellationToken = default);
    Task<double> GetCurrentPriceAsync(string symbol, CancellationToken cancellationToken = default);
}
