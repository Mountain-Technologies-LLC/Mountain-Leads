using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using website.Models;

namespace website.Services;

public interface ILeadService
{
    Task<Lead?> CreateLeadAsync(CreateLeadRequest request);
    Task<List<Lead>> GetLeadsAsync();
    Task<Lead?> GetLeadAsync(string leadId);
    Task<Lead?> UpdateLeadAsync(string leadId, UpdateLeadRequest request);
    Task<bool> DeleteLeadAsync(string leadId);
    Task<bool> InitializeDefaultLeadsAsync();
}

public class LeadService : ILeadService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly string _apiGatewayUrl;
    private const string IdTokenKey = "idToken";

    public LeadService(HttpClient httpClient, IJSRuntime jsRuntime, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _apiGatewayUrl = configuration["AWS:ApiGatewayUrl"] ?? throw new InvalidOperationException("ApiGatewayUrl not configured");
    }

    private async Task<string?> GetAuthTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", IdTokenKey);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting auth token: {ex.Message}");
            return null;
        }
    }

    private async Task<HttpRequestMessage> CreateAuthorizedRequestAsync(HttpMethod method, string url, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url);
        var token = await GetAuthTokenAsync();
        
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        
        if (content != null)
        {
            request.Content = content;
        }
        
        return request;
    }

    public async Task<Lead?> CreateLeadAsync(CreateLeadRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var httpRequest = await CreateAuthorizedRequestAsync(HttpMethod.Post, $"{_apiGatewayUrl}/leads", content);
            var response = await _httpClient.SendAsync(httpRequest);

            if (response.IsSuccessStatusCode)
            {
                var leadResponse = await response.Content.ReadFromJsonAsync<LeadResponse>();
                return leadResponse?.Lead;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Create lead failed: {response.StatusCode} - {errorContent}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Create lead error: {ex.Message}");
            return null;
        }
    }

    public async Task<List<Lead>> GetLeadsAsync()
    {
        try
        {
            var httpRequest = await CreateAuthorizedRequestAsync(HttpMethod.Get, $"{_apiGatewayUrl}/leads");
            var response = await _httpClient.SendAsync(httpRequest);

            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"GetLeads Response Status: {response.StatusCode}");
            Console.WriteLine($"GetLeads Response Body: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ListLeadsResponse>>();
                Console.WriteLine($"Parsed API Response - Success: {apiResponse?.Success}, Data: {apiResponse?.Data != null}");
                
                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    Console.WriteLine($"Leads Count: {apiResponse.Data.Leads?.Count ?? 0}");
                    return apiResponse.Data.Leads ?? new List<Lead>();
                }
                
                Console.WriteLine($"API returned unsuccessful response or no data");
                return new List<Lead>();
            }

            Console.WriteLine($"Get leads failed: {response.StatusCode} - {responseContent}");
            return new List<Lead>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get leads error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return new List<Lead>();
        }
    }

    public async Task<Lead?> GetLeadAsync(string leadId)
    {
        try
        {
            var httpRequest = await CreateAuthorizedRequestAsync(HttpMethod.Get, $"{_apiGatewayUrl}/leads/{leadId}");
            var response = await _httpClient.SendAsync(httpRequest);

            if (response.IsSuccessStatusCode)
            {
                var leadResponse = await response.Content.ReadFromJsonAsync<LeadResponse>();
                return leadResponse?.Lead;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine($"Lead not found: {leadId}");
                return null;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Get lead failed: {response.StatusCode} - {errorContent}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get lead error: {ex.Message}");
            return null;
        }
    }

    public async Task<Lead?> UpdateLeadAsync(string leadId, UpdateLeadRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var httpRequest = await CreateAuthorizedRequestAsync(HttpMethod.Put, $"{_apiGatewayUrl}/leads/{leadId}", content);
            var response = await _httpClient.SendAsync(httpRequest);

            if (response.IsSuccessStatusCode)
            {
                var leadResponse = await response.Content.ReadFromJsonAsync<LeadResponse>();
                return leadResponse?.Lead;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Update lead failed: {response.StatusCode} - {errorContent}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Update lead error: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteLeadAsync(string leadId)
    {
        try
        {
            var httpRequest = await CreateAuthorizedRequestAsync(HttpMethod.Delete, $"{_apiGatewayUrl}/leads/{leadId}");
            var response = await _httpClient.SendAsync(httpRequest);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Delete lead failed: {response.StatusCode} - {errorContent}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Delete lead error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> InitializeDefaultLeadsAsync()
    {
        try
        {
            var httpRequest = await CreateAuthorizedRequestAsync(HttpMethod.Post, $"{_apiGatewayUrl}/leads/init");
            var response = await _httpClient.SendAsync(httpRequest);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Initialize default leads failed: {response.StatusCode} - {errorContent}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Initialize default leads error: {ex.Message}");
            return false;
        }
    }
}
