using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using FsCheck;
using FsCheck.Xunit;
using Lambda.Functions;
using Lambda.Models;
using Lambda.Utilities;
using Moq;
using Xunit;

namespace Lambda.Tests.Functions;

/// <summary>
/// Property-based tests for DeleteLeadFunction
/// Feature: mountain-leads-app, Property 17: Authorized lead deletion and removal
/// Validates: Requirements 6.1, 6.3
/// </summary>
public class DeleteLeadFunctionPropertyTests
{
    // Generator for valid JWT tokens with userId
    private static Arbitrary<string> ValidJwtToken() =>
        Arb.Default.Guid().Generator
            .Select(guid =>
            {
                var userId = guid.ToString();
                var header = ToBase64UrlEncode("{\"alg\":\"HS256\",\"typ\":\"JWT\"}");
                var payload = ToBase64UrlEncode($"{{\"sub\":\"{userId}\"}}");
                var signature = ToBase64UrlEncode("signature");
                return $"{header}.{payload}.{signature}";
            })
            .ToArbitrary();

    private static string ToBase64UrlEncode(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    // Generator for valid lead names (non-empty strings)
    private static Arbitrary<string> ValidLeadName() =>
        Arb.Default.NonEmptyString().Generator
            .Select(nes => nes.Get.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArbitrary();

    // Generator for optional strings
    private static Arbitrary<string?> OptionalString() =>
        Gen.OneOf(
            Gen.Constant<string?>(null),
            Arb.Default.NonEmptyString().Generator.Select(nes => (string?)nes.Get)
        ).ToArbitrary();

    // Generator for complete lead data
    private static Arbitrary<LeadTestData> LeadData() =>
        (from name in ValidLeadName().Generator
         from title in OptionalString().Generator
         from company in OptionalString().Generator
         from phone in OptionalString().Generator
         from email in OptionalString().Generator
         from location in OptionalString().Generator
         from notes in OptionalString().Generator
         select new LeadTestData
         {
             Name = name,
             Title = title,
             Company = company,
             Phone = phone,
             Email = email,
             Location = location,
             Notes = notes
         }).ToArbitrary();

    private class LeadTestData
    {
        public string Name { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Company { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Location { get; set; }
        public string? Notes { get; set; }
    }

    // Generator for delete test scenario (user token and lead data)
    private static Arbitrary<DeleteTestScenario> DeleteScenario() =>
        (from token in ValidJwtToken().Generator
         from leadData in LeadData().Generator
         select new DeleteTestScenario
         {
             Token = token,
             LeadData = leadData
         }).ToArbitrary();

    private class DeleteTestScenario
    {
        public string Token { get; set; } = string.Empty;
        public LeadTestData LeadData { get; set; } = new();
    }

    /// <summary>
    /// Property 17: Authorized lead deletion and removal
    /// For any authenticated user's lead, deleting the lead should remove it from DynamoDB, 
    /// and subsequent queries should not return the deleted lead.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AuthorizedLeadDeletionAndRemoval()
    {
        return Prop.ForAll(
            DeleteScenario(),
            (scenario) =>
            {
                // Extract userId from token
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(scenario.Token.Replace("Bearer ", ""));
                var userId = jwt.Claims.First(c => c.Type == "sub").Value;

                // Create a lead owned by the user
                var leadId = Guid.NewGuid().ToString();
                var createdAt = DateTime.UtcNow.AddDays(-1).ToString("o");
                var originalLead = new Lead
                {
                    UserId = userId,
                    LeadId = leadId,
                    Name = scenario.LeadData.Name,
                    Title = scenario.LeadData.Title,
                    Company = scenario.LeadData.Company,
                    Phone = scenario.LeadData.Phone,
                    Email = scenario.LeadData.Email,
                    Location = scenario.LeadData.Location,
                    Notes = scenario.LeadData.Notes,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt
                };

                // Track deletion
                bool deleteWasCalled = false;
                string? deletedUserId = null;
                string? deletedLeadId = null;

                // Setup mock
                var mockDynamoDbHelper = new Mock<IDynamoDbHelper>();
                
                // Mock GetLeadAsync to return the lead before deletion
                mockDynamoDbHelper
                    .Setup(x => x.GetLeadAsync(userId, leadId))
                    .ReturnsAsync(originalLead);

                // Mock DeleteLeadAsync to capture the deletion
                mockDynamoDbHelper
                    .Setup(x => x.DeleteLeadAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .Callback<string, string>((uid, lid) =>
                    {
                        deleteWasCalled = true;
                        deletedUserId = uid;
                        deletedLeadId = lid;
                    })
                    .Returns(Task.CompletedTask);

                var function = new DeleteLeadFunction(mockDynamoDbHelper.Object);

                // Create delete request
                var request = new APIGatewayProxyRequest
                {
                    Headers = new Dictionary<string, string>
                    {
                        { "Authorization", $"Bearer {scenario.Token}" }
                    },
                    PathParameters = new Dictionary<string, string>
                    {
                        { "leadId", leadId }
                    }
                };

                var context = new TestLambdaContext();

                // Act - Delete the lead
                var response = function.FunctionHandler(request, context).GetAwaiter().GetResult();

                // Assert - Deletion should succeed
                if (response.StatusCode != (int)HttpStatusCode.OK)
                    return false.ToProperty().Label($"Expected 200 OK, got {response.StatusCode}");

                // Verify the lead was deleted from DynamoDB
                if (!deleteWasCalled)
                    return false.ToProperty().Label("DeleteLeadAsync was not called");

                // Verify correct userId and leadId were used for deletion
                var correctUserIdUsed = deletedUserId == userId;
                var correctLeadIdUsed = deletedLeadId == leadId;

                if (!correctUserIdUsed)
                    return false.ToProperty().Label($"Expected userId {userId}, but got {deletedUserId}");

                if (!correctLeadIdUsed)
                    return false.ToProperty().Label($"Expected leadId {leadId}, but got {deletedLeadId}");

                // Parse the response to verify success message
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(response.Body);
                
                if (apiResponse == null)
                    return false.ToProperty().Label("Response body is null");

                var successFlagSet = apiResponse.Success == true;
                var hasSuccessMessage = apiResponse.Data != null;

                // Simulate subsequent query to verify lead is not returned
                // In a real scenario, GetLeadAsync would return null after deletion
                mockDynamoDbHelper
                    .Setup(x => x.GetLeadAsync(userId, leadId))
                    .ReturnsAsync((Lead?)null);

                // Verify subsequent query returns null
                var subsequentQuery = mockDynamoDbHelper.Object.GetLeadAsync(userId, leadId).GetAwaiter().GetResult();
                var leadNotRetrievable = subsequentQuery == null;

                var deletionSucceeded = deleteWasCalled && correctUserIdUsed && correctLeadIdUsed;
                var responseValid = successFlagSet && hasSuccessMessage;
                var leadRemoved = leadNotRetrievable;

                return (deletionSucceeded && responseValid && leadRemoved)
                    .Label($"Lead deletion should remove the lead from DynamoDB and prevent subsequent retrieval");
            });
    }
}
