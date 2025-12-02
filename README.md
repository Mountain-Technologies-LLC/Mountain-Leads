# Mountain Leads
Mountain Leads repo of Mountain Technologies LLC

# Local Commands
* `npm run start`
- `npm run build`

# AWS Commands for https://leads.mountaintechnologiesllc.com
- See [infrastructure README.md](./infrastructure/README.md) for more.
- `npm run synth`
- `npm run diff`
- `npm run deploy`
- Destroy from AWS
   - `npm run destroy -- --context name=leads.mountaintechnologiesllc.com`


# Initial Prompt
I want to create a leads website called Mountain Leads. I want the website to be a .NET Blazor WebAssembly (Wasm) web app. I want the .NET Blazor WebAssembly (Wasm) web app to be placed in the ./website folder. 

In the ./infrastructure folder, I have copied .NET cdk code to deploy a website to a s3 bucket, with distributions and routing. I will be deploying to http://leads.mountaintechnologiesllc.com and this should work once the wasm site is created. I am using NPM to locally run builds. Please do implement fixes and optimizations to existing cdk code.

I want the infrastructure to deploy Amazon Cognito, based on the used URL—leads.mountaintechnologiesllc.com. I want the .NET Blazor WebAssembly web app it implement authenticating with Amazon Cognito. I want a simple user registration with only email and password. I want each user to be private and only see the leads they enter. Users should be able to create, read, update, and delete, leads. Lead information should contain: name, title, company, phone, email, location, notes. Given the leads data model, I would like to use DynamoDB. I would like to add this in the cdk infrastructure code. I would like to also use an AWS API Gateway for CRUD operations that authorizes the user and updates the DynamoDB database. The name of the DynamoDB should be based on the used URL—leads.mountaintechnologiesllc.com. I want the .NET Blazor WebAssembly web app to be able to consume the CRUD APIs in the AWS API Gateway. When the user registers, I want a default lead to be generated: Anthony Pearson; CTO; Mountain Technologies LLC; 952-111-1111; Minneapolis, MN, info@mountaintechnologiesllc.com; Likes to code. I also want the registered user to be added as a lead as well with just their email added but everything else left blank. I want the users main dashboard to include all the CRUD operations including a table of all of their leads.