using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Constructs;

namespace Infrastructure.Constructs
{
    public class DynamoDbConstructProps : IStackProps
    {
        public string Name;
        public string DomainName;
    }

    public class DynamoDbConstruct : Construct
    {
        public Table Table;

        public DynamoDbConstruct(Construct scope, string id, DynamoDbConstructProps props = null) : base(scope, id)
        {
            // Set table name based on domain: leads-mountaintechnologiesllc-com
            var tableName = props.DomainName.Replace(".", "-");

            // Define DynamoDB table with partition key userId (String) and sort key leadId (String)
            Table = new Table(this, "LeadsTable", new TableProps
            {
                TableName = tableName,
                
                // Partition key: userId (String)
                PartitionKey = new Amazon.CDK.AWS.DynamoDB.Attribute
                {
                    Name = "userId",
                    Type = AttributeType.STRING
                },
                
                // Sort key: leadId (String)
                SortKey = new Amazon.CDK.AWS.DynamoDB.Attribute
                {
                    Name = "leadId",
                    Type = AttributeType.STRING
                },
                
                // Configure on-demand billing mode
                BillingMode = BillingMode.PAY_PER_REQUEST,
                
                // Enable point-in-time recovery
                PointInTimeRecoverySpecification = new PointInTimeRecoverySpecification
                {
                    PointInTimeRecoveryEnabled = true
                },
                
                // Removal policy for development
                RemovalPolicy = RemovalPolicy.DESTROY
            });

            // Add CloudFormation output for table name
            _ = new CfnOutput(this, "TableName", new CfnOutputProps
            {
                Value = Table.TableName,
                Description = "DynamoDB Table Name",
                ExportName = $"{props.Name}-TableName"
            });
        }
    }
}
