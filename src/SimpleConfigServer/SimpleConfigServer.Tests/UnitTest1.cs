using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.Json;
using System.Text;
using FluentAssertions;
namespace SimpleConfigServer.Tests;

public class ConfigServerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _tempDataDir;
    private readonly string _tempJsonDir;
    private readonly string _tempTextDir;
    private readonly string _tempLogDir;

    public ConfigServerTests(WebApplicationFactory<Program> factory)
    {
        //create all directories
        _tempDataDir = Path.GetTempPath();
        _tempJsonDir = Path.Combine(_tempDataDir, "storage", "json");
        _tempTextDir = Path.Combine(_tempDataDir, "storage", "txt");
        _tempLogDir = Path.Combine(_tempDataDir, "logs");

        Directory.CreateDirectory(_tempDataDir);
        Directory.CreateDirectory(_tempJsonDir);
        Directory.CreateDirectory(_tempTextDir);
        Directory.CreateDirectory(_tempLogDir);
        Environment.SetEnvironmentVariable("DATA_DIR", _tempDataDir);

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services => { });
        });
    }

    // create json testfile
    private async Task CreateTestJsonConfig(string id, object config)
    {
        var configPath = Path.Combine(_tempJsonDir, $"{id}.json");
        await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(config));
    }

    // create txt test file
    private async Task CreateTestTextConfig(string id, string content)
    {
        var configPath = Path.Combine(_tempTextDir, $"{id}.txt");
        await File.WriteAllTextAsync(configPath, content);
    }

    // cleanup the directories
    private void CleanupFile(string id, bool isJson)
    {
        var path = isJson ?
            Path.Combine(_tempJsonDir, $"{id}.json") :
            Path.Combine(_tempTextDir, $"{id}.txt");

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }


    // JSON GET: Missing ID - Bad Request
    [Fact]
    public async Task JsonGet_MissingId_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/json");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }


    // JSON GET: Invalid ID - Bad Request
    [Fact]
    public async Task JsonGet_InvalidId_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/json?id=invalid/id");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }


    // JSON GET: Nonexisting ID - Not Found
    [Fact]
    public async Task JsonGet_NonexistingId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/json?id=nonexisting");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    // JSON GET: Invalid Json - Server Error
    [Fact]
    public async Task JsonGet_InvalidJson_ReturnsServerError()
    {
        var client = _factory.CreateClient();
        var id = "invalid-json";
        await File.WriteAllTextAsync(Path.Combine(_tempJsonDir, $"{id}.json"), "{invalid-json}");

        var response = await client.GetAsync($"/json?id={id}");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.InternalServerError);
    }

    // JSON GET: Valid ID - Content retrieved
    [Fact]
    public async Task JsonGet_ValidId_ReturnsContent()
    {
        var client = _factory.CreateClient();
        var id = "valid-config";
        var config = new { name = "test", value = 123 };
        var expectedJson = JsonSerializer.Serialize(config);
        await CreateTestJsonConfig(id, config);

        var response = await client.GetAsync($"/json?id={id}");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var actualJson = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonElement>(content));
        actualJson.Should().Be(expectedJson);
    }

    // Text GET: Nonexisting ID - Not Found
    [Fact]
    public async Task TextGet_NonexistingId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/text?id=nonexisting");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    // Text GET: Valid ID - Content Retrieved
    [Fact]
    public async Task TextGet_ValidId_ReturnsContent()
    {
        var client = _factory.CreateClient();
        var id = "valid-text";
        var content = "Test content";
        await CreateTestTextConfig(id, content);

        var response = await client.GetAsync($"/text?id={id}");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var resultContent = await response.Content.ReadAsStringAsync();
        resultContent.Should().Be(content);
    }


    // JSON POST: Existing ID - Conflict
    [Fact]
    public async Task JsonPost_ExistingId_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        var id = "existing-config";
        var config = new { name = "test" };
        await CreateTestJsonConfig(id, config);

        var content = new StringContent(JsonSerializer.Serialize(config), Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"/json?id={id}", content);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }


    // JSON POST: Invalid Json - Bad Request
    [Fact]
    public async Task JsonPost_InvalidJson_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var content = new StringContent("{invalid-json}", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/json?id=test-config", content);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    // JSON POST: Valid ID and Content - Ok
    [Fact]
    public async Task JsonPost_ValidIdAndContent_ReturnsOk()
    {
        var client = _factory.CreateClient();
        CleanupFile("new-config", true);
        var config = new { name = "test" };
        var content = new StringContent(JsonSerializer.Serialize(config), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/json?id=new-config", content);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    // Text POST: Existing ID - Conflict
    [Fact]
    public async Task TextPost_ExistingId_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        var id = "existing-text";
        await CreateTestTextConfig(id, "Existing content");

        var content = new StringContent("New content", Encoding.UTF8, "text/plain");
        var response = await client.PostAsync($"/text?id={id}", content);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    // Text POST: Valid ID - Ok
    [Fact]
    public async Task TextPost_ValidId_ReturnsOk()
    {
        var client = _factory.CreateClient();
        CleanupFile("new-text", false);
        var content = new StringContent("Test content", Encoding.UTF8, "text/plain");
        var response = await client.PostAsync("/text?id=new-text", content);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    // JSON PUT: Invalid Json - Bad Request
    [Fact]
    public async Task JsonPut_InvalidJson_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var content = new StringContent("{invalid-json}", Encoding.UTF8, "application/json");
        var response = await client.PutAsync("/json?id=test-config", content);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    // JSON PUT: Valid ID and Content - Ok
    [Fact]
    public async Task JsonPut_ValidIdAndContent_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var config = new { name = "test" };
        var content = new StringContent(JsonSerializer.Serialize(config), Encoding.UTF8, "application/json");
        var response = await client.PutAsync("/json?id=new-config", content);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    // Text PUT: Valid ID - Ok
    [Fact]
    public async Task TextPut_ValidId_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var content = new StringContent("Test content", Encoding.UTF8, "text/plain");
        var response = await client.PutAsync("/text?id=new-text", content);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    // JSON DELETE: Nonexisting ID - Not Found
    [Fact]
    public async Task JsonDelete_NonexistingId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var response = await client.DeleteAsync("/json?id=nonexisting");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    // JSON DELETE: Valid ID - Ok
    [Fact]
    public async Task JsonDelete_ValidId_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var id = "delete-config";
        var config = new { name = "test" };
        await CreateTestJsonConfig(id, config);

        var response = await client.DeleteAsync($"/json?id={id}");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        File.Exists(Path.Combine(_tempJsonDir, $"{id}.json")).Should().BeFalse();
    }

    // Text DELETE: Nonexisting ID - Not Found
    [Fact]
    public async Task TextDelete_NonexistingId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var response = await client.DeleteAsync("/text?id=nonexisting");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    // Text DELETE: Valid ID - Ok
    [Fact]
    public async Task TextDelete_ValidId_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var id = "delete-text";
        await CreateTestTextConfig(id, "Test content");

        var response = await client.DeleteAsync($"/text?id={id}");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        File.Exists(Path.Combine(_tempTextDir, $"{id}.txt")).Should().BeFalse();
    }

    public void Dispose()
    {
        Directory.Delete(_tempDataDir, true);
    }
}