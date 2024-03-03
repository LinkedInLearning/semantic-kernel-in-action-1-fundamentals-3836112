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
    var azureOpenAIEndpoint = Environment.GetEnvironmentVariable("AzureOpenAI_Endpoint", EnvironmentVariableTarget.User);
    var azureOpenAIApiKey = Environment.GetEnvironmentVariable("AzureOpenAI_ApiKey", EnvironmentVariableTarget.User);
    string bingApiKey = Environment.GetEnvironmentVariable("Bing_ApiKey", EnvironmentVariableTarget.User);

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
  }

}