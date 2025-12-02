# Troubleshooting 403 Error on Dashboard

## Issue
Getting a 403 Forbidden error when trying to access the dashboard or make API calls.

## Root Causes

The 403 error can be caused by several issues:

1. **CORS Configuration** - API Gateway rejecting requests from CloudFront domain
2. **Missing/Invalid Authorization Token** - Token not being sent or is malformed
3. **Cognito Authorizer Configuration** - Authorizer not properly validating tokens
4. **Token Expiration** - JWT token has expired

## Fixes Applied

### 1. CORS Configuration Updated

**Problem**: API Gateway was only allowing specific domains, but CloudFront uses a different URL.

**Fix**: Updated `ApiGatewayConstruct.cs` to allow all origins for development:
```csharp
AllowOrigins = Cors.ALL_ORIGINS,
AllowMethods = Cors.ALL_METHODS,
AllowCredentials = false
```

### 2. Self-Sign-Up Enabled

**Problem**: Cognito User Pool had self-sign-up disabled.

**Fix**: Added `SelfSignUpEnabled = true` to `CognitoConstruct.cs`

## Deployment Steps

To apply these fixes, run:

```bash
npm run deploy:infra
```

This will:
1. Update the Cognito User Pool to allow self-registration
2. Update the API Gateway CORS configuration
3. Redeploy all infrastructure

**Wait 2-5 minutes** for the deployment to complete.

## Verification Steps

### Step 1: Check Browser Console

1. Open your CloudFront URL in a browser
2. Open Developer Tools (F12)
3. Go to the Console tab
4. Look for any error messages

### Step 2: Check Network Tab

1. In Developer Tools, go to the Network tab
2. Try to load the dashboard
3. Look for the API request (should be to your API Gateway URL)
4. Click on the failed request
5. Check the **Request Headers** - verify `Authorization: Bearer <token>` is present
6. Check the **Response** - see what error message is returned

### Step 3: Verify Token is Stored

In the browser console, run:
```javascript
sessionStorage.getItem('idToken')
```

This should return a long JWT token string. If it returns `null`, the login didn't work properly.

### Step 4: Decode the Token

If you have a token, you can decode it at https://jwt.io to verify:
- The token is valid
- It has the correct `iss` (issuer) - should match your User Pool
- It has a `sub` (subject) claim with the user ID
- It hasn't expired (`exp` claim)

## Common Issues and Solutions

### Issue: Token is null or undefined

**Cause**: Login didn't complete successfully or token wasn't stored.

**Solution**:
1. Logout completely
2. Clear browser cache and sessionStorage
3. Login again
4. Check console for any login errors

### Issue: Token exists but API still returns 403

**Cause**: Cognito authorizer configuration mismatch.

**Solution**:
1. Verify the User Pool ID in `appsettings.json` matches the deployed User Pool
2. Check that the API Gateway authorizer is using the correct User Pool
3. Ensure the token is being sent in the `Authorization` header with `Bearer ` prefix

### Issue: CORS errors in console

**Cause**: API Gateway CORS not configured correctly.

**Solution**:
1. Redeploy infrastructure with updated CORS settings
2. Clear browser cache
3. Try again in incognito mode

### Issue: "User is not confirmed"

**Cause**: Email verification required but not completed.

**Solution**:
1. Check your email for verification link
2. Click the verification link
3. Try logging in again

Alternatively, you can manually confirm the user in AWS Console:
1. Go to Cognito → User Pools → Your Pool → Users
2. Find your user
3. Click "Confirm user" if status is "UNCONFIRMED"

## Manual Testing with curl

You can test the API directly with curl to isolate the issue:

### 1. Get your token

After logging in successfully, get the token from browser console:
```javascript
sessionStorage.getItem('idToken')
```

### 2. Test API with token

```bash
# Replace with your actual values
API_URL="https://your-api-id.execute-api.us-east-1.amazonaws.com/prod"
TOKEN="your-id-token-here"

# Test GET /leads
curl -X GET "$API_URL/leads" \
  -H "Authorization: Bearer $TOKEN" \
  -v
```

**Expected**: 200 OK with JSON response containing leads array

**If 403**: The token or authorizer configuration is the issue

**If CORS error**: The CORS configuration needs updating

## AWS Console Verification

### Check Cognito User Pool

1. Go to AWS Console → Cognito → User Pools
2. Find your pool: `leads-mountaintechnologiesllc-com-users`
3. Click on it
4. Go to "App integration" tab → "App clients"
5. Verify:
   - Client ID matches your `appsettings.json`
   - Auth flows include "ALLOW_USER_PASSWORD_AUTH"
   - "Prevent user existence errors" is enabled

### Check API Gateway Authorizer

1. Go to AWS Console → API Gateway
2. Find your API: `leads-mountaintechnologiesllc-com-api`
3. Click on it
4. Go to "Authorizers" in the left menu
5. Click on your Cognito authorizer
6. Verify:
   - User Pool is correct
   - Token source is `Authorization`
   - Token validation is enabled

### Check API Gateway CORS

1. In API Gateway, go to "Resources"
2. Click on any resource (e.g., `/leads`)
3. Click on "OPTIONS" method
4. Check the integration response
5. Verify headers include:
   - `Access-Control-Allow-Origin: *`
   - `Access-Control-Allow-Methods: GET,POST,PUT,DELETE,OPTIONS`
   - `Access-Control-Allow-Headers: Content-Type,Authorization,...`

## Still Having Issues?

If you're still getting 403 errors after trying these steps:

1. **Check CloudWatch Logs**:
   - Go to CloudWatch → Log Groups
   - Find `/aws/lambda/CreateLeadFunction` (or other function)
   - Look for recent logs
   - Check for authorization errors

2. **Enable API Gateway Logging**:
   - Go to API Gateway → Your API → Stages → prod
   - Enable CloudWatch Logs
   - Set log level to INFO
   - Check logs for authorization failures

3. **Verify Lambda Permissions**:
   - Go to Lambda → Your function
   - Check the execution role has DynamoDB permissions
   - Verify environment variables are set correctly

4. **Test with a Fresh User**:
   - Register a completely new user
   - Verify email (if required)
   - Login with new user
   - Try accessing dashboard

## Quick Fix Checklist

- [ ] Deployed infrastructure with updated CORS and Cognito settings
- [ ] Cleared browser cache
- [ ] Logged out and logged back in
- [ ] Verified token exists in sessionStorage
- [ ] Checked browser console for errors
- [ ] Checked Network tab for failed requests
- [ ] Verified User Pool ID and Client ID in appsettings.json
- [ ] Confirmed user in Cognito (if needed)
- [ ] Waited 5-10 minutes for CloudFront cache to clear

## Next Steps

After fixing the 403 error:

1. Complete user registration
2. Verify default leads are created
3. Test CRUD operations
4. Continue with end-to-end testing guide

---

**Last Updated**: After fixing CORS and self-sign-up issues
