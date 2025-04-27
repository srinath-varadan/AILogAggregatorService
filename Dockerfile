# Use official .NET 8 runtime for production
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Build stage with SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only csproj and restore early (better caching)
COPY ["AILogAggregator.csproj", "./"]
RUN dotnet restore "AILogAggregator.csproj"

# Copy everything else and build
COPY . .
RUN dotnet publish "AILogAggregator.csproj" -c Release -o /app/publish

# Final runtime stage
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
# Start the service
ENTRYPOINT ["dotnet", "AILogAggregator.dll"]