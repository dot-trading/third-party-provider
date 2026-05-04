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
        services.Configure<CoinGeckoSettings>(configuration.GetSection("CoinGecko"));
        
        services.AddHttpClient("Binance", (sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<BinanceSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddHttpClient("AlternativeMe", client =>
        {
            client.BaseAddress = new Uri("https://api.alternative.me/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddHttpClient("CoinGecko", client =>
        {
            client.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent", "TradingProject");
        });

        services.AddTransient<IBinanceService, BinanceService>();
        services.AddTransient<ISentimentService, AlternativeMeService>();
        services.AddTransient<ICoinGeckoService, CoinGeckoService>();
        
        services.Configure<RedisSettings>(configuration.GetSection("Redis"));
        services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<RedisSettings>>().Value;
            var configurationOptions = new StackExchange.Redis.ConfigurationOptions
            {
                EndPoints = { settings.ConnectionString },
                AbortOnConnectFail = false,
            };
            return StackExchange.Redis.ConnectionMultiplexer.Connect(configurationOptions);
        });

        return services;
    }
}
