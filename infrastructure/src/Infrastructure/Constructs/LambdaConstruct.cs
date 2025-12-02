using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;
using Constructs;
using System.Collections.Generic;

namespace Infrastructure.Constructs
{
    public class LambdaConstructProps : IStackProps
    {
        public string Name;
        public string DomainName;
        public Table DynamoDbTable;
    }

    public class LambdaConstruct : Construct
    {
        public Function CreateLeadFunction;
        public Function ListLeadsFunction;
        public Function GetLeadFunction;
        public Function UpdateLeadFunction;
        public Function DeleteLeadFunction;
        public Function InitLeadsFunction;

        public LambdaConstruct(Construct scope, string id, LambdaConstructProps props = null) : base(scope, id)
        {
            // Common environment variables for all Lambda functions
            var environment = new Dictionary<string, string>
            {
                { "TABLE_NAME", props.DynamoDbTable.TableName }
                // AWS_REGION is automatically provided by Lambda runtime
            };

            // Common Lambda function configuration
            var functionProps = new FunctionProps
            {
                Runtime = Runtime.DOTNET_8,
                Handler = "Lambda",
                Code = Code.FromAsset("./infrastructure/src/Lambda/bin/Release/net8.0/publish"),
                // Code = Code.FromAsset("../src/Lambda/bin/Release/net8.0/publish"),
                Timeout = Duration.Seconds(30),
                MemorySize = 512,
                Environment = environment
            };

            // Create Lambda function for creating leads (POST /leads)
            CreateLeadFunction = new Function(this, "CreateLeadFunction", new FunctionProps
            {
                FunctionName = $"{props.DomainName.Replace(".", "-")}-create-lead",
                Runtime = functionProps.Runtime,
                Handler = "Lambda::Lambda.Functions.CreateLeadFunction::FunctionHandler",
                Code = functionProps.Code,
                Timeout = functionProps.Timeout,
                MemorySize = functionProps.MemorySize,
                Environment = functionProps.Environment
            });

            // Create Lambda function for listing leads (GET /leads)
            ListLeadsFunction = new Function(this, "ListLeadsFunction", new FunctionProps
            {
                FunctionName = $"{props.DomainName.Replace(".", "-")}-list-leads",
                Runtime = functionProps.Runtime,
                Handler = "Lambda::Lambda.Functions.ListLeadsFunction::FunctionHandler",
                Code = functionProps.Code,
                Timeout = functionProps.Timeout,
                MemorySize = functionProps.MemorySize,
                Environment = functionProps.Environment
            });

            // Create Lambda function for getting a specific lead (GET /leads/{leadId})
            GetLeadFunction = new Function(this, "GetLeadFunction", new FunctionProps
            {
                FunctionName = $"{props.DomainName.Replace(".", "-")}-get-lead",
                Runtime = functionProps.Runtime,
                Handler = "Lambda::Lambda.Functions.GetLeadFunction::FunctionHandler",
                Code = functionProps.Code,
                Timeout = functionProps.Timeout,
                MemorySize = functionProps.MemorySize,
                Environment = functionProps.Environment
            });

            // Create Lambda function for updating a lead (PUT /leads/{leadId})
            UpdateLeadFunction = new Function(this, "UpdateLeadFunction", new FunctionProps
            {
                FunctionName = $"{props.DomainName.Replace(".", "-")}-update-lead",
                Runtime = functionProps.Runtime,
                Handler = "Lambda::Lambda.Functions.UpdateLeadFunction::FunctionHandler",
                Code = functionProps.Code,
                Timeout = functionProps.Timeout,
                MemorySize = functionProps.MemorySize,
                Environment = functionProps.Environment
            });

            // Create Lambda function for deleting a lead (DELETE /leads/{leadId})
            DeleteLeadFunction = new Function(this, "DeleteLeadFunction", new FunctionProps
            {
                FunctionName = $"{props.DomainName.Replace(".", "-")}-delete-lead",
                Runtime = functionProps.Runtime,
                Handler = "Lambda::Lambda.Functions.DeleteLeadFunction::FunctionHandler",
                Code = functionProps.Code,
                Timeout = functionProps.Timeout,
                MemorySize = functionProps.MemorySize,
                Environment = functionProps.Environment
            });

            // Create Lambda function for initializing default leads (POST /leads/init)
            InitLeadsFunction = new Function(this, "InitLeadsFunction", new FunctionProps
            {
                FunctionName = $"{props.DomainName.Replace(".", "-")}-init-leads",
                Runtime = functionProps.Runtime,
                Handler = "Lambda::Lambda.Functions.InitLeadsFunction::FunctionHandler",
                Code = functionProps.Code,
                Timeout = functionProps.Timeout,
                MemorySize = functionProps.MemorySize,
                Environment = functionProps.Environment
            });

            // Grant DynamoDB permissions to all Lambda functions
            // PutItem, GetItem, Query, UpdateItem, DeleteItem
            var functions = new[] 
            { 
                CreateLeadFunction, 
                ListLeadsFunction, 
                GetLeadFunction, 
                UpdateLeadFunction, 
                DeleteLeadFunction, 
                InitLeadsFunction 
            };

            foreach (var function in functions)
            {
                props.DynamoDbTable.GrantReadWriteData(function);
            }

            // Add CloudFormation outputs for Lambda function ARNs
            _ = new CfnOutput(this, "CreateLeadFunctionArn", new CfnOutputProps
            {
                Value = CreateLeadFunction.FunctionArn,
                Description = "Create Lead Lambda Function ARN",
                ExportName = $"{props.Name}-CreateLeadFunctionArn"
            });

            _ = new CfnOutput(this, "ListLeadsFunctionArn", new CfnOutputProps
            {
                Value = ListLeadsFunction.FunctionArn,
                Description = "List Leads Lambda Function ARN",
                ExportName = $"{props.Name}-ListLeadsFunctionArn"
            });

            _ = new CfnOutput(this, "GetLeadFunctionArn", new CfnOutputProps
            {
                Value = GetLeadFunction.FunctionArn,
                Description = "Get Lead Lambda Function ARN",
                ExportName = $"{props.Name}-GetLeadFunctionArn"
            });

            _ = new CfnOutput(this, "UpdateLeadFunctionArn", new CfnOutputProps
            {
                Value = UpdateLeadFunction.FunctionArn,
                Description = "Update Lead Lambda Function ARN",
                ExportName = $"{props.Name}-UpdateLeadFunctionArn"
            });

            _ = new CfnOutput(this, "DeleteLeadFunctionArn", new CfnOutputProps
            {
                Value = DeleteLeadFunction.FunctionArn,
                Description = "Delete Lead Lambda Function ARN",
                ExportName = $"{props.Name}-DeleteLeadFunctionArn"
            });

            _ = new CfnOutput(this, "InitLeadsFunctionArn", new CfnOutputProps
            {
                Value = InitLeadsFunction.FunctionArn,
                Description = "Init Leads Lambda Function ARN",
                ExportName = $"{props.Name}-InitLeadsFunctionArn"
            });
        }
    }
}