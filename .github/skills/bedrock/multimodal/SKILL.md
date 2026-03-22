---
name: multimodal
description: 'Process images, documents, and videos alongside text using AWS Bedrock multimodal foundation models (Claude 3/3.5, Llama 3.2 Vision, Amazon Nova). Use when: analyzing images, extracting information from PDFs or screenshots, describing visual content, or combining image and text inputs in a single model call. Covers image encoding, content block construction, supported formats, file size limits, and document understanding patterns in C#, Python, or TypeScript.'
argument-hint: 'Describe the multimodal task and input types (image, document, video)'
---

# AWS Bedrock Multimodal

## When to Use
- Sending images alongside text to a foundation model
- Analyzing screenshots, charts, diagrams, or photographs
- Extracting structured data from PDFs or scanned documents
- Describing, captioning, or comparing images
- Visual question answering (VQA)
- Document understanding and layout analysis

## Supported Models and Modalities

| Model | Images | Documents (PDF) | Video | Max Images |
|---|---|---|---|---|
| Claude 3.5 Sonnet v2 | ✅ | ✅ | ❌ | 20 per request |
| Claude 3 Haiku | ✅ | ✅ | ❌ | 20 per request |
| Claude 3 Opus | ✅ | ✅ | ❌ | 20 per request |
| Llama 3.2 Vision (11B/90B) | ✅ | ❌ | ❌ | 1 per request |
| Amazon Nova Pro | ✅ | ✅ | ✅ | 20 per request |
| Amazon Nova Lite | ✅ | ✅ | ✅ | 20 per request |

## Supported Image Formats

`jpeg`, `png`, `gif`, `webp`

Max image size: **3.75 MB** per image (after base64 encoding adds ~33% overhead, keep source under ~2.8 MB).

## Procedure

### 1. Inline Image (Base64)

Load the image from disk and base64-encode it:

```csharp
byte[] imageBytes = await File.ReadAllBytesAsync("diagram.png");
string base64Image = Convert.ToBase64String(imageBytes);

var response = await client.ConverseAsync(new ConverseRequest
{
    ModelId = "anthropic.claude-3-5-sonnet-20241022-v2:0",
    Messages = new List<Message>
    {
        new Message
        {
            Role = "user",
            Content = new List<ContentBlock>
            {
                new ContentBlock
                {
                    Image = new ImageBlock
                    {
                        Format = ImageFormat.Png,
                        Source = new ImageSource
                        {
                            Bytes = new MemoryStream(imageBytes)  // SDK accepts raw bytes
                        }
                    }
                },
                new ContentBlock
                {
                    Text = "Describe the architecture shown in this diagram."
                }
            }
        }
    }
});
```

### 2. Image via S3 URL

For large images or video (Nova models), use S3 URI instead of inline bytes:

```csharp
new ContentBlock
{
    Image = new ImageBlock
    {
        Format = ImageFormat.Jpeg,
        Source = new ImageSource
        {
            S3Location = new S3Location
            {
                Uri = "s3://my-bucket/images/photo.jpg",
                // BucketOwner = "123456789012"  // Optional, for cross-account
            }
        }
    }
}
```

> Model execution role must have `s3:GetObject` on the bucket.

### 3. Document Input (PDF)

```csharp
byte[] pdfBytes = await File.ReadAllBytesAsync("report.pdf");

new ContentBlock
{
    Document = new DocumentBlock
    {
        Format = DocumentFormat.Pdf,
        Name = "quarterly-report",       // Logical name for model reference
        Source = new DocumentSource
        {
            Bytes = new MemoryStream(pdfBytes)
        }
    }
}
```

### 4. Multiple Images in One Request

```csharp
var content = new List<ContentBlock>();

foreach (var imagePath in imagePaths)   // Up to 20
{
    content.Add(new ContentBlock
    {
        Image = new ImageBlock
        {
            Format = ImageFormat.Png,
            Source = new ImageSource { Bytes = new MemoryStream(await File.ReadAllBytesAsync(imagePath)) }
        }
    });
}

content.Add(new ContentBlock { Text = "Which of these images shows a bar chart?" });
```

### 5. Video Input (Amazon Nova)

```csharp
new ContentBlock
{
    Video = new VideoBlock
    {
        Format = VideoFormat.Mp4,
        Source = new VideoSource
        {
            S3Location = new S3Location { Uri = "s3://my-bucket/videos/demo.mp4" }
        }
    }
}
```

## Common Patterns

- **OCR / Text extraction**: Send a scanned image + "Extract all text from this image."
- **Structured data from PDF**: Send PDF + "Extract a JSON object with fields: date, amount, vendor."
- **Image comparison**: Send two images + "What are the key differences between these two diagrams?"
- **Chart analysis**: Send a chart image + "Summarize the trends shown in this chart."

## Best Practices

- Place text instructions **after** image/document blocks for best model attention.
- Resize images to the minimum needed resolution — lower resolution reduces token cost.
- Use `Name` on `DocumentBlock` to let the model reference documents by name in multi-doc scenarios.
- For large files, prefer S3 URIs over inline bytes to stay within Lambda/API Gateway payload limits.
- Test with Claude models first — they have the broadest document understanding support.

## Size and Limit Reference

| Resource | Limit |
|---|---|
| Image size (inline) | 3.75 MB |
| Images per request | 20 |
| PDF pages | 100 |
| Video length (Nova) | 30 minutes |
| Total request payload | 10 MB (API Gateway limit) |

## References

- [Multimodal Converse API Guide](./references/multimodal-converse.md)
- [Supported File Formats](./references/supported-formats.md)
- [S3 Access Setup for Bedrock](./references/s3-access.md)
