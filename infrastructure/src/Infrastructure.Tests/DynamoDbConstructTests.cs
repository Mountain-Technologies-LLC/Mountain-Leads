using System;
using System.Linq;
using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using FsCheck;
using FsCheck.Xunit;
using Infrastructure.Constructs;
using Xunit;

namespace Infrastructure.Tests
{
    /// <summary>
    /// Property-based tests for DynamoDbConstruct
    /// Feature: mountain-leads-app, Property 11: User data isolation
    /// </summary>
    public class DynamoDbConstructTests
    {
        /// <summary>
        /// Property 11: User data isolation
        /// For any two distinct users U1 and U2, the DynamoDB table structure must ensure
        /// that leads created by U1 are isolated from U2 through proper partition key design.
        /// This property verifies that the table is configured with userId as partition key
        /// and leadId as sort key, which enables DynamoDB to automatically isolate data
        /// by user when queries are performed with userId as the partition key.
        /// Validates: Requirements 4.1, 9.1, 9.2, 9.4, 9.5
        /// </summary>
        [Property(MaxTest = 100)]
        public void DynamoDbConstruct_ConfiguresTableForUserDataIsolation(string domainSuffix)
        {
            // Filter to valid domain suffixes
            if (string.IsNullOrWhiteSpace(domainSuffix) || domainSuffix.Length < 3 || domainSuffix.Length > 50)
            {
                return; // Skip invalid inputs
            }
            
            var cleaned = domainSuffix.Replace(" ", "").Replace(".", "-").ToLower();
            if (cleaned.Length == 0)
            {
                return; // Skip invalid inputs
            }
            
            var domainName = $"leads.{cleaned}.com";
            
            // Arrange
            var app = new App();
            var stack = new Stack(app, "TestStack");
            
            var props = new DynamoDbConstructProps
            {
                Name = "test-stack",
                DomainName = domainName
            };

            // Act
            var construct = new DynamoDbConstruct(stack, "TestDynamoDb", props);

            // Assert - Verify Table is created
            Assert.NotNull(construct.Table);

            // Synthesize the stack to get CloudFormation template
            var assembly = app.Synth();
            var template = assembly.GetStackByName(stack.StackName).Template;

            // Verify DynamoDB Table resource exists in template
            var templateDict = template as Dictionary<string, object>;
            Assert.NotNull(templateDict);
            Assert.True(templateDict.ContainsKey("Resources"));
            
            var resources = templateDict["Resources"] as Dictionary<string, object>;
            Assert.NotNull(resources);

            // Find the DynamoDB Table resource
            var tableResource = resources.Values
                .Cast<Dictionary<string, object>>()
                .FirstOrDefault(r => r.ContainsKey("Type") && 
                                   r["Type"].ToString() == "AWS::DynamoDB::Table");
            
            Assert.NotNull(tableResource);

            // Verify Table properties that enable user data isolation
            var properties = tableResource["Properties"] as Dictionary<string, object>;
            Assert.NotNull(properties);

            // CRITICAL: Verify partition key is userId (enables data isolation)
            Assert.True(properties.ContainsKey("KeySchema"));
            var keySchemaObj = properties["KeySchema"];
            Assert.NotNull(keySchemaObj);
            
            // KeySchema is an Object[] array
            var keySchema = (keySchemaObj as object[])?.Cast<Dictionary<string, object>>().ToList();
            Assert.NotNull(keySchema);
            Assert.True(keySchema.Count >= 1, "KeySchema must have at least partition key");

            // Find partition key (HASH)
            var partitionKeyEntry = keySchema
                .FirstOrDefault(k => k.ContainsKey("KeyType") && 
                                   k["KeyType"].ToString() == "HASH");
            
            Assert.NotNull(partitionKeyEntry);
            Assert.True(partitionKeyEntry.ContainsKey("AttributeName"));
            Assert.Equal("userId", partitionKeyEntry["AttributeName"].ToString());

            // CRITICAL: Verify sort key is leadId (enables unique lead identification per user)
            // KeySchema should have exactly 2 entries: partition key (HASH) and sort key (RANGE)
            Assert.True(keySchema.Count == 2, $"KeySchema must have exactly 2 keys (partition and sort), but has {keySchema.Count}");
            
            var sortKeyEntry = keySchema
                .FirstOrDefault(k => k.ContainsKey("KeyType") && 
                                   k["KeyType"].ToString() == "RANGE");
            
            Assert.NotNull(sortKeyEntry);
            Assert.True(sortKeyEntry.ContainsKey("AttributeName"));
            Assert.Equal("leadId", sortKeyEntry["AttributeName"].ToString());

            // Verify attribute definitions include userId and leadId
            Assert.True(properties.ContainsKey("AttributeDefinitions"));
            var attributeDefinitionsObj = properties["AttributeDefinitions"];
            Assert.NotNull(attributeDefinitionsObj);
            
            // AttributeDefinitions is an Object[] array
            var attributeDefs = (attributeDefinitionsObj as object[])?.Cast<Dictionary<string, object>>().ToList();
            Assert.NotNull(attributeDefs);
            
            // Verify userId attribute is defined as String
            var userIdAttr = attributeDefs.FirstOrDefault(a => 
                a.ContainsKey("AttributeName") && 
                a["AttributeName"].ToString() == "userId");
            Assert.NotNull(userIdAttr);
            Assert.True(userIdAttr.ContainsKey("AttributeType"));
            Assert.Equal("S", userIdAttr["AttributeType"].ToString()); // S = String

            // Verify leadId attribute is defined as String
            var leadIdAttr = attributeDefs.FirstOrDefault(a => 
                a.ContainsKey("AttributeName") && 
                a["AttributeName"].ToString() == "leadId");
            Assert.NotNull(leadIdAttr);
            Assert.True(leadIdAttr.ContainsKey("AttributeType"));
            Assert.Equal("S", leadIdAttr["AttributeType"].ToString()); // S = String

            // Verify billing mode is PAY_PER_REQUEST (on-demand)
            Assert.True(properties.ContainsKey("BillingMode"));
            Assert.Equal("PAY_PER_REQUEST", properties["BillingMode"].ToString());

            // Verify point-in-time recovery is enabled
            Assert.True(properties.ContainsKey("PointInTimeRecoverySpecification"));
            var pitrSpec = properties["PointInTimeRecoverySpecification"] as Dictionary<string, object>;
            Assert.NotNull(pitrSpec);
            Assert.True(pitrSpec.ContainsKey("PointInTimeRecoveryEnabled"));
            Assert.True(Convert.ToBoolean(pitrSpec["PointInTimeRecoveryEnabled"]));

            // Verify table name follows domain-based naming convention
            Assert.True(properties.ContainsKey("TableName"));
            var tableName = properties["TableName"].ToString();
            Assert.NotNull(tableName);
            Assert.Contains(cleaned, tableName);

            // Property Validation:
            // The combination of userId (partition key) and leadId (sort key) ensures:
            // 1. All leads for a user are stored in the same partition (userId)
            // 2. Queries with userId will only return that user's leads (data isolation)
            // 3. Each lead has a unique identifier within the user's partition (leadId)
            // 4. Cross-user access is prevented at the database level by partition key design
            // 5. DynamoDB automatically enforces isolation when queries specify userId
        }
    }
}
