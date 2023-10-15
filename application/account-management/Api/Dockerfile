# Build stage
FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build
ARG VERSION=1.0.0.0  # Use version 1.0.0.0 as default
WORKDIR /src

# Copy all source code to /src
COPY ./ ./

# Set the working directory to the API project
WORKDIR /src/account-management/Api
RUN dotnet restore
RUN dotnet publish -c Release -o /publish /p:Version="$VERSION"


# Runtime stage (start with a clean image and copy only published binairies)
FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine AS runtime
WORKDIR /app
COPY --from=build /publish ./

# Install ICU libraries and other essentials
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Create non-root user
RUN addgroup -S nonroot && adduser -S nonroot -G nonroot
USER nonroot

# Expose port 8443 (non-root user can't use ports below 1024)
EXPOSE 8443

# Entry point
ENTRYPOINT ["dotnet", "PlatformPlatform.AccountManagement.Api.dll"]
