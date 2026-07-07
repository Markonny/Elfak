using System.Net;
using System.Text;
using System.Text.Json;
using System.Web;
using Akka.Actor;
using NewsTopicServer.Messages;
using NewsTopicServer.Services;
 
namespace NewsTopicServer.WebServer;

public class HttpServer
{
    private readonly HttpListener _listener = new();
    private readonly IActorRef _coordinatorActor;
    private readonly string _prefix;
 
    public HttpServer(IActorRef coordinatorActor, string prefix = "http://localhost:8080/")
    {
        _coordinatorActor = coordinatorActor;
        _prefix = prefix;
        _listener.Prefixes.Add(prefix);
    }
 
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _listener.Start();
        Logger.Info($"Web server pokrenut na {_prefix} (Ctrl+C za izlaz)");
 
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
 
            _ = Task.Run(() => HandleRequestAsync(context), cancellationToken);
        }
 
        _listener.Stop();
    }
 
    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
 
        var query = HttpUtility.ParseQueryString(request.Url?.Query ?? "");
        Logger.Request(request.HttpMethod ?? "GET", request.Url?.AbsolutePath ?? "/", request.Url?.Query.TrimStart('?') ?? "");
 
        try
        {
            if (request.Url?.AbsolutePath != "/topics")
            {
                await WriteResponseAsync(response, HttpStatusCode.NotFound,
                    new { error = "Nepoznata ruta. Koristi GET /topics?keyword=..." });
                return;
            }
 
            var keyword = query["keyword"];
            if (string.IsNullOrWhiteSpace(keyword))
            {
                await WriteResponseAsync(response, HttpStatusCode.BadRequest,
                    new { error = "Nedostaje obavezan 'keyword' query parametar." });
                return;
            }
 
            var result = await _coordinatorActor.Ask<TopicsResult>(
                new GetTopicsRequest(keyword), TimeSpan.FromSeconds(30));
 
            Logger.RequestResult(keyword, success: true,
                details: $"{result.TotalDescriptionsAnalyzed} opisa, {result.TopicDistribution.Count} tema");
 
            await WriteResponseAsync(response, HttpStatusCode.OK, result);
        }
        catch (TaskCanceledException)
        {
            Logger.Error("Zahtev je istekao (timeout) pri cekanju odgovora od aktora.");
            await WriteResponseAsync(response, HttpStatusCode.GatewayTimeout,
                new { error = "Server nije odgovorio na vreme (timeout)." });
        }
        catch (Exception ex)
        {
            Logger.Error($"Neocekivana greska pri obradi zahteva: {ex.Message}");
            await WriteResponseAsync(response, HttpStatusCode.InternalServerError,
                new { error = ex.Message });
        }
    }
 
    private static async Task WriteResponseAsync(HttpListenerResponse response, HttpStatusCode status, object payload)
    {
        response.StatusCode = (int)status;
        response.ContentType = "application/json; charset=utf-8";
 
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        var buffer = Encoding.UTF8.GetBytes(json);
 
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
        response.OutputStream.Close();
    }
}