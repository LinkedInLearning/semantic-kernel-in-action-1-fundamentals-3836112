using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
namespace _03_10e;

public class HandlebarsChainingFunctions
{
  public static async Task Execute()
  {
    var modelDeploymentName = "Gpt4v32k";
    var azureOpenAIEndpoint = Environment.GetEnvironmentVariable("AZUREOPENAI_ENDPOINT");
    var azureOpenAIApiKey = Environment.GetEnvironmentVariable("AZUREOPENAI_APIKEY");

    var builder = Kernel.CreateBuilder();
    builder.Services.AddAzureOpenAIChatCompletion(
        modelDeploymentName,
        azureOpenAIEndpoint,
        azureOpenAIApiKey,
        modelId: "gpt-4-32k"
    );
    var kernel = builder.Build();

    var pluginDirectory = Path.Combine(
        System.IO.Directory.GetCurrentDirectory(),
        "plugins",
        "RoleTalk");
    kernel.ImportPluginFromPromptDirectory(pluginDirectory);

    string question = "What's the best way to deal with a city-wide power outage?";
    var chainingFunctionsWithHandlebarsFunction = kernel.CreateFunctionFromPrompt(
        new()
        {
          Template = @"
                {{set ""responseAsPoliceman"" (RoleTalk-RespondAsPoliceman input) }}
                {{set ""responseAsScientific"" (RoleTalk-RespondAsScientist input) }}
                {{set ""opinionFromScientificToPoliceman"" (RoleTalk-RespondAsScientist responseAsPoliceman) }}

                {{!-- Example of concatenating text and variables to finally output it with json --}}
                {{set ""finalOutput"" (concat ""Policeman: "" responseAsPoliceman "" Scientific: "" responseAsScientific  "" Scientific to Policeman: "" opinionFromScientificToPoliceman)}}
                
                Output the following responses as is, do not modify anything:
                {{json finalOutput}}
                ",
          TemplateFormat = "handlebars"
        },
        new HandlebarsPromptTemplateFactory()
    );

    var resp =
        await kernel.InvokeAsync(
            chainingFunctionsWithHandlebarsFunction,
            new() {
                    { "input", question }
            });

    Console.WriteLine($"Result:  {resp}");

    Console.ReadLine();
  }
}