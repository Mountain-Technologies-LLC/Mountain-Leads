# .NET 10 Release Guide

This document covers upgrading Mountain Leads from .NET 8 to .NET 10 and releasing the updated application to AWS.

## What Changed

| Area | Before | After |
|------|--------|-------|
| Lambda runtime | `Runtime.DOTNET_8` (dotnet8) | `Runtime.DOTNET_10` (dotnet10) |
| All `TargetFramework` entries | `net8.0` | `net10.0` |
| Build output paths | `…/net8.0/publish` | `…/net10.0/publish` |
| WebAssembly packages | `8.0.0` | `10.0.0` |
| `Microsoft.Extensions.*` packages | `8.0.0` | `10.0.0` |
| `Microsoft.JSInterop` | `8.0.0` | `10.0.0` |

### Files Modified

**Source / Infrastructure**
- `infrastructure/src/Lambda/Lambda.csproj`
- `infrastructure/src/Infrastructure/Infrastructure.csproj`
- `infrastructure/src/Infrastructure/Constructs/LambdaConstruct.cs`
- `infrastructure/src/Infrastructure/Constructs/BucketDeploymentConstruct.cs`
- `infrastructure/src/Infrastructure/Program.cs`
- `infrastructure/src/Infrastructure.Tests/ApiGatewayConstructTests.cs`
- `website/website.csproj`
- `website.Tests/website.Tests.csproj`
- `package.json`

**Documentation / Steering**
- `README.md`
- `scripts/README.md`
- `Documents/DEPLOYMENT_CHECKPOINT.md`
- `.kiro/steering/tech.md`
- `.kiro/specs/mountain-leads-app/tasks.md`
- `.kiro/specs/mountain-leads-app/design.md`

---

## Prerequisites

Before deploying, confirm the following are installed and configured on the machine running the release:

```bash
# Verify .NET 10 SDK
dotnet --version  # Must be 10.0.x

# Verify Node.js
node --version    # Must be 18+

# Verify AWS CLI
aws --version

# Verify AWS credentials are active
aws sts get-caller-identity

# Verify CDK CLI
cdk --version     # 2.x recommended
```

If .NET 10 SDK is not installed, download it from https://dotnet.microsoft.com/download/dotnet/10.0.

---

## Step 1: Run All Tests Locally

Confirm the codebase is healthy before touching AWS.

```bash
# Lambda function tests
dotnet test infrastructure/src/Lambda.Tests/Lambda.Tests.csproj

# Website (Blazor) tests
dotnet test website.Tests/website.Tests.csproj

# Infrastructure (CDK construct) tests
dotnet test infrastructure/src/Infrastructure.Tests/Infrastructure.Tests.csproj
```

All tests must pass before proceeding. Do not deploy with failing tests.

---

## Step 2: Build All Artifacts

```bash
npm run build:all
```

This runs two sub-commands in sequence:

1. **`build:lambda`** — publishes Lambda functions to `infrastructure/src/Lambda/bin/Release/net10.0/publish` targeting `linux-x64`.
2. **`build:website`** — publishes the Blazor WebAssembly app to `website/bin/Release/net10.0/publish`.

Verify the output directories exist after the build:

```bash
ls infrastructure/src/Lambda/bin/Release/net10.0/publish
ls website/bin/Release/net10.0/publish/wwwroot
```

---

## Step 3: Preview Infrastructure Changes

Before deploying, review what CDK will change. This is especially important for the Lambda runtime upgrade — CDK will update the runtime on every existing Lambda function in the stack.

```bash
npm run diff
```

Expected changes you should see:

- All six Lambda functions updated: `Runtime` changes from `dotnet8` to `dotnet10`
- No DynamoDB, Cognito, API Gateway, S3, or CloudFront changes (this upgrade only touches Lambda runtime)

If the diff shows unexpected resource replacements (e.g., DynamoDB table recreation), stop and investigate before proceeding.

---

## Step 4: Deploy Infrastructure

```bash
npm run deploy:infra
```

This deploys the CDK stack with `--require-approval never`. CDK will perform an in-place update on the Lambda functions, swapping the runtime from `dotnet8` to `dotnet10`. Existing function configurations, environment variables, IAM permissions, and API Gateway integrations are preserved.

The deployment typically takes 3–6 minutes. Watch for any `UPDATE_FAILED` events in the CloudFormation console if the command exits with an error.

---

## Step 5: Verify Lambda Runtime in AWS Console

After deployment completes, spot-check the Lambda runtime upgrade:

1. Open **AWS Console → Lambda → Functions**
2. Open any of the six functions (e.g., `leads-mountaintechnologiesllc-com-create-lead`)
3. Under **Runtime settings**, confirm **Runtime** is `dotnet10`
4. Repeat for all six functions:
   - `leads-mountaintechnologiesllc-com-create-lead`
   - `leads-mountaintechnologiesllc-com-list-leads`
   - `leads-mountaintechnologiesllc-com-get-lead`
   - `leads-mountaintechnologiesllc-com-update-lead`
   - `leads-mountaintechnologiesllc-com-delete-lead`
   - `leads-mountaintechnologiesllc-com-init-leads`

Alternatively, verify via CLI:

```bash
aws lambda get-function-configuration \
  --function-name leads-mountaintechnologiesllc-com-create-lead \
  --query 'Runtime'
# Expected output: "dotnet10"
```

---

## Step 6: Update Blazor Configuration

If this is a first-time deployment or CDK outputs changed, refresh the Blazor app configuration:

```bash
npm run update-config
```

This fetches the live CloudFormation stack outputs (Cognito User Pool ID, Client ID, API Gateway URL) and writes them to `website/wwwroot/appsettings.json`.

Skip this step on subsequent deployments when only the Lambda runtime changed and no infrastructure outputs were modified.

---

## Step 7: Rebuild and Deploy the Website

After configuration is updated, rebuild the website and push it to S3:

```bash
npm run build:website
npm run deploy:infra
```

The second `deploy:infra` uploads the freshly built Blazor files from `website/bin/Release/net10.0/publish/wwwroot` to the S3 bucket and invalidates the CloudFront cache.

### Or run the full sequence in one command:

```bash
npm run deploy:full
```

`deploy:full` runs: `deploy:infra` → `update-config` → `build:website` → `deploy:infra`.

---

## Step 8: Smoke Test the Deployed Application

Once CloudFront propagates (allow 2–5 minutes), run a basic smoke test against production:

```bash
# Get your API Gateway URL from CDK outputs or CloudFormation
API_URL=$(aws cloudformation describe-stacks \
  --stack-name Infrastructure-Stack-Id-leadsmountaintechnologiesllccom \
  --query 'Stacks[0].Outputs[?OutputKey==`ApiGatewayUrl`].OutputValue' \
  --output text)

# Unauthenticated request should return 401
curl -s -o /dev/null -w "%{http_code}" "$API_URL/leads"
# Expected: 401
```

Then open the CloudFront URL in a browser and verify:

- [ ] Login page loads
- [ ] Registration works (or existing user can log in)
- [ ] Dashboard loads and shows leads
- [ ] Create, edit, and delete a lead successfully

For a full test matrix see `Documents/END_TO_END_TESTING_GUIDE.md`.

---

## Rollback Procedure

If the deployment fails or the application is broken after upgrading, roll back by reverting the Lambda runtime to `dotnet8`.

### Option A: Redeploy from the previous git commit

```bash
git checkout <previous-commit-sha>
npm run deploy:infra
```

### Option B: Update Lambda runtime directly via CLI (faster)

```bash
for fn in create-lead list-leads get-lead update-lead delete-lead init-leads; do
  aws lambda update-function-configuration \
    --function-name "leads-mountaintechnologiesllc-com-$fn" \
    --runtime dotnet8
done
```

Note: Option B only reverts the runtime identifier. You must also redeploy the `dotnet8`-compiled function code (from the previous build artifact) for a complete rollback.

---

## Troubleshooting

### `The folder './website/bin/Release/net10.0/publish/wwwroot' does not exist`

The CDK pre-flight check runs before synthesis. Run `npm run build:website` first, then retry the deploy command.

### Lambda function returns `Init` errors in CloudWatch

This usually means the compiled binary targets the wrong runtime. Confirm `Lambda.csproj` has `<TargetFramework>net10.0</TargetFramework>` and re-run `npm run build:lambda` followed by `npm run deploy:infra`.

### `dotnet: command not found` during build

The .NET 10 SDK is not installed or not on PATH. Install from https://dotnet.microsoft.com/download/dotnet/10.0 and open a new terminal session.

### NuGet restore fails for `Microsoft.AspNetCore.Components.WebAssembly 10.0.0`

Ensure the NuGet feed includes the `nuget.org` source. Run:

```bash
dotnet nuget list source
# Should show https://api.nuget.org/v3/index.json as Enabled
```

If missing, add it:

```bash
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
```

---

## Reference

- [.NET 10 on AWS Lambda announcement](https://aws.amazon.com/blogs/compute/net-10-runtime-now-available-in-aws-lambda/)
- [AWS CDK Runtime.DOTNET_10 API docs](https://docs.aws.amazon.com/cdk/api/v2/dotnet/api/Amazon.CDK.AWS.Lambda.Runtime.html)
- [.NET 10 SDK download](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Mountain Leads deployment checkpoint](./DEPLOYMENT_CHECKPOINT.md)
- [End-to-end testing guide](./END_TO_END_TESTING_GUIDE.md)
