---
name: text-generation
description: 'Generate text using AWS Bedrock foundation models (Claude, Titan, Llama, Mistral, Command R). Use when: writing InvokeModel or Converse API calls for text completion, summarization, classification, translation, code generation, or question answering. Covers model selection, prompt formatting, request construction, parameter tuning (temperature, top_p, max_tokens), and response parsing in C#, Python, or TypeScript.'
argument-hint: 'Describe the text generation task, model, and language'
---

# AWS Bedrock Text Generation

## When to Use
- Calling a Bedrock foundation model for text completion or chat
- Writing or debugging `InvokeModel` / `ConverseAsync` API calls
- Tuning inference parameters (temperature, top_p, max_tokens, stop_sequences)
- Parsing model response payloads
- Selecting between available foundation models for a use case

## Supported Models

| Model ID | Provider | Strengths |
|---|---|---|
| `anthropic.claude-3-5-sonnet-20241022-v2:0` | Anthropic | Best reasoning, long context |
| `anthropic.claude-3-haiku-20240307-v1:0` | Anthropic | Fast, cheap, good for chat |
| `amazon.titan-text-express-v1` | Amazon | General purpose, cheap |
| `meta.llama3-70b-instruct-v1:0` | Meta | Open weights, instruction tuned |
| `mistral.mistral-7b-instruct-v0:2` | Mistral | Low latency, small footprint |
| `cohere.command-r-plus-v1:0` | Cohere | RAG-optimized, tool use |

## Procedure

### 1. Choose the API

| Scenario | API |
|---|---|
| Single-turn, raw payload control | `InvokeModelAsync` |
| Multi-turn chat, cross-model compatibility | `ConverseAsync` |

Prefer **Converse API** for new code — it normalizes request/response shapes across all models.

### 2. Construct a Request (Converse API)

```csharp
var client = new AmazonBedrockRuntimeClient(RegionEndpoint.USEast1);
var response = await client.ConverseAsync(new ConverseRequest
{
    ModelId = "anthropic.claude-3-haiku-20240307-v1:0",
    System = new List<SystemContentBlock>
    {
        new SystemContentBlock { Text = "You are a helpful assistant." }
    },
    Messages = new List<Message>
    {
        new Message
        {
            Role = "user",
            Content = new List<ContentBlock>
            {
                new ContentBlock { Text = "Summarize the following text: ..." }
            }
        }
    },
    InferenceConfig = new InferenceConfiguration
    {
        MaxTokens = 512,
        Temperature = 0.7F,
        TopP = 0.9F
    }
});
string result = response.Output.Message.Content[0].Text;
```

### 3. Tune Key Parameters

| Parameter | Effect | Recommended Range |
|---|---|---|
| `Temperature` | Randomness of output | 0.0 (deterministic) – 1.0 (creative) |
| `TopP` | Nucleus sampling threshold | 0.9 for most tasks |
| `MaxTokens` | Maximum output length | Task-dependent, start at 512 |
| `StopSequences` | Early termination signals | e.g. `["\n\nHuman:"]` for Claude |

### 4. Parse Response

Converse API response structure:
```csharp
// Text output
string text = response.Output.Message.Content
    .FirstOrDefault(c => c.Text != null)?.Text;

// Stop reason
string stopReason = response.StopReason; // "end_turn" | "max_tokens" | "stop_sequence" | "tool_use"

// Token usage
int inputTokens = response.Usage.InputTokens;
int outputTokens = response.Usage.OutputTokens;
```

### 5. Handle Errors

```csharp
catch (AmazonBedrockRuntimeException ex) when (ex.ErrorCode == "ThrottlingException")
{
    // Implement exponential backoff
}
catch (AmazonBedrockRuntimeException ex) when (ex.ErrorCode == "ValidationException")
{
    // Check model ID, region, and request payload
}
```

## Common Patterns

- **Structured output**: Instruct the model to return JSON; combine with Tool Calling skill for guaranteed schema adherence.
- **Streaming**: Use `ConverseStreamAsync` for long outputs to improve perceived latency.
- **Batch**: Use Bedrock Batch Inference for large-scale offline jobs.

## References

- [AWS Bedrock Converse API Docs](./references/converse-api.md)
- [Model IDs and Regions](./references/model-ids.md)
- [Prompt Engineering Guide](./references/prompt-engineering.md)
