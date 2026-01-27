# WingetWizard AI Service Test Script
# Tests Claude, Perplexity, and AWS Bedrock APIs

$ErrorActionPreference = "Continue"

Write-Host "==========================================="
Write-Host "  WingetWizard AI Service Test Suite"
Write-Host "==========================================="
Write-Host ""

# Load configuration
$configPath = Join-Path $PSScriptRoot "..\config.json"
if (-not (Test-Path $configPath)) {
    Write-Host "[ERROR] Config file not found at: $configPath" -ForegroundColor Red
    exit 1
}

$config = Get-Content $configPath | ConvertFrom-Json
Write-Host "[INFO] Config loaded from: $configPath"
Write-Host ""

$passed = 0
$failed = 0

# Test data - a simple upgrade scenario
$testPrompt = @"
You are a software analyst. Provide a brief (2-3 sentences) recommendation for upgrading Visual Studio Code from version 1.85.0 to 1.86.0. Focus on security and stability.
"@

# ===========================================
# Test 1: Claude API (Anthropic Direct)
# ===========================================
Write-Host "[1/3] Testing Claude (Anthropic Direct)..." -ForegroundColor Cyan

if ([string]::IsNullOrEmpty($config.AnthropicApiKey)) {
    Write-Host "  [SKIP] Claude API key not configured" -ForegroundColor Yellow
} else {
    try {
        $claudeBody = @{
            model = $config.SelectedAiModel
            max_tokens = 500
            messages = @(
                @{
                    role = "user"
                    content = $testPrompt
                }
            )
        } | ConvertTo-Json -Depth 5

        $claudeHeaders = @{
            "x-api-key" = $config.AnthropicApiKey
            "anthropic-version" = "2023-06-01"
            "Content-Type" = "application/json"
        }

        $response = Invoke-RestMethod -Uri "https://api.anthropic.com/v1/messages" `
            -Method Post `
            -Headers $claudeHeaders `
            -Body $claudeBody `
            -TimeoutSec 60

        if ($response.content -and $response.content[0].text) {
            $textLength = $response.content[0].text.Length
            Write-Host "  [PASS] Claude API working! Response: $textLength chars" -ForegroundColor Green
            Write-Host "  Model: $($response.model)" -ForegroundColor Gray
            $passed++
        } else {
            Write-Host "  [FAIL] Claude API returned empty response" -ForegroundColor Red
            $failed++
        }
    } catch {
        Write-Host "  [FAIL] Claude API error: $($_.Exception.Message)" -ForegroundColor Red
        $failed++
    }
}
Write-Host ""

# ===========================================
# Test 2: Perplexity API
# ===========================================
Write-Host "[2/3] Testing Perplexity..." -ForegroundColor Cyan

if ([string]::IsNullOrEmpty($config.PerplexityApiKey)) {
    Write-Host "  [SKIP] Perplexity API key not configured" -ForegroundColor Yellow
} else {
    try {
        $perplexityBody = @{
            model = "sonar"
            messages = @(
                @{
                    role = "system"
                    content = "You are a software research assistant."
                },
                @{
                    role = "user"
                    content = $testPrompt
                }
            )
            max_tokens = 500
            temperature = 0.1
        } | ConvertTo-Json -Depth 5

        $perplexityHeaders = @{
            "Authorization" = "Bearer $($config.PerplexityApiKey)"
            "Content-Type" = "application/json"
        }

        $response = Invoke-RestMethod -Uri "https://api.perplexity.ai/chat/completions" `
            -Method Post `
            -Headers $perplexityHeaders `
            -Body $perplexityBody `
            -TimeoutSec 60

        if ($response.choices -and $response.choices[0].message.content) {
            $textLength = $response.choices[0].message.content.Length
            Write-Host "  [PASS] Perplexity API working! Response: $textLength chars" -ForegroundColor Green
            Write-Host "  Model: $($response.model)" -ForegroundColor Gray
            $passed++
        } else {
            Write-Host "  [FAIL] Perplexity API returned empty response" -ForegroundColor Red
            $failed++
        }
    } catch {
        Write-Host "  [FAIL] Perplexity API error: $($_.Exception.Message)" -ForegroundColor Red
        $failed++
    }
}
Write-Host ""

# ===========================================
# Test 3: AWS Bedrock API
# ===========================================
Write-Host "[3/3] Testing AWS Bedrock..." -ForegroundColor Cyan

if ([string]::IsNullOrEmpty($config.AwsAccessKeyId) -or [string]::IsNullOrEmpty($config.AwsSecretAccessKey)) {
    Write-Host "  [SKIP] AWS Bedrock credentials not configured" -ForegroundColor Yellow
} else {
    try {
        # AWS Signature Version 4 signing
        $region = if ($config.AwsRegion) { $config.AwsRegion } else { "us-east-1" }
        $service = "bedrock-runtime"
        $modelId = "us.anthropic.claude-sonnet-4-5-20250929-v1:0"
        $endpoint = "https://bedrock-runtime.$region.amazonaws.com/model/$modelId/invoke"

        $bedrockBody = @{
            anthropic_version = "bedrock-2023-05-31"
            max_tokens = 500
            messages = @(
                @{
                    role = "user"
                    content = $testPrompt
                }
            )
        } | ConvertTo-Json -Depth 5

        # Create signature
        $now = [DateTime]::UtcNow
        $dateStamp = $now.ToString("yyyyMMdd")
        $amzDate = $now.ToString("yyyyMMddTHHmmssZ")

        $uri = [System.Uri]$endpoint
        $canonicalUri = $uri.AbsolutePath
        $canonicalQueryString = ""

        # Hash payload
        $payloadBytes = [System.Text.Encoding]::UTF8.GetBytes($bedrockBody)
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        $payloadHash = [BitConverter]::ToString($sha256.ComputeHash($payloadBytes)).Replace("-", "").ToLower()

        # Create canonical headers
        $canonicalHeaders = "host:$($uri.Host)`nx-amz-date:$amzDate`n"
        $signedHeaders = "host;x-amz-date"

        # Create canonical request
        $canonicalRequest = "POST`n$canonicalUri`n$canonicalQueryString`n$canonicalHeaders`n$signedHeaders`n$payloadHash"
        $canonicalRequestHash = [BitConverter]::ToString($sha256.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($canonicalRequest))).Replace("-", "").ToLower()

        # Create string to sign
        $algorithm = "AWS4-HMAC-SHA256"
        $credentialScope = "$dateStamp/$region/$service/aws4_request"
        $stringToSign = "$algorithm`n$amzDate`n$credentialScope`n$canonicalRequestHash"

        # Calculate signature
        function HmacSha256($key, $data) {
            $hmac = New-Object System.Security.Cryptography.HMACSHA256
            $hmac.Key = $key
            return $hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($data))
        }

        $kSecret = [System.Text.Encoding]::UTF8.GetBytes("AWS4$($config.AwsSecretAccessKey)")
        $kDate = HmacSha256 $kSecret $dateStamp
        $kRegion = HmacSha256 $kDate $region
        $kService = HmacSha256 $kRegion $service
        $kSigning = HmacSha256 $kService "aws4_request"

        $signatureBytes = HmacSha256 $kSigning $stringToSign
        $signature = [BitConverter]::ToString($signatureBytes).Replace("-", "").ToLower()

        # Create authorization header
        $authHeader = "$algorithm Credential=$($config.AwsAccessKeyId)/$credentialScope, SignedHeaders=$signedHeaders, Signature=$signature"

        $bedrockHeaders = @{
            "X-Amz-Date" = $amzDate
            "Authorization" = $authHeader
            "Content-Type" = "application/json"
        }

        $response = Invoke-RestMethod -Uri $endpoint `
            -Method Post `
            -Headers $bedrockHeaders `
            -Body $bedrockBody `
            -TimeoutSec 60

        if ($response.content -and $response.content[0].text) {
            $textLength = $response.content[0].text.Length
            Write-Host "  [PASS] Bedrock API working! Response: $textLength chars" -ForegroundColor Green
            Write-Host "  Model: $modelId" -ForegroundColor Gray
            $passed++
        } else {
            Write-Host "  [FAIL] Bedrock API returned empty response" -ForegroundColor Red
            $failed++
        }
    } catch {
        Write-Host "  [FAIL] Bedrock API error: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $errorBody = $reader.ReadToEnd()
            Write-Host "  Details: $errorBody" -ForegroundColor Red
        }
        $failed++
    }
}
Write-Host ""

# ===========================================
# Summary
# ===========================================
Write-Host "==========================================="
Write-Host "  Results: $passed passed, $failed failed"
Write-Host "==========================================="

if ($failed -gt 0) {
    exit 1
} else {
    exit 0
}
