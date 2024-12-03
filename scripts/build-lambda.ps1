# Navigate to the Lambda project directory
$projectDir = Join-Path $PSScriptRoot "..\backend\WordCountFunction\src\WordCountFunction"
cd $projectDir

# Build and publish the Lambda function
dotnet publish -c Release

# Create a zip file for Lambda deployment
$publishFolder = Join-Path $projectDir "bin\Release\net6.0\publish"
$zipFile = Join-Path $publishFolder "WordCountFunction.zip"

# Ensure the parent directory exists
$zipFileDirectory = Split-Path -Parent $zipFile
if (-not (Test-Path $zipFileDirectory)) {
    New-Item -ItemType Directory -Path $zipFileDirectory
}

# Remove existing zip if it exists
if (Test-Path $zipFile) {
    Remove-Item $zipFile
}

# Create the zip file
Compress-Archive -Path "$publishFolder\*" -DestinationPath $zipFile

Write-Host "Lambda package created at: $zipFile"

# Return to the original directory
cd $PSScriptRoot
