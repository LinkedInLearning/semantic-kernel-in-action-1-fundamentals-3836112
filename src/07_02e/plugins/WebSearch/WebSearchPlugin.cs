using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;

namespace _07_02e.WebSearch;

public sealed class WebSearchPlugin
{
  private readonly string _bingApiKey;
  public static readonly HttpClient Client = new HttpClient();

  private static readonly JsonSerializerOptions s_jsonOptionsCache = new()
  {
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
  };

  public WebSearchPlugin(string bingApiKey)
  {
    _bingApiKey = bingApiKey;
  }

  [KernelFunction, Description("Perform a web search.")]
  public async Task<string> Search(
      [Description("Search query")] string query,
      [Description("Number of results")] int count = 20,
      [Description("Number of results to skip")] int offset = 0,
      [Description("Freshness")] string freshness = "Week",
      CancellationToken cancellationToken = default)
  {
    string result = string.Empty;

    try
    {
      result = await WebSearchJSON(query, count, offset, freshness, cancellationToken);
    }
    catch (Exception ex)
    {
      throw;
    }

    return result;
  }

  private async Task<string> WebSearchJSON(
      string query,
      int numResults,
      int offset,
      string freshness,
      CancellationToken cancellationToken = default)
  {
    WebPage[]? results = await WebSearchInternal(query, numResults, offset, freshness);

    return JsonSerializer.Serialize(results);
  }

  private async Task<WebPage[]?> WebSearchInternal(
    string query,
    int numResults,
    int offset,
    string freshness)
  {
    string json = string.Empty;

    Uri uri = new($"https://api.bing.microsoft.com/v7.0/search?q={Uri.EscapeDataString(query)}&count={numResults}&offset={offset}");
    if (!string.IsNullOrEmpty(freshness))
    {
      uri = new($"https://api.bing.microsoft.com/v7.0/search?q={Uri.EscapeDataString(query)}&count={numResults}&offset={offset}&freshness={freshness}");
    }

    Client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _bingApiKey);
    json = await Client.GetStringAsync(uri);
    BingSearchResponse? data = JsonSerializer.Deserialize<BingSearchResponse>(json);
    WebPage[]? results = data?.WebPages?.Value;

    return results;
  }

  [SuppressMessage("Performance", "CA1812:Internal class that is apparently never instantiated",
      Justification = "Class is instantiated through deserialization.")]
  private sealed class BingSearchResponse
  {
    [JsonPropertyName("webPages")]
    public WebPages? WebPages { get; set; }
  }

  [SuppressMessage("Performance", "CA1812:Internal class that is apparently never instantiated",
      Justification = "Class is instantiated through deserialization.")]
  private sealed class WebPages
  {
    [JsonPropertyName("value")]
    public WebPage[]? Value { get; set; }
  }

  [SuppressMessage("Performance", "CA1812:Internal class that is apparently never instantiated",
      Justification = "Class is instantiated through deserialization.")]
  private sealed class WebPage
  {
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("snippet")]
    public string Snippet { get; set; } = string.Empty;

    [JsonPropertyName("datePublished")]
    public DateTime DatePublished { get; set; } = DateTime.MinValue;
  }
}