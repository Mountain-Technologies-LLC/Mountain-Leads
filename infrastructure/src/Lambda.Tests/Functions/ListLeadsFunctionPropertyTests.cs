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
/// Property-based tests for ListLeadsFunction
/// Feature: mountain-leads-app, Property 11: User data isolation
/// Validates: Requirements 4.1, 9.1, 9.2, 9.4, 9.5
/// </summary>
public class ListLeadsFunctionPropertyTests
{
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

    private static Lead CreateLead(string userId, string name)
    {
        var now = DateTime.UtcNow.ToString("o");
        return new Lead
        {
            UserId = userId,
            LeadId = Guid.NewGuid().ToString(),
            Name = name,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private class TwoUserTestData
    {
        public string Token1 { get; set; } = string.Empty;
        public string Token2 { get; set; } = string.Empty;
        public int Count1 { get; set; }
        public int Count2 { get; set; }
    }

    private static Arbitrary<TwoUserTestData> TwoUserData() =>
        (from token1 in ValidJwtToken().Generator
         from token2 in ValidJwtToken().Generator
         from count1 in Arb.Default.PositiveInt().Generator
         from count2 in Arb.Default.PositiveInt().Generator
         select new TwoUserTestData
         {
             Token1 = token1,
             Token2 = token2,
             Count1 = (count1.Get % 10) + 1,
             Count2 = (count2.Get % 10) + 1
         }).ToArbitrary();

    /// <summary>
    /// Property 11: User data isolation
    /// For any two distinct users U1 and U2, where U1 creates leads L1 and U2 creates leads L2,
    /// querying leads as U1 should return only L1, and querying as U2 should return only L2, with no overlap.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property UserDataIsolation()
    {
        return Prop.ForAll(
            TwoUserData(),
            (testData) =>
            {
                // Ensure we have two different users
                if (testData.Token1 == testData.Token2)
                    return true.ToProperty().Label("Skipped: same user tokens");

                // Extract user IDs from tokens
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwt1 = handler.ReadJwtToken(testData.Token1.Replace("Bearer ", ""));
                var jwt2 = handler.ReadJwtToken(testData.Token2.Replace("Bearer ", ""));
                var userId1 = jwt1.Claims.First(c => c.Type == "sub").Value;
                var userId2 = jwt2.Claims.First(c => c.Type == "sub").Value;

                // Create leads for user 1
                var leadsForUser1 = Enumerable.Range(0, testData.Count1)
                    .Select(i => CreateLead(userId1, $"User1Lead{i}"))
                    .ToList();

                // Create leads for user 2
                var leadsForUser2 = Enumerable.Range(0, testData.Count2)
                    .Select(i => CreateLead(userId2, $"User2Lead{i}"))
                    .ToList();

                // Setup mock for user 1
                var mockHelper1 = new Mock<IDynamoDbHelper>();
                mockHelper1
                    .Setup(x => x.QueryLeadsByUserIdAsync(userId1))
                    .ReturnsAsync(leadsForUser1);

                var function1 = new ListLeadsFunction(mockHelper1.Object);
                var request1 = new APIGatewayProxyRequest
                {
                    Headers = new Dictionary<string, string>
                    {
                        { "Authorization", $"Bearer {testData.Token1}" }
                    }
                };
                var context1 = new TestLambdaContext();
                var response1 = function1.FunctionHandler(request1, context1).GetAwaiter().GetResult();

                // Setup mock for user 2
                var mockHelper2 = new Mock<IDynamoDbHelper>();
                mockHelper2
                    .Setup(x => x.QueryLeadsByUserIdAsync(userId2))
                    .ReturnsAsync(leadsForUser2);

                var function2 = new ListLeadsFunction(mockHelper2.Object);
                var request2 = new APIGatewayProxyRequest
                {
                    Headers = new Dictionary<string, string>
                    {
                        { "Authorization", $"Bearer {testData.Token2}" }
                    }
                };
                var context2 = new TestLambdaContext();
                var response2 = function2.FunctionHandler(request2, context2).GetAwaiter().GetResult();

                // Parse responses
                var apiResponse1 = JsonSerializer.Deserialize<ApiResponse<ListLeadsResponse>>(response1.Body);
                var apiResponse2 = JsonSerializer.Deserialize<ApiResponse<ListLeadsResponse>>(response2.Body);

                // Verify isolation
                var user1LeadsReturned = apiResponse1?.Data?.Leads ?? new List<Lead>();
                var user2LeadsReturned = apiResponse2?.Data?.Leads ?? new List<Lead>();

                var user1OnlyGetsUser1Leads = user1LeadsReturned.All(l => l.UserId == userId1);
                var user2OnlyGetsUser2Leads = user2LeadsReturned.All(l => l.UserId == userId2);
                var noOverlap = !user1LeadsReturned.Any(l1 => user2LeadsReturned.Any(l2 => l2.LeadId == l1.LeadId));
                var correctCounts = user1LeadsReturned.Count == testData.Count1 && user2LeadsReturned.Count == testData.Count2;

                return (user1OnlyGetsUser1Leads && user2OnlyGetsUser2Leads && noOverlap && correctCounts)
                    .Label($"User data isolation: U1 has {testData.Count1} leads, U2 has {testData.Count2} leads, no overlap");
            });
    }
}
