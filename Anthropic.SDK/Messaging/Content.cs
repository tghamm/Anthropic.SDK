using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Anthropic.SDK.Messaging
{
    public abstract class ContentBase
    {
        /// <summary>
        /// Type of Content (Text, pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public abstract ContentType Type { get; }
    }


    
    
    /// <summary>
    /// Helper Class for Text Content to Send to Claude
    /// </summary>
    public class TextContent: ContentBase
    {
        /// <summary>
        /// Type of Content (Text, pre-set)
        /// </summary>
        [JsonPropertyName("type")] 
        public override ContentType Type => ContentType.text;

        /// <summary>
        /// Text to send to Claude in a Block
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    /// <summary>
    /// Helper Class for Image Content to Send to Claude
    /// </summary>
    public class ImageContent: ContentBase
    {
        /// <summary>
        /// Type of Content (Image, pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public override ContentType Type => ContentType.image;

        /// <summary>
        /// Source of Image
        /// </summary>
        [JsonPropertyName("source")]
        public ImageSource Source { get; set; }
    }

    /// <summary>
    /// Image Format Types
    /// </summary>
    public static class ImageSourceType
    {
        /// <summary>
        /// Base 64 Image Type
        /// </summary>
        public static string Base64 => "base64";
    }

    /// <summary>
    /// Definition of image to be sent to Claude
    /// </summary>
    public class ImageSource
    {
        /// <summary>
        /// Image data format (pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public string Type => ImageSourceType.Base64;

        /// <summary>
        /// Image format
        /// </summary>
        [JsonPropertyName("media_type")]
        public string MediaType { get; set; }

        /// <summary>
        /// Base 64 image data
        /// </summary>
        [JsonPropertyName("data")]
        public string Data { get; set; }
    }
}
