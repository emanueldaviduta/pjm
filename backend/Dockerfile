FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until a project file changes.
COPY src/ProjectManager.Domain/ProjectManager.Domain.csproj src/ProjectManager.Domain/
COPY src/ProjectManager.Application/ProjectManager.Application.csproj src/ProjectManager.Application/
COPY src/ProjectManager.Infrastructure/ProjectManager.Infrastructure.csproj src/ProjectManager.Infrastructure/
COPY src/ProjectManager.Api/ProjectManager.Api.csproj src/ProjectManager.Api/
RUN dotnet restore src/ProjectManager.Api/ProjectManager.Api.csproj

COPY src/ src/
RUN dotnet publish src/ProjectManager.Api/ProjectManager.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ProjectManager.Api.dll"]
