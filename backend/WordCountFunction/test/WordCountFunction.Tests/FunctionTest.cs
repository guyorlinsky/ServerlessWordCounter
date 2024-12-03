using Xunit;
using Xunit.Abstractions;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.APIGatewayEvents;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using System.Collections.Generic;
using System.IO;

namespace WordCountFunction.Tests;

public class FunctionTest
{
    private readonly string _bucketName = "word-count-results"; // Replace with your bucket name
    private readonly ITestOutputHelper _output;

    public FunctionTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task TestS3Credentials()
    {
        // This test verifies that your AWS credentials are properly configured
        var s3Client = new AmazonS3Client();
        
        try
        {
            // Try to list buckets (this will fail if credentials are invalid)
            var response = await s3Client.ListBucketsAsync();
            Assert.NotNull(response.Buckets);
            
            // Try to check if our specific bucket exists
            try
            {
                await s3Client.GetBucketLocationAsync(_bucketName);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new Exception($"Bucket '{_bucketName}' does not exist. Please create it first.");
            }
        }
        catch (AmazonS3Exception ex)
        {
            throw new Exception("AWS credentials are not properly configured: " + ex.Message);
        }
    }

    [Fact]
    public async Task TestWordCountFunction()
    {
        // Arrange
        var function = new Function();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            Body = "hello world hello test world"
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.Equal(200, response.StatusCode);
        Assert.Contains("message", response.Body);
        Assert.Contains("resultLocation", response.Body);
        Assert.Contains("wordCount", response.Body);
        Assert.Contains("uniqueWords", response.Body);
    }

    [Fact]
    public async Task ManualTestWithResults()
    {
        // 1. Create the Lambda function instance
        var function = new Function();
        var context = new TestLambdaContext();

        // Sample text to count words from
        string sampleText = @"
            This is a sample text.
            It has multiple lines and repeated words.
            This text will help us test our word counter.
            The word counter should count each word.
            Let's see how it handles this text!
        ";

        // 2. Create and send the request
        var request = new APIGatewayProxyRequest
        {
            Body = sampleText
        };

        // 3. Process the text and save to S3
        var response = await function.FunctionHandler(request, context);
        _output.WriteLine($"Response from function: {response.Body}");

        // 4. Retrieve and display the results from S3
        if (response.StatusCode == 200)
        {
            var s3Key = response.Body.Replace("Results saved to ", "");
            var s3Client = new AmazonS3Client();

            try
            {
                // Get the results file from S3
                var getObjectResponse = await s3Client.GetObjectAsync(_bucketName, s3Key);
                using var reader = new StreamReader(getObjectResponse.ResponseStream);
                var jsonContent = await reader.ReadToEndAsync();
                
                // Parse and display the word counts
                var wordCounts = JsonSerializer.Deserialize<Dictionary<string, int>>(jsonContent);
                
                _output.WriteLine("\nWord count results:");
                foreach (var count in wordCounts.OrderByDescending(x => x.Value))
                {
                    _output.WriteLine($"'{count.Key}': {count.Value} times");
                }
            }
            catch (AmazonS3Exception ex)
            {
                _output.WriteLine($"Error retrieving results: {ex.Message}");
            }
        }
    }
}
