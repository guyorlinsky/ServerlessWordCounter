using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.S3;
using System.Text.Json;
using Amazon.S3.Model;
using System.Text.RegularExpressions;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace WordCountFunction;

public class Function
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private const int MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB limit
    private const string ALLOWED_FILE_TYPES = "text/plain";

    public Function()
    {
        _s3Client = new AmazonS3Client();
        _bucketName = Environment.GetEnvironmentVariable("RESULTS_BUCKET_NAME") ?? "word-count-results";
    }

    public Function(IAmazonS3 s3Client, string bucketName)
    {
        _s3Client = s3Client;
        _bucketName = bucketName;
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            // Input validation
            if (string.IsNullOrEmpty(request.Body))
            {
                return CreateResponse(400, "Request body is empty");
            }

            if (request.Headers != null && 
                request.Headers.TryGetValue("Content-Type", out var contentType) && 
                !contentType.StartsWith(ALLOWED_FILE_TYPES))
            {
                return CreateResponse(400, "Invalid file type. Only text files are allowed.");
            }

            if (request.Body.Length > MAX_FILE_SIZE)
            {
                return CreateResponse(400, "File size exceeds maximum limit of 5MB");
            }

            // Log request metadata
            context.Logger.LogInformation($"Processing file of size: {request.Body.Length} bytes");

            // Word count logic with input sanitization
            var wordCounts = CountWords(request.Body);

            // Generate a unique filename with timestamp
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss");
            var s3Key = $"results/{timestamp}-{Guid.NewGuid()}.json";

            // Upload to S3 with metadata and encryption
            await UploadToS3(s3Key, wordCounts, context);

            // Return success response with result location
            return CreateResponse(200, new
            {
                message = "Word count completed successfully",
                resultLocation = s3Key,
                wordCount = wordCounts.Values.Sum(),
                uniqueWords = wordCounts.Count
            });
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error processing request: {ex.Message}");
            return CreateResponse(500, "Internal server error occurred");
        }
    }

    private Dictionary<string, int> CountWords(string text)
    {
        // Sanitize input and split into words
        var sanitizedText = Regex.Replace(text, @"[^\w\s-]", " ");
        var words = sanitizedText.Split(new[] { ' ', '\n', '\r', '\t' }, 
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Count words (case-insensitive)
        return words
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .GroupBy(word => word.ToLowerInvariant())
            .ToDictionary(
                group => group.Key,
                group => group.Count()
            );
    }

    private async Task UploadToS3(string key, Dictionary<string, int> data, ILambdaContext context)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });

        var metadata = new Dictionary<string, string>
        {
            { "ProcessedDate", DateTime.UtcNow.ToString("O") },
            { "WordCount", data.Values.Sum().ToString() },
            { "UniqueWords", data.Count.ToString() }
        };

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            ContentBody = json,
            ContentType = "application/json",
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
        };

        // Add metadata after creating the request
        foreach (var item in metadata)
        {
            request.Metadata.Add(item.Key, item.Value);
        }

        try
        {
            await _s3Client.PutObjectAsync(request);
            context.Logger.LogInformation($"Successfully uploaded results to {key}");
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error uploading to S3: {ex.Message}");
            throw;
        }
    }

    private APIGatewayProxyResponse CreateResponse(int statusCode, object body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Body = JsonSerializer.Serialize(body),
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "Access-Control-Allow-Origin", "*" },
                { "Access-Control-Allow-Methods", "POST" }
            }
        };
    }
}
