# Use the official .NET 10 SDK image to build the application
FROM ://microsoft.com AS build-env
WORKDIR /source

# Copy the project file and restore dependencies
COPY ChessEngine.csproj .
RUN dotnet restore

# Copy the rest of the application files and build
COPY . .
RUN dotnet publish ChessEngine.csproj -c Release -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM ://microsoft.com
WORKDIR /app
COPY --from=build-env /app/publish .

# Render dynamically assigns a port via the PORT env variable (usually 10000)
ENV ASPNETCORE_URLS=http://+:10000

ENTRYPOINT ["dotnet", "ChessEngine.dll"]