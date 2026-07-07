using System.Reactive.Concurrency;
using System.Reactive.Linq;
using NewsTopicServer.Models;
 
namespace NewsTopicServer.Services;
 
public class NewsApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
 
    public NewsApiClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
 
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add(
                "User-Agent", "NewsTopicServer/1.0 (ELFAK Sistemsko Programiranje - Projekat 3)");
        }
    }
 
    public IObservable<List<string>> FetchSortedDescriptions(string keyword)
    {
        return Observable
            .FromAsync(() => FetchArticlesAsync(keyword))
            .SubscribeOn(TaskPoolScheduler.Default)
            .Select(articles => articles
                .Select((article, index) => new
                {
                    Description = article.Description,
                    PopularityRank = index
                })           
                .Where(x => !string.IsNullOrWhiteSpace(x.Description))
                .OrderBy(x => x.PopularityRank)
                .Select(x => x.Description!)
                .ToList());
    }
 
    private async Task<List<Article>> FetchArticlesAsync(string keyword)
    {
        var url = "https://newsapi.org/v2/everything" +
                  $"?q={Uri.EscapeDataString(keyword)}" +
                  "&sortBy=popularity" +
                  "&language=en" +
                  "&pageSize=50" +
                  $"&apiKey={_apiKey}";
 
        var httpResponse = await _httpClient.GetAsync(url);
        var body = await httpResponse.Content.ReadAsStringAsync();
 
        NewsApiResponse? response;
        try
        {
            response = System.Text.Json.JsonSerializer.Deserialize<NewsApiResponse>(body);
        }
        catch (System.Text.Json.JsonException)
        {
            response = null;
        }
 
        if (!httpResponse.IsSuccessStatusCode)
        {
            var reason = response is not null
                ? $"{response.ErrorCode}: {response.ErrorMessage}"
                : $"HTTP {(int)httpResponse.StatusCode} - {body}";
            throw new InvalidOperationException($"NewsAPI greska ({(int)httpResponse.StatusCode}): {reason}");
        }
 
        if (response is null)
        {
            throw new InvalidOperationException("NewsAPI je vratio prazan ili nevalidan JSON odgovor.");
        }
 
        if (!string.Equals(response.Status, "ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"NewsAPI greška: {response.ErrorCode} - {response.ErrorMessage}");
        }
 
        return response.Articles;
    }
}