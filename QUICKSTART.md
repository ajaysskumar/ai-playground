# Quick Start Guide

Get up and running with the AI Demos application in minutes.

## Prerequisites Checklist

- [ ] .NET 8.0 SDK installed (`dotnet --version`)
- [ ] AWS Account with Bedrock enabled
- [ ] AWS credentials configured

## Step 1: Clone/Setup Project

The project is located at:
```
/Users/ajaykumar/Projects/Personal/ai-demos
```

## Step 2: Install NuGet Packages

```bash
cd /Users/ajaykumar/Projects/Personal/ai-demos
dotnet restore
```

## Step 3: Configure AWS Credentials

### Quick Option: Environment Variables

```bash
export AWS_REGION=us-east-1
export AWS_ACCESS_KEY_ID=your_key_here
export AWS_SECRET_ACCESS_KEY=your_secret_here
```

### Recommended Option: AWS CLI

```bash
aws configure
```

Then enter your credentials when prompted.

## Step 4: Run the Application

### Run the Movie Demo

```bash
cd /Users/ajaykumar/Projects/Personal/ai-demos
dotnet run -- --demo bedrock-movie
```

The application will prompt you to enter a movie title. Try:
- `Matrix`
- `Inception`
- `Avengers`

### Run with Default Settings

```bash
dotnet run
```

This runs `bedrock-movie` by default.

## Expected Output

```
Starting AI Demo: bedrock-movie

Enter movie title or part of title (e.g., 'Matrix', 'Inception'):
The Matrix

Searching for movies matching: The Matrix...

=== Movie Details ===

Title: The Matrix
Release Date: 1999
Summary: A computer hacker learns...
Directors: Lana Wachowski, Lilly Wachowski
Actors: Keanu Reeves, Laurence Fishburne, Carrie-Anne Moss
```

## Troubleshooting

### Error: "The AWS Access Key ID is invalid"
- Verify AWS credentials with: `aws sts get-caller-identity`
- Re-run: `aws configure` to update credentials

### Error: "User is not authorized to perform: bedrock:InvokeModel"
- Check AWS IAM permissions include Bedrock access
- Verify your user/role has `bedrock:InvokeModel` permission

### Build Error: "The type or namespace name 'AIDemos' could not be found"
- Ensure you're in the correct directory
- Run: `dotnet build` to verify project builds
- Check that all files are created in the correct locations

### No Results Returned
- Try a longer, more specific movie title
- Check internet connectivity
- Verify AWS region supports Bedrock

## Project Structure After Setup

```
/Users/ajaykumar/Projects/Personal/ai-demos/
├── bin/                    # Build output (auto-generated)
├── obj/                    # Build artifacts (auto-generated)
├── Properties/
│   └── launchSettings.json
├── Models/
│   └── Movie.cs
├── Services/
│   └── BedrockService.cs
├── AIDemos.csproj
├── ArgumentParser.cs
├── Program.cs
├── README.md
├── QUICKSTART.md          # This file
├── .gitignore
├── .env.example
└── .vs/                   # Visual Studio cache (auto-generated)
```

## Next Steps

1. **Explore the Code**: Start with [Program.cs](Program.cs)
2. **Add More Demos**: Create new services in `Services/` folder
3. **Customize Models**: Modify [Models/Movie.cs](Models/Movie.cs) for different domains
4. **Review Documentation**: See [README.md](README.md) for full details

## Common Commands

```bash
# Build the project
dotnet build

# Clean build output
dotnet clean

# Run tests (if added)
dotnet test

# Publish for release
dotnet publish -c Release

# View installed packages
dotnet list package
```

## Support

For issues with AWS Bedrock:
- [AWS Bedrock Documentation](https://docs.aws.amazon.com/bedrock/)
- [AWS SDK for .NET](https://docs.aws.amazon.com/sdk-for-net/)

For .NET issues:
- [Microsoft .NET Documentation](https://docs.microsoft.com/en-us/dotnet/)

## Environment Setup Tips

### macOS

```bash
# Install .NET 8 using Homebrew
brew install dotnet

# Verify installation
dotnet --version

# Set AWS credentials for current session
export AWS_REGION=us-east-1
export AWS_ACCESS_KEY_ID=your_key
export AWS_SECRET_ACCESS_KEY=your_secret
```

### Keep Credentials Safe

Never commit `.env` file with real credentials to git:
- Always use `.env.example` as template
- Add `.env` to `.gitignore`
- Use AWS IAM roles when possible
