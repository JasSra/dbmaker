# Use the official .NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Use the .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["DbMaker.Workers/DbMaker.Workers.csproj", "DbMaker.Workers/"]
COPY ["DbMaker.Shared/DbMaker.Shared.csproj", "DbMaker.Shared/"]
RUN dotnet restore "DbMaker.Workers/DbMaker.Workers.csproj"

# Copy all source code
COPY . .
WORKDIR "/src/DbMaker.Workers"

# Build the application
RUN dotnet build "DbMaker.Workers.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "DbMaker.Workers.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app

# Create data directory
RUN mkdir -p /app/data

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "DbMaker.Workers.dll"]
