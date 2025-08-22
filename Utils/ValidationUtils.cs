using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace UpgradeApp.Utils
{
    /// <summary>
    /// Provides comprehensive input validation and sanitization utilities.
    /// Ensures data integrity and security across all user inputs.
    /// </summary>
    public static class ValidationUtils
    {
        // Validation patterns and constants - using centralized constants
        private static readonly Regex ValidPackageNamePattern = new(AppConstants.PACKAGE_NAME_PATTERN, RegexOptions.Compiled);
        private static readonly Regex ValidVersionPattern = new(AppConstants.VERSION_PATTERN, RegexOptions.Compiled);
        private static readonly Regex ValidApiKeyPattern = new(AppConstants.API_KEY_PATTERN, RegexOptions.Compiled);
        private static readonly Regex ValidFileNamePattern = new(AppConstants.FILE_NAME_PATTERN, RegexOptions.Compiled);
        private static readonly Regex ValidUrlPattern = new(AppConstants.URL_PATTERN, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        // Input length limits - using centralized constants
        private const int MaxPackageNameLength = AppConstants.MAX_PACKAGE_NAME_LENGTH;
        private const int MaxVersionLength = AppConstants.MAX_VERSION_LENGTH;
        private const int MaxApiKeyLength = AppConstants.MAX_API_KEY_LENGTH;
        private const int MaxFileNameLength = AppConstants.MAX_FILE_NAME_LENGTH;
        private const int MaxDescriptionLength = AppConstants.MAX_DESCRIPTION_LENGTH;
        private const int MaxPathLength = AppConstants.MAX_PATH_LENGTH;

        /// <summary>
        /// Validates and sanitizes a package name input.
        /// </summary>
        /// <param name="packageName">The package name to validate</param>
        /// <param name="errors">Collection to store validation errors</param>
        /// <returns>Sanitized package name or null if invalid</returns>
        public static string? ValidatePackageName(string? packageName, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                errors.Add("Package name cannot be empty");
                return null;
            }

            var trimmed = packageName.Trim();
            
            if (trimmed.Length > MaxPackageNameLength)
            {
                errors.Add($"Package name cannot exceed {MaxPackageNameLength} characters");
                return null;
            }

            if (!ValidPackageNamePattern.IsMatch(trimmed))
            {
                errors.Add("Package name contains invalid characters. Use only letters, numbers, dots, underscores, and hyphens");
                return null;
            }

            // Check for potentially dangerous patterns
            if (ContainsDangerousPatterns(trimmed))
            {
                errors.Add("Package name contains potentially dangerous patterns");
                return null;
            }

            return trimmed;
        }

        /// <summary>
        /// Validates and sanitizes a version string input.
        /// </summary>
        /// <param name="version">The version string to validate</param>
        /// <param name="errors">Collection to store validation errors</param>
        /// <returns>Sanitized version string or null if invalid</returns>
        public static string? ValidateVersion(string? version, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                errors.Add("Version cannot be empty");
                return null;
            }

            var trimmed = version.Trim();
            
            if (trimmed.Length > MaxVersionLength)
            {
                errors.Add($"Version cannot exceed {MaxVersionLength} characters");
                return null;
            }

            if (!ValidVersionPattern.IsMatch(trimmed))
            {
                errors.Add("Invalid version format. Use format: major.minor[.patch][.build]");
                return null;
            }

            return trimmed;
        }

        /// <summary>
        /// Validates and sanitizes an API key input.
        /// </summary>
        /// <param name="apiKey">The API key to validate</param>
        /// <param name="errors">Collection to store validation errors</param>
        /// <returns>Sanitized API key or null if invalid</returns>
        public static string? ValidateApiKey(string? apiKey, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                errors.Add("API key cannot be empty");
                return null;
            }

            var trimmed = apiKey.Trim();
            
            if (trimmed.Length > MaxApiKeyLength)
            {
                errors.Add($"API key cannot exceed {MaxApiKeyLength} characters");
                return null;
            }

            if (!ValidApiKeyPattern.IsMatch(trimmed))
            {
                errors.Add("API key format is invalid. Must be at least 20 characters and contain only letters, numbers, hyphens, and underscores");
                return null;
            }

            return trimmed;
        }

        /// <summary>
        /// Validates and sanitizes a file name input.
        /// </summary>
        /// <param name="fileName">The file name to validate</param>
        /// <param name="errors">Collection to store validation errors</param>
        /// <returns>Sanitized file name or null if invalid</returns>
        public static string? ValidateFileName(string? fileName, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                errors.Add("File name cannot be empty");
                return null;
            }

            var trimmed = fileName.Trim();
            
            if (trimmed.Length > MaxFileNameLength)
            {
                errors.Add($"File name cannot exceed {MaxFileNameLength} characters");
                return null;
            }

            if (!ValidFileNamePattern.IsMatch(trimmed))
            {
                errors.Add("File name contains invalid characters. Use only letters, numbers, dots, underscores, hyphens, and spaces");
                return null;
            }

            // Check for reserved Windows file names
            var reservedNames = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
            var fileNameUpper = Path.GetFileNameWithoutExtension(trimmed).ToUpperInvariant();
            
            if (reservedNames.Contains(fileNameUpper))
            {
                errors.Add($"File name '{trimmed}' is a reserved Windows name");
                return null;
            }

            // Check for potentially dangerous extensions
            var dangerousExtensions = new[] { ".exe", ".bat", ".cmd", ".com", ".pif", ".scr", ".vbs", ".js", ".ps1" };
            var extension = Path.GetExtension(trimmed).ToLowerInvariant();
            
            if (dangerousExtensions.Contains(extension))
            {
                errors.Add($"File extension '{extension}' is not allowed for security reasons");
                return null;
            }

            return trimmed;
        }

        /// <summary>
        /// Validates and sanitizes a file path input.
        /// </summary>
        /// <param name="filePath">The file path to validate</param>
        /// <param name="errors">Collection to store validation errors</param>
        /// <returns>Sanitized file path or null if invalid</returns>
        public static string? ValidateFilePath(string? filePath, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                errors.Add("File path cannot be empty");
                return null;
            }

            var trimmed = filePath.Trim();
            
            if (trimmed.Length > MaxPathLength)
            {
                errors.Add($"File path cannot exceed {MaxPathLength} characters");
                return null;
            }

            try
            {
                // Check if path contains invalid characters
                var invalidChars = Path.GetInvalidPathChars();
                if (trimmed.Any(c => invalidChars.Contains(c)))
                {
                    errors.Add("File path contains invalid characters");
                    return null;
                }

                // Check for potentially dangerous patterns
                if (ContainsDangerousPatterns(trimmed))
                {
                    errors.Add("File path contains potentially dangerous patterns");
                    return null;
                }

                // Normalize the path
                var normalizedPath = Path.GetFullPath(trimmed);
                
                // Check if path is within application directory (security measure)
                var appPath = Path.GetFullPath(Application.StartupPath);
                if (!normalizedPath.StartsWith(appPath, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("File path must be within the application directory for security reasons");
                    return null;
                }

                return normalizedPath;
            }
            catch (Exception ex)
            {
                errors.Add($"Invalid file path: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Validates and sanitizes a URL input.
        /// </summary>
        /// <param name="url">The URL to validate</param>
        /// <param name="errors">Collection to store validation errors</param>
        /// <returns>Sanitized URL or null if invalid</returns>
        public static string? ValidateUrl(string? url, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                errors.Add("URL cannot be empty");
                return null;
            }

            var trimmed = url.Trim();
            
            if (!ValidUrlPattern.IsMatch(trimmed))
            {
                errors.Add("Invalid URL format. Must start with http:// or https://");
                return null;
            }

            // Check for potentially dangerous patterns
            if (ContainsDangerousPatterns(trimmed))
            {
                errors.Add("URL contains potentially dangerous patterns");
                return null;
            }

            return trimmed;
        }

        /// <summary>
        /// Validates and sanitizes a description text input.
        /// </summary>
        /// <param name="description">The description to validate</param>
        /// <param name="errors">Collection to store validation errors</param>
        /// <returns>Sanitized description or null if invalid</returns>
        public static string? ValidateDescription(string? description, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return string.Empty; // Empty descriptions are allowed
            }

            var trimmed = description.Trim();
            
            if (trimmed.Length > MaxDescriptionLength)
            {
                errors.Add($"Description cannot exceed {MaxDescriptionLength} characters");
                return null;
            }

            // Remove potentially dangerous HTML/script tags
            var sanitized = RemoveHtmlTags(trimmed);
            
            // Check for potentially dangerous patterns
            if (ContainsDangerousPatterns(sanitized))
            {
                errors.Add("Description contains potentially dangerous patterns");
                return null;
            }

            return sanitized;
        }

        /// <summary>
        /// Validates and sanitizes a numeric input.
        /// </summary>
        /// <param name="value">The numeric value to validate</param>
        /// <param name="minValue">Minimum allowed value</param>
        /// <param name="maxValue">Maximum allowed value</param>
        /// <param name="errors">Collection to store validation errors</param>
        /// <returns>Validated numeric value or null if invalid</returns>
        public static int? ValidateNumericInput(string? value, int minValue, int maxValue, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add("Numeric value cannot be empty");
                return null;
            }

            if (!int.TryParse(value, out var numericValue))
            {
                errors.Add("Value must be a valid number");
                return null;
            }

            if (numericValue < minValue || numericValue > maxValue)
            {
                errors.Add($"Value must be between {minValue} and {maxValue}");
                return null;
            }

            return numericValue;
        }

        /// <summary>
        /// Validates and sanitizes a boolean input.
        /// </summary>
        /// <param name="value">The boolean value to validate</param>
        /// <param name="errors">Collection to store validation errors</param>
        /// <returns>Validated boolean value or null if invalid</returns>
        public static bool? ValidateBooleanInput(string? value, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add("Boolean value cannot be empty");
                return null;
            }

            var trimmed = value.Trim().ToLowerInvariant();
            
            switch (trimmed)
            {
                case "true":
                case "1":
                case "yes":
                case "on":
                    return true;
                case "false":
                case "0":
                case "no":
                case "off":
                    return false;
                default:
                    errors.Add("Boolean value must be 'true', 'false', '1', '0', 'yes', 'no', 'on', or 'off'");
                    return null;
            }
        }

        /// <summary>
        /// Validates that a collection is not null or empty.
        /// </summary>
        /// <typeparam name="T">Type of collection elements</typeparam>
        /// <param name="collection">The collection to validate</param>
        /// <param name="collectionName">Name of the collection for error messages</param>
        /// <param name="errors">Collection to store validation errors</param>
        /// <returns>True if collection is valid, false otherwise</returns>
        public static bool ValidateCollection<T>(IEnumerable<T>? collection, string collectionName, ICollection<string> errors)
        {
            if (collection == null)
            {
                errors.Add($"{collectionName} cannot be null");
                return false;
            }

            if (!collection.Any())
            {
                errors.Add($"{collectionName} cannot be empty");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sanitizes a string by removing potentially dangerous HTML tags and scripts.
        /// </summary>
        /// <param name="input">The input string to sanitize</param>
        /// <returns>Sanitized string</returns>
        private static string RemoveHtmlTags(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Remove HTML tags
            var withoutTags = Regex.Replace(input, @"<[^>]*>", string.Empty);
            
            // Remove script-like content
            var withoutScripts = Regex.Replace(withoutTags, @"javascript:", string.Empty, RegexOptions.IgnoreCase);
            
            // Remove potentially dangerous protocols
            var withoutProtocols = Regex.Replace(withoutScripts, @"(data|vbscript|file):", string.Empty, RegexOptions.IgnoreCase);
            
            return withoutProtocols.Trim();
        }

        /// <summary>
        /// Checks if a string contains potentially dangerous patterns.
        /// Enhanced with additional security pattern detection.
        /// </summary>
        /// <param name="input">The input string to check</param>
        /// <returns>True if dangerous patterns are found, false otherwise</returns>
        private static bool ContainsDangerousPatterns(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            var lowerInput = input.ToLowerInvariant();
            
            // Enhanced script injection patterns
            var scriptPatterns = new[]
            {
                // XSS patterns
                "<script", "javascript:", "vbscript:", "data:", "file:", "ftp:",
                "onload=", "onerror=", "onclick=", "onmouseover=", "onfocus=", "onblur=",
                "onchange=", "onsubmit=", "onreset=", "onselect=", "onkeydown=", "onkeyup=",
                
                // Command injection patterns  
                "eval(", "exec(", "system(", "shell(", "cmd(", "powershell", "bash", "sh ",
                "net.exe", "reg.exe", "sc.exe", "wmic.exe", "rundll32", "regsvr32",
                "certutil", "bitsadmin", "mshta", "cscript", "wscript",
                
                // Path traversal patterns
                "..\\", "..//", "..\\\\", "..//", "..%5c", "..%2f", "..%252f", "..%c0%af",
                "..\\\\", "../", "%2e%2e%2f", "%2e%2e\\", "..%255c",
                
                // SQL injection patterns
                "union select", "select * from", "drop table", "insert into", "update set",
                "delete from", "alter table", "create table", "exec(", "execute(",
                "sp_executesql", "xp_cmdshell", "sp_oacreate", "sp_oamethod",
                
                // LDAP injection patterns
                "*)(&", "*))%00", "admin*)((|userpassword=*)",
                
                // XML injection patterns
                "<!entity", "<!doctype", "<?xml", "]]>", "<![cdata[",
                
                // NoSQL injection patterns
                "$where", "$regex", "$ne", "$gt", "$lt", "$in", "$nin",
                
                // Template injection patterns
                "{{", "}}", "${", "<%", "%>", "<#", "#>", "[%", "%]",
                
                // Protocol handlers
                "mailto:", "tel:", "sms:", "market:", "intent:", "chrome:", "ms-excel:",
                
                // Binary/executable patterns
                "\\x00", "\\x0a", "\\x0d", "\\xff", "\\xfe", "\\xef\\xbb\\xbf",
                
                // Server-side includes
                "<!--#exec", "<!--#include", "<!--#echo", "<!--#config",
                
                // Expression language injection
                "#{", "#{", "${", "<%=", "<?=", "{{", "%{", "#{}"
            };

            // Check for dangerous patterns
            if (scriptPatterns.Any(pattern => lowerInput.Contains(pattern)))
                return true;

            // Check for encoded attacks
            var encodedPatterns = new[]
            {
                "%3cscript", "%3c%2fscript", "&#x3c;", "&#60;", "&#x3e;", "&#62;",
                "%3c%21%2d%2d", "%2d%2d%3e", "\\u003c", "\\u003e", "\\u0022", "\\u0027"
            };

            if (encodedPatterns.Any(pattern => lowerInput.Contains(pattern)))
                return true;

            // Check for suspicious character sequences
            if (ContainsSuspiciousCharacterSequences(input))
                return true;

            // Check for polyglot attacks (multiple injection types combined)
            if (IsPolyglotAttack(lowerInput))
                return true;

            return false;
        }

        /// <summary>
        /// Checks for suspicious character sequences that might indicate an attack
        /// </summary>
        private static bool ContainsSuspiciousCharacterSequences(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            // Check for excessive repeating characters (potential buffer overflow)
            for (char c = 'A'; c <= 'Z'; c++)
            {
                if (input.Contains(new string(c, 100)) || input.Contains(new string(char.ToLower(c), 100)))
                    return true;
            }

            // Check for suspicious character combinations
            var suspiciousSequences = new[]
            {
                "\0", "\x01", "\x02", "\x03", "\x04", "\x05", "\x06", "\x07",
                "\x08", "\x0B", "\x0C", "\x0E", "\x0F", "\x10", "\x11", "\x12",
                "\x13", "\x14", "\x15", "\x16", "\x17", "\x18", "\x19", "\x1A",
                "\x1B", "\x1C", "\x1D", "\x1E", "\x1F", "\x7F"
            };

            return suspiciousSequences.Any(seq => input.Contains(seq));
        }

        /// <summary>
        /// Detects polyglot attacks (attacks that work across multiple contexts)
        /// </summary>
        private static bool IsPolyglotAttack(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            // Count different types of injection indicators
            int indicators = 0;

            // XSS indicators
            if (input.Contains("<script") || input.Contains("javascript:") || input.Contains("onerror="))
                indicators++;

            // SQL injection indicators
            if (input.Contains("union select") || input.Contains("' or '1'='1") || input.Contains("drop table"))
                indicators++;

            // Command injection indicators
            if (input.Contains("$(") || input.Contains("`;") || input.Contains("&&"))
                indicators++;

            // Path traversal indicators
            if (input.Contains("../") || input.Contains("..\\"))
                indicators++;

            // Template injection indicators
            if (input.Contains("{{") || input.Contains("${"))
                indicators++;

            // If multiple types of injection indicators are present, it's likely a polyglot attack
            return indicators >= 2;
        }

        /// <summary>
        /// Gets a summary of all validation errors in a user-friendly format.
        /// </summary>
        /// <param name="errors">Collection of validation errors</param>
        /// <returns>Formatted error summary</returns>
        public static string GetValidationErrorSummary(ICollection<string> errors)
        {
            if (errors == null || errors.Count == 0)
                return "No validation errors found.";

            var summary = new StringBuilder();
            summary.AppendLine($"Found {errors.Count} validation error(s):");
            summary.AppendLine();

            foreach (var error in errors)
            {
                summary.AppendLine($"• {error}");
            }

            return summary.ToString();
        }

        /// <summary>
        /// Validates that all required fields are provided.
        /// </summary>
        /// <param name="requiredFields">Dictionary of field names and their values</param>
        /// <param name="errors">Collection to store validation errors</param>
        /// <returns>True if all required fields are valid, false otherwise</returns>
        public static bool ValidateRequiredFields(Dictionary<string, string?> requiredFields, ICollection<string> errors)
        {
            var isValid = true;

            foreach (var field in requiredFields)
            {
                if (string.IsNullOrWhiteSpace(field.Value))
                {
                    errors.Add($"Required field '{field.Key}' cannot be empty");
                    isValid = false;
                }
            }

            return isValid;
        }

        /// <summary>
        /// Validates input against OWASP security guidelines
        /// </summary>
        /// <param name="input">Input to validate</param>
        /// <param name="context">Context of the input (e.g., "username", "filename", "command")</param>
        /// <param name="errors">Collection to store validation errors</param>
        /// <returns>Sanitized input or null if invalid</returns>
        public static string? ValidateSecureInput(string? input, string context, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                errors.Add($"{context} cannot be empty");
                return null;
            }

            var trimmed = input.Trim();

            // Check for dangerous patterns first
            if (ContainsDangerousPatterns(trimmed))
            {
                errors.Add($"{context} contains potentially dangerous patterns");
                return null;
            }

            // Apply context-specific validation
            return context.ToLowerInvariant() switch
            {
                "packageid" or "package_id" => ValidatePackageName(trimmed, errors),
                "filename" or "file_name" => ValidateFileName(trimmed, errors),
                "filepath" or "file_path" => ValidateFilePath(trimmed, errors),
                "url" => ValidateUrl(trimmed, errors),
                "apikey" or "api_key" => ValidateApiKey(trimmed, errors),
                "version" => ValidateVersion(trimmed, errors),
                "description" => ValidateDescription(trimmed, errors),
                _ => ValidateGenericInput(trimmed, errors)
            };
        }

        /// <summary>
        /// Validates generic input with basic security checks
        /// </summary>
        private static string? ValidateGenericInput(string input, ICollection<string> errors)
        {
            if (input.Length > 500) // Reasonable default limit
            {
                errors.Add("Input exceeds maximum allowed length (500 characters)");
                return null;
            }

            // Remove potentially harmful content
            var sanitized = RemoveHtmlTags(input);
            
            if (ContainsDangerousPatterns(sanitized))
            {
                errors.Add("Input contains potentially dangerous content");
                return null;
            }

            return sanitized;
        }

        /// <summary>
        /// Validates command arguments for safe execution
        /// </summary>
        /// <param name="command">Base command</param>
        /// <param name="arguments">Command arguments</param>
        /// <param name="allowedCommands">Whitelist of allowed commands</param>
        /// <param name="errors">Collection to store validation errors</param>
        /// <returns>True if command and arguments are safe</returns>
        public static bool ValidateCommandExecution(string command, string[] arguments, IEnumerable<string> allowedCommands, ICollection<string> errors)
        {
            // Validate command
            if (string.IsNullOrWhiteSpace(command))
            {
                errors.Add("Command cannot be empty");
                return false;
            }

            if (!allowedCommands.Contains(command.ToLowerInvariant()))
            {
                errors.Add($"Command '{command}' is not in the allowed commands list");
                return false;
            }

            if (ContainsDangerousPatterns(command))
            {
                errors.Add($"Command '{command}' contains dangerous patterns");
                return false;
            }

            // Validate arguments
            foreach (var arg in arguments ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(arg))
                    continue;

                if (ContainsDangerousPatterns(arg))
                {
                    errors.Add($"Argument '{arg}' contains dangerous patterns");
                    return false;
                }

                // Check for argument injection patterns
                if (arg.Contains("$(") || arg.Contains("`") || arg.Contains("${") || 
                    arg.Contains("&&") || arg.Contains("||") || arg.Contains(";"))
                {
                    errors.Add($"Argument '{arg}' contains command injection patterns");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validates input length against specified limits
        /// </summary>
        /// <param name="input">Input to validate</param>
        /// <param name="minLength">Minimum allowed length</param>
        /// <param name="maxLength">Maximum allowed length</param>
        /// <param name="fieldName">Name of the field for error messages</param>
        /// <param name="errors">Collection to store validation errors</param>
        /// <returns>True if length is valid</returns>
        public static bool ValidateInputLength(string? input, int minLength, int maxLength, string fieldName, ICollection<string> errors)
        {
            if (input == null)
            {
                if (minLength > 0)
                {
                    errors.Add($"{fieldName} cannot be null");
                    return false;
                }
                return true;
            }

            if (input.Length < minLength)
            {
                errors.Add($"{fieldName} must be at least {minLength} characters long");
                return false;
            }

            if (input.Length > maxLength)
            {
                errors.Add($"{fieldName} cannot exceed {maxLength} characters");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates that input contains only allowed characters
        /// </summary>
        /// <param name="input">Input to validate</param>
        /// <param name="allowedPattern">Regex pattern for allowed characters</param>
        /// <param name="fieldName">Name of the field for error messages</param>
        /// <param name="errors">Collection to store validation errors</param>
        /// <returns>True if input matches allowed pattern</returns>
        public static bool ValidateAllowedCharacters(string? input, string allowedPattern, string fieldName, ICollection<string> errors)
        {
            if (string.IsNullOrEmpty(input))
                return true; // Empty input handling should be done elsewhere

            try
            {
                var regex = new Regex(allowedPattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
                if (!regex.IsMatch(input))
                {
                    errors.Add($"{fieldName} contains invalid characters");
                    return false;
                }
                return true;
            }
            catch (RegexMatchTimeoutException)
            {
                errors.Add($"Pattern matching timeout for {fieldName}");
                return false;
            }
            catch (Exception ex)
            {
                errors.Add($"Pattern validation failed for {fieldName}: {ex.Message}");
                return false;
            }
        }
    }
}
