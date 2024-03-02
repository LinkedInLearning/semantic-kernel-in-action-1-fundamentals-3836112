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
                .WithDescription("Answer questions about the menu by using the menuPlugin tool.")
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
      "What is on today's menu? ",
      "how much does the Eye Steak with veggies cost? ",
      "Can you talk like pirate?",
      "Thank you",
    };

    // note that threads aren't attached to specific agents
    _agentsThread = await toolAgent.NewThreadAsync();

    try
    {
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
    if (_agentsThread != null)
    {
      _agentsThread.DeleteAsync();
      _agentsThread = null;
    }

    if (_agents.Any())
    {
      await Task.WhenAll(_agents.Select(agent => agent.DeleteAsync()));
      _agents.Clear();
    }
  }


}