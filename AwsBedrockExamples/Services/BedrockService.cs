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
                        Text = $"Tell me about the movie: {movieQuery}" 
                    }
                }
            }
        };

        // Create the Converse API request (basic example without tools)
        var converseRequest = new ConverseRequest
        {
            System = [ new SystemContentBlock
            {
                Text = $"Return the results in JSON format with title, year, director, and plot summary. The fields should be: title, year, director, plot. The response should only contain the JSON array object. If no movie is found, return an empty JSON array. If multiple movies match, return details. The response should be a strict JSON array. Nothing else."
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

        return responseText;
    }
}