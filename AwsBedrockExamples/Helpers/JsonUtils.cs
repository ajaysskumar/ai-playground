using System;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AwsBedrockExamples.Helpers
{
    public static class JsonUtils
    {
        /// <summary>
        /// Parses a JSON-escaped string and returns formatted (pretty-printed) JSON.
        /// </summary>
        /// <param name="escapedJson">The JSON-escaped string.</param>
        /// <returns>Formatted JSON string.</returns>
        public static string ParseEscapedJson(string escapedJson)
        {
            if (string.IsNullOrWhiteSpace(escapedJson))
                return string.Empty;

            // Unescape Unicode and special characters
            string unescaped = Regex.Unescape(escapedJson);

            // Remove extra escape characters (e.g., \" to ")
            unescaped = unescaped.Replace("\\\"", "\"");

            // Parse and format JSON
            try
            {
                using var doc = JsonDocument.Parse(unescaped);
                return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch (Exception ex)
            {
                // Return unescaped string if parsing fails
                return $"Error parsing JSON: {ex.Message}\n{unescaped}";
            }
        }
    }
}
