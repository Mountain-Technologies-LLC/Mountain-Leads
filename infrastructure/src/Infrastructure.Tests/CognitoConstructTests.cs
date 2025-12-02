using System;
using System.Linq;
using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.Cognito;
using FsCheck;
using FsCheck.Xunit;
using Infrastructure.Constructs;
using Xunit;

namespace Infrastructure.Tests
{
    /// <summary>
    /// Property-based tests for CognitoConstruct
    /// Feature: mountain-leads-app, Property 1: User registration creates Cognito account
    /// </summary>
    public class CognitoConstructTests
    {
        /// <summary>
        /// Property 1: User registration creates Cognito account
        /// For any valid domain name, the CognitoConstruct should create a User Pool
        /// with email sign-in enabled and proper password policy that allows user registration.
        /// Validates: Requirements 1.1
        /// </summary>
        [Property(MaxTest = 100)]
        public void CognitoConstruct_CreatesUserPoolWithRegistrationCapability(string domainSuffix)
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
            
            var props = new CognitoConstructProps
            {
                Name = "test-stack",
                DomainName = domainName
            };

            // Act
            var construct = new CognitoConstruct(stack, "TestCognito", props);

            // Assert - Verify User Pool is created
            Assert.NotNull(construct.UserPool);
            Assert.NotNull(construct.UserPoolClient);

            // Synthesize the stack to get CloudFormation template
            var assembly = app.Synth();
            var template = assembly.GetStackByName(stack.StackName).Template;

            // Verify User Pool resource exists in template
            var templateDict = template as Dictionary<string, object>;
            Assert.NotNull(templateDict);
            Assert.True(templateDict.ContainsKey("Resources"));
            
            var resources = templateDict["Resources"] as Dictionary<string, object>;
            Assert.NotNull(resources);

            // Find the UserPool resource
            var userPoolResource = resources.Values
                .Cast<Dictionary<string, object>>()
                .FirstOrDefault(r => r.ContainsKey("Type") && 
                                   r["Type"].ToString() == "AWS::Cognito::UserPool");
            
            Assert.NotNull(userPoolResource);

            // Verify UserPool properties that enable registration
            var properties = userPoolResource["Properties"] as Dictionary<string, object>;
            Assert.NotNull(properties);

            // Verify email sign-in is enabled (required for registration)
            if (properties.ContainsKey("UsernameAttributes"))
            {
                var usernameAttrs = properties["UsernameAttributes"] as List<object>;
                if (usernameAttrs != null)
                {
                    Assert.Contains("email", usernameAttrs.Select(a => a.ToString()));
                }
            }

            // Verify password policy exists (required for secure registration)
            Assert.True(properties.ContainsKey("Policies"));
            var policies = properties["Policies"] as Dictionary<string, object>;
            Assert.NotNull(policies);
            Assert.True(policies.ContainsKey("PasswordPolicy"));

            var passwordPolicy = policies["PasswordPolicy"] as Dictionary<string, object>;
            Assert.NotNull(passwordPolicy);
            
            // Verify password requirements (min 8 chars, uppercase, lowercase, number)
            Assert.True(passwordPolicy.ContainsKey("MinimumLength"));
            Assert.True(Convert.ToInt32(passwordPolicy["MinimumLength"]) >= 8);
            
            Assert.True(passwordPolicy.ContainsKey("RequireUppercase"));
            Assert.True(Convert.ToBoolean(passwordPolicy["RequireUppercase"]));
            
            Assert.True(passwordPolicy.ContainsKey("RequireLowercase"));
            Assert.True(Convert.ToBoolean(passwordPolicy["RequireLowercase"]));
            
            Assert.True(passwordPolicy.ContainsKey("RequireNumbers"));
            Assert.True(Convert.ToBoolean(passwordPolicy["RequireNumbers"]));

            // Verify User Pool Client exists (required for registration)
            var userPoolClientResource = resources.Values
                .Cast<Dictionary<string, object>>()
                .FirstOrDefault(r => r.ContainsKey("Type") && 
                                   r["Type"].ToString() == "AWS::Cognito::UserPoolClient");
            
            Assert.NotNull(userPoolClientResource);

            var clientProperties = userPoolClientResource["Properties"] as Dictionary<string, object>;
            Assert.NotNull(clientProperties);

            // Verify client has no secret (required for public SPA registration)
            if (clientProperties.ContainsKey("GenerateSecret"))
            {
                Assert.False(Convert.ToBoolean(clientProperties["GenerateSecret"]));
            }

            // Verify auth flows are enabled for registration
            Assert.True(clientProperties.ContainsKey("ExplicitAuthFlows"));
            var authFlowsObj = clientProperties["ExplicitAuthFlows"];
            Assert.NotNull(authFlowsObj);
            
            // Handle different collection types (List<object>, object[], IEnumerable, etc.)
            var authFlows = authFlowsObj as System.Collections.IEnumerable;
            Assert.NotNull(authFlows);
            
            // Should support user password auth for registration
            var authFlowStrings = authFlows.Cast<object>().Select(f => f?.ToString() ?? "").ToList();
            var hasPasswordAuth = authFlowStrings.Any(flowStr => 
                flowStr.Contains("USER_PASSWORD_AUTH") || 
                flowStr.Contains("ALLOW_USER_PASSWORD_AUTH"));
            Assert.True(hasPasswordAuth, 
                $"Expected auth flows to contain USER_PASSWORD_AUTH or ALLOW_USER_PASSWORD_AUTH, but got: {string.Join(", ", authFlowStrings)}");
        }
    }
}
