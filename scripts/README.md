# Build and Deploy Scripts

This directory contains scripts for building and deploying the Mountain Leads application.

## Available NPM Scripts

### Build Scripts

- **`npm run build:lambda`** - Builds the Lambda functions for deployment
  - Publishes the Lambda project to `infrastructure/src/Lambda/bin/Release/net8.0/publish`
  - Uses .NET 8 with linux-x64 runtime

- **`npm run build:website`** - Builds the Blazor WebAssembly application
  - Publishes the website project to `website/bin/Release/net8.0/publish`
  - Produces static files ready for S3 deployment

- **`npm run build:all`** - Builds both Lambda functions and Blazor website
  - Runs `build:lambda` and `build:website` in sequence

### Deploy Scripts

- **`npm run deploy:infra`** - Deploys CDK infrastructure to AWS
  - Builds all components first
  - Deploys the CDK stack without requiring approval
  - Creates/updates: Cognito User Pool, DynamoDB table, API Gateway, Lambda functions, S3 bucket, CloudFront distribution

- **`npm run deploy`** - Deploys CDK infrastructure with approval prompt
  - Same as `deploy:infra` but requires manual approval for changes

- **`npm run update-config`** - Updates Blazor configuration with CDK outputs
  - Fetches stack outputs from CloudFormation
  - Updates `website/wwwroot/appsettings.json` with:
    - Cognito User Pool ID
    - Cognito Client ID
    - API Gateway URL
  - Requires AWS credentials to be configured

- **`npm run deploy:full`** - Complete build and deploy workflow
  - Deploys infrastructure
  - Updates configuration with CDK outputs
  - Rebuilds website with updated configuration
  - Redeploys infrastructure to upload new website files
  - This is the recommended script for full deployments

### Other Scripts

- **`npm run synth`** - Synthesizes CloudFormation template
  - Useful for reviewing infrastructure changes before deployment

- **`npm run diff`** - Shows differences between deployed and local infrastructure
  - Useful for reviewing what will change on deployment

- **`npm run destroy`** - Destroys the CDK stack
  - Removes all AWS resources (use with caution!)

## Configuration Update Script

The `update-config.js` script automates the process of updating the Blazor application configuration with values from the deployed CDK stack.

### How it works:

1. Connects to AWS CloudFormation using the AWS SDK
2. Retrieves outputs from the `InfrastructureStack`
3. Reads the current `website/wwwroot/appsettings.json`
4. Updates the AWS configuration section with:
   - `UserPoolId` - Cognito User Pool ID
   - `ClientId` - Cognito User Pool Client ID
   - `ApiGatewayUrl` - API Gateway endpoint URL
5. Writes the updated configuration back to the file

### Prerequisites:

- AWS credentials configured (via `aws configure` or environment variables)
- CDK stack must be deployed first
- Node.js and npm installed

### Environment Variables:

- `AWS_REGION` - AWS region (defaults to `us-east-1`)

## Deployment Workflow

### First-time deployment:

```bash
# 1. Install dependencies
npm install

# 2. Deploy infrastructure
npm run deploy:infra

# 3. Update configuration with CDK outputs
npm run update-config

# 4. Rebuild and redeploy with updated config
npm run build:website
npm run deploy:infra
```

### Or use the full deployment script:

```bash
npm install
npm run deploy:full
```

### Subsequent deployments:

```bash
# For infrastructure changes only
npm run deploy

# For full rebuild and redeploy
npm run deploy:full
```

## Requirements

- .NET 8 SDK
- Node.js 18+
- AWS CDK CLI (`npm install -g aws-cdk`)
- AWS credentials configured
- AWS account with appropriate permissions

## Troubleshooting

### "Stack not found" error

If you get a "Stack not found" error when running `update-config`, make sure you've deployed the infrastructure first:

```bash
npm run deploy:infra
```

### Configuration not updating

Make sure your AWS credentials have permission to read CloudFormation stack outputs:

```json
{
  "Effect": "Allow",
  "Action": [
    "cloudformation:DescribeStacks"
  ],
  "Resource": "*"
}
```

### Build failures

If builds fail, ensure you have the correct .NET SDK version installed:

```bash
dotnet --version  # Should be 8.0.x
```
