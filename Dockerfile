# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Copy project files
COPY . ./

# Restore dependencies
RUN dotnet restore

# Build and publish
RUN dotnet publish -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "ChessEngine.dll"]