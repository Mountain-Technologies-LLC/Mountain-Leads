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

public class InitLeadsFunction
{
    private readonly IDynamoDbHelper _dynamoDbHelper;

    public InitLeadsFunction()
    {
        var tableName = Environment.GetEnvironmentVariable("TABLE_NAME") 
            ?? throw new InvalidOperationException("TABLE_NAME environment variable is not set");
        
        var dynamoDbClient = new AmazonDynamoDBClient();
        _dynamoDbHelper = new DynamoDbHelper(dynamoDbClient, tableName);
    }

    // Constructor for testing with dependency injection
    public InitLeadsFunction(IDynamoDbHelper dynamoDbHelper)
    {
        _dynamoDbHelper = dynamoDbHelper;
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            context.Logger.LogInformation($"InitLeadsFunction invoked");

            // Extract userId and email from request context (Cognito authorizer)
            string userId;
            string email;
            try
            {
                userId = JwtHelper.ExtractUserId(request);
                email = JwtHelper.ExtractEmail(request);
                context.Logger.LogInformation($"Extracted userId: {userId}, email: {email}");
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

            var now = DateTime.UtcNow.ToString("o");

            // Create Anthony Pearson default lead with all fields populated
            var anthonyPearsonLead = new Lead
            {
                UserId = userId,
                LeadId = Guid.NewGuid().ToString(),
                Name = "Anthony Pearson",
                Title = "CTO",
                Company = "Mountain Technologies LLC",
                Phone = "952-111-1111",
                Email = "info@mountaintechnologiesllc.com",
                Location = "Minneapolis, MN",
                Notes = "Likes to code",
                CreatedAt = now,
                UpdatedAt = now
            };

            // Create user email lead with only email field populated
            var userEmailLead = new Lead
            {
                UserId = userId,
                LeadId = Guid.NewGuid().ToString(),
                Name = string.Empty,
                Title = null,
                Company = null,
                Phone = null,
                Email = email,
                Location = null,
                Notes = null,
                CreatedAt = now,
                UpdatedAt = now
            };

            // Store both leads in DynamoDB
            await _dynamoDbHelper.CreateLeadAsync(anthonyPearsonLead);
            await _dynamoDbHelper.CreateLeadAsync(userEmailLead);

            context.Logger.LogInformation($"Default leads created successfully for user {userId}");

            // Return success response
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Access-Control-Allow-Origin", "*" }
                },
                Body = JsonSerializer.Serialize(new ApiResponse<object>
                {
                    Success = true,
                    Data = new 
                    { 
                        message = "Default leads created successfully",
                        leads = new[] { anthonyPearsonLead, userEmailLead }
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
