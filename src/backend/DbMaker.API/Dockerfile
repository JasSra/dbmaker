# Use the official .NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

# Use the .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["DbMaker.API/DbMaker.API.csproj", "DbMaker.API/"]
COPY ["DbMaker.Shared/DbMaker.Shared.csproj", "DbMaker.Shared/"]
RUN dotnet restore "DbMaker.API/DbMaker.API.csproj"

# Copy all source code
COPY . .
WORKDIR "/src/DbMaker.API"

# Build the application
RUN dotnet build "DbMaker.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "DbMaker.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create data directory
RUN mkdir -p /app/data

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "DbMaker.API.dll"]
