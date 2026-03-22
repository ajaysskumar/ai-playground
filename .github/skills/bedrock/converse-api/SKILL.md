---
name: converse-api
description: 'Build multi-turn conversational applications using the AWS Bedrock Converse and ConverseStream APIs. Use when: implementing chat interfaces, maintaining conversation history, sending system prompts, handling multi-turn context, building streaming chat responses, or needing a unified cross-model API instead of model-specific InvokeModel payloads. Covers request construction, message history management, streaming, system prompts, stop reasons, and token usage in C#, Python, or TypeScript.'
argument-hint: 'Describe the conversation scenario, model, and whether streaming is needed'
---

# AWS Bedrock Converse API

## When to Use
- Building a multi-turn chat or virtual assistant
- Needing a single API shape that works across Claude, Titan, Llama, Mistral, Cohere, and Nova models
- Sending system prompts to control model persona or behavior
- Implementing streaming chat responses for real-time UX
- Managing and passing conversation history across turns
- Replacing model-specific `InvokeModel` payloads with a unified interface

## Why Converse API over InvokeModel

| Concern | InvokeModel | Converse API |
|---|---|---|
| API shape | Model-specific JSON payloads | Unified across all models |
| Multi-turn | Manual history serialization | Structured `Messages` list |
| System prompts | Model-specific | Single `System` field |
| Tool use | Model-specific | Unified `ToolConfig` |
| Streaming | `InvokeModelWithResponseStream` | `ConverseStreamAsync` |

## Procedure

### 1. Single-Turn Request

```csharp
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;

var client = new AmazonBedrockRuntimeClient(RegionEndpoint.USEast1);

var response = await client.ConverseAsync(new ConverseRequest
{
    ModelId = "anthropic.claude-3-haiku-20240307-v1:0",
    System = new List<SystemContentBlock>
    {
        new SystemContentBlock { Text = "You are a helpful assistant. Be concise." }
    },
    Messages = new List<Message>
    {
        new Message
        {
            Role = "user",
            Content = new List<ContentBlock>
            {
                new ContentBlock { Text = "What is the capital of France?" }
            }
        }
    },
    InferenceConfig = new InferenceConfiguration
    {
        MaxTokens = 256,
        Temperature = 0.5F
    }
});

string reply = response.Output.Message.Content[0].Text;
Console.WriteLine(reply);
```

### 2. Multi-Turn Conversation

Maintain conversation history by appending messages after each turn:

```csharp
var history = new List<Message>();

while (true)
{
    string userInput = Console.ReadLine();
    if (userInput == "exit") break;

    // Append user turn
    history.Add(new Message
    {
        Role = "user",
        Content = new List<ContentBlock> { new ContentBlock { Text = userInput } }
    });

    var response = await client.ConverseAsync(new ConverseRequest
    {
        ModelId = "anthropic.claude-3-haiku-20240307-v1:0",
        System = new List<SystemContentBlock>
        {
            new SystemContentBlock { Text = "You are a helpful assistant." }
        },
        Messages = history,
        InferenceConfig = new InferenceConfiguration { MaxTokens = 512 }
    });

    var assistantMessage = response.Output.Message;
    history.Add(assistantMessage);  // Append assistant turn to history

    Console.WriteLine($"AI: {assistantMessage.Content[0].Text}");
}
```

### 3. Streaming Response (ConverseStreamAsync)

Use streaming to display partial tokens in real-time:

```csharp
var stream = await client.ConverseStreamAsync(new ConverseStreamRequest
{
    ModelId = "anthropic.claude-3-5-sonnet-20241022-v2:0",
    Messages = new List<Message>
    {
        new Message
        {
            Role = "user",
            Content = new List<ContentBlock> { new ContentBlock { Text = "Write a haiku about the ocean." } }
        }
    }
});

var sb = new StringBuilder();
await foreach (var @event in stream.Stream)
{
    if (@event is ContentBlockDeltaEvent delta && delta.Delta?.Text != null)
    {
        Console.Write(delta.Delta.Text);   // Stream tokens as they arrive
        sb.Append(delta.Delta.Text);
    }
    else if (@event is MessageStopEvent stop)
    {
        Console.WriteLine();
        Console.WriteLine($"Stop reason: {stop.StopReason}");
    }
}
string fullResponse = sb.ToString();
```

### 4. Inference Parameters

```csharp
InferenceConfig = new InferenceConfiguration
{
    MaxTokens   = 1024,          // Max output tokens
    Temperature = 0.7F,          // 0.0 = deterministic, 1.0 = creative
    TopP        = 0.9F,          // Nucleus sampling threshold
    StopSequences = new List<string> { "\n\nHuman:" }  // Custom stop strings
}
```

### 5. Handle Stop Reasons

```csharp
switch (response.StopReason)
{
    case StopReason.END_TURN:
        // Normal completion
        break;
    case StopReason.MAX_TOKENS:
        // Truncated — increase MaxTokens or summarize context
        break;
    case StopReason.STOP_SEQUENCE:
        // Hit a stop sequence
        break;
    case StopReason.TOOL_USE:
        // Model wants to call a tool — see tool-calling skill
        break;
    case StopReason.GUARDRAIL_INTERVENED:
        // Content blocked by guardrail — see guardrails skill
        break;
}
```

### 6. Token Usage

```csharp
Console.WriteLine($"Input tokens:  {response.Usage.InputTokens}");
Console.WriteLine($"Output tokens: {response.Usage.OutputTokens}");
Console.WriteLine($"Total tokens:  {response.Usage.TotalTokens}");
```

## Context Window Management

LLMs have finite context windows. For long conversations:

1. **Summarize**: Periodically summarize older turns and replace them with the summary.
2. **Sliding window**: Keep only the last N turns in history.
3. **Truncate by tokens**: Count tokens from the end of history and truncate older messages.

```csharp
// Simple turn-count windowing
const int maxTurns = 20;
if (history.Count > maxTurns * 2)
    history = history.TakeLast(maxTurns * 2).ToList();
```

## System Prompt Best Practices

- Define persona, tone, and scope clearly in the system prompt.
- Specify output format constraints (e.g., "respond only in JSON").
- Include safety instructions or topic restrictions that complement Guardrails.
- Keep the system prompt stable across turns — it does not need to be in history.

## Supported Models

| Model ID | Notes |
|---|---|
| `anthropic.claude-3-5-sonnet-20241022-v2:0` | Best quality, long context (200K) |
| `anthropic.claude-3-haiku-20240307-v1:0` | Fastest, cheapest |
| `meta.llama3-70b-instruct-v1:0` | Open weights, strong reasoning |
| `amazon.titan-text-express-v1` | Cost-effective general use |
| `cohere.command-r-plus-v1:0` | Optimized for RAG and tool use |
| `amazon.nova-pro-v1:0` | Best Nova model, multimodal |

## References

- [Converse API Request Schema](./references/converse-request-schema.md)
- [Streaming Guide](./references/streaming.md)
- [Context Window Limits per Model](./references/context-windows.md)
- [Tool Calling with Converse API](./references/tool-use.md)
