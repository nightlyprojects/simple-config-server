using Makaretu.Dns;
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();
        var mdns = new MulticastService();
        var sd = new ServiceDiscovery(mdns);
        mdns.Start();
        
        // Doesn't work: Hostname is MyService.config.local - Windows don't like that for some reason
        var service = new ServiceProfile("MyService", "_config._tcp", 24024);

        // Works, since hostname is now MyService.local, but the type definition is against the standard:
        // var service = new ServiceProfile("MyService", "._tcp", 24024); 

        sd.Advertise(service);
        app.MapGet("/config", () => "{ \"test\":\"data\" }");
        app.Run($"http://*:{24024}");
    }
}