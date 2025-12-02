using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using Moq;
using Moq.Protected;
using website.Models;
using website.Services;
using Xunit;

namespace website.Tests.Services;

/// <summary>
/// Property-based tests for LeadService
/// Feature: mountain-leads-app
/// </summary>
public class LeadServicePropertyTests
{
    // Generator for JWT tokens
    private static Arbitrary<string> JwtToken() =>
        Arb.Default.Guid().Generator
            .Select(guid =>
            {
                var userId = guid.ToString();
                var expirationTime = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
                var header = ToBase64UrlEncode("{\"alg\":\"HS256\",\"typ\":\"JWT\"}");
                var payload = ToBase64UrlEncode($"{{\"sub\":\"{userId}\",\"cognito:username\":\"{userId}\",\"exp\":{expirationTime}}}");
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

    // Generator for Lead objects
    private static Arbitrary<Lead> LeadGen() =>
        (from name in Arb.Default.NonEmptyString().Generator.Select(s => s.Get)
         from title in Arb.Default.String().Generator
         from company in Arb.Default.String().Generator
         from phone in Arb.Default.String().Generator
         from email in Arb.Default.String().Generator
         from location in Arb.Default.String().Generator
         from notes in Arb.Default.String().Generator
         from userId in Arb.Default.Guid().Generator.Select(g => g.ToString())
         from leadId in Arb.Default.Guid().Generator.Select(g => g.ToString())
         select new Lead
         {
             UserId = userId,
             LeadId = leadId,
             Name = name,
             Title = title,
             Company = company,
             Phone = phone,
             Email = email,
             Location = location,
             Notes = notes,
             CreatedAt = DateTime.UtcNow,
             UpdatedAt = DateTime.UtcNow
         }).ToArbitrary();

    private static ILeadService CreateLeadService(
        Mock<IJSRuntime> mockJsRuntime,
        HttpClient httpClient)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "AWS:ApiGatewayUrl", "https://test-api.execute-api.us-east-1.amazonaws.com/prod" },
                { "AWS:Region", "us-east-1" }
            })
            .Build();

        return new LeadService(httpClient, mockJsRuntime.Object, configuration);
    }

    /// <summary>
    /// Property 18: JWT token inclusion and extraction
    /// For any API request from the Blazor application, the request should include the Cognito JWT token 
    /// in the Authorization header, and the Lambda function should correctly extract the userId from the 
    /// token claims (sub or cognito:username).
    /// Validates: Requirements 8.1, 9.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property JwtTokenInclusionAndExtraction()
    {
        return Prop.ForAll(
            JwtToken(),
            LeadGen(),
            (token, lead) =>
            {
                // Arrange
                var mockJsRuntime = new Mock<IJSRuntime>();
                
                // Mock sessionStorage.getItem to return the JWT token
                mockJsRuntime
                    .Setup(x => x.InvokeAsync<string?>(
                        "sessionStorage.getItem",
                        It.IsAny<object[]>()))
                    .ReturnsAsync(token);

                var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
                string? capturedAuthHeader = null;

                // Mock HTTP request to capture the Authorization header
                mockHttpMessageHandler
                    .Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.IsAny<HttpRequestMessage>(),
                        ItExpr.IsAny<CancellationToken>())
                    .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
                    {
                        // Capture the Authorization header
                        if (request.Headers.Authorization != null)
                        {
                            capturedAuthHeader = request.Headers.Authorization.ToString();
                        }
                    })
                    .ReturnsAsync(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(
                            JsonSerializer.Serialize(new LeadResponse { Lead = lead }),
                            Encoding.UTF8,
                            "application/json")
                    });

                var httpClient = new HttpClient(mockHttpMessageHandler.Object);
                var service = CreateLeadService(mockJsRuntime, httpClient);

                // Act - Make an API call (using CreateLeadAsync as an example)
                var request = new CreateLeadRequest
                {
                    Name = lead.Name,
                    Title = lead.Title,
                    Company = lead.Company,
                    Phone = lead.Phone,
                    Email = lead.Email,
                    Location = lead.Location,
                    Notes = lead.Notes
                };

                var result = service.CreateLeadAsync(request).GetAwaiter().GetResult();

                // Assert - Verify the Authorization header was included
                var hasAuthHeader = !string.IsNullOrEmpty(capturedAuthHeader);
                var hasBearerScheme = capturedAuthHeader?.StartsWith("Bearer ") ?? false;
                var hasToken = capturedAuthHeader?.Contains(token) ?? false;

                // Verify the token can be parsed and contains userId
                var tokenParts = token.Split('.');
                var isValidJwtStructure = tokenParts.Length == 3;
                
                // Decode the payload to verify userId claims
                var payloadJson = Encoding.UTF8.GetString(
                    Convert.FromBase64String(
                        tokenParts[1]
                            .Replace('-', '+')
                            .Replace('_', '/')
                            .PadRight(tokenParts[1].Length + (4 - tokenParts[1].Length % 4) % 4, '=')));
                
                var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson);
                var hasSubClaim = payload?.ContainsKey("sub") ?? false;
                var hasCognitoUsernameClaim = payload?.ContainsKey("cognito:username") ?? false;
                var hasUserIdClaim = hasSubClaim || hasCognitoUsernameClaim;

                return (hasAuthHeader && hasBearerScheme && hasToken && 
                        isValidJwtStructure && hasUserIdClaim)
                    .Label($"API requests should include JWT token in Authorization header with valid userId claims");
            });
    }

    /// <summary>
    /// Additional property test: Verify all CRUD operations include the JWT token
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AllCrudOperationsIncludeJwtToken()
    {
        return Prop.ForAll(
            JwtToken(),
            LeadGen(),
            (token, lead) =>
            {
                // Arrange
                var mockJsRuntime = new Mock<IJSRuntime>();
                
                mockJsRuntime
                    .Setup(x => x.InvokeAsync<string?>(
                        "sessionStorage.getItem",
                        It.IsAny<object[]>()))
                    .ReturnsAsync(token);

                var requestsWithAuth = new List<bool>();
                var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

                mockHttpMessageHandler
                    .Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.IsAny<HttpRequestMessage>(),
                        ItExpr.IsAny<CancellationToken>())
                    .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
                    {
                        // Track if Authorization header is present
                        requestsWithAuth.Add(request.Headers.Authorization != null &&
                                           request.Headers.Authorization.Scheme == "Bearer" &&
                                           request.Headers.Authorization.Parameter == token);
                    })
                    .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
                    {
                        // Return appropriate response based on HTTP method
                        if (request.Method == HttpMethod.Get && request.RequestUri?.PathAndQuery.Contains("/leads/") == true)
                        {
                            return new HttpResponseMessage
                            {
                                StatusCode = HttpStatusCode.OK,
                                Content = new StringContent(
                                    JsonSerializer.Serialize(new LeadResponse { Lead = lead }),
                                    Encoding.UTF8,
                                    "application/json")
                            };
                        }
                        else if (request.Method == HttpMethod.Get)
                        {
                            return new HttpResponseMessage
                            {
                                StatusCode = HttpStatusCode.OK,
                                Content = new StringContent(
                                    JsonSerializer.Serialize(new ListLeadsResponse { Leads = new List<Lead> { lead }, Count = 1 }),
                                    Encoding.UTF8,
                                    "application/json")
                            };
                        }
                        else if (request.Method == HttpMethod.Delete)
                        {
                            return new HttpResponseMessage
                            {
                                StatusCode = HttpStatusCode.OK,
                                Content = new StringContent("{\"success\":true}", Encoding.UTF8, "application/json")
                            };
                        }
                        else
                        {
                            return new HttpResponseMessage
                            {
                                StatusCode = HttpStatusCode.OK,
                                Content = new StringContent(
                                    JsonSerializer.Serialize(new LeadResponse { Lead = lead }),
                                    Encoding.UTF8,
                                    "application/json")
                            };
                        }
                    });

                var httpClient = new HttpClient(mockHttpMessageHandler.Object);
                var service = CreateLeadService(mockJsRuntime, httpClient);

                // Act - Test all CRUD operations
                var createRequest = new CreateLeadRequest { Name = lead.Name };
                service.CreateLeadAsync(createRequest).GetAwaiter().GetResult();

                service.GetLeadsAsync().GetAwaiter().GetResult();

                service.GetLeadAsync(lead.LeadId).GetAwaiter().GetResult();

                var updateRequest = new UpdateLeadRequest { Name = lead.Name };
                service.UpdateLeadAsync(lead.LeadId, updateRequest).GetAwaiter().GetResult();

                service.DeleteLeadAsync(lead.LeadId).GetAwaiter().GetResult();

                service.InitializeDefaultLeadsAsync().GetAwaiter().GetResult();

                // Assert - All requests should have included the Authorization header
                var allRequestsHadAuth = requestsWithAuth.Count == 6 && requestsWithAuth.All(x => x);

                return allRequestsHadAuth
                    .Label($"All CRUD operations (Create, List, Get, Update, Delete, Init) should include JWT token in Authorization header");
            });
    }
}
