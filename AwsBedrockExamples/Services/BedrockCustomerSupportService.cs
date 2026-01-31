using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AwsBedrockExamples.Services;

public class BedrockCustomerSupportService
{
    private readonly AmazonBedrockRuntimeClient _client;
    private const string ModelId = "anthropic.claude-3-haiku-20240307-v1:0";

    public BedrockCustomerSupportService()
    {
        _client = new AmazonBedrockRuntimeClient(Amazon.RegionEndpoint.USEast1);
    }

    /// <summary>
    /// Simulates an AI customer support staff in a multi-turn conversation using the Bedrock Converse API (no tools).
    /// </summary>
    /// <param name="conversationHistory">The full conversation history as a list of Message objects (user and assistant turns).</param>
    /// <returns>AI-generated customer support response as plain text.</returns>
    public async Task<string> GetSupportResponse(List<Message> conversationHistory)
    {
        var converseRequest = new ConverseRequest
        {
            System = new List<SystemContentBlock>
            {
                new SystemContentBlock
                {
                    Text = "You are an AI customer support staff. Be polite, helpful, and concise. Address the customer's issue and provide clear next steps or solutions. Maintain context across the conversation."
                }
            },
            ModelId = ModelId,
            Messages = conversationHistory
        };

        var response = await _client.ConverseAsync(converseRequest);

        var responseText = string.Empty;
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
