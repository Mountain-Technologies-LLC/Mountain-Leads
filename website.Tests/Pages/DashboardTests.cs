using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using Moq;
using Moq.Protected;
using website.Models;
using website.Services;
using Xunit;

namespace website.Tests.Pages;

/// <summary>
/// Unit tests for Dashboard component logic
/// Feature: mountain-leads-app
/// Validates: Requirements 4.2, 4.3
/// </summary>
public class DashboardTests
{
    private static string CreateJwtToken()
    {
        var userId = Guid.NewGuid().ToString();
        var expirationTime = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var header = ToBase64UrlEncode("{\"alg\":\"HS256\",\"typ\":\"JWT\"}");
        var payload = ToBase64UrlEncode($"{{\"sub\":\"{userId}\",\"exp\":{expirationTime}}}");
        var signature = ToBase64UrlEncode("signature");
        return $"{header}.{payload}.{signature}";
    }

    private static string ToBase64UrlEncode(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

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
    /// Test rendering with leads - verifies that GetLeadsAsync returns leads successfully
    /// Validates: Requirements 4.2
    /// </summary>
    [Fact]
    public async Task Dashboard_LoadLeads_Returns_Leads_Successfully()
    {
        // Arrange
        var mockJsRuntime = new Mock<IJSRuntime>();
        var token = CreateJwtToken();
        
        mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>(
                "sessionStorage.getItem",
                It.IsAny<object[]>()))
            .ReturnsAsync(token);

        var testLeads = new List<Lead>
        {
            new Lead
            {
                LeadId = Guid.NewGuid().ToString(),
                UserId = "test-user",
                Name = "John Doe",
                Title = "CEO",
                Company = "Test Corp",
                Phone = "555-1234",
                Email = "john@test.com",
                Location = "New York",
                Notes = "Important client",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Lead
            {
                LeadId = Guid.NewGuid().ToString(),
                UserId = "test-user",
                Name = "Jane Smith",
                Title = "CTO",
                Company = "Tech Inc",
                Phone = "555-5678",
                Email = "jane@tech.com",
                Location = "San Francisco",
                Notes = "Tech lead",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    JsonSerializer.Serialize(new ListLeadsResponse { Leads = testLeads, Count = testLeads.Count }),
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = CreateLeadService(mockJsRuntime, httpClient);

        // Act
        var result = await service.GetLeadsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("John Doe", result[0].Name);
        Assert.Equal("Jane Smith", result[1].Name);
    }

    /// <summary>
    /// Test rendering empty state - verifies that GetLeadsAsync returns empty list when no leads exist
    /// Validates: Requirements 4.3
    /// </summary>
    [Fact]
    public async Task Dashboard_LoadLeads_Returns_EmptyList_When_No_Leads()
    {
        // Arrange
        var mockJsRuntime = new Mock<IJSRuntime>();
        var token = CreateJwtToken();
        
        mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>(
                "sessionStorage.getItem",
                It.IsAny<object[]>()))
            .ReturnsAsync(token);

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    JsonSerializer.Serialize(new ListLeadsResponse { Leads = new List<Lead>(), Count = 0 }),
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = CreateLeadService(mockJsRuntime, httpClient);

        // Act
        var result = await service.GetLeadsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// Test create lead form submission - verifies that CreateLeadAsync creates a lead successfully
    /// Validates: Requirements 4.2
    /// </summary>
    [Fact]
    public async Task Dashboard_CreateLead_Succeeds_With_Valid_Data()
    {
        // Arrange
        var mockJsRuntime = new Mock<IJSRuntime>();
        var token = CreateJwtToken();
        
        mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>(
                "sessionStorage.getItem",
                It.IsAny<object[]>()))
            .ReturnsAsync(token);

        var createdLead = new Lead
        {
            LeadId = Guid.NewGuid().ToString(),
            UserId = "test-user",
            Name = "New Lead",
            Title = "Manager",
            Company = "New Corp",
            Phone = "555-9999",
            Email = "new@corp.com",
            Location = "Boston",
            Notes = "New contact",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    JsonSerializer.Serialize(new LeadResponse { Lead = createdLead }),
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = CreateLeadService(mockJsRuntime, httpClient);

        var request = new CreateLeadRequest
        {
            Name = "New Lead",
            Title = "Manager",
            Company = "New Corp",
            Phone = "555-9999",
            Email = "new@corp.com",
            Location = "Boston",
            Notes = "New contact"
        };

        // Act
        var result = await service.CreateLeadAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Lead", result.Name);
        Assert.Equal("Manager", result.Title);
        Assert.Equal("New Corp", result.Company);
    }

    /// <summary>
    /// Test update lead form submission - verifies that UpdateLeadAsync updates a lead successfully
    /// Validates: Requirements 4.2
    /// </summary>
    [Fact]
    public async Task Dashboard_UpdateLead_Succeeds_With_Valid_Data()
    {
        // Arrange
        var mockJsRuntime = new Mock<IJSRuntime>();
        var token = CreateJwtToken();
        
        mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>(
                "sessionStorage.getItem",
                It.IsAny<object[]>()))
            .ReturnsAsync(token);

        var leadId = Guid.NewGuid().ToString();
        var updatedLead = new Lead
        {
            LeadId = leadId,
            UserId = "test-user",
            Name = "Updated Lead",
            Title = "Senior Manager",
            Company = "Updated Corp",
            Phone = "555-8888",
            Email = "updated@corp.com",
            Location = "Chicago",
            Notes = "Updated contact",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    JsonSerializer.Serialize(new LeadResponse { Lead = updatedLead }),
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = CreateLeadService(mockJsRuntime, httpClient);

        var request = new UpdateLeadRequest
        {
            Name = "Updated Lead",
            Title = "Senior Manager",
            Company = "Updated Corp",
            Phone = "555-8888",
            Email = "updated@corp.com",
            Location = "Chicago",
            Notes = "Updated contact"
        };

        // Act
        var result = await service.UpdateLeadAsync(leadId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Lead", result.Name);
        Assert.Equal("Senior Manager", result.Title);
        Assert.Equal("Updated Corp", result.Company);
        Assert.Equal(leadId, result.LeadId);
    }

    /// <summary>
    /// Test delete lead confirmation - verifies that DeleteLeadAsync deletes a lead successfully
    /// Validates: Requirements 4.3
    /// </summary>
    [Fact]
    public async Task Dashboard_DeleteLead_Succeeds()
    {
        // Arrange
        var mockJsRuntime = new Mock<IJSRuntime>();
        var token = CreateJwtToken();
        
        mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>(
                "sessionStorage.getItem",
                It.IsAny<object[]>()))
            .ReturnsAsync(token);

        var leadId = Guid.NewGuid().ToString();

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    JsonSerializer.Serialize(new { success = true }),
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = CreateLeadService(mockJsRuntime, httpClient);

        // Act
        var result = await service.DeleteLeadAsync(leadId);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Test that create lead fails when name is missing
    /// Validates: Requirements 4.2
    /// </summary>
    [Fact]
    public async Task Dashboard_CreateLead_Fails_When_Name_Is_Empty()
    {
        // Arrange
        var mockJsRuntime = new Mock<IJSRuntime>();
        var token = CreateJwtToken();
        
        mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>(
                "sessionStorage.getItem",
                It.IsAny<object[]>()))
            .ReturnsAsync(token);

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(
                    JsonSerializer.Serialize(new { error = new { code = "VALIDATION_FAILED", message = "Name is required" } }),
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = CreateLeadService(mockJsRuntime, httpClient);

        var request = new CreateLeadRequest
        {
            Name = "", // Empty name
            Title = "Manager"
        };

        // Act
        var result = await service.CreateLeadAsync(request);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Test that leads are loaded after successful create
    /// Validates: Requirements 4.2
    /// </summary>
    [Fact]
    public async Task Dashboard_LoadLeads_After_Create_Returns_Updated_List()
    {
        // Arrange
        var mockJsRuntime = new Mock<IJSRuntime>();
        var token = CreateJwtToken();
        
        mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>(
                "sessionStorage.getItem",
                It.IsAny<object[]>()))
            .ReturnsAsync(token);

        var initialLeads = new List<Lead>
        {
            new Lead
            {
                LeadId = Guid.NewGuid().ToString(),
                UserId = "test-user",
                Name = "Existing Lead",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        var newLead = new Lead
        {
            LeadId = Guid.NewGuid().ToString(),
            UserId = "test-user",
            Name = "New Lead",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var updatedLeads = new List<Lead>(initialLeads) { newLead };

        var callCount = 0;
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    // First call - create lead
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(
                            JsonSerializer.Serialize(new LeadResponse { Lead = newLead }),
                            Encoding.UTF8,
                            "application/json")
                    };
                }
                else
                {
                    // Second call - get leads
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(
                            JsonSerializer.Serialize(new ListLeadsResponse { Leads = updatedLeads, Count = updatedLeads.Count }),
                            Encoding.UTF8,
                            "application/json")
                    };
                }
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = CreateLeadService(mockJsRuntime, httpClient);

        // Act
        var createResult = await service.CreateLeadAsync(new CreateLeadRequest { Name = "New Lead" });
        var leadsResult = await service.GetLeadsAsync();

        // Assert
        Assert.NotNull(createResult);
        Assert.NotNull(leadsResult);
        Assert.Equal(2, leadsResult.Count);
        Assert.Contains(leadsResult, l => l.Name == "New Lead");
    }
}
