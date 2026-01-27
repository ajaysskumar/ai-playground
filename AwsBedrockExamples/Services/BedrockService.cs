using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using System.Collections.Generic;
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
        // Create a simple message
        var messages = new List<Message>
        {
            new Message
            {
                Role = "user",
                Content = new List<ContentBlock>
                {
                    new ContentBlock 
                    { 
                        Text = $"Provide detailed information about the movie '{movieQuery}'. Return the results as a JSON array of objects with the following fields: title (string), year (integer), category (string, Movie/TV), directors (array of strings), actors (array of strings), plot (string), genre (string), rating (string). If no movie is found, return an empty JSON array. The response should be a strict JSON array and nothing else. If multiple movies match, return all of them in the array."
                    }
                }
            }
        };

        // Create the Converse API request (basic example without tools)
        var converseRequest = new ConverseRequest
        {
            System = [ new SystemContentBlock
            {
                Text = "You are a movie information expert. Always return the results as a JSON array of objects with the following fields: title, year, category, directors, actors, plot, genre, rating. The response should be a strict JSON array and nothing else."
            }],
            ModelId = ModelId,
            Messages = messages
        };

        // Send the request to Bedrock
        var response = await _client.ConverseAsync(converseRequest);

        // Extract the text response
        var responseText = "";
        foreach (var content in response.Output.Message.Content)
        {
            if (content.Text != null)
            {
                responseText += content.Text;
            }
        }

        // Optionally, validate/parse the response to ensure it is a JSON array (optional, can be added for robustness)
        return responseText;
    }
}