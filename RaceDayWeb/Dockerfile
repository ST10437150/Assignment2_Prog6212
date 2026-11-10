# ---------- build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first so the layer is cached until the project file changes
COPY RaceDayWeb.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish RaceDayWeb.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish ./

# Run as the non-root user provided by the .NET 8 images
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "RaceDayWeb.dll"]
