# Testing User Registration

## Prerequisites

Before you can test user registration, you need to:

1. **Deploy the CDK Infrastructure** (Task 18)
   ```bash
   cd infrastructure/src
   cdk deploy
   ```

2. **Capture the CDK Outputs**
   After deployment, CDK will output:
   - Cognito User Pool ID
   - Cognito Client ID
   - API Gateway URL

3. **Update appsettings.json** (Task 19)
   Update `website/wwwroot/appsettings.json` with the real values:
   ```json
   {
     "AWS": {
       "Region": "us-east-1",
       "UserPoolId": "us-east-1_XXXXXXXXX",
       "ClientId": "XXXXXXXXXXXXXXXXXXXXXXXXXX",
       "ApiGatewayUrl": "https://XXXXXXXXXX.execute-api.us-east-1.amazonaws.com/prod"
     }
   }
   ```

## Testing Registration

### 1. Build and Run the Blazor App

```bash
cd website
dotnet run
```

The app will be available at `http://localhost:5000` (or the port shown in the console).

### 2. Navigate to Registration Page

Open your browser and go to: `http://localhost:5000/register`

### 3. Test Registration

**Valid Registration:**
- Email: `test@example.com`
- Password: `TestPass123` (must have uppercase, lowercase, and number)
- Confirm Password: `TestPass123`

**Expected Behavior:**
1. Click "Register" button
2. You should see "Registering..." with a spinner
3. If successful: "Registration successful! Redirecting to login..."
4. You'll be redirected to the login page
5. Two default leads will be created:
   - Anthony Pearson (CTO, Mountain Technologies LLC)
   - Your email address lead

**Common Errors:**

1. **"Registration failed. Please check your email and password requirements."**
   - Check browser console for detailed error
   - Verify appsettings.json has correct values
   - Ensure Cognito User Pool allows USER_PASSWORD_AUTH

2. **"Value null at 'clientId'"**
   - This means appsettings.json still has placeholder values
   - Update with real CDK output values

3. **"Operation is not supported on this platform"**
   - This was fixed - shouldn't occur anymore

## Verifying Registration in AWS Console

1. Go to AWS Console → Cognito → User Pools
2. Select your user pool (leads-mountaintechnologiesllc-com-users)
3. Click "Users" tab
4. You should see your registered user

## Testing Login After Registration

1. Go to `/login`
2. Enter the same email and password
3. You should be redirected to `/dashboard`
4. You should see the two default leads

## Current Status

✅ **Code is Ready:**
- AuthService properly configured for browser
- JSON serialization fixed (PascalCase)
- HTTP headers correctly set
- All tests passing (26/26)

⏳ **Waiting for:**
- CDK infrastructure deployment
- Real AWS configuration values

## Next Steps

1. Complete task 18: Deploy CDK infrastructure
2. Complete task 19: Update appsettings.json with real values
3. Test registration with the steps above
