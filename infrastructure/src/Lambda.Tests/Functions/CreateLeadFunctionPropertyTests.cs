using System;
using System.Collections.Generic;
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
/// Property-based tests for CreateLeadFunction
/// Feature: mountain-leads-app, Property 8: Authorized lead creation and storage
/// Validates: Requirements 3.1, 3.3, 3.4
/// </summary>
public class CreateLeadFunctionPropertyTests
{
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

    // Generator for valid JWT tokens with userId
    private static Arbitrary<string> ValidJwtToken() =>
        Arb.Default.Guid().Generator
            .Select(guid =>
            {
                var userId = guid.ToString();
                // Create a simple JWT-like token structure for testing
                // In real scenario, this would be a properly signed JWT
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
    /// Property 8: Authorized lead creation and storage
    /// For any authenticated user and valid lead data, creating a lead should store it in DynamoDB 
    /// with a unique leadId, the user's userId, and all provided field values, and the lead should 
    /// be retrievable in subsequent queries.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AuthorizedLeadCreationAndStorage()
    {
        return Prop.ForAll(
            ValidJwtToken(),
            LeadData(),
            (token, leadData) =>
            {
                // Arrange
                var mockDynamoDbHelper = new Mock<IDynamoDbHelper>();
                Lead? capturedLead = null;

                mockDynamoDbHelper
                    .Setup(x => x.CreateLeadAsync(It.IsAny<Lead>()))
                    .Callback<Lead>(lead => capturedLead = lead)
                    .ReturnsAsync((Lead lead) => lead);

                var function = new CreateLeadFunction(mockDynamoDbHelper.Object);

                var createRequest = new CreateLeadRequest
                {
                    Name = leadData.Name,
                    Title = leadData.Title,
                    Company = leadData.Company,
                    Phone = leadData.Phone,
                    Email = leadData.Email,
                    Location = leadData.Location,
                    Notes = leadData.Notes
                };

                var request = new APIGatewayProxyRequest
                {
                    Headers = new Dictionary<string, string>
                    {
                        { "Authorization", $"Bearer {token}" }
                    },
                    Body = JsonSerializer.Serialize(createRequest)
                };

                var context = new TestLambdaContext();

                // Act - Run async code synchronously for property testing
                var response = function.FunctionHandler(request, context).GetAwaiter().GetResult();

                // Assert
                var isSuccess = response.StatusCode == (int)HttpStatusCode.Created;
                var leadWasStored = capturedLead != null;
                var hasUniqueLeadId = capturedLead != null && !string.IsNullOrEmpty(capturedLead.LeadId);
                var hasUserId = capturedLead != null && !string.IsNullOrEmpty(capturedLead.UserId);
                var nameMatches = capturedLead != null && capturedLead.Name == leadData.Name;
                var titleMatches = capturedLead != null && capturedLead.Title == leadData.Title;
                var companyMatches = capturedLead != null && capturedLead.Company == leadData.Company;
                var phoneMatches = capturedLead != null && capturedLead.Phone == leadData.Phone;
                var emailMatches = capturedLead != null && capturedLead.Email == leadData.Email;
                var locationMatches = capturedLead != null && capturedLead.Location == leadData.Location;
                var notesMatches = capturedLead != null && capturedLead.Notes == leadData.Notes;
                var hasCreatedAt = capturedLead != null && !string.IsNullOrEmpty(capturedLead.CreatedAt);
                var hasUpdatedAt = capturedLead != null && !string.IsNullOrEmpty(capturedLead.UpdatedAt);

                // Verify the lead can be retrieved (simulated by checking it was stored)
                var isRetrievable = leadWasStored;

                return (isSuccess && leadWasStored && hasUniqueLeadId && hasUserId && 
                        nameMatches && titleMatches && companyMatches && phoneMatches && 
                        emailMatches && locationMatches && notesMatches && 
                        hasCreatedAt && hasUpdatedAt && isRetrievable)
                    .Label($"Lead creation should succeed and store all fields correctly");
            });
    }
}
