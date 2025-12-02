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
/// Property-based tests for cross-user modification prevention
/// Feature: mountain-leads-app, Property 15: Cross-user modification prevention
/// Validates: Requirements 5.2, 5.5, 6.2, 6.4
/// </summary>
public class CrossUserModificationPropertyTests
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

    // Generator for cross-user test scenario (two different users and lead data)
    private static Arbitrary<CrossUserTestScenario> CrossUserScenario() =>
        (from user1Token in ValidJwtToken().Generator
         from user2Token in ValidJwtToken().Generator
         from leadData in LeadData().Generator
         from updateData in LeadData().Generator
         where user1Token != user2Token // Ensure different users
         select new CrossUserTestScenario
         {
             User1Token = user1Token,
             User2Token = user2Token,
             LeadData = leadData,
             UpdateData = updateData
         }).ToArbitrary();

    private class CrossUserTestScenario
    {
        public string User1Token { get; set; } = string.Empty;
        public string User2Token { get; set; } = string.Empty;
        public LeadTestData LeadData { get; set; } = new();
        public LeadTestData UpdateData { get; set; } = new();
    }

    /// <summary>
    /// Property 15: Cross-user modification prevention
    /// For any two distinct users U1 and U2, where U1 creates a lead L, any attempt by U2 to 
    /// update or delete L should be rejected with an authorization error, and L should remain unchanged.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property CrossUserModificationPrevention()
    {
        return Prop.ForAll(
            CrossUserScenario(),
            (scenario) =>
            {
                // Extract userIds from tokens
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwt1 = handler.ReadJwtToken(scenario.User1Token.Replace("Bearer ", ""));
                var user1Id = jwt1.Claims.First(c => c.Type == "sub").Value;
                
                var jwt2 = handler.ReadJwtToken(scenario.User2Token.Replace("Bearer ", ""));
                var user2Id = jwt2.Claims.First(c => c.Type == "sub").Value;

                // Ensure we have two different users
                if (user1Id == user2Id)
                    return true.ToProperty().Label("Skipping: same user generated");

                // Create a lead owned by User1
                var leadId = Guid.NewGuid().ToString();
                var createdAt = DateTime.UtcNow.AddDays(-1).ToString("o");
                var originalLead = new Lead
                {
                    UserId = user1Id,
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

                // Track if the lead was modified
                bool leadWasModified = false;
                Lead? capturedLead = null;

                // Setup mock for update test
                var mockDynamoDbHelperUpdate = new Mock<IDynamoDbHelper>();
                
                // Mock GetLeadAsync to return the original lead (owned by User1)
                mockDynamoDbHelperUpdate
                    .Setup(x => x.GetLeadAsync(It.IsAny<string>(), leadId))
                    .ReturnsAsync((string userId, string lid) =>
                    {
                        // Return the lead only if the correct userId is queried
                        return userId == user1Id ? originalLead : null;
                    });

                // Mock UpdateLeadAsync to capture any update attempts
                mockDynamoDbHelperUpdate
                    .Setup(x => x.UpdateLeadAsync(It.IsAny<Lead>()))
                    .Callback<Lead>(lead =>
                    {
                        leadWasModified = true;
                        capturedLead = lead;
                    })
                    .ReturnsAsync((Lead lead) => lead);

                var updateFunction = new UpdateLeadFunction(mockDynamoDbHelperUpdate.Object);

                // Create update request from User2
                var updateRequest = new UpdateLeadRequest
                {
                    Name = scenario.UpdateData.Name,
                    Title = scenario.UpdateData.Title,
                    Company = scenario.UpdateData.Company,
                    Phone = scenario.UpdateData.Phone,
                    Email = scenario.UpdateData.Email,
                    Location = scenario.UpdateData.Location,
                    Notes = scenario.UpdateData.Notes
                };

                var updateApiRequest = new APIGatewayProxyRequest
                {
                    Headers = new Dictionary<string, string>
                    {
                        { "Authorization", $"Bearer {scenario.User2Token}" }
                    },
                    PathParameters = new Dictionary<string, string>
                    {
                        { "leadId", leadId }
                    },
                    Body = JsonSerializer.Serialize(updateRequest)
                };

                var context = new TestLambdaContext();

                // Act - User2 attempts to update User1's lead
                var updateResponse = updateFunction.FunctionHandler(updateApiRequest, context).GetAwaiter().GetResult();

                // Assert - Update should be rejected
                var updateRejected = updateResponse.StatusCode == (int)HttpStatusCode.NotFound || 
                                     updateResponse.StatusCode == (int)HttpStatusCode.Forbidden;
                var leadNotModified = !leadWasModified;

                // Verify error response contains appropriate error code
                bool hasAuthError = false;
                if (updateResponse.StatusCode != (int)HttpStatusCode.OK)
                {
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ApiResponse<object>>(updateResponse.Body);
                        hasAuthError = errorResponse?.Error?.Code == "AUTH_UNAUTHORIZED" || 
                                       errorResponse?.Error?.Code == "RESOURCE_NOT_FOUND";
                    }
                    catch
                    {
                        hasAuthError = false;
                    }
                }

                // Test delete operation
                bool deleteWasExecuted = false;

                // Setup mock for delete test
                var mockDynamoDbHelperDelete = new Mock<IDynamoDbHelper>();
                
                // Mock GetLeadAsync to return the original lead (owned by User1)
                mockDynamoDbHelperDelete
                    .Setup(x => x.GetLeadAsync(It.IsAny<string>(), leadId))
                    .ReturnsAsync((string userId, string lid) =>
                    {
                        // Return the lead only if the correct userId is queried
                        return userId == user1Id ? originalLead : null;
                    });

                // Mock DeleteLeadAsync to capture any delete attempts
                mockDynamoDbHelperDelete
                    .Setup(x => x.DeleteLeadAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .Callback(() => deleteWasExecuted = true)
                    .Returns(Task.CompletedTask);

                var deleteFunction = new DeleteLeadFunction(mockDynamoDbHelperDelete.Object);

                var deleteApiRequest = new APIGatewayProxyRequest
                {
                    Headers = new Dictionary<string, string>
                    {
                        { "Authorization", $"Bearer {scenario.User2Token}" }
                    },
                    PathParameters = new Dictionary<string, string>
                    {
                        { "leadId", leadId }
                    }
                };

                // Act - User2 attempts to delete User1's lead
                var deleteResponse = deleteFunction.FunctionHandler(deleteApiRequest, context).GetAwaiter().GetResult();

                // Assert - Delete should be rejected
                var deleteRejected = deleteResponse.StatusCode == (int)HttpStatusCode.NotFound || 
                                     deleteResponse.StatusCode == (int)HttpStatusCode.Forbidden;
                var leadNotDeleted = !deleteWasExecuted;

                // Verify error response contains appropriate error code
                bool hasDeleteAuthError = false;
                if (deleteResponse.StatusCode != (int)HttpStatusCode.OK)
                {
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ApiResponse<object>>(deleteResponse.Body);
                        hasDeleteAuthError = errorResponse?.Error?.Code == "AUTH_UNAUTHORIZED" || 
                                             errorResponse?.Error?.Code == "RESOURCE_NOT_FOUND";
                    }
                    catch
                    {
                        hasDeleteAuthError = false;
                    }
                }

                var updatePreventionWorks = updateRejected && leadNotModified && hasAuthError;
                var deletePreventionWorks = deleteRejected && leadNotDeleted && hasDeleteAuthError;

                return (updatePreventionWorks && deletePreventionWorks)
                    .Label($"Cross-user modification should be prevented for both update and delete operations");
            });
    }
}
