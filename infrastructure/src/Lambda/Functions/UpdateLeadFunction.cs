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

public class UpdateLeadFunction
{
    private readonly IDynamoDbHelper _dynamoDbHelper;

    public UpdateLeadFunction()
    {
        var tableName = Environment.GetEnvironmentVariable("TABLE_NAME") 
            ?? throw new InvalidOperationException("TABLE_NAME environment variable is not set");
        
        var dynamoDbClient = new AmazonDynamoDBClient();
        _dynamoDbHelper = new DynamoDbHelper(dynamoDbClient, tableName);
    }

    // Constructor for testing with dependency injection
    public UpdateLeadFunction(IDynamoDbHelper dynamoDbHelper)
    {
        _dynamoDbHelper = dynamoDbHelper;
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            context.Logger.LogInformation($"UpdateLeadFunction invoked");

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

            // Extract leadId from path parameters
            var leadId = request.PathParameters?.GetValueOrDefault("leadId");
            if (string.IsNullOrWhiteSpace(leadId))
            {
                return CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    "VALIDATION_FAILED",
                    "Lead ID is required"
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

            UpdateLeadRequest? updateRequest;
            try
            {
                updateRequest = JsonSerializer.Deserialize<UpdateLeadRequest>(request.Body);
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
            if (updateRequest == null || string.IsNullOrWhiteSpace(updateRequest.Name))
            {
                return CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    "VALIDATION_FAILED",
                    "Name is required"
                );
            }

            // Verify lead exists and belongs to user
            var existingLead = await _dynamoDbHelper.GetLeadAsync(userId, leadId);
            
            if (existingLead == null)
            {
                context.Logger.LogInformation($"Lead not found: {leadId}");
                return CreateErrorResponse(
                    HttpStatusCode.NotFound,
                    "RESOURCE_NOT_FOUND",
                    "Lead not found"
                );
            }

            if (existingLead.UserId != userId)
            {
                context.Logger.LogWarning($"Authorization failed: User {userId} attempted to update lead {leadId} owned by {existingLead.UserId}");
                return CreateErrorResponse(
                    HttpStatusCode.Forbidden,
                    "AUTH_UNAUTHORIZED",
                    "You are not authorized to update this lead"
                );
            }

            // Update lead fields
            existingLead.Name = updateRequest.Name;
            existingLead.Title = updateRequest.Title;
            existingLead.Company = updateRequest.Company;
            existingLead.Phone = updateRequest.Phone;
            existingLead.Email = updateRequest.Email;
            existingLead.Location = updateRequest.Location;
            existingLead.Notes = updateRequest.Notes;
            existingLead.UpdatedAt = DateTime.UtcNow.ToString("o");

            // Save updated lead
            await _dynamoDbHelper.UpdateLeadAsync(existingLead);
            context.Logger.LogInformation($"Lead updated successfully: {leadId}");

            // Return success response
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Access-Control-Allow-Origin", "*" }
                },
                Body = JsonSerializer.Serialize(new ApiResponse<Lead>
                {
                    Success = true,
                    Data = existingLead
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
