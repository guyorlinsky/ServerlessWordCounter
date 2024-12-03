terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 4.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
  }
}

# Random suffix for globally unique S3 bucket name
resource "random_string" "suffix" {
  length  = 8
  special = false
  upper   = false
}

# S3 Bucket for storing word count results
resource "aws_s3_bucket" "word_count_bucket" {
  bucket = "word-count-results-${random_string.suffix.result}"
}

# S3 bucket versioning
resource "aws_s3_bucket_versioning" "bucket_versioning" {
  bucket = aws_s3_bucket.word_count_bucket.id
  versioning_configuration {
    status = "Enabled"
  }
}

# S3 bucket encryption
resource "aws_s3_bucket_server_side_encryption_configuration" "bucket_encryption" {
  bucket = aws_s3_bucket.word_count_bucket.id
  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

# S3 bucket public access block
resource "aws_s3_bucket_public_access_block" "bucket_public_access_block" {
  bucket                  = aws_s3_bucket.word_count_bucket.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

# Lambda function
resource "aws_lambda_function" "word_count_function" {
  filename         = "../backend/WordCountFunction/src/WordCountFunction/bin/Release/net6.0/publish/WordCountFunction.zip"
  function_name    = "WordCountFunction"
  role            = aws_iam_role.lambda_exec_role.arn
  handler         = "WordCountFunction::WordCountFunction.Function::FunctionHandler"
  runtime         = "dotnet6"
  timeout         = 30
  memory_size     = 256

  environment {
    variables = {
      RESULTS_BUCKET_NAME = aws_s3_bucket.word_count_bucket.id
    }
  }
}

# API Gateway
resource "aws_apigatewayv2_api" "word_count_api" {
  name          = "word-count-api"
  protocol_type = "HTTP"
  cors_configuration {
    allow_headers = ["Content-Type"]
    allow_methods = ["POST"]
    allow_origins = ["*"] # In production, restrict this to specific domains
  }
}

# API Gateway integration with Lambda
resource "aws_apigatewayv2_integration" "lambda_integration" {
  api_id           = aws_apigatewayv2_api.word_count_api.id
  integration_type = "AWS_PROXY"
  integration_uri  = aws_lambda_function.word_count_function.invoke_arn
}

# API Gateway route
resource "aws_apigatewayv2_route" "lambda_route" {
  api_id    = aws_apigatewayv2_api.word_count_api.id
  route_key = "POST /word-count"
  target    = "integrations/${aws_apigatewayv2_integration.lambda_integration.id}"
}

# API Gateway stage
resource "aws_apigatewayv2_stage" "lambda_stage" {
  api_id      = aws_apigatewayv2_api.word_count_api.id
  name        = "prod"
  auto_deploy = true
}

# Lambda execution role
resource "aws_iam_role" "lambda_exec_role" {
  name = "word_count_lambda_role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = "lambda.amazonaws.com"
      }
    }]
  })
}

# Lambda CloudWatch logs policy
resource "aws_iam_role_policy_attachment" "lambda_logs" {
  role       = aws_iam_role.lambda_exec_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

# Lambda S3 access policy
resource "aws_iam_role_policy" "lambda_s3_policy" {
  name = "lambda_s3_policy"
  role = aws_iam_role.lambda_exec_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "s3:PutObject",
          "s3:GetObject",
          "s3:ListBucket"
        ]
        Resource = [
          aws_s3_bucket.word_count_bucket.arn,
          "${aws_s3_bucket.word_count_bucket.arn}/*"
        ]
      }
    ]
  })
}

# Allow API Gateway to invoke Lambda
resource "aws_lambda_permission" "api_gw" {
  statement_id  = "AllowAPIGatewayInvoke"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.word_count_function.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.word_count_api.execution_arn}/*/*/word-count"
}

# Outputs
output "api_endpoint" {
  value = "${aws_apigatewayv2_stage.lambda_stage.invoke_url}/word-count"
}

output "s3_bucket_name" {
  value = aws_s3_bucket.word_count_bucket.id
}
