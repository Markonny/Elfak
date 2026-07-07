using Akka.Actor;
using Akka.Configuration;
using System.Net.Http;
using NewsTopicServer.Actors;
using NewsTopicServer.Services;
using NewsTopicServer.WebServer;
 
namespace NewsTopicServer;
 
public static class Program
{
    public static async Task Main(string[] args)
    {

        var apiKey = Environment.GetEnvironmentVariable("NEWSAPI_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Logger.Error("NEWSAPI_KEY environment promenljiva nije postavljena. Server se prekida.");
            Console.WriteLine("Postavi NEWSAPI_KEY environment promenljivu na tvoj NewsAPI kljuc i pokreni ponovo.");
            return;
        }
 
        var configPath = Path.Combine(AppContext.BaseDirectory, "akka.conf");
        var config = File.Exists(configPath)
            ? ConfigurationFactory.ParseString(File.ReadAllText(configPath))
            : ConfigurationFactory.Default();
 
        using var actorSystem = ActorSystem.Create("NewsTopicSystem", config);

        var socketsHandler = new SocketsHttpHandler
        {
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                CertificateRevocationCheckMode = System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck
            }
        };
 
        var httpClient = new HttpClient(socketsHandler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        var newsApiClient = new NewsApiClient(httpClient, apiKey);

        var topicModelingActor = actorSystem.ActorOf(
            Props.Create(() => new TopicModelingActor()),
            "topicModelingActor");
 
        var coordinatorActor = actorSystem.ActorOf(
            Props.Create(() => new KeywordCoordinatorActor(newsApiClient, topicModelingActor)),
            "keywordCoordinatorActor");
 
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            Logger.Info("Zahtev za gasenje servera (Ctrl+C) primljen...");
            cts.Cancel();
        };
 
        var server = new HttpServer(coordinatorActor, "http://localhost:8080/");
 
        try
        {
            await server.RunAsync(cts.Token);
        }
        finally
        {
            Logger.Info("Gasenje ActorSystem-a...");
            await actorSystem.Terminate();
        }
    }
}