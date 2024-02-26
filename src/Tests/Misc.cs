using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Assistants;
using Microsoft.Extensions.Configuration;
using Moq;
using Newtonsoft.Json.Linq;

namespace Devlooped.Assistants;

public class Misc(ITestOutputHelper output)
{
    static readonly IConfiguration config = new ConfigurationBuilder()
        .AddUserSecrets("720fb04e-8a57-480c-bfc8-a24ed9dfd68c")
        .Build();

    record Entity(string Id, IDictionary<string, string> Metadata);

    record Thread(string Id, IDictionary<string, string> Metadata) : Entity(Id, Metadata);

    interface IThreadManager
    {
        Task<Thread> CreateAsync();
        Task AppendAsync(Content content);
    }

    record Content(string Type);
    record UserContent(string Message) : Content("User");
    record FileContent(string Path, string Type) : Content(Type)
    {
        protected FileContent(string Path) : this(Path, "File") { }
    }

    record ImageContent(string Path) : FileContent(Path, "Image");

    static class ThreadManagerExtensions
    {
        //public async Task AppendAsync(string message) => 
    }

    [Fact]
    public async Task Ask()
    {
        var manager = Mock.Of<IThreadManager>();

        var thread = await manager.CreateAsync();

        //manager.Append("hello");

    }


    [Fact]
    public async Task RunAsync()
    {
        var client = new Azure.AI.OpenAI.Assistants.AssistantsClient(config["OpenAI:Key"] ??
            throw new InvalidOperationException("Please provide the OpenAI API key. See readme for more information."));
        
        // client.UploadFile("", OpenAIFilePurpose.)

        var assistant = (await client.GetAssistantsAsync()).Value
            .FirstOrDefault(x => x.Name == "ChandlerBot") ??
            (await client.CreateAssistantAsync(new Azure.AI.OpenAI.Assistants.AssistantCreationOptions("gpt-3.5-turbo-1106")
            {
                Description = "Test assistant",
                FileIds = { },
                Instructions = "You are funny, like Chandler from Friends.",
                Metadata =
                {
                    { "foo", "bar" }
                },
                Name = "ChandlerBot",
                Tools = { },
            })).Value;

        var thread = (await client.CreateThreadAsync(new Azure.AI.OpenAI.Assistants.AssistantThreadCreationOptions
        {
            Metadata = 
            {
                { "foo", "bar" }
            },
            Messages = 
            {
               new Azure.AI.OpenAI.Assistants.ThreadInitializationMessage(Azure.AI.OpenAI.Assistants.MessageRole.User, "Hi there, I'm kzu!")
            },
        })).Value;
        
        var msg = (await client.CreateMessageAsync(thread.Id, Azure.AI.OpenAI.Assistants.MessageRole.User, "What was funniest Friends episode?")).Value;

        var run = (await client.CreateRunAsync(thread.Id, new Azure.AI.OpenAI.Assistants.CreateRunOptions(assistant.Id)
        {
            AdditionalInstructions = "Respond in less than 100 words.",
            Metadata = { },
            OverrideInstructions = "",
            OverrideModelName = "",
            OverrideTools = { },
        })).Value;

        while (run.Status != Azure.AI.OpenAI.Assistants.RunStatus.Completed)
        {
            run = (await client.GetRunAsync(thread.Id, run.Id)).Value;
            Thread.Sleep(1000);
        }

        foreach (var message in (await client.GetMessagesAsync(thread.Id)).Value)
        {
            output.WriteLine($"{message.Role}: {string.Join(Environment.NewLine, message.ContentItems.OfType<MessageTextContent>().Select(x => x.Text))}");
        }

        //var oai = new OpenAIClient(config["OpenAI:Key"] ??
        //    throw new InvalidOperationException("Please provide the OpenAI API key. See readme for more information."));

        //var options = new ChatCompletionsOptions
        //{
        //    DeploymentName = "gpt-4-1106-preview",
        //    Messages =
        //    {
        //        new ChatRequestUserMessage("Hello world")
        //    }
        //};

        //foreach (var function in functions)
        //{
        //    options.Tools.Add(new ChatCompletionsFunctionToolDefinition(function));
        //}

        //var response = await oai.GetChatCompletionsStreamingAsync(options, cancellation);
        //var tools = new List<(string Id, string Name, StringBuilder Args)>();
    }
}

