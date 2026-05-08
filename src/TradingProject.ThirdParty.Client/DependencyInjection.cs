using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TradingProject.ThirdParty.Client.Configuration;
using TradingProject.ThirdParty.Client.Services;

namespace TradingProject.ThirdParty.Client;

/// <summary>
/// Extension methods for registering the Third-Party API client services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the <see cref="IThirdPartyApiClient"/> with its <see cref="HttpClient"/> and configuration.
    /// </summary>
    /// <param name="services">The dependency injection container.</param>
    /// <param name="configuration">The application configuration (used to bind <c>"ThirdPartyApi"</c> section).</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="OptionsValidationException">Thrown at start-up if configuration is missing or invalid.</exception>
    public static IServiceCollection AddThirdPartyApiClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind & validate options
        services.AddOptions<ThirdPartyApiClientOptions>()
            .Bind(configuration.GetSection(ThirdPartyApiClientOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register typed HttpClient
        services.AddHttpClient<IThirdPartyApiClient, ThirdPartyApiClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ThirdPartyApiClientOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrEmpty(options.ApiKey))
            {
                client.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
            }
        });

        return services;
    }
}
