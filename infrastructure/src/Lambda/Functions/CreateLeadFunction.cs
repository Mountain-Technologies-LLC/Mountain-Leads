using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.DynamoDBv2;
using Lambda.Models;
using Lambda.Utilities;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Lambda.Functions;

public class CreateLeadFunction
{
    private readonly IDynamoDbHelper _dynamoDbHelper;

    public CreateLeadFunction()
    {
        var tableName = Environment.GetEnvironmentVariable("TABLE_NAME") 
            ?? throw new InvalidOperationException("TABLE_NAME environment variable is not set");
        
        var dynamoDbClient = new AmazonDynamoDBClient();
        _dynamoDbHelper = new DynamoDbHelper(dynamoDbClient, tableName);
    }

    // Constructor for testing with dependency injection
    public CreateLeadFunction(IDynamoDbHelper dynamoDbHelper)
    {
        _dynamoDbHelper = dynamoDbHelper;
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            context.Logger.LogInformation($"CreateLeadFunction invoked");

            // Extract userId from request context (Cognito authorizer)
            string userId;
            try
            {
                userId = JwtHelper.ExtractUserId(request);
                context.Logger.LogInformation($"Extracted userId: {userId}");
            }
            catch (ArgumentException ex)
            {
                context.Logger.LogError($"Authorization error: {ex.Message}");
                return CreateErrorResponse(
                    HttpStatusCode.Unauthorized,
                    "AUTH_TOKEN_MISSING",
                    "Valid authorization token is required"
                );
            }

            // Parse request body
            if (string.IsNullOrWhiteSpace(request.Body))
            {
                return CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    "VALIDATION_FAILED",
                    "Request body is required"
                );
            }

            CreateLeadRequest? createRequest;
            try
            {
                createRequest = JsonSerializer.Deserialize<CreateLeadRequest>(request.Body);
            }
            catch (JsonException ex)
            {
                context.Logger.LogError($"JSON parsing error: {ex.Message}");
                return CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    "VALIDATION_FAILED",
                    "Invalid JSON in request body"
                );
            }

            // Validate required fields
            if (createRequest == null || string.IsNullOrWhiteSpace(createRequest.Name))
            {
                return CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    "VALIDATION_FAILED",
                    "Name is required"
                );
            }

            // Create lead object
            var now = DateTime.UtcNow.ToString("o");
            var lead = new Lead
            {
                UserId = userId,
                LeadId = Guid.NewGuid().ToString(),
                Name = createRequest.Name,
                Title = createRequest.Title,
                Company = createRequest.Company,
                Phone = createRequest.Phone,
                Email = createRequest.Email,
                Location = createRequest.Location,
                Notes = createRequest.Notes,
                CreatedAt = now,
                UpdatedAt = now
            };

            // Store in DynamoDB
            await _dynamoDbHelper.CreateLeadAsync(lead);
            context.Logger.LogInformation($"Lead created successfully: {lead.LeadId}");

            // Return success response
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Access-Control-Allow-Origin", "*" }
                },
                Body = JsonSerializer.Serialize(new ApiResponse<Lead>
                {
                    Success = true,
                    Data = lead
                })
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Unexpected error: {ex.Message}");
            context.Logger.LogError($"Stack trace: {ex.StackTrace}");
            
            return CreateErrorResponse(
                HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR",
                "An unexpected error occurred"
            );
        }
    }

    private static APIGatewayProxyResponse CreateErrorResponse(HttpStatusCode statusCode, string errorCode, string message)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)statusCode,
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "Access-Control-Allow-Origin", "*" }
            },
            Body = JsonSerializer.Serialize(new ApiResponse<object>
            {
                Success = false,
                Error = new ErrorDetails
                {
                    Code = errorCode,
                    Message = message
                }
            })
        };
    }
}
