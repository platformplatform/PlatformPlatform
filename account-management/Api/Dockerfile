FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine

# Install ICU libraries
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

WORKDIR /app
EXPOSE 80
EXPOSE 443
COPY ./publish .
ENTRYPOINT ["dotnet", "PlatformPlatform.AccountManagement.Api.dll"]
