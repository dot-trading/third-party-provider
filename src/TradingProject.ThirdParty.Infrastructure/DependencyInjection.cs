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
    private static readonly Dictionary<string, string> CommonHeader = new Dictionary<string, string>()
    {
        ["User-Agent"] = "TradingProject",
    };
    
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        services.AddOptions<BinanceSettings>()
            .Bind(configuration.GetSection(HttpClientNames.Binance))
            .ValidateOnStart();
        
        services.AddOptions<CoinGeckoSettings>()
            .Bind(configuration.GetSection(HttpClientNames.CoinGecko))
            .ValidateOnStart();
        
        services.AddOptions<AlternativeMeSettings>()
            .Bind(configuration.GetSection(HttpClientNames.AlternativeMe))
            .ValidateOnStart();
        
        services.AddOptions<RssNewsSettings>()
            .Bind(configuration.GetSection(HttpClientNames.AlternativeMe))
            .ValidateOnStart();
        
        services.AddOptions<RedisSettings>()
            .Bind(configuration.GetSection("Redis"))
            .ValidateOnStart();

        services.AddHttpClient(HttpClientNames.Binance, (sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<BinanceSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.WithDefaultRequestHeaders();
            
            client.DefaultRequestHeaders.Add("X-MBX-APIKEY", settings.ApiKey);
        }).AddStandardResilienceHandler();

        services.AddHttpClient(HttpClientNames.AlternativeMe, (sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<AlternativeMeSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.WithDefaultRequestHeaders();
        }).AddStandardResilienceHandler();

        services.AddHttpClient(HttpClientNames.CoinGecko, (sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<CoinGeckoSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.WithDefaultRequestHeaders();
            
            if (!string.IsNullOrEmpty(settings.ApiKey))
                client.DefaultRequestHeaders.Add("x-cg-demo-api-key", settings.ApiKey);
        }).AddStandardResilienceHandler();

        
        services.AddHttpClient(HttpClientNames.RssNews, client =>
        {
            client.WithDefaultRequestHeaders();
        }).AddStandardResilienceHandler();

        services.AddTransient<IBinanceService, BinanceService>();
        services.AddTransient<ISentimentService, AlternativeMeService>();
        services.AddTransient<ICoinGeckoService, CoinGeckoService>();
        services.AddTransient<ITimerService, TimerService>();
        services.AddTransient<INewsService, RssNewsService>();


        services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<RedisSettings>>().Value;
            return StackExchange.Redis.ConnectionMultiplexer.Connect(new StackExchange.Redis.ConfigurationOptions
            {
                EndPoints = { settings.ConnectionString },
                AbortOnConnectFail = false,
                ConnectRetry = 3,
                ConnectTimeout = 5000
            });
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }

    private static HttpClient WithDefaultRequestHeaders(this HttpClient client)
    {
        foreach (var headerItem in CommonHeader)
        {
            client.DefaultRequestHeaders.Add(headerItem.Key, headerItem.Value);
        }

        return client;
    }
}
