using System;
using System.Text.Json.Serialization;
using Amazon.DynamoDBv2.DataModel;

namespace Lambda.Models;

[DynamoDBTable("leads-mountaintechnologiesllc-com")]
public class Lead
{
    [DynamoDBHashKey("userId")]
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [DynamoDBRangeKey("leadId")]
    [JsonPropertyName("leadId")]
    public string LeadId { get; set; } = string.Empty;

    [DynamoDBProperty("name")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [DynamoDBProperty("title")]
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [DynamoDBProperty("company")]
    [JsonPropertyName("company")]
    public string? Company { get; set; }

    [DynamoDBProperty("phone")]
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [DynamoDBProperty("email")]
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [DynamoDBProperty("location")]
    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [DynamoDBProperty("notes")]
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [DynamoDBProperty("createdAt")]
    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;

    [DynamoDBProperty("updatedAt")]
    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; set; } = string.Empty;
}
