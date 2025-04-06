using Microsoft.Extensions.Hosting;
using Serina.Semantic.Ai.Pipelines.Filters;
using Serina.Semantic.Ai.Pipelines.Models;
using Serina.Semantic.Ai.Pipelines.SemanticKernel;
using Serina.Semantic.Ai.Pipelines.SemanticKernel.Reducers;
using Serina.Semantic.Ai.Pipelines.SemanticKernel.ServiceSelectors;
using Serina.Semantic.Ai.Pipelines.Steps;
using Serina.Semantic.Ai.Pipelines.Steps.Chat;
using Serina.Semantic.Ai.Pipelines.Utils;
using Serina.Semantic.Ai.Pipelines.ValueObject;

namespace LLMPipelineSamples.Services
{
    internal class SimpleChatService : BackgroundService
    {

        private const string ModelName = "dolphin-mixtral";
        public const string ollamaHost = "http://192.168.88.104:11434";

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Simple chat service example");

            BuildPipeline();

            var pipe = PipelineRegistry.Get("SimpleChat");

            var history = new List<RequestMessage>()
            {
                 new RequestMessage("You are helpfull assistant answer polite be short when answering.", MessageRole.System, Guid.NewGuid())
            };


            while (true)
            {
                var input = Console.ReadLine();

                history.Add(new RequestMessage(input, MessageRole.User, Guid.NewGuid()));


                var context = new PipelineContext
                {
                    RequestMessage = new RequestMessage("", MessageRole.User, Guid.NewGuid(), History: history.ToArray()),
                    Response = new MessageResponse()
                };

                await pipe.ExecuteStepAsync(context, default);

                var response = context.Response.Content;

                history.Add(new RequestMessage(response, MessageRole.Bot, Guid.NewGuid()));

                Console.WriteLine("Bot:" + response);
            }

        }

        private void BuildPipeline()
        {
            PipelineBuilder.New()
                  .SetNext(new SimpleChatStep())
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


                                    .AttachKernel()

                                  // add summarization step 

                                  .SetNext(new DialogSummaryStep())
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
                                    .AttachKernel()

                                    .WithName("SimpleChat")
                                    .Build();
        }

    }
}
