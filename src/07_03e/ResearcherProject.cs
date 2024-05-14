using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using System.Text.Json;
using _07_02e.WebSearch;


namespace _07_02e;

public class ResearcherProject
{
  static string searchResultsFileName = "searchResults.json";
  static string researchReportFileName = "ResearchReport.txt";

  public async Task ExecuteAsync()
  {
    var modelDeploymentName = "Gpt4v32k";
    var azureOpenAIEndpoint = Environment.GetEnvironmentVariable("AZUREOPENAI_ENDPOINT");
    var azureOpenAIApiKey = Environment.GetEnvironmentVariable("AZUREOPENAI_APIKEY");
    string bingApiKey = Environment.GetEnvironmentVariable("BING_APIKEY");

    var builder = Kernel.CreateBuilder();
    builder.Services.AddAzureOpenAIChatCompletion(
        modelDeploymentName,
        azureOpenAIEndpoint,
        azureOpenAIApiKey,
        modelId: "gpt-4-32k"
    );
    var kernel = builder.Build();

    // AddingCustom plugin for web search 
    var webSearchEnginePlugin = new WebSearchPlugin(bingApiKey);
    var webSearchEnginePluginName = webSearchEnginePlugin.GetType().Name;
    kernel.ImportPluginFromObject(webSearchEnginePlugin, webSearchEnginePluginName);

    // Execute the search
    // Note: this is decoupled from the handlebars template due that executing it from it is not working.
    // the issue is that the handlebars function call is executed but the outcome converted from JSON to plain text.
    // see: https://github.com/microsoft/semantic-kernel/issues/4895
    var topicOfResearch = "What are the latest generative AI models and advancements for the last week?";
    List<WebSearchResult> webSearchResults = await SearchWithPlugin(
        kernel,
        webSearchEnginePluginName,
        topicOfResearch,
        10,
        false);

  }

  private static async Task<List<WebSearchResult>> SearchWithPlugin(
      Kernel kernel,
      string searchPluginName,
      string question,
      int searchResultsCount = 10,
      bool retrieveSearchFromFile = false)
  {
    string searchResultValue = string.Empty;

    if (!retrieveSearchFromFile)
    {
      Console.WriteLine(question);
      Console.WriteLine($"----{searchPluginName}----");

      var searchResult =
          await kernel.InvokeAsync(searchPluginName, "Search", new() {
              { "query", question },
              { "count", searchResultsCount },
              { "offset", 0 },
              { "freshness", "Week" }
          });

      Console.WriteLine(searchResult);
      searchResultValue = searchResult.GetValue<string>();

      // Save the search result
      await File.WriteAllTextAsync(searchResultsFileName, searchResultValue);
    }
    else
    {
      // Retrieve the search result
      searchResultValue = await File.ReadAllTextAsync(searchResultsFileName);
    }

    // Parse into array of objects
    List<WebSearchResult> webSearchResults = DeserializeWebSearchResults(searchResultValue);

    return webSearchResults;
  }

  private static List<WebSearchResult> DeserializeWebSearchResults(string json)
  {
    var options = new JsonSerializerOptions
    {
      PropertyNameCaseInsensitive = true // This makes the parser case-insensitive to property names
    };

    List<WebSearchResult> webSearchResults =
        JsonSerializer.Deserialize<List<WebSearchResult>>(json, options);

    return webSearchResults;
  }

}