using TradingProject.ThirdParty.Application.Common.Models;

namespace TradingProject.ThirdParty.Application.Abstractions;

public interface ICoinGeckoService
{
    Task<CoinPriceDto> GetPriceAsync(string coinId, string vsCurrency = "usd", CancellationToken cancellationToken = default);
    Task<CoinPriceDto[]> GetPricesAsync(IEnumerable<string> coinIds, string vsCurrency = "usd", CancellationToken cancellationToken = default);
}
