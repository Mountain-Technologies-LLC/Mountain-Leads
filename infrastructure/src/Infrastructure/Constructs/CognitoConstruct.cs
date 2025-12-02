using Amazon.CDK;
using Amazon.CDK.AWS.Cognito;
using Constructs;

namespace Infrastructure.Constructs
{
    public class CognitoConstructProps : IStackProps
    {
        public string Name;
        public string DomainName;
    }

    public class CognitoConstruct : Construct
    {
        public UserPool UserPool;
        public UserPoolClient UserPoolClient;

        public CognitoConstruct(Construct scope, string id, CognitoConstructProps props = null) : base(scope, id)
        {
            // Create user pool name based on domain: leads-mountaintechnologiesllc-com-users
            var userPoolName = $"{props.DomainName.Replace(".", "-")}-users";

            // Define Cognito User Pool with email sign-in
            UserPool = new UserPool(this, "UserPool", new UserPoolProps
            {
                UserPoolName = userPoolName,
                
                // Enable self-sign-up
                SelfSignUpEnabled = true,
                
                // Email sign-in configuration
                SignInAliases = new SignInAliases
                {
                    Email = true,
                    Username = false
                },
                
                // Auto-verify email
                AutoVerify = new AutoVerifiedAttrs
                {
                    Email = true
                },
                
                // Configure password policy (min 8 chars, uppercase, lowercase, number)
                PasswordPolicy = new PasswordPolicy
                {
                    MinLength = 8,
                    RequireUppercase = true,
                    RequireLowercase = true,
                    RequireDigits = true,
                    RequireSymbols = false
                },
                
                // Standard attributes
                StandardAttributes = new StandardAttributes
                {
                    Email = new StandardAttribute
                    {
                        Required = true,
                        Mutable = true
                    }
                },
                
                // Account recovery
                AccountRecovery = AccountRecovery.EMAIL_ONLY,
                
                // Removal policy for development
                RemovalPolicy = RemovalPolicy.DESTROY
            });

            // Create User Pool Client for Blazor app (no client secret for public SPA)
            UserPoolClient = new UserPoolClient(this, "UserPoolClient", new UserPoolClientProps
            {
                UserPool = UserPool,
                UserPoolClientName = $"{userPoolName}-client",
                
                // No client secret for public Blazor WebAssembly app
                GenerateSecret = false,
                
                // Auth flows for direct Cognito SDK authentication
                AuthFlows = new AuthFlow
                {
                    UserPassword = true,
                    UserSrp = true
                },
                
                // Prevent user existence errors
                PreventUserExistenceErrors = true
            });

            // Add CloudFormation outputs for User Pool ID and Client ID
            _ = new CfnOutput(this, "UserPoolId", new CfnOutputProps
            {
                Value = UserPool.UserPoolId,
                Description = "Cognito User Pool ID",
                ExportName = $"{props.Name}-UserPoolId"
            });

            _ = new CfnOutput(this, "UserPoolClientId", new CfnOutputProps
            {
                Value = UserPoolClient.UserPoolClientId,
                Description = "Cognito User Pool Client ID",
                ExportName = $"{props.Name}-UserPoolClientId"
            });
        }
    }
}
