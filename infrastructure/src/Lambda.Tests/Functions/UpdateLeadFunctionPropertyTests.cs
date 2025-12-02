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
/// Property-based tests for UpdateLeadFunction
/// Feature: mountain-leads-app, Property 14: Authorized lead update and consistency
/// Validates: Requirements 5.1, 5.3
/// </summary>
public class UpdateLeadFunctionPropertyTests
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

    // Generator for update test scenario (original lead + updated data)
    private static Arbitrary<UpdateTestScenario> UpdateScenario() =>
        (from token in ValidJwtToken().Generator
         from originalData in LeadData().Generator
         from updatedData in LeadData().Generator
         select new UpdateTestScenario
         {
             Token = token,
             OriginalData = originalData,
             UpdatedData = updatedData
         }).ToArbitrary();

    private class UpdateTestScenario
    {
        public string Token { get; set; } = string.Empty;
        public LeadTestData OriginalData { get; set; } = new();
        public LeadTestData UpdatedData { get; set; } = new();
    }

    /// <summary>
    /// Property 14: Authorized lead update and consistency
    /// For any authenticated user's lead and valid update data, updating the lead should persist 
    /// the changes to DynamoDB, and subsequent retrieval of that lead should return the updated values.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AuthorizedLeadUpdateAndConsistency()
    {
        return Prop.ForAll(
            UpdateScenario(),
            (scenario) =>
            {
                // Extract userId from token
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(scenario.Token.Replace("Bearer ", ""));
                var userId = jwt.Claims.First(c => c.Type == "sub").Value;

                // Create original lead
                var leadId = Guid.NewGuid().ToString();
                var createdAt = DateTime.UtcNow.AddDays(-1).ToString("o");
                var originalLead = new Lead
                {
                    UserId = userId,
                    LeadId = leadId,
                    Name = scenario.OriginalData.Name,
                    Title = scenario.OriginalData.Title,
                    Company = scenario.OriginalData.Company,
                    Phone = scenario.OriginalData.Phone,
                    Email = scenario.OriginalData.Email,
                    Location = scenario.OriginalData.Location,
                    Notes = scenario.OriginalData.Notes,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt
                };

                // Track the updated lead
                Lead? updatedLead = null;

                // Setup mock
                var mockDynamoDbHelper = new Mock<IDynamoDbHelper>();
                
                // Mock GetLeadAsync to return the original lead
                mockDynamoDbHelper
                    .Setup(x => x.GetLeadAsync(userId, leadId))
                    .ReturnsAsync(originalLead);

                // Mock UpdateLeadAsync to capture the updated lead
                mockDynamoDbHelper
                    .Setup(x => x.UpdateLeadAsync(It.IsAny<Lead>()))
                    .Callback<Lead>(lead => updatedLead = lead)
                    .ReturnsAsync((Lead lead) => lead);

                var function = new UpdateLeadFunction(mockDynamoDbHelper.Object);

                // Create update request
                var updateRequest = new UpdateLeadRequest
                {
                    Name = scenario.UpdatedData.Name,
                    Title = scenario.UpdatedData.Title,
                    Company = scenario.UpdatedData.Company,
                    Phone = scenario.UpdatedData.Phone,
                    Email = scenario.UpdatedData.Email,
                    Location = scenario.UpdatedData.Location,
                    Notes = scenario.UpdatedData.Notes
                };

                var request = new APIGatewayProxyRequest
                {
                    Headers = new Dictionary<string, string>
                    {
                        { "Authorization", $"Bearer {scenario.Token}" }
                    },
                    PathParameters = new Dictionary<string, string>
                    {
                        { "leadId", leadId }
                    },
                    Body = JsonSerializer.Serialize(updateRequest)
                };

                var context = new TestLambdaContext();

                // Act - Update the lead
                var response = function.FunctionHandler(request, context).GetAwaiter().GetResult();

                // Assert - Update should succeed
                if (response.StatusCode != (int)HttpStatusCode.OK)
                    return false.ToProperty().Label($"Expected 200 OK, got {response.StatusCode}");

                // Verify the lead was updated in DynamoDB
                if (updatedLead == null)
                    return false.ToProperty().Label("Lead was not updated in DynamoDB");

                // Verify all fields were updated correctly
                var nameUpdated = updatedLead.Name == scenario.UpdatedData.Name;
                var titleUpdated = updatedLead.Title == scenario.UpdatedData.Title;
                var companyUpdated = updatedLead.Company == scenario.UpdatedData.Company;
                var phoneUpdated = updatedLead.Phone == scenario.UpdatedData.Phone;
                var emailUpdated = updatedLead.Email == scenario.UpdatedData.Email;
                var locationUpdated = updatedLead.Location == scenario.UpdatedData.Location;
                var notesUpdated = updatedLead.Notes == scenario.UpdatedData.Notes;

                // Verify immutable fields remain unchanged
                var userIdUnchanged = updatedLead.UserId == userId;
                var leadIdUnchanged = updatedLead.LeadId == leadId;
                var createdAtUnchanged = updatedLead.CreatedAt == createdAt;

                // Verify updatedAt timestamp was updated
                var updatedAtChanged = updatedLead.UpdatedAt != createdAt;

                // Parse the response to verify it returns the updated lead
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<Lead>>(response.Body);
                var returnedLead = apiResponse?.Data;

                if (returnedLead == null)
                    return false.ToProperty().Label("Response data is null");

                // Verify the returned lead matches the updated values
                var returnedNameMatches = returnedLead.Name == scenario.UpdatedData.Name;
                var returnedTitleMatches = returnedLead.Title == scenario.UpdatedData.Title;
                var returnedCompanyMatches = returnedLead.Company == scenario.UpdatedData.Company;
                var returnedPhoneMatches = returnedLead.Phone == scenario.UpdatedData.Phone;
                var returnedEmailMatches = returnedLead.Email == scenario.UpdatedData.Email;
                var returnedLocationMatches = returnedLead.Location == scenario.UpdatedData.Location;
                var returnedNotesMatches = returnedLead.Notes == scenario.UpdatedData.Notes;

                var allFieldsUpdated = nameUpdated && titleUpdated && companyUpdated && 
                                       phoneUpdated && emailUpdated && locationUpdated && notesUpdated;
                var immutableFieldsPreserved = userIdUnchanged && leadIdUnchanged && createdAtUnchanged;
                var timestampUpdated = updatedAtChanged;
                var responseConsistent = returnedNameMatches && returnedTitleMatches && 
                                         returnedCompanyMatches && returnedPhoneMatches && 
                                         returnedEmailMatches && returnedLocationMatches && 
                                         returnedNotesMatches;

                return (allFieldsUpdated && immutableFieldsPreserved && timestampUpdated && responseConsistent)
                    .Label($"Lead update should persist changes and return updated values");
            });
    }
}
