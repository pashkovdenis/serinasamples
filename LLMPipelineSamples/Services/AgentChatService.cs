using Microsoft.Extensions.Hosting;
using Serina.Semantic.Ai.Pipelines.Filters;
using Serina.Semantic.Ai.Pipelines.Interfaces;
using Serina.Semantic.Ai.Pipelines.SemanticKernel.Reducers;
using Serina.Semantic.Ai.Pipelines.SemanticKernel.ServiceSelectors;
using Serina.Semantic.Ai.Pipelines.SemanticKernel;
using Serina.Semantic.Ai.Pipelines.Steps.Chat;
using Serina.Semantic.Ai.Pipelines.Streams;
using Serina.Semantic.Ai.Pipelines.Utils;
using Microsoft.SemanticKernel.Agents;
using static Serina.Semantic.Ai.Pipelines.Steps.Chat.AgentStreamingChat;
using Serina.Semantic.Ai.Pipelines.Models;
using System.Diagnostics;
using System.Text;
using Serina.Semantic.Ai.Pipelines.ValueObject;

namespace LLMPipelineSamples.Services
{
    internal class AgentChatService : BackgroundService
    {
        private const string ModelName = "dolphin-mixtral";
        public const string ollamaHost = "http://192.168.88.104:11434";

        private readonly IPipelineStream Stream = new PipelineChannelStream();


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Agent chat service example");

            BuildPipeline();

            var pipe = PipelineRegistry.Get("SimpleChat");


            var contextId = Guid.NewGuid();

         

            while (true)
            {
                var input = Console.ReadLine();

              

                var context = new PipelineContext
                {
                    RequestMessage = new RequestMessage(input, MessageRole.User, contextId ),
                    Response = new MessageResponse()
                };

                _ = Task.Run(() => pipe.ExecuteStepAsync(context, default));

 
                // Assert
                await foreach (var response in Stream.ReadResponses(context.Id))
                {
                    Console.Write(response.Content);
                    Debug.Write(response.Content); 
                }

             
            }
        }






        private void BuildPipeline()
        {
            const string ReviewerName = "Reviewer";
            const string TerminationToken = "yes";
            const string WriterName = "Writer";


            var cfg = new AgentChatConfiguration
            {

                HistoryReducerLimit = 1,
                ResetOnInput = false,
                StrategySelectionFunction = AgentGroupChat.CreatePromptFunctionForStrategy(
                    $$$"""
                Examine the provided RESPONSE and choose the next participant.
                State only the name of the chosen participant without explanation.
                Never choose the participant named in the RESPONSE.

                Choose only from these participants:
                - {{{ReviewerName}}}
                - {{{WriterName}}}

                Always follow these rules when choosing the next participant:
                - If RESPONSE is user input, it is {{{ReviewerName}}}'s turn.
                - If RESPONSE is by {{{ReviewerName}}}, it is {{{WriterName}}}'s turn.
                - If RESPONSE is by {{{WriterName}}}, it is {{{ReviewerName}}}'s turn.

                RESPONSE:
                
                {{$lastmessage}}

                """,
                    safeParameterNames: "lastmessage"),
                TerminationFunction = AgentGroupChat.CreatePromptFunctionForStrategy(
                    $$$"""
                Examine the RESPONSE and determine whether the content has been deemed satisfactory.
                In Chat Both Reviwer and writer needs to respond
                If content is satisfactory, respond with a single word without explanation: {{{TerminationToken}}}.
                If specific suggestions are being provided, it is not satisfactory.
                If no correction is suggested, it is satisfactory.

                RESPONSE:
                {{$lastmessage}}
                """,
                    safeParameterNames: "lastmessage")

            };

            cfg.AgentDefinition.Add(ReviewerName, @"Your responsiblity is to review and identify how to improve user provided content.
                    If the user has providing input or direction for content already provided, specify how to address this input.
                    Never directly perform the correction or provide example.
                    Once the content has been updated in a subsequent response, you will review the content again until satisfactory.
                    Always copy satisfactory content to the clipboard using available tools and inform user.

                    RULES:
                    - Only identify suggestions that are specific and actionable.
                    - Verify previous suggestions have been addressed.
                    - Never repeat previous suggestions. ");

            cfg.AgentDefinition.Add(WriterName, @"Your sole responsiblity is to rewrite content according to review suggestions.
                    - Always apply all review direction.
                    - Always revise the content in its entirety without explanation.
                    - Never address the user. ");


            var step = new AgentStreamingChat(Stream);


            PipelineBuilder.New()
                  .SetNext(step)
                                    .WithKernel(new SemanticKernelOptions
                                    {
                                        Models = new List<SemanticModelOption>
                                        {
                                         new SemanticModelOption
                                         {
                                               Endpoint = ollamaHost,
                                               Name = ModelName,
                                               Key = "123456",
                                               EngineType = 1
                                         }
                                        }
                                    }, serviceSelector: new RandomServiceSelector())
                                    .AddFilter(new ClearTextFilter())
                                    .WithModelSelectStrategy(new RandomServiceSelector())
                                    .AddFilter(new TextChunkerFilter())
                                    .AddReducer(new PairedSlidingWindowReducer())
                                     .AddConfig(cfg)
                                    .AttachKernel()

                                    .WithName("SimpleChat")
                                    .Build();
        }



    }
}
