using Microsoft.Extensions.Hosting;
using Serina.Semantic.Ai.Pipelines.Filters;
using Serina.Semantic.Ai.Pipelines.SemanticKernel.Reducers;
using Serina.Semantic.Ai.Pipelines.SemanticKernel.ServiceSelectors;
using Serina.Semantic.Ai.Pipelines.SemanticKernel;
using Serina.Semantic.Ai.Pipelines.Steps.Chat;
using Serina.Semantic.Ai.Pipelines.Steps;
using Serina.Semantic.Ai.Pipelines.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serina.Semantic.Ai.Pipelines.Interfaces;
using Serina.Semantic.Ai.Pipelines.Streams;
using Serina.Semantic.Ai.Pipelines.Models;
using Serina.Semantic.Ai.Pipelines.ValueObject;
using System.Diagnostics;
using Azure;

namespace LLMPipelineSamples.Services
{
    internal class StreamingChatService : BackgroundService
    {
        private const string ModelName = "dolphin-mixtral";
        public const string ollamaHost = "http://192.168.88.104:11434";
        
        private readonly IPipelineStream Stream = new PipelineChannelStream();


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Streaming chat service example");

            BuildPipeline();

            var pipe = PipelineRegistry.Get("SimpleChat");


            var contextId = Guid.NewGuid();

            var history = new List<RequestMessage>()
            {
                 new RequestMessage("You are helpfull assistant answer polite be short when answering.", MessageRole.System, contextId)
            };

            while (true)
            {
                var input = Console.ReadLine();

                history.Add(new RequestMessage(input, MessageRole.User, contextId));


                var context = new PipelineContext
                {
                    RequestMessage = new RequestMessage("", MessageRole.User, contextId, History: history.ToArray()),
                    Response = new MessageResponse()
                };

                 _ = Task.Run(() =>   pipe.ExecuteStepAsync(context, default));

                 
                var rsp = new StringBuilder();
                 
                // Assert
                await foreach (var response in Stream.ReadResponses(context.Id))
                {
                    Console.Write(response.Content);
                    Debug.Write(response.Content);
                    rsp.Append(response.Content);
                }

                history.Add(new RequestMessage(rsp.ToString(), MessageRole.Bot, contextId));
            }
             
        }

        private void BuildPipeline()
        {
            PipelineBuilder.New()
                  .SetNext(new StreamingChatStep(Stream))
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

                                    .WithName("SimpleChat")
                                    .Build();
        }


    }
}
