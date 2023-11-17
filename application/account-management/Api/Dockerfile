# Build stage
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
# Use version 1.0.0.0 as default
ARG VERSION=1.0.0.0

SHELL ["/bin/bash", "-l", "-c"]
RUN apt-get update
RUN apt-get --no-install-recommends install zip unzip

RUN curl -fsSL https://bun.sh/install | bash

WORKDIR /src

# Copy all source code to /src
COPY ./ ./

# Set the working directory to the WebApp project
WORKDIR /src/account-management/WebApp
RUN bun install --freeze-lockfile

# Set the working directory to the API project
WORKDIR /src/account-management/Api
RUN dotnet tool restore
RUN dotnet restore
RUN dotnet publish -c Release -o /publish /p:Version="$VERSION"


# Runtime stage (start with a clean image and copy only published binairies)
FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine AS runtime

WORKDIR /app/WebApp/dist
COPY --from=build /src/account-management/WebApp/dist ./

WORKDIR /app
COPY --from=build /publish ./

# Install ICU libraries and other essentials
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Create non-root user
RUN adduser -S nonroot
USER nonroot

# Expose port 8443 (non-root user can't use ports below 1024)
EXPOSE 8443

# Entry point
ENTRYPOINT ["dotnet", "PlatformPlatform.AccountManagement.Api.dll"]
