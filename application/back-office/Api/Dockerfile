FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine

RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

WORKDIR /app
COPY ./Api/publish .

USER app

ENTRYPOINT ["dotnet", "PlatformPlatform.BackOffice.Api.dll"]
