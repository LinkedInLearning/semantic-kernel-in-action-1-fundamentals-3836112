using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Experimental.Agents;

namespace _05_07e;

public class AgentCollaborationPractice
{
#pragma warning disable SKEXP0101  
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

    // Create agents
    // Marketing Editor agent => Reviews slogan, provides feedback and gives the FINAL APPROVAL
    var editorAgent =
        Track(
            await new AgentBuilder()
                .WithOpenAIChatCompletion(openAIFunctionEnabledModelId, openAIApiKey)
                .WithInstructions("You are a professional editor with a profound expertise in crafting and refining content for marketing. You aredeeply passionate about the intersection of technology and storytelling and love when words ryhme together. Your goal is to determine if a marketing slogan is acceptable, even if it isn't perfect.  If not, provide constructive insights on how to improve the slogan without providing an example.  Respond to the most recent message by evaluating and providing feedback without giving any example.  Always repeat the slogan at the beginning.  If the slogan is is acceptable and meets your criteria, say: I APPROVE.")
                .WithName("Marketing Editor")
                .WithDescription("Marketing Editor")
                .BuildAsync());

    // Marketing Writer Agent => generates ideas
    var writerAgent =
        Track(
            await new AgentBuilder()
                .WithOpenAIChatCompletion(openAIFunctionEnabledModelId, openAIApiKey)
                .WithInstructions("You are a marketing writer with some years of experience, you like efficiency of words and sarcasm. You like to deliver greatness and do your outmost always. Your goal is given an idea description to provide a Marketing slogan. If feedback is provided, take it into consideration to improve the Slogan.")
                .WithName("Marketing Writer")
                .WithDescription("Marketing Writer")
                .BuildAsync());

    // Create coordinator agent to oversee collaboration
    var coordinatorAgent =
        Track(
            await new AgentBuilder()
                .WithOpenAIChatCompletion(openAIFunctionEnabledModelId, openAIApiKey)
                .WithInstructions("Reply the provided Slogan and have the Marketing writer generate a Slogan. Then have the Marketing Editor review and reply to the marketing writer with feedback on the Slogan. Always include the source Slogan in all the messages.  Always include the Marketing Editor feedback when interacting with the Marketing Writer. Coordinate the flow of replies between the marketing writer and the marketing editor until the Marketing Editor approves the Slogan.")
                .WithPlugin(writerAgent.AsPlugin())
                .WithPlugin(editorAgent.AsPlugin())
                .BuildAsync());

    // note that threads aren't attached to specific agents
    _agentsThread = await coordinatorAgent.NewThreadAsync();

    string ideaToEllaborate = "concept: AI Agents that can write twitter and LinkedIn" +
        " messages and blog posts with the style of an author.";

    try
    {
      // We delegate the messages to the coordinatorAgent who takes care of the coordination and delegates & oversees their subordinate plugin agents
      bool useCoordinator = false;

      if (useCoordinator)
      {
        var responseMessages =
            await _agentsThread.InvokeAsync(
                coordinatorAgent,
                ideaToEllaborate)
                .ToArrayAsync();
        DisplayMessages(responseMessages, coordinatorAgent);
      }
      else
      {
        var messageUser = await _agentsThread.AddUserMessageAsync(ideaToEllaborate);
        DisplayMessage(messageUser);

        bool isComplete = false;
        do
        {
          var agentMessages = await _agentsThread.InvokeAsync(writerAgent).ToArrayAsync();
          DisplayMessages(agentMessages, writerAgent);

          agentMessages = await _agentsThread.InvokeAsync(editorAgent).ToArrayAsync();
          DisplayMessages(agentMessages, editorAgent);

          if (agentMessages.First().Content.Contains("I APPROVE", StringComparison.OrdinalIgnoreCase))
          {
            isComplete = true;
          }
        }
        while (!isComplete);
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
    Console.WriteLine("Cleaned up agents and threads.");
  }


}