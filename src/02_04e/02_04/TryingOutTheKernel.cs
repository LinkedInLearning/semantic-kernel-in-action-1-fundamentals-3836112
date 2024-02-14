using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.Core;

namespace _02_04;

public class TryingOutTheKernel
{
    public static async Task Execute()
    {
        var builder = Kernel.CreateBuilder();

        var modelDeploymentName = "Gpt4v32k";
        var azureOpenAIEndpoint = Environment.GetEnvironmentVariable("AzureOpenAI_Endpoint", EnvironmentVariableTarget.User);
        var azureOpenAIApiKey = Environment.GetEnvironmentVariable("AzureOpenAI_ApiKey", EnvironmentVariableTarget.User);

        builder.Services.AddAzureOpenAIChatCompletion(
            modelDeploymentName,
            azureOpenAIEndpoint,
            azureOpenAIApiKey,
            modelId: "gpt-4-32k"
        );

        builder.Plugins.AddFromPromptDirectory("./plugins/WriterPlugin");
        var kernel = builder.Build();

        var SKText = "The Semantic Kernel SDK has been born and is out to the world on December 19th, now all .NET developers are AI developers...";
        var poemResultSK =
            await kernel.InvokeAsync(
                "WriterPlugin",
                "ShortPoem",
                new() {
                    { "input", SKText }
                });

        Console.WriteLine(poemResultSK);

#pragma warning disable SKEXP0050        
        kernel.ImportPluginFromType<TimePlugin>();
        var today = await kernel.InvokeAsync("TimePlugin", "Today");
        Console.WriteLine(today);
        Console.ReadLine();
    }
}
