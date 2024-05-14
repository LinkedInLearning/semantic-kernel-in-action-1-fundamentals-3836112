using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Diagnostics;
using System.Text.Json;
using _07_02e.WebSearch;

namespace _07_02e;

public class ResearcherProject
{
  static string searchResultsFileName = "searchResults.json";
  static string researchReportFileName = "ResearchReport.txt";
  static string searchResultsSummarizedFileName = "searchResultsSummarized.txt";

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

    // Adding custom plugin for web search analysis and report generation
    AddWebSearchAnalysisPlugin(kernel);

    // Execute the search
    // Note: this is decoupled from the handlebars template due that executing it from it is not working.
    // the issue is that the handlebars function call is executed but the outcome converted from JSON to plain text.
    // see: https://github.com/microsoft/semantic-kernel/issues/4895
    var topicOfResearch = "What are the latest generative AI models and advancements for the last week?";
    List<WebSearchResult> webSearchResults = await SearchWithPlugin(
        kernel,
        webSearchEnginePluginName,
        topicOfResearch,
        35,
        false);

    var outcome = await ProcessAndSummarizeSearchResults(kernel, webSearchResults, topicOfResearch);

  }


  private async Task<string> ProcessAndSummarizeSearchResults(
    Kernel kernel,
    List<WebSearchResult> webSearchResults,
    string topicOfResearch)
  {
    string summarizeWebSearch = @"
            {{set ""searchResults"" (concat  ""---\n"" "" Search Results \n Topic: "" topicOfResearch ""\n---\n"")}}

            {{#each webSearchResults}}
                {{set ""summary"" (WebSearchAnalysis-Summarize this.Name this.Snippet )}}
                {{set ""searchResults"" (concat searchResults ""URL: "" this.Url  ""\n"")}}
                {{set ""searchResults"" (concat searchResults ""Name: "" this.Name  ""\n"")}}                  
                {{set ""score"" (WebSearchAnalysis-DetermineRelevance topicOfResearch this.Name this.Snippet )}}
                {{set ""searchResults"" (concat searchResults ""Relevance: "" score  ""\n"")}}               
                {{set ""searchResults"" (concat searchResults ""Summary: "" summary  ""\n"")}}
                {{set ""searchResults"" (concat searchResults ""---\n"")}}                  
            {{/each}}

            {{set ""reportForResearchTopic"" (WebSearchAnalysis-GenerateResearchReport topicofresearch=topicOfResearch searchresults=searchResults)}}

            Your goal is to provide the search results as they are provided next.
            Just OUTPUT The following search results as is, do not modify anything:
            {{json reportForResearchTopic}}            
         ";

    PromptExecutionSettings promptExecutionSettings = new OpenAIPromptExecutionSettings()
    {
      MaxTokens = 18000,
      Temperature = 0.4,
    };

    var HandlebarsSPromptFunction = kernel.CreateFunctionFromPrompt(
        new PromptTemplateConfig()
        {
          Template = summarizeWebSearch,
          TemplateFormat = "handlebars",
          ExecutionSettings = {
                  {
                      "default",
                      new OpenAIPromptExecutionSettings() {
                          MaxTokens = 18000,
                          Temperature = 0.3
                      }
                  }
              }
        },
          new HandlebarsPromptTemplateFactory()
        );

    // Time it
    var sw = new Stopwatch();
    sw.Start();

    // Invoke prompt
    var customHandlebarsPromptResult = await kernel.InvokeAsync(
                HandlebarsSPromptFunction,
                new() {
                        { "webSearchResults", webSearchResults.Take(35) },
                        { "topicOfResearch", topicOfResearch }
                }
            );

    // Stop the timer
    sw.Stop();

    Console.WriteLine($"OUTCOME  \n");
    Console.WriteLine($"Milliseconds: {sw.ElapsedMilliseconds} \n");
    long totalMilliseconds = sw.ElapsedMilliseconds;
    long minutes = totalMilliseconds / (1000 * 60);
    long seconds = (totalMilliseconds / 1000) % 60;
    long milliseconds = totalMilliseconds % 1000;

    Console.WriteLine($"{minutes} min, {seconds} sec, {milliseconds} ms");

    string resultantString = customHandlebarsPromptResult.GetValue<string>();

    var correctedOutput = resultantString.Replace("\\n", "\n");

    // Store to file
    await File.WriteAllTextAsync(searchResultsSummarizedFileName, correctedOutput);

    Console.WriteLine($" Result: {correctedOutput}");

    return resultantString;
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

  private static async Task AddWebSearchAnalysisPlugin(Kernel kernel)
  {
    PromptExecutionSettings promptExecutionSettings = new OpenAIPromptExecutionSettings()
    {
      MaxTokens = 8000,
      Temperature = 0.5,
    };

    var kernelFunctionDetermineRelevance = kernel.CreateFunctionFromPrompt(
        new PromptTemplateConfig()
        {
          Name = "DetermineRelevance",
          Description = "Determines the relevance of a concrete web search result.",
          Template = @"Provided with a web search result of a Name and a Snippet of the search content, 
                    analyze the name and snippet to determine the relevance to the provided topic of research.
                    Topic of research: {{$topicofresearch}},
                    Name: {{$name}},
                    Snippet: {{$snippet}},
                    Respond Only true, being 0 no relevance at all and 10 very relevant.
                    Provide Just the number, nothing else.",
          TemplateFormat = "semantic-kernel",
          InputVariables = [
                new() { Name = "topicofresearch" },
                new() { Name = "name" },
                new() { Name = "snippet" }
            ]
        });

    var kernelFunctionSummarize = kernel.CreateFunctionFromPrompt(
        new PromptTemplateConfig()
        {
          Name = "Summarize",
          Description = "Summarizes a concrete web search result.",
          Template = @"Provided with a web search result of a Name and a Snippet of the search content, 
                    Summarize the name and snippet into a short and concise paragraph.
                    The paragraph should be a summary of the content of the snippet and the name.
                    The summary should contain all the facts, names, and relevant information mentioned in the snippet and name.
                    Name: {{$name}},
                    Snippet: {{$snippet}},
                    Do not add any information which is not present in the snippet and name.",
          TemplateFormat = "semantic-kernel",
          InputVariables = [
                new() { Name = "name" },
                new() { Name = "snippet" }
            ]
        });

    var kernelFunctionResearchReportMaker = KernelFunctionFactory.CreateFromPrompt(
            "You are an expert web researcher report maker." +
            "Your goal is to make a report on a concrete topic and elaborate a report on the provided web findings . " +
            "You must produce a concise summary report on the different findings. " +
            "You will receive, for each search result:" +
            " URL - URL of the web search result." +
            " Name - Name of the web search result." +
            " Relevance - the relevance of the web search result respect the goal." +
            " Summary - A concise summary of the web search result including some of the main points." +
            "With this you will analyze those search results and provide a precise and concise report on the topic, made of:" +
            "1. A Title" +
            "2. A Summary" +
            "3. Relevant findings (news regarding announcements, findings, new releases or other)" +
            "4. Reference URLs" +
            "For the Title, provide a suitable title which is catchy, fits the topic of research, the summary and the points. " +
            "Ideally it should be engaging, compelling and sound a bit poetical." +
            "The summary should be a concise report on all the relevant topics provided by the search results. " +
            "The summary will be preceded by 'Summary:'." +
            "After the summary, the relevant points should be made of all the web search results  ordered by importance " +
            "and relevance ." +
            "If one or more web results are about the same topic please put them together, avoid repetition. " +
            "The relevant points must be preceded by 'Relevant points:'." +
            "After this, the Reference URLs must be provided, stating the reference URLs used to make the report, preceded by 'Reference URLs:'" +
            "For the summary, use 500 to 1000 words maximum." +
            "  You will use short, complete sentences, using active voice. " +
            "  Maximize detail and meaning. Focusing on the content." +
            "" +
            "The topic, goal of the report is the following:" +
            "---" +
            "{{$topicofresearch}}" +
            "---" +
            "Please provide a concise report for this topic using the provided search results as a base." +
            "And here are the search results:" +
            "---" +
            " {{$searchresults}}" +
            "---" +
            "Please provide a concise report for this topic using the provided search results as a base." +
            "BE SURE YOU LIST THE URLs OF THE SEARCH RESULTS IN THE REFERENCE URLS! DO NOT MISS THIS!!",
            functionName: "GenerateResearchReport",
            description: "Generate a concise research report given several web search results.",
            executionSettings: promptExecutionSettings);

    KernelPlugin webSearchAnalysisPlugin =
        KernelPluginFactory.CreateFromFunctions(
            "WebSearchAnalysis",
            "Helps Analyze web search results and make a report of them.",
            new[] {
                    kernelFunctionDetermineRelevance,
                    kernelFunctionSummarize,
                    kernelFunctionResearchReportMaker
            });

    kernel.Plugins.Add(webSearchAnalysisPlugin);
  }

}