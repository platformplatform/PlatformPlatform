FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled

WORKDIR /app
COPY ./AppGateway/publish .

USER app

ENTRYPOINT ["dotnet", "PlatformPlatform.AppGateway.dll"]
