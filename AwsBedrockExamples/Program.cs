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
    default:
        Console.WriteLine("Available demos:");
        Console.WriteLine("  dotnet run -- --demo bedrock-movie");
        Console.WriteLine("  dotnet run -- --demo bedrock-movie-converse-tools");
        Console.WriteLine("  dotnet run -- --demo bedrock-customer-support");
        break;
}

async Task RunBedrockMovieDemo()
{
    try
    {
        var bedrockService = new BedrockService();
        
        string movieQuery = "Lord of the rings";
        
        Console.WriteLine($"\nSearching for: {movieQuery}...\n");
        
        var result = await bedrockService.GetMovieDetails(movieQuery);
        
        if (string.IsNullOrEmpty(result))
        {
            Console.WriteLine("No response from Bedrock.");
            return;
        }
        
        Console.WriteLine("=== Response ===\n");
        Console.WriteLine(result);
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

async Task RunBedrockToolsMovieDemo()
{
    try
    {
        var bedrockToolsService = new BedrockWithConverseToolsService();
        
        string movieQuery = "Lord of the rings";
        
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