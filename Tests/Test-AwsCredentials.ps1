# AWS Credentials Verification Test
# Tests if AWS credentials are valid and have basic Bedrock access

$ErrorActionPreference = "Continue"

Write-Host "==========================================="
Write-Host "  AWS Credentials Verification"
Write-Host "==========================================="
Write-Host ""

# Load configuration
$configPath = Join-Path $PSScriptRoot "..\config.json"
$config = Get-Content $configPath | ConvertFrom-Json

$accessKeyId = $config.AwsAccessKeyId
$secretKey = $config.AwsSecretAccessKey
$region = "us-east-1"

Write-Host "[INFO] Testing AWS credentials..."
Write-Host "[INFO] Access Key ID: $($accessKeyId.Substring(0,8))..."
Write-Host "[INFO] Region: $region"
Write-Host ""

# Test 1: List Foundation Models (requires bedrock:ListFoundationModels)
Write-Host "[Test 1] Listing Bedrock Foundation Models..." -ForegroundColor Cyan

function Sign-AwsRequest {
    param($Method, $Uri, $Service, $Body = "")

    $now = [DateTime]::UtcNow
    $dateStamp = $now.ToString("yyyyMMdd")
    $amzDate = $now.ToString("yyyyMMddTHHmmssZ")

    $parsedUri = [System.Uri]$Uri
    $canonicalUri = $parsedUri.AbsolutePath
    $canonicalQueryString = if ($parsedUri.Query) { $parsedUri.Query.TrimStart('?') } else { "" }

    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $payloadBytes = if ($Body) { [System.Text.Encoding]::UTF8.GetBytes($Body) } else { [byte[]]@() }
    $payloadHash = [BitConverter]::ToString($sha256.ComputeHash($payloadBytes)).Replace("-", "").ToLower()

    $canonicalHeaders = "host:$($parsedUri.Host)`nx-amz-date:$amzDate`n"
    $signedHeaders = "host;x-amz-date"

    $canonicalRequest = "$Method`n$canonicalUri`n$canonicalQueryString`n$canonicalHeaders`n$signedHeaders`n$payloadHash"
    $canonicalRequestHash = [BitConverter]::ToString($sha256.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($canonicalRequest))).Replace("-", "").ToLower()

    $algorithm = "AWS4-HMAC-SHA256"
    $credentialScope = "$dateStamp/$region/$Service/aws4_request"
    $stringToSign = "$algorithm`n$amzDate`n$credentialScope`n$canonicalRequestHash"

    function HmacSha256($key, $data) {
        $hmac = New-Object System.Security.Cryptography.HMACSHA256
        $hmac.Key = $key
        return $hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($data))
    }

    $kSecret = [System.Text.Encoding]::UTF8.GetBytes("AWS4$secretKey")
    $kDate = HmacSha256 $kSecret $dateStamp
    $kRegion = HmacSha256 $kDate $region
    $kService = HmacSha256 $kRegion $Service
    $kSigning = HmacSha256 $kService "aws4_request"

    $signatureBytes = HmacSha256 $kSigning $stringToSign
    $signature = [BitConverter]::ToString($signatureBytes).Replace("-", "").ToLower()

    return @{
        Authorization = "$algorithm Credential=$accessKeyId/$credentialScope, SignedHeaders=$signedHeaders, Signature=$signature"
        "X-Amz-Date" = $amzDate
    }
}

try {
    $listEndpoint = "https://bedrock.$region.amazonaws.com/foundation-models?byProvider=anthropic"
    $headers = Sign-AwsRequest -Method "GET" -Uri $listEndpoint -Service "bedrock"

    $response = Invoke-RestMethod -Uri $listEndpoint -Method Get -Headers $headers -TimeoutSec 30

    Write-Host "  [PASS] Credentials are valid!" -ForegroundColor Green
    Write-Host "  Available Anthropic models in $region`:" -ForegroundColor Gray

    foreach ($model in $response.modelSummaries | Select-Object -First 10) {
        $status = if ($model.modelLifecycle.status -eq "ACTIVE") { "[Active]" } else { "[$($model.modelLifecycle.status)]" }
        Write-Host "    - $($model.modelId) $status"
    }
    Write-Host ""
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "  [FAIL] Status: $statusCode" -ForegroundColor Red

    if ($statusCode -eq 403) {
        Write-Host "  Your IAM user lacks bedrock:ListFoundationModels permission" -ForegroundColor Yellow
    }
    elseif ($statusCode -eq 401) {
        Write-Host "  Invalid AWS credentials!" -ForegroundColor Red
    }

    if ($_.Exception.Response) {
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $errorBody = $reader.ReadToEnd()
            Write-Host "  Error: $errorBody" -ForegroundColor Yellow
        } catch {}
    }
}

# Test 2: Get caller identity using STS
Write-Host "[Test 2] Verifying AWS Identity (STS)..." -ForegroundColor Cyan

try {
    $stsEndpoint = "https://sts.$region.amazonaws.com/?Action=GetCallerIdentity&Version=2011-06-15"
    $headers = Sign-AwsRequest -Method "GET" -Uri $stsEndpoint -Service "sts"

    $response = Invoke-WebRequest -Uri $stsEndpoint -Method Get -Headers $headers -TimeoutSec 30

    if ($response.StatusCode -eq 200) {
        Write-Host "  [PASS] AWS credentials are valid!" -ForegroundColor Green

        # Parse XML response
        [xml]$xml = $response.Content
        $arn = $xml.GetCallerIdentityResponse.GetCallerIdentityResult.Arn
        $account = $xml.GetCallerIdentityResponse.GetCallerIdentityResult.Account

        Write-Host "  Account: $account" -ForegroundColor Gray
        Write-Host "  ARN: $arn" -ForegroundColor Gray
    }
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "  [FAIL] Status: $statusCode" -ForegroundColor Red
    Write-Host "  AWS credentials may be invalid or expired" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "==========================================="
Write-Host "  Diagnostic Complete"
Write-Host "==========================================="
Write-Host ""
Write-Host "If credentials are valid but models fail:" -ForegroundColor Yellow
Write-Host "1. Go to: https://console.aws.amazon.com/bedrock/home?region=us-east-1#/modelaccess"
Write-Host "2. Click 'Manage model access'"
Write-Host "3. Enable access for Anthropic Claude models"
Write-Host "4. Wait a few minutes for access to propagate"
Write-Host ""
Write-Host "Required IAM permissions:" -ForegroundColor Yellow
Write-Host "  - bedrock:InvokeModel"
Write-Host "  - bedrock:ListFoundationModels (for listing)"
