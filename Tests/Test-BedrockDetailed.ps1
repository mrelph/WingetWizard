# Detailed AWS Bedrock Diagnostic Test
# Tests various model IDs and endpoints to diagnose 403 errors

$ErrorActionPreference = "Continue"

Write-Host "==========================================="
Write-Host "  AWS Bedrock Detailed Diagnostic"
Write-Host "==========================================="
Write-Host ""

# Load configuration
$configPath = Join-Path $PSScriptRoot "..\config.json"
$config = Get-Content $configPath | ConvertFrom-Json

$accessKeyId = $config.AwsAccessKeyId
$secretKey = $config.AwsSecretAccessKey
$region = "us-east-1"

Write-Host "[INFO] AWS Access Key ID: $($accessKeyId.Substring(0,8))..."
Write-Host "[INFO] Region: $region"
Write-Host ""

# Test prompt
$testPrompt = "Say 'Hello from Bedrock' in exactly 5 words."

# Models to test
$models = @(
    @{ Id = "us.anthropic.claude-sonnet-4-5-20250929-v1:0"; Name = "Claude Sonnet 4.5 (Cross-Region)" },
    @{ Id = "anthropic.claude-3-5-sonnet-20241022-v2:0"; Name = "Claude 3.5 Sonnet v2" },
    @{ Id = "anthropic.claude-3-sonnet-20240229-v1:0"; Name = "Claude 3 Sonnet" },
    @{ Id = "anthropic.claude-3-haiku-20240307-v1:0"; Name = "Claude 3 Haiku" }
)

function Test-BedrockModel {
    param($ModelId, $ModelName)

    Write-Host "Testing: $ModelName" -ForegroundColor Cyan
    Write-Host "  Model ID: $ModelId"

    try {
        $endpoint = "https://bedrock-runtime.$region.amazonaws.com/model/$ModelId/invoke"
        Write-Host "  Endpoint: $endpoint"

        $body = @{
            anthropic_version = "bedrock-2023-05-31"
            max_tokens = 100
            messages = @(
                @{
                    role = "user"
                    content = $testPrompt
                }
            )
        } | ConvertTo-Json -Depth 5

        # AWS Signature V4
        $now = [DateTime]::UtcNow
        $dateStamp = $now.ToString("yyyyMMdd")
        $amzDate = $now.ToString("yyyyMMddTHHmmssZ")
        $service = "bedrock-runtime"

        $uri = [System.Uri]$endpoint
        $canonicalUri = $uri.AbsolutePath

        # Hash payload
        $payloadBytes = [System.Text.Encoding]::UTF8.GetBytes($body)
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        $payloadHash = [BitConverter]::ToString($sha256.ComputeHash($payloadBytes)).Replace("-", "").ToLower()

        # Canonical headers
        $canonicalHeaders = "host:$($uri.Host)`nx-amz-date:$amzDate`n"
        $signedHeaders = "host;x-amz-date"

        # Canonical request
        $canonicalRequest = "POST`n$canonicalUri`n`n$canonicalHeaders`n$signedHeaders`n$payloadHash"
        $canonicalRequestHash = [BitConverter]::ToString($sha256.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($canonicalRequest))).Replace("-", "").ToLower()

        # String to sign
        $algorithm = "AWS4-HMAC-SHA256"
        $credentialScope = "$dateStamp/$region/$service/aws4_request"
        $stringToSign = "$algorithm`n$amzDate`n$credentialScope`n$canonicalRequestHash"

        # Calculate signature
        function HmacSha256($key, $data) {
            $hmac = New-Object System.Security.Cryptography.HMACSHA256
            $hmac.Key = $key
            return $hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($data))
        }

        $kSecret = [System.Text.Encoding]::UTF8.GetBytes("AWS4$secretKey")
        $kDate = HmacSha256 $kSecret $dateStamp
        $kRegion = HmacSha256 $kDate $region
        $kService = HmacSha256 $kRegion $service
        $kSigning = HmacSha256 $kService "aws4_request"

        $signatureBytes = HmacSha256 $kSigning $stringToSign
        $signature = [BitConverter]::ToString($signatureBytes).Replace("-", "").ToLower()

        # Authorization header
        $authHeader = "$algorithm Credential=$accessKeyId/$credentialScope, SignedHeaders=$signedHeaders, Signature=$signature"

        $headers = @{
            "X-Amz-Date" = $amzDate
            "Authorization" = $authHeader
            "Content-Type" = "application/json"
        }

        $response = Invoke-RestMethod -Uri $endpoint -Method Post -Headers $headers -Body $body -TimeoutSec 60

        if ($response.content -and $response.content[0].text) {
            Write-Host "  [PASS] Response: $($response.content[0].text)" -ForegroundColor Green
            return $true
        }
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "  [FAIL] Status: $statusCode" -ForegroundColor Red

        if ($_.Exception.Response) {
            try {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $errorBody = $reader.ReadToEnd()
                $errorJson = $errorBody | ConvertFrom-Json
                Write-Host "  Error Type: $($errorJson.type)" -ForegroundColor Yellow
                Write-Host "  Message: $($errorJson.message)" -ForegroundColor Yellow
            }
            catch {
                Write-Host "  Raw Error: $errorBody" -ForegroundColor Yellow
            }
        }
        return $false
    }

    return $false
}

# Test each model
$passed = 0
$failed = 0

foreach ($model in $models) {
    $result = Test-BedrockModel -ModelId $model.Id -ModelName $model.Name
    if ($result) { $passed++ } else { $failed++ }
    Write-Host ""
}

Write-Host "==========================================="
Write-Host "  Results: $passed passed, $failed failed"
Write-Host "==========================================="

# Diagnostic suggestions
if ($failed -gt 0) {
    Write-Host ""
    Write-Host "Troubleshooting Steps:" -ForegroundColor Yellow
    Write-Host "1. Go to AWS Bedrock Console -> Model access"
    Write-Host "2. Request access to Claude models for us-east-1"
    Write-Host "3. Check IAM policy has bedrock:InvokeModel permission"
    Write-Host "4. For cross-region models, ensure inference profile access"
    Write-Host ""
    Write-Host "Required IAM Policy:" -ForegroundColor Cyan
    Write-Host @"
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "bedrock:InvokeModel",
                "bedrock:InvokeModelWithResponseStream"
            ],
            "Resource": [
                "arn:aws:bedrock:*::foundation-model/anthropic.*",
                "arn:aws:bedrock:*::foundation-model/us.anthropic.*"
            ]
        }
    ]
}
"@
}
