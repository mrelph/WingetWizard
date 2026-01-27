using System;
using System.Threading.Tasks;

namespace WingetWizard.Tests
{
    public static class TestRunner
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("  WingetWizard AI Service Test Suite");
            Console.WriteLine("===========================================");
            Console.WriteLine();

            var testClaude = args.Length == 0 || Array.Exists(args, a => a.ToLower() == "claude" || a.ToLower() == "all");
            var testPerplexity = args.Length == 0 || Array.Exists(args, a => a.ToLower() == "perplexity" || a.ToLower() == "all");
            var testBedrock = args.Length == 0 || Array.Exists(args, a => a.ToLower() == "bedrock" || a.ToLower() == "all");

            int passed = 0;
            int failed = 0;

            if (testClaude)
            {
                Console.WriteLine("[1/3] Testing Claude (Anthropic Direct)...");
                var (success, message) = await AIServiceTest.TestClaudeAsync();
                PrintResult("Claude API", success, message);
                if (success) passed++; else failed++;
                Console.WriteLine();
            }

            if (testPerplexity)
            {
                Console.WriteLine("[2/3] Testing Perplexity...");
                var (success, message) = await AIServiceTest.TestPerplexityAsync();
                PrintResult("Perplexity API", success, message);
                if (success) passed++; else failed++;
                Console.WriteLine();
            }

            if (testBedrock)
            {
                Console.WriteLine("[3/3] Testing AWS Bedrock...");
                var (success, message) = await AIServiceTest.TestBedrockAsync();
                PrintResult("Bedrock API", success, message);
                if (success) passed++; else failed++;
                Console.WriteLine();
            }

            Console.WriteLine("===========================================");
            Console.WriteLine($"  Results: {passed} passed, {failed} failed");
            Console.WriteLine("===========================================");

            Environment.ExitCode = failed > 0 ? 1 : 0;
        }

        private static void PrintResult(string testName, bool success, string message)
        {
            var status = success ? "PASS" : "FAIL";
            var color = success ? ConsoleColor.Green : ConsoleColor.Red;

            Console.ForegroundColor = color;
            Console.Write($"  [{status}] ");
            Console.ResetColor();
            Console.WriteLine($"{testName}");
            Console.WriteLine($"         {message}");
        }
    }
}
