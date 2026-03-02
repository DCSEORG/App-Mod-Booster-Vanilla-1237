# Architecture Diagram – Expense Management App

## Azure Services Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          Azure (uksouth)                                    │
│                                                                             │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                      Azure App Service (S1)                          │  │
│  │                      app-expensemgmt-{suffix}                        │  │
│  │                                                                      │  │
│  │  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐  │  │
│  │  │  Web App (Razor  │  │   REST API       │  │   Chat UI        │  │  │
│  │  │  Pages - .NET 8) │  │  /api/expenses   │  │  /chatui/        │  │  │
│  │  │  /Index          │  │  /api/users      │  │  index.html      │  │  │
│  │  │  /Create         │  │  /api/statuses   │  │                  │  │  │
│  │  │  /Details        │  │  /api/chat       │  │                  │  │  │
│  │  └──────────────────┘  └──────────────────┘  └──────────────────┘  │  │
│  │                                                                      │  │
│  │  Identity: User-Assigned Managed Identity (mid-AppModAssist-02-03-38)│  │
│  └──────────────┬────────────────────┬───────────────────────────────┘  │
│                 │                    │                                    │
│                 │ SQL Auth           │ OpenAI / Search                    │
│                 ▼ (Managed Identity) │ (Managed Identity)                │
│  ┌──────────────────────────┐        │                                    │
│  │     Azure SQL Database   │        │                                    │
│  │     (Basic Tier)         │        │                                    │
│  │     Database: Northwind  │        │                                    │
│  │                          │        │                                    │
│  │  Tables:                 │        │                                    │
│  │  • Roles                 │        │                                    │
│  │  • Users                 │        │                                    │
│  │  • ExpenseCategories     │        │                                    │
│  │  • ExpenseStatus         │        │                                    │
│  │  • Expenses              │        │                                    │
│  │                          │        │                                    │
│  │  Stored Procedures:      │        │                                    │
│  │  • sp_GetExpenses        │        │                                    │
│  │  • sp_GetExpenseById     │        │                                    │
│  │  • sp_CreateExpense      │        │                                    │
│  │  • sp_UpdateExpenseStatus│        │                                    │
│  │  • sp_GetUsers           │        │                                    │
│  │  • sp_GetCategories      │        │                                    │
│  │  • sp_GetStatuses        │        │                                    │
│  └──────────────────────────┘        │                                    │
│                                      │                                    │
│          ┌───────────────────────────┘                                    │
│          │                                                                 │
│          ▼                           ▼                                     │
│  ┌────────────────────┐   ┌──────────────────────┐                        │
│  │  Azure OpenAI      │   │  Azure AI Search     │                        │
│  │  (swedencentral)   │   │  (uksouth, S0)       │                        │
│  │  Model: gpt-4o     │   │                      │                        │
│  │  Capacity: 8 TPM   │   │  Role: Search Index  │                        │
│  │                    │   │       Data Reader    │                        │
│  │  Role: Cognitive   │   │                      │                        │
│  │  Services OpenAI   │   │  (Optional - only    │                        │
│  │  User (MI)         │   │   when deployGenAI   │                        │
│  │                    │   │   = true)            │                        │
│  │  (Optional)        │   │                      │                        │
│  └────────────────────┘   └──────────────────────┘                        │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

         ▲
         │ HTTPS
         │
  ┌──────────────┐
  │     User     │
  │  (Browser)   │
  └──────────────┘
```

## Data Flow

```
User (Browser)
      │
      │ HTTPS Request
      ▼
Azure App Service (.NET 8 Razor Pages + API)
      │                           │
      │ Stored Procedure calls    │ Azure OpenAI API calls
      │ (Managed Identity)        │ (Managed Identity)
      ▼                           ▼
Azure SQL Database         Azure OpenAI (gpt-4o)
  (Northwind)                    │
                                 │ Function calling
                                 │ (get_expenses, create_expense, etc.)
                                 ▼
                          Back to App Service
                          (orchestrates results)
                                 │
                                 ▼
                          Response to Chat UI
```

## Security Model

- **No SQL Authentication**: Azure SQL uses Entra ID (Azure AD) only authentication
- **Managed Identity**: App Service uses a User-Assigned Managed Identity for both SQL and OpenAI access
- **RBAC Roles**:
  - SQL: `db_datareader`, `db_datawriter`, `EXECUTE` on stored procedures
  - OpenAI: `Cognitive Services OpenAI User`
  - AI Search: `Search Index Data Reader`
- **No secrets in code**: All credentials via managed identity token flow
- **HTTPS Only**: App Service enforces HTTPS

## Deployment Options

| Script | GenAI | Cost |
|--------|-------|------|
| `deploy.sh` | ❌ No | Lower (SQL + App Service only) |
| `deploy-with-chat.sh` | ✅ Yes | Higher (+ OpenAI + AI Search) |

> **Note**: The Chat UI returns a helpful dummy message if GenAI is not deployed, so the app works fully without it.
