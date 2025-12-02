# Technology Stack

## Frontend
- **.NET 8 Blazor WebAssembly**: Client-side SPA framework
- **C# with nullable reference types enabled**
- **AWS SDK for .NET**: Cognito authentication, DynamoDB client
- **System.IdentityModel.Tokens.Jwt**: JWT token handling

## Backend
- **AWS Lambda (.NET 8)**: Serverless compute for API endpoints
- **AWS API Gateway**: REST API with Cognito authorizer
- **Amazon DynamoDB**: NoSQL database (PAY_PER_REQUEST billing)
- **Amazon Cognito**: User authentication and authorization

## Infrastructure
- **AWS CDK with C#**: Infrastructure as code
- **Amazon S3**: Static website hosting
- **Amazon CloudFront**: CDN for global distribution
- **Node.js/NPM**: Build orchestration and deployment scripts

## Key Libraries

### Infrastructure (CDK)
- `Amazon.CDK.Lib` (v2.180.0)
- `Constructs` (v10.x)

### Lambda Functions
- `Amazon.Lambda.Core` (v2.2.0)
- `Amazon.Lambda.APIGatewayEvents` (v2.7.0)
- `AWSSDK.DynamoDBv2` (v3.7.400)
- `System.IdentityModel.Tokens.Jwt` (v8.2.1)

### Blazor Website
- `Microsoft.AspNetCore.Components.WebAssembly` (v8.0.0)
- `Amazon.Extensions.CognitoAuthentication` (v3.1.2)
- `AWSSDK.DynamoDBv2` (v4.0.10.1)
- `System.IdentityModel.Tokens.Jwt` (v8.0.0)

## Common Commands

### Local Development
```bash
# Start local development server (Blazor)
npm run start

# Build website locally
npm run build
```

### Building Components
```bash
# Build Lambda functions for deployment
npm run build:lambda

# Build Blazor website for deployment
npm run build:website

# Build everything
npm run build:all
```

### AWS Deployment
```bash
# Synthesize CloudFormation template
npm run synth

# Preview infrastructure changes
npm run diff

# Deploy infrastructure (with approval prompts)
npm run deploy

# Deploy infrastructure (no approval required)
npm run deploy:infra

# Full deployment (infra + config update + website)
npm run deploy:full

# Destroy all AWS resources
npm run destroy
```

### Testing
```bash
# Run Lambda tests
dotnet test infrastructure/src/Lambda.Tests/Lambda.Tests.csproj

# Run website tests
dotnet test website.Tests/website.Tests.csproj

# Run infrastructure tests
dotnet test infrastructure/src/Infrastructure.Tests/Infrastructure.Tests.csproj
```

### Configuration
```bash
# Update website config with deployed AWS resource values
npm run update-config
```

## Prerequisites

- .NET 8 SDK
- Node.js 18+
- AWS CLI configured with credentials (`aws configure`)
- AWS CDK CLI (`npm install -g aws-cdk`)
- Valid AWS account with appropriate IAM permissions

## Build System

The project uses NPM scripts to orchestrate .NET builds and AWS CDK deployments. The CDK context parameter `name=leads.mountaintechnologiesllc.com` is used throughout to configure resource naming based on the domain.
