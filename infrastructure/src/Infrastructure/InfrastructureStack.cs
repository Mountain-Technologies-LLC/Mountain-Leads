using Amazon.CDK;
using Constructs;
using Infrastructure.Constructs;

namespace Infrastructure
{
    internal class InfrastructureStackProps : StackProps
    {
        public string Name;
        public string DomainName;
    }

    public class InfrastructureStack : Stack
    {
        internal InfrastructureStack(Construct scope, string id, InfrastructureStackProps props = null) : base(scope, id, props)
        {
            // S3 and CloudFront constructs (existing)
            var bucketConstruct = new BucketConstruct(this, "BucketConstruct", new BucketConstructProps
            {
                Name = props.Name,
                DomainName = props.DomainName
            });

            var distributionConstruct = new DistributionConstruct(this, "DistributionConstruct", new DistributionConstructProps
            {
                Bucket = bucketConstruct.Bucket,
                DomainName = props.DomainName
            });

            _ = new BucketDeploymentConstruct(this, "BucketDeploymentConstruct", new BucketDeploymentConstructProps
            {
                Bucket = bucketConstruct.Bucket,
                distribution = distributionConstruct.distribution
            });

            // Add CognitoConstruct instantiation
            var cognitoConstruct = new CognitoConstruct(this, "CognitoConstruct", new CognitoConstructProps
            {
                Name = props.Name,
                DomainName = props.DomainName
            });

            // Add DynamoDbConstruct instantiation
            var dynamoDbConstruct = new DynamoDbConstruct(this, "DynamoDbConstruct", new DynamoDbConstructProps
            {
                Name = props.Name,
                DomainName = props.DomainName
            });

            // Add LambdaConstruct instantiation (pass DynamoDB table reference)
            var lambdaConstruct = new LambdaConstruct(this, "LambdaConstruct", new LambdaConstructProps
            {
                Name = props.Name,
                DomainName = props.DomainName,
                DynamoDbTable = dynamoDbConstruct.Table
            });

            // Add ApiGatewayConstruct instantiation (pass Cognito and Lambda references)
            _ = new ApiGatewayConstruct(this, "ApiGatewayConstruct", new ApiGatewayConstructProps
            {
                Name = props.Name,
                DomainName = props.DomainName,
                UserPool = cognitoConstruct.UserPool,
                CreateLeadFunction = lambdaConstruct.CreateLeadFunction,
                ListLeadsFunction = lambdaConstruct.ListLeadsFunction,
                GetLeadFunction = lambdaConstruct.GetLeadFunction,
                UpdateLeadFunction = lambdaConstruct.UpdateLeadFunction,
                DeleteLeadFunction = lambdaConstruct.DeleteLeadFunction,
                InitLeadsFunction = lambdaConstruct.InitLeadsFunction
            });
        }
    }
}
