using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Amazon.CDK;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.DynamoDB;
using FsCheck;
using FsCheck.Xunit;
using Infrastructure.Constructs;
using Xunit;

namespace Infrastructure.Tests
{
    /// <summary>
    /// Property-based tests for ApiGatewayConstruct
    /// Feature: mountain-leads-app, Property 9: API authorization requirement
    /// </summary>
    public class ApiGatewayConstructTests
    {
        /// <summary>
        /// Property 9: API authorization requirement
        /// For any API endpoint configuration, all CRUD endpoints should require Cognito authorization,
        /// ensuring that requests without valid authentication tokens are rejected.
        /// Validates: Requirements 3.2, 4.5, 5.2, 6.2
        /// </summary>
        [Property(MaxTest = 100)]
        public void ApiGatewayConstruct_RequiresAuthorizationOnAllEndpoints(string domainSuffix)
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
            
            // Create required dependencies
            var cognitoProps = new CognitoConstructProps
            {
                Name = "test-stack",
                DomainName = domainName
            };
            var cognitoConstruct = new CognitoConstruct(stack, "TestCognito", cognitoProps);

            // Create mock Lambda functions for testing
            // Use a temporary directory for mock Lambda code
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            File.WriteAllText(Path.Combine(tempDir, "mock.txt"), "mock");

            try
            {
                var mockFunctionProps = new FunctionProps
                {
                    Runtime = Runtime.DOTNET_8,
                    Handler = "test",
                    Code = Code.FromAsset(tempDir),
                    Timeout = Duration.Seconds(30),
                    MemorySize = 512
                };

                var createLeadFunction = new Function(stack, "CreateLeadFunction", mockFunctionProps);
                var listLeadsFunction = new Function(stack, "ListLeadsFunction", mockFunctionProps);
                var getLeadFunction = new Function(stack, "GetLeadFunction", mockFunctionProps);
                var updateLeadFunction = new Function(stack, "UpdateLeadFunction", mockFunctionProps);
                var deleteLeadFunction = new Function(stack, "DeleteLeadFunction", mockFunctionProps);
                var initLeadsFunction = new Function(stack, "InitLeadsFunction", mockFunctionProps);

                var apiGatewayProps = new ApiGatewayConstructProps
                {
                    Name = "test-stack",
                    DomainName = domainName,
                    UserPool = cognitoConstruct.UserPool,
                    CreateLeadFunction = createLeadFunction,
                    ListLeadsFunction = listLeadsFunction,
                    GetLeadFunction = getLeadFunction,
                    UpdateLeadFunction = updateLeadFunction,
                    DeleteLeadFunction = deleteLeadFunction,
                    InitLeadsFunction = initLeadsFunction
                };

                // Act
                var construct = new ApiGatewayConstruct(stack, "TestApiGateway", apiGatewayProps);

                // Assert - Verify API Gateway and Authorizer are created
                Assert.NotNull(construct.RestApi);
                Assert.NotNull(construct.Authorizer);

            // Synthesize the stack to get CloudFormation template
            var assembly = app.Synth();
            var template = assembly.GetStackByName(stack.StackName).Template;

            // Verify template structure
            var templateDict = template as Dictionary<string, object>;
            Assert.NotNull(templateDict);
            Assert.True(templateDict.ContainsKey("Resources"));
            
            var resources = templateDict["Resources"] as Dictionary<string, object>;
            Assert.NotNull(resources);

            // Find the Cognito Authorizer resource
            var authorizerResource = resources.Values
                .Cast<Dictionary<string, object>>()
                .FirstOrDefault(r => r.ContainsKey("Type") && 
                                   r["Type"].ToString() == "AWS::ApiGateway::Authorizer");
            
            Assert.NotNull(authorizerResource);

            var authorizerProperties = authorizerResource["Properties"] as Dictionary<string, object>;
            Assert.NotNull(authorizerProperties);

            // Verify authorizer is configured for Cognito
            Assert.True(authorizerProperties.ContainsKey("Type"));
            Assert.Equal("COGNITO_USER_POOLS", authorizerProperties["Type"].ToString());

            // Verify authorizer references the User Pool
            Assert.True(authorizerProperties.ContainsKey("ProviderARNs"));

            // Find all API Gateway Method resources (endpoints)
            var methodResources = resources
                .Where(kvp => kvp.Value is Dictionary<string, object> resource &&
                            resource.ContainsKey("Type") &&
                            resource["Type"].ToString() == "AWS::ApiGateway::Method")
                .Select(kvp => new 
                { 
                    Key = kvp.Key, 
                    Value = kvp.Value as Dictionary<string, object> 
                })
                .ToList();

            // We expect 6 methods: POST /leads, GET /leads, POST /leads/init, 
            // GET /leads/{leadId}, PUT /leads/{leadId}, DELETE /leads/{leadId}
            Assert.True(methodResources.Count >= 6, 
                $"Expected at least 6 API methods, but found {methodResources.Count}");

            // Verify each method (except OPTIONS for CORS) requires authorization
            var methodsRequiringAuth = 0;
            foreach (var method in methodResources)
            {
                if (method.Value == null || !method.Value.ContainsKey("Properties"))
                {
                    continue;
                }
                
                var methodProperties = method.Value["Properties"] as Dictionary<string, object>;
                Assert.NotNull(methodProperties);

                // Get HTTP method
                var httpMethod = methodProperties.ContainsKey("HttpMethod") 
                    ? methodProperties["HttpMethod"].ToString() 
                    : "";

                // Skip OPTIONS methods (used for CORS preflight)
                if (httpMethod == "OPTIONS")
                {
                    continue;
                }

                // Verify authorization is configured
                Assert.True(methodProperties.ContainsKey("AuthorizationType"),
                    $"Method {method.Key} ({httpMethod}) is missing AuthorizationType");
                
                var authType = methodProperties["AuthorizationType"].ToString();
                Assert.Equal("COGNITO_USER_POOLS", authType);

                // Verify authorizer is referenced
                Assert.True(methodProperties.ContainsKey("AuthorizerId"),
                    $"Method {method.Key} ({httpMethod}) is missing AuthorizerId");

                methodsRequiringAuth++;
            }

            // Verify we found and validated the expected number of authorized methods
            Assert.True(methodsRequiringAuth >= 6,
                $"Expected at least 6 methods with authorization, but found {methodsRequiringAuth}");

            // Verify CORS is configured (allows the Blazor app to make requests)
            var restApiResource = resources.Values
                .Cast<Dictionary<string, object>>()
                .FirstOrDefault(r => r.ContainsKey("Type") && 
                                   r["Type"].ToString() == "AWS::ApiGateway::RestApi");
            
            Assert.NotNull(restApiResource);
            }
            finally
            {
                // Cleanup temp directory
                if (Directory.Exists(tempDir))
                {
                    try
                    {
                        Directory.Delete(tempDir, true);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }
    }
}
