using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
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
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            });

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

        services.AddOptions<GeminiSettings>()
            .Bind(configuration.GetSection(HttpClientNames.Gemini))
            .ValidateOnStart();

        services.AddOptions<GrokSettings>()
            .Bind(configuration.GetSection(HttpClientNames.XAi))
            .ValidateOnStart();

        services.AddOptions<RedisSettings>()
            .Bind(configuration.GetSection("Redis"))
            .ValidateOnStart();

        services.AddOptions<CacheSettings>()
            .Bind(configuration.GetSection("Cache"))
            .ValidateDataAnnotations()
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

        services.AddHttpClient(HttpClientNames.Gemini, (sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<GeminiSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.WithDefaultRequestHeaders();
        }).AddStandardResilienceHandler();

        services.AddHttpClient(HttpClientNames.XAi, client =>
        {
            client.BaseAddress = new Uri("https://api.x.ai");
            client.WithDefaultRequestHeaders();
        }).AddStandardResilienceHandler();

        services.AddTransient<IBinanceService, BinanceService>();
        services.AddTransient<ISentimentService, AlternativeMeService>();
        services.AddTransient<ICoinGeckoService, CoinGeckoService>();
        services.AddTransient<ITimerService, TimerService>();
        services.AddTransient<INewsService, RssNewsService>();
        services.AddTransient<IAgentIAService, AgentIAService>();

        RegisterCacheService(services);

        return services;
    }

    private static void RegisterCacheService(IServiceCollection services)
    {
        // Singleton ICacheService — resolved once, then reused for the app lifetime.
        // The factory reads CacheSettings at resolution time to decide which implementation
        // to create. This keeps the registration code simple and avoids implementing large
        // interfaces like IConnectionMultiplexer for placeholder objects.
        services.AddSingleton<ICacheService>(sp =>
        {
            var cacheSettings = sp.GetRequiredService<IOptions<CacheSettings>>().Value;

            if (!cacheSettings.Enabled)
                return new NullCacheService();

            return cacheSettings.Provider switch
            {
                "Memory" => CreateMemoryCacheService(),
                _ => CreateRedisCacheService(sp),
            };
        });
    }

    private static ICacheService CreateRedisCacheService(IServiceProvider sp)
    {
        var redisSettings = sp.GetRequiredService<IOptions<RedisSettings>>().Value;
        var multiplexer = ConnectionMultiplexer.Connect(
            new ConfigurationOptions
            {
                EndPoints = { redisSettings.ConnectionString },
                AbortOnConnectFail = false,
                ConnectRetry = 3,
                ConnectTimeout = 5000
            });
        return new RedisCacheService(multiplexer);
    }

    private static ICacheService CreateMemoryCacheService()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        return new MemoryCacheService(memoryCache);
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
