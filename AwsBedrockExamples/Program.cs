using AwsBedrockExamples.Services;

ArgumentParser parser = new ArgumentParser(args);
string demoType = parser.GetDemo();

Console.WriteLine($"Starting AI Demo: {demoType}\n");

switch (demoType.ToLower())
{
    case "bedrock-movie":
        await RunBedrockMovieDemo();
        break;
    case "bedrock-tools-movie":
        await RunBedrockToolsMovieDemo();
        break;
    default:
        Console.WriteLine("Available demos:");
        Console.WriteLine("  dotnet run -- --demo bedrock-movie");
        Console.WriteLine("  dotnet run -- --demo bedrock-tools-movie");
        break;
}

async Task RunBedrockMovieDemo()
{
    try
    {
        var bedrockService = new BedrockService();
        
        Console.WriteLine("Enter movie title or part of title (e.g., 'Matrix', 'Inception'):");
        string movieQuery = "Matrix";
        
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

async Task RunBedrockToolsMovieDemo()
{
    try
    {
        var bedrockToolsService = new BedrockWithConverseToolsService();
        
        Console.WriteLine("Enter movie title (e.g., 'The Matrix', 'Inception'):");
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