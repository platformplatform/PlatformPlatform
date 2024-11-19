FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine

RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

WORKDIR /app
COPY ./Workers/publish .

USER app

ENTRYPOINT ["dotnet", "PlatformPlatform.AccountManagement.Workers.dll"]
