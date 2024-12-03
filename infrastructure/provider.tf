provider "aws" {
  region = var.aws_region
  # Using environment variables for credentials
  # Make sure AWS_ACCESS_KEY_ID and AWS_SECRET_ACCESS_KEY are set
}
