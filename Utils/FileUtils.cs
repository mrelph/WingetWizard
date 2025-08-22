using System;
using System.IO;
using System.Text;

namespace UpgradeApp.Utils
{
    /// <summary>
    /// Utility class for file operations and common file-related tasks
    /// Provides helper methods for file handling, validation, and safe operations
    /// </summary>
    public static class FileUtils
    {
        /// <summary>
        /// Safely creates a directory if it doesn't exist
        /// </summary>
        /// <param name="path">Directory path to create</param>
        /// <returns>True if directory exists or was created successfully</returns>
        public static bool EnsureDirectoryExists(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return false;
                
                // Validate path to prevent traversal attacks
                var fullPath = Path.GetFullPath(path);
                var basePath = Path.GetFullPath(GetApplicationBaseDirectory());
                if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                    return false;
                
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }
                return true;
            }
            catch (Exception ex)
            {
                var safePath = System.Net.WebUtility.HtmlEncode(path);
                System.Diagnostics.Debug.WriteLine($"Failed to create directory {safePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Safely writes text to a file with UTF-8 encoding
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <param name="content">Content to write</param>
        /// <returns>True if write was successful</returns>
        public static bool SafeWriteText(string filePath, string content)
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    EnsureDirectoryExists(directory);
                }

                File.WriteAllText(filePath, content, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to write file {filePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Safely reads text from a file with UTF-8 encoding
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <param name="defaultContent">Default content if file doesn't exist or can't be read</param>
        /// <returns>File content or default content</returns>
        public static string SafeReadText(string filePath, string defaultContent = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    return defaultContent;
                
                // Validate path to prevent traversal attacks
                var fullPath = Path.GetFullPath(filePath);
                var basePath = Path.GetFullPath(GetApplicationBaseDirectory());
                if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                    return defaultContent;
                
                if (File.Exists(fullPath))
                {
                    return File.ReadAllText(fullPath, Encoding.UTF8);
                }
                return defaultContent;
            }
            catch (UnauthorizedAccessException)
            {
                return defaultContent;
            }
            catch (FileNotFoundException)
            {
                return defaultContent;
            }
            catch (DirectoryNotFoundException)
            {
                return defaultContent;
            }
            catch (IOException ex)
            {
                var safeFilePath = System.Net.WebUtility.HtmlEncode(filePath);
                System.Diagnostics.Debug.WriteLine($"Failed to read file {safeFilePath}: {ex.Message}");
                return defaultContent;
            }
        }

        /// <summary>
        /// Creates a safe filename by removing invalid characters
        /// </summary>
        /// <param name="fileName">Original filename</param>
        /// <returns>Safe filename with invalid characters replaced</returns>
        public static string CreateSafeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "unnamed";

            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = fileName;

            foreach (var invalidChar in invalidChars)
            {
                safeName = safeName.Replace(invalidChar, '_');
            }

            // Ensure filename isn't too long
            if (safeName.Length > 200)
            {
                var extension = Path.GetExtension(safeName);
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(safeName);
                safeName = nameWithoutExtension.Substring(0, 200 - extension.Length) + extension;
            }

            return safeName;
        }

        /// <summary>
        /// Gets file size in human-readable format
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>Human-readable file size or "Unknown" if error</returns>
        public static string GetFileSize(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    return "Invalid file path";
                    
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    return FormatFileSize(fileInfo.Length);
                }
                return "File not found";
            }
            catch (Exception ex)
            {
                var safeFilePath = System.Net.WebUtility.HtmlEncode(filePath);
                System.Diagnostics.Debug.WriteLine($"Failed to get file size for {safeFilePath}: {ex.Message}");
                return "Unknown";
            }
        }

        /// <summary>
        /// Formats file size in bytes to human-readable format
        /// </summary>
        /// <param name="bytes">Size in bytes</param>
        /// <returns>Human-readable size string</returns>
        public static string FormatFileSize(long bytes)
        {
            if (bytes < 0)
                return "0 B";
                
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Checks if a file path is valid and accessible
        /// </summary>
        /// <param name="filePath">Path to check</param>
        /// <returns>True if path is valid and accessible</returns>
        public static bool IsValidFilePath(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    return false;

                // Check if path contains invalid characters
                var fileName = Path.GetFileName(filePath);
                if (string.IsNullOrEmpty(fileName))
                    return false;

                var invalidChars = Path.GetInvalidFileNameChars();
                if (fileName.IndexOfAny(invalidChars) >= 0)
                    return false;

                // Check if directory path is valid
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    var invalidPathChars = Path.GetInvalidPathChars();
                    if (directory.IndexOfAny(invalidPathChars) >= 0)
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the application's base directory
        /// </summary>
        /// <returns>Application base directory path</returns>
        public static string GetApplicationBaseDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// Combines multiple path segments safely
        /// </summary>
        /// <param name="paths">Path segments to combine</param>
        /// <returns>Combined path</returns>
        public static string CombinePaths(params string[] paths)
        {
            if (paths == null || paths.Length == 0)
                return string.Empty;
            
            // Validate all path elements are not null/empty
            foreach (var path in paths)
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("Path elements cannot be null or empty");
            }

            var result = paths[0];
            for (int i = 1; i < paths.Length; i++)
            {
                result = Path.Combine(result, paths[i]);
            }
            return result;
        }
    }
}
