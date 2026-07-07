using System.Text.Json.Serialization;

namespace NewsTopicServer.Models;

public class ArticleSource
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class Article
{
    [JsonPropertyName("source")]
    public ArticleSource? Source { get; set; }

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("publishedAt")]
    public DateTime? PublishedAt { get; set; }
}
public class NewsApiResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("totalResults")]
    public int TotalResults { get; set; }

    [JsonPropertyName("articles")]
    public List<Article> Articles { get; set; } = new();

    [JsonPropertyName("code")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("message")]
    public string? ErrorMessage { get; set; }
}
