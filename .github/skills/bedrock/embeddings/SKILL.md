---
name: embeddings
description: 'Generate vector embeddings using AWS Bedrock embedding models (Amazon Titan Embeddings, Cohere Embed). Use when: implementing semantic search, RAG (Retrieval-Augmented Generation) pipelines, document similarity, clustering, or building vector stores. Covers InvokeModel payload construction, embedding model selection, chunking strategies, and integration with vector databases (OpenSearch, Pinecone, pgvector) in C#, Python, or TypeScript.'
argument-hint: 'Describe the embedding use case and target model or vector store'
---

# AWS Bedrock Embeddings

## When to Use
- Generating vector representations of text for semantic search
- Building or querying RAG pipelines
- Computing similarity between documents or queries
- Populating or querying vector databases (OpenSearch Serverless, Pinecone, pgvector)
- Clustering or classification using embeddings

## Supported Embedding Models

| Model ID | Provider | Dimensions | Max Tokens | Best For |
|---|---|---|---|---|
| `amazon.titan-embed-text-v2:0` | Amazon | 1024 (configurable: 256, 512) | 8192 | General purpose, multilingual |
| `amazon.titan-embed-image-v1` | Amazon | 1024 | N/A | Image + text embeddings |
| `cohere.embed-english-v3` | Cohere | 1024 | 512 | English retrieval, RAG |
| `cohere.embed-multilingual-v3` | Cohere | 1024 | 512 | Multilingual retrieval |

## Procedure

### 1. Chunk Your Input

- Embedding models have token limits; split large documents before embedding.
- Recommended chunk size: 256–512 tokens with 10–20% overlap.
- Use sentence boundaries where possible for semantic coherence.

```csharp
// Simple fixed-size chunker
IEnumerable<string> Chunk(string text, int maxChars = 1000, int overlap = 100)
{
    for (int i = 0; i < text.Length; i += maxChars - overlap)
        yield return text.Substring(i, Math.Min(maxChars, text.Length - i));
}
```

### 2. Call Titan Embed (InvokeModel)

Titan Embeddings uses `InvokeModelAsync` with a JSON payload:

```csharp
var client = new AmazonBedrockRuntimeClient(RegionEndpoint.USEast1);

var payload = JsonSerializer.Serialize(new
{
    inputText = "The quick brown fox jumps over the lazy dog.",
    dimensions = 1024,          // 256 | 512 | 1024 (Titan v2 only)
    normalize = true            // Normalize output vector
});

var response = await client.InvokeModelAsync(new InvokeModelRequest
{
    ModelId = "amazon.titan-embed-text-v2:0",
    ContentType = "application/json",
    Accept = "application/json",
    Body = new MemoryStream(Encoding.UTF8.GetBytes(payload))
});

using var doc = JsonDocument.Parse(response.Body);
float[] embedding = doc.RootElement
    .GetProperty("embedding")
    .EnumerateArray()
    .Select(e => e.GetSingle())
    .ToArray();
```

### 3. Call Cohere Embed (InvokeModel)

```csharp
var payload = JsonSerializer.Serialize(new
{
    texts = new[] { "query text here" },
    input_type = "search_query",   // "search_document" for indexing, "search_query" for queries
    truncate = "END"
});

var response = await client.InvokeModelAsync(new InvokeModelRequest
{
    ModelId = "cohere.embed-english-v3",
    ContentType = "application/json",
    Accept = "application/json",
    Body = new MemoryStream(Encoding.UTF8.GetBytes(payload))
});

using var doc = JsonDocument.Parse(response.Body);
var embeddings = doc.RootElement.GetProperty("embeddings")[0]; // First text's embedding
```

### 4. Cohere `input_type` Values

| Value | Use |
|---|---|
| `search_document` | When indexing documents into a vector store |
| `search_query` | When embedding a user query for retrieval |
| `classification` | For classification tasks |
| `clustering` | For clustering tasks |

### 5. Store and Query Embeddings

```csharp
// Example: cosine similarity (in-memory)
double CosineSimilarity(float[] a, float[] b)
{
    double dot = a.Zip(b, (x, y) => x * (double)y).Sum();
    double normA = Math.Sqrt(a.Sum(x => x * (double)x));
    double normB = Math.Sqrt(b.Sum(x => x * (double)x));
    return dot / (normA * normB);
}
```

For production, integrate with:
- **Amazon OpenSearch Serverless** (kNN index)
- **pgvector** on Amazon Aurora/RDS
- **Pinecone / Weaviate / Qdrant** via SDK

### 6. RAG Pipeline Pattern

```
User query
    → Embed query (search_query)
    → ANN search in vector store → top-K chunks
    → Inject chunks into Converse API prompt as context
    → Return grounded response
```

## Best Practices

- Always use `normalize = true` (Titan) for cosine similarity — avoids magnitude bias.
- Index with `search_document` and query with `search_query` for Cohere models.
- Cache embeddings for static content — embeddings are deterministic for the same model.
- Batch embed documents during ingestion to reduce API call overhead.

## References

- [Titan Embeddings Model Card](./references/titan-embeddings.md)
- [Cohere Embed Model Card](./references/cohere-embed.md)
- [RAG Pattern Guide](./references/rag-pattern.md)
