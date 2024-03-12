using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Assistants;
using DotNetConfig;
using Microsoft.Extensions.Configuration;

namespace Devlooped.AI;

public class Misc(ITestOutputHelper output)
{
    static readonly IConfiguration config = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .AddDotNetConfig()
        .AddUserSecrets("720fb04e-8a57-480c-bfc8-a24ed9dfd68c")
        .Build();

    [Fact]
    public void PolymorphicJson()
    {
        var contents = new List<Content>
        {
            new MessageContent("Hello world"),
            new FileContent("file.txt", "text/plain"),
            new FileContent("image.png", "image/png"),
        };

        var json = JsonSerializer.Serialize(contents);

        output.WriteLine(json);

        var deserialized = JsonSerializer.Deserialize<List<Content>>(json);

        Assert.NotNull(deserialized);
        Assert.IsType<MessageContent>(deserialized[0]);
        Assert.IsType<FileContent>(deserialized[1]);
        Assert.IsType<FileContent>(deserialized[2]);
    }

    [Fact]
    public async Task RunChatStreamAsync()
    {
        await foreach (var text in Ask("What was funniest Friends episode?"))
        {
            output.WriteLine(text);
        }
    }

    static async IAsyncEnumerable<string> Ask(string question, [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        var oai = new OpenAIClient(config["OpenAI:Key"] ??
            throw new InvalidOperationException("Please provide the OpenAI API key. See readme for more information."));

        var options = new ChatCompletionsOptions
        {
            DeploymentName = "gpt-3.5-turbo-1106",
            Messages =
            {
                new ChatRequestSystemMessage("You are funny, like Chandler from Friends."),
                new ChatRequestSystemMessage("Respond in les than 50 words."),
                new ChatRequestUserMessage(question)
            }
        };

        var response = await oai.GetChatCompletionsStreamingAsync(options, cancellation);
        await foreach (var choice in response.EnumerateValues())
        {
            if (choice.ContentUpdate != null)
                yield return choice.ContentUpdate;
        }
    }

    [Fact]
    public async Task RunAssistantAsync()
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

        var threadId = config["StreamR:Tests:ThreadID"];
        if (threadId is null)
        {
            var thread = (await client.CreateThreadAsync(new Azure.AI.OpenAI.Assistants.AssistantThreadCreationOptions
            {
                Metadata =
                {
                    { "foo", "bar" }
                },
                Messages =
                {
                   new Azure.AI.OpenAI.Assistants.ThreadInitializationMessage(Azure.AI.OpenAI.Assistants.MessageRole.User,  "What was funniest Friends episode?")
                },
            })).Value;

            Config.Build(DotNetConfig.ConfigLevel.Global)
                .SetString("StreamR", "Tests", "ThreadID", thread.Id);

            threadId = thread.Id;

            var run = (await client.CreateRunAsync(threadId, new Azure.AI.OpenAI.Assistants.CreateRunOptions(assistant.Id)
            {
                AdditionalInstructions = "Respond in less than 50 words.",
                Metadata = { },
                OverrideInstructions = "",
                OverrideModelName = "",
                OverrideTools = { },
            })).Value;

            while (run.Status != Azure.AI.OpenAI.Assistants.RunStatus.Completed)
            {
                run = (await client.GetRunAsync(threadId, run.Id)).Value;
                await Task.Delay(1000);
            }
        }

        foreach (var message in (await client.GetMessagesAsync(threadId)).Value)
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

