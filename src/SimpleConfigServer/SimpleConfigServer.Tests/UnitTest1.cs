using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.Json;
using System.Text;
using FluentAssertions;
namespace SimpleConfigServer.Tests;

public class ConfigServerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _tempDataDir;
    private readonly string _tempConfigDir;
    private readonly string _tempLogDir;

    public ConfigServerTests(WebApplicationFactory<Program> factory)
    {
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
        var testConfig = new { test = "data" };
        await CreateTestConfig("test-_V1.1", testConfig);
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/config?id=test-_V1.1");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var resultConfig = JsonSerializer.Deserialize<JsonElement>(content);
        resultConfig.GetProperty("test").GetString().Should().Be("data");
    }

    [Fact]
    public async Task GetConfig_WithInvalidId_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/config?id=test:invalidid");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetConfig_WithMissingId_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/config");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetConfig_WithNonexistentId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/config?id=nonexistentid");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetConfig_WithInvalidJson_ReturnsServerError()
    {
        var configPath = Path.Combine(_tempConfigDir, "invalidjson.json");
        await File.WriteAllTextAsync(configPath, "{ invalid json }");
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/config?id=invalidjson");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task PostConfig_WithValidIdAndJson_SavesConfig()
    {
        var client = _factory.CreateClient();
        var testConfig = new { test = "data" };
        var content = new StringContent(JsonSerializer.Serialize(testConfig), Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/config?id=test1", content);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var savedConfig = await File.ReadAllTextAsync(Path.Combine(_tempConfigDir, "test1.json"));
        var resultConfig = JsonSerializer.Deserialize<JsonElement>(savedConfig);
        resultConfig.GetProperty("test").GetString().Should().Be("data");
    }

    [Fact]
    public async Task PostConfig_WithInvalidJson_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var content = new StringContent("{ invalid json }", Encoding.UTF8);

        var response = await client.PostAsync("/config?id=test2", content);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostConfig_WithInvalidId_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var testConfig = new { test = "data" };
        var content = new StringContent(JsonSerializer.Serialize(testConfig), Encoding.UTF8);

        var response = await client.PostAsync("/config?id=invalid:id", content);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostConfig_WithMissingId_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var testConfig = new { test = "data" };
        var content = new StringContent(JsonSerializer.Serialize(testConfig), Encoding.UTF8);

        var response = await client.PostAsync("/config", content);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostConfig_OverwritesExistingConfig()
    {
        var client = _factory.CreateClient();
        var initialConfig = new { test = "initial" };
        var updatedConfig = new { test = "updated" };
        await CreateTestConfig("test3", initialConfig);

        var content = new StringContent(JsonSerializer.Serialize(updatedConfig), Encoding.UTF8);
        var response = await client.PostAsync("/config?id=test3", content);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var savedConfig = await File.ReadAllTextAsync(Path.Combine(_tempConfigDir, "test3.json"));
        var resultConfig = JsonSerializer.Deserialize<JsonElement>(savedConfig);
        resultConfig.GetProperty("test").GetString().Should().Be("updated");
    }

    public void Dispose()
    {
        Directory.Delete(_tempDataDir, true);
    }
}