<p align="center">
  <i align="center">Kick-start the creation of production-ready multi-tenant SaaS solutions with enterprise-grade security 🚀</i>
</p>

<h4 align="center">

[![PlatformPlatform](https://github.com/PlatformPlatform/platformplatform/actions/workflows/platformplatform-build-and-test.yml/badge.svg)](https://github.com/PlatformPlatform/platformplatform/actions/workflows/account-management.yml?query=branch%3Amain)
[![GitHub issues with enhancement label](https://img.shields.io/github/issues-raw/PlatformPlatform/platformplatform/enhancement?label=enhancements&logo=github&color=%23A2EEEF)](https://github.com/orgs/PlatformPlatform/projects/1/views/3?filterQuery=-status%3A%22%E2%9C%85+Done%22+label%3Aenhancement)
[![GitHub issues with roadmap label](https://img.shields.io/github/issues-raw/PlatformPlatform/platformplatform/roadmap?label=roadmap&logo=github&color=%23006B75)](https://github.com/orgs/PlatformPlatform/projects/2/views/2?filterQuery=is%3Aopen+label%3Aroadmap)
[![GitHub issues with bug label](https://img.shields.io/github/issues-raw/PlatformPlatform/platformplatform/bug?label=bugs&logo=github&color=red)](https://github.com/PlatformPlatform/platformplatform/issues?q=is%3Aissue+is%3Aopen+label%3Abug)

[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=PlatformPlatform_platformplatform&metric=coverage)](https://sonarcloud.io/component_measures?id=PlatformPlatform_platformplatform&metric=coverage)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=PlatformPlatform_platformplatform&metric=alert_status)](https://sonarcloud.io/summary/overall?id=PlatformPlatform_platformplatform)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=PlatformPlatform_platformplatform&metric=security_rating)](https://sonarcloud.io/component_measures?id=PlatformPlatform_platformplatform&metric=Security)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=PlatformPlatform_platformplatform&metric=reliability_rating)](https://sonarcloud.io/component_measures?id=PlatformPlatform_platformplatform&metric=Reliability)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=PlatformPlatform_platformplatform&metric=sqale_rating)](https://sonarcloud.io/component_measures?id=PlatformPlatform_platformplatform&metric=Maintainability)

[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=PlatformPlatform_platformplatform&metric=code_smells)](https://sonarcloud.io/project/issues?id=PlatformPlatform_platformplatform&resolved=false&types=CODE_SMELL)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=PlatformPlatform_platformplatform&metric=vulnerabilities)](https://sonarcloud.io/project/issues?id=PlatformPlatform_platformplatform&resolved=false&types=VULNERABILITY)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=PlatformPlatform_platformplatform&metric=bugs)](https://sonarcloud.io/project/issues?id=PlatformPlatform_platformplatform&resolved=false&types=BUG)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=PlatformPlatform_platformplatform&metric=sqale_index)](https://sonarcloud.io/component_measures?id=PlatformPlatform_platformplatform&metric=sqale_index)

</h4>

## 👋 Welcome to PlatformPlatform

PlatformPlatform aims to showcase an end-to-end solution for building a multi-tenant application using Azure, .NET, React, Infrastructure as Code, GitHub workflows, and more. The roadmap includes features such as Single Sign-On (SSO), subscription management, usage tracking, feature flags, A/B testing, rate limiting, multi-region, disaster recovery, localization, accessibility, and much more. Follow the [continuously updated roadmap here](https://github.com/orgs/PlatformPlatform/projects/2/views/2).

Just getting off the ground, your star can help lift this higher! ⭐ Thanks!

## .NET backend with Clean Architecture, DDD, CQRS, and Minimal API

The backend is built using the most popular, mature, and commonly used technologies in the .NET ecosystem:

- [.NET](https://dotnet.microsoft.com) and [C#](https://learn.microsoft.com/en-us/dotnet/csharp/tour-of-csharp)
- [ASP.NET Minimal API](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [Entity Framework](https://learn.microsoft.com/en-us/ef)
- [MediatR](https://github.com/jbogard/MediatR)
- [FluentValidation](https://fluentvalidation.net)
- [Mapster](https://github.com/MapsterMapper/Mapster)
- [XUnit](https://xunit.net), [FluentAssertions](https://fluentassertions.com), [NSubstitute](https://nsubstitute.github.io), and [Bogus](https://github.com/bchavez/Bogus)
- [SonarCloud](https://sonarcloud.io) and [JetBrains Code style and Cleanup](https://www.jetbrains.com/help/rider/Code_Style_Assistance.html)

<details>

<summary>Read more about the backend architecture</summary>

- **Clean Architecture**: The codebase is organized into layers that promote separation of concerns and maintainability.
- **Domain-Driven Design (DDD)**: DDD principles are applied to ensure a clear and expressive domain model.
- **Command Query Responsibility Segregation (CQRS)**: This clearly separates read (query) and write (command) operations, adhering to the single responsibility principle (each action is in a separate command).
- **Screaming architecture**: The architecture is designed with namespaces (folders) per feature, making the concepts easily visible and expressive, rather than organizing the code by types like models and repositories.
- **MediatR pipelines**: MediatR pipeline behaviors are used to ensure consistent handling of cross-cutting concerns like validation, unit of work, and handling of domain events.
- **Strongly Typed IDs**: The codebase uses strongly typed IDs, which are a combination of the entity type and the entity ID. This is even at the outer API layer, and Swagger translates this to the underlying contract. This ensures type safety and consistency across the codebase.
- **JetBrains Code style and Cleanup**: JetBrains Rider/ReSharper is used for code style and automatic cleanup (configured in `.editorconfig`), ensuring consistent code formatting. No need to discuss tabs vs. spaces anymore; Invalid formatting breaks the build.
- **Monolith prepared for self-contained systems**: The codebase is organized into a monolith, but the architecture is prepared for splitting in to self-contained systems. A self-contained system is a large microservice (or a small monolith) that contains the full stack including frontend, background jobs, etc. These can be developed, tested, deployed, and scaled in isolation, making it a good compromise between a large monolith and many small microservices. Unlike the popular backend-for-frontend (BFF) style with one shared frontend, this allows teams to work fully independently.
- **Shared Kernel**: The codebase uses a shared kernel for all the boilerplate code required to build a clean codebase. The shared kernel ensures consistency between self-contained systems, e.g., enforcing tenant isolation, auditing, tracking, implementation of tactical DDD patterns like aggregate, entities, repository base, ID generation, etc.

Although some features like authentication and multi-tenancy are not yet implemented, the current implementation serves as a solid foundation for building business logic without unnecessary boilerplate.

</details>

## React frontend with TypeScript, Bun, and Ant Design

The Frontend has not yet been created. The plan is to eventually create a frontend with these technologies:

- [React](https://react.dev)
- [TypeScript](https://www.typescriptlang.org)
- [Bun](https://bun.sh)
- [Ant Design](https://ant.design)

## Azure cloud infrastructure with enterprise-grade security and zero secrets

PlatformPlatform's cloud infrastructure is built using the latest Azure Platform as a Service (PaaS) technologies:

- [Azure Container Apps](https://learn.microsoft.com/en-us/azure/container-apps/overview)
- [Azure SQL](https://azure.microsoft.com/en-us/products/azure-sql)
- [Azure Blob Storage](https://azure.microsoft.com/en-us/services/storage/blobs)
- [Azure Service Bus](https://azure.microsoft.com/en-us/services/service-bus)
- [Azure Key Vault](https://azure.microsoft.com/en-us/services/key-vault)
- [Azure Application Insights](https://azure.microsoft.com/en-us/services/monitor)
- [Azure Log Analytics](https://azure.microsoft.com/en-us/services/monitor)
- [Azure Virtual Network](https://azure.microsoft.com/en-us/services/virtual-network)
- [Azure Managed Identities](https://docs.microsoft.com/en-us/azure/active-directory/lifecyclesmanaged-identities-azure-resources/overview)
- [Azure Container Registry](https://azure.microsoft.com/en-us/services/container-registry)
- [Microsoft Defender for Cloud](https://azure.microsoft.com/en-us/products/defender-for-cloud)

<details>

<summary>Read more about this enterprise-grade architecture</summary>

- **Platform as a Service (PaaS) technologies**: Azure is the leading Cloud Service Provider (CSP) when it comes to PaaS technologies. PlatformPlatform uses PaaS technologies which are fully managed by Microsoft, as opposed to Infrastructure as a Service (IaaS) technologies where the customer is responsible for the underlying infrastructure. This means that Microsoft is responsible for the availability of the infrastructure, and you are only responsible for the application and data. This makes it possible for even a small team to run a highly scalable, stable, and secure solution.
- **Enterprise-grade security with zero secrets**:
  - **Managed Identities**: No secrets are used when Container Apps connect to e.g. Databases, Blob Storage, and Service Bus. The infrastructure uses Managed Identities for all communication with Azure resources, eliminating the need for secrets.
  - **Federated credentials**: Deployment from GitHub to Azure is done using federated credentials, establishing a trust between the GitHub repository and Azure subscription based on the repository's URL, without the need for secrets.
  - **No secrets expires**: Since no secrets are used, there is no need to rotate secrets, and no risk of secrets expiring.
  - **100% Security Score**: The current infrastructure configuration follows best practices, and the current setup code achieves a 100% Security Score in Microsoft Defender for Cloud. This minimizes the attack surface and protects against even sophisticated attacks.
- **Automatic certificate management**: The infrastructure is configured to automatically request and renew SSL certificates from Let's Encrypt, eliminating the need for manual certificate management.
- **Multiple environments**: The setup includes different environments like Development, Staging, and Production, deployed into clearly named resource groups within a single Azure Subscription.
- **Multi-region**: Spinning up a cluster in a new region is a matter of adding one extra deployment job to the GitHub workflow. This allows customers to select a region where their data is close to the user and local data protection laws like GDPR, CCPA, etc. are followed.
- **Azure Container Apps**: The application is hosted using Azure Container Apps, which is a new service from Azure that provides a fully managed Kubernetes environment for running containerized applications. You don't need to be a Kubernetes expert to run your application in a scalable and secure environment.
- **Scaling from zero to millions of users**: The Azure Container App Environment is configured to scale from zero to millions of users, and the infrastructure is configured to scale automatically based on load. This means the starting costs are very low, and the solution can scale to millions of users without any manual intervention. This enables having Development and Staging environments running with very low costs.
- **Azure SQL**: The database is hosted using Azure SQL Database, which is a fully managed SQL Server instance. SQL Server is known for its high performance, stability, scalability, and security. The server will easily handle millions of users with single-digit millisecond response times.

</details>

## GitHub SDLC for deploying application and infrastructure in minutes

PlatformPlatform is built on a solid foundation for a modern software development lifecycle (SDLC):

- [GitHub Pull Requests](https://docs.github.com/en/pull-requests)
- [GitHub Actions](https://docs.github.com/en/actions)
- [GitHub projects](https://docs.github.com/en/issues/planning-and-tracking-with-projects/learning-about-projects/quickstart-for-projects)
- [GitHub environments](https://docs.github.com/en/actions/reference/environments)
- [GitHub CODEOWNERS](https://docs.github.com/en/github/creating-cloning-and-archiving-repositories/about-code-owners)
- [GitHub dependabot](https://docs.github.com/en/code-security/dependabot)
- [Bash scripts](https://www.gnu.org/software/bash/)
- [Bicep](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/overview)

## Screenshots

This is how it looks when GitHub workflows have deployed Azure Infrastructure:

![GitHub Environments](https://camo.githubusercontent.com/9b9c33e6c18939d49c3571c610585713707d7b553095270a78c3fe39dc49f811/68747470733a2f2f6d656469612e636c65616e73686f742e636c6f75642f6d656469612f34363533392f7652617130366a374c314b696d4b4a3247434a4e6d724a776c69754572313752577366486f6b78492e6a7065673f457870697265733d31363935353137323638265369676e61747572653d6c6d5171664e59777a735a71634d707a45634b646c51714b636b796b6a706d376b6c62442d5437395a794d537542704b666332794b417832414a6a4a45674a6543575657736671714836624d4c44323974303049317a726848686e59516c4f314f5859665266586b5a6f46304a4c32506d594b5373466a56583763564c4d643774626a48684c6748497e4565573464544f6f564e3434737573436d4452354335383555424b3075626d423161566b763062534e4d466b4435705438394f78656744694f326f4b763061715037464e57764377516978426146314d3830726f36456857514e506d6d4e42345265684e3066784e346676424b3865376573364a34556c5a6b386543687774363953745132395364697635514a756177714a7e413146556e3443796666625737467a725734684d5755554b6f78734d6468596e7667546b78634b48394e663477542d76336c52414c544130515f5f264b65792d506169722d49643d4b3236394a4d4154395a4634475a)

These are the resource groups created when deploying one staging cluster, and two production clusters:

![PlatformPlatform Resoruce Groups](https://camo.githubusercontent.com/cfe23aa287e301b2cc4d510a510a8ba6f718de1b295ab6e4c2ecc9ea99ac7978/68747470733a2f2f6d656469612e636c65616e73686f742e636c6f75642f6d656469612f34363533392f7137446d537378583330544e614e61636c6d4148456d4769706b6c644d6f7235583139356e5161322e6a7065673f457870697265733d31363935343932393131265369676e61747572653d473461595775634853706655524169776e66456778646874414a7e38413442713656417a6639427469526746496b5238646a754647577a754f4d70714244477675773435527e75452d7a312d786b30613375614271383835475649756a5064622d397e48636c3046367577734f3169644f486c77586431726f47672d6e616e4a6b4461694d43593056763934797675486a5362774a696a4954754161736e77627867444370376372793258584f626f525978706a4178763346616872576c2d306f63555554747a77706f764b454472564843547638567968746b354f44664344666437454530713943434f6e4678575242396e39736e565676503451495975634c4e4d6d6a774d2d6974495277482d336c614b7e355674347132496e6c75427a74745842666c70613570676f755839543776574a5a4c4b7a7a4c466550367a4e68477171456279705547794f6e7033426b46703250415f5f264b65792d506169722d49643d4b3236394a4d4154395a4634475a)

 This is the security score after deploying PlatformPlatform resources to Azure. Achieving a 100% security score in Azure Defender for Cloud without exemptions is not trivial.

![Azure Security Recommendations](https://camo.githubusercontent.com/7f4217d4f4f96cd2fa83c0047fe162375a8dffe55d616e66bd4fde4baccad4d5/68747470733a2f2f6d656469612e636c65616e73686f742e636c6f75642f6d656469612f34363533392f385a47635a5972723034337a31535846764e716b696c5465544c6f527737726663717755335472392e6a7065673f457870697265733d31363934333935323634265369676e61747572653d4f736b336a447e353879396c466b3271464d4843575a4e39454b374c33456964647e706d59506a6830716f7a7e675243336c6d3938515148646b336b6a61716641526a626d66506f4d485543795767383445634b556433347831525730434f68454637426a787568774e64365268557e444b6165457178515045787251767362766f525a5472453041366b37704b6279566733545638585452544b7e44614d396f5574626571545a6d62705a4a692d5646674f645157724c545733595533556e716a4244373056354d4354444a4e466d656c337347552d7272316c526137567347384b444673443176697543517768762d584676704962506b584c6e374e4c73453833695354676a76324c426d7067754d43764c496d79555a494249617a78534c42354238784c73316f5141746661495a614830487552483462684b672d504b37424f73765a6934304b54567e327137366a506437775f5f264b65792d506169722d49643d4b3236394a4d4154395a4634475a)

## Developer environment for both Mac and Windows

### Prerequisites

PlatformPlatform is designed to support development on both Mac and Windows. The only requirements are:

- [.NET](https://dotnet.microsoft.com)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [JetBrains Rider](https://www.jetbrains.com/rider) or [Visual Studio](https://visualstudio.microsoft.com) with [JetBrains ReSharper](https://www.jetbrains.com/resharper)

<details>

<summary>Want to use Visual Studio Code?</summary>

While [Visual Studio Code](https://code.visualstudio.com/) can be used and ReSharper is not strictly necessary, in both cases, you should either install the free [ReSharper command line tool](https://www.jetbrains.com/help/resharper/ReSharper_Command_Line_Tools.html) to ensure the codebase is formatted correctly.

To install ReSharper command line tool run this command

```bash
dotnet tool install -g JetBrains.ReSharper.GlobalTools # Add --arch arm64 for Apple silicon
```

Having a strong conventions for naming, formatting, code style from the start is saving time and giving high quality code.

To inspect code run this command (Use Solution-Wide Analysis in Rider/ReSharper)

```bash
jb inspectcode PlatformPlatform.sln --build --output=result.xml
```

To clean up code this command (equvelent to `Ctrl+E+C` / `Cmd+E+C` in Rider/ReSharper)

```bash
jb cleanupcode PlatformPlatform.sln
```

</details>

### Get started

To debug the system locally we run SQL Server using Docker. The SQL Server password used is stored in an environment variable, and added to the connectionstring by .NET (when running in Azure we use Managed Identities instead of passwords).

Likewise, when running .NET self-contained systems on `localhost` we need a valid and trusted certificate, and we need the password when building the Docker image.

Please run the following scripts once to setup your localhost.

- **macOS**:

  ```bash
  # Generate SQL_SERVER_PASSWORD and add to environment variables
  SQL_SERVER_PASSWORD=$(curl -s "https://www.random.org/strings/?num=1&len=13&digits=on&upperalpha=on&loweralpha=on&unique=on&format=plain&rnd=new")_
  echo "export SQL_SERVER_PASSWORD='$SQL_SERVER_PASSWORD'" >> ~/.zshrc  # or ~/.bashrc, ~/.bash_profile

  # Generate CERTIFICATE_PASSWORD and add to environment variables
  CERTIFICATE_PASSWORD=$(curl -s "https://www.random.org/strings/?num=1&len=13&digits=on&upperalpha=on&loweralpha=on&unique=on&format=plain&rnd=new")_
  echo "export CERTIFICATE_PASSWORD='$CERTIFICATE_PASSWORD'" >> ~/.zshrc  # or ~/.bashrc, ~/.bash_profile

  # Generate dev certificate
  dotnet dev-certs https --trust -ep ${HOME}/.aspnet/https/localhost.pfx -p $CERTIFICATE_PASSWORD
  ```

- **Windows**:

  ```powershell
  # Generate SQL_SERVER_PASSWORD and add to environment variables
  $SQL_SERVER_PASSWORD = Invoke-RestMethod -Uri "https://www.random.org/strings/?num=1&len=13&digits=on&upperalpha=on&loweralpha=on&unique=on&format=plain&rnd=new" + "_"
  [Environment]::SetEnvironmentVariable('SQL_SERVER_PASSWORD', $SQL_SERVER_PASSWORD, [System.EnvironmentVariableTarget]::User)

  # Generate CERTIFICATE_PASSWORD and add to environment variables
  $CERTIFICATE_PASSWORD = Invoke-RestMethod -Uri "https://www.random.org/strings/?num=1&len=13&digits=on&upperalpha=on&loweralpha=on&unique=on&format=plain&rnd=new" + "_"
  [Environment]::SetEnvironmentVariable('CERTIFICATE_PASSWORD', $CERTIFICATE_PASSWORD, [System.EnvironmentVariableTarget]::User)

  # Generate dev certificate
  dotnet dev-certs https --trust -ep $env:USERPROFILE\.aspnet\https\localhost.pfx -p $env:CERTIFICATE_PASSWORD
  ```

To test the system you can now run this (the first time the API runs it will create the database and schema):

```bash
# Run from the application folder
docker compose up -d
open https://localhost:8443/swagger
```

While developing you might want to only run the SQL Server, and compile and debug the source code from Rider or Visual Studio:

```bash
# Run from the application folder
docker compose up sql-server -d
```

## Automated first-time setup of GitHub and Azure

Refer to the [Set up automatic deployment of Azure infrastructure and code from GitHub](cloud-infrastructure/README.md#set-up-automatic-deployment-of-azure-infrastructure-and-code-from-github) in the [cloud-infrastructure/README](/cloud-infrastructure/README.md). It's just one Bash script that you run locally, and it will set up everything for you. 🎉
