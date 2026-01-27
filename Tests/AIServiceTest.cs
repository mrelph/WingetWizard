using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using WingetWizard.Models;
using WingetWizard.Services;

namespace WingetWizard.Tests
{
    /// <summary>
    /// Simple test runner for AI services
    /// </summary>
    public static class AIServiceTest
    {
        public static async Task<(bool Success, string Message)> TestClaudeAsync()
        {
            try
            {
                var config = LoadConfig();
                if (string.IsNullOrEmpty(config.AnthropicApiKey))
                    return (false, "Claude API key not configured");

                var aiService = new AIService(
                    claudeApiKey: config.AnthropicApiKey,
                    perplexityApiKey: "",
                    selectedAiModel: config.SelectedAiModel ?? "claude-sonnet-4-20250514",
                    usePerplexity: false,
                    selectedProvider: "Claude"
                );

                var testApp = new UpgradableApp
                {
                    Name = "Visual Studio Code",
                    Id = "Microsoft.VisualStudioCode",
                    Version = "1.85.0",
                    Available = "1.86.0"
                };

                Console.WriteLine("[TEST] Testing Claude API...");
                var result = await aiService.GetAIRecommendationAsync(testApp);

                if (result.Contains("API Error") || result.Contains("failed"))
                    return (false, $"Claude API Error: {result.Substring(0, Math.Min(200, result.Length))}");

                return (true, $"Claude API working! Response length: {result.Length} chars");
            }
            catch (Exception ex)
            {
                return (false, $"Claude test exception: {ex.Message}");
            }
        }

        public static async Task<(bool Success, string Message)> TestPerplexityAsync()
        {
            try
            {
                var config = LoadConfig();
                if (string.IsNullOrEmpty(config.PerplexityApiKey))
                    return (false, "Perplexity API key not configured");

                var aiService = new AIService(
                    claudeApiKey: "",
                    perplexityApiKey: config.PerplexityApiKey,
                    selectedAiModel: "sonar",
                    usePerplexity: false,
                    selectedProvider: "Perplexity"
                );

                var testApp = new UpgradableApp
                {
                    Name = "Visual Studio Code",
                    Id = "Microsoft.VisualStudioCode",
                    Version = "1.85.0",
                    Available = "1.86.0"
                };

                Console.WriteLine("[TEST] Testing Perplexity API...");
                var result = await aiService.GetAIRecommendationAsync(testApp);

                if (result.Contains("API Error") || result.Contains("failed") || result.Contains("not configured"))
                    return (false, $"Perplexity API Error: {result.Substring(0, Math.Min(200, result.Length))}");

                return (true, $"Perplexity API working! Response length: {result.Length} chars");
            }
            catch (Exception ex)
            {
                return (false, $"Perplexity test exception: {ex.Message}");
            }
        }

        public static async Task<(bool Success, string Message)> TestBedrockAsync()
        {
            try
            {
                var config = LoadConfig();
                if (string.IsNullOrEmpty(config.AwsAccessKeyId) || string.IsNullOrEmpty(config.AwsSecretAccessKey))
                    return (false, "AWS Bedrock credentials not configured");

                var bedrockService = new BedrockService(
                    accessKeyId: config.AwsAccessKeyId,
                    secretAccessKey: config.AwsSecretAccessKey,
                    region: config.AwsRegion ?? "us-east-1",
                    selectedModel: "anthropic.claude-3-5-sonnet-20241022-v2:0"
                );

                var testApp = new UpgradableApp
                {
                    Name = "Visual Studio Code",
                    Id = "Microsoft.VisualStudioCode",
                    Version = "1.85.0",
                    Available = "1.86.0"
                };

                Console.WriteLine("[TEST] Testing AWS Bedrock API...");
                var result = await bedrockService.GetAIRecommendationAsync(testApp);

                if (result.Contains("Error") || result.Contains("failed") || result.Contains("not configured"))
                    return (false, $"Bedrock API Error: {result.Substring(0, Math.Min(200, result.Length))}");

                return (true, $"Bedrock API working! Response length: {result.Length} chars");
            }
            catch (Exception ex)
            {
                return (false, $"Bedrock test exception: {ex.Message}");
            }
        }

        private static ConfigData LoadConfig()
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "config.json");
            if (!File.Exists(configPath))
            {
                // Try alternate path
                configPath = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
            }

            if (!File.Exists(configPath))
                throw new FileNotFoundException($"Config file not found");

            var json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<ConfigData>(json) ?? new ConfigData();
        }

        public class ConfigData
        {
            public string? AnthropicApiKey { get; set; }
            public string? PerplexityApiKey { get; set; }
            public string? AwsAccessKeyId { get; set; }
            public string? AwsSecretAccessKey { get; set; }
            public string? AwsRegion { get; set; }
            public string? SelectedAiModel { get; set; }
        }
    }
}
