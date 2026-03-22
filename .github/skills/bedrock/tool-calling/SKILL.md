---
name: tool-calling
description: 'Implement structured tool use (function calling) with AWS Bedrock Converse API to give models access to external functions, APIs, and data sources. Use when: defining tools for a model to invoke, returning tool results back to the model, building agentic loops (plan-act-observe), enforcing structured JSON output via a schema, implementing calculator/search/database integrations, or designing multi-step reasoning workflows. Covers ToolSpec construction, tool dispatch, result injection, parallel tool use, and forced tool choice in C#, Python, or TypeScript.'
argument-hint: 'Describe the tools or functions the model should be able to call'
---

# AWS Bedrock Tool Calling

## When to Use
- Giving a model access to external functions (search, database, calculator, APIs)
- Guaranteeing structured JSON output by wrapping it in a forced tool call
- Building agentic reasoning loops (plan → call tool → observe → repeat)
- Running multiple tool calls in parallel from a single model turn
- Implementing a retrieval step inside a RAG agent
- Designing workflows where the model decides which tool to invoke

## How It Works

1. **Define tools** via `ToolConfig` with a JSON Schema input spec.
2. **Model responds** with a `tool_use` content block indicating which tool to call and with what arguments.
3. **Your code dispatches** the call and gets a result.
4. **Inject the result** back into the conversation as a `tool_result` block.
5. **Repeat** until the model returns a final text response (stop reason = `end_turn`).

## Procedure

### 1. Define a Tool

```csharp
var getWeatherTool = new Tool
{
    ToolSpec = new ToolSpecification
    {
        Name        = "get_weather",
        Description = "Returns the current weather for a given city and unit.",
        InputSchema = new ToolInputSchema
        {
            Json = Document.FromObject(new
            {
                type = "object",
                properties = new
                {
                    city = new
                    {
                        type        = "string",
                        description = "The city name, e.g. 'Seattle'"
                    },
                    unit = new
                    {
                        type        = "string",
                        @enum       = new[] { "celsius", "fahrenheit" },
                        description = "Temperature unit"
                    }
                },
                required            = new[] { "city" },
                additionalProperties = false
            })
        }
    }
};
```

### 2. Register Tools with the Request

```csharp
var converseRequest = new ConverseRequest
{
    ModelId    = "anthropic.claude-3-haiku-20240307-v1:0",
    Messages   = new List<Message> { userMessage },
    ToolConfig = new ToolConfiguration
    {
        Tools = new List<Tool> { getWeatherTool },
        // Optional: force a specific tool
        ToolChoice = new ToolChoice
        {
            Auto = new AutoToolChoice()   // Let model decide (default)
            // OR: Tool = new SpecificToolChoice { Name = "get_weather" }   // Force specific tool
            // OR: Any  = new AnyToolChoice()                                // Force any tool
        }
    }
};
```

### 3. Agentic Loop

```csharp
var messages = new List<Message> { userMessage };

while (true)
{
    var response = await client.ConverseAsync(new ConverseRequest
    {
        ModelId    = "anthropic.claude-3-haiku-20240307-v1:0",
        Messages   = messages,
        ToolConfig = new ToolConfiguration { Tools = new List<Tool> { getWeatherTool } }
    });

    // Add model response to history
    messages.Add(response.Output.Message);

    if (response.StopReason != StopReason.TOOL_USE)
    {
        // Model is done — return final text response
        Console.WriteLine(response.Output.Message.Content
            .FirstOrDefault(c => c.Text != null)?.Text);
        break;
    }

    // Dispatch tool calls and collect results
    var toolResults = new List<ContentBlock>();

    foreach (var block in response.Output.Message.Content.Where(c => c.ToolUse != null))
    {
        string toolName  = block.ToolUse.Name;
        var    toolInput = block.ToolUse.Input;

        string result = toolName switch
        {
            "get_weather" => await GetWeather(
                toolInput["city"].ToString(),
                toolInput.TryGetValue("unit", out var u) ? u.ToString() : "celsius"),
            _ => throw new InvalidOperationException($"Unknown tool: {toolName}")
        };

        toolResults.Add(new ContentBlock
        {
            ToolResult = new ToolResultBlock
            {
                ToolUseId = block.ToolUse.ToolUseId,
                Content   = new List<ToolResultContentBlock>
                {
                    new ToolResultContentBlock { Text = result }
                }
            }
        });
    }

    // Inject tool results as a "user" message
    messages.Add(new Message
    {
        Role    = "user",
        Content = toolResults
    });
}
```

### 4. Parallel Tool Calls

Models can request multiple tool calls in a single response. Always iterate over **all** `ToolUse` blocks — not just the first:

```csharp
var toolUseCalls = response.Output.Message.Content
    .Where(c => c.ToolUse != null)
    .ToList();

// Dispatch all in parallel
var tasks = toolUseCalls.Select(async block =>
{
    string result = await DispatchTool(block.ToolUse.Name, block.ToolUse.Input);
    return (block.ToolUse.ToolUseId, result);
});

var results = await Task.WhenAll(tasks);
```

### 5. Forced Tool Call (Structured Output)

Use `SpecificToolChoice` to guarantee the model fills a defined schema — ideal for structured extraction:

```csharp
ToolChoice = new ToolChoice
{
    Tool = new SpecificToolChoice { Name = "extract_movie_info" }
}
```

The model **must** call `extract_movie_info` before responding, producing JSON matching the tool's `InputSchema`.

### 6. Extract Tool Input

```csharp
// Tool input is Document — serialize it back to JSON
string json = JsonSerializer.Serialize(block.ToolUse.Input);

// Or access individual fields
string city = block.ToolUse.Input["city"].ToString();
```

## Tool Schema Best Practices

- Use `description` on every property — the model uses these to correctly populate arguments.
- Set `required` for mandatory parameters; optional params should have sane defaults in your dispatcher.
- Set `additionalProperties: false` to prevent the model from hallucinating extra fields.
- Keep schemas focused: one tool per responsibility. Avoid catch-all tools.
- Use enums for constrained choices to reduce hallucination.

## Error Handling in Tool Results

Return errors as tool results so the model can reason about failures:

```csharp
new ToolResultContentBlock { Text = JsonSerializer.Serialize(new { error = ex.Message }) }
// Model will often try a different approach or report the error gracefully
```

## ToolChoice Options

| Option | Behavior |
|---|---|
| `AutoToolChoice` | Model decides whether and which tool to call (default) |
| `AnyToolChoice` | Model must call at least one tool |
| `SpecificToolChoice { Name }` | Model must call that exact tool |

## Supported Models for Tool Use

| Model | Parallel Tools | Forced Tool |
|---|---|---|
| Claude 3.5 Sonnet | ✅ | ✅ |
| Claude 3 Haiku | ✅ | ✅ |
| Llama 3.1 / 3.2 | ✅ | ✅ |
| Cohere Command R+ | ✅ | ✅ |
| Amazon Nova Pro | ✅ | ✅ |
| Mistral Large | ✅ | ✅ |

## References

- [Tool Calling API Reference](./references/tool-calling-api.md)
- [JSON Schema Cheat Sheet](./references/json-schema.md)
- [Agentic Loop Patterns](./references/agentic-loop.md)
- [Structured Output via Forced Tools](./references/structured-output.md)
