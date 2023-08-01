# Use the ASP.NET 7.0 Alpine image
FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine

# Create a group and user 'nonroot'
RUN addgroup -S nonroot \
    && adduser -S nonroot -G nonroot

# Install ICU libraries
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Set the working directory in the container
WORKDIR /app

# Expose ports
EXPOSE 80
EXPOSE 443

# Switch to 'nonroot' user
USER nonroot

# Copy the build app to the container
COPY ./publish .

# Configure the entry point
ENTRYPOINT ["dotnet", "PlatformPlatform.AccountManagement.Api.dll"]
