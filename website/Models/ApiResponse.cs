using System.Text.Json.Serialization;

namespace website.Models;

public class ApiResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("error")]
    public ErrorDetails? Error { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }
}

public class ErrorDetails
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public Dictionary<string, object>? Details { get; set; }
}

public class LeadResponse
{
    [JsonPropertyName("lead")]
    public Lead? Lead { get; set; }
}

public class ListLeadsResponse
{
    [JsonPropertyName("leads")]
    public List<Lead> Leads { get; set; } = new();

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public class CreateLeadRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("company")]
    public string? Company { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

public class UpdateLeadRequest : CreateLeadRequest
{
    // Inherits all fields from CreateLeadRequest
}
