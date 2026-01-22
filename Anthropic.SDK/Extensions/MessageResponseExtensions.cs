using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Extensions
{
    /// <summary>
    /// Extension methods for MessageResponse to simplify common operations.
    /// </summary>
    public static class MessageResponseExtensions
    {
        /// <summary>
        /// Downloads all file outputs from bash code execution results to the specified directory path.
        /// This is a convenience method that automatically iterates through response content,
        /// identifies file outputs, and downloads them using the Files API.
        /// </summary>
        /// <param name="response">The message response containing potential file outputs.</param>
        /// <param name="client">The AnthropicClient instance to use for downloading files.</param>
        /// <param name="outputPath">The directory path where files should be downloaded. If the directory doesn't exist, it will be created.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A list of full file paths for all downloaded files.</returns>
        /// <exception cref="ArgumentNullException">Thrown when response, client, or outputPath is null or empty.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when the output directory cannot be created.</exception>
        /// <example>
        /// <code>
        /// var response = await client.Messages.GetClaudeMessageAsync(parameters);
        /// var downloadedFiles = await response.DownloadFilesAsync(client, "C:\\Downloads\\SkillOutputs");
        /// foreach (var filePath in downloadedFiles)
        /// {
        ///     Console.WriteLine($"Downloaded: {filePath}");
        /// }
        /// </code>
        /// </example>
        public static async Task<List<string>> DownloadFilesAsync(
            this MessageResponse response,
            AnthropicClient client,
            string outputPath,
            CancellationToken cancellationToken = default)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentNullException(nameof(outputPath));
            }

            // Ensure output directory exists
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            var downloadedFiles = new List<string>();

            // Iterate through content looking for bash code execution results
            foreach (var content in response.Content)
            {
                if (content is BashCodeExecutionToolResultContent bashResult)
                {
                    if (bashResult.Content is BashCodeExecutionResultContent result)
                    {
                        // Process all file outputs
                        foreach (var output in result.Content)
                        {
                            if (!string.IsNullOrWhiteSpace(output.FileId))
                            {
                                try
                                {
                                    // Get file metadata to retrieve the original filename
                                    var metadata = await client.Files.GetFileMetadataAsync(
                                        output.FileId, 
                                        cancellationToken);

                                    // Construct the full output file path
                                    var fileName = metadata?.Filename ?? $"file_{output.FileId}";
                                    var fullPath = Path.Combine(outputPath, fileName);

                                    // Download the file
                                    await client.Files.DownloadFileAsync(
                                        output.FileId, 
                                        fullPath, 
                                        cancellationToken);

                                    downloadedFiles.Add(fullPath);
                                }
                                catch (Exception ex)
                                {
                                    // Log the error but continue with other files
                                    // Consider adding logging here or rethrowing based on your needs
                                    Console.Error.WriteLine(
                                        $"Failed to download file {output.FileId}: {ex.Message}");
                                }
                            }
                        }
                    }
                }
            }

            return downloadedFiles;
        }

        /// <summary>
        /// Gets all file IDs from bash code execution results in the response.
        /// This is useful if you want to handle file downloads manually or perform other operations with the file IDs.
        /// </summary>
        /// <param name="response">The message response to extract file IDs from.</param>
        /// <returns>A list of file IDs found in the response.</returns>
        /// <example>
        /// <code>
        /// var response = await client.Messages.GetClaudeMessageAsync(parameters);
        /// var fileIds = response.GetFileIds();
        /// foreach (var fileId in fileIds)
        /// {
        ///     var metadata = await client.Files.GetFileMetadataAsync(fileId);
        ///     Console.WriteLine($"File: {metadata.Filename} ({metadata.SizeBytes} bytes)");
        /// }
        /// </code>
        /// </example>
        public static List<string> GetFileIds(this MessageResponse response)
        {
            if (response?.Content == null)
            {
                return new List<string>();
            }

            var fileIds = new List<string>();

            foreach (var content in response.Content)
            {
                if (content is BashCodeExecutionToolResultContent bashResult)
                {
                    if (bashResult.Content is BashCodeExecutionResultContent result && result.Content != null)
                    {
                        fileIds.AddRange(result.Content
                            .Where(o => !string.IsNullOrWhiteSpace(o.FileId))
                            .Select(o => o.FileId));
                    }
                }
            }

            return fileIds;
        }
    }
}
