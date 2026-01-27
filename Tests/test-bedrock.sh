#!/bin/bash
# Test AWS Bedrock access

export AWS_ACCESS_KEY_ID="YOUR_ACCESS_KEY_ID"
export AWS_SECRET_ACCESS_KEY="YOUR_SECRET_ACCESS_KEY"
export AWS_DEFAULT_REGION="us-east-1"

echo "=== Testing AWS Identity ==="
aws sts get-caller-identity

echo ""
echo "=== Listing Anthropic Models ==="
aws bedrock list-foundation-models --by-provider anthropic --query 'modelSummaries[*].[modelId,modelLifecycle.status]' --output table 2>&1 | head -20

echo ""
echo "=== Testing Model Invocation ==="
aws bedrock-runtime invoke-model \
    --model-id "anthropic.claude-3-haiku-20240307-v1:0" \
    --body '{"anthropic_version":"bedrock-2023-05-31","max_tokens":50,"messages":[{"role":"user","content":"Say hello in 3 words"}]}' \
    --content-type "application/json" \
    --accept "application/json" \
    /tmp/bedrock-response.json 2>&1

if [ -f /tmp/bedrock-response.json ]; then
    echo "Response:"
    cat /tmp/bedrock-response.json
    rm /tmp/bedrock-response.json
fi
