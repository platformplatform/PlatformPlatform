![PlatformPlatform Resource Groups](https://platformplatformgithub.blob.core.windows.net/$root/GitHubTopBanner.png)

<h4 align="center">

[![App Gateway](https://github.com/platformplatform/PlatformPlatform/actions/workflows/app-gateway.yml/badge.svg)](https://github.com/platformplatform/PlatformPlatform/actions/workflows/app-gateway.yml?query=branch%3Amain)
[![Account](https://github.com/platformplatform/PlatformPlatform/actions/workflows/account.yml/badge.svg)](https://github.com/platformplatform/PlatformPlatform/actions/workflows/account.yml?query=branch%3Amain)
[![Main](https://github.com/platformplatform/PlatformPlatform/actions/workflows/main.yml/badge.svg)](https://github.com/platformplatform/PlatformPlatform/actions/workflows/main.yml?query=branch%3Amain)
[![Back Office](https://github.com/platformplatform/PlatformPlatform/actions/workflows/back-office.yml/badge.svg)](https://github.com/platformplatform/PlatformPlatform/actions/workflows/back-office.yml?query=branch%3Amain)
[![Cloud Infrastructure](https://github.com/platformplatform/PlatformPlatform/actions/workflows/cloud-infrastructure.yml/badge.svg)](https://github.com/platformplatform/PlatformPlatform/actions/workflows/cloud-infrastructure.yml?query=branch%3Amain)

[![GitHub issues with enhancement label](https://img.shields.io/github/issues-raw/platformplatform/PlatformPlatform/enhancement?label=enhancements&logo=github&color=%23A2EEEF)](https://github.com/orgs/PlatformPlatform/projects/1/views/3?filterQuery=-status%3A%22%E2%9C%85+Done%22+label%3Aenhancement)
[![GitHub issues with roadmap label](https://img.shields.io/github/issues-raw/platformplatform/PlatformPlatform/roadmap?label=roadmap&logo=github&color=%23006B75)](https://github.com/orgs/PlatformPlatform/projects/2/views/2?filterQuery=is%3Aopen+label%3Aroadmap)
[![GitHub issues with bug label](https://img.shields.io/github/issues-raw/platformplatform/PlatformPlatform/bug?label=bugs&logo=github&color=red)](https://github.com/platformplatform/PlatformPlatform/issues?q=is%3Aissue+is%3Aopen+label%3Abug)

[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=PlatformPlatform_platformplatform&metric=coverage)](https://sonarcloud.io/component_measures?id=PlatformPlatform_platformplatform&metric=coverage)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=PlatformPlatform_platformplatform&metric=alert_status)](https://sonarcloud.io/summary/overall?id=PlatformPlatform_platformplatform)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=PlatformPlatform_platformplatform&metric=security_rating)](https://sonarcloud.io/component_measures?id=PlatformPlatform_platformplatform&metric=Security)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=PlatformPlatform_platformplatform&metric=reliability_rating)](https://sonarcloud.io/component_measures?id=PlatformPlatform_platformplatform&metric=Reliability)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=PlatformPlatform_platformplatform&metric=sqale_rating)](https://sonarcloud.io/component_measures?id=PlatformPlatform_platformplatform&metric=Maintainability)

</h4>

# 👋 Welcome to PlatformPlatform

Kick-start building top-tier B2B & B2C cloud SaaS products with sleek design, fully localized and accessible, vertical slice architecture, automated and fast DevOps, and top-notch security.

Built to demonstrate seamless flow: backend contracts feed a fully-typed React UI, pipelines make fully automated deployments to Azure, and a multi-agent AI workflow where PlatformPlatform-expert agents collaborate to deliver complete features following the opinionated architecture. Think of it as a ready-made blueprint, not a pile of parts to assemble.

## What's inside

* **Backend** - .NET 10 and C# 14 adhering to the principles of vertical slice architecture, DDD, CQRS, and clean code
* **Frontend** - React 19, TypeScript, TanStack Router & Query, ShadCN 2.0 with Base UI for accessible UI
* **CI/CD** - GitHub actions for fast passwordless deployments of docker containers and infrastructure (Bicep)
* **Infrastructure** - Cost efficient and scalable Azure PaaS services like Azure Container Apps, Azure PostgreSQL, etc.
* **Developer CLI** - Extendable .NET CLI for DevEx - set up CI/CD is one command and a couple of questions
* **AI rules** - 30+ rules & workflows for Claude Code - sync to other editors can be enabled via `.gitignore`
* **Multi-agent development** - Specialized autonomous Claude Code agents expert in PlatformPlatform's architecture

![Multi Agent Workflow](https://platformplatformgithub.blob.core.windows.net/multi-agent-workflow.png)

Follow our [up-to-date roadmap](https://github.com/orgs/PlatformPlatform/projects/2/views/2) with core SaaS features like SSO, monitoring, alerts, multi-region, feature flags, back office for support, etc.

Show your support for our project - give us a star on GitHub! It truly means a lot! ⭐

# Getting Started

TL;DR: Open the [PlatformPlatform](./application/PlatformPlatform.slnx) solution in Rider or Visual Studio and run the [Aspire AppHost](./application/AppHost/AppHost.csproj) project.

### Prerequisites

For development, you need .NET, Docker, and Node. And GitHub and Azure CLI for setting up CI/CD.

<details>

<summary>Install prerequisites for Windows</summary>
	
1.	Open a PowerShell terminal as Administrator and run the following command to install Windows Subsystem for Linux (required for Docker). Restart your computer if prompted.
  
    ```powershell
    wsl --install
    ```

2.	From an Administrator PowerShell terminal, use [winget](https://learn.microsoft.com/en-us/windows/package-manager/winget/) (preinstalled on Windows 11) to install any missing packages:

    ```powershell
    winget install Microsoft.DotNet.SDK.10
    winget install Git.Git
    winget install Docker.DockerDesktop
    winget install Microsoft.AzureCLI
    winget install GitHub.cli
    ```

3. Install Node.js — the version must match [`.node-version`](./application/.node-version). We recommend [fnm](https://github.com/Schniz/fnm) which auto-installs the exact version via the Developer CLI. When using an IDE like Rider, ensure the active fnm version matches [`.node-version`](./application/.node-version).

    ```powershell
    # Option A: fnm (recommended)
    winget install Schniz.fnm

    # Option B: Node.js directly
    winget install OpenJS.NodeJS
    ```

4.	(Recommended) Install language servers for enhanced Claude Code support:

    ```powershell
    npm install -g typescript-language-server typescript
    dotnet tool install -g csharp-ls
    ```

</details>

<details>

<summary>Install prerequisites for Mac</summary>

Open a terminal and run the following commands (if not installed):

1. Install [Homebrew](https://brew.sh/), a package manager for Mac

2. Install packages

   ```bash
   brew install --cask dotnet-sdk
   brew install --cask docker
   brew install git azure-cli gh
   ```

3. Install Node.js — the version must match [`.node-version`](./application/.node-version). We recommend [fnm](https://github.com/Schniz/fnm) which auto-installs the exact version via the Developer CLI. When using an IDE like Rider, ensure the active fnm version matches [`.node-version`](./application/.node-version).

   ```bash
   # Option A: fnm (recommended)
   brew install fnm

   # Option B: Node.js directly
   brew install node
   ```

4. (Recommended) Install language servers for enhanced Claude Code support:

   ```bash
   npm install -g typescript-language-server typescript
   dotnet tool install -g csharp-ls
   ````

</details>

<details>

<summary>Install prerequisites for Linux (Ubuntu/Debian)</summary>

Open a terminal and run the following commands (if not installed):

1. Install basic tools

   ```bash
   sudo apt update && sudo apt install -y git wget curl libnss3-tools
   ```

2. Add Microsoft package repository

   ```bash
   source /etc/os-release
   wget https://packages.microsoft.com/config/$ID/$VERSION_ID/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
   sudo dpkg -i packages-microsoft-prod.deb
   rm packages-microsoft-prod.deb
   ```

3. Install .NET SDK and Docker

   ```bash
   sudo apt-get update && sudo apt-get install -y dotnet-sdk-10.0 docker.io docker-compose-v2
   ```

   ```bash
   sudo systemctl enable --now docker
   sudo usermod -aG docker $USER
   ```

4. Install Node.js — the version must match [`.node-version`](./application/.node-version). We recommend [fnm](https://github.com/Schniz/fnm) which auto-installs the exact version via the Developer CLI. When using an IDE like Rider, ensure the active fnm version matches [`.node-version`](./application/.node-version).

   ```bash
   # Option A: fnm (recommended)
   curl -fsSL https://fnm.vercel.app/install | bash

   # Option B: Node.js directly
   curl -fsSL https://deb.nodesource.com/setup_24.x | sudo -E bash -
   sudo apt-get install -y nodejs
   ```

5. Trust the HTTPS development certificate

   ```bash
   echo 'export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:${SSL_CERT_DIR:-/usr/lib/ssl/certs}"' >> ~/.bashrc
   ```

   ```bash
   source ~/.bashrc
   ```

   ```bash
   dotnet dev-certs https --trust
   ```

6. **Log out and log back in** to apply Docker group and shell configuration changes.

7. (Recommended) Install language servers for enhanced Claude Code support

   ```bash
   npm install -g typescript-language-server typescript
   dotnet tool install -g csharp-ls
   ```

8. (Optional) If using Snap Chromium, trust the certificate in its sandbox

   ```bash
   certutil -d sql:$HOME/snap/chromium/current/.pki/nssdb -L >/dev/null 2>&1 || (mkdir -p $HOME/snap/chromium/current/.pki/nssdb && certutil -d sql:$HOME/snap/chromium/current/.pki/nssdb -N --empty-password)
   dotnet dev-certs https --trust
   ```

9. (Optional) Install GitHub CLI and Azure CLI (needed for CI/CD setup)

   ```bash
   (type -p wget >/dev/null || (sudo apt update && sudo apt-get install wget -y)) \
   && sudo mkdir -p -m 755 /etc/apt/keyrings \
   && wget -qO- https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo tee /etc/apt/keyrings/githubcli-archive-keyring.gpg > /dev/null \
   && sudo chmod go+r /etc/apt/keyrings/githubcli-archive-keyring.gpg \
   && echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null
   sudo apt-get update && sudo apt-get install -y gh
   curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
   ```

</details>

## 1. Clone the repository

```bash
git clone https://github.com/platformplatform/PlatformPlatform.git
```

We recommend you keep the commit history, which serves as a great learning and troubleshooting resource. 😃

## 2. (Optional) Install the Developer CLI

The PlatformPlatform CLI provides convenient commands for common tasks. From the cloned repository, install it globally to use the `pp` command from anywhere in your terminal.

```bash
cd developer-cli
dotnet run install
```

Restart your terminal to make the `pp` command available.

![Developer CLI](https://platformplatformgithub.blob.core.windows.net/$root/developer-cli.png)

## 3. Run the Aspire AppHost to spin up everything on localhost

Using Aspire, docker images with PostgreSQL, Blob Storage emulator, and development mail server will be downloaded and started. No need to install anything, or learn complicated commands. Simply run this command, and everything just works 🎉

With the CLI installed:

```bash
pp run --attach
```

Or without the CLI:

```bash
cd application/AppHost
dotnet run # First time downloading Docker containers will take several minutes
```

Alternatively, open the [PlatformPlatform](./application/PlatformPlatform.slnx) solution in Rider or Visual Studio and run the [Aspire AppHost](./application/AppHost/AppHost.csproj) project.

On first startup, Aspire will prompt for `stripe-enabled` -- enter `true` to configure Stripe integration (see the optional Stripe setup section below) or `false` to skip.

Once the Aspire dashboard fully loads, click to the WebApp and sign up for a new account (https://localhost:9000/signup). A one-time password (OTP) will be sent to the development mail server, but for local development, you can always use the code `UNLOCK` instead of checking the mail server. As shown here:

<img src="https://platformplatformgithub.blob.core.windows.net/$root/local-development-exp.gif" alt="Getting Started" title="Developer Experience" width="800"/>

### 3.1 (Optional) Set up Google OAuth for "Sign in with Google" on localhost

PlatformPlatform supports authentication via Google OAuth using OpenID Connect with PKCE. This is optional for local development since email-based one-time passwords work without any configuration. The Aspire dashboard prompts whether to enable Google OAuth on first startup.

<details>

<summary>Google Cloud Console setup</summary>

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project (e.g., "YourProduct OAuth")
3. Navigate to **APIs & Services** > **Credentials**
4. Configure OAuth consent screen (first time only):
   - App name, support email, audience (External), contact info
   - Agree to Google API Services: User Data Policy
5. Create OAuth client ID:
   - Application type: "Web application"
   - Name: "YourProduct Localhost"
6. Add Authorized redirect URIs:
   - `https://localhost:9000/api/account/authentication/Google/login/callback`
   - `https://localhost:9000/api/account/authentication/Google/signup/callback`
7. Note the Client ID and Client Secret

</details>

**Aspire parameter configuration** (two restarts required):

1. **First restart**: Aspire prompts whether to enable Google OAuth. Enter `true` to enable or `false` to skip. Once entered, restart Aspire.
2. **Second restart**: Aspire prompts for the **Client ID** and **Client Secret**. Enter the values from the Google Cloud Console, then restart Aspire to apply the configuration.

All values are stored securely in .NET user secrets and persist across restarts.

### 3.2 (Optional) Set up Stripe sandbox for localhost

PlatformPlatform includes a comprehensive Stripe integration for subscription management and payments: embedded Stripe checkout and payment elements, prorated plan upgrades and downgrades, tax management, localized UI and invoice text, refund overview in billing history, telemetry events for the subscription lifecycle, grandfather pricing for existing subscribers, dunning and failed payment recovery, and full sync between Stripe and the local database via webhooks. The Stripe Dashboard must be set up and configured according to the Stripe Dashboard setup guide below before enabling Stripe in Aspire.

<details>

<summary>Stripe Dashboard setup</summary>

Each developer needs their own Stripe sandbox. The local database stays in sync with Stripe via webhook events -- when payments, subscriptions, or billing details change in Stripe, webhook events update the local database with matching customer and subscription IDs. If developers share a sandbox, webhook events from one developer's actions would corrupt another developer's local database.

1. **Create a sandbox**: Go to [Stripe Dashboard](https://dashboard.stripe.com) and create an account. Click the **account picker** (top-left) > **Sandboxes** > **Create**. Name it `dev-yourname` and open it. All subsequent steps are performed inside your sandbox.
2. **Create products**: Navigate to **Product catalog** > **+ Create product**. Create a `Standard` product with **Recurring** / **Monthly** pricing (e.g., 19 EUR), then a `Premium` product (e.g., 39 EUR). Important: Click **More pricing options** and add `standard_monthly` and `premium_monthly` respectively in the **Lookup key** field.
3. **Disable non-card payment methods**: Go to **Settings** (gear icon) > **Payments** > **Payment methods**. Turn off every payment method except **Cards** (and **Cartes Bancaires** which cannot be disabled)
4. **Limit to 1 subscription**: Go to **Settings** > **Payments** > **Checkout and Payment Links**, scroll to **Subscriptions**, and enable **Limit customers to 1 subscription**. Add link to `https://app.yourcompany.com/account/billing` (`https://localhost:9000/account/billing` is not valid)
5. **Configure failed payment recovery**: Go to **Settings** > **Billing** > **Subscriptions and emails** > **Manage failed payments**, and configure desired retry behavior
6. **Configure email notifications**: Go to **Settings** > **Billing** > **Subscriptions and emails** > **Email notifications and customer management**, and enable all settings as you see fit. Set "Use your own custom link" to `https://localhost:9000/account/billing`
7. **Enable 3D Secure**: Go to **Settings** > **Billing** > **Subscriptions and emails** > **Manage payments that require confirmation**, and check off **Enable 3D Secure**. The embedded Stripe components support showing e.g. Visa and Danish MitID multi-factor confirmation dialogs
8. **Set invoice prefix**: Go to **Settings** > **Billing** > **Invoices**. In the **Invoice numbering** section, change the **Invoice prefix** to something meaningful for your organization, and optionally reset the **Invoice sequence** if needed
9. **Disable payment link in invoice emails**: Go to **Settings** > **Billing** > **Invoices** and uncheck **Include a link to a payment page in the invoice email**
10. **Set up Tax**: Go to **More** > **Tax** > **Locations** > **+ Add test registration** and follow the guide. Below is an example for a Danish company (adapt based on your business):
    - Fill out the "Add head office address" side pane with your company address. Click **Continue**
    - In "Add Tax registration" select your country (e.g., **Denmark**) and **I'm already registered**. Click **Continue**
    - Select **Domestic** (e.g., "registered in Denmark"). Click **Continue**
    - Select **No** to "Do you want to collect VAT on cross-border sales...". Click **Continue**
    - Select **Yes** to "Have your sales of goods or digital services... been less than EUR 10,000". Click **Continue**
    - Select **Start collecting immediately** in "Schedule tax collection". Click **Continue**
    - Select **Software as a service (SaaS) - business use** in "Confirm your tax rates" (should show 25% for Denmark). Click **Start collecting**
11. **Get API keys**: Navigate to **Developers** > **API keys**. Note the **Publishable key** (`pk_test_...`) and **Secret key** (`sk_test_...`). These will be needed for the Aspire configuration below.

</details>

**Aspire parameter configuration** (two restarts required):

1. **First restart**: Aspire prompts for the **Publishable key** and **Secret key** (API key). Once entered, the Stripe CLI container starts and connects to Stripe. Find the generated webhook signing secret in either:
   - The **Stripe Dashboard** under **Developers** > **Workbench** > **Webhooks** -- click the three-dot menu on the event destination to reveal the signing secret
   - The **stripe-cli** container logs in the Aspire dashboard (look for "Your webhook signing secret is whsec_...")
2. **Second restart**: Aspire prompts for the **Webhook secret**. Enter the `whsec_` value from the previous step.

All values are stored securely in .NET user secrets and persist across restarts.

## 4. Set up CI/CD with passwordless deployments from GitHub to Azure

Run this command to automate Azure Subscription configuration and set up [GitHub Workflows](https://github.com/platformplatform/PlatformPlatform/actions) for deploying [Azure Infrastructure](./cloud-infrastructure) (using Bicep) and compiling [application code](./application) to Docker images deployed to Azure Container Apps:

```bash
cd developer-cli
dotnet run deploy # Tip: Add --verbose-logging to show the used CLI commands
```

You need to be the owner of the GitHub repository and the Azure Subscription, plus have permissions to create Service Principals and Active Directory Groups.

The command will first prompt you to login to Azure and GitHub, and collect information. You will be presented with a complete list of changes before they are applied. It will look something like this:

![Configure Continuous Deployments](https://platformplatformgithub.blob.core.windows.net/$root/ConfigureContinuousDeployments.png)

Except for adding a DNS record, everything is fully automated. After successful setup, the command will provide simple instructions on how to configure branch policies, Sonar Cloud static code analysis, and more.

The infrastructure is configured with auto-scaling and hosting costs in focus. It will cost less than 2 USD per day for a cluster, and it will allow scaling to millions of users 🎉

![Azure Costs](https://platformplatformgithub.blob.core.windows.net/$root/azure-costs-center.png)

### (Optional) Configure Google OAuth for staging and production

If you set up Google OAuth locally, use the Developer CLI to store your Google OAuth credentials as GitHub secrets for deployment to Azure Key Vault:

```bash
pp github-config
```

Remember to add redirect URIs for each environment in your Google Cloud Console configuration, e.g.:
- `https://staging.yourproduct.com/api/account/authentication/Google/login/callback`
- `https://staging.yourproduct.com/api/account/authentication/Google/signup/callback`
- `https://app.yourproduct.com/api/account/authentication/Google/login/callback`
- `https://app.yourproduct.com/api/account/authentication/Google/signup/callback`

### (Optional) Configure Stripe for staging and production

Create a separate Stripe account (or sandbox) for each environment. For production, use a live Stripe account instead of a sandbox. Follow the Stripe Dashboard setup steps in [section 3.2](#32-optional-set-up-stripe-sandbox-for-localhost) to configure products, payment methods, tax, invoices, and other settings.

On localhost, the Stripe CLI container automatically forwards webhook events. On staging and production, you need to configure a webhook endpoint manually in the Stripe Dashboard:

1. Go to **Developers** > **Webhooks** > **Add destination**
2. Keep **Your account** selected. Under events, select the **Charge**, **Checkout**, **Credit Note**, **Customer**, **Invoice**, **Payment Intent**, **Payment Method**, **Price**, **Product**, **Refund**, **Setup Intent**, and **Subscription Schedule** categories (roughly 91 events as of March 2026, though Stripe may add or remove events over time). Do not use **Select all** as it includes v2 thin-payload events the handler does not support. Click **Continue**
3. Select **Webhook endpoint** as the destination type
4. Set the **Destination name** to the environment (e.g., `Staging` or `Production`), set the **Endpoint URL** to `https://app.yourproduct.com/api/account/subscriptions/stripe-webhook` (replace with your actual domain), and click **Continue**
5. On the destination detail page, click **Reveal** under **Signing secret** to get the webhook secret (`whsec_...`)

Use the Developer CLI to store Stripe credentials as GitHub secrets for deployment to Azure Key Vault:

```bash
pp github-config
```

Select the **Stripe** group and enter the **Publishable Key**, **API Key** (Secret key), and **Webhook Secret** (the signing secret from the webhook endpoint). The subscription feature is automatically enabled on Azure when all three secrets are present in Key Vault.

# Multi-Agent Development with Claude Code

PlatformPlatform includes a multi-agent autonomous development workflow powered by [Claude Code Agent Teams](https://code.claude.com/docs/en/agent-teams). Specialized AI agents collaborate to deliver complete features, from requirements to production-ready code, while enforcing enterprise-grade quality standards.

## What makes this different

**Zero-tolerance code reviews**: AI agents follow rules well until they hit problems, then cut corners, which is why many struggle to get AI to write production-ready code. Dedicated reviewer agents catch this. They reject any code that can objectively be made better: compiler warnings, static analysis errors, browser console warnings, or deviation from established patterns. All warnings including warnings in seemingly unrelated parts of the system are fixed. This boy scout rule approach ensures every commit meets production standards.

**Native Agent Teams coordination**: Built on Claude Code's Agent Teams primitives (TeamCreate, SendMessage, shared TaskList). The team lead spawns and coordinates all agents automatically. No custom orchestration layer, no external process managers.

**Parallel execution with task sets**: Backend, frontend, and E2E tracks run concurrently within each task set. Engineers implement in parallel, reviewers validate independently, and the Guardian commits everything in dependency order once all tracks are approved.

**Guardian-owned commits**: A dedicated Guardian agent owns all git commits, staging, and final validation. It runs build, test, format, and inspect before every commit and refuses to proceed if anything fails. No other agent touches git.

**Architect coherence across task sets**: A persistent architect agent tracks how implementation evolves. Engineers discuss with the architect when they need to diverge from the plan during development. After each commit, the architect reads divergence notes and updates upcoming tasks when the implementation reveals something that changes future plans.

**Continuous regression testing**: A regression tester runs visual and functional tests via Claude in Chrome browser automation throughout the implementation, catching UI regressions and console errors in real time.

**Cross-team collaboration**: Agents communicate directly via SendMessage and interrupt signals. If the frontend engineer needs a backend API change, they ask the backend engineer, wait for implementation, then continue. Engineers interrupt QA when contracts change so tests stay in sync.

**No context window exhaustion**: With specialized agents for each domain, no single agent accumulates context bloat. Fresh engineer and reviewer pairs are spawned for each task set, always starting with a clean context window while persistent agents (guardian, architect, regression tester) maintain continuity across the feature.

**Session persistence**: The developer CLI saves session IDs for the team lead and pair programmer so they can be resumed after a restart. Saved sessions can be continued or resumed from a menu.

**Retrospectives**: After all tasks are completed, the team lead facilitates a retrospective. Each agent reflects over the implementation and the workflow, then all agents cross-review each other's findings. The team lead aggregates the results into a prioritized list of recommended improvements.

**Standard product management tool integration**: Works with Linear, Azure DevOps, Jira, GitHub, or markdown files in the local filesystem. Tasks flow through statuses (planned, active, review, completed) with full audit trail. Adjust tasks mid-flight or restart features entirely if the first attempt misses the mark.

## Agent roles

**Team lead** (launched via the developer CLI):
- Coordinates the full agent team. Spawns all sub-agents, delegates work, and tracks progress. Never writes code directly

**Persistent agents** (spawned once per feature by the team lead):
- **guardian**: Owns all git commits, Aspire restarts, and final validation. Zero tolerance for failures
- **architect**: Tracks implementation evolution across task sets, reviews divergence notes, and updates upcoming tasks
- **regression-tester**: Continuous visual and functional testing via Claude in Chrome browser automation

**Fresh agents** (spawned per task set by the team lead):
- **backend**, **frontend**, **qa**: Engineers who implement code within their specialty
- **backend-reviewer**, **frontend-reviewer**, **qa-reviewer**: Zero-tolerance gatekeepers who review line-by-line, then approve files for staging

**On-demand agents**:
- **researcher**: Investigates APIs, libraries, and best practices. Reports findings but never writes code

## How to use

This workflow requires Claude Code and will not work with other AI coding assistants.

### 1. Create a feature branch

```bash
git checkout -b feature-name
```

### 2. Define your feature

Start the team lead agent using the [Developer CLI](#2-optional-install-the-developer-cli):

```bash
pp claude-agent team-lead
```

Use the `/create-prd` skill. The team lead will guide you through a brief interview to understand what you want to build, then generate a complete feature specification with tasks in your product management tool (Linear, Azure DevOps, Jira, GitHub, or markdown files).

### 3. Let the team lead take over

Tell the team lead which feature to implement by providing the title or ID. From here, the team lead spawns all the agents automatically: guardian, architect, regression tester, and fresh engineer/reviewer pairs for each task set.

Backend and frontend engineers work in parallel. QA engineers write tests alongside implementation but wait for reviewer approval before running them. Reviewers scrutinize every change line by line and only approve code that meets production standards. The Guardian runs final validation and commits in dependency order (backend, then frontend, then E2E).

The entire process can take several hours depending on complexity, but at the end you get a fully implemented feature: backend logic, database migrations, API endpoints, frontend UI, localization, and end-to-end tests. All committed. All tests passing. Ready to ship.

## Ad-hoc work without the agent team

For smaller tasks, bug fixes, or exploratory work that don't need the full agent team, use the pair programmer instead:

```bash
pp claude-agent pair-programmer
```

The pair programmer is a standalone agent that works directly with you as a hands-on collaborator. It is not part of the agent team workflow above. It can spawn sub-agents when the task benefits from parallel work or code review.

# Inside Our Monorepo

PlatformPlatform is a [monorepo](https://en.wikipedia.org/wiki/Monorepo) containing all application code, infrastructure, tools, libraries, documentation, etc. A monorepo is a powerful way to organize a codebase, used by Google, Facebook, Uber, Microsoft, etc.

```bash
.
├─ .claude               # Claude Code agent definitions and team configurations
│  ├─ agents             # Agent Teams agent definitions (team-lead, engineers, reviewers, etc.)
│  ├─ commands           # Slash commands and workflows
│  ├─ hooks              # Claude Code hooks to enforce MCP tool usage and prevent dangerous git operations
│  └─ rules              # AI rules for code generation patterns
├─ .github               # GitHub configuration and CI/CD workflows
├─ application           # Contains the application source code
│  ├─ AppHost            # Aspire project starting app and all dependencies in Docker
│  ├─ AppGateway         # Main entry point for the app using YARP as a reverse proxy
│  ├─ main               # Primary SCS and shell app -- build your product here
│  │   ├─ WebApp         # React SPA frontend using TypeScript and ShadCN 2.0 with Base UI
│  │   ├─ Api            # Presentation layer exposing the API to WebApp or other clients
│  │   ├─ Core           # Core business logic, application use cases, and infrastructure
│  │   ├─ Workers        # Background workers for long-running tasks and event processing
│  │   └─ Tests          # Tests for the Api, Core, and Workers
│  ├─ account            # Federated module for authentication, user and account management
│  │   ├─ WebApp         # React SPA loaded into main via Module Federation
│  │   ├─ Api            # Presentation layer exposing the API to WebApp or other clients
│  │   ├─ Core           # Core business logic, application use cases, and infrastructure
│  │   ├─ Workers        # Background workers for long-running tasks and event processing
│  │   └─ Tests          # Tests for the Api, Core, and Workers
│  ├─ back-office        # Standalone SCS for operations and support (separate login)
│  │   ├─ WebApp         # React SPA frontend using TypeScript and ShadCN 2.0 with Base UI
│  │   ├─ Api            # Presentation layer exposing the API to WebApp or other clients
│  │   ├─ Core           # Core business logic, application use cases, and infrastructure
│  │   ├─ Workers        # Background workers for long-running tasks and event processing
│  │   └─ Tests          # Tests for the Api, Core, and Workers
│  ├─ shared-kernel      # Reusable components and default configuration for all systems
│  └─ shared-webapp      # Reusable ShadCN 2.0 components with Base UI that affect all systems
├─ cloud-infrastructure  # Contains Bash and Bicep scripts (IaC) for Azure resources
│  ├─ cluster            # Scale units like production-west-eu, production-east-us, etc.
│  ├─ environment        # Shared resources like App Insights, Container Registry, etc.
│  └─ modules            # Reusable Bicep modules like Container App, PostgreSQL, etc.
└─ developer-cli         # A .NET CLI tool for automating common developer tasks
```

** A [Self-Contained System](https://scs-architecture.org/) is a large microservice (or a small monolith) that contains the full stack, including frontend, background jobs, etc. The `main` SCS is the shell application with catch-all routing where you build your product. The `account` SCS is loaded into main via Module Federation, enabling seamless navigation between product pages and account pages without full page reloads. The `back-office` SCS runs as a fully standalone application with its own login.

# Technologies

### .NET 10 Backend With Vertical Sliced Architecture, DDD, CQRS, Minimal API, and Aspire

The backend is built using the most popular, mature, and commonly used technologies in the .NET ecosystem:

- [.NET 10](https://dotnet.microsoft.com) and [C# 14](https://learn.microsoft.com/en-us/dotnet/csharp/tour-of-csharp)
- [Aspire](https://aka.ms/dotnet-aspire)
- [YARP](https://microsoft.github.io/reverse-proxy)
- [ASP.NET Minimal API](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [Entity Framework](https://learn.microsoft.com/en-us/ef)
- [MediatR](https://github.com/jbogard/MediatR)
- [FluentValidation](https://fluentvalidation.net)
- [Mapster](https://github.com/MapsterMapper/Mapster)
- [XUnit](https://xunit.net), [FluentAssertions](https://fluentassertions.com), [NSubstitute](https://nsubstitute.github.io), and [Bogus](https://github.com/bchavez/Bogus)
- [SonarCloud](https://sonarcloud.io) and [JetBrains Code style and Cleanup](https://www.jetbrains.com/help/rider/Code_Style_Assistance.html)

<details>

<summary>Read more about the backend architecture</summary>

- **Vertical Slice Architecture**: The codebase is organized around vertical slices, each representing a feature or module, promoting separation of concerns and maintainability.
- **Domain-Driven Design (DDD)**: DDD principles are applied to ensure a clear and expressive domain model.
- **Command Query Responsibility Segregation (CQRS)**: This clearly separates read (query) and write (command) operations, adhering to the single responsibility principle (each action is in a separate command).
- **Screaming architecture**: The architecture is designed with namespaces (folders) per feature, making the concepts easily visible and expressive, rather than organizing the code by types like models and repositories.
- **MediatR pipelines**: MediatR pipeline behaviors are used to ensure consistent handling of cross-cutting concerns like validation, unit of work, and handling of domain events.
- **Strongly Typed IDs**: The codebase uses strongly typed IDs, which are a combination of the entity type and the entity ID. This is even at the outer API layer, and Swagger translates this to the underlying contract. This ensures type safety and consistency across the codebase.
- **JetBrains Code style and Cleanup**: JetBrains Rider/ReSharper is used for code style and automatic cleanup (configured in `.DotSettings`), ensuring consistent code formatting. No need to discuss tabs vs. spaces anymore; Invalid formatting breaks the build.
- **Self-contained systems**: The codebase is organized into self-contained systems. A self-contained system is a large microservice (or a small monolith) that contains the full stack including frontend, background jobs, etc. These can be developed, tested, deployed, and scaled in isolation, making it a good compromise between a large monolith and many small microservices. Unlike the popular backend-for-frontend (BFF) style with one shared frontend, this allows teams to work fully independently. The main SCS is the shell application where you build your product.
- **Shared Kernel**: The codebase uses a shared kernel for all the boilerplate code required to build a clean codebase. The shared kernel ensures consistency between self-contained systems, e.g., enforcing tenant isolation, auditing, tracking, implementation of tactical DDD patterns like aggregate, entities, repository base, ID generation, etc.

</details>

### React 19 Frontend With TypeScript, ShadCN 2.0, Base UI, and Node

The frontend is built with these technologies:

- [React 19](https://react.dev)
- [TypeScript](https://www.typescriptlang.org)
- [ShadCN 2.0](https://ui.shadcn.com) with [Base UI](https://base-ui.com)
- [Tanstack Router](https://tanstack.com/router)
- [Tanstack Query](https://tanstack.com/query)
- [Lingui](https://lingui.dev) for internationalization (i18n)
- [oxlint](https://oxc.rs/docs/guide/usage/linter) and [oxfmt](https://oxc.rs/docs/guide/usage/formatter) for linting and formatting
- [Node](https://nodejs.org/en)

### Azure Cloud Infrastructure With Enterprise-Grade Security and Zero Secrets

PlatformPlatform's cloud infrastructure is built using the latest Azure Platform as a Service (PaaS) technologies:

- [Azure Container Apps](https://learn.microsoft.com/en-us/azure/container-apps/overview)
- [Azure Database for PostgreSQL](https://azure.microsoft.com/en-us/products/postgresql)
- [Azure Blob Storage](https://azure.microsoft.com/en-us/services/storage/blobs)
- [Azure Service Bus](https://azure.microsoft.com/en-us/services/service-bus)
- [Azure Key Vault](https://azure.microsoft.com/en-us/services/key-vault)
- [Azure Application Insights](https://azure.microsoft.com/en-us/services/monitor)
- [Azure Log Analytics](https://azure.microsoft.com/en-us/services/monitor)
- [Azure Virtual Network](https://azure.microsoft.com/en-us/services/virtual-network)
- [Azure Managed Identities](https://docs.microsoft.com/en-us/azure/active-directory/lifecyclesmanaged-identities-azure-resources/overview)
- [Azure Container Registry](https://azure.microsoft.com/en-us/products/communication-services/)
- [Azure Communication Services](https://azure.microsoft.com/en-us/products/communication-services/)
- [Microsoft Defender for Cloud](https://azure.microsoft.com/en-us/products/defender-for-cloud)

<details>

<summary>Read more about this enterprise-grade architecture</summary>

- **Platform as a Service (PaaS) technologies**: Azure is the leading Cloud Service Provider (CSP) when it comes to PaaS technologies. PlatformPlatform uses PaaS technologies which are fully managed by Microsoft, as opposed to Infrastructure as a Service (IaaS) technologies where the customer is responsible for the underlying infrastructure. This means that Microsoft is responsible for the availability of the infrastructure, and you are only responsible for the application and data. This makes it possible for even a small team to run a highly scalable, stable, and secure solution.
- **Enterprise-grade security with zero secrets**:
  - **Managed Identities**: No secrets are used when Container Apps connect to e.g. Databases, Blob Storage, and Service Bus. The infrastructure uses Managed Identities for all communication with Azure resources, eliminating the need for secrets.
  - **Federated credentials**: Deployment from GitHub to Azure is done using federated credentials, establishing a trust between the GitHub repository and Azure subscription based on the repository's URL, without the need for secrets.
  - **No secrets expires**: Since no secrets are used, there is no need to rotate secrets, and no risk of secrets expiring.
  - **100% Security Score**: The current infrastructure configuration follows best practices, and the current setup code achieves a 100% Security Score in Microsoft Defender for Cloud. This minimizes the attack surface and protects against even sophisticated attacks.
- **Automatic certificate management**: The infrastructure is configured to automatically request and renew SSL certificates, eliminating the need for manual certificate management.
- **Multiple environments**: The setup includes different environments like Development, Staging, and Production, deployed into clearly named resource groups within a single Azure Subscription.
- **Multi-region**: Spinning up a cluster in a new region is a matter of adding one extra deployment job to the GitHub workflow. This allows customers to select a region where their data is close to the user and local data protection laws like GDPR, CCPA, etc. are followed.
- **Azure Container Apps**: The application is hosted using Azure Container Apps, which is a new service from Azure that provides a fully managed Kubernetes environment for running containerized applications. You don't need to be a Kubernetes expert to run your application in a scalable and secure environment.
- **Scaling from zero to millions of users**: The Azure Container App Environment is configured to scale from zero to millions of users, and the infrastructure is configured to scale automatically based on load. This means the starting costs are very low, and the solution can scale to millions of users without any manual intervention. This enables having Development and Staging environments running with very low costs.
- **Azure PostgreSQL**: The database is hosted using Azure Database for PostgreSQL Flexible Server, which is a fully managed PostgreSQL database. PostgreSQL is known for its high performance, stability, scalability, and security. The server will easily handle millions of users with single-digit millisecond response times.

</details>

# Screenshots

This is how it looks when GitHub workflows has deployed Azure Infrastructure:

![GitHub Environments](https://platformplatformgithub.blob.core.windows.net/GitHubInfrastructureDeployments.png)

These are the resource groups created when deploying one staging cluster, and two production clusters:

![PlatformPlatform Resource Groups](https://platformplatformgithub.blob.core.windows.net/PlatformPlatformResourceGroups.png)

This is the security score after deploying PlatformPlatform resources to Azure. Achieving a 100% security score in Azure Defender for Cloud without exemptions is not trivial.

![Azure Security Recommendations](https://platformplatformgithub.blob.core.windows.net/AzureSecurityRecommendations.png)
