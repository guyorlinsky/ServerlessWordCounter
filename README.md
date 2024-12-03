# Word Count Serverless Application

A scalable AWS Lambda function that processes text files and provides word count analysis.

## Setup

1. Prerequisites
   - AWS CLI configured
   - Terraform installed
   - .NET 6.0 SDK
   - PowerShell

2. AWS Credentials
   Do NOT commit AWS credentials to the repository. Instead:
   - Use AWS credentials file (`~/.aws/credentials`)
   - Or set environment variables:
     ```bash
     export AWS_ACCESS_KEY_ID="your_access_key"
     export AWS_SECRET_ACCESS_KEY="your_secret_key"
     ```

3. Deployment
   ```powershell
   # Build and deploy
   cd scripts
   ./deploy.ps1
   ```

## Project Map
```
📦 Word Count Serverless Application
├── 📂 backend                        # Application code
│   └── 📂 WordCountFunction
│       ├── 📂 src
│       │   └── 📂 WordCountFunction
│       │       ├── 📄 Function.cs    # Main Lambda function
│       │       └── 📄 *.csproj       # Project configuration
│       └── 📂 test
│           └── 📂 WordCountFunction.Tests
│               └── 📄 FunctionTest.cs # Unit tests
│
├── 📂 infrastructure                 # Terraform IaC
│   ├── 📄 main.tf                   # Main infrastructure definition
│   ├── 📄 variables.tf              # Variable definitions
│   ├── 📄 provider.tf               # AWS provider configuration
│   └── 📄 provider.tf.template      # Template for provider setup
│
├── 📂 scripts                        # Deployment scripts
│   ├── 📄 build-lambda.ps1          # Build .NET Lambda package
│   └── 📄 deploy.ps1                # Deploy infrastructure
│
├── 📄 README.md                      # Project documentation
└── 📄 .gitignore                     # Git ignore rules
```

## Architecture Diagram
```
┌─────────────┐         ┌─────────────┐         ┌─────────────┐
│   Client    │ ──POST─>│     API     │ ───────>│   Lambda    │
│             │         │   Gateway   │         │  Function   │
└─────────────┘         └─────────────┘         └──────┬──────┘
                                                       │
                                                       │
                                                       ▼
                                               ┌─────────────┐
                                               │     S3      │
                                               │   Bucket    │
                                               └─────────────┘
```

## Security Notes
- Never commit AWS credentials
- Use environment variables or AWS credentials file
- Keep terraform.tfstate secure
- Review CORS settings before production use

## Architecture
- AWS Lambda (.NET 6.0)
- API Gateway (HTTP API)
- S3 (Results Storage)
- Infrastructure as Code (Terraform)

## Limitations and Considerations

### AWS Lambda Limitations
- API Gateway has a payload size limit of 10 MB for incoming requests
  - Requests with text files larger than 10 MB will be rejected
- Lambda functions have memory and execution time limits:
  - Maximum execution time: 15 minutes
  - Memory: Configurable (current setting: 256 MB)
  - Consider these limits when processing large files
