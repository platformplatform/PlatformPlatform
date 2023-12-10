## Developer CLI

PlatformPlatform comes with a command line interface (CLI) to get up and running and help you with daily tasks. The CLI is a .NET console application that can be run on Windows and macOS.

### Installation

Just run the following command to install the CLI:

```bash
dotnet run
```

This will show a prompt to register an alias for the CLI. The default is `pp`, but you can choose any alias you like. After that, the CLI is ready to use.

![PlatformPlatform CLI](https://platformplatformgithub.blob.core.windows.net/$root/PlatformPlatformCli.png)

## Self updating

Since the CLI is compiled from source, it can update itself. When you run the CLI, it will detect any changes in the source code and compile itself. This means that you will always have the latest version of the CLI, and you don't have to inform teammates to update the CLI. This is awesome for teams to share new improvements to the CLI.

## Prerequisites

The CLI will check for the Prerequisites described in the [main README](../README.md) and will prompt you to install them if they are not installed or not up to date.
