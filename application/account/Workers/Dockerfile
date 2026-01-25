FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra

WORKDIR /app
COPY ./Workers/publish .

USER app

ENTRYPOINT ["dotnet", "PlatformPlatform.Account.Workers.dll"]
