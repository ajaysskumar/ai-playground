using Amazon.Bedrock;
using Amazon.Bedrock.Model;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;

// Alias the ambiguous types so both namespaces can coexist cleanly
using BedrockGuardrailConfiguration = Amazon.BedrockRuntime.Model.GuardrailConfiguration;
using BedrockContentFilterType = Amazon.Bedrock.GuardrailContentFilterType;

namespace AwsBedrockExamples.Services;

/// <summary>
/// Demonstrates AWS Bedrock Guardrails by:
///   1. Creating a guardrail with content filters and PII redaction
///   2. Publishing a versioned guardrail
///   3. Running sample prompts through a guarded customer-support chat to show pass, block, and redaction behaviour
///   4. Cleaning up the guardrail at the end
/// </summary>
public class BedrockGuardrailsDemoService
{
    private readonly AmazonBedrockClient _bedrockClient;
    private readonly AmazonBedrockRuntimeClient _runtimeClient;
    private const string ModelId = "openai.gpt-oss-20b-1:0";

    public BedrockGuardrailsDemoService()
    {
        _bedrockClient = new AmazonBedrockClient(Amazon.RegionEndpoint.USEast1);
        _runtimeClient = new AmazonBedrockRuntimeClient(Amazon.RegionEndpoint.USEast1);
    }

    // -------------------------------------------------------------------------
    // Step 1: Create + publish guardrail
    // -------------------------------------------------------------------------

    public async Task<(string guardrailId, string version)> CreateAndPublishGuardrailAsync()
    {
        Console.WriteLine(">>> Creating guardrail...");

        var createRequest = new CreateGuardrailRequest
        {
            Name        = $"customer-support-guardrail-{DateTime.UtcNow:yyyyMMddHHmmss}",
            Description = "Demo guardrail: content filters + topic denial + PII redaction for a customer support chatbot",

            // Content filters — block hate and insults in customer support interactions
            ContentPolicyConfig = new GuardrailContentPolicyConfig
            {
                FiltersConfig = new List<GuardrailContentFilterConfig>
                {
                    new() { Type = BedrockContentFilterType.HATE,         InputStrength = GuardrailFilterStrength.HIGH,   OutputStrength = GuardrailFilterStrength.HIGH   },
                    new() { Type = BedrockContentFilterType.SEXUAL,         InputStrength = GuardrailFilterStrength.HIGH,   OutputStrength = GuardrailFilterStrength.HIGH   },
                    new() { Type = BedrockContentFilterType.VIOLENCE,      InputStrength = GuardrailFilterStrength.MEDIUM, OutputStrength = GuardrailFilterStrength.MEDIUM },
                    new() { Type = BedrockContentFilterType.INSULTS,       InputStrength = GuardrailFilterStrength.MEDIUM, OutputStrength = GuardrailFilterStrength.MEDIUM },
                    new() { Type = BedrockContentFilterType.MISCONDUCT,    InputStrength = GuardrailFilterStrength.HIGH,   OutputStrength = GuardrailFilterStrength.HIGH   },
                    new() { Type = BedrockContentFilterType.PROMPT_ATTACK, InputStrength = GuardrailFilterStrength.MEDIUM,   OutputStrength = GuardrailFilterStrength.NONE   },
                }
            },

            // Topic denial — enforce support-only scope; reject general knowledge, jokes, coding help
            TopicPolicyConfig = new GuardrailTopicPolicyConfig
            {
                TopicsConfig = new List<GuardrailTopicConfig>
                {
                    new()
                    {
                        Name       = "off-topic-requests",
                        Definition = "Requests asking for entertainment, general knowledge, recipes, coding help, jokes, or trivia unrelated to customer support operations.",
                        Examples   = new List<string>
                        {
                            "Tell me a joke",
                            "What is the capital of France?",
                            "How do I write Python code?",
                            "Give me a recipe for pasta",
                            "What's the weather today?"
                        },
                        Type = Amazon.Bedrock.GuardrailTopicType.DENY
                    }
                }
            },

            // PII redaction — anonymise email, phone, name, and IP address in both input and output
            SensitiveInformationPolicyConfig = new GuardrailSensitiveInformationPolicyConfig
            {
                PiiEntitiesConfig =
                [
                    new() { Type = Amazon.Bedrock.GuardrailPiiEntityType.EMAIL, Action = GuardrailSensitiveInformationAction.ANONYMIZE },
                    new() { Type = Amazon.Bedrock.GuardrailPiiEntityType.PHONE, Action = GuardrailSensitiveInformationAction.ANONYMIZE },
                    new() { Type = Amazon.Bedrock.GuardrailPiiEntityType.NAME, Action = GuardrailSensitiveInformationAction.ANONYMIZE },
                    new() { Type = Amazon.Bedrock.GuardrailPiiEntityType.IP_ADDRESS, Action = GuardrailSensitiveInformationAction.ANONYMIZE },
                ]
            },

            BlockedInputMessaging   = "[BLOCKED] Your message was blocked by the content policy.",
            BlockedOutputsMessaging = "[BLOCKED] The response was blocked by the content policy."
        };

        var createResponse = await _bedrockClient.CreateGuardrailAsync(createRequest);
        string guardrailId = createResponse.GuardrailId;
        Console.WriteLine($"    Guardrail created  → ID: {guardrailId}  (status: DRAFT)");

        // Publish a numbered version so we can pin the model call to it
        Console.WriteLine(">>> Publishing guardrail version...");
        var versionResponse = await _bedrockClient.CreateGuardrailVersionAsync(
            new CreateGuardrailVersionRequest
            {
                GuardrailIdentifier = guardrailId,
                Description         = "Initial demo version"
            });

        string version = versionResponse.Version;
        Console.WriteLine($"    Version published  → {version}\n");

        return (guardrailId, version);
    }

    // -------------------------------------------------------------------------
    // Step 2: Interactive Mode - Test Custom Questions
    // -------------------------------------------------------------------------
    // Sample questions to try:
    //   1. "Where is my order number ORD-2025-001234? I placed it 3 days ago and expected it by now."  (should PASS)
    //   2. "Tell me a joke to cheer me up."                                                              (should be DENIED by topic policy)
    //   3. "You're all useless idiots! I hate your company and everyone working here is incompetent!"  (should be BLOCKED by content filter)
    //   4. "Hi, my name is John Smith. Email me at john@smith.com or call 555-987-6543."                (PII should be redacted: name, email, phone)

    public async Task RunInteractiveModeAsync(string guardrailId, string version)
    {
        Console.WriteLine("=== Interactive Mode: Test Custom Questions ===\n");
        Console.WriteLine("Enter your questions to test against the guardrail.");
        Console.WriteLine("Type 'exit' to end the session.\n");

        while (true)
        {
            Console.Write("Your question > ");
            string userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput) || userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("\nExiting interactive mode.\n");
                break;
            }

            Console.WriteLine();
            await RunSingleTurnAsync(guardrailId, version, userInput);
            Console.WriteLine();
        }
    }

    private async Task RunSingleTurnAsync(string guardrailId, string version, string userInput)
    {
        var request = new ConverseRequest
        {
            ModelId = ModelId,
            System  = new List<SystemContentBlock>
            {
                new() { Text = "You are a helpful customer support assistant. Help customers with order tracking, billing questions, shipping issues, and technical support. Be professional, empathetic, and courteous. Keep your answers concise." }
            },
            Messages = new List<Amazon.BedrockRuntime.Model.Message>
            {
                new()
                {
                    Role    = "user",
                    Content = new List<ContentBlock>
                    {
                        new() { Text = userInput }
                    }
                }
            },
            GuardrailConfig = new BedrockGuardrailConfiguration
            {
                GuardrailIdentifier = guardrailId,
                GuardrailVersion    = version,
                Trace               = GuardrailTrace.Enabled
            }
        };

        var response = await _runtimeClient.ConverseAsync(request);

        bool intervened = response.StopReason == StopReason.Guardrail_intervened;

        // Extract text from model or blocked-output message
        string outputText = response.Output?.Message?.Content
            .FirstOrDefault(c => c.Text != null)?.Text
            ?? "[no text content]";

        Console.WriteLine($"Stop Reason: {response.StopReason}");
        
        string statusMessage = "PASSED";
        if (intervened && response.Trace?.Guardrail != null)
        {
            string policyName = GetViolatedPolicy(response.Trace.Guardrail);
            statusMessage = $"GUARDRAIL INTERVENED - {policyName}";
        }
        else if (intervened)
        {
            statusMessage = "GUARDRAIL INTERVENED";
        }
        
        Console.WriteLine($"Status : {statusMessage}");
        Console.WriteLine($"Model  : {outputText}");
    }

    private static void PrintGuardrailTrace(GuardrailTraceAssessment trace)
    {
        // Trace details are available via response.Trace.Guardrail if needed for debugging
    }

    private static string GetViolatedPolicy(GuardrailTraceAssessment trace)
    {
        var violations = new List<string>();

        // Check input assessments
        if (trace.InputAssessment != null)
        {
            foreach (var (_, assessment) in trace.InputAssessment)
            {
                if (assessment.ContentPolicy?.Filters?.Count > 0)
                    violations.Add("Content Filter");
                if (assessment.TopicPolicy?.Topics?.Count > 0)
                {
                    var topicNames = assessment.TopicPolicy.Topics
                        .Where(t => !string.IsNullOrEmpty(t.Name))
                        .Select(t => t.Name)
                        .Distinct();
                    violations.Add($"Topic Policy: {string.Join(", ", topicNames)}");
                }
                if (assessment.SensitiveInformationPolicy?.PiiEntities?.Count > 0)
                    violations.Add("PII");
            }
        }

        // Check output assessments
        if (trace.OutputAssessments != null)
        {
            foreach (var (_, assessments) in trace.OutputAssessments)
            {
                foreach (var assessment in assessments)
                {
                    if (assessment.ContentPolicy?.Filters?.Count > 0)
                        violations.Add("Content Filter");
                    if (assessment.TopicPolicy?.Topics?.Count > 0)
                    {
                        var topicNames = assessment.TopicPolicy.Topics
                            .Where(t => !string.IsNullOrEmpty(t.Name))
                            .Select(t => t.Name)
                            .Distinct();
                        violations.Add($"Topic Policy: {string.Join(", ", topicNames)}");
                    }
                    if (assessment.SensitiveInformationPolicy?.PiiEntities?.Count > 0)
                        violations.Add("PII");
                }
            }
        }

        return violations.Count > 0 ? string.Join(", ", violations.Distinct()) : "Unknown Policy";
    }

    // -------------------------------------------------------------------------
    // Step 3: Cleanup
    // -------------------------------------------------------------------------

    public async Task DeleteGuardrailAsync(string guardrailId)
    {
        Console.WriteLine(new string('─', 70));
        Console.WriteLine(">>> Deleting guardrail (cleanup)...");
        await _bedrockClient.DeleteGuardrailAsync(new DeleteGuardrailRequest
        {
            GuardrailIdentifier = guardrailId
        });
        Console.WriteLine($"    Guardrail {guardrailId} deleted.\n");
    }
}
