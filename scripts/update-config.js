#!/usr/bin/env node

/**
 * Script to update Blazor appsettings.json with CDK stack outputs
 * This script reads the CDK outputs from CloudFormation and updates the configuration file
 */

const { CloudFormationClient, DescribeStacksCommand } = require('@aws-sdk/client-cloudformation');
const fs = require('fs');
const path = require('path');

// Configuration
const STACK_NAME = 'InfrastructureStack';
const CONFIG_PATH = path.join(__dirname, '..', 'website', 'wwwroot', 'appsettings.json');
const AWS_REGION = process.env.AWS_REGION || 'us-east-1';

async function getStackOutputs() {
  const client = new CloudFormationClient({ region: AWS_REGION });
  
  try {
    const command = new DescribeStacksCommand({ StackName: STACK_NAME });
    const response = await client.send(command);
    
    if (!response.Stacks || response.Stacks.length === 0) {
      throw new Error(`Stack ${STACK_NAME} not found`);
    }
    
    const stack = response.Stacks[0];
    const outputs = {};
    
    if (stack.Outputs) {
      stack.Outputs.forEach(output => {
        outputs[output.OutputKey] = output.OutputValue;
      });
    }
    
    return outputs;
  } catch (error) {
    console.error('Error fetching stack outputs:', error.message);
    throw error;
  }
}

async function updateConfig() {
  try {
    console.log('Fetching CDK stack outputs...');
    const outputs = await getStackOutputs();
    
    console.log('Stack outputs:', outputs);
    
    // Read current config
    console.log(`Reading config from ${CONFIG_PATH}...`);
    const configContent = fs.readFileSync(CONFIG_PATH, 'utf8');
    const config = JSON.parse(configContent);
    
    // Update AWS configuration with CDK outputs
    if (!config.AWS) {
      config.AWS = {};
    }
    
    config.AWS.Region = AWS_REGION;
    
    if (outputs.UserPoolId) {
      config.AWS.UserPoolId = outputs.UserPoolId;
      console.log(`Updated UserPoolId: ${outputs.UserPoolId}`);
    } else {
      console.warn('Warning: UserPoolId not found in stack outputs');
    }
    
    if (outputs.UserPoolClientId) {
      config.AWS.ClientId = outputs.UserPoolClientId;
      console.log(`Updated ClientId: ${outputs.UserPoolClientId}`);
    } else {
      console.warn('Warning: UserPoolClientId not found in stack outputs');
    }
    
    if (outputs.ApiGatewayUrl) {
      config.AWS.ApiGatewayUrl = outputs.ApiGatewayUrl;
      console.log(`Updated ApiGatewayUrl: ${outputs.ApiGatewayUrl}`);
    } else {
      console.warn('Warning: ApiGatewayUrl not found in stack outputs');
    }
    
    // Write updated config
    console.log(`Writing updated config to ${CONFIG_PATH}...`);
    fs.writeFileSync(CONFIG_PATH, JSON.stringify(config, null, 2) + '\n', 'utf8');
    
    console.log('Configuration updated successfully!');
    console.log('\nUpdated configuration:');
    console.log(JSON.stringify(config, null, 2));
    
  } catch (error) {
    console.error('Error updating configuration:', error.message);
    process.exit(1);
  }
}

// Run the script
updateConfig();
