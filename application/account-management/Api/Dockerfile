FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine

RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

RUN adduser -S nonroot
USER nonroot

WORKDIR /app
COPY ./Api/publish .

EXPOSE 8443

ENTRYPOINT ["dotnet", "PlatformPlatform.AccountManagement.Api.dll"]
