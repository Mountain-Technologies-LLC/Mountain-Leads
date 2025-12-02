using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using website.Models;

namespace website.Services;

public interface IAuthService
{
    Task<AuthenticationResult?> RegisterAsync(string email, string password);
    Task<AuthenticationResult?> LoginAsync(string email, string password);
    Task LogoutAsync();
    Task<string?> GetCurrentUserIdAsync();
    Task<bool> IsAuthenticatedAsync();
}

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly string _userPoolId;
    private readonly string _clientId;
    private readonly string _region;

    private const string IdTokenKey = "idToken";
    private const string AccessTokenKey = "accessToken";
    private const string RefreshTokenKey = "refreshToken";

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _userPoolId = configuration["AWS:UserPoolId"] ?? throw new InvalidOperationException("UserPoolId not configured");
        _clientId = configuration["AWS:ClientId"] ?? throw new InvalidOperationException("ClientId not configured");
        _region = configuration["AWS:Region"] ?? "us-east-1";
    }

    public async Task<AuthenticationResult?> RegisterAsync(string email, string password)
    {
        try
        {
            var signUpRequest = new
            {
                ClientId = _clientId,
                Username = email,
                Password = password,
                UserAttributes = new[]
                {
                    new { Name = "email", Value = email }
                }
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null // Use exact property names (PascalCase)
            };
            var jsonString = JsonSerializer.Serialize(signUpRequest, jsonOptions);
            var content = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/x-amz-json-1.1");

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://cognito-idp.{_region}.amazonaws.com/")
            {
                Content = content
            };
            request.Headers.Add("X-Amz-Target", "AWSCognitoIdentityProviderService.SignUp");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Registration failed: {response.StatusCode} - {responseContent}");
                return null;
            }

            // Auto-confirm user for development (in production, email verification would be required)
            // For now, we'll attempt to login immediately after registration
            return await LoginAsync(email, password);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Registration error: {ex.Message}");
            return null;
        }
    }

    public async Task<AuthenticationResult?> LoginAsync(string email, string password)
    {
        try
        {
            var authRequest = new
            {
                ClientId = _clientId,
                AuthFlow = "USER_PASSWORD_AUTH",
                AuthParameters = new Dictionary<string, string>
                {
                    { "USERNAME", email },
                    { "PASSWORD", password }
                }
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null // Use exact property names (PascalCase)
            };
            var jsonString = JsonSerializer.Serialize(authRequest, jsonOptions);
            var content = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/x-amz-json-1.1");

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://cognito-idp.{_region}.amazonaws.com/")
            {
                Content = content
            };
            request.Headers.Add("X-Amz-Target", "AWSCognitoIdentityProviderService.InitiateAuth");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Login failed: {response.StatusCode} - {responseContent}");
                return null;
            }

            var authResponse = JsonSerializer.Deserialize<CognitoAuthResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (authResponse?.AuthenticationResult != null)
            {
                var result = new AuthenticationResult
                {
                    IdToken = authResponse.AuthenticationResult.IdToken,
                    AccessToken = authResponse.AuthenticationResult.AccessToken,
                    RefreshToken = authResponse.AuthenticationResult.RefreshToken ?? string.Empty,
                    ExpiresIn = authResponse.AuthenticationResult.ExpiresIn
                };

                // Store tokens in sessionStorage
                await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", IdTokenKey, result.IdToken);
                await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", AccessTokenKey, result.AccessToken);
                await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", RefreshTokenKey, result.RefreshToken);

                return result;
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
            return null;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            // Clear tokens from sessionStorage
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", IdTokenKey);
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", AccessTokenKey);
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", RefreshTokenKey);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Logout error: {ex.Message}");
        }
    }

    public async Task<string?> GetCurrentUserIdAsync()
    {
        try
        {
            var idToken = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", IdTokenKey);
            
            if (string.IsNullOrEmpty(idToken))
            {
                return null;
            }

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(idToken);

            // Try to get userId from 'sub' claim first, then 'cognito:username'
            var userId = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
                        ?? token.Claims.FirstOrDefault(c => c.Type == "cognito:username")?.Value;

            return userId;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetCurrentUserId error: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var idToken = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", IdTokenKey);
            
            if (string.IsNullOrEmpty(idToken))
            {
                return false;
            }

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(idToken);

            // Check if token is expired
            var expirationTime = token.ValidTo;
            return expirationTime > DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"IsAuthenticated error: {ex.Message}");
            return false;
        }
    }
}

// Helper classes for Cognito API responses
internal class CognitoAuthResponse
{
    [JsonPropertyName("AuthenticationResult")]
    public CognitoAuthenticationResult? AuthenticationResult { get; set; }
}

internal class CognitoAuthenticationResult
{
    [JsonPropertyName("IdToken")]
    public string IdToken { get; set; } = string.Empty;

    [JsonPropertyName("AccessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("RefreshToken")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("ExpiresIn")]
    public int ExpiresIn { get; set; }
}
