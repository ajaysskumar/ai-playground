# AI Demos — Copilot Instructions

## Project Overview

This repository contains C#/.NET demos for AI services, currently focused on AWS Bedrock. The primary project is `AwsBedrockExamples/`.

## Tech Stack

- **Language**: C# / .NET 10
- **AI SDK**: AWSSDK.BedrockRuntime, AWSSDK.Bedrock
- **Primary region**: `us-east-1`

## Architecture

- `Services/` — One service class per Bedrock capability
- `Helpers/` — Shared utilities (argument parsing, JSON formatting)
- `Models/` — Domain models
- `Program.cs` — Entry point; demo selected via `--demo <name>` CLI flag

## Available AWS Bedrock Skills

Use the following skills when working on Bedrock-related tasks. Load the skill before implementing.

| Skill | Use When |
|---|---|
| `aws-bedrock-text-generation` | Writing InvokeModel / Converse API calls for text completion, summarization, classification |
| `aws-bedrock-converse-api` | Building multi-turn chat, streaming, or cross-model unified API calls |
| `aws-bedrock-tool-calling` | Implementing function/tool calling, agentic loops, or forced structured JSON output |
| `aws-bedrock-embeddings` | Generating vector embeddings, building RAG pipelines, or querying vector stores |
| `aws-bedrock-guardrails` | Creating or applying Guardrails for content filtering, PII, topic denial, grounding |
| `aws-bedrock-moderation` | Screening inputs/outputs for harmful content via content filters and ApplyGuardrail |
| `aws-bedrock-multimodal` | Processing images, PDFs, or video inputs alongside text |

Skill files are located at `.github/skills/bedrock/<skill-name>/SKILL.md`.

## Conventions

- Use `ConverseAsync` (Converse API) for new code — not `InvokeModelAsync` — unless raw payload control is required.
- Always handle `ThrottlingException` with exponential backoff.
- Default model: `anthropic.claude-3-haiku-20240307-v1:0` (fast and cheap for demos).
- Prefer `AmazonBedrockRuntimeClient` for inference; `AmazonBedrockClient` for control-plane operations (guardrails, etc.).
- JSON serialization: use `System.Text.Json` (not Newtonsoft).

## Build and Run

```sh
dotnet build
dotnet run --project AwsBedrockExamples -- --demo <demo-name>
```

Available demos: `bedrock-movie`, `bedrock-movie-converse-tools`, `bedrock-customer-support`
