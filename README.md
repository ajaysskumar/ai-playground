# AI Demos - .NET Console Application

A .NET console application demonstrating various AI use cases including AWS Bedrock integration.

## Overview

This application provides a flexible platform for running different AI demos via command-line parameters. Currently supports AWS Bedrock with tool use for movie information retrieval.

## Prerequisites

- .NET 8.0 SDK
- AWS Account with Bedrock access
- AWS CLI configured with credentials or environment variables set

## Setup

### 1. Install Dependencies

```bash
cd /Users/ajaykumar/Projects/Personal/ai-demos
dotnet restore
```

### 2. Configure AWS Credentials

Set up AWS credentials using one of these methods:

**Option A: AWS CLI Configuration**
```bash
aws configure
```

**Option B: Environment Variables**
```bash
export AWS_ACCESS_KEY_ID=your_access_key
export AWS_SECRET_ACCESS_KEY=your_secret_key
export AWS_REGION=us-east-1
```

**Option C: IAM Role** (if running on AWS infrastructure)
No additional configuration needed; the application will use the IAM role.

## Project Structure

```
/Users/ajaykumar/Projects/Personal/ai-demos/
├── AIDemos.csproj           # Project file with NuGet dependencies
├── Program.cs               # Main entry point and demo orchestration
├── ArgumentParser.cs        # Command-line argument parsing
├── Models/
│   └── Movie.cs            # Movie data models
├── Services/
│   └── BedrockService.cs   # AWS Bedrock integration with tool use
├── README.md               # This file
└── .gitignore              # Git ignore rules
```

## Available Demos

### 1. Bedrock Movie Search (`bedrock-movie`)

Search for movie information using AWS Bedrock with Claude 3.5 Sonnet and tool use.

**Features:**
- Search for movies by title or partial title
- Retrieve movie summaries
- Get directors list
- Get actors list
- Display release dates
- Return multiple matches

**Usage:**
```bash
dotnet run -- --demo bedrock-movie
```

Then enter a movie title when prompted (e.g., "Matrix", "Inception", "Avengers").

**Example Output:**
```
Starting AI Demo: bedrock-movie

Enter movie title or part of title (e.g., 'Matrix', 'Inception'):
Matrix

Searching for movies matching: Matrix...

=== Movie Details ===

Title: The Matrix
Release Date: 1999-03-31
Summary: A computer programmer discovers that reality as he knows it is a simulation...
Directors: Lana Wachowski, Lilly Wachowski
Actors: Keanu Reeves, Laurence Fishburne, Carrie-Anne Moss
```

## Usage

Run the application with different demos:

```bash
# Run the default demo (bedrock-movie)
dotnet run

# Run with explicit demo parameter
dotnet run -- --demo bedrock-movie
```

## Architecture

### BedrockService

The `BedrockService` class handles:
1. **Tool Definition**: Creates a `movie_lookup` tool specification for Claude
2. **API Calls**: Uses Bedrock Runtime's `ConverseAsync` API for tool use
3. **Response Processing**: Parses Claude's responses to extract structured movie data

### Tool Use Flow

1. User enters a movie query
2. Application sends query to Claude via Bedrock with `movie_lookup` tool definition
3. Claude uses the tool to search for movie information
4. Application parses the structured response
5. Movie details are displayed to the user

## Configuration

### Model ID

The application uses: `anthropic.claude-3-5-sonnet-20241022`

To use a different model, update the `ModelId` constant in [Services/BedrockService.cs](Services/BedrockService.cs):

```csharp
private const string ModelId = "anthropic.claude-3-5-sonnet-20241022";
```

### AWS Region

Default region: `us-east-1`

Modify in [Services/BedrockService.cs](Services/BedrockService.cs):

```csharp
_client = new AmazonBedrockRuntimeClient(Amazon.RegionEndpoint.USEast1);
```

## Extending the Application

To add a new demo:

1. Create a new service class in the `Services/` folder
2. Add a new case in the switch statement in [Program.cs](Program.cs)
3. Implement your demo logic

Example:
```csharp
case "new-demo":
    await RunNewDemo();
    break;
```

## Troubleshooting

### AWS Credentials Not Found
- Verify AWS credentials are properly configured
- Check environment variables: `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`
- Ensure IAM permissions include Bedrock access

### Model Not Found
- Verify Bedrock is available in your AWS region
- Check that you have access to the specified model
- Use AWS Console to verify Bedrock model access

### No Results Returned
- Try a more specific movie title
- Verify the query is in English
- Check token limits and API response sizes

## NuGet Dependencies

- `AWSSDK.Bedrock` - AWS Bedrock API
- `AWSSDK.BedrockRuntime` - AWS Bedrock Runtime for inference
- `System.Text.Json` - JSON serialization/deserialization

## License

This project is for demonstration purposes.

## Future Enhancements

- Add more AI services (OpenAI, Anthropic, etc.)
- Implement caching for frequently searched movies
- Add configuration file support
- Add CSV/JSON export for results
- Interactive menu system
- Database integration for caching
