using TradingProject.ThirdParty.Application.Abstractions;

namespace TradingProject.ThirdParty.Infrastructure.Services;

public class TimerService : ITimerService
{
    public long BinanceNowDateTimeOffset() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}