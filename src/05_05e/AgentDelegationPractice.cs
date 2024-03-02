using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Experimental.Agents;

namespace _05_05e;

public class AgentDelegationPractice
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

    // Preparation for agents creation
    var menuPlugin = KernelPluginFactory.CreateFromType<MenuPlugin>();
    var pathToParrotAgent = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Agents", "ParrotAgent.yaml");
    var pathToToolAgent = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Agents", "ToolAgent.yaml");

    // Create agents
    var menuAgent =
        Track(
            await new AgentBuilder()
                .WithOpenAIChatCompletion(openAIFunctionEnabledModelId, openAIApiKey)
                .FromTemplatePath(pathToToolAgent)
                .WithDescription("Answer questions about how the menu uses the tool.")
                .WithPlugin(menuPlugin)
                .BuildAsync());

    var parrotAgent =
        Track(
            await new AgentBuilder()
                .WithOpenAIChatCompletion(openAIFunctionEnabledModelId, openAIApiKey)
                .FromTemplatePath(pathToParrotAgent)
                .BuildAsync());

    var toolAgent =
        Track(
            await new AgentBuilder()
                .WithOpenAIChatCompletion(openAIFunctionEnabledModelId, openAIApiKey)
                .FromTemplatePath(pathToToolAgent)
                .WithPlugin(parrotAgent.AsPlugin())
                .WithPlugin(menuAgent.AsPlugin())
                .BuildAsync());

    var messages = new string[]
    {
      "What's on the menu? ",
      "Can you talk like pirate?",
      "Thank you",
    };

    // note that threads aren't attached to specific agents
    _agentsThread = await toolAgent.NewThreadAsync();
    Console.WriteLine("The message thread 🧵 is ready!");

    try
    {
      // We delegate the messages to the toolAgent who delegates to its subordinate plugin agents
      foreach (var message in messages)
      {
        var responseMessages =
          await _agentsThread.InvokeAsync(toolAgent, message).ToArrayAsync();

        DisplayMessages(responseMessages, toolAgent);
      }
    }
    finally
    {
      await CleanUpAsync();
    }

    Console.ReadLine();
  }

  private IAgent Track(IAgent agent)
  {
    _agents.Add(agent);

    return agent;
  }

  private void DisplayMessages(IEnumerable<IChatMessage> messages, IAgent? agent = null)
  {
    foreach (var message in messages)
    {
      DisplayMessage(message, agent);
    }
  }

  private void DisplayMessage(IChatMessage message, IAgent? agent = null)
  {
    Console.WriteLine($"[{message.Id}]");
    if (agent != null)
    {
      Console.WriteLine($"# {message.Role}: ({agent.Name}) {message.Content}");
    }
    else
    {
      Console.WriteLine($"# {message.Role}: {message.Content}");
    }
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