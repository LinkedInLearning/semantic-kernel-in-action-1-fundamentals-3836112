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
    // artDirector agent => Reviews ideas, provides feedback and gives the FINAL APPROVAL
    var artDirectorAgent =
        Track(
            await new AgentBuilder()
                .WithOpenAIChatCompletion(openAIFunctionEnabledModelId, openAIApiKey)
                .WithInstructions("You are an art director who has opinions about copywriting born of a love for David Ogilvy. The goal is to determine is the given copy is acceptable to print, even if it isn't perfect.  If not, provide insight on how to refine suggested copy without example.  Always respond to the most recent message by evaluating and providing critique without example.  Always repeat the copy at the beginning.  If copy is acceptable and meets your criteria, say: PRINT IT.")
                .WithName("Art Director")
                .WithDescription("Art Director")
                .BuildAsync());

    // copyWriterAgent => generates ideas
    var copyWriterAgent =
        Track(
            await new AgentBuilder()
                .WithOpenAIChatCompletion(openAIFunctionEnabledModelId, openAIApiKey)
                .WithInstructions("You are a copywriter with ten years of experience and are known for brevity and a dry humor. You're laser focused on the goal at hand. Don't waste time with chit chat. The goal is to refine and decide on the single best copy as an expert in the field.  Consider suggestions when refining an idea.")
                .WithName("Copywriter")
                .WithDescription("Copywriter")
                .BuildAsync());

    // Create coordinator agent to oversee collaboration
    var coordinatorAgent =
        Track(
            await new AgentBuilder()
                .WithOpenAIChatCompletion(openAIFunctionEnabledModelId, openAIApiKey)
                .WithInstructions("Reply the provided concept and have the copy-writer generate an marketing idea (copy).  Then have the art-director reply to the copy-writer with a review of the copy.  Always include the source copy in any message.  Always include the art-director comments when interacting with the copy-writer.  Coordinate the repeated replies between the copy-writer and art-director until the art-director approves the copy.")
                .WithPlugin(copyWriterAgent.AsPlugin())
                .WithPlugin(artDirectorAgent.AsPlugin())
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
        // Add the user message
        var messageUser = await _agentsThread.AddUserMessageAsync(ideaToEllaborate);
        DisplayMessage(messageUser);

        bool isComplete = false;
        do
        {
          // Initiate copy-writer input
          var agentMessages = await _agentsThread.InvokeAsync(copyWriterAgent).ToArrayAsync();
          DisplayMessages(agentMessages, copyWriterAgent);

          // Initiate art-director input
          agentMessages = await _agentsThread.InvokeAsync(artDirectorAgent).ToArrayAsync();
          DisplayMessages(agentMessages, artDirectorAgent);

          // Evaluate if goal is met.
          if (agentMessages.First().Content.Contains("PRINT IT", StringComparison.OrdinalIgnoreCase))
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