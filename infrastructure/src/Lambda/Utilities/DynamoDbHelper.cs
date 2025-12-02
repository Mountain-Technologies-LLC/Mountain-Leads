using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Lambda.Models;

namespace Lambda.Utilities;

public class DynamoDbHelper : IDynamoDbHelper
{
    private readonly DynamoDBContext _context;
    private readonly string _tableName;

    public DynamoDbHelper(IAmazonDynamoDB dynamoDbClient, string tableName)
    {
        _context = new DynamoDBContext(dynamoDbClient);
        _tableName = tableName;
    }

    /// <summary>
    /// Creates a new lead in DynamoDB
    /// </summary>
    public virtual async Task<Lead> CreateLeadAsync(Lead lead)
    {
        await _context.SaveAsync(lead);
        return lead;
    }

    /// <summary>
    /// Gets a specific lead by userId and leadId
    /// </summary>
    public virtual async Task<Lead?> GetLeadAsync(string userId, string leadId)
    {
        return await _context.LoadAsync<Lead>(userId, leadId);
    }

    /// <summary>
    /// Queries all leads for a specific user
    /// </summary>
    public virtual async Task<List<Lead>> QueryLeadsByUserIdAsync(string userId)
    {
        var config = new DynamoDBOperationConfig
        {
            QueryFilter = new List<ScanCondition>()
        };

        var search = _context.QueryAsync<Lead>(userId, config);
        var leads = await search.GetRemainingAsync();
        return leads;
    }

    /// <summary>
    /// Updates an existing lead
    /// </summary>
    public virtual async Task<Lead> UpdateLeadAsync(Lead lead)
    {
        await _context.SaveAsync(lead);
        return lead;
    }

    /// <summary>
    /// Deletes a lead by userId and leadId
    /// </summary>
    public virtual async Task DeleteLeadAsync(string userId, string leadId)
    {
        await _context.DeleteAsync<Lead>(userId, leadId);
    }
}
