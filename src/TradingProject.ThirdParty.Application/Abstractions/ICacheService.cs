namespace TradingProject.ThirdParty.Application.Abstractions;

public interface ICacheService
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task SetAsync(string key, string value, TimeSpan duration, CancellationToken cancellationToken = default);
}
