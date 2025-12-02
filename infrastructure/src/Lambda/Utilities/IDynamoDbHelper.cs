using System.Collections.Generic;
using System.Threading.Tasks;
using Lambda.Models;

namespace Lambda.Utilities;

public interface IDynamoDbHelper
{
    Task<Lead> CreateLeadAsync(Lead lead);
    Task<Lead?> GetLeadAsync(string userId, string leadId);
    Task<List<Lead>> QueryLeadsByUserIdAsync(string userId);
    Task<Lead> UpdateLeadAsync(Lead lead);
    Task DeleteLeadAsync(string userId, string leadId);
}
