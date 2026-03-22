---
name: moderation
description: 'Detect and filter harmful, toxic, or policy-violating content using AWS Bedrock content moderation capabilities. Use when: screening user inputs or model outputs for harmful content categories (hate speech, violence, sexual content, self-harm, profanity), implementing content safety pipelines, applying Bedrock Guardrails content filters, or validating model responses before returning to end users. Covers content filter configuration, ApplyGuardrail API, severity thresholds, and integration patterns in C#, Python, or TypeScript.'
argument-hint: 'Describe the content type to moderate or the filter categories needed'
---

# AWS Bedrock Moderation

## When to Use
- Screening user-provided text before sending to a model
- Filtering model responses before returning to end users
- Applying content safety rules (hate, violence, sexual content, self-harm, profanity)
- Blocking or flagging policy-violating content in a pipeline
- Implementing responsible AI guardrails in production systems

## Moderation Mechanism

Bedrock provides content moderation via **Guardrails** — reusable safety policies attached to model calls. Guardrails include:

| Filter Type | Description |
|---|---|
| **Content Filters** | Harmful content categories with configurable severity thresholds |
| **Word Filters** | Block exact words/phrases and managed profanity lists |
| **Topic Denial** | Block discussion of specific topics (e.g., competitor products) |
| **PII Redaction** | Detect and anonymize PII in inputs and outputs |
| **Grounding Check** | Detect hallucinations relative to a reference source |

## Procedure

### 1. Create a Guardrail with Content Filters

Use the AWS Console or SDK to configure filter strengths per category.

```csharp
var bedrockClient = new AmazonBedrockClient(RegionEndpoint.USEast1);

var createResponse = await bedrockClient.CreateGuardrailAsync(new CreateGuardrailRequest
{
    Name = "content-moderation-guardrail",
    Description = "Blocks harmful content before and after model inference",
    ContentPolicyConfig = new ContentPolicyConfig
    {
        FiltersConfig = new List<ContentFilterConfig>
        {
            new ContentFilterConfig { Type = ContentFilterType.HATE,        InputStrength = FilterStrength.HIGH,   OutputStrength = FilterStrength.HIGH   },
            new ContentFilterConfig { Type = ContentFilterType.VIOLENCE,     InputStrength = FilterStrength.MEDIUM, OutputStrength = FilterStrength.MEDIUM },
            new ContentFilterConfig { Type = ContentFilterType.SEXUAL,       InputStrength = FilterStrength.HIGH,   OutputStrength = FilterStrength.HIGH   },
            new ContentFilterConfig { Type = ContentFilterType.INSULTS,      InputStrength = FilterStrength.MEDIUM, OutputStrength = FilterStrength.MEDIUM },
            new ContentFilterConfig { Type = ContentFilterType.MISCONDUCT,   InputStrength = FilterStrength.HIGH,   OutputStrength = FilterStrength.HIGH   },
            new ContentFilterConfig { Type = ContentFilterType.PROMPT_ATTACK,InputStrength = FilterStrength.HIGH,   OutputStrength = FilterStrength.NONE   }
        }
    },
    BlockedInputMessaging = "Your message violates our content policy.",
    BlockedOutputsMessaging = "The response was blocked for safety reasons."
});

string guardrailId = createResponse.GuardrailId;
string guardrailVersion = createResponse.Version; // "DRAFT" initially
```

### 2. Filter Strength Levels

| Strength | Meaning |
|---|---|
| `NONE` | No filtering |
| `LOW` | Only highest severity content blocked |
| `MEDIUM` | Moderate and high severity blocked |
| `HIGH` | All potentially harmful content blocked (strict) |

### 3. Apply Guardrail Explicitly (ApplyGuardrail API)

Use this to moderate text **without invoking a model** — for pre/post-processing pipelines:

```csharp
var runtimeClient = new AmazonBedrockRuntimeClient(RegionEndpoint.USEast1);

var result = await runtimeClient.ApplyGuardrailAsync(new ApplyGuardrailRequest
{
    GuardrailIdentifier = guardrailId,
    GuardrailVersion = "1",      // Published version or "DRAFT"
    Source = GuardrailContentSource.INPUT,  // INPUT | OUTPUT
    Content = new List<GuardrailContentBlock>
    {
        new GuardrailContentBlock
        {
            Text = new GuardrailTextBlock { Text = userInput }
        }
    }
});

if (result.Action == GuardrailAction.GUARDRAIL_INTERVENED)
{
    // Content was blocked
    Console.WriteLine($"Blocked. Reason: {string.Join(", ", result.Assessments
        .SelectMany(a => a.ContentPolicy?.Filters ?? new())
        .Where(f => f.Action == GuardrailAction.GUARDRAIL_INTERVENED)
        .Select(f => f.Type))}");
}
```

### 4. Attach Guardrail to Model Call (Auto-filter)

```csharp
var converseResponse = await runtimeClient.ConverseAsync(new ConverseRequest
{
    ModelId = "anthropic.claude-3-haiku-20240307-v1:0",
    GuardrailConfig = new GuardrailStreamConfiguration
    {
        GuardrailIdentifier = guardrailId,
        GuardrailVersion = "1",
        Trace = GuardrailTrace.ENABLED   // Include trace in response for debugging
    },
    Messages = new List<Message> { /* ... */ }
});

// Check if guardrail intervened
if (converseResponse.StopReason == StopReason.GUARDRAIL_INTERVENED)
{
    // Inspect converseResponse.Trace.Guardrail for details
}
```

### 5. Inspect Assessment Trace

```csharp
foreach (var assessment in result.Assessments)
{
    foreach (var filter in assessment.ContentPolicy?.Filters ?? new())
    {
        Console.WriteLine($"Category: {filter.Type}, Confidence: {filter.Confidence}, Action: {filter.Action}");
    }
}
```

## Integration Patterns

- **Input screening**: Call `ApplyGuardrail` before sending user input to a model.
- **Output screening**: Call `ApplyGuardrail(Source = OUTPUT)` on model responses.
- **Attached guardrail**: Set `GuardrailConfig` on Converse/InvokeModel for automatic filtering.
- **Audit logging**: Log all `GUARDRAIL_INTERVENED` events with category and user context.

## Best Practices

- **Prefer attached guardrails** over manual `ApplyGuardrail` calls for atomic model+filter behavior.
- Enable `PROMPT_ATTACK` filtering on `INPUT` only (not needed on output).
- Use `Trace = ENABLED` in development for debugging; disable in production to reduce response size.
- Publish a versioned guardrail (not `DRAFT`) in production to avoid unintended changes.
- Add **Topic Denial** for domain-scoped apps (e.g., block off-topic discussions).

## References

- [Guardrails Overview](./references/guardrails.md)
- [Content Filter Categories](./references/content-filter-categories.md)
- [ApplyGuardrail API Reference](./references/apply-guardrail-api.md)
