FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine
WORKDIR /app
EXPOSE 80
EXPOSE 443
COPY ./publish .
ENTRYPOINT ["dotnet", "PlatformPlatform.AccountManagement.Api.dll"]
