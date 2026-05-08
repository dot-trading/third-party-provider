using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using TradingProject.ThirdParty.Infrastructure.Settings;

namespace TradingProject.ThirdParty.Application.Tests.Infrastructure.Settings;

public class CacheSettingsValidationTests
{
    [Fact]
    public void DefaultSettings_ShouldBeValid()
    {
        var settings = new CacheSettings();
        var errors = Validate(settings);

        errors.Should().BeEmpty();
        settings.Enabled.Should().BeTrue();
        settings.Provider.Should().Be("Redis");
    }

    [Fact]
    public void EnabledFalse_WithAnyProvider_ShouldBeValid()
    {
        var settings = new CacheSettings { Enabled = false, Provider = "Memory" };
        var errors = Validate(settings);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void MemoryProvider_ShouldBeValid()
    {
        var settings = new CacheSettings { Enabled = true, Provider = "Memory" };
        var errors = Validate(settings);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void RedisProvider_ShouldBeValid()
    {
        var settings = new CacheSettings { Enabled = true, Provider = "Redis" };
        var errors = Validate(settings);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void InvalidProvider_ShouldFailValidation()
    {
        var settings = new CacheSettings { Enabled = true, Provider = "InvalidProvider" };
        var errors = Validate(settings);

        errors.Should().Contain(e =>
            e.ErrorMessage != null &&
            e.ErrorMessage.Contains("Cache provider must be either 'Redis' or 'Memory'."));
    }

    [Fact]
    public void EmptyProvider_ShouldFailValidation()
    {
        var settings = new CacheSettings { Enabled = true, Provider = string.Empty };
        var errors = Validate(settings);

        errors.Should().NotBeEmpty();
    }

    [Fact]
    public void NullProvider_ShouldFailValidation()
    {
        var settings = new CacheSettings { Enabled = true, Provider = null! };
        var errors = Validate(settings);

        errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ProviderCaseSensitivity_ShouldBeCaseSensitive()
    {
        // The [RegularExpression] attribute is case-sensitive by default,
        // so "redis" (lowercase) should fail.
        var settings = new CacheSettings { Enabled = true, Provider = "redis" };
        var errors = Validate(settings);

        errors.Should().Contain(e =>
            e.ErrorMessage != null &&
            e.ErrorMessage.Contains("Cache provider must be either 'Redis' or 'Memory'."));
    }

    /// <summary>
    /// Runs DataAnnotations validation against the <paramref name="settings"/> and returns
    /// the collection of validation errors (if any).
    /// </summary>
    private static List<ValidationResult> Validate(CacheSettings settings)
    {
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(settings, context, results, validateAllProperties: true);
        return results;
    }
}
