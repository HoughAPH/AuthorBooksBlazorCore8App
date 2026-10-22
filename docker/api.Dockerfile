# Build context is the repository root (the API references SharedClassLibrary): see docker-compose.yml.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY SharedClassLibrary/SharedClassLibrary.csproj SharedClassLibrary/
COPY IdentityManagerServerApi/IdentityManagerServerApi.csproj IdentityManagerServerApi/
RUN dotnet restore IdentityManagerServerApi/IdentityManagerServerApi.csproj

COPY SharedClassLibrary SharedClassLibrary
COPY IdentityManagerServerApi IdentityManagerServerApi
RUN dotnet publish IdentityManagerServerApi/IdentityManagerServerApi.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "IdentityManagerServerApi.dll"]
