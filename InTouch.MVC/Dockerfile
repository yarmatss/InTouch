# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
# USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["InTouch.MVC/InTouch.MVC.csproj", "InTouch.MVC/"]
RUN dotnet restore "./InTouch.MVC/InTouch.MVC.csproj"
COPY . .
WORKDIR "/src/InTouch.MVC"
RUN dotnet build "./InTouch.MVC.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./InTouch.MVC.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create directories for certificates and keys with proper permissions
RUN mkdir -p /app/certs /app/keys /app/wwwroot/uploads && \
    chmod 755 /app/certs && \
    chmod 777 /app/keys && \
    chmod 777 /app/wwwroot/uploads

# Now set the user after permissions are configured
USER $APP_UID

ENTRYPOINT ["dotnet", "InTouch.MVC.dll"]