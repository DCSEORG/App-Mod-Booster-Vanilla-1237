![Header image](https://github.com/DougChisholm/App-Mod-Booster/blob/main/repo-header-booster.png)

# App-Mod-Booster
A project to show how GitHub coding agent can turn screenshots of a legacy app into a working proof-of-concept for a cloud native Azure replacement if the legacy database schema is also provided.

Steps to modernise an app:

1. Fork this repo 
2. In new repo replace the screenshots and sql schema (or keep the samples)
3. Open the coding agent and use app-mod-booster agent telling it "modernise my app"
4. When the app code is generated (can take up to 30 minutes) there will be a pull request to approve.
5. Now you can use codespaces to deploy the app to azure (or open VS Code and clone the repo locally - you will need to install some tools locally or use the devcontainer)
6. Open terminal and type "az login" to set subscription/context
7. Then type "bash deploy.sh" to deploy the app and db or "bash deploy-with-chat.sh" to deploy the app, db and chat UI.

Supporting slides for Microsoft Employees:
[Here](<https://microsofteur-my.sharepoint.com/:p:/g/personal/dchisholm_microsoft_com/IQAY41LQ12fjSIfFz3ha4hfFAZc7JQQuWaOrF7ObgxRK6f4?e=p6arJs>)

---

## Deployment Instructions

### Prerequisites

Before deploying, ensure you have the following installed:

- **Azure CLI** (`az`) – [Install guide](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)
- **jq** – JSON processor: `sudo apt-get install jq` / `brew install jq`
- **Python 3** – `python3 --version`
- **.NET 8 SDK** – [Install guide](https://dotnet.microsoft.com/download/dotnet/8.0)
- **ODBC Driver 18 for SQL Server** – [Install guide](https://learn.microsoft.com/en-us/sql/connect/odbc/download-odbc-driver-for-sql-server)

### Steps

1. **Log in to Azure**
   ```bash
   az login
   az account set --subscription "YOUR_SUBSCRIPTION_ID"
   ```

2. **Create a Resource Group**
   ```bash
   az group create --name rg-expensemgmt-demo --location uksouth
   ```

3. **Get your Object ID and Email**
   ```bash
   az ad signed-in-user show --query id -o tsv       # Your object ID
   az ad signed-in-user show --query userPrincipalName -o tsv  # Your email
   ```

4. **Update variables in deploy.sh**
   Open `deploy.sh` and update:
   ```bash
   ADMIN_OBJECT_ID="your-object-id-here"
   ADMIN_LOGIN="your-email@example.com"
   ```

5. **Run the deployment**

   Without GenAI (recommended to start):
   ```bash
   bash deploy.sh
   ```

   With GenAI (Azure OpenAI + AI Search):
   ```bash
   bash deploy-with-chat.sh
   ```

6. **Navigate to the app**

   At the end of the deployment, the URL will be printed:
   ```
   App URL: https://<app-name>.azurewebsites.net/Index
   ```

   > **Important**: Navigate to `/Index` (not the root URL) to view the app.

### What Gets Deployed

| Resource | Description |
|----------|-------------|
| Azure App Service (S1) | Hosts the .NET 8 Razor Pages web app + REST API |
| Azure SQL Database (Basic) | Northwind database with expense schema |
| User-Assigned Managed Identity | Secure authentication between services |
| Azure OpenAI (optional) | GPT-4o model for AI chat assistant |
| Azure AI Search (optional) | AI Search service (S0) |

### Architecture

See [ARCHITECTURE.md](ARCHITECTURE.md) for a full diagram of the system architecture.

### App Features

- 📋 **Expense List** – View all expenses with status filtering
- ➕ **New Expense** – Create expenses with employee, category, amount, date
- 🔍 **Expense Details** – Review expenses and approve/reject as a manager
- 🔌 **REST API** – Full CRUD API with Swagger documentation at `/swagger`
- 🤖 **AI Chat** – Chat assistant using Azure OpenAI with function calling

### Troubleshooting

**Database connection error on first load?**
The app will show a banner with the error and display sample data. Common fix:
- Ensure managed identity has been granted DB access (run-sql-dbrole.py)
- Check firewall rules allow your IP and Azure services

**Managed Identity error?**
The error banner shows a specific fix message. Run:
```bash
python3 run-sql-dbrole.py
```
