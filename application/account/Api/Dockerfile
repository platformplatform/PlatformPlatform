FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra

WORKDIR /app
COPY ./Api/publish .

USER app

ENTRYPOINT ["dotnet", "PlatformPlatform.Account.Api.dll"]
