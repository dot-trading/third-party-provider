using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Constants;
using TradingProject.ThirdParty.Infrastructure.Services;
using TradingProject.ThirdParty.Infrastructure.Settings;

namespace TradingProject.ThirdParty.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(new JsonSerializerOptions(JsonSerializerDefaults.Web));

        services.Configure<BinanceSettings>(configuration.GetSection("Binance"));
        services.Configure<CoinGeckoSettings>(configuration.GetSection("CoinGecko"));

        services.AddHttpClient(HttpClientNames.Binance, (sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<BinanceSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.Add("X-MBX-APIKEY", settings.ApiKey);
        });

        services.AddHttpClient("AlternativeMe", client =>
        {
            client.BaseAddress = new Uri("https://api.alternative.me/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddHttpClient("CoinGecko", (sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<CoinGeckoSettings>>().Value;
            client.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent", "TradingProject");
            if (!string.IsNullOrEmpty(settings.ApiKey))
                client.DefaultRequestHeaders.Add("x-cg-demo-api-key", settings.ApiKey);
        });

        services.AddTransient<IBinanceService, BinanceService>();
        services.AddTransient<ISentimentService, AlternativeMeService>();
        services.AddTransient<ICoinGeckoService, CoinGeckoService>();
        services.AddTransient<ITimerService, TimerService>();

        services.Configure<RedisSettings>(configuration.GetSection("Redis"));
        services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<RedisSettings>>().Value;
            return StackExchange.Redis.ConnectionMultiplexer.Connect(new StackExchange.Redis.ConfigurationOptions
            {
                EndPoints = { settings.ConnectionString },
                AbortOnConnectFail = false,
            });
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }
}
