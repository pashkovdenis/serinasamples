

reference: https://github.com/pashkovdenis/serina-pipeline

Build an AI Chat Pipeline Using Semantic Kernel and Serina Pipelines
In this article, we’ll walk through building a flexible AI pipeline using Semantic Kernel and the Serina.Semantic.Ai.Pipelines package. We’ll explore various pipeline configurations to power a local chat app with summarization, streaming responses, and even agent-based multi-role conversations.

What is an AI Pipeline?
An AI pipeline is a configurable series of steps, each containing its own logic, LLM models, plugins, filters, reducers, and more. These steps can be used to build complex processing flows for both conversational and non-conversational tasks.

By chaining and customizing these steps, you can create intelligent, context-aware bots or agents that process input in a smart and flexible way.

What is Semantic Kernel?
Semantic Kernel is a lightweight, open-source SDK developed by Microsoft. It allows developers to easily integrate large language models (LLMs) like GPT, Mistral, or LLaMA into .NET applications. You can build agents, plug in your own logic, and define workflows for various AI use cases.

What We'll Build
Using Semantic Kernel and the Serina.Semantic.Ai.Pipelines NuGet package, we’ll build:

A simple local chat app

A streaming chatbot with real-time response rendering

An agent-based conversation flow with roles like Writer and Reviewer

A summarization pipeline step for summarizing the whole conversation

We'll use Ollama for local LLM models like LLaMA3 or Mixtral. So make sure you have the Ollama server installed and running locally.

Getting Started
First, create a new .NET Console App and add the required NuGet package:

xml 
<PackageReference Include="Serina.Semantic.Ai.Pipelines" Version="1.0.0" />
Basic Chat Example with SimpleChatStep
Let’s begin with a simple chat example that reads user input from the console and responds using a local LLM.

We'll use the built-in SimpleChatStep from the pipeline library.

csharp 
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
                Key = "123456", // Ollama doesn't require it but the field is mandatory
                EngineType = 1
            }
        }
    }, serviceSelector: new RandomServiceSelector())
    .AddFilter(new ClearTextFilter())
    .AddFilter(new TextChunkerFilter())
    .AddReducer(new PairedSlidingWindowReducer())
    .WithModelSelectStrategy(new RandomServiceSelector())
    .WithName("SimpleChat")
    .AttachKernel()
    .Build();
Explanation:
SimpleChatStep: handles simple request/response logic.

Filters like ClearTextFilter and TextChunkerFilter clean and chunk messages.

PairedSlidingWindowReducer: reduces history by maintaining only recent relevant chunks.

.WithName("SimpleChat"): adds the pipeline to a registry for easy access later.

You can now retrieve the pipeline by name:

csharp
Копировать
Редактировать
var pipeline = PipelineRegistry.Get("SimpleChat");
Adding Summarization with DialogSummaryStep
Let’s now extend the chat by summarizing the full conversation.

We’ll chain another step: DialogSummaryStep.

```csharp
PipelineBuilder.New()
    .SetNext(new SimpleChatStep())
    .WithKernel(...) // same as before
    .AddFilter(new ClearTextFilter())
    .AddFilter(new TextChunkerFilter())
    .AddReducer(new PairedSlidingWindowReducer())
    .WithModelSelectStrategy(new RandomServiceSelector())
    .AttachKernel()

    .SetNext(new DialogSummaryStep())
    .WithKernel(...) // another kernel instance
    .AttachKernel()
    
    .WithName("SimpleChat")
    .Build();

```
    
Now, when you run the app, it’ll return the summary of the entire chat as part of the response.

Streaming Chat with StreamingChatStep
Now let’s upgrade the user experience with real-time response rendering using streaming.

```csharp
 
var stream = new PipelineChannelStream();

PipelineBuilder.New()
    .SetNext(new StreamingChatStep(stream))
    .WithKernel(...)
    .AddFilter(new ClearTextFilter())
    .AddFilter(new TextChunkerFilter())
    .AddReducer(new PairedSlidingWindowReducer())
    .AttachKernel()
    .WithName("StreamingChat")
    .Build();

// Reading the response:
await foreach (var message in stream.ReadResponses(context.Id))
{
    Console.Write(message);
}
Instead of waiting for the whole message, you’ll now see tokens arriving in real time.

Multi-Agent Chat with AgentStreamingChat
Here's where it gets more exciting. Let’s build a multi-agent chat system with two roles:

Writer: rewrites content

Reviewer: gives feedback

The conversation goes back and forth between these two until the content is approved.

```csharp
 
var config = new AgentChatConfiguration
{
    HistoryReducerLimit = 1,
    ResetOnInput = false,
    StrategySelectionFunction = AgentGroupChat.CreatePromptFunctionForStrategy( ... ),
    TerminationFunction = AgentGroupChat.CreatePromptFunctionForStrategy( ... )
};

config.AgentDefinition.Add("Reviewer", @"Your responsibility is to review...");
config.AgentDefinition.Add("Writer", @"Your responsibility is to rewrite...");

PipelineBuilder.New()
    .SetNext(new AgentStreamingChat(stream))
    .WithKernel(...)
    .AddFilter(new ClearTextFilter())
    .AddFilter(new TextChunkerFilter())
    .AddReducer(new PairedSlidingWindowReducer())
    .AddConfig(config)
    .AttachKernel()
    .WithName("AgentChat")
    .Build();

```

The roles interact with each other based on the logic in the config. The bot will automatically switch between writer and reviewer based on the strategy functions.

This approach is powerful for content generation, editing workflows, or task delegation within bots.

Recap
Using the Serina.Semantic.Ai.Pipelines package with Semantic Kernel, we explored:

✅ Building a simple chat app
✅ Adding filters and reducers
✅ Implementing streaming chat
✅ Creating summarization steps
✅ Configuring multi-agent workflows with roles

All of this works locally with Ollama and open-source models.

Final Thoughts
This pipeline architecture is flexible, composable, and powerful. You can easily add custom steps, plug-ins, cache layers, filters, or reducers to tailor your pipeline for any AI task — from chat to document processing, or even AI assistants.

Whether you're building a bot for customer support, a writing assistant, or a research agent, this framework provides a strong foundation.
