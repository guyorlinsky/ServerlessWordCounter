
/// the _____ASYNC_____ keyword means that this method will perform asynchronous operations,
/// allowing it to handle non-blocking I/O operations efficiently (such as network or database calls).
/// It allows the method to await tasks or other asynchronous operations within it,
/// enabling it to return control to the caller without blocking the thread until the task completes.

/// _____Task_____ is used for asynchronous operations.
/// It indicates that the method will return a value at some point in the future.

/// <summary>
/// This serves as the entry point for the AWS Lambda handler.
/// </summary>
public class WordCountFunction
{
 
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        // Extract file content from the request
        var fileContent = ExtractFileContent(request);
        
        // Word count logic
        var wordCounts = CountWords(fileContent);

        // Upload result to S3
		// Guid Guarantees uniqueness for each result.
        var s3Key = $"results/{Guid.NewGuid()}.json";
        await UploadToS3(s3Key, wordCounts);
		
		//  Return a successful HTTP response with the location of the stored results.
        return new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = $"Results saved to {s3Key}"
        };
    }

    private Dictionary<string, int> CountWords(string text)
    {
        var words = text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
		// Groups words by their lowercase representation (case-insensitive counting).
		// Converts the grouping into a dictionary where- Key: The word, Value: The word’s occurrence count.
        return words.GroupBy(word => word.ToLower())
            .ToDictionary(group => group.Key, group => group.Count());
    }

    private async Task UploadToS3(string key, Dictionary<string, int> data)
    {
        var s3Client = new AmazonS3Client();// An SDK client for interacting with Amazon S3.
        var json = JsonSerializer.Serialize(data);// Converts the Dictionary to a JSON string for storage.

		// PutObjectRequest: Specifies the bucket, key, content, and metadata for the S3 upload.
        await s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = Environment.GetEnvironmentVariable("S3_BUCKET"), //  Keeps the bucket name configurable.
            Key = key,
            ContentBody = json,
            ContentType = "application/json"
        });
    }
	
	private string ExtractFileContent(APIGatewayProxyRequest request)
	{
    	try
    	{
        	// Extract the base64-encoded file content from the body
        	if (request.Body == null)
        	{
            	throw new ArgumentException("Request body is empty or null.");
        	}

        	// Decode the base64 string to a byte array
        	byte[] fileBytes = Convert.FromBase64String(request.Body);

	        // Assuming the file is a text file, we convert the byte array to a string.
    	    // Note: You may need to adjust encoding based on the file type (e.g., UTF8).
        	string fileContent = Encoding.UTF8.GetString(fileBytes);

        	return fileContent;
    	}
    	catch (Exception ex)
    	{
        	// Handle any errors gracefully
        	throw new InvalidOperationException("Error extracting file content", ex);
    	}
	}
}


