
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
        Environment.SetEnvironmentVariable("DATA_DIR", _testDataDir);

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services => { });
        });
    }

    private async Task CreateTestConfig(string id, object config)
    {
        var configPath = Path.Combine(_testDataDir, "configs", $"{id}.json");
        await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(config));
    }

    private async Task CreateServerConfig(ServerConfig config)
    {
        var configPath = Path.Combine(_testDataDir, "server-config.json");
        await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(config));
    }

    [Fact]
    public async Task GetConfig_WithValidId_ReturnsConfig()
    {
        // Arrange
        var testConfig = new { test = "data" };
        await CreateTestConfig("test-_#1", testConfig);

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/config?id=test-_#1");

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
        var response = await client.GetAsync("/config?id=test:invalid");

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
        var response = await client.GetAsync("/config?id=nonexistent");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetConfig_WithInvalidJson_ReturnsServerError()
    {
        // Arrange
        var configPath = Path.Combine(_testDataDir, "configs", "invalid.json");
        await File.WriteAllTextAsync(configPath, "{ invalid json }");

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/config?id=invalid");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Server_UsesCustomConfig_WhenProvided()
    {
        // Arrange
        var serverConfig = new ServerConfig
        {
            ServiceName = "customserver",
            ServiceType = "_customconfig._tcp",
            Port = 5001
        };
        await CreateServerConfig(serverConfig);

        // We need to create a new factory instance to pick up the new config
        var customFactory = _factory.WithWebHostBuilder(builder => { });
        var config = customFactory.Services.GetRequiredService<ServerConfig>();

        // Assert
        config.ServiceName.Should().Be("customserver");
        config.ServiceType.Should().Be("_customconfig._tcp");
        config.Port.Should().Be(5001);
    }

    public void Dispose()
    {
        Directory.Delete(_testDataDir, true);
    }
}