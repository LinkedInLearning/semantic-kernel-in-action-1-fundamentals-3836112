using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
namespace _04_03e;


// Note: do not update to 1.5 until the following issue is resolved:
// https://github.com/microsoft/semantic-kernel/issues/5264
public class FunctionCalling
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

    KernelFunction kernelFunctionRespondAsTony =
        KernelFunctionFactory.CreateFromPrompt(
            "Respond to the user question as if you were Tony Stark, Iron man. Respond to it as you were him, showing your personality",
            functionName: "RespondAsTony",
            description: "Responds to a question as the superhero Tony Stark, Iron Man.");

    KernelFunction kernelFunctionRespondAsThor =
        KernelFunctionFactory.CreateFromPrompt(
            "Respond to the user question as if you were Thor, the god of thunder, from the Avengers. Respond to it as you were him, showing your personality, humor and level of intelligence.",
            functionName: "RespondAsThor",
            description: "Responds to a question as the superhero Thor, the god of thunder.");

    KernelPlugin superheroOpinionsPlugin =
        KernelPluginFactory.CreateFromFunctions(
            "SuperHeroTalk",
            "Responds to questions or statements as superheros do.",
            new[] {
                    kernelFunctionRespondAsTony,
                    kernelFunctionRespondAsThor
                  });
    kernel.Plugins.Add(superheroOpinionsPlugin);
    kernel.Plugins.AddFromType<WhatDateIsIt>();

    string userPrompt = "I just woke up and found myself in the middle of nowhere, " +
        "do you know what date is it? and what would Tony Stark do in my place?";

    OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
    {
      ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
    };

    var result = await kernel.InvokePromptAsync(
        userPrompt,
        new(openAIPromptExecutionSettings));

    Console.WriteLine($"Result: {result}");
    Console.ReadLine();
  }
}