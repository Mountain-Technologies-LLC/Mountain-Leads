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

namespace Lambda.Functions;

public class ListLeadsFunction
{
    private readonly IDynamoDbHelper _dynamoDbHelper;

    public ListLeadsFunction()
    {
        var tableName = Environment.GetEnvironmentVariable("TABLE_NAME") 
            ?? throw new InvalidOperationException("TABLE_NAME environment variable is not set");
        
        var dynamoDbClient = new AmazonDynamoDBClient();
        _dynamoDbHelper = new DynamoDbHelper(dynamoDbClient, tableName);
    }

    // Constructor for testing with dependency injection
    public ListLeadsFunction(IDynamoDbHelper dynamoDbHelper)
    {
        _dynamoDbHelper = dynamoDbHelper;
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            context.Logger.LogInformation($"ListLeadsFunction invoked");

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

            // Query all leads for the user
            var leads = await _dynamoDbHelper.QueryLeadsByUserIdAsync(userId);
            context.Logger.LogInformation($"Found {leads.Count} leads for user {userId}");

            // Return success response
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Access-Control-Allow-Origin", "*" }
                },
                Body = JsonSerializer.Serialize(new ApiResponse<ListLeadsResponse>
                {
                    Success = true,
                    Data = new ListLeadsResponse
                    {
                        Leads = leads,
                        Count = leads.Count
                    }
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
