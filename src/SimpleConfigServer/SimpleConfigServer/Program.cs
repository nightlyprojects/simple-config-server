using SimpleConfigServer.Logger;
using System.Text.Json;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        //for debugging:
        //Environment.SetEnvironmentVariable("DATA_DIR", @"C:\Users\andre\Documents\Github\nightlyprojects\simple-config-server\examples\sample-working-dir\data");

        // do not set the variable if using docker. Mount the volume to "/data"
        var dataDir = Environment.GetEnvironmentVariable("DATA_DIR") ?? "data";
        var logLevel = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "Information";
        ushort port = 24025;

        //setup directories
        var jsonDir = Path.Combine(dataDir, "storage", "json");
        var textDir = Path.Combine(dataDir, "storage", "txt");
        var logDir = Path.Combine(dataDir, "logs");

        if (!Directory.Exists(dataDir))
        {
            Console.Error.WriteLine($"Directory {dataDir} does not exist or is not accessible");
            Environment.Exit(1);
        }

        Directory.CreateDirectory(jsonDir);
        Directory.CreateDirectory(textDir);
        Directory.CreateDirectory(logDir);
        
        //build app
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new CustomLoggerProvider(logDir));

        var app = builder.Build();
        var logger = app.Logger;

        // Method to validate a given ID
        bool IsValidId(string id)
            => System.Text.RegularExpressions.Regex.IsMatch(id, @"^[a-zA-Z0-9][a-zA-Z0-9\-_.]*$");



        // JSON Config endpoint: read file
        app.MapGet("/json", async (string? id, HttpContext context) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    logger.LogError("Missing required 'id' parameter");
                    return Results.BadRequest("Missing required 'id' parameter");
                }

                if (!IsValidId(id))
                {
                    logger.LogError($"Invalid identifier format: {id}");
                    return Results.BadRequest("Invalid identifier format");
                }

                var configPath = Path.Combine(jsonDir, $"{id}.json");

                if (!File.Exists(configPath))
                {
                    logger.LogError($"Config not found for identifier {id}");
                    return Results.NotFound($"Config not found for identifier {id}");
                }

                var content = await File.ReadAllTextAsync(configPath);

                try
                {
                    await Task.Run(() => JsonDocument.Parse(content));
                }
                catch (Exception ex)
                {
                    logger.LogError($"Invalid Json in config file {id}: {ex.Message}");
                    return Results.StatusCode(500);
                }

                logger.LogInformation($"Successfully served config for {id}");
                return Results.Content(content, "application/json");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error processing request for {id}: {ex}");
                return Results.StatusCode(500);
            }
        });

        // JSON Config endpoint: create new file
        app.MapPost("/json", async (string? id, HttpContext context) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    logger.LogError("Missing required 'id' parameter");
                    return Results.BadRequest("Missing required 'id' parameter");
                }

                if (!IsValidId(id))
                {
                    logger.LogError($"Invalid identifier format: {id}");
                    return Results.BadRequest("Invalid identifier format");
                }

                var configPath = Path.Combine(jsonDir, $"{id}.json");
                if (File.Exists(configPath))
                {
                    logger.LogError($"Config already exists for identifier {id}. Use PUT to overwrite the content.");
                    return Results.Conflict($"Config already exists for identifier {id}. Use PUT to overwrite the content.");
                }

                using var reader = new StreamReader(context.Request.Body);
                var content = await reader.ReadToEndAsync();

                try
                {
                    await Task.Run(() => JsonDocument.Parse(content));
                }
                catch (Exception ex)
                {
                    logger.LogError($"Invalid JSON in request body: {ex.Message}");
                    return Results.BadRequest("Invalid JSON format");
                }

                await File.WriteAllTextAsync(configPath, content);
                logger.LogInformation($"Successfully created config for {id}");
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError($"Error processing request for {id}: {ex}");
                return Results.StatusCode(500);
            }
        });

        // JSON Config endpoint: update existing file or create new file
        app.MapPut("/json", async (string? id, HttpContext context) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    logger.LogError("Missing required 'id' parameter");
                    return Results.BadRequest("Missing required 'id' parameter");
                }

                if (!IsValidId(id))
                {
                    logger.LogError($"Invalid identifier format: {id}");
                    return Results.BadRequest("Invalid identifier format");
                }

                using var reader = new StreamReader(context.Request.Body);
                var content = await reader.ReadToEndAsync();

                try
                {
                    await Task.Run(() => JsonDocument.Parse(content));
                }
                catch (Exception ex)
                {
                    logger.LogError($"Invalid JSON in request body: {ex.Message}");
                    return Results.BadRequest("Invalid JSON format");
                }

                var configPath = Path.Combine(jsonDir, $"{id}.json");
                await File.WriteAllTextAsync(configPath, content);
                logger.LogInformation($"Successfully updated config for {id}");
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError($"Error processing request for {id}: {ex}");
                return Results.StatusCode(500);
            }
        });

        // JSON Config endpoint: delete file
        app.MapDelete("/json", async (string? id, HttpContext context) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    logger.LogError("Missing required 'id' parameter");
                    return Results.BadRequest("Missing required 'id' parameter");
                }

                if (!IsValidId(id))
                {
                    logger.LogError($"Invalid identifier format: {id}");
                    return Results.BadRequest("Invalid identifier format");
                }

                var configPath = Path.Combine(jsonDir, $"{id}.json");
                if (!File.Exists(configPath))
                {
                    logger.LogError($"Config not found for identifier {id}");
                    return Results.NotFound($"Config not found for identifier {id}");
                }

                await Task.Run( () => File.Delete(configPath));
                logger.LogInformation($"Successfully deleted config for {id}");
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError($"Error processing request for {id}: {ex}");
                return Results.StatusCode(500);
            }
        });



        //Text File endpoint: read file
        app.MapGet("/text", async (string? id, HttpContext context) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    logger.LogError("Missing required 'id' parameter");
                    return Results.BadRequest("Missing required 'id' parameter");
                }

                if (!IsValidId(id))
                {
                    logger.LogError($"Invalid identifier format: {id}");
                    return Results.BadRequest("Invalid identifier format");
                }

                var textPath = Path.Combine(textDir, $"{id}.txt");
                if (!File.Exists(textPath))
                {
                    logger.LogError($"Text not found for identifier {id}");
                    return Results.NotFound($"Text not found for identifier {id}");
                }

                var content = await File.ReadAllTextAsync(textPath);
                logger.LogInformation($"Successfully served text for {id}");
                return Results.Content(content, "text/plain");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error processing request for {id}: {ex}");
                return Results.StatusCode(500);
            }
        });

        //Text File endpoint: create file
        app.MapPost("/text", async (string? id, HttpContext context) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    logger.LogError("Missing required 'id' parameter");
                    return Results.BadRequest("Missing required 'id' parameter");
                }

                if (!IsValidId(id))
                {
                    logger.LogError($"Invalid identifier format: {id}");
                    return Results.BadRequest("Invalid identifier format");
                }

                var textPath = Path.Combine(textDir, $"{id}.txt");
                if (File.Exists(textPath))
                {
                    logger.LogError($"Text already exists for identifier {id}");
                    return Results.Conflict($"Text already exists for identifier {id}");
                }

                using var reader = new StreamReader(context.Request.Body);
                var content = await reader.ReadToEndAsync();

                await File.WriteAllTextAsync(textPath, content);
                logger.LogInformation($"Successfully created text for {id}");
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError($"Error processing request for {id}: {ex}");
                return Results.StatusCode(500);
            }
        });

        //Text File endpoint: update existing file or create new file
        app.MapPut("/text", async (string? id, HttpContext context) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    logger.LogError("Missing required 'id' parameter");
                    return Results.BadRequest("Missing required 'id' parameter");
                }

                if (!IsValidId(id))
                {
                    logger.LogError($"Invalid identifier format: {id}");
                    return Results.BadRequest("Invalid identifier format");
                }

                using var reader = new StreamReader(context.Request.Body);
                var content = await reader.ReadToEndAsync();

                var textPath = Path.Combine(textDir, $"{id}.txt");
                await File.WriteAllTextAsync(textPath, content);
                logger.LogInformation($"Successfully updated text for {id}");
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError($"Error processing request for {id}: {ex}");
                return Results.StatusCode(500);
            }
        });

        //Text File endpoint: delete file
        app.MapDelete("/text", async (string? id, HttpContext context) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    logger.LogError("Missing required 'id' parameter");
                    return Results.BadRequest("Missing required 'id' parameter");
                }

                if (!IsValidId(id))
                {
                    logger.LogError($"Invalid identifier format: {id}");
                    return Results.BadRequest("Invalid identifier format");
                }

                var textPath = Path.Combine(textDir, $"{id}.txt");
                if (!File.Exists(textPath))
                {
                    logger.LogError($"Text not found for identifier {id}");
                    return Results.NotFound($"Text not found for identifier {id}");
                }

                await Task.Run(() => File.Delete(textPath));
                logger.LogInformation($"Successfully deleted text for {id}");
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError($"Error processing request for {id}: {ex}");
                return Results.StatusCode(500);
            }
        });


        app.Run($"http://*:{port}");
    }
}