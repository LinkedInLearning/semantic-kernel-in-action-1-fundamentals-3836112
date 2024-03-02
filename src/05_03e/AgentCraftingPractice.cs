using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Experimental.Agents;

namespace _05_03e;

public class AgentCraftingPractice
{
#pragma warning disable SKEXP0101  
  // Track agents for clean-up
  readonly List<IAgent> _agents = new();

  IAgentThread? _agentsThread = null;

  public async Task Execute()
  {
    var openAIFunctionEnabledModelId = "gpt-4-turbo-preview";
    var openAIApiKey = Environment.GetEnvironmentVariable("OPENAI_APIKEY");
    var builder = Kernel.CreateBuilder();
    builder.Services.AddOpenAIChatCompletion(
        openAIFunctionEnabledModelId,
        openAIApiKey);
    var kernel = builder.Build();

    // create agent in code
    var codeAgent = await new AgentBuilder()
                    .WithOpenAIChatCompletion(openAIFunctionEnabledModelId, openAIApiKey)
                    .WithInstructions("Repeat the user message in the voice of a pirate " +
                    "and then end with parrot sounds.")
                    .WithName("CodeParrot")
                    .WithDescription("A fun chat bot that repeats the user message in the" +
                    " voice of a pirate.")
                    .BuildAsync();
    _agents.Add(codeAgent);

    // Create agent from file
    var pathToPlugin = Path.Combine(Directory.GetCurrentDirectory(), "Agents", "ParrotAgent.yaml");
    string agentDefinition = File.ReadAllText(pathToPlugin);
    var fileAgent = await new AgentBuilder()
        .WithOpenAIChatCompletion(openAIFunctionEnabledModelId, openAIApiKey)
        .FromTemplatePath(pathToPlugin)
        .BuildAsync();
    _agents.Add(fileAgent);

    try
    {
      // Invoke agent plugin.
      var response =
          await fileAgent.AsPlugin().InvokeAsync(
              "Practice makes perfect.",
              new KernelArguments { { "count", 2 } }
          );

      // Display result.
      Console.WriteLine(response ?? $"No response from agent: {fileAgent.Id}");
    }
    finally
    {
      // Clean-up (storage costs $)
      await CleanUpAsync();
      await fileAgent.DeleteAsync();
      await codeAgent.DeleteAsync();
    }

    Console.ReadLine();
  }

  private async Task CleanUpAsync()
  {
    Console.WriteLine("🧽 Cleaning up ...");

    if (_agentsThread != null)
    {
      Console.WriteLine("Thread going away ...");
      _agentsThread.DeleteAsync();
      _agentsThread = null;
    }

    if (_agents.Any())
    {
      Console.WriteLine("Agents going away ...");
      await Task.WhenAll(_agents.Select(agent => agent.DeleteAsync()));
      _agents.Clear();
    }
  }

}