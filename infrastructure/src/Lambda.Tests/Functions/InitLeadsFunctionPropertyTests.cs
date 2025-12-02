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
/// Property-based tests for InitLeadsFunction
/// Feature: mountain-leads-app, Property 2: Default Anthony Pearson lead creation
/// Validates: Requirements 1.2
/// </summary>
public class InitLeadsFunctionPropertyTests
{
    // Generator for valid email addresses (alphanumeric only to avoid JWT encoding issues)
    private static Arbitrary<string> ValidEmail() =>
        (from localPart in Gen.Choose(5, 15)
                .SelectMany(length => Gen.ArrayOf(length, Gen.Elements("abcdefghijklmnopqrstuvwxyz0123456789".ToCharArray())))
                .Select(chars => new string(chars))
                .Where(s => s.Length > 0)
         from domain in Gen.Choose(3, 10)
                .SelectMany(length => Gen.ArrayOf(length, Gen.Elements("abcdefghijklmnopqrstuvwxyz".ToCharArray())))
                .Select(chars => new string(chars))
                .Where(s => s.Length > 0)
         select $"{localPart}@{domain}.com").ToArbitrary();

    // Generator for valid JWT tokens with userId and email
    private static Arbitrary<(string token, string userId, string email)> ValidJwtTokenWithEmail()
    {
        var generator = from userId in Arb.Default.Guid().Generator
                        from email in ValidEmail().Generator
                        select CreateTokenData(userId, email);
        return generator.ToArbitrary();
    }

    private static (string token, string userId, string email) CreateTokenData(Guid userId, string email)
    {
        var userIdStr = userId.ToString();
        // Create a JWT-like token structure for testing
        var header = ToBase64UrlEncode("{\"alg\":\"HS256\",\"typ\":\"JWT\"}");
        var payload = ToBase64UrlEncode($"{{\"sub\":\"{userIdStr}\",\"email\":\"{email}\"}}");
        var signature = ToBase64UrlEncode("signature");
        var token = $"{header}.{payload}.{signature}";
        return (token, userIdStr, email);
    }

    private static string ToBase64UrlEncode(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>
    /// Property 2: Default Anthony Pearson lead creation
    /// For any user registration, the system should create a lead record with exact values:
    /// name="Anthony Pearson", title="CTO", company="Mountain Technologies LLC", 
    /// phone="952-111-1111", email="info@mountaintechnologiesllc.com", 
    /// location="Minneapolis, MN", notes="Likes to code".
    /// </summary>
    [Property(MaxTest = 100)]
    public Property DefaultAnthonyPearsonLeadCreation()
    {
        return Prop.ForAll(
            ValidJwtTokenWithEmail(),
            (tokenData) =>
            {
                var (token, userId, userEmail) = tokenData;

                // Arrange
                var mockDynamoDbHelper = new Mock<IDynamoDbHelper>();
                var capturedLeads = new List<Lead>();

                mockDynamoDbHelper
                    .Setup(x => x.CreateLeadAsync(It.IsAny<Lead>()))
                    .Callback<Lead>(lead => capturedLeads.Add(lead))
                    .ReturnsAsync((Lead lead) => lead);

                var function = new InitLeadsFunction(mockDynamoDbHelper.Object);

                var request = new APIGatewayProxyRequest
                {
                    Headers = new Dictionary<string, string>
                    {
                        { "Authorization", $"Bearer {token}" }
                    }
                };

                var context = new TestLambdaContext();

                // Act - Run async code synchronously for property testing
                var response = function.FunctionHandler(request, context).GetAwaiter().GetResult();

                // Assert
                var isSuccess = response.StatusCode == (int)HttpStatusCode.Created;
                
                // Should have created exactly 2 leads
                var createdTwoLeads = capturedLeads.Count == 2;

                // Find the Anthony Pearson lead
                var anthonyLead = capturedLeads.FirstOrDefault(l => l.Name == "Anthony Pearson");
                var anthonyLeadExists = anthonyLead != null;

                // Verify all Anthony Pearson lead fields match exactly
                var nameMatches = anthonyLead?.Name == "Anthony Pearson";
                var titleMatches = anthonyLead?.Title == "CTO";
                var companyMatches = anthonyLead?.Company == "Mountain Technologies LLC";
                var phoneMatches = anthonyLead?.Phone == "952-111-1111";
                var emailMatches = anthonyLead?.Email == "info@mountaintechnologiesllc.com";
                var locationMatches = anthonyLead?.Location == "Minneapolis, MN";
                var notesMatches = anthonyLead?.Notes == "Likes to code";

                // Verify the lead has proper metadata
                var hasUserId = anthonyLead?.UserId == userId;
                var hasLeadId = anthonyLead != null && !string.IsNullOrEmpty(anthonyLead.LeadId);
                var hasCreatedAt = anthonyLead != null && !string.IsNullOrEmpty(anthonyLead.CreatedAt);
                var hasUpdatedAt = anthonyLead != null && !string.IsNullOrEmpty(anthonyLead.UpdatedAt);

                return (isSuccess && createdTwoLeads && anthonyLeadExists &&
                        nameMatches && titleMatches && companyMatches && phoneMatches &&
                        emailMatches && locationMatches && notesMatches &&
                        hasUserId && hasLeadId && hasCreatedAt && hasUpdatedAt)
                    .Label($"InitLeads should create Anthony Pearson lead with exact values for any user");
            });
    }

    /// <summary>
    /// Property 3: User email lead creation
    /// For any user registration with email address E, the system should create a lead record 
    /// with email=E and all other fields (name, title, company, phone, location, notes) empty or null.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property UserEmailLeadCreation()
    {
        return Prop.ForAll(
            ValidJwtTokenWithEmail(),
            (tokenData) =>
            {
                var (token, userId, userEmail) = tokenData;

                // Arrange
                var mockDynamoDbHelper = new Mock<IDynamoDbHelper>();
                var capturedLeads = new List<Lead>();

                mockDynamoDbHelper
                    .Setup(x => x.CreateLeadAsync(It.IsAny<Lead>()))
                    .Callback<Lead>(lead => capturedLeads.Add(lead))
                    .ReturnsAsync((Lead lead) => lead);

                var function = new InitLeadsFunction(mockDynamoDbHelper.Object);

                var request = new APIGatewayProxyRequest
                {
                    Headers = new Dictionary<string, string>
                    {
                        { "Authorization", $"Bearer {token}" }
                    }
                };

                var context = new TestLambdaContext();

                // Act - Run async code synchronously for property testing
                var response = function.FunctionHandler(request, context).GetAwaiter().GetResult();

                // Assert
                var isSuccess = response.StatusCode == (int)HttpStatusCode.Created;
                
                // Should have created exactly 2 leads
                var createdTwoLeads = capturedLeads.Count == 2;

                // Find the user email lead (the one that's not Anthony Pearson)
                var userEmailLead = capturedLeads.FirstOrDefault(l => l.Name != "Anthony Pearson");
                var userEmailLeadExists = userEmailLead != null;

                // Verify the email matches the user's email from the token
                var emailMatches = userEmailLead?.Email == userEmail;

                // Verify all other fields are empty or null
                var nameIsEmpty = string.IsNullOrEmpty(userEmailLead?.Name);
                var titleIsNull = userEmailLead?.Title == null;
                var companyIsNull = userEmailLead?.Company == null;
                var phoneIsNull = userEmailLead?.Phone == null;
                var locationIsNull = userEmailLead?.Location == null;
                var notesIsNull = userEmailLead?.Notes == null;

                // Verify the lead has proper metadata
                var hasUserId = userEmailLead?.UserId == userId;
                var hasLeadId = userEmailLead != null && !string.IsNullOrEmpty(userEmailLead.LeadId);
                var hasCreatedAt = userEmailLead != null && !string.IsNullOrEmpty(userEmailLead.CreatedAt);
                var hasUpdatedAt = userEmailLead != null && !string.IsNullOrEmpty(userEmailLead.UpdatedAt);

                return (isSuccess && createdTwoLeads && userEmailLeadExists &&
                        emailMatches && nameIsEmpty && titleIsNull && companyIsNull &&
                        phoneIsNull && locationIsNull && notesIsNull &&
                        hasUserId && hasLeadId && hasCreatedAt && hasUpdatedAt)
                    .Label($"InitLeads should create user email lead with email={userEmail} and all other fields empty/null");
            });
    }
}
