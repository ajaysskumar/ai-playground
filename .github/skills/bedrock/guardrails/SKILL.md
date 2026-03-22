---
name: guardrails
description: 'Design, create, update, and apply AWS Bedrock Guardrails to enforce safety, privacy, and topic policies on LLM inputs and outputs. Use when: configuring content filtering, PII detection and redaction, topic denial, custom word filters, grounding checks (hallucination detection), or attaching guardrails to model inference calls. Covers CreateGuardrail, UpdateGuardrail, ApplyGuardrail, and guardrail versioning in C#, Python, or TypeScript.'
argument-hint: 'Describe the guardrail policy needed (content, PII, topic, grounding)'
---

# AWS Bedrock Guardrails

## When to Use
- Designing a safety policy for a Bedrock-powered application
- Enabling PII detection or redaction on model inputs/outputs
- Blocking discussion of specific topics (e.g., competitors, illegal activities)
- Filtering custom profanity or sensitive business terms
- Detecting hallucinations by grounding model outputs against reference documents
- Creating, versioning, and publishing guardrail policies

## Guardrail Policy Components

| Policy | What It Does |
|---|---|
| **Content Filters** | Block harmful content by category and severity |
| **Topic Denial** | Block or flag discussion of named topics |
| **Word Filters** | Block exact words/phrases; enable AWS managed profanity list |
| **PII Filters** | Detect, redact, or block PII types (names, SSN, email, phone, etc.) |
| **Grounding Check** | Verify model outputs are grounded in provided source content |
| **Contextual Grounding** | Detect relevance and groundedness scores per response |

## Procedure

### 1. Create a Guardrail

```csharp
using Amazon.Bedrock;
using Amazon.Bedrock.Model;

var bedrockClient = new AmazonBedrockClient(RegionEndpoint.USEast1);

var request = new CreateGuardrailRequest
{
    Name = "app-safety-guardrail",
    Description = "Enforces safety and privacy policies",

    // --- Content Filters ---
    ContentPolicyConfig = new ContentPolicyConfig
    {
        FiltersConfig = new List<ContentFilterConfig>
        {
            new() { Type = ContentFilterType.HATE,        InputStrength = FilterStrength.HIGH,   OutputStrength = FilterStrength.HIGH   },
            new() { Type = ContentFilterType.VIOLENCE,     InputStrength = FilterStrength.MEDIUM, OutputStrength = FilterStrength.MEDIUM },
            new() { Type = ContentFilterType.SEXUAL,       InputStrength = FilterStrength.HIGH,   OutputStrength = FilterStrength.HIGH   },
            new() { Type = ContentFilterType.PROMPT_ATTACK,InputStrength = FilterStrength.HIGH,   OutputStrength = FilterStrength.NONE   }
        }
    },

    // --- Topic Denial ---
    TopicPolicyConfig = new TopicPolicyConfig
    {
        TopicsConfig = new List<GuardrailTopicConfig>
        {
            new()
            {
                Name = "competitor-products",
                Definition = "Discussion of competitor products or services.",
                Examples = new List<string> { "How does this compare to OpenAI?", "Is GPT-4 better?" },
                Type = GuardrailTopicType.DENY
            }
        }
    },

    // --- Word Filters ---
    WordPolicyConfig = new WordPolicyConfig
    {
        WordsConfig = new List<GuardrailWordConfig>
        {
            new() { Text = "internal-only-term" },
            new() { Text = "confidential-product-name" }
        },
        ManagedWordListsConfig = new List<GuardrailManagedWordsConfig>
        {
            new() { Type = GuardrailManagedWordsType.PROFANITY }
        }
    },

    // --- PII ---
    SensitiveInformationPolicyConfig = new SensitiveInformationPolicyConfig
    {
        PiiEntitiesConfig = new List<GuardrailPiiEntityConfig>
        {
            new() { Type = GuardrailPiiEntityType.EMAIL,   Action = GuardrailSensitiveInformationAction.ANONYMIZE },
            new() { Type = GuardrailPiiEntityType.PHONE,   Action = GuardrailSensitiveInformationAction.ANONYMIZE },
            new() { Type = GuardrailPiiEntityType.NAME,    Action = GuardrailSensitiveInformationAction.ANONYMIZE },
            new() { Type = GuardrailPiiEntityType.US_SSN,  Action = GuardrailSensitiveInformationAction.BLOCK     }
        }
    },

    BlockedInputMessaging  = "Your input was blocked for policy reasons.",
    BlockedOutputsMessaging = "The response was blocked for safety reasons."
};

var response = await bedrockClient.CreateGuardrailAsync(request);
Console.WriteLine($"Guardrail ID: {response.GuardrailId}, Version: {response.Version}");
```

### 2. Publish a Versioned Guardrail

```csharp
var versionResponse = await bedrockClient.CreateGuardrailVersionAsync(new CreateGuardrailVersionRequest
{
    GuardrailIdentifier = response.GuardrailId,
    Description = "Initial production release"
});
string publishedVersion = versionResponse.Version; // "1", "2", etc.
```

### 3. Attach Guardrail to a Model Call

```csharp
var runtimeClient = new AmazonBedrockRuntimeClient(RegionEndpoint.USEast1);

var converseResponse = await runtimeClient.ConverseAsync(new ConverseRequest
{
    ModelId = "anthropic.claude-3-haiku-20240307-v1:0",
    GuardrailConfig = new GuardrailConfiguration
    {
        GuardrailIdentifier = guardrailId,
        GuardrailVersion = "1",
        Trace = GuardrailTrace.ENABLED  // Remove in production for smaller payloads
    },
    Messages = new List<Message> { /* ... */ }
});

if (converseResponse.StopReason == StopReason.GUARDRAIL_INTERVENED)
{
    Console.WriteLine("Guardrail intervened. Blocked message returned to user.");
}
```

### 4. Apply Guardrail Standalone (No Model Call)

```csharp
var applyResponse = await runtimeClient.ApplyGuardrailAsync(new ApplyGuardrailRequest
{
    GuardrailIdentifier = guardrailId,
    GuardrailVersion = "1",
    Source = GuardrailContentSource.INPUT,
    Content = new List<GuardrailContentBlock>
    {
        new() { Text = new GuardrailTextBlock { Text = userInput } }
    }
});

bool blocked = applyResponse.Action == GuardrailAction.GUARDRAIL_INTERVENED;
```

### 5. Update an Existing Guardrail

> `UpdateGuardrail` replaces the full guardrail config (not partial update). Fetch existing config first.

```csharp
await bedrockClient.UpdateGuardrailAsync(new UpdateGuardrailRequest
{
    GuardrailIdentifier = guardrailId,
    Name = "app-safety-guardrail",
    // ... include ALL policy configs (both updated and unchanged)
    BlockedInputMessaging  = "Blocked.",
    BlockedOutputsMessaging = "Blocked."
});
```

### 6. Contextual Grounding (Hallucination Detection)

```csharp
new ContextualGroundingPolicyConfig
{
    FiltersConfig = new List<GuardrailContextualGroundingFilterConfig>
    {
        new() { Type = GuardrailContextualGroundingFilterType.GROUNDING, Threshold = 0.7 },
        new() { Type = GuardrailContextualGroundingFilterType.RELEVANCE,  Threshold = 0.7 }
    }
}
```

Pass source documents in the `grounding_source` content block when calling `ApplyGuardrail`.

## PII Entity Types Reference

| Entity | Action Options |
|---|---|
| `NAME`, `EMAIL`, `PHONE`, `ADDRESS` | `ANONYMIZE` / `BLOCK` |
| `US_SSN`, `CREDIT_DEBIT_NUMBER` | `BLOCK` (recommended) |
| `AWS_ACCESS_KEY`, `AWS_SECRET_KEY` | `BLOCK` |
| `USERNAME`, `PASSWORD` | `BLOCK` |

## Versioning Strategy

- Use `DRAFT` version during development and testing.
- Publish numbered versions (`1`, `2`, ...) for production.
- Pin model calls to a specific version — never use `DRAFT` in production.
- Create a new version for policy changes; old versions remain callable.

## Best Practices

- Enable `PROMPT_ATTACK` filtering on inputs to defend against jailbreak attempts.
- Use `ANONYMIZE` over `BLOCK` for PII unless regulatory requirements mandate blocking.
- Enable `Trace = ENABLED` during development; disable in production.
- Align `BlockedInputMessaging` and `BlockedOutputsMessaging` with your app's UX copy.
- Audit `GUARDRAIL_INTERVENED` events via CloudWatch for tuning and compliance.

## References

- [Guardrails Concepts](./references/guardrails-concepts.md)
- [PII Types Reference](./references/pii-types.md)
- [Grounding Check Guide](./references/grounding-check.md)
- [Guardrail Versioning](./references/guardrail-versioning.md)
