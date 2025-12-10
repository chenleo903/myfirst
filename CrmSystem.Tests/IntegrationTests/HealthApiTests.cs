using System.Net;
using System.Text.Json;
using Xunit;

namespace CrmSystem.Tests.IntegrationTests;

/// <summary>
/// Integration tests for Health API endpoint
/// Tests Requirements: 7.3, 7.4, 7.7
/// </summary>
[Collection("Integration")]
public class HealthApiTests : IntegrationTestBase
{
    public HealthApiTests(CrmApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetHealth_ReturnsHealthyStatus()
    {
        // Act
        var response = await Client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var healthStatus = JsonSerializer.Deserialize<HealthStatusResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        Assert.NotNull(healthStatus);
        Assert.Equal("Healthy", healthStatus.Status);
        Assert.NotNull(healthStatus.Checks);
        Assert.True(healthStatus.Checks.ContainsKey("database"));
        Assert.Equal("Healthy", healthStatus.Checks["database"]);
    }

    [Fact]
    public async Task GetHealth_DoesNotExposeConnectionString()
    {
        // Act
        var response = await Client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Should not contain sensitive database info
        Assert.DoesNotContain("password", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Server=", content, StringComparison.OrdinalIgnoreCase);
    }

    private class HealthStatusResponse
    {
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, string> Checks { get; set; } = new();
    }
}
