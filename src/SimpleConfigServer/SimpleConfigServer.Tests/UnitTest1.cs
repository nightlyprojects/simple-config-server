
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;


namespace SimpleConfigServer.Tests;

public class ConfigServerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _tempDataDir;
    private readonly string _tempConfigDir;
    private readonly string _tempLogDir;


    public ConfigServerTests(WebApplicationFactory<Program> factory)
    {
        // specify all paths
        _tempDataDir = Path.GetTempPath();
        _tempConfigDir = Path.Combine(_tempDataDir, "configs");
        _tempLogDir = Path.Combine(_tempDataDir, "logs");

        Directory.CreateDirectory(_tempDataDir);
        Directory.CreateDirectory(_tempConfigDir);
        Directory.CreateDirectory(_tempLogDir);
        Environment.SetEnvironmentVariable("DATA_DIR", _tempDataDir);

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services => { });
        });
    }

    private async Task CreateTestConfig(string id, object config)
    {
        var configPath = Path.Combine(_tempConfigDir, $"{id}.json");
        await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(config));
    }

    [Fact]
    public async Task GetConfig_WithValidId_ReturnsConfig()
    {
        // Arrange
        var testConfig = new { test = "data" };
        await CreateTestConfig("test-_V1.1", testConfig);

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/config?id=test-_V1.1");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var resultConfig = JsonSerializer.Deserialize<JsonElement>(content);
        resultConfig.GetProperty("test").GetString().Should().Be("data");
    }

    [Fact]
    public async Task GetConfig_WithInvalidId_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/config?id=test:invalidid");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetConfig_WithMissingId_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/config");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetConfig_WithNonexistentId_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/config?id=nonexistentid");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetConfig_WithInvalidJson_ReturnsServerError()
    {
        // Arrange
        var configPath = Path.Combine(_tempConfigDir, "invalidjson.json");
        await File.WriteAllTextAsync(configPath, "{ invalid json }");

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/config?id=invalidjson");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.InternalServerError);
    }


    public void Dispose()
    {
        Directory.Delete(_tempDataDir, true);
    }
}