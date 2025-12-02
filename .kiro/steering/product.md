# Mountain Leads

Mountain Leads is a private lead management system for tracking business contacts and opportunities. Each user maintains their own isolated collection of leads with full CRUD capabilities.

## Core Features

- **User Authentication**: Email/password registration and login via AWS Cognito
- **Lead Management**: Create, read, update, and delete lead records
- **Data Privacy**: Each user can only access their own leads (user-scoped data isolation)
- **Default Data**: New users receive two initial leads:
  - Anthony Pearson (CTO, Mountain Technologies LLC) with full contact details
  - The registered user's email as a lead entry

## Lead Data Model

Each lead contains:
- Name (required)
- Title
- Company
- Phone
- Email
- Location
- Notes

## Deployment

Production deployment: https://leads.mountaintechnologiesllc.com
