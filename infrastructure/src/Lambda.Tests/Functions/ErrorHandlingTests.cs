using System;
using System.Text.Json;
using Xunit;
using Lambda.Models;

namespace Lambda.Tests.Functions;

public class ErrorHandlingTests
{
    [Fact]
    public void ApiResponse_WithError_SerializesCorrectly()
    {
        // Arrange
        var response = new ApiResponse<object>
        {
            Success = false,
            Error = new ErrorDetails
            {
                Code = "VALIDATION_FAILED",
                Message = "Name is required"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(response);
        var deserialized = JsonSerializer.Deserialize<ApiResponse<object>>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.False(deserialized.Success);
        Assert.NotNull(deserialized.Error);
        Assert.Equal("VALIDATION_FAILED", deserialized.Error.Code);
        Assert.Equal("Name is required", deserialized.Error.Message);
    }

    [Fact]
    public void ApiResponse_WithSuccess_SerializesCorrectly()
    {
        // Arrange
        var lead = new Lead
        {
            UserId = "user-123",
            LeadId = "lead-456",
            Name = "Test Lead",
            Email = "test@example.com"
        };
        var response = new ApiResponse<Lead>
        {
            Success = true,
            Data = lead
        };

        // Act
        var json = JsonSerializer.Serialize(response);
        var deserialized = JsonSerializer.Deserialize<ApiResponse<Lead>>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.True(deserialized.Success);
        Assert.NotNull(deserialized.Data);
        Assert.Equal("user-123", deserialized.Data.UserId);
        Assert.Equal("lead-456", deserialized.Data.LeadId);
        Assert.Equal("Test Lead", deserialized.Data.Name);
    }

    [Fact]
    public void ErrorDetails_WithAllFields_SerializesCorrectly()
    {
        // Arrange
        var error = new ErrorDetails
        {
            Code = "AUTH_UNAUTHORIZED",
            Message = "You are not authorized to access this resource",
            Details = new System.Collections.Generic.Dictionary<string, string>
            {
                { "resource", "lead-123" },
                { "action", "delete" }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(error);
        var deserialized = JsonSerializer.Deserialize<ErrorDetails>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("AUTH_UNAUTHORIZED", deserialized.Code);
        Assert.Equal("You are not authorized to access this resource", deserialized.Message);
        Assert.NotNull(deserialized.Details);
        Assert.Equal(2, deserialized.Details.Count);
        Assert.Equal("lead-123", deserialized.Details["resource"]);
        Assert.Equal("delete", deserialized.Details["action"]);
    }

    [Fact]
    public void CreateLeadRequest_Deserializes_Correctly()
    {
        // Arrange
        var json = @"{
            ""name"": ""John Doe"",
            ""title"": ""CEO"",
            ""company"": ""Acme Corp"",
            ""phone"": ""555-1234"",
            ""email"": ""john@acme.com"",
            ""location"": ""New York"",
            ""notes"": ""Important client""
        }";

        // Act
        var request = JsonSerializer.Deserialize<CreateLeadRequest>(json);

        // Assert
        Assert.NotNull(request);
        Assert.Equal("John Doe", request.Name);
        Assert.Equal("CEO", request.Title);
        Assert.Equal("Acme Corp", request.Company);
        Assert.Equal("555-1234", request.Phone);
        Assert.Equal("john@acme.com", request.Email);
        Assert.Equal("New York", request.Location);
        Assert.Equal("Important client", request.Notes);
    }

    [Fact]
    public void UpdateLeadRequest_Deserializes_Correctly()
    {
        // Arrange
        var json = @"{
            ""name"": ""Jane Smith"",
            ""title"": ""CTO"",
            ""company"": ""Tech Inc"",
            ""phone"": ""555-5678"",
            ""email"": ""jane@tech.com"",
            ""location"": ""San Francisco"",
            ""notes"": ""Tech lead""
        }";

        // Act
        var request = JsonSerializer.Deserialize<UpdateLeadRequest>(json);

        // Assert
        Assert.NotNull(request);
        Assert.Equal("Jane Smith", request.Name);
        Assert.Equal("CTO", request.Title);
        Assert.Equal("Tech Inc", request.Company);
        Assert.Equal("555-5678", request.Phone);
        Assert.Equal("jane@tech.com", request.Email);
        Assert.Equal("San Francisco", request.Location);
        Assert.Equal("Tech lead", request.Notes);
    }

    [Fact]
    public void ListLeadsResponse_Serializes_Correctly()
    {
        // Arrange
        var leads = new System.Collections.Generic.List<Lead>
        {
            new Lead { UserId = "user-1", LeadId = "lead-1", Name = "Lead 1" },
            new Lead { UserId = "user-1", LeadId = "lead-2", Name = "Lead 2" }
        };
        var response = new ListLeadsResponse
        {
            Leads = leads,
            Count = leads.Count
        };

        // Act
        var json = JsonSerializer.Serialize(response);
        var deserialized = JsonSerializer.Deserialize<ListLeadsResponse>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized.Count);
        Assert.Equal(2, deserialized.Leads.Count);
        Assert.Equal("Lead 1", deserialized.Leads[0].Name);
        Assert.Equal("Lead 2", deserialized.Leads[1].Name);
    }
}
