using Microsoft.AspNetCore.Hosting.Server;
using SimpleConfigServer.Logger;
using System.Reflection.Metadata;
using System;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        //for testing:
        //Environment.SetEnvironmentVariable("DATA_DIR", @"C:\Users\andre\Documents\Github\nightlyprojects\simple-config-server\examples\sample-working-dir\data");

        // do not set the variable if using docker. Mount the volume to "/data"
        var dataDir = Environment.GetEnvironmentVariable("DATA_DIR") ?? "data";
        var logLevel = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "Information";
        ushort port = 24025;

        //setup directories
        var configDir = Path.Combine(dataDir, "configs");
        var logDir = Path.Combine(dataDir, "logs");
        if (!Directory.Exists(dataDir))
        {
            Console.Error.WriteLine($"Directory {dataDir} does not exist or is not accessible");
            Console.WriteLine($"Directory {dataDir} does not exist or is not accessible");
            Environment.Exit(1);
        }
        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
        }
        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }

        //build app
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new CustomLoggerProvider(logDir));

        var app = builder.Build();
        var logger = app.Logger;

        app.MapGet("/config", async (string? id, HttpContext context) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    logger.LogError("Missing required 'id' parameter");
                    return Results.BadRequest("Missing required 'id' parameter");
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(id, @"^[a-zA-Z0-9][a-zA-Z0-9\-_.]*$"))
                {
                    logger.LogError($"Invalid identifier format: {id}");
                    return Results.BadRequest("Invalid identifier format");
                }

                var configPath = Path.Combine(configDir, $"{id}.json");

                if (!File.Exists(configPath))
                {
                    logger.LogError($"Config not found for identifier {id}");
                    return Results.NotFound($"Config not found for identifier {id}");
                }

                var content = File.ReadAllText(configPath);

                try
                {
                    JsonDocument.Parse(content);
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

        app.MapPost("/config", async (string? id, HttpContext context) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    logger.LogError("Missing required 'id' parameter");
                    return Results.BadRequest("Missing required 'id' parameter");
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(id, @"^[a-zA-Z0-9][a-zA-Z0-9\-_.]*$"))
                {
                    logger.LogError($"Invalid identifier format: {id}");
                    return Results.BadRequest("Invalid identifier format");
                }

                using var reader = new StreamReader(context.Request.Body);
                var content = await reader.ReadToEndAsync();

                try
                {
                    JsonDocument.Parse(content);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Invalid JSON in request body: {ex.Message}");
                    return Results.BadRequest("Invalid JSON format");
                }

                var configPath = Path.Combine(configDir, $"{id}.json");
                await File.WriteAllTextAsync(configPath, content);

                logger.LogInformation($"Successfully saved config for {id}");
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