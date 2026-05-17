using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using TradingProject.ThirdParty.Application.Common.Models;
using TradingProject.ThirdParty.Domain.Models.Market;

namespace TradingProject.ThirdParty.Application.Tests.Common.Models;

public class FearAndGreedResponseDtoTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private const string Json = """
        {
            "name": "Fear and Greed Index",
            "data": [
                {
                    "value": "27",
                    "value_classification": "Fear",
                    "timestamp": "1778976000",
                    "time_until_update": "11562"
                }
            ],
            "metadata": {
                "error": null
            }
        }
        """;

    [Fact]
    public void Deserialize_ShouldMapAllProperties_WhenGivenValidJson()
    {
        // Act
        var result = JsonSerializer.Deserialize<FearAndGreedResponseDto>(Json, JsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Fear and Greed Index");
        result.Data.Should().HaveCount(1);

        var data = result.Data[0];
        data.Value.Should().Be(27);
        data.ValueClassification.Should().Be("Fear");
        data.Timestamp.Should().Be(1778976000);
        data.TimeUntilUpdate.Should().Be(11562);
    }

    [Fact]
    public void Deserialize_ShouldMapToFearAndGreedIndex_UsingSamePipelineAsService()
    {
        // Act — same pipeline as AlternativeMeService.GetFearAndGreedIndexAsync
        var dto = JsonSerializer.Deserialize<FearAndGreedResponseDto>(Json, JsonOptions);
        dto.Should().NotBeNull();

        var data = dto!.Data[0];
        var index = new FearAndGreedIndex(
            Value: data.Value,
            Classification: data.ValueClassification,
            Timestamp: data.Timestamp);

        // Assert
        index.Value.Should().Be(27);
        index.Classification.Should().Be("Fear");
        index.Timestamp.Should().Be(1_778_976_000);
    }
}
