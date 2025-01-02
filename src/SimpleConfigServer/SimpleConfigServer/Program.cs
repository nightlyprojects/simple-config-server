using SimpleConfigServer.RollingLogger;
using System.Text.Json;
using Makaretu.Dns;
using System.Net;
using System.Net.Sockets;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // just for temp test, remove for testing
        Environment.SetEnvironmentVariable("DATA_DIR"
            , @"C:\Users\andre\Documents\Github\nightlyprojects\simple-config-server\examples\sample-working-dir\data");
        Environment.SetEnvironmentVariable("LOG_LEVEL", "Information");

        // setup all constants
        var dataDir = Environment.GetEnvironmentVariable("DATA_DIR") ?? "data";
        var logLevel = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "Information";

        ushort port = 24024;

        var configDir = Path.Combine(dataDir, "configs");
        var logDir = Path.Combine(dataDir, "logs");
      
        // check working directory
        if (!Directory.Exists(dataDir))
        {
            Console.Error.WriteLine($"Directory {dataDir} does not exist or is not accessible");
            Environment.Exit(1);
        }

        // create directory for accessible config files
        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
        }

        // create directory for logs
        if (!Directory.Exists(logDir))  //create log directory if not existing
        {
            Directory.CreateDirectory(logDir);
        }

        // Setup logging
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new CustomLoggerProvider(logDir));

        // create application and service instances
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

                if (!System.Text.RegularExpressions.Regex.IsMatch(id, @"^[a-zA-Z0-9][a-zA-Z0-9\-_#]*$"))
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

                logger.LogInformation($"Sucessfully served config for {id}");
                return Results.Content(content, "application/json");

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