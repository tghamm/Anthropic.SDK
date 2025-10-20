using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Anthropic.SDK.Skills
{
    /// <summary>
    /// Skills endpoint for managing custom skills.
    /// The Skills API allows you to create, list, retrieve, and delete custom skills that extend Claude's capabilities.
    /// </summary>
    public class SkillsEndpoint : EndpointBase
    {
        /// <summary>
        /// Constructor of the api endpoint. Rather than instantiating this yourself, access it through an instance of <see cref="AnthropicClient"/> as <see cref="AnthropicClient.Skills"/>.
        /// </summary>
        /// <param name="client"></param>
        internal SkillsEndpoint(AnthropicClient client) : base(client) { }

        protected override string Endpoint => "skills";

        /// <summary>
        /// Creates a new custom skill by uploading skill files.
        /// All files must be in the same top-level directory and must include a SKILL.md file at the root of that directory.
        /// </summary>
        /// <param name="displayTitle">Display title for the skill. This is a human-readable label that is not included in the prompt sent to the model.</param>
        /// <param name="skillDirectoryPath">Path to the directory containing skill files. Must include a SKILL.md file.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The created skill response.</returns>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid or SKILL.md is not found.</exception>
        public async Task<SkillResponse> CreateSkillAsync(
            string displayTitle,
            string skillDirectoryPath,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(skillDirectoryPath))
            {
                throw new ArgumentException("Skill directory path cannot be null or empty.", nameof(skillDirectoryPath));
            }

            if (!Directory.Exists(skillDirectoryPath))
            {
                throw new ArgumentException($"Skill directory not found: {skillDirectoryPath}", nameof(skillDirectoryPath));
            }

            // Verify SKILL.md exists
            var skillMdPath = Path.Combine(skillDirectoryPath, "SKILL.md");
            if (!File.Exists(skillMdPath))
            {
                throw new ArgumentException("SKILL.md file must exist in the skill directory.", nameof(skillDirectoryPath));
            }

            // Get all files in the directory
            var files = Directory.GetFiles(skillDirectoryPath, "*", SearchOption.AllDirectories);
            
            using var content = new MultipartFormDataContent();

            // Add display_title if provided
            if (!string.IsNullOrWhiteSpace(displayTitle))
            {
                content.Add(new StringContent(displayTitle), "display_title");
            }

            // Add all files
            foreach (var filePath in files)
            {
                // Get relative path (compatible with .NET Standard 2.0)
                var relativePath = GetRelativePath(skillDirectoryPath, filePath);
                // Normalize path separators to forward slashes for consistency
                relativePath = relativePath.Replace(Path.DirectorySeparatorChar, '/');
                
#if NET6_0_OR_GREATER
                var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
#else
                var fileBytes = File.ReadAllBytes(filePath);
#endif
                var fileContent = new ByteArrayContent(fileBytes);
                var mimeType = GetMimeType(filePath);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
                
                // Use the relative path as the filename in the multipart form
                content.Add(fileContent, "files[]", relativePath);
            }

            return await HttpRequestSimple<SkillResponse>(Url, HttpMethod.Post, content, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a new custom skill by uploading a zip file.
        /// The zip file must contain a SKILL.md file at its root.
        /// </summary>
        /// <param name="displayTitle">Display title for the skill. This is a human-readable label that is not included in the prompt sent to the model.</param>
        /// <param name="zipFilePath">Path to the zip file containing skill files. Must include a SKILL.md file.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The created skill response.</returns>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
        public async Task<SkillResponse> CreateSkillFromZipAsync(
            string displayTitle,
            string zipFilePath,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(zipFilePath))
            {
                throw new ArgumentException("Zip file path cannot be null or empty.", nameof(zipFilePath));
            }

            if (!File.Exists(zipFilePath))
            {
                throw new ArgumentException($"Zip file not found: {zipFilePath}", nameof(zipFilePath));
            }

#if NET6_0_OR_GREATER
            var fileBytes = await File.ReadAllBytesAsync(zipFilePath, cancellationToken).ConfigureAwait(false);
#else
            var fileBytes = File.ReadAllBytes(zipFilePath);
#endif

            using var content = new MultipartFormDataContent();

            // Add display_title if provided
            if (!string.IsNullOrWhiteSpace(displayTitle))
            {
                content.Add(new StringContent(displayTitle), "display_title");
            }

            // Add zip file
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            content.Add(fileContent, "files[]", Path.GetFileName(zipFilePath));

            return await HttpRequestSimple<SkillResponse>(Url, HttpMethod.Post, content, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a new custom skill by uploading file streams.
        /// Files must include a SKILL.md file.
        /// </summary>
        /// <param name="displayTitle">Display title for the skill. This is a human-readable label that is not included in the prompt sent to the model.</param>
        /// <param name="files">List of tuples containing (filename, stream, mimeType) for each file to upload.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The created skill response.</returns>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
        /// <exception cref="ArgumentNullException">Thrown when files is null or empty.</exception>
        public async Task<SkillResponse> CreateSkillFromStreamsAsync(
            string displayTitle,
            List<(string filename, Stream stream, string mimeType)> files,
            CancellationToken cancellationToken = default)
        {
            if (files == null || !files.Any())
            {
                throw new ArgumentNullException(nameof(files), "Files list cannot be null or empty.");
            }

            // Verify SKILL.md is present
            if (!files.Any(f => f.filename.EndsWith("SKILL.md", StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("Files must include a SKILL.md file.", nameof(files));
            }

            using var content = new MultipartFormDataContent();

            // Add display_title if provided
            if (!string.IsNullOrWhiteSpace(displayTitle))
            {
                content.Add(new StringContent(displayTitle), "display_title");
            }

            // Add all files
            foreach (var (filename, stream, mimeType) in files)
            {
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
                content.Add(streamContent, "files[]", filename);
            }

            return await HttpRequestSimple<SkillResponse>(Url, HttpMethod.Post, content, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a new version of an existing skill by uploading skill files.
        /// All files must be in the same top-level directory and must include a SKILL.md file at the root of that directory.
        /// </summary>
        /// <param name="skillId">The ID of the skill to create a version for.</param>
        /// <param name="skillDirectoryPath">Path to the directory containing skill files. Must include a SKILL.md file.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The created skill version response.</returns>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid or SKILL.md is not found.</exception>
        public async Task<SkillVersionResponse> CreateSkillVersionAsync(
            string skillId,
            string skillDirectoryPath,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                throw new ArgumentNullException(nameof(skillId), "Skill ID cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(skillDirectoryPath))
            {
                throw new ArgumentException("Skill directory path cannot be null or empty.", nameof(skillDirectoryPath));
            }

            if (!Directory.Exists(skillDirectoryPath))
            {
                throw new ArgumentException($"Skill directory not found: {skillDirectoryPath}", nameof(skillDirectoryPath));
            }

            // Verify SKILL.md exists
            var skillMdPath = Path.Combine(skillDirectoryPath, "SKILL.md");
            if (!File.Exists(skillMdPath))
            {
                throw new ArgumentException("SKILL.md file must exist in the skill directory.", nameof(skillDirectoryPath));
            }

            // Get all files in the directory
            var files = Directory.GetFiles(skillDirectoryPath, "*", SearchOption.AllDirectories);
            
            using var content = new MultipartFormDataContent();

            // Add all files
            foreach (var filePath in files)
            {
                // Get relative path (compatible with .NET Standard 2.0)
                var relativePath = GetRelativePath(skillDirectoryPath, filePath);
                // Normalize path separators to forward slashes for consistency
                relativePath = relativePath.Replace(Path.DirectorySeparatorChar, '/');
                
#if NET6_0_OR_GREATER
                var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
#else
                var fileBytes = File.ReadAllBytes(filePath);
#endif
                var fileContent = new ByteArrayContent(fileBytes);
                var mimeType = GetMimeType(filePath);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
                
                // Use the relative path as the filename in the multipart form
                content.Add(fileContent, "files[]", relativePath);
            }

            return await HttpRequestSimple<SkillVersionResponse>($"{Endpoint}/{skillId}/versions", HttpMethod.Post, content, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a new version of an existing skill by uploading a zip file.
        /// The zip file must contain a SKILL.md file at its root.
        /// </summary>
        /// <param name="skillId">The ID of the skill to create a version for.</param>
        /// <param name="zipFilePath">Path to the zip file containing skill files. Must include a SKILL.md file.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The created skill version response.</returns>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
        public async Task<SkillVersionResponse> CreateSkillVersionFromZipAsync(
            string skillId,
            string zipFilePath,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                throw new ArgumentNullException(nameof(skillId), "Skill ID cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(zipFilePath))
            {
                throw new ArgumentException("Zip file path cannot be null or empty.", nameof(zipFilePath));
            }

            if (!File.Exists(zipFilePath))
            {
                throw new ArgumentException($"Zip file not found: {zipFilePath}", nameof(zipFilePath));
            }

#if NET6_0_OR_GREATER
            var fileBytes = await File.ReadAllBytesAsync(zipFilePath, cancellationToken).ConfigureAwait(false);
#else
            var fileBytes = File.ReadAllBytes(zipFilePath);
#endif

            using var content = new MultipartFormDataContent();

            // Add zip file
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            content.Add(fileContent, "files[]", Path.GetFileName(zipFilePath));

            return await HttpRequestSimple<SkillVersionResponse>($"{Endpoint}/{skillId}/versions", HttpMethod.Post, content, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a new version of an existing skill by uploading file streams.
        /// Files must include a SKILL.md file.
        /// </summary>
        /// <param name="skillId">The ID of the skill to create a version for.</param>
        /// <param name="files">List of tuples containing (filename, stream, mimeType) for each file to upload.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The created skill version response.</returns>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
        /// <exception cref="ArgumentNullException">Thrown when files is null or empty.</exception>
        public async Task<SkillVersionResponse> CreateSkillVersionFromStreamsAsync(
            string skillId,
            List<(string filename, Stream stream, string mimeType)> files,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                throw new ArgumentNullException(nameof(skillId), "Skill ID cannot be null or empty.");
            }

            if (files == null || !files.Any())
            {
                throw new ArgumentNullException(nameof(files), "Files list cannot be null or empty.");
            }

            // Verify SKILL.md is present
            if (!files.Any(f => f.filename.EndsWith("SKILL.md", StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("Files must include a SKILL.md file.", nameof(files));
            }

            using var content = new MultipartFormDataContent();

            // Add all files
            foreach (var (filename, stream, mimeType) in files)
            {
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
                content.Add(streamContent, "files[]", filename);
            }

            return await HttpRequestSimple<SkillVersionResponse>($"{Endpoint}/{skillId}/versions", HttpMethod.Post, content, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Lists all versions of a specific skill. Supports pagination using page tokens.
        /// </summary>
        /// <param name="skillId">The ID of the skill to list versions for.</param>
        /// <param name="page">Pagination token for fetching a specific page of results. Optionally set to the next_page token from the previous response.</param>
        /// <param name="limit">Number of items to return per page. Defaults to 20. Ranges from 1 to 1000.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A paginated list of skill version objects.</returns>
        /// <exception cref="ArgumentNullException">Thrown when skillId is null or empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when limit is outside the valid range.</exception>
        public async Task<SkillVersionListResponse> ListSkillVersionsAsync(
            string skillId,
            string page = null,
            int limit = 20,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                throw new ArgumentNullException(nameof(skillId), "Skill ID cannot be null or empty.");
            }

            if (limit < 1 || limit > 1000)
            {
                throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be between 1 and 1000.");
            }

            var queryParams = new List<string> { $"limit={limit}" };
            if (!string.IsNullOrEmpty(page))
                queryParams.Add($"page={page}");

            var queryString = "?" + string.Join("&", queryParams);
            return await HttpRequestSimple<SkillVersionListResponse>($"{Endpoint}/{skillId}/versions{queryString}", HttpMethod.Get, null, cancellationToken);
        }

        /// <summary>
        /// Retrieves details for a specific version of a skill.
        /// </summary>
        /// <param name="skillId">The ID of the skill.</param>
        /// <param name="version">Version identifier for the skill. Each version is identified by a Unix epoch timestamp (e.g., "1759178010641129").</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The skill version response.</returns>
        /// <exception cref="ArgumentNullException">Thrown when skillId or version is null or empty.</exception>
        public async Task<SkillVersionResponse> GetSkillVersionAsync(
            string skillId,
            string version,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                throw new ArgumentNullException(nameof(skillId), "Skill ID cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(version))
            {
                throw new ArgumentNullException(nameof(version), "Version cannot be null or empty.");
            }

            return await HttpRequestSimple<SkillVersionResponse>($"{Endpoint}/{skillId}/versions/{version}", HttpMethod.Get, null, cancellationToken);
        }

        /// <summary>
        /// Deletes a specific version of a skill, making it inaccessible through the API.
        /// </summary>
        /// <param name="skillId">The ID of the skill.</param>
        /// <param name="version">Version identifier for the skill. Each version is identified by a Unix epoch timestamp (e.g., "1759178010641129").</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A response confirming the skill version deletion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when skillId or version is null or empty.</exception>
        public async Task<SkillVersionDeleteResponse> DeleteSkillVersionAsync(
            string skillId,
            string version,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                throw new ArgumentNullException(nameof(skillId), "Skill ID cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(version))
            {
                throw new ArgumentNullException(nameof(version), "Version cannot be null or empty.");
            }

            return await HttpRequestSimple<SkillVersionDeleteResponse>($"{Endpoint}/{skillId}/versions/{version}", HttpMethod.Delete, null, cancellationToken);
        }

        /// <summary>
        /// Lists all skills in the organization. Supports pagination using page tokens.
        /// </summary>
        /// <param name="page">Pagination token for fetching a specific page of results. Pass the value from a previous response's next_page field to get the next page of results.</param>
        /// <param name="limit">Number of results to return per page. Maximum value is 100. Defaults to 20.</param>
        /// <param name="source">Filter skills by source. If provided, only skills from the specified source will be returned: "custom" for user-created skills, "anthropic" for Anthropic-created skills.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A paginated list of skill objects.</returns>
        public async Task<SkillListResponse> ListSkillsAsync(
            string page = null,
            int limit = 20,
            string source = null,
            CancellationToken cancellationToken = default)
        {
            if (limit < 1 || limit > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be between 1 and 100.");
            }

            var queryParams = new List<string> { $"limit={limit}" };
            if (!string.IsNullOrEmpty(page))
                queryParams.Add($"page={page}");
            if (!string.IsNullOrEmpty(source))
                queryParams.Add($"source={source}");

            var queryString = "?" + string.Join("&", queryParams);
            return await HttpRequestSimple<SkillListResponse>($"{Endpoint}{queryString}", HttpMethod.Get, null, cancellationToken);
        }

        /// <summary>
        /// Retrieves details for a specific skill by its ID.
        /// </summary>
        /// <param name="skillId">The ID of the skill to retrieve.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The skill response.</returns>
        /// <exception cref="ArgumentNullException">Thrown when skillId is null or empty.</exception>
        public async Task<SkillResponse> GetSkillAsync(
            string skillId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                throw new ArgumentNullException(nameof(skillId), "Skill ID cannot be null or empty.");
            }

            return await HttpRequestSimple<SkillResponse>($"{Endpoint}/{skillId}", HttpMethod.Get, null, cancellationToken);
        }

        /// <summary>
        /// Deletes a skill, making it inaccessible through the API.
        /// </summary>
        /// <param name="skillId">The ID of the skill to delete.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A response confirming the skill deletion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when skillId is null or empty.</exception>
        public async Task<SkillDeleteResponse> DeleteSkillAsync(
            string skillId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                throw new ArgumentNullException(nameof(skillId), "Skill ID cannot be null or empty.");
            }

            return await HttpRequestSimple<SkillDeleteResponse>($"{Endpoint}/{skillId}", HttpMethod.Delete, null, cancellationToken);
        }

        /// <summary>
        /// Gets the MIME type based on file extension.
        /// </summary>
        private static string GetMimeType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".md" => "text/markdown",
                ".py" => "text/x-python",
                ".js" => "text/javascript",
                ".ts" => "text/typescript",
                ".json" => "application/json",
                ".txt" => "text/plain",
                ".html" => "text/html",
                ".htm" => "text/html",
                ".xml" => "application/xml",
                ".csv" => "text/csv",
                ".yaml" => "text/yaml",
                ".yml" => "text/yaml",
                ".sh" => "text/x-shellscript",
                ".bash" => "text/x-shellscript",
                ".java" => "text/x-java",
                ".c" => "text/x-c",
                ".cpp" => "text/x-c++",
                ".h" => "text/x-c",
                ".hpp" => "text/x-c++",
                ".cs" => "text/x-csharp",
                ".rb" => "text/x-ruby",
                ".go" => "text/x-go",
                ".rs" => "text/x-rust",
                ".php" => "text/x-php",
                ".sql" => "text/x-sql",
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// Gets the relative path from base path to target path.
        /// Compatible with .NET Standard 2.0 which doesn't have Path.GetRelativePath.
        /// </summary>
        private static string GetRelativePath(string basePath, string targetPath)
        {
            // Ensure paths are absolute
            basePath = Path.GetFullPath(basePath);
            targetPath = Path.GetFullPath(targetPath);

            // Add trailing separator to base path if not present
            if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()) && 
                !basePath.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            {
                basePath += Path.DirectorySeparatorChar;
            }

            var baseUri = new Uri(basePath);
            var targetUri = new Uri(targetPath);

            var relativeUri = baseUri.MakeRelativeUri(targetUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            // Convert forward slashes to platform-specific separators
            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
