# Check for AWS credentials
if (-not (Test-Path "~/.aws/credentials") -and 
    (-not $env:AWS_ACCESS_KEY_ID -or -not $env:AWS_SECRET_ACCESS_KEY)) {
    Write-Host "No AWS credentials found. Please either:"
    Write-Host "1. Create ~/.aws/credentials file with your AWS credentials"
    Write-Host "2. Set AWS_ACCESS_KEY_ID and AWS_SECRET_ACCESS_KEY environment variables"
    exit 1
}

# Add Terraform to the current session's PATH
$env:Path += ";C:\terraform"

# Store original directory
$originalDir = Get-Location

# Build and package the Lambda function
Write-Host "Building and packaging Lambda function..."
& $PSScriptRoot\build-lambda.ps1

# Navigate to infrastructure directory
$infraDir = Join-Path $PSScriptRoot "..\infrastructure"
cd $infraDir

# Initialize and apply Terraform
Write-Host "Initializing Terraform..."
terraform init

Write-Host "Applying Terraform configuration..."
terraform apply -auto-approve

# Get the outputs
Write-Host "`nDeployment complete! Here are your endpoints:"
Write-Host "API Endpoint: $(terraform output -raw api_endpoint)"
Write-Host "S3 Bucket: $(terraform output -raw s3_bucket_name)"

# Return to original directory
cd $originalDir
