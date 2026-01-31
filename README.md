# AI Demos

This project is a collection of .NET demos showcasing integration with AWS Bedrock and related AI services. It is organized for clarity and maintainability, with source code under the `src` directory.

## Folder Structure
- `src/` - Main source code
  - `Models/` - Data models (e.g., Movie)
  - `Services/` - Service classes for Bedrock and related tools
  - `Properties/` - Project properties and launch settings
  - `ArgumentParser.cs` - Command-line argument parsing
  - `Program.cs` - Main entry point
  - `AIDemos.csproj` - Project file

## Authentication to Bedrock
Authentication is handled via a `.credentials` file in your home directory. This file should contain your AWS credentials in the following format:

```
[default]
aws_access_key_id=YOUR_ACCESS_KEY
aws_secret_access_key=YOUR_SECRET_KEY
```

The services read these credentials to authenticate requests to AWS Bedrock. Make sure this file is present and correctly configured before running the demos.

## Services Overview
 - **BedrockService**: Handles basic interactions with AWS Bedrock, such as sending prompts and receiving responses.
 - **BedrockWithConverseToolsService**: Extends BedrockService to support advanced scenarios, including tool use and multi-step conversations.
 - **BedrockCustomerSupportService**: Demonstrates an AI-powered customer support staff using Bedrock's Converse API. Supports multi-turn, context-aware chat interactions.

## How to Run

### Prerequisites
- .NET 8.0 or later
- AWS credentials configured in `.credentials`

### Build the Project
```sh
dotnet build
```

### Run the Project
The main entry point is `Program.cs`. You can run the project with different arguments to trigger various use cases:

#### Example: Basic Bedrock Prompt
```sh
dotnet run --project AwsBedrockExamples -- --demo bedrock-movie
```

#### Example: Use Bedrock with Converse Tools
```sh
dotnet run --project AwsBedrockExamples -- --demo bedrock-movie-converse-tools
```

#### Example: AI Customer Support Chat
```sh
dotnet run --project AwsBedrockExamples -- --demo bedrock-customer-support
```
This will start an interactive chat session with an AI customer support agent. Type your questions and issues, and enter `Exit Chat` to end the conversation.

### Command-Line Arguments
 - `--demo` - Runs a demo. Acceptable values are:
   - `bedrock-movie` - Basic movie info demo
   - `bedrock-movie-converse-tools` - Structured movie info with tools
   - `bedrock-customer-support` - Interactive customer support chat demo
- Additional arguments may be supported; see `ArgumentParser.cs` for details

## Notes
- Ensure your `.credentials` file is secure and not checked into version control.
