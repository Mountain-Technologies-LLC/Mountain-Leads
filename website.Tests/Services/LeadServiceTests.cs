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

namespace website.Tests.Services;

/// <summary>
/// Unit tests for LeadService error handling
/// Feature: mountain-leads-app
/// </summary>
public class LeadServiceTests
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
    /// Test handling 401 unauthorized responses
    /// Validates: Requirements 8.2
    /// </summary>
    [Fact]
    public async Task CreateLeadAsync_Returns_Null_On_401_Unauthorized()
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
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent(
                    JsonSerializer.Serialize(new { error = new { code = "AUTH_UNAUTHORIZED", message = "Unauthorized" } }),
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = CreateLeadService(mockJsRuntime, httpClient);

        var request = new CreateLeadRequest { Name = "Test Lead" };

        // Act
        var result = await service.CreateLeadAsync(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetLeadsAsync_Returns_EmptyList_On_401_Unauthorized()
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
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent(
                    JsonSerializer.Serialize(new { error = new { code = "AUTH_UNAUTHORIZED", message = "Unauthorized" } }),
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

    [Fact]
    public async Task UpdateLeadAsync_Returns_Null_On_401_Unauthorized()
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
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent(
                    JsonSerializer.Serialize(new { error = new { code = "AUTH_UNAUTHORIZED", message = "Unauthorized" } }),
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = CreateLeadService(mockJsRuntime, httpClient);

        var request = new UpdateLeadRequest { Name = "Updated Lead" };

        // Act
        var result = await service.UpdateLeadAsync("test-lead-id", request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteLeadAsync_Returns_False_On_401_Unauthorized()
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
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent(
                    JsonSerializer.Serialize(new { error = new { code = "AUTH_UNAUTHORIZED", message = "Unauthorized" } }),
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = CreateLeadService(mockJsRuntime, httpClient);

        // Act
        var result = await service.DeleteLeadAsync("test-lead-id");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Test handling 404 not found responses
    /// Validates: Requirements 8.2
    /// </summary>
    [Fact]
    public async Task GetLeadAsync_Returns_Null_On_404_NotFound()
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
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent(
                    JsonSerializer.Serialize(new { error = new { code = "RESOURCE_NOT_FOUND", message = "Lead not found" } }),
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = CreateLeadService(mockJsRuntime, httpClient);

        // Act
        var result = await service.GetLeadAsync("non-existent-lead-id");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateLeadAsync_Returns_Null_On_404_NotFound()
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
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent(
                    JsonSerializer.Serialize(new { error = new { code = "RESOURCE_NOT_FOUND", message = "Lead not found" } }),
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = CreateLeadService(mockJsRuntime, httpClient);

        var request = new UpdateLeadRequest { Name = "Updated Lead" };

        // Act
        var result = await service.UpdateLeadAsync("non-existent-lead-id", request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteLeadAsync_Returns_False_On_404_NotFound()
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
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent(
                    JsonSerializer.Serialize(new { error = new { code = "RESOURCE_NOT_FOUND", message = "Lead not found" } }),
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = CreateLeadService(mockJsRuntime, httpClient);

        // Act
        var result = await service.DeleteLeadAsync("non-existent-lead-id");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Test handling 500 server error responses
    /// Validates: Requirements 8.2
    /// </summary>
    [Fact]
    public async Task CreateLeadAsync_Returns_Null_On_500_InternalServerError()
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
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(
                    JsonSerializer.Serialize(new { error = new { code = "INTERNAL_ERROR", message = "Internal server error" } }),
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = CreateLeadService(mockJsRuntime, httpClient);

        var request = new CreateLeadRequest { Name = "Test Lead" };

        // Act
        var result = await service.CreateLeadAsync(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetLeadsAsync_Returns_EmptyList_On_500_InternalServerError()
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
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(
                    JsonSerializer.Serialize(new { error = new { code = "INTERNAL_ERROR", message = "Internal server error" } }),
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

    [Fact]
    public async Task GetLeadAsync_Returns_Null_On_500_InternalServerError()
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
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(
                    JsonSerializer.Serialize(new { error = new { code = "INTERNAL_ERROR", message = "Internal server error" } }),
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = CreateLeadService(mockJsRuntime, httpClient);

        // Act
        var result = await service.GetLeadAsync("test-lead-id");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateLeadAsync_Returns_Null_On_500_InternalServerError()
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
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(
                    JsonSerializer.Serialize(new { error = new { code = "INTERNAL_ERROR", message = "Internal server error" } }),
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = CreateLeadService(mockJsRuntime, httpClient);

        var request = new UpdateLeadRequest { Name = "Updated Lead" };

        // Act
        var result = await service.UpdateLeadAsync("test-lead-id", request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteLeadAsync_Returns_False_On_500_InternalServerError()
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
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(
                    JsonSerializer.Serialize(new { error = new { code = "INTERNAL_ERROR", message = "Internal server error" } }),
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = CreateLeadService(mockJsRuntime, httpClient);

        // Act
        var result = await service.DeleteLeadAsync("test-lead-id");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task InitializeDefaultLeadsAsync_Returns_False_On_500_InternalServerError()
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
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(
                    JsonSerializer.Serialize(new { error = new { code = "INTERNAL_ERROR", message = "Internal server error" } }),
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = CreateLeadService(mockJsRuntime, httpClient);

        // Act
        var result = await service.InitializeDefaultLeadsAsync();

        // Assert
        Assert.False(result);
    }
}
