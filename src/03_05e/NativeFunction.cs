using Microsoft.SemanticKernel;

namespace _03_05e;

public class NativeFunction
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
    builder.Plugins.AddFromType<MyMathPlugin>();
    var kernel = builder.Build();
    

    // Also able to add it after the kernel has been built
    // kernel.ImportPluginFromType<MyMathPlugin>();
    var NumberToSquareRoot = 81;
    var squareRootResult =
        await kernel.InvokeAsync(
          "MyMathPlugin",
          "Sqrt",
          new() {
            { "number1", NumberToSquareRoot }
          });

    Console.WriteLine($"The Square root of {NumberToSquareRoot} is:  {squareRootResult}");

    Console.ReadLine();
  }
}