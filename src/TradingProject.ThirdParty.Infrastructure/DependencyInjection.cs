using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TradingProject.ThirdParty.Domain.Abstractions;
using TradingProject.ThirdParty.Infrastructure.Services;
using TradingProject.ThirdParty.Infrastructure.Settings;

namespace TradingProject.ThirdParty.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BinanceSettings>(configuration.GetSection("Binance"));
        
        services.AddHttpClient("Binance", (sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<BinanceSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddTransient<IBinanceService, BinanceService>();

        return services;
    }
}
