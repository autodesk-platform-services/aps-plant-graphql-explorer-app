# Plant 3D AEC Data Model Explorer

This repository helps you set up **Plant 3D AEC Data Model (AECDM) Explorer** OAuth credentials and use them to obtain access tokens for Autodesk Platform Services (APS) APIs.

---

## Table of Contents

- [Prerequisites](#prerequisites)
- [Step 1 — Create APS App Credentials](#step-1--create-aps-app-credentials)
- [Step 2 — Configure This Tool](#step-2--configure-this-tool)
- [Step 3 — Use This Tool](#step-3--use-this-tool)

---

## Prerequisites

- An Autodesk Platform Services (APS) account
- Access to the APS Developer Portal
- ACC/Docs access
- Knowledge of GraphQL syntax

---

## Step 1 — Create APS App Credentials

To use APS APIs, you must create an application and obtain OAuth credentials.

1. Sign in to the [**APS Developer Portal**](https://aps.autodesk.com)
2. Open **Applications** from your profile menu
3. Click **Create application**
4. Enter an application name and select the appropriate application type (for example, **Web App**)
5. After creation, copy:
   - **Client ID**
   - **Client Secret** (confidential clients only)
6. Configure the **Callback URL**, for example:

   ```
   http://localhost:8080/api/auth/callback
   ```

   > The callback URL **must exactly match** what your application/tool uses.

7. Enable the APIs required by your use case. Make sure you include:
   - **AEC Data Model API**
   - **Autodesk Construction Cloud API**
8. Click **Save changes**

---

## Step 2 — Configure This Tool

Your OAuth configuration depends on the application type you created.
In this example, **Option A** is used.  
**Option B** can also be used with this sample application, depending on the application type.

### Option A — OAuth (Client Secret)

Use this for confidential clients (for example, server-side web apps).

```text
Client ID      = "your_client_id"
Client Secret  = "your_client_secret"
Callback URL   = "http://localhost:8080/api/auth/callback"  # must match Step 1
```

### Option B — OAuth with PKCE (No Client Secret)

Use this for public clients (for example, desktop or SPA apps) using PKCE.

```text
Client ID    = "your_client_id"
Callback URL = "http://localhost:8080/api/auth/callback"  # must match Step 1
```

---

## Step 3 — Use This Tool

### Login

After you complete the OAuth configuration, click **Sign in with Autodesk** and follow the standard Autodesk login flow.

### Accounts

After login, the first view lists all **Accounts** the current user can access. Double-click an account to view its Plant Collaboration Projects.

### Plant Collaboration Projects

Items with the Plant icon are Plant Collaboration Projects.

- **Black icon**: The project supports AECDM
- **Red icon**: The project does not support AECDM, or the project is broken

### Query

When you double-click a supported Plant Collaboration Project, you’ll see a query view where you can query data from AECDM.

#### Scope

Select the data source to query:

- 3D Model
- P&ID

#### P3D References

The dropdown contains three sections:

1. **Example Queries** — Examples to help you learn how to query AECDM
2. **All** — All Plant parameters you can use in queries
3. **Parameter Categories** — Parameters grouped into Categories

> Double-click an item to automatically insert its query snippet into the query input.

#### Query Input

Enter your GraphQL query here. For filtering guidance, see:
- https://aps.autodesk.com/en/docs/aecdatamodel/v1/developers_guide/filtering/

#### Export CSV

Export the query results to CSV for downstream use.
