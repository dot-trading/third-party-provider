namespace TradingProject.ThirdParty.Application.Abstractions;

public interface ICoinGeckoService
{
    Task<double> GetPriceAsync(string coinId, string vsCurrency = "usd", CancellationToken cancellationToken = default);
    Task<Dictionary<string, double>> GetPricesAsync(IEnumerable<string> coinIds, string vsCurrency = "usd", CancellationToken cancellationToken = default);
}
