Examples:

[![Build Account Management and run tests](https://github.com/PlatformPlatform/platformplatform/actions/workflows/account-management.yml/badge.svg)](https://github.com/PlatformPlatform/platformplatform/actions/workflows/account-management.yml?query=branch%3Amain)
[![GitHub issues with bug label](https://img.shields.io/github/issues-raw/PlatformPlatform/platformplatform/bug?label=bugs&logo=github&color=red)](https://github.com/PlatformPlatform/platformplatform/issues?q=is%3Aissue+is%3Aopen+label%3Abug)
[![GitHub issues with enhancement label](https://img.shields.io/github/issues-raw/PlatformPlatform/platformplatform/enhancement?label=enhancements&logo=github&color=%23A2EEEF)](https://github.com/orgs/PlatformPlatform/projects/1/views/3?filterQuery=-status%3A%22%E2%9C%85+Done%22+label%3Aenhancement)
[![GitHub issues with roadmap label](https://img.shields.io/github/issues-raw/PlatformPlatform/platformplatform/roadmap?label=roadmap&logo=github&color=%23006B75)](https://github.com/orgs/PlatformPlatform/projects/2/views/2?filterQuery=is%3Aopen+label%3Aroadmap)

[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=PlatformPlatform_platformplatform&metric=coverage)](https://sonarcloud.io/component_measures?id=PlatformPlatform_platformplatform&metric=coverage)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=PlatformPlatform_platformplatform&metric=alert_status)](https://sonarcloud.io/summary/overall?id=PlatformPlatform_platformplatform)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=PlatformPlatform_platformplatform&metric=security_rating)](https://sonarcloud.io/component_measures?id=PlatformPlatform_platformplatform&metric=Security)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=PlatformPlatform_platformplatform&metric=reliability_rating)](https://sonarcloud.io/component_measures?id=PlatformPlatform_platformplatform&metric=Reliability)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=PlatformPlatform_platformplatform&metric=sqale_rating)](https://sonarcloud.io/component_measures?id=PlatformPlatform_platformplatform&metric=Maintainability)

[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=PlatformPlatform_platformplatform&metric=code_smells)](https://sonarcloud.io/project/issues?id=PlatformPlatform_platformplatform&resolved=false&types=CODE_SMELL)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=PlatformPlatform_platformplatform&metric=vulnerabilities)](https://sonarcloud.io/project/issues?id=PlatformPlatform_platformplatform&resolved=false&types=VULNERABILITY)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=PlatformPlatform_platformplatform&metric=bugs)](https://sonarcloud.io/project/issues?id=PlatformPlatform_platformplatform&resolved=false&types=BUG)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=PlatformPlatform_platformplatform&metric=sqale_index)](https://sonarcloud.io/component_measures?id=PlatformPlatform_platformplatform&metric=sqale_index)


# 👋 Welcome to PlatformPlatform 

The ultimate open-source foundation designed for startups looking to create multi-tenant cloud SaaS services with ease, speed, scalability and enterprise grade security. Our platform showcases the best practices in building SaaS products, combining a cutting-edge technology stack, robust cloud architecture using Infrastructure as Code, full DevOps pipelines, and powerful tools to transform the way you develop and grow your software solutions. 🚀 

Embrace the power of PlatformPlatform, built using .NET 7.0, C# 11.0, ASP.NET Minimal API, Entity Framework, Azure SQL, MediatR, and Fluent Validation. Elevate your frontend development with React, TypeScript, SCSS, and Jest. Leverage Azure Container Apps, Azure Service Bus, and other Azure PaaS services to create a seamless, reliable infrastructure. The platform is built showcasing Clean Architecture with Domain-Driven Design and CQRS at its core. 🏂

Please note that, as of now, PlatformPlatform is still in a very early stage. 🐣 You can follow our [backlog and roadmap](https://github.com/PlatformPlatform/platformplatform/projects) on the Projects tab.


## 🛠️ Setting up local debugging for Mac and Windows

### Install SQL Server for local debugging
PlatformPlatform requires a SQL Server instance for debugging locally. You can use Azure SQL Edge in Docker Desktop on both Mac and Windows. On Windows you can also install SQL Server or SQL Server Express locally.

#### Running Azure SQL Edge in Docker Desktop

1. Install [Docker Desktop](https://www.docker.com/products/docker-desktop) if you haven't already.
2. Run the following command to pull the Azure SQL Edge image and start a container (use a password of your choice):

       docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=!MySecretPassword1" -p 1433:1433 --name sql_server -d mcr.microsoft.com/azure-sql-edge

3. Add the following line to your shell's configuration file:

       export SQL_DATABASE_PASSWORD='!MySecretPassword1'

- If you're using bash, edit the `~/.bashrc` or `~/.bash_profile` file.
- If you're using zsh, edit the `~/.zshrc` file.

4. Restart the terminal and run `echo $SQL_DATABASE_PASSWORD` to verify that the environment variable is set correctly.

#### Windows: Installing SQL Server or SQL Server Express

1. Download and install [SQL Server Developer Edition](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) or [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads). Alternatively you can also use [Docker Desktop](https://www.docker.com/products/docker-desktop).
2. During the installation, set the password for the `sa` user as `!MySecretPassword1`.
3. Set the enviroment varible in Windows (PowerShell) using this command:

       $Env:DB_PASSWORD="!MySecretPassword1"

### Clone the repository

1. Open a terminal and navigate to the folder where you want to clone the repository.
2. Run the following command to clone the repository:

       git clone https://github.com/PlatformPlatform/platformplatform.git

### Run and debug

1. Open a terminal and navigate to the root folder of the cloned PlatformPlatform repository.
2. Change directory to the `account-management` folder: `cd account-management`.
3. Run the following command to restore the dependencies and tools of the project: `dotnet restore`.
4. Run the following command to build and run the application: `dotnet run --project AccountManagement.WebApi`.
5. The application should now be running. You can access the API by navigating to `https://localhost:5001` or `http://localhost:5002`.
6. To run tests, navigate to the test project folder (e.g., `AccountManagement.Tests`) and run the following command: `dotnet test`.
7. To debug the application, you can use an IDE like JetBrains Rider on both Windows and Mac or Visual Studio with ReSharper on Windows. Open the solution file (`AccountManagement.sln`) in your preferred IDE and start debugging using the built-in debugging tools.

You should now be able to run and debug your application locally on both Mac and Windows.


## 🤝 Code of Conduct 

We are committed to fostering an open and welcoming environment for everyone involved in the project. Please read our [Code of Conduct](.github/CODE_OF_CONDUCT.md) to understand our community guidelines and expectations.


## 🐞 Reporting Bugs and Feature Requests

If you encounter any bugs or have ideas for new features, we'd love to hear about them! To report a bug, please use this [bug report template](https://github.com/PlatformPlatform/platformplatform/issues/new?template=bug_report.md&labels=bug). For feature requests, please use our [feature request template](https://github.com/PlatformPlatform/platformplatform/issues/new?template=feature_request.md&labels=enhancement). This will help us keep track of issues and enhancements and respond to them efficiently.


## 💻 Contributing 

We appreciate any contributions to the PlatformPlatform project! If you'd like to contribute, please read our [Contributing Guidelines](.github/CONTRIBUTING.md) to understand the process and best practices for submitting your changes.


## 🔒 Security Policy 

We take the security of our platform seriously. If you discover any security-related issues or vulnerabilities, please review and follow our [Security Policy](.github/SECURITY.md) to report them responsibly. You can report security incidents using our [GitHub Security Advisories page](https://github.com/PlatformPlatform/platformplatform/security/advisories/new).


## 🔏 License 

PlatformPlatform is released under the [MIT License](LICENSE).
