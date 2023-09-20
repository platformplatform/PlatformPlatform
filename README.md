[![PlatformPlatform](https://github.com/PlatformPlatform/platformplatform/actions/workflows/platformplatform-build-and-test.yml/badge.svg)](https://github.com/PlatformPlatform/platformplatform/actions/workflows/account-management.yml?query=branch%3Amain)
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

Please note that, as of now, PlatformPlatform is still in a very early stage. 🐣 You can follow our always up-to-date [backlog and roadmap](https://github.com/PlatformPlatform/platformplatform/projects) on the Projects tab.

PlatformPlatform is designed to showcase a state-of-the-art cloud solution using Azure, .NET, React, TypeScript, Infrastructure as Code, GitHub workflows, and much more. The goal is to enable creating production-ready, multi-tenant SaaS services with ease, speed, scalability, and enterprise-grade security. 🚀

PlatformPlatform is built around Microsoft technologies, which play nicely together.

## .NET Backend with DDD, CQRS, Clean Architecture, and Minimal API

The backend is built showcasing Clean Architecture with Domain-Driven Design and CQRS at its core. The backend API and services are built using the newest versions of technologies like .NET 7.0, C# 11.0, ASP.NET Minimal API, MediatR 12, Fluent Validation, and Entity Framework 7. While not feature-complete (e.g., authentication and multi-tenant not started), the current implementation showcases a best-in-class DDD, CQRS solution, and a very slim API front-end, making it very easy to create business logic without any boiler code.

## Monolith prepared for microservices

While the solution is currently a monolith, the [shared-kernel](/application/shared-kernel) hosts all the common infrastructure. This includes tactical DDD concepts like Aggregate Roots, Entities, Base Repository, UnitOfWork, DomainEvents, etc. The [shared-kernel](/application/shared-kernel) contains common classes to create a clean architecture using CQRS, with MediatR behaviours, reusable validation logic. The [shared-kernel](/application/shared-kernel) also contains other reusable components like Global Exception handler, Entity Framework filters, etc. This makes the development of the actual application logic very clean, and it's very easy to create a second self-contained system.

A self-contained system is a large microservice (or a small monolith) that contains the full stack including frontend, background jobs, etc. These can be developed, tested, deployed, and scaled in isolation, making it a good compromise between a large Monolith and many small microservices with a large monolithic frontend. [account-management](/application/account-management) is an example of a self-contained system.

## Azure Cloud Infrastructure

Currently, PlatformPlatform uses Azure Container Apps (managed Kubernetes), Azure SQL, Azure Storage, Azure Service Bus, Azure Log Analytics, Azure Application Insights, Azure Key Vault, etc. All these are PaaS (Platform as a Service) technologies, making everything very reliable and requiring little to no maintenance. Everything is designed with Azure Managed Identities (no passwords or secrets). The GitHub workflows show how to deploy these using Azure Bicep (Infrastructure as Code) to multiple environments (`Staging` and `Production`). The infrastructure is built with a multi-region setup, and it's extremely easy to set up an extra cluster in e.g., US, Australia, etc. (just create a copy of a script file with cluster variables and duplicate a deployment step).

## Frontend

The plan is to eventually create a frontend with React and TypeScript with a strongly typed integration to the backend API. The plan is to use a mature Design System like [Ant Design](https://ant.design).

# 🛠️ Setting up local debugging for Mac or Windows

PlatformPlatform requires a SQL Server instance for debugging locally. You can use Azure SQL Edge in Docker Desktop on both Mac and Windows.

## Running Azure SQL Edge in Docker Desktop

1. Install [Docker Desktop](https://www.docker.com/products/docker-desktop) if you haven't already.
2. Run the following command to pull the Azure SQL Edge image and start a container (use a password of your choice):

       docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=!MySecretPassword1" -p 1433:1433 --name sql_server -d mcr.microsoft.com/azure-sql-edge

3. Set the `SQL_DATABASE_PASSWORD` environment variable:
    - On MacOS: Add the following line to your shell's configuration file (`~/.zshrc`, `~/.bashrc`, or `~/.bash_profile` depending on your terminal): `export SQL_DATABASE_PASSWORD='!MySecretPassword1'`
    - On Windows: In PowerShell run the following command: `[Environment]::SetEnvironmentVariable("SQL_DATABASE_PASSWORD", "!MySecretPassword1", "User")`

## Run and debug

1. Clone the repository: `git clone https://github.com/PlatformPlatform/platformplatform.git`
2. Navigate to the `application` folder of the cloned repository and run the following command to restore the dependencies and tools of the project: `dotnet restore`
3. Run the following command to build and run the application: `dotnet run --project account-management/Api`
4. The application should now be running. You can access the API by navigating to `https://localhost:8443`.
5. To run tests, run the following command: `dotnet test`
6. To debug the application, you can use an IDE like JetBrains Rider on both Windows and Mac or Visual Studio on Windows. Open the solution file (`application/PlatformPlatform.sln`) in your preferred IDE and start debugging using the built-in debugging tools. You can also open the `application/account-management/AccountManagement.sln` to work with a lightweight, self-contained system in isolation without the [shared-kernel](/application/shared-kernel).

You should now be able to run and debug your application locally on both Mac and Windows.
