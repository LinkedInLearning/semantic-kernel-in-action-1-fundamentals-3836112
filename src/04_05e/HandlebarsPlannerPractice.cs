using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning.Handlebars;

namespace _04_05e;

public class HandlebarsPlannerPractice
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

        var kernelFunctionRespondAsTony = kernel.CreateFunctionFromPrompt(
            new PromptTemplateConfig()
            {
                Name = "RespondAsTony",
                Description = "Respond as if you were Tony Stark.",
                Template = @"After the user request/question, 
                    {{$input}},
                    Respond to the user question as if you were Tony Stark, Iron man. 
                    Respond to it as you were him, showing your personality",
                TemplateFormat = "semantic-kernel",
                InputVariables = [
                    new() { Name = "input" }
                ]
            });

        var kernelFunctionRespondAsThor = kernel.CreateFunctionFromPrompt(
            new PromptTemplateConfig()
            {
                Name = "RespondAsThor",
                Description = "Respond as if you were Thor, god of thunder.",
                Template = @"After the user request/question, 
                    {{$input}},
                    Respond to the user question as if you were Thor, the god of thunder, 
                    from the Avengers. Respond to it as you were him, showing your personality, 
                    humor and level of intelligence.",
                TemplateFormat = "semantic-kernel",
                InputVariables = [
                    new() { Name = "input" }
                ]
            });

        KernelPlugin superheroOpinionsPlugin =
            KernelPluginFactory.CreateFromFunctions(
                "SuperHeroTalk",
                "Responds to questions or statements as superheros do.",
                new[] {
                    kernelFunctionRespondAsTony,
                    kernelFunctionRespondAsThor
                      });
        kernel.Plugins.Add(superheroOpinionsPlugin);

        string planPrompt = "This is the user question to my superhero friends:" +
            "---" +
            "User Question: " +
            "I am being attacked by a thug which wants to rob me, what do the superheroes recommend me to do in my position? I am weak, no combat skills and not a good runner... " +
            "---" +
            "Please take this question as input for getting the superheroes opinions, Tony, Thor suggestions. Do not modify the input." +
            "Use the plugin SuperHeroTalk to get the suggestions and opinions of the superheroes." +
            "In addition state each superheros opinion on each other stated opinions." +
            "Put the Hero responses preceded with SUPERHERO SUGGESTIONS: and inside that preceed with Tony: and Thor: for clarity." +
            "Perform this with the following steps: " +
            "1. Get the suggestions from each the superheroes." +
            "2. Get the opinions of each superhero on the other superheroes suggestions." +
            "3. Return the results in the format: " +
            "SUPERHERO SUGGESTIONS: Tony: <suggestion> Thor: <suggestion> " +
            "SUPERHERO OPINIONS: Tony: <opinion on Thor> Thor: <opinion on Tony> " +
            "IMPORTANT: on the plan ensure that the user question is asigned to a variable and used as input. Do not modify the user question input.";

        var planner = new HandlebarsPlanner(new HandlebarsPlannerOptions() { AllowLoops = false });
        var plan = await planner.CreatePlanAsync(kernel, planPrompt);

        // Print the plan to the console
        Console.WriteLine($"Plan: {plan}");

        // Execute the plan
        var planExecutionResult = await plan.InvokeAsync(kernel);

        // Print the result to the console
        Console.WriteLine($"Plan Execution Result: {planExecutionResult}");

        Console.ReadLine();
    }
}