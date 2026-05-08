using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TradingProject.ThirdParty.Client.Services;

namespace TradingProject.ThirdParty.Client.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddThirdPartyApiClient_WithValidConfig_ShouldResolveIThirdPartyApiClient()
    {
        // Arrange
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThirdPartyApi:BaseUrl"] = "http://localhost:5114",
                ["ThirdPartyApi:TimeoutSeconds"] = "15"
            })
            .Build();

        // Act
        services.AddThirdPartyApiClient(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var client = provider.GetRequiredService<IThirdPartyApiClient>();
        client.Should().NotBeNull();
        client.Should().BeAssignableTo<ThirdPartyApiClient>();
    }

    [Fact]
    public void AddThirdPartyApiClient_WithApiKey_ShouldConfigureClientSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        const string apiKey = "test-api-key-12345";

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThirdPartyApi:BaseUrl"] = "http://localhost:5114",
                ["ThirdPartyApi:ApiKey"] = apiKey,
                ["ThirdPartyApi:TimeoutSeconds"] = "30"
            })
            .Build();

        // Act
        services.AddThirdPartyApiClient(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var client = provider.GetRequiredService<IThirdPartyApiClient>();
        client.Should().NotBeNull();
    }

    [Fact]
    public void AddThirdPartyApiClient_WithoutBaseUrl_ShouldThrowValidationException()
    {
        // Arrange
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // BaseUrl intentionally omitted
                ["ThirdPartyApi:TimeoutSeconds"] = "30"
            })
            .Build();

        // Act
        services.AddThirdPartyApiClient(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var act = () => provider.GetRequiredService<IThirdPartyApiClient>();
        act.Should().Throw<OptionsValidationException>();
    }
}
