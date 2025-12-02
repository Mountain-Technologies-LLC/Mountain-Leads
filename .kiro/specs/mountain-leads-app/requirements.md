# Requirements Document

## Introduction

Mountain Leads is a secure, cloud-based lead management system built as a .NET Blazor WebAssembly application. The system enables users to register, authenticate via Amazon Cognito, and manage their personal lead database through a private dashboard. Each user maintains an isolated collection of leads stored in DynamoDB, accessible only to them through authenticated API Gateway endpoints. The application is deployed to AWS infrastructure using CDK, with static hosting on S3 and CloudFront distribution.

## Glossary

- **Mountain Leads System**: The complete lead management application including the Blazor WebAssembly frontend, AWS infrastructure, and backend services
- **User**: An authenticated individual who registers and manages their private lead collection
- **Lead**: A contact record containing business information (name, title, company, phone, email, location, notes)
- **Blazor WebAssembly Application**: The client-side .NET Blazor WebAssembly web application that runs in the browser. It will be located in the ./website folder.
- **Amazon Cognito User Pool**: The AWS service managing user authentication and authorization
- **DynamoDB Table**: The NoSQL database storing lead records with user isolation
- **API Gateway**: The AWS service providing RESTful CRUD endpoints for lead management
- **CDK Infrastructure**: The .NET C# CDK code that provisions and configures all AWS resources. It will be located in the ./infrastructure folder.
- **Default Leads**: Two pre-populated lead records created automatically upon user registration

## Requirements

### Requirement 1

**User Story:** As a new user, I want to register with only my email and password, so that I can quickly create an account and start managing leads.

#### Acceptance Criteria

1. WHEN a user submits registration credentials THEN the Mountain Leads System SHALL create a new user account in the Amazon Cognito User Pool with email and password
2. WHEN a user completes registration THEN the Mountain Leads System SHALL create a default lead record for Anthony Pearson with complete information (CTO, Mountain Technologies LLC, 952-111-1111, info@mountaintechnologiesllc.com, Minneapolis MN, "Likes to code")
3. WHEN a user completes registration THEN the Mountain Leads System SHALL create a lead record containing the user's email address with all other fields empty
4. WHEN registration validation fails THEN the Mountain Leads System SHALL display clear error messages and prevent account creation
5. WHEN a user attempts to register with an existing email THEN the Mountain Leads System SHALL reject the registration and notify the user

### Requirement 2

**User Story:** As a registered user, I want to authenticate with my email and password, so that I can securely access my private lead collection.

#### Acceptance Criteria

1. WHEN a user submits valid credentials THEN the Mountain Leads System SHALL authenticate the user through the Amazon Cognito User Pool and grant access to the dashboard
2. WHEN a user submits invalid credentials THEN the Mountain Leads System SHALL reject authentication and display an error message
3. WHEN a user successfully authenticates THEN the Mountain Leads System SHALL obtain authentication tokens from Amazon Cognito for API authorization
4. WHEN authentication tokens expire THEN the Mountain Leads System SHALL prompt the user to re-authenticate
5. WHEN a user logs out THEN the Mountain Leads System SHALL invalidate the session and redirect to the login page

### Requirement 3

**User Story:** As an authenticated user, I want to create new lead records, so that I can capture and store contact information for potential business opportunities.

#### Acceptance Criteria

1. WHEN a user submits a new lead with all required fields THEN the Mountain Leads System SHALL store the lead in the DynamoDB Table associated with that user
2. WHEN a user submits a new lead THEN the API Gateway SHALL authorize the request using the Amazon Cognito authentication token before processing
3. WHEN a lead creation request is authorized THEN the Mountain Leads System SHALL generate a unique identifier for the lead
4. WHEN a lead is successfully created THEN the Mountain Leads System SHALL display the new lead in the user's dashboard table
5. WHEN a lead creation fails THEN the Mountain Leads System SHALL display an error message and preserve the user's input

### Requirement 4

**User Story:** As an authenticated user, I want to view all my leads in a dashboard table, so that I can see my complete lead collection at a glance.

#### Acceptance Criteria

1. WHEN a user accesses the dashboard THEN the Mountain Leads System SHALL retrieve only the leads belonging to that authenticated user from the DynamoDB Table
2. WHEN leads are retrieved THEN the Mountain Leads System SHALL display them in a table showing name, title, company, phone, email, location, and notes
3. WHEN the dashboard loads with no leads THEN the Mountain Leads System SHALL display an empty state message
4. WHEN a user is not authenticated THEN the Mountain Leads System SHALL prevent access to the dashboard and redirect to login
5. WHEN lead data is retrieved THEN the API Gateway SHALL verify the user's authentication token before returning results

### Requirement 5

**User Story:** As an authenticated user, I want to update existing lead information, so that I can keep contact details current and accurate.

#### Acceptance Criteria

1. WHEN a user modifies a lead's information and saves THEN the Mountain Leads System SHALL update the corresponding record in the DynamoDB Table
2. WHEN a user attempts to update a lead THEN the API Gateway SHALL verify the lead belongs to the authenticated user before allowing modification
3. WHEN a lead update succeeds THEN the Mountain Leads System SHALL refresh the dashboard table to show the updated information
4. WHEN a lead update fails THEN the Mountain Leads System SHALL display an error message and preserve the original data
5. WHEN a user attempts to update another user's lead THEN the Mountain Leads System SHALL reject the request and return an authorization error

### Requirement 6

**User Story:** As an authenticated user, I want to delete leads I no longer need, so that I can maintain a clean and relevant lead collection.

#### Acceptance Criteria

1. WHEN a user confirms deletion of a lead THEN the Mountain Leads System SHALL remove the lead record from the DynamoDB Table
2. WHEN a user attempts to delete a lead THEN the API Gateway SHALL verify the lead belongs to the authenticated user before allowing deletion
3. WHEN a lead is successfully deleted THEN the Mountain Leads System SHALL remove it from the dashboard table display
4. WHEN a user attempts to delete another user's lead THEN the Mountain Leads System SHALL reject the request and return an authorization error
5. WHEN a deletion fails THEN the Mountain Leads System SHALL display an error message and maintain the lead in the collection

### Requirement 7

**User Story:** As a system administrator, I want the infrastructure deployed via CDK in the language of C#, so that all AWS resources are provisioned consistently and reproducibly.

#### Acceptance Criteria

1. WHEN the CDK Infrastructure is deployed THEN the Mountain Leads System SHALL create an Amazon Cognito User Pool with configuration based on the domain leads.mountaintechnologiesllc.com
2. WHEN the CDK Infrastructure is deployed THEN the Mountain Leads System SHALL create a DynamoDB Table named based on leads.mountaintechnologiesllc.com with appropriate partition and sort keys for user isolation
3. WHEN the CDK Infrastructure is deployed THEN the Mountain Leads System SHALL create an API Gateway with CRUD endpoints that integrate with Amazon Cognito for authorization
4. WHEN the CDK Infrastructure is deployed THEN the Mountain Leads System SHALL configure the existing S3 bucket and CloudFront distribution to serve the Blazor WebAssembly Application
5. WHEN the CDK Infrastructure is deployed THEN the Mountain Leads System SHALL output the API Gateway endpoint URL and Cognito User Pool configuration for frontend integration

### Requirement 8

**User Story:** As a developer, I want the Blazor WebAssembly application to consume the API Gateway endpoints, so that users can perform CRUD operations on their leads through the web interface.

#### Acceptance Criteria

1. WHEN the Blazor WebAssembly Application makes API requests THEN the Mountain Leads System SHALL include the Amazon Cognito authentication token in request headers
2. WHEN the Blazor WebAssembly Application receives API responses THEN the Mountain Leads System SHALL handle success and error states appropriately
3. WHEN API requests fail due to network issues THEN the Mountain Leads System SHALL display user-friendly error messages
4. WHEN the Blazor WebAssembly Application is built THEN the Mountain Leads System SHALL produce static files deployable to the S3 bucket
5. WHEN the Blazor WebAssembly Application loads THEN the Mountain Leads System SHALL retrieve API Gateway and Cognito configuration from environment settings

### Requirement 9

**User Story:** As a user, I want my lead data to be private and isolated, so that other users cannot access or modify my information.

#### Acceptance Criteria

1. WHEN storing leads in the DynamoDB Table THEN the Mountain Leads System SHALL associate each lead with the authenticated user's unique identifier
2. WHEN querying leads from the DynamoDB Table THEN the Mountain Leads System SHALL filter results to include only records belonging to the requesting user
3. WHEN the API Gateway processes requests THEN the Mountain Leads System SHALL extract the user identifier from the Amazon Cognito authentication token
4. WHEN a user attempts to access another user's lead THEN the Mountain Leads System SHALL deny the request and return an authorization error
5. WHEN DynamoDB queries execute THEN the Mountain Leads System SHALL use the user identifier as part of the partition key to ensure data isolation

### Requirement 10

**User Story:** As a developer, I want the build process managed through NPM scripts, so that I can consistently build and deploy the application locally.

#### Acceptance Criteria

1. WHEN the NPM build script executes THEN the Mountain Leads System SHALL compile the Blazor WebAssembly Application into static files
2. WHEN the NPM deploy script executes THEN the Mountain Leads System SHALL run the CDK Infrastructure deployment
3. WHEN build commands are invoked THEN the Mountain Leads System SHALL provide clear output indicating success or failure
4. WHEN the package.json is configured THEN the Mountain Leads System SHALL include scripts for building the Blazor application and deploying infrastructure
5. WHEN dependencies are installed THEN the Mountain Leads System SHALL ensure all required tools for .NET and CDK are available
