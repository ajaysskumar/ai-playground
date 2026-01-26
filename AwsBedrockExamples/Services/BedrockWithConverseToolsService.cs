using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Runtime.Documents;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace AwsBedrockExamples.Services;

public class BedrockWithConverseToolsService
{
    private readonly AmazonBedrockRuntimeClient _client;
    private const string ModelId = "anthropic.claude-3-haiku-20240307-v1:0";

    public BedrockWithConverseToolsService()
    {
        _client = new AmazonBedrockRuntimeClient(Amazon.RegionEndpoint.USEast1);
    }

    public async Task<string> GetMovieDetailsAsJson(string movieQuery)
    {
        // Build the tool configuration - this GUARANTEES the output structure
        var toolConfig = BuildToolConfiguration();

        // Create the user message
        var userMessage = new Message
        {
            Role = "user",
            Content = new List<ContentBlock>
            {
                new ContentBlock
                {
                    Text = $"Provide detailed information about the movie '{movieQuery}'."
                }
            }
        };

        var converseRequest = new ConverseRequest
        {
            ModelId = ModelId,
            Messages = new List<Message> { userMessage },
            ToolConfig = toolConfig,
            System = new List<SystemContentBlock>
            {
                new SystemContentBlock
                {
                    Text = "You are a movie information expert"
                }
            }
        };

        // Send the request to Bedrock
        var response = await _client.ConverseAsync(converseRequest);

        if (response?.Output?.Message?.Content == null)
        {
            return JsonSerializer.Serialize(new { error = "Invalid response from Bedrock" });
        }

        // Find the tool use response - in v4 it's accessed via ToolUse property
        var toolUseBlock = response.Output.Message.Content
            .FirstOrDefault(c => c.ToolUse != null);

        if (toolUseBlock?.ToolUse == null)
        {
            return JsonSerializer.Serialize(new { error = "Model did not use the expected tool" });
        }

        // Extract the structured data from the tool input
        return ExtractMovieInfoAsJson(toolUseBlock.ToolUse.Input);
    }

    private ToolConfiguration BuildToolConfiguration()
    {
        // Define the JSON schema for the tool input - this is what the model MUST follow
        // Root must be object type, with movies array as a property
        var toolInputSchema = new
        {
            type = "object",
            properties = new
            {
                movies = new
                {
                    type = "array",
                    description = "Array of movie information objects",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            title = new
                            {
                                type = "string",
                                description = "The movie title"
                            },
                            year = new
                            {
                                type = "integer",
                                description = "The release year"
                            },
                            category = new
                            {
                                type = "string",
                                description = "Category to which this title belongs to. Like a TV show or Movie"
                            },
                            directors = new
                            {
                                type = "array",
                                description = "Array of director names",
                                items = new
                                {
                                    type = "string",
                                    description = "A director name"
                                }
                            },
                            actors = new
                            {
                                type = "array",
                                description = "Array of actor names",
                                items = new
                                {
                                    type = "string",
                                    description = "An actor name"
                                }
                            },
                            plot = new
                            {
                                type = "string",
                                description = "Brief plot summary"
                            },
                            genre = new
                            {
                                type = "string",
                                description = "Movie genre"
                            },
                            rating = new
                            {
                                type = "string",
                                description = "IMDb rating or similar"
                            }
                        },
                        required = new[] { "title", "year", "category", "directors", "actors", "plot", "genre", "rating" },
                        additionalProperties = false
                    }
                }
            },
            required = new[] { "movies" },
            additionalProperties = false
        };

        var tool = new Tool
        {
            ToolSpec = new ToolSpecification
            {
                Name = "return_movie_info",
                Description = "Return structured movie information as an array to handle multiple matching movies",
                InputSchema = new ToolInputSchema
                {
                    Json = Document.FromObject(toolInputSchema)
                }
            }
        };

        return new ToolConfiguration
        {
            Tools = new List<Tool> { tool },
            ToolChoice = new ToolChoice
            {
                Tool = new SpecificToolChoice
                {
                    Name = "return_movie_info"
                }
            }
        };
    }

    /// <summary>
    /// Extracts movie info from the tool input Document and returns as formatted JSON
    /// The schema GUARANTEES this will be an object with a movies array
    /// </summary>
    private string ExtractMovieInfoAsJson(Document toolInput)
    {
        try
        {
            // The root is now an object with a movies property
            // var inputDictList = toolInput.Cast<List<MovieDto>>();
            // Console.WriteLine($"Extracted {inputDictList?.Count()} movies from tool input.");

            var inputDict = toolInput.AsDictionary();
            
            if (!inputDict.ContainsKey("movies"))
            {
                return JsonSerializer.Serialize(new { error = "Expected 'movies' property in response" });
            }

            var moviesDoc = inputDict["movies"];
            if (!moviesDoc.IsList())
            {
                return JsonSerializer.Serialize(new { error = "Expected 'movies' to be an array" });
            }

            var moviesList = moviesDoc.AsList();
            var movies = new List<object>();

            foreach (var movieDoc in moviesList)
            {
                var movieDict = movieDoc.AsDictionary();

                // Extract array fields - convert Document array to string array
                var directors = new List<string>();
                if (movieDict.ContainsKey("directors") && movieDict["directors"].IsList())
                {
                    directors = movieDict["directors"].AsList()
                        .Select(doc => doc.AsString())
                        .ToList();
                }

                var actors = new List<string>();
                if (movieDict.ContainsKey("actors") && movieDict["actors"].IsList())
                {
                    actors = movieDict["actors"].AsList()
                        .Select(doc => doc.AsString())
                        .ToList();
                }

                // Extract each field - they're all guaranteed to exist per schema
                var movieData = new
                {
                    title = movieDict.ContainsKey("title") ? movieDict["title"].AsString() : "",
                    year = movieDict.ContainsKey("year") ? movieDict["year"].AsInt() : 0,
                    category = movieDict.ContainsKey("category") ? movieDict["category"].AsString() : "",
                    directors = directors,
                    actors = actors,
                    plot = movieDict.ContainsKey("plot") ? movieDict["plot"].AsString() : "",
                    genre = movieDict.ContainsKey("genre") ? movieDict["genre"].AsString() : "",
                    rating = movieDict.ContainsKey("rating") ? movieDict["rating"].AsString() : ""
                };

                movies.Add(movieData);
            }

            return JsonSerializer.Serialize(movies, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Failed to extract movie info: {ex.Message}" });
        }
    }
}
