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
        Console.WriteLine($"Status : {(intervened ? "GUARDRAIL INTERVENED" : "PASSED")}");
        Console.WriteLine($"Model  : {outputText}");

        // Print which filter categories fired
        if (response.Trace?.Guardrail != null)
        {
            PrintGuardrailTrace(response.Trace.Guardrail);
        }
        else if (intervened)
        {
            Console.WriteLine("  [Guardrail Trace] No detailed trace available");
        }
    }

    private static void PrintGuardrailTrace(GuardrailTraceAssessment trace)
    {
        Console.WriteLine("  [Guardrail Trace]");
        
        // Note: PII redaction happens transparently before the model sees input,
        // so explicit PII assessment traces may not always appear—only policy violations
        // (content filters, topic denials) will show in the trace.
        if (trace.InputAssessment != null && trace.InputAssessment.Count > 0)
        {
            Console.WriteLine("    Input Assessment:");
            Console.WriteLine($"    Input Assessment Count: {trace.InputAssessment.Count}");
            foreach (var (key, assessment) in trace.InputAssessment)
            {
                PrintAssessmentDetailed(assessment, "Input");
            }
        }
        else
        {
            Console.WriteLine("    Input Assessment: None");
        }

        if (trace.OutputAssessments != null && trace.OutputAssessments.Count > 0)
        {
            Console.WriteLine("    Output Assessments:");
            foreach (var (key, assessments) in trace.OutputAssessments)
            {
                foreach (var assessment in assessments)
                    PrintAssessmentDetailed(assessment, "Output");
            }
        }
        else
        {
            Console.WriteLine("    Output Assessments: None");
        }
    }

    private static void PrintAssessmentDetailed(GuardrailAssessment assessment, string direction)
    {
        bool foundAny = false;
        
        if (assessment.ContentPolicy?.Filters != null && assessment.ContentPolicy.Filters.Count > 0)
        {
            foundAny = true;
            foreach (var filter in assessment.ContentPolicy.Filters)
            {
                Console.WriteLine($"      [{direction} Content Filter] {filter.Type}: Action={filter.Action}, Confidence={filter.Confidence}");
            }
        }

        if (assessment.TopicPolicy?.Topics != null && assessment.TopicPolicy.Topics.Count > 0)
        {
            foundAny = true;
            foreach (var topic in assessment.TopicPolicy.Topics)
            {
                Console.WriteLine($"      [{direction} Topic Policy] {topic.Name}: Action={topic.Action}");
            }
        }

        if (assessment.SensitiveInformationPolicy?.PiiEntities != null && assessment.SensitiveInformationPolicy.PiiEntities.Count > 0)
        {
            foundAny = true;
            foreach (var pii in assessment.SensitiveInformationPolicy.PiiEntities)
            {
                Console.WriteLine($"      [{direction} PII] {pii.Type}: Action={pii.Action}");
            }
        }
        
        if (!foundAny)
        {
            Console.WriteLine($"      [{direction}] No policy violations detected");
        }
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
