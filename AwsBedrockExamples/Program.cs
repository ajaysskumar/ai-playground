using AwsBedrockExamples.Helpers;
using AwsBedrockExamples.Services;

ArgumentParser parser = new ArgumentParser(args);
string demoType = parser.GetDemo();

Console.WriteLine($"Starting AI Demo: {demoType}\n");

switch (demoType.ToLower())
{
    case "bedrock-movie":
        await RunBedrockMovieDemo();
        break;
    case "bedrock-movie-converse-tools":
        await RunBedrockToolsMovieDemo();
        break;
    case "bedrock-customer-support":
        await RunBedrockCustomerSupportChat();
        break;
    case "bedrock-guardrails":
        await RunBedrockGuardrailsDemo();
        break;
    default:
        Console.WriteLine("Available demos:");
        Console.WriteLine("  dotnet run -- --demo bedrock-movie");
        Console.WriteLine("  dotnet run -- --demo bedrock-movie-converse-tools");
        Console.WriteLine("  dotnet run -- --demo bedrock-customer-support");
        Console.WriteLine("  dotnet run -- --demo bedrock-guardrails");
        break;
}

async Task RunBedrockMovieDemo()
{
    try
    {
        var bedrockService = new BedrockService();
        
        string movieQuery = "Inception";
        
        Console.WriteLine($"\nSearching for: {movieQuery}...\n");
        
        var result = await bedrockService.GetMovieDetails(movieQuery);
        
        if (string.IsNullOrEmpty(result))
        {
            Console.WriteLine("No response from Bedrock.");
            return;
        }
        
        Console.WriteLine("=== Response ===\n");
        Console.WriteLine(result);
        Console.WriteLine("=== Formatted JSON ===\n");
        // Extract the 'text' property from the response JSON
        string textValue = result;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(result);
            var contentArray = doc.RootElement.GetProperty("content");
            if (contentArray.ValueKind == System.Text.Json.JsonValueKind.Array && contentArray.GetArrayLength() > 0)
            {
                var firstContent = contentArray[0];
                if (firstContent.TryGetProperty("text", out var textProp))
                {
                    textValue = textProp.GetString();
                }
            }
        }
        catch { /* fallback to original result if parsing fails */ }
        Console.WriteLine(JsonUtils.ParseEscapedJson(textValue));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
    }
}


// Customer support chat demo
async Task RunBedrockCustomerSupportChat()
{
    try
    {
        var supportService = new BedrockCustomerSupportService();
        var conversation = new List<Amazon.BedrockRuntime.Model.Message>();
        Console.WriteLine("\nWelcome to AI Customer Support! Type 'Exit Chat' to end the conversation.\n");
        while (true)
        {
            Console.Write("You: ");
            var userInput = Console.ReadLine();
            if (string.Equals(userInput, "Exit Chat", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("\nThank you for chatting with AI Customer Support. Goodbye!\n");
                break;
            }
            // Add user message to conversation
            conversation.Add(new Amazon.BedrockRuntime.Model.Message
            {
                Role = "user",
                Content = new List<Amazon.BedrockRuntime.Model.ContentBlock>
                {
                    new Amazon.BedrockRuntime.Model.ContentBlock { Text = userInput }
                }
            });

            // Get AI response
            var response = await supportService.GetSupportResponse(conversation);

            // Add assistant response to conversation
            conversation.Add(new Amazon.BedrockRuntime.Model.Message
            {
                Role = "assistant",
                Content = new List<Amazon.BedrockRuntime.Model.ContentBlock>
                {
                    new Amazon.BedrockRuntime.Model.ContentBlock { Text = response }
                }
            });

            Console.WriteLine($"AI: {response}\n");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
    }
}

async Task RunBedrockGuardrailsDemo()
{
    try
    {
        var service = new BedrockGuardrailsDemoService();

        Console.WriteLine("=== AWS Bedrock Guardrails Demo ===");
        Console.WriteLine("This demo creates a Guardrail with content filters, topic denial, and PII redaction.");
        Console.WriteLine("Test it interactively with custom questions.\n");

        // Step 1: Provision guardrail
        var (guardrailId, version) = await service.CreateAndPublishGuardrailAsync();

        // Step 2: Run interactive mode
        await service.RunInteractiveModeAsync(guardrailId, version);

        // Step 3: Cleanup
        await service.DeleteGuardrailAsync(guardrailId);

        Console.WriteLine("=== Demo complete ===");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
    }
}

async Task RunBedrockToolsMovieDemo()
{
    try
    {
        var bedrockToolsService = new BedrockWithConverseToolsService();
        
        string movieQuery = "Inception";
        
        Console.WriteLine($"\nRequesting movie info for: {movieQuery}...\n");
        
        var result = await bedrockToolsService.GetMovieDetailsAsJson(movieQuery);
        
        if (string.IsNullOrEmpty(result))
        {
            Console.WriteLine("No response from Bedrock.");
            return;
        }
        
        Console.WriteLine("=== Movie Information (Structured JSON) ===\n");
        Console.WriteLine(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
    }
}