FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine

WORKDIR /app
COPY ./AppGateway/publish .

USER app

ENTRYPOINT ["dotnet", "PlatformPlatform.AppGateway.dll"]
