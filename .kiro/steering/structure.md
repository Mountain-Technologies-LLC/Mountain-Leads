# Project Structure

## Root Level

```
/
├── infrastructure/          # AWS CDK infrastructure and Lambda functions
├── website/                 # Blazor WebAssembly frontend application
├── website.Tests/           # Frontend unit and property-based tests
├── scripts/                 # Deployment and configuration scripts
├── Documents/               # Project documentation and guides
├── package.json             # NPM build orchestration
└── README.md                # Project overview
```

## Infrastructure (`/infrastructure`)

CDK infrastructure code and Lambda backend functions.

```
infrastructure/
├── src/
│   ├── Infrastructure/              # CDK stack and constructs
│   │   ├── Constructs/              # Reusable CDK constructs
│   │   │   ├── ApiGatewayConstruct.cs
│   │   │   ├── BucketConstruct.cs
│   │   │   ├── BucketDeploymentConstruct.cs
│   │   │   ├── CognitoConstruct.cs
│   │   │   ├── DistributionConstruct.cs
│   │   │   ├── DynamoDbConstruct.cs
│   │   │   └── LambdaConstruct.cs
│   │   ├── InfrastructureStack.cs   # Main CDK stack
│   │   ├── Program.cs               # CDK app entry point
│   │   └── Infrastructure.csproj
│   │
│   ├── Infrastructure.Tests/        # Infrastructure unit tests
│   │   ├── ApiGatewayConstructTests.cs
│   │   ├── CognitoConstructTests.cs
│   │   ├── DynamoDbConstructTests.cs
│   │   └── Infrastructure.Tests.csproj
│   │
│   ├── Lambda/                      # Lambda function handlers
│   │   ├── Functions/               # Lambda function implementations
│   │   │   ├── CreateLeadFunction.cs
│   │   │   ├── DeleteLeadFunction.cs
│   │   │   ├── GetLeadFunction.cs
│   │   │   ├── InitLeadsFunction.cs
│   │   │   ├── ListLeadsFunction.cs
│   │   │   └── UpdateLeadFunction.cs
│   │   ├── Models/                  # Shared data models
│   │   │   ├── ApiResponse.cs
│   │   │   └── Lead.cs
│   │   ├── Utilities/               # Helper classes
│   │   │   ├── DictionaryExtensions.cs
│   │   │   ├── DynamoDbHelper.cs
│   │   │   ├── IDynamoDbHelper.cs
│   │   │   └── JwtHelper.cs
│   │   └── Lambda.csproj
│   │
│   └── Lambda.Tests/                # Lambda function tests
│       ├── Functions/               # Property-based and unit tests
│       ├── Utilities/               # Utility tests
│       └── Lambda.Tests.csproj
│
├── cdk.json                         # CDK configuration
└── README.md
```

## Website (`/website`)

Blazor WebAssembly frontend application.

```
website/
├── Pages/                           # Razor page components
│   ├── Dashboard.razor              # Main lead management UI
│   ├── Login.razor                  # User login page
│   ├── Register.razor               # User registration page
│   └── *.razor.css                  # Component-scoped styles
│
├── Services/                        # Business logic services
│   ├── AuthService.cs               # Cognito authentication
│   └── LeadService.cs               # Lead CRUD operations
│
├── Models/                          # Data transfer objects
│   ├── ApiResponse.cs
│   ├── AuthenticationResult.cs
│   ├── Lead.cs
│   └── UserCredentials.cs
│
├── Layout/                          # Layout components
├── Components/                      # Reusable UI components
├── wwwroot/                         # Static assets
│   ├── appsettings.json             # Configuration (AWS resource IDs)
│   ├── css/                         # Global styles
│   └── index.html                   # SPA entry point
│
├── App.razor                        # Root component
├── Program.cs                       # Application entry point
├── _Imports.razor                   # Global using statements
└── website.csproj
```

## Tests (`/website.Tests`)

Frontend unit and property-based tests.

```
website.Tests/
├── Pages/                           # Page component tests
│   └── DashboardTests.cs
├── Services/                        # Service layer tests
│   ├── AuthServicePropertyTests.cs
│   ├── LeadServicePropertyTests.cs
│   └── LeadServiceTests.cs
└── website.Tests.csproj
```

## Architecture Patterns

### Construct Pattern (CDK)
Infrastructure is organized into reusable constructs (e.g., `CognitoConstruct`, `DynamoDbConstruct`) that encapsulate related AWS resources. The main `InfrastructureStack` composes these constructs.

### Service Layer Pattern (Blazor)
Business logic is separated into service classes (`AuthService`, `LeadService`) that are injected via dependency injection. Pages consume services through interfaces.

### Dependency Injection
Lambda functions accept `IDynamoDbHelper` for testability. Blazor services are registered in `Program.cs` and injected into components.

### API Response Pattern
All API responses follow a consistent structure with `ApiResponse<T>` containing `Success`, `Data`, and `Error` fields.

### User Isolation
All data operations are scoped by `userId` extracted from JWT tokens. DynamoDB uses composite keys: `userId` (partition key) + `leadId` (sort key).

## Naming Conventions

- **C# Classes**: PascalCase (e.g., `CreateLeadFunction`, `AuthService`)
- **C# Methods**: PascalCase (e.g., `FunctionHandler`, `LoginAsync`)
- **C# Properties**: PascalCase (e.g., `UserId`, `LeadId`)
- **C# Private Fields**: `_camelCase` with underscore prefix (e.g., `_dynamoDbHelper`)
- **Razor Components**: PascalCase with `.razor` extension
- **AWS Resources**: Kebab-case based on domain (e.g., `leads-mountaintechnologiesllc-com`)
- **Environment Variables**: UPPER_SNAKE_CASE (e.g., `TABLE_NAME`, `AWS_REGION`)

## Configuration Management

- **CDK Context**: Domain name passed via `--context name=leads.mountaintechnologiesllc.com`
- **Website Config**: `wwwroot/appsettings.json` contains AWS resource IDs (updated by `scripts/update-config.js`)
- **Lambda Environment**: Environment variables injected by CDK constructs
