# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy everything (simpler approach)
COPY . .

# Restore, build, and publish in one command
RUN dotnet publish LifeSprint.Api/LifeSprint.Api.csproj \
    -c Release \
    -o /app/publish \
    --verbosity normal

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "LifeSprint.Api.dll"]
