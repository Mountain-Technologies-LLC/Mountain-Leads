using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.Lambda;
using Constructs;
using System.Collections.Generic;

namespace Infrastructure.Constructs
{
    public class ApiGatewayConstructProps : IStackProps
    {
        public string Name;
        public string DomainName;
        public UserPool UserPool;
        public Function CreateLeadFunction;
        public Function ListLeadsFunction;
        public Function GetLeadFunction;
        public Function UpdateLeadFunction;
        public Function DeleteLeadFunction;
        public Function InitLeadsFunction;
    }

    public class ApiGatewayConstruct : Construct
    {
        public RestApi RestApi;
        public CognitoUserPoolsAuthorizer Authorizer;

        public ApiGatewayConstruct(Construct scope, string id, ApiGatewayConstructProps props = null) : base(scope, id)
        {
            // Create REST API with name based on domain
            var apiName = $"{props.DomainName.Replace(".", "-")}-api";

            RestApi = new RestApi(this, "RestApi", new RestApiProps
            {
                RestApiName = apiName,
                Description = $"API Gateway for {props.DomainName}",
                
                // Deploy to prod stage
                DeployOptions = new StageOptions
                {
                    StageName = "prod",
                    ThrottlingRateLimit = 100,
                    ThrottlingBurstLimit = 200
                },
                
                // Configure CORS - Allow all origins for development
                // In production, restrict to specific CloudFront distribution
                DefaultCorsPreflightOptions = new CorsOptions
                {
                    AllowOrigins = Cors.ALL_ORIGINS,
                    AllowMethods = Cors.ALL_METHODS,
                    AllowHeaders = new[]
                    {
                        "Content-Type",
                        "X-Amz-Date",
                        "Authorization",
                        "X-Api-Key",
                        "X-Amz-Security-Token"
                    },
                    AllowCredentials = false // Must be false when using ALL_ORIGINS
                },
                
                // CloudWatch logging
                CloudWatchRole = true
            });

            // Configure Cognito User Pool authorizer
            Authorizer = new CognitoUserPoolsAuthorizer(this, "CognitoAuthorizer", new CognitoUserPoolsAuthorizerProps
            {
                CognitoUserPools = new[] { props.UserPool },
                AuthorizerName = $"{apiName}-authorizer",
                IdentitySource = "method.request.header.Authorization"
            });

            // Create /leads resource
            var leadsResource = RestApi.Root.AddResource("leads");

            // POST /leads - Create lead
            leadsResource.AddMethod("POST", new LambdaIntegration(props.CreateLeadFunction), new MethodOptions
            {
                Authorizer = Authorizer,
                AuthorizationType = AuthorizationType.COGNITO
            });

            // GET /leads - List all leads for user
            leadsResource.AddMethod("GET", new LambdaIntegration(props.ListLeadsFunction), new MethodOptions
            {
                Authorizer = Authorizer,
                AuthorizationType = AuthorizationType.COGNITO
            });

            // Create /leads/init resource for initializing default leads
            var initResource = leadsResource.AddResource("init");
            
            // POST /leads/init - Initialize default leads
            initResource.AddMethod("POST", new LambdaIntegration(props.InitLeadsFunction), new MethodOptions
            {
                Authorizer = Authorizer,
                AuthorizationType = AuthorizationType.COGNITO
            });

            // Create /leads/{leadId} resource
            var leadIdResource = leadsResource.AddResource("{leadId}");

            // GET /leads/{leadId} - Get specific lead
            leadIdResource.AddMethod("GET", new LambdaIntegration(props.GetLeadFunction), new MethodOptions
            {
                Authorizer = Authorizer,
                AuthorizationType = AuthorizationType.COGNITO
            });

            // PUT /leads/{leadId} - Update lead
            leadIdResource.AddMethod("PUT", new LambdaIntegration(props.UpdateLeadFunction), new MethodOptions
            {
                Authorizer = Authorizer,
                AuthorizationType = AuthorizationType.COGNITO
            });

            // DELETE /leads/{leadId} - Delete lead
            leadIdResource.AddMethod("DELETE", new LambdaIntegration(props.DeleteLeadFunction), new MethodOptions
            {
                Authorizer = Authorizer,
                AuthorizationType = AuthorizationType.COGNITO
            });

            // Add CloudFormation output for API Gateway URL
            _ = new CfnOutput(this, "ApiGatewayUrl", new CfnOutputProps
            {
                Value = RestApi.Url,
                Description = "API Gateway URL",
                ExportName = $"{props.Name}-ApiGatewayUrl"
            });
        }
    }
}
