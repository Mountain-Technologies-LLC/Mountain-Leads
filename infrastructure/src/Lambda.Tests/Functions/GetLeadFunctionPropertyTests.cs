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
/// Property-based tests for GetLeadFunction
/// Feature: mountain-leads-app, Property 12: Lead data completeness
/// Validates: Requirements 4.2
/// </summary>
public class GetLeadFunctionPropertyTests
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

    /// <summary>
    /// Property 12: Lead data completeness
    /// For any lead retrieved from the API, the response should contain all required fields:
    /// leadId, userId, name, title, company, phone, email, location, notes, createdAt, updatedAt.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property LeadDataCompleteness()
    {
        return Prop.ForAll(
            ValidJwtToken(),
            LeadData(),
            (token, leadData) =>
            {
                // Extract userId from token
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token.Replace("Bearer ", ""));
                var userId = jwt.Claims.First(c => c.Type == "sub").Value;

                // Create a lead with all fields populated
                var leadId = Guid.NewGuid().ToString();
                var now = DateTime.UtcNow.ToString("o");
                var storedLead = new Lead
                {
                    UserId = userId,
                    LeadId = leadId,
                    Name = leadData.Name,
                    Title = leadData.Title,
                    Company = leadData.Company,
                    Phone = leadData.Phone,
                    Email = leadData.Email,
                    Location = leadData.Location,
                    Notes = leadData.Notes,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                // Setup mock to return the lead
                var mockDynamoDbHelper = new Mock<IDynamoDbHelper>();
                mockDynamoDbHelper
                    .Setup(x => x.GetLeadAsync(userId, leadId))
                    .ReturnsAsync(storedLead);

                var function = new GetLeadFunction(mockDynamoDbHelper.Object);

                var request = new APIGatewayProxyRequest
                {
                    Headers = new Dictionary<string, string>
                    {
                        { "Authorization", $"Bearer {token}" }
                    },
                    PathParameters = new Dictionary<string, string>
                    {
                        { "leadId", leadId }
                    }
                };

                var context = new TestLambdaContext();

                // Act
                var response = function.FunctionHandler(request, context).GetAwaiter().GetResult();

                // Assert - Response should be successful
                if (response.StatusCode != (int)HttpStatusCode.OK)
                    return false.ToProperty().Label($"Expected 200 OK, got {response.StatusCode}");

                // Parse the response
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<Lead>>(response.Body);
                var returnedLead = apiResponse?.Data;

                if (returnedLead == null)
                    return false.ToProperty().Label("Response data is null");

                // Verify all required fields are present and not empty/null
                var hasLeadId = !string.IsNullOrEmpty(returnedLead.LeadId);
                var hasUserId = !string.IsNullOrEmpty(returnedLead.UserId);
                var hasName = !string.IsNullOrEmpty(returnedLead.Name);
                var hasCreatedAt = !string.IsNullOrEmpty(returnedLead.CreatedAt);
                var hasUpdatedAt = !string.IsNullOrEmpty(returnedLead.UpdatedAt);

                // Verify optional fields are present (even if null)
                var hasTitleField = true; // Title property exists
                var hasCompanyField = true; // Company property exists
                var hasPhoneField = true; // Phone property exists
                var hasEmailField = true; // Email property exists
                var hasLocationField = true; // Location property exists
                var hasNotesField = true; // Notes property exists

                // Verify field values match what was stored
                var leadIdMatches = returnedLead.LeadId == leadId;
                var userIdMatches = returnedLead.UserId == userId;
                var nameMatches = returnedLead.Name == leadData.Name;
                var titleMatches = returnedLead.Title == leadData.Title;
                var companyMatches = returnedLead.Company == leadData.Company;
                var phoneMatches = returnedLead.Phone == leadData.Phone;
                var emailMatches = returnedLead.Email == leadData.Email;
                var locationMatches = returnedLead.Location == leadData.Location;
                var notesMatches = returnedLead.Notes == leadData.Notes;
                var createdAtMatches = returnedLead.CreatedAt == now;
                var updatedAtMatches = returnedLead.UpdatedAt == now;

                var allRequiredFieldsPresent = hasLeadId && hasUserId && hasName && hasCreatedAt && hasUpdatedAt;
                var allOptionalFieldsPresent = hasTitleField && hasCompanyField && hasPhoneField && 
                                               hasEmailField && hasLocationField && hasNotesField;
                var allFieldValuesMatch = leadIdMatches && userIdMatches && nameMatches && 
                                          titleMatches && companyMatches && phoneMatches && 
                                          emailMatches && locationMatches && notesMatches &&
                                          createdAtMatches && updatedAtMatches;

                return (allRequiredFieldsPresent && allOptionalFieldsPresent && allFieldValuesMatch)
                    .Label($"Lead data completeness: all fields present and values match");
            });
    }
}
