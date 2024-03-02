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

        var kernelFunctionRespondAsScientific = kernel.CreateFunctionFromPrompt(
            new PromptTemplateConfig()
            {
                Name = "RespondAsScientific",
                Description = "Respond as if you were a Scientific.",
                Template = @"After the user request/question, 
                    {{$input}},
                    Respond to the user question as if you were a Scientific. 
                    Respond to it as you were him, showing your personality",
                TemplateFormat = "semantic-kernel",
                InputVariables = [
                    new() { Name = "input" }
                ]
            });

        var kernelFunctionRespondAsPoliceman = kernel.CreateFunctionFromPrompt(
            new PromptTemplateConfig()
            {
                Name = "RespondAsPoliceman",
                Description = "Respond as if you were a Policeman.",
                Template = @"After the user request/question, 
                    {{$input}},
                    Respond to the user question as if you were a Policeman, showing your personality, 
                    humor and level of intelligence.",
                TemplateFormat = "semantic-kernel",
                InputVariables = [
                    new() { Name = "input" }
                ]
            });

        KernelPlugin roleOpinionsPlugin =
            KernelPluginFactory.CreateFromFunctions(
                "roleTalk",
                "Responds to questions or statements asuming different roles.",
                new[] {
                    kernelFunctionRespondAsScientific,
                    kernelFunctionRespondAsPoliceman
                      });
        kernel.Plugins.Add(roleOpinionsPlugin);

        string planPrompt = "This is the user question to my expert friends:" +
            "---" +
            "User Question: " +
            "I am being attacked by a thug which wants to rob me, what do the experts recommend me to do in my position? I am weak, no combat skills and not a good runner... " +
            "---" +
            "Please take this question as input for getting the expert opinions, Mr. Policeman, Scientist suggestions. Do not modify the input." +
            "Use the plugin roleTalk to get the suggestions and opinions of the experts." +
            "In addition state each expert opinion on each other stated opinions." +
            "Put the expert responses preceded with EXPERT SUGGESTIONS: and inside that preceed with Policeman: and Scientist: for clarity." +
            "Perform this with the following steps: " +
            "1. Get the suggestions from each the experts." +
            "2. Get the opinions of each expert on the other expert suggestions." +
            "3. Return the results in the format: " +
            "Expert SUGGESTIONS: Policeman: <suggestion> Scientist: <suggestion> " +
            "OPINIONS: Policeman: <opinion on Scientist> Scientist: <opinion on Policeman> " +
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