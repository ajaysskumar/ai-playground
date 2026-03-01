using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace AwsBedrockExamples.Services;

public class BedrockService
{
    private readonly AmazonBedrockRuntimeClient _client;
    private const string ModelId = "anthropic.claude-3-haiku-20240307-v1:0";

    public BedrockService()
    {
        _client = new AmazonBedrockRuntimeClient(Amazon.RegionEndpoint.USEast1);
    }

    public async Task<string> GetMovieDetails(string movieQuery)
    {
        // Build the prompt for Claude models: must start with "\n\nHuman: " and end with "\n\nAssistant:"
        var prompt =
            $"\n\nHuman: You are a movie information expert. Provide detailed information about the movie '{movieQuery}'. Return the results as a JSON array of objects with the following fields: title (string), year (integer), category (string, Movie/TV), directors (array of strings), actors (array of strings), plot (string), genre (string), rating (string). If no movie is found, return an empty JSON array. The response should be a strict JSON array and nothing else. If multiple movies match, return all of them in the array.\n\nAssistant:";

        var request = new InvokeModelRequest
        {
            ModelId = ModelId,
            ContentType = "application/json",
            Accept = "application/json",
            Body = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(
        JsonSerializer.Serialize(new
        {
            anthropic_version = "bedrock-2023-05-31",
            max_tokens = 1024,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        })
    ))
        };

        var response = await _client.InvokeModelAsync(request);

        var responseBody = System.Text.Encoding.UTF8.GetString(response.Body.ToArray());

        // Optionally, parse the response to extract the model's answer if wrapped in a JSON object
        // For simplicity, return the raw response body
        return FormatJsonString(responseBody);
    }

    private string FormatJsonString(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        // Try to extract the first JSON array or object from the string
        var match = System.Text.RegularExpressions.Regex.Match(input, @"(\{[\s\S]*\}|\[[\s\S]*\])");
        if (!match.Success)
            return input;
        var jsonContent = match.Value;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(jsonContent);
            return System.Text.Json.JsonSerializer.Serialize(doc.RootElement, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            // If not valid JSON, return as-is
            return jsonContent;
        }
    }
}