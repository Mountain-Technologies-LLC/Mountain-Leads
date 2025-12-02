# End-to-End Testing Guide - Mountain Leads Application

## Overview

This guide provides step-by-step instructions for performing comprehensive end-to-end testing of the Mountain Leads application. Follow these tests in order to validate all functionality.

## Prerequisites

Before starting, ensure:
- ✅ Infrastructure is deployed to AWS (Task 18 completed)
- ✅ Blazor application configuration is updated with AWS resource values (Task 19 completed)
- ✅ Blazor application is built and deployed to S3/CloudFront (Task 20 completed)
- ✅ You have the CloudFront distribution URL

## Test Environment Setup

### Get Your Application URL

From the CDK deployment outputs, locate:
```
InfrastructureStack.CloudFrontDistributionUrl = https://XXXXXXXXXX.cloudfront.net
```

This is your application URL for testing.

---

## Test Suite

### Test 1: User Registration Flow

**Objective**: Verify new users can register with email and password

**Steps**:
1. Navigate to the CloudFront URL in your browser
2. You should be redirected to the Login page
3. Click the "Register" link or navigate to `/register`
4. Fill in the registration form:
   - Email: `testuser1@example.com` (use a real email you can access)
   - Password: `TestPass123!` (meets requirements: 8+ chars, uppercase, lowercase, number)
5. Click "Register" button

**Expected Results**:
- ✅ Registration succeeds
- ✅ Success message displayed
- ✅ Redirected to login page
- ✅ Confirmation email sent to provided email address (check inbox/spam)

**Requirements Validated**: 1.1, 1.4

---

### Test 2: Verify Default Leads Created

**Objective**: Confirm Anthony Pearson and user email leads are automatically created

**Steps**:
1. After registration, log in with the credentials from Test 1
2. You should be redirected to the Dashboard
3. Examine the leads table

**Expected Results**:
- ✅ Dashboard displays 2 leads
- ✅ First lead is Anthony Pearson with:
  - Name: "Anthony Pearson"
  - Title: "CTO"
  - Company: "Mountain Technologies LLC"
  - Phone: "952-111-1111"
  - Email: "info@mountaintechnologiesllc.com"
  - Location: "Minneapolis, MN"
  - Notes: "Likes to code"
- ✅ Second lead contains:
  - Email: `testuser1@example.com` (your registration email)
  - All other fields empty

**Requirements Validated**: 1.2, 1.3

---

### Test 3: User Login Flow

**Objective**: Verify registered users can authenticate

**Steps**:
1. If still logged in, click "Logout" button
2. You should be redirected to Login page
3. Enter credentials:
   - Email: `testuser1@example.com`
   - Password: `TestPass123!`
4. Click "Login" button

**Expected Results**:
- ✅ Login succeeds
- ✅ Redirected to Dashboard
- ✅ Dashboard displays the 2 default leads from Test 2

**Requirements Validated**: 2.1, 2.3

---

### Test 4: Invalid Login Attempt

**Objective**: Verify invalid credentials are rejected

**Steps**:
1. Logout if logged in
2. Attempt to login with:
   - Email: `testuser1@example.com`
   - Password: `WrongPassword123!`
3. Click "Login" button

**Expected Results**:
- ✅ Login fails
- ✅ Error message displayed: "Invalid credentials" or similar
- ✅ User remains on login page
- ✅ No access to dashboard

**Requirements Validated**: 2.2

---

### Test 5: Create New Lead

**Objective**: Verify users can create new lead records

**Steps**:
1. Login as `testuser1@example.com`
2. On the Dashboard, locate the "Create Lead" button or form
3. Fill in the new lead form:
   - Name: "Jane Smith"
   - Title: "VP of Sales"
   - Company: "Tech Corp"
   - Phone: "555-123-4567"
   - Email: "jane.smith@techcorp.com"
   - Location: "San Francisco, CA"
   - Notes: "Met at conference"
4. Click "Create" or "Save" button

**Expected Results**:
- ✅ Lead creation succeeds
- ✅ Success message displayed
- ✅ Dashboard now shows 3 leads (2 default + 1 new)
- ✅ New lead appears in the table with all entered information
- ✅ Lead has a unique ID
- ✅ CreatedAt and UpdatedAt timestamps are set

**Requirements Validated**: 3.1, 3.3, 3.4

---

### Test 6: View All Leads in Dashboard

**Objective**: Verify dashboard displays all user's leads

**Steps**:
1. While logged in as `testuser1@example.com`
2. Observe the dashboard table

**Expected Results**:
- ✅ Dashboard displays all 3 leads
- ✅ Table shows columns: Name, Title, Company, Phone, Email, Location, Notes
- ✅ All lead data is visible and correctly formatted
- ✅ Leads are sorted (by creation date or name)

**Requirements Validated**: 4.1, 4.2

---

### Test 7: Update Existing Lead

**Objective**: Verify users can modify lead information

**Steps**:
1. While logged in as `testuser1@example.com`
2. Locate the "Jane Smith" lead created in Test 5
3. Click the "Edit" button for that lead
4. Modify the following fields:
   - Title: "Senior VP of Sales" (changed)
   - Phone: "555-987-6543" (changed)
   - Notes: "Met at conference. Follow up next week." (updated)
5. Click "Save" or "Update" button

**Expected Results**:
- ✅ Update succeeds
- ✅ Success message displayed
- ✅ Dashboard refreshes and shows updated information
- ✅ Modified fields reflect new values
- ✅ Unmodified fields remain unchanged
- ✅ UpdatedAt timestamp is updated

**Requirements Validated**: 5.1, 5.3

---

### Test 8: Delete Lead

**Objective**: Verify users can remove leads

**Steps**:
1. While logged in as `testuser1@example.com`
2. Locate the "Jane Smith" lead
3. Click the "Delete" button for that lead
4. If prompted, confirm the deletion

**Expected Results**:
- ✅ Deletion succeeds
- ✅ Success message displayed
- ✅ Dashboard refreshes
- ✅ "Jane Smith" lead no longer appears in the table
- ✅ Dashboard now shows 2 leads (the 2 default leads)

**Requirements Validated**: 6.1, 6.3

---

### Test 9: Logout Flow

**Objective**: Verify logout invalidates session

**Steps**:
1. While logged in as `testuser1@example.com`
2. Click the "Logout" button
3. After logout, try to navigate directly to `/dashboard`

**Expected Results**:
- ✅ Logout succeeds
- ✅ Redirected to login page
- ✅ Attempting to access `/dashboard` redirects to login
- ✅ Session is cleared (no authentication token)

**Requirements Validated**: 2.5

---

### Test 10: Data Isolation - Create Second User

**Objective**: Verify users can only access their own leads

**Steps**:
1. Ensure you're logged out
2. Register a second user:
   - Email: `testuser2@example.com`
   - Password: `TestPass456!`
3. Login as `testuser2@example.com`
4. Observe the dashboard

**Expected Results**:
- ✅ Registration succeeds for second user
- ✅ Login succeeds
- ✅ Dashboard displays exactly 2 leads (default leads for testuser2)
- ✅ Anthony Pearson lead is present
- ✅ User email lead contains `testuser2@example.com`
- ✅ NO leads from testuser1 are visible
- ✅ Jane Smith lead (if recreated by testuser1) is NOT visible

**Requirements Validated**: 9.1, 9.2, 9.4, 9.5

---

### Test 11: Data Isolation - Verify First User's Data

**Objective**: Confirm first user's data remains isolated

**Steps**:
1. Logout from `testuser2@example.com`
2. Login as `testuser1@example.com`
3. Observe the dashboard

**Expected Results**:
- ✅ Dashboard displays only testuser1's leads
- ✅ Anthony Pearson lead is present
- ✅ User email lead contains `testuser1@example.com`
- ✅ NO leads from testuser2 are visible
- ✅ Lead count matches what testuser1 created

**Requirements Validated**: 9.1, 9.2, 9.4, 9.5

---

### Test 12: Create Multiple Leads

**Objective**: Verify system handles multiple leads correctly

**Steps**:
1. While logged in as `testuser1@example.com`
2. Create 3 new leads with different information:
   
   **Lead 1**:
   - Name: "Bob Johnson"
   - Company: "Startup Inc"
   - Email: "bob@startup.com"
   
   **Lead 2**:
   - Name: "Alice Williams"
   - Company: "Enterprise Co"
   - Email: "alice@enterprise.com"
   
   **Lead 3**:
   - Name: "Charlie Brown"
   - Company: "SMB Solutions"
   - Email: "charlie@smb.com"

**Expected Results**:
- ✅ All 3 leads created successfully
- ✅ Dashboard now shows 5 leads total (2 default + 3 new)
- ✅ Each lead has unique ID
- ✅ All leads display correctly in table
- ✅ No data corruption or mixing between leads

**Requirements Validated**: 3.1, 4.1, 4.2

---

### Test 13: Empty State

**Objective**: Verify dashboard handles no leads gracefully

**Steps**:
1. Register a third user: `testuser3@example.com` / `TestPass789!`
2. Login as `testuser3@example.com`
3. Delete both default leads (Anthony Pearson and user email lead)
4. Observe the dashboard

**Expected Results**:
- ✅ After deleting all leads, dashboard shows empty state message
- ✅ Message indicates "No leads found" or similar
- ✅ Create lead functionality still available
- ✅ No errors displayed

**Requirements Validated**: 4.3

---

### Test 14: Unauthenticated Access Prevention

**Objective**: Verify protected routes require authentication

**Steps**:
1. Logout from all accounts
2. Try to access the following URLs directly:
   - `/dashboard`
   - Any other protected routes

**Expected Results**:
- ✅ All protected routes redirect to `/login`
- ✅ No data is displayed
- ✅ No API calls are made without authentication

**Requirements Validated**: 4.4

---

### Test 15: Invalid Registration Attempts

**Objective**: Verify registration validation works

**Steps**:
1. Logout if logged in
2. Navigate to registration page
3. Attempt to register with invalid data:
   
   **Test A - Weak Password**:
   - Email: `test@example.com`
   - Password: `weak` (too short, no uppercase, no number)
   
   **Test B - Invalid Email**:
   - Email: `notanemail`
   - Password: `ValidPass123!`
   
   **Test C - Existing Email**:
   - Email: `testuser1@example.com` (already registered)
   - Password: `TestPass123!`

**Expected Results**:
- ✅ Test A: Registration fails with password policy error
- ✅ Test B: Registration fails with invalid email error
- ✅ Test C: Registration fails with "email already exists" error
- ✅ No accounts created for invalid attempts
- ✅ Clear error messages displayed for each case

**Requirements Validated**: 1.4, 1.5

---

### Test 16: Lead Validation

**Objective**: Verify lead creation validates required fields

**Steps**:
1. Login as any user
2. Attempt to create a lead with missing name:
   - Name: (empty)
   - Email: "test@example.com"
3. Click Create

**Expected Results**:
- ✅ Lead creation fails
- ✅ Error message indicates "Name is required" or similar
- ✅ No lead is created
- ✅ User input is preserved (email field still filled)

**Requirements Validated**: 3.5

---

### Test 17: Update Non-Existent Lead

**Objective**: Verify system handles invalid update attempts

**Steps**:
1. Login as `testuser1@example.com`
2. Using browser dev tools or API client, attempt to update a lead with a non-existent leadId
3. Send PUT request to `/leads/non-existent-id` with valid data

**Expected Results**:
- ✅ Update fails with 404 Not Found
- ✅ Error message indicates "Lead not found"
- ✅ No data is modified

**Requirements Validated**: 5.4

---

### Test 18: Cross-User Modification Prevention

**Objective**: Verify users cannot modify other users' leads

**Steps**:
1. Login as `testuser1@example.com`
2. Note the leadId of one of testuser1's leads
3. Logout and login as `testuser2@example.com`
4. Using browser dev tools or API client, attempt to:
   - Update testuser1's lead (PUT `/leads/{testuser1-lead-id}`)
   - Delete testuser1's lead (DELETE `/leads/{testuser1-lead-id}`)

**Expected Results**:
- ✅ Both attempts fail with 403 Forbidden or 404 Not Found
- ✅ Error message indicates authorization failure
- ✅ testuser1's lead remains unchanged
- ✅ testuser2 cannot see or access testuser1's leads

**Requirements Validated**: 5.2, 5.5, 6.2, 6.4

---

### Test 19: API Authorization

**Objective**: Verify all API endpoints require authentication

**Steps**:
1. Logout from all accounts
2. Using browser dev tools or API client (curl, Postman), attempt to call API endpoints without authentication token:
   - GET `/leads`
   - POST `/leads`
   - GET `/leads/{any-id}`
   - PUT `/leads/{any-id}`
   - DELETE `/leads/{any-id}`

**Expected Results**:
- ✅ All requests fail with 401 Unauthorized
- ✅ No data is returned
- ✅ No operations are performed

**Requirements Validated**: 3.2, 4.5, 8.1

---

### Test 20: Session Persistence

**Objective**: Verify authentication persists across page refreshes

**Steps**:
1. Login as any user
2. Navigate to dashboard
3. Refresh the browser page (F5 or Cmd+R)
4. Navigate to different pages within the app

**Expected Results**:
- ✅ User remains logged in after refresh
- ✅ Dashboard still displays user's leads
- ✅ No re-authentication required
- ✅ Session persists until logout or token expiration

**Requirements Validated**: 2.3, 2.4

---

## Test Results Summary

After completing all tests, fill in the results:

| Test # | Test Name | Status | Notes |
|--------|-----------|--------|-------|
| 1 | User Registration Flow | ⬜ Pass / ⬜ Fail | |
| 2 | Verify Default Leads Created | ⬜ Pass / ⬜ Fail | |
| 3 | User Login Flow | ⬜ Pass / ⬜ Fail | |
| 4 | Invalid Login Attempt | ⬜ Pass / ⬜ Fail | |
| 5 | Create New Lead | ⬜ Pass / ⬜ Fail | |
| 6 | View All Leads in Dashboard | ⬜ Pass / ⬜ Fail | |
| 7 | Update Existing Lead | ⬜ Pass / ⬜ Fail | |
| 8 | Delete Lead | ⬜ Pass / ⬜ Fail | |
| 9 | Logout Flow | ⬜ Pass / ⬜ Fail | |
| 10 | Data Isolation - Create Second User | ⬜ Pass / ⬜ Fail | |
| 11 | Data Isolation - Verify First User's Data | ⬜ Pass / ⬜ Fail | |
| 12 | Create Multiple Leads | ⬜ Pass / ⬜ Fail | |
| 13 | Empty State | ⬜ Pass / ⬜ Fail | |
| 14 | Unauthenticated Access Prevention | ⬜ Pass / ⬜ Fail | |
| 15 | Invalid Registration Attempts | ⬜ Pass / ⬜ Fail | |
| 16 | Lead Validation | ⬜ Pass / ⬜ Fail | |
| 17 | Update Non-Existent Lead | ⬜ Pass / ⬜ Fail | |
| 18 | Cross-User Modification Prevention | ⬜ Pass / ⬜ Fail | |
| 19 | API Authorization | ⬜ Pass / ⬜ Fail | |
| 20 | Session Persistence | ⬜ Pass / ⬜ Fail | |

---

## Troubleshooting

### Cannot Access Application

**Issue**: CloudFront URL returns error or doesn't load

**Solutions**:
- Verify CloudFront distribution is deployed (check AWS Console)
- Wait 5-10 minutes for CloudFront to propagate
- Check S3 bucket has Blazor files deployed
- Verify CloudFront origin is pointing to correct S3 bucket

### Registration Fails

**Issue**: User registration returns error

**Solutions**:
- Verify Cognito User Pool exists and is configured correctly
- Check password meets requirements (8+ chars, uppercase, lowercase, number)
- Ensure email format is valid
- Check CloudWatch logs for Lambda errors

### Login Fails

**Issue**: Valid credentials don't work

**Solutions**:
- Verify user was created in Cognito (check AWS Console → Cognito → Users)
- Check if email verification is required
- Ensure Cognito User Pool Client ID is correct in appsettings.json
- Review browser console for errors

### Dashboard Empty or Errors

**Issue**: Dashboard doesn't show leads or shows errors

**Solutions**:
- Check browser console for JavaScript errors
- Verify API Gateway URL is correct in appsettings.json
- Check API Gateway has correct Lambda integrations
- Verify DynamoDB table exists and has correct schema
- Check CloudWatch logs for Lambda function errors

### API Returns 401 Unauthorized

**Issue**: All API calls fail with 401

**Solutions**:
- Verify Cognito authorizer is configured on API Gateway
- Check authentication token is being sent in Authorization header
- Ensure token hasn't expired
- Verify Cognito User Pool ID matches in API Gateway authorizer

### Leads Not Persisting

**Issue**: Created leads disappear after refresh

**Solutions**:
- Verify DynamoDB table exists
- Check Lambda functions have DynamoDB permissions
- Review CloudWatch logs for DynamoDB errors
- Ensure TABLE_NAME environment variable is set correctly on Lambda functions

---

## Requirements Coverage

This test suite validates all requirements from the specification:

- **Requirement 1**: User Registration ✅ (Tests 1, 2, 15)
- **Requirement 2**: User Authentication ✅ (Tests 3, 4, 9, 20)
- **Requirement 3**: Create Leads ✅ (Tests 5, 12, 16)
- **Requirement 4**: View Leads ✅ (Tests 6, 13, 14)
- **Requirement 5**: Update Leads ✅ (Tests 7, 17, 18)
- **Requirement 6**: Delete Leads ✅ (Tests 8, 18)
- **Requirement 7**: Infrastructure Deployment ✅ (Validated in Task 18)
- **Requirement 8**: API Integration ✅ (Tests 19)
- **Requirement 9**: Data Isolation ✅ (Tests 10, 11, 18)
- **Requirement 10**: Build Process ✅ (Validated in Tasks 16, 20)

---

## Completion Criteria

The end-to-end testing is complete when:

- ✅ All 20 tests pass successfully
- ✅ No critical bugs or errors encountered
- ✅ All requirements are validated
- ✅ Application is accessible via CloudFront URL
- ✅ Data isolation is confirmed between users
- ✅ Authentication and authorization work correctly
- ✅ CRUD operations function as expected

---

## Next Steps

After completing end-to-end testing:

1. Document any issues found in a bug report
2. Fix any critical bugs before production release
3. Consider additional testing:
   - Performance testing (load testing with multiple concurrent users)
   - Security testing (penetration testing, vulnerability scanning)
   - Accessibility testing (WCAG compliance)
   - Cross-browser testing (Chrome, Firefox, Safari, Edge)
   - Mobile responsiveness testing
4. Prepare for production deployment
5. Set up monitoring and alerting (CloudWatch alarms, dashboards)

---

## Notes

- This testing guide assumes the application is deployed to AWS
- Some tests require browser developer tools or API client (curl, Postman)
- Tests should be performed in order as some depend on previous test results
- Keep track of test user credentials for reference
- Document any deviations from expected results

**Testing Date**: _________________

**Tester Name**: _________________

**Application Version**: _________________

**CloudFront URL**: _________________
